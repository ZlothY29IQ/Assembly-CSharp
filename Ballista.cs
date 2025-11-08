using System.Collections;
using CjLib;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public class Ballista : MonoBehaviourPun
{
	public Animator animator;

	public Transform launchStart;

	public Transform launchEnd;

	public Transform launchBone;

	public float reloadDelay = 1f;

	public float loadTime = 1.933f;

	public float playerMagnetismStrength = 3f;

	public float launchSpeed = 20f;

	[Range(0f, 1f)]
	public float pitch;

	private bool useSpeedOptions;

	public float[] speedOptions = new float[4] { 10f, 15f, 20f, 25f };

	public int currentSpeedIndex;

	public GorillaPressableButton speedZeroButton;

	public GorillaPressableButton speedOneButton;

	public GorillaPressableButton speedTwoButton;

	public GorillaPressableButton speedThreeButton;

	private bool debugDrawTrajectoryOnLaunch;

	private int loadTriggerHash = Animator.StringToHash("Load");

	private int fireTriggerHash = Animator.StringToHash("Fire");

	private int pitchParamHash = Animator.StringToHash("Pitch");

	private int idleStateHash = Animator.StringToHash("Idle");

	private int loadStateHash = Animator.StringToHash("Load");

	private int fireStateHash = Animator.StringToHash("Fire");

	private int prevStateHash = Animator.StringToHash("Idle");

	private float fireCompleteTime;

	private float loadStartTime;

	private bool playerInTrigger;

	private bool playerReadyToFire;

	private bool playerLaunched;

	private float playerReadyToFireDist = 0.1f;

	private Vector3 playerBodyOffsetFromHead = new Vector3(0f, -0.4f, -0.15f);

	private Vector3 launchDirection;

	private float launchRampDistance;

	private int collidingLayer;

	private int notCollidingLayer;

	private float playerPullInRate;

	private float appliedAnimatorPitch;

	private const int predictionLineSamples = 240;

	private Vector3[] predictionLinePoints = new Vector3[240];

	private float LaunchSpeed
	{
		get
		{
			if (!useSpeedOptions)
			{
				return launchSpeed;
			}
			return speedOptions[currentSpeedIndex];
		}
	}

	public void TriggerLoad()
	{
		animator.SetTrigger(loadTriggerHash);
	}

	public void TriggerFire()
	{
		animator.SetTrigger(fireTriggerHash);
	}

	private void Awake()
	{
		launchDirection = launchEnd.position - launchStart.position;
		launchRampDistance = launchDirection.magnitude;
		launchDirection /= launchRampDistance;
		collidingLayer = LayerMask.NameToLayer("Default");
		notCollidingLayer = LayerMask.NameToLayer("Prop");
		playerPullInRate = Mathf.Exp(playerMagnetismStrength);
		animator.SetFloat(pitchParamHash, pitch);
		appliedAnimatorPitch = pitch;
		RefreshButtonColors();
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.shortNameHash == idleStateHash)
		{
			if (prevStateHash == fireStateHash)
			{
				fireCompleteTime = Time.time;
			}
			if (Time.time - fireCompleteTime > reloadDelay)
			{
				animator.SetTrigger(loadTriggerHash);
				loadStartTime = Time.time;
			}
		}
		else if (currentAnimatorStateInfo.shortNameHash == loadStateHash)
		{
			if (Time.time - loadStartTime > loadTime)
			{
				if (playerInTrigger)
				{
					GTPlayer instance = GTPlayer.Instance;
					Vector3 playerBodyCenterPosition = GetPlayerBodyCenterPosition(instance);
					Vector3 vector = Vector3.Dot(playerBodyCenterPosition - launchStart.position, launchDirection) * launchDirection + launchStart.position;
					Vector3 vector2 = playerBodyCenterPosition - vector;
					Vector3 vector3 = Vector3.Lerp(Vector3.zero, vector2, Mathf.Exp((0f - playerPullInRate) * deltaTime));
					instance.transform.position = instance.transform.position + (vector3 - vector2);
					playerReadyToFire = vector3.sqrMagnitude < playerReadyToFireDist * playerReadyToFireDist;
				}
				else
				{
					playerReadyToFire = false;
				}
				if (playerReadyToFire)
				{
					if (PhotonNetwork.InRoom)
					{
						base.photonView.RPC("FireBallistaRPC", RpcTarget.Others);
					}
					FireLocal();
				}
			}
		}
		else if (currentAnimatorStateInfo.shortNameHash == fireStateHash && !playerLaunched && (playerReadyToFire || playerInTrigger))
		{
			float num = Vector3.Dot(launchBone.position - launchStart.position, launchDirection) / launchRampDistance;
			GTPlayer instance2 = GTPlayer.Instance;
			Vector3 playerBodyCenterPosition2 = GetPlayerBodyCenterPosition(instance2);
			float b = Vector3.Dot(playerBodyCenterPosition2 - launchStart.position, launchDirection) / launchRampDistance;
			float num2 = 0.25f / launchRampDistance;
			float num3 = Mathf.Max(num + num2, b);
			float num4 = num3 * launchRampDistance;
			Vector3 vector4 = launchDirection * num4 + launchStart.position;
			_ = instance2.transform.position + (vector4 - playerBodyCenterPosition2);
			instance2.transform.position = instance2.transform.position + (vector4 - playerBodyCenterPosition2);
			instance2.SetPlayerVelocity(Vector3.zero);
			if (num3 >= 1f)
			{
				playerLaunched = true;
				instance2.SetPlayerVelocity(LaunchSpeed * launchDirection);
				instance2.SetMaximumSlipThisFrame();
			}
		}
		prevStateHash = currentAnimatorStateInfo.shortNameHash;
	}

	private void FireLocal()
	{
		animator.SetTrigger(fireTriggerHash);
		playerLaunched = false;
		if (debugDrawTrajectoryOnLaunch)
		{
			DebugDrawTrajectory(8f);
		}
	}

	private Vector3 GetPlayerBodyCenterPosition(GTPlayer player)
	{
		return player.headCollider.transform.position + Quaternion.Euler(0f, player.headCollider.transform.rotation.eulerAngles.y, 0f) * new Vector3(0f, 0f, -0.15f) + Vector3.down * 0.4f;
	}

	private void OnTriggerEnter(Collider other)
	{
		GTPlayer instance = GTPlayer.Instance;
		if (instance != null && instance.bodyCollider == other)
		{
			playerInTrigger = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		GTPlayer instance = GTPlayer.Instance;
		if (instance != null && instance.bodyCollider == other)
		{
			playerInTrigger = false;
		}
	}

	[PunRPC]
	public void FireBallistaRPC(PhotonMessageInfo info)
	{
		FireLocal();
	}

	private void UpdatePredictionLine()
	{
		float num = 1f / 30f;
		Vector3 position = launchEnd.position;
		Vector3 vector = (launchEnd.position - launchStart.position).normalized * LaunchSpeed;
		for (int i = 0; i < 240; i++)
		{
			predictionLinePoints[i] = position;
			position += vector * num;
			vector += Vector3.down * 9.8f * num;
		}
	}

	private IEnumerator DebugDrawTrajectory(float duration)
	{
		UpdatePredictionLine();
		float startTime = Time.time;
		while (Time.time < startTime + duration)
		{
			DebugUtil.DrawLine(launchStart.position, launchEnd.position, Color.yellow);
			DebugUtil.DrawLines(predictionLinePoints, Color.yellow);
			yield return null;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (launchStart != null && launchEnd != null)
		{
			UpdatePredictionLine();
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(launchStart.position, launchEnd.position);
			Gizmos.DrawLineList(predictionLinePoints);
		}
	}

	public void RefreshButtonColors()
	{
		speedZeroButton.isOn = currentSpeedIndex == 0;
		speedZeroButton.UpdateColor();
		speedOneButton.isOn = currentSpeedIndex == 1;
		speedOneButton.UpdateColor();
		speedTwoButton.isOn = currentSpeedIndex == 2;
		speedTwoButton.UpdateColor();
		speedThreeButton.isOn = currentSpeedIndex == 3;
		speedThreeButton.UpdateColor();
	}

	public void SetSpeedIndex(int index)
	{
		currentSpeedIndex = index;
		RefreshButtonColors();
	}
}
