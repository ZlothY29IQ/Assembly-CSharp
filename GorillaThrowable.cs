using System;
using GorillaExtensions;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class GorillaThrowable : MonoBehaviourPun, IPunObservable, IPhotonViewCallback
{
	public int trackingHistorySize;

	public float throwMultiplier;

	public float throwMagnitudeLimit;

	private Vector3[] velocityHistory;

	private Vector3[] headsetVelocityHistory;

	private Vector3[] positionHistory;

	private Vector3[] headsetPositionHistory;

	private Vector3[] rotationHistory;

	private Vector3[] rotationalVelocityHistory;

	private Vector3 previousPosition;

	private Vector3 previousRotation;

	private Vector3 previousHeadsetPosition;

	private int currentIndex;

	private Vector3 currentVelocity;

	private Vector3 currentHeadsetVelocity;

	private Vector3 currentRotationalVelocity;

	public Vector3 denormalizedVelocityAverage;

	private Vector3 denormalizedHeadsetVelocityAverage;

	private Vector3 denormalizedRotationalVelocityAverage;

	private Transform headsetTransform;

	private Vector3 targetPosition;

	private Quaternion targetRotation;

	public bool initialLerp;

	public float lerpValue = 0.4f;

	public float lerpDistanceLimit = 0.01f;

	public bool isHeld;

	public Rigidbody rigidbody;

	private int loopIndex;

	private Transform transformToFollow;

	private Vector3 offset;

	private Quaternion offsetRotation;

	public AudioSource audioSource;

	public int timeLastReceived;

	public bool synchThrow;

	public float tempFloat;

	public Transform grabbingTransform;

	public float pickupLerp;

	public float minVelocity;

	public float maxVelocity;

	public float minVolume;

	public float maxVolume;

	public bool isLinear;

	public float linearMax;

	public float exponThrowMultMax;

	public int bounceAudioClip;

	public virtual void Start()
	{
		offset = Vector3.zero;
		headsetTransform = GTPlayer.Instance.headCollider.transform;
		velocityHistory = new Vector3[trackingHistorySize];
		positionHistory = new Vector3[trackingHistorySize];
		headsetPositionHistory = new Vector3[trackingHistorySize];
		rotationHistory = new Vector3[trackingHistorySize];
		rotationalVelocityHistory = new Vector3[trackingHistorySize];
		for (int i = 0; i < trackingHistorySize; i++)
		{
			velocityHistory[i] = Vector3.zero;
			positionHistory[i] = base.transform.position - headsetTransform.position;
			headsetPositionHistory[i] = headsetTransform.position;
			rotationHistory[i] = base.transform.eulerAngles;
			rotationalVelocityHistory[i] = Vector3.zero;
		}
		currentIndex = 0;
		rigidbody = GetComponentInChildren<Rigidbody>();
	}

	public virtual void LateUpdate()
	{
		if (isHeld && base.photonView.IsMine)
		{
			base.transform.rotation = transformToFollow.rotation * offsetRotation;
			if (!initialLerp && (base.transform.position - transformToFollow.position).magnitude > lerpDistanceLimit)
			{
				base.transform.position = Vector3.Lerp(base.transform.position, transformToFollow.position + transformToFollow.rotation * offset, pickupLerp);
			}
			else
			{
				initialLerp = true;
				base.transform.position = transformToFollow.position + transformToFollow.rotation * offset;
			}
		}
		if (!base.photonView.IsMine)
		{
			rigidbody.isKinematic = true;
			base.transform.position = Vector3.Lerp(base.transform.position, targetPosition, lerpValue);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, targetRotation, lerpValue);
		}
		StoreHistories();
	}

	private void IsHandPushing(XRNode node)
	{
	}

	private void StoreHistories()
	{
		previousPosition = positionHistory[currentIndex];
		previousRotation = rotationHistory[currentIndex];
		previousHeadsetPosition = headsetPositionHistory[currentIndex];
		currentIndex = (currentIndex + 1) % trackingHistorySize;
		currentVelocity = (base.transform.position - headsetTransform.position - previousPosition) / Time.deltaTime;
		currentHeadsetVelocity = (headsetTransform.position - previousHeadsetPosition) / Time.deltaTime;
		currentRotationalVelocity = (base.transform.eulerAngles - previousRotation) / Time.deltaTime;
		denormalizedVelocityAverage = Vector3.zero;
		denormalizedRotationalVelocityAverage = Vector3.zero;
		for (loopIndex = 0; loopIndex < trackingHistorySize; loopIndex++)
		{
			denormalizedVelocityAverage += velocityHistory[loopIndex];
			denormalizedRotationalVelocityAverage += rotationalVelocityHistory[loopIndex];
		}
		denormalizedVelocityAverage /= (float)trackingHistorySize;
		denormalizedRotationalVelocityAverage /= (float)trackingHistorySize;
		velocityHistory[currentIndex] = currentVelocity;
		positionHistory[currentIndex] = base.transform.position - headsetTransform.position;
		headsetPositionHistory[currentIndex] = headsetTransform.position;
		rotationHistory[currentIndex] = base.transform.eulerAngles;
		rotationalVelocityHistory[currentIndex] = currentRotationalVelocity;
	}

	public virtual void Grabbed(Transform grabTransform)
	{
		grabbingTransform = grabTransform;
		isHeld = true;
		transformToFollow = grabbingTransform;
		offsetRotation = base.transform.rotation * Quaternion.Inverse(transformToFollow.rotation);
		initialLerp = false;
		rigidbody.isKinematic = true;
		rigidbody.useGravity = false;
		base.photonView.RequestOwnership();
	}

	public virtual void ThrowThisThingo()
	{
		transformToFollow = null;
		isHeld = false;
		synchThrow = true;
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		rigidbody.isKinematic = false;
		rigidbody.useGravity = true;
		if (isLinear || denormalizedVelocityAverage.magnitude < linearMax)
		{
			if (denormalizedVelocityAverage.magnitude * throwMultiplier < throwMagnitudeLimit)
			{
				rigidbody.linearVelocity = denormalizedVelocityAverage * throwMultiplier + currentHeadsetVelocity;
			}
			else
			{
				rigidbody.linearVelocity = denormalizedVelocityAverage.normalized * throwMagnitudeLimit + currentHeadsetVelocity;
			}
		}
		else
		{
			rigidbody.linearVelocity = denormalizedVelocityAverage.normalized * Mathf.Max(Mathf.Min(Mathf.Pow(throwMultiplier * denormalizedVelocityAverage.magnitude / linearMax, exponThrowMultMax), 0.1f) * denormalizedHeadsetVelocityAverage.magnitude, throwMagnitudeLimit) + currentHeadsetVelocity;
		}
		rigidbody.angularVelocity = denormalizedRotationalVelocityAverage * MathF.PI / 180f;
		rigidbody.MovePosition(rigidbody.transform.position + rigidbody.linearVelocity * Time.deltaTime);
	}

	void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(base.transform.position);
			stream.SendNext(base.transform.rotation);
			stream.SendNext(rigidbody.linearVelocity);
		}
		else
		{
			targetPosition.SetValueSafe((Vector3)stream.ReceiveNext());
			targetRotation.SetValueSafe((Quaternion)stream.ReceiveNext());
			Vector3 v = rigidbody.linearVelocity;
			v.SetValueSafe((Vector3)stream.ReceiveNext());
			rigidbody.linearVelocity = v;
		}
	}

	public virtual void OnCollisionEnter(Collision collision)
	{
		if (collision.collider.GetComponent<GorillaSurfaceOverride>() != null)
		{
			if (NetworkSystem.Instance.InRoom)
			{
				base.photonView.RPC("PlaySurfaceHit", RpcTarget.Others, bounceAudioClip, InterpolateVolume());
			}
			PlaySurfaceHit(collision.collider.GetComponent<GorillaSurfaceOverride>().overrideIndex, InterpolateVolume());
		}
	}

	public void PlaySurfaceHit(int soundIndex, float tapVolume)
	{
		if (soundIndex > -1 && soundIndex < GTPlayer.Instance.materialData.Count)
		{
			audioSource.volume = tapVolume;
			audioSource.clip = (GTPlayer.Instance.materialData[soundIndex].overrideAudio ? GTPlayer.Instance.materialData[soundIndex].audio : GTPlayer.Instance.materialData[0].audio);
			audioSource.GTPlayOneShot(audioSource.clip);
		}
	}

	public float InterpolateVolume()
	{
		return (Mathf.Clamp(rigidbody.linearVelocity.magnitude, minVelocity, maxVelocity) - minVelocity) / (maxVelocity - minVelocity) * (maxVolume - minVolume) + minVolume;
	}
}
