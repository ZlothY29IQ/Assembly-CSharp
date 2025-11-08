using System;
using System.Collections;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class GhostPal : MonoBehaviour
{
	[SerializeField]
	private float minDistanceFromPlayer = 1f;

	[SerializeField]
	private float orbitRadius = 1f;

	[SerializeField]
	private float orbitHeight = 1f;

	[SerializeField]
	private float orbitSpeed = 0.1f;

	[SerializeField]
	private float faceMovementDirectionStrength = 1f;

	[Space]
	[SerializeField]
	private float lookAtDotProductMin = 0.95f;

	[SerializeField]
	private AnimationCurve rotateTowardsPlayerFromLookTime;

	[SerializeField]
	private float minLookTimeToTrigger = 2f;

	[SerializeField]
	private AnimationCurve bounceOnTrigger;

	[SerializeField]
	private AudioSource triggerAudioSource;

	[SerializeField]
	private Vector2 triggerAudioPitchMinMax = new Vector2(0.9f, 1.1f);

	[SerializeField]
	private AudioClip[] triggerAudioClips;

	private VRRig rig;

	private Animator animator;

	private float lookAtTime;

	private bool hasTriggered;

	private Coroutine bounceCoroutine;

	private float bounceHeight;

	private Vector3 trailingPosition;

	private int triggerAudioClipIndex;

	private int neutralAnimID = Animator.StringToHash("Neutral");

	private int friendlyAnimID = Animator.StringToHash("Friendly");

	private void Awake()
	{
		rig = GetComponentInParent<VRRig>();
		animator = GetComponentInChildren<Animator>();
		trailingPosition = base.transform.position;
		triggerAudioClipIndex = triggerAudioClips.GetRandomIndex();
	}

	private IEnumerator BounceOnTrigger()
	{
		float startTime = Time.time;
		while (Time.time - startTime < bounceOnTrigger[bounceOnTrigger.length - 1].time)
		{
			bounceHeight = bounceOnTrigger.Evaluate(Time.time - startTime);
			yield return null;
		}
		bounceHeight = 0f;
	}

	private void LateUpdate()
	{
		Vector3 position = rig.bodyTransform.position;
		Vector3 vector = base.transform.parent.position - position;
		float num = vector.y * 0.5f + orbitHeight;
		vector.y = 0f;
		float num2 = vector.magnitude + minDistanceFromPlayer;
		vector = vector.normalized * num2;
		vector.y = num + bounceHeight;
		double num3 = (double)orbitSpeed * (PhotonNetwork.InRoom ? ((PhotonNetwork.Time - (double)rig.OwningNetPlayer.UserId.GetStaticHash()) * (double)((rig.OwningNetPlayer.ActorNumber % 2 == 0) ? 1 : (-1))) : Time.timeAsDouble);
		Vector3 vector2 = new Vector3(orbitRadius * (float)Math.Cos(num3), 0f, orbitRadius * (float)Math.Sin(num3));
		Vector3 vector3 = position + vector + vector2;
		Vector3 vector4 = vector3 - rig.head.rigTarget.position;
		if (Vector3.Dot(rig.head.rigTarget.forward, vector4.normalized) >= lookAtDotProductMin)
		{
			lookAtTime = Mathf.Min(lookAtTime + Time.deltaTime, Mathf.Max(rotateTowardsPlayerFromLookTime[rotateTowardsPlayerFromLookTime.length - 1].time, minLookTimeToTrigger));
			if (lookAtTime >= minLookTimeToTrigger && !hasTriggered && bounceHeight == 0f)
			{
				animator.SetTrigger(friendlyAnimID);
				bounceCoroutine = StartCoroutine(BounceOnTrigger());
				triggerAudioSource.pitch = UnityEngine.Random.Range(triggerAudioPitchMinMax.x, triggerAudioPitchMinMax.y);
				triggerAudioSource.clip = triggerAudioClips[triggerAudioClipIndex];
				triggerAudioSource.GTPlay();
				triggerAudioClipIndex = (triggerAudioClipIndex + UnityEngine.Random.Range(0, triggerAudioClips.Length - 1)) % triggerAudioClips.Length;
				hasTriggered = true;
			}
		}
		else
		{
			lookAtTime = Mathf.Max(lookAtTime - Time.deltaTime, 0f);
			if (lookAtTime < minLookTimeToTrigger && hasTriggered && bounceHeight == 0f)
			{
				animator.SetTrigger(neutralAnimID);
				hasTriggered = false;
			}
		}
		if ((vector3 - trailingPosition).sqrMagnitude > 0.1f)
		{
			float t = 1f - Mathf.Exp((0f - faceMovementDirectionStrength) * Time.deltaTime);
			trailingPosition = Vector3.Lerp(trailingPosition, vector3, t);
		}
		Quaternion rotation = Quaternion.Slerp(Quaternion.LookRotation(vector3 - trailingPosition, Vector3.up), Quaternion.LookRotation(-vector4, Vector3.up), rotateTowardsPlayerFromLookTime.Evaluate(lookAtTime));
		base.transform.SetPositionAndRotation(vector3, rotation);
	}
}
