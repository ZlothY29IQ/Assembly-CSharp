using System.Collections;
using System.Collections.Generic;
using CjLib;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderPieceBallista : MonoBehaviour, IBuilderPieceComponent, IBuilderPieceFunctional
{
	private enum BallistaState
	{
		Idle,
		Loading,
		WaitingForTrigger,
		PlayerInTrigger,
		PrepareForLaunch,
		PrepareForLaunchLocal,
		Launching,
		LaunchingLocal,
		Count
	}

	[SerializeField]
	private BuilderPiece myPiece;

	[SerializeField]
	private List<Collider> triggers;

	[SerializeField]
	private List<Collider> disableWhileLaunching;

	[Tooltip("Trigger to start the launch if not autoLaunch")]
	[SerializeField]
	private BuilderSmallHandTrigger handTrigger;

	[Tooltip("Should the player launch without a hand trigger press")]
	[SerializeField]
	private bool autoLaunch;

	[SerializeField]
	private float autoLaunchDelay = 0.75f;

	private double enteredTriggerTime;

	public Animator animator;

	public Transform launchStart;

	public Transform launchEnd;

	public Transform launchBone;

	[SerializeField]
	private SoundBankPlayer loadSFX;

	[SerializeField]
	private SoundBankPlayer launchSFX;

	[SerializeField]
	private SoundBankPlayer cockSFX;

	[SerializeField]
	private ParticleSystem launchParticles;

	private bool hasLaunchParticles;

	public float reloadDelay = 1f;

	public float loadTime = 1.933f;

	public float slipOverrideDuration = 0.1f;

	private double launchedTime;

	public float playerMagnetismStrength = 3f;

	[Tooltip("Speed will be scaled by piece scale")]
	public float launchSpeed = 20f;

	[Range(0f, 1f)]
	public float pitch;

	private bool debugDrawTrajectoryOnLaunch;

	private int loadTriggerHash = Animator.StringToHash("Load");

	private int fireTriggerHash = Animator.StringToHash("Fire");

	private int pitchParamHash = Animator.StringToHash("Pitch");

	private int idleStateHash = Animator.StringToHash("Idle");

	private int loadStateHash = Animator.StringToHash("Load");

	private int fireStateHash = Animator.StringToHash("Fire");

	private bool playerInTrigger;

	private VRRig playerRigInTrigger;

	private bool playerLaunched;

	private float playerReadyToFireDist = 1.6667f;

	private float prepareForLaunchDistance = 2.5f;

	private Vector3 launchDirection;

	private float launchRampDistance;

	private float playerPullInRate;

	private float appliedAnimatorPitch;

	private bool launchBigMonkes;

	private Vector3 playerBodyOffsetFromHead = new Vector3(0f, -0.4f, -0.15f);

	private double loadCompleteTime;

	private BallistaState ballistaState;

	private const int predictionLineSamples = 240;

	private Vector3[] predictionLinePoints = new Vector3[240];

	private void Awake()
	{
		animator.SetFloat(pitchParamHash, pitch);
		appliedAnimatorPitch = pitch;
		launchDirection = launchEnd.position - launchStart.position;
		launchRampDistance = launchDirection.magnitude;
		launchDirection /= launchRampDistance;
		playerPullInRate = Mathf.Exp(playerMagnetismStrength);
		if (handTrigger != null)
		{
			handTrigger.TriggeredEvent.AddListener(OnHandTriggerPressed);
		}
		hasLaunchParticles = launchParticles != null;
	}

	private void OnDestroy()
	{
		if (handTrigger != null)
		{
			handTrigger.TriggeredEvent.RemoveListener(OnHandTriggerPressed);
		}
	}

	private void OnHandTriggerPressed()
	{
		if (!autoLaunch && ballistaState == BallistaState.PlayerInTrigger)
		{
			myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 4);
		}
	}

	private void UpdateStateMaster()
	{
		if (!NetworkSystem.Instance.InRoom || !NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		switch (ballistaState)
		{
		case BallistaState.Idle:
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			break;
		case BallistaState.Loading:
			if (currentAnimatorStateInfo.shortNameHash == loadStateHash && (double)Time.time > loadCompleteTime)
			{
				if (playerInTrigger && playerRigInTrigger != null && (launchBigMonkes || (double)playerRigInTrigger.scaleFactor < 0.99))
				{
					myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 3, playerRigInTrigger.Creator.GetPlayerRef(), NetworkSystem.Instance.ServerTimestamp);
					break;
				}
				playerInTrigger = false;
				playerRigInTrigger = null;
				ballistaState = BallistaState.WaitingForTrigger;
			}
			break;
		case BallistaState.WaitingForTrigger:
			if (!playerInTrigger || playerRigInTrigger == null || (!launchBigMonkes && playerRigInTrigger.scaleFactor >= 0.99f))
			{
				playerInTrigger = false;
				playerRigInTrigger = null;
			}
			else if (playerInTrigger)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 3, playerRigInTrigger.Creator.GetPlayerRef(), NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		case BallistaState.PlayerInTrigger:
			if (!playerInTrigger || playerRigInTrigger == null || (!launchBigMonkes && playerRigInTrigger.scaleFactor >= 0.99f))
			{
				playerInTrigger = false;
				playerRigInTrigger = null;
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 2, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			else if (autoLaunch && (double)Time.time > enteredTriggerTime + (double)autoLaunchDelay)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 4, playerRigInTrigger.Creator.GetPlayerRef(), NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		case BallistaState.PrepareForLaunch:
		case BallistaState.PrepareForLaunchLocal:
		{
			if (!playerInTrigger || playerRigInTrigger == null || (!launchBigMonkes && playerRigInTrigger.scaleFactor >= 0.99f))
			{
				playerInTrigger = false;
				playerRigInTrigger = null;
				ResetFlags();
				myPiece.functionalPieceState = 0;
				ballistaState = BallistaState.Idle;
				break;
			}
			Vector3 playerBodyCenterPosition = GetPlayerBodyCenterPosition(playerRigInTrigger.transform, playerRigInTrigger.scaleFactor);
			Vector3 vector = Vector3.Dot(playerBodyCenterPosition - launchStart.position, launchDirection) * launchDirection + launchStart.position;
			Vector3 b = playerBodyCenterPosition - vector;
			if (Vector3.Lerp(Vector3.zero, b, Mathf.Exp((0f - playerPullInRate) * Time.deltaTime)).sqrMagnitude < playerReadyToFireDist * myPiece.GetScale() * playerReadyToFireDist * myPiece.GetScale())
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 6, playerRigInTrigger.Creator.GetPlayerRef(), NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		}
		case BallistaState.Launching:
		case BallistaState.LaunchingLocal:
			if (currentAnimatorStateInfo.shortNameHash == idleStateHash)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		}
	}

	private void ResetFlags()
	{
		playerLaunched = false;
		loadCompleteTime = double.MaxValue;
	}

	private void UpdatePlayerPosition()
	{
		if (ballistaState != BallistaState.PrepareForLaunchLocal && ballistaState != BallistaState.LaunchingLocal)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		GTPlayer instance = GTPlayer.Instance;
		Vector3 playerBodyCenterPosition = GetPlayerBodyCenterPosition(instance.headCollider.transform, instance.scale);
		Vector3 lhs = playerBodyCenterPosition - launchStart.position;
		switch (ballistaState)
		{
		case BallistaState.PrepareForLaunchLocal:
		{
			Vector3 vector2 = Vector3.Dot(lhs, launchDirection) * launchDirection + launchStart.position;
			Vector3 vector3 = playerBodyCenterPosition - vector2;
			Vector3 vector4 = Vector3.Lerp(Vector3.zero, vector3, Mathf.Exp((0f - playerPullInRate) * deltaTime));
			instance.transform.position = instance.transform.position + (vector4 - vector3);
			instance.SetPlayerVelocity(Vector3.zero);
			instance.SetMaximumSlipThisFrame();
			break;
		}
		case BallistaState.LaunchingLocal:
			if (!playerLaunched)
			{
				float num = Vector3.Dot(launchBone.position - launchStart.position, launchDirection) / launchRampDistance;
				float b = Vector3.Dot(lhs, launchDirection) / launchRampDistance;
				float num2 = 0.25f * myPiece.GetScale() / launchRampDistance;
				float num3 = Mathf.Max(num + num2, b);
				float num4 = num3 * launchRampDistance;
				Vector3 vector = launchDirection * num4 + launchStart.position;
				_ = instance.transform.position + (vector - playerBodyCenterPosition);
				instance.transform.position = instance.transform.position + (vector - playerBodyCenterPosition);
				instance.SetPlayerVelocity(Vector3.zero);
				instance.SetMaximumSlipThisFrame();
				if (num3 >= 1f)
				{
					playerLaunched = true;
					launchedTime = Time.time;
					instance.SetPlayerVelocity(launchSpeed * myPiece.GetScale() * launchDirection);
					instance.SetMaximumSlipThisFrame();
				}
			}
			else if ((double)Time.time < launchedTime + (double)slipOverrideDuration)
			{
				instance.SetMaximumSlipThisFrame();
			}
			break;
		}
	}

	private Vector3 GetPlayerBodyCenterPosition(Transform headTransform, float playerScale)
	{
		return headTransform.position + Quaternion.Euler(0f, headTransform.rotation.eulerAngles.y, 0f) * new Vector3(0f, 0f, playerBodyOffsetFromHead.z * playerScale) + Vector3.down * (playerBodyOffsetFromHead.y * playerScale);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (playerRigInTrigger != null || other.GetComponent<CapsuleCollider>() == null || other.attachedRigidbody == null)
		{
			return;
		}
		VRRig vRRig = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (vRRig == null)
		{
			if (!(GTPlayer.Instance.bodyCollider == other))
			{
				return;
			}
			vRRig = GorillaTagger.Instance.offlineVRRig;
		}
		if (launchBigMonkes || !((double)vRRig.scaleFactor > 0.99))
		{
			playerRigInTrigger = vRRig;
			playerInTrigger = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (playerRigInTrigger == null || !playerInTrigger || other.GetComponent<CapsuleCollider>() == null || other.attachedRigidbody == null)
		{
			return;
		}
		VRRig vRRig = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (vRRig == null)
		{
			if (!(GTPlayer.Instance.bodyCollider == other))
			{
				return;
			}
			vRRig = GorillaTagger.Instance.offlineVRRig;
		}
		if (playerRigInTrigger.Equals(vRRig))
		{
			playerInTrigger = false;
			playerRigInTrigger = null;
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		if (!myPiece.GetTable().isTableMutable)
		{
			launchBigMonkes = true;
		}
		ballistaState = BallistaState.Idle;
		playerInTrigger = false;
		playerRigInTrigger = null;
		playerLaunched = false;
	}

	public void OnPieceDestroy()
	{
		myPiece.functionalPieceState = 0;
		ballistaState = BallistaState.Idle;
	}

	public void OnPiecePlacementDeserialized()
	{
		launchDirection = launchEnd.position - launchStart.position;
		launchRampDistance = launchDirection.magnitude;
		launchDirection /= launchRampDistance;
	}

	public void OnPieceActivate()
	{
		foreach (Collider trigger in triggers)
		{
			trigger.enabled = true;
		}
		animator.SetFloat(pitchParamHash, pitch);
		appliedAnimatorPitch = pitch;
		launchDirection = launchEnd.position - launchStart.position;
		launchRampDistance = launchDirection.magnitude;
		launchDirection /= launchRampDistance;
		myPiece.GetTable().RegisterFunctionalPiece(this);
	}

	public void OnPieceDeactivate()
	{
		foreach (Collider trigger in triggers)
		{
			trigger.enabled = false;
		}
		if (hasLaunchParticles)
		{
			launchParticles.Stop();
			launchParticles.Clear();
		}
		myPiece.functionalPieceState = 0;
		ballistaState = BallistaState.Idle;
		playerInTrigger = false;
		playerRigInTrigger = null;
		ResetFlags();
		myPiece.GetTable().UnregisterFunctionalPiece(this);
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (!NetworkSystem.Instance.IsMasterClient || !IsStateValid(newState) || instigator == null || (BallistaState)newState == ballistaState)
		{
			return;
		}
		if (newState == 4)
		{
			if (ballistaState == BallistaState.PlayerInTrigger && playerInTrigger && playerRigInTrigger != null)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 4, playerRigInTrigger.Creator.GetPlayerRef(), timeStamp);
			}
		}
		else
		{
			Debug.LogWarning("BuilderPiece Ballista unexpected state request for " + newState);
		}
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (!IsStateValid(newState))
		{
			return;
		}
		BallistaState ballistaState = (BallistaState)newState;
		if (ballistaState == this.ballistaState)
		{
			return;
		}
		switch ((BallistaState)newState)
		{
		case BallistaState.Idle:
			ResetFlags();
			break;
		case BallistaState.Loading:
			ResetFlags();
			foreach (Collider item in disableWhileLaunching)
			{
				item.enabled = true;
			}
			if (this.ballistaState == BallistaState.Launching || this.ballistaState == BallistaState.LaunchingLocal)
			{
				loadCompleteTime = Time.time + reloadDelay;
				if (loadSFX != null)
				{
					loadSFX.Play();
				}
			}
			else
			{
				loadCompleteTime = Time.time + loadTime;
			}
			animator.SetTrigger(loadTriggerHash);
			break;
		case BallistaState.PlayerInTrigger:
			enteredTriggerTime = Time.time;
			if (autoLaunch && cockSFX != null)
			{
				cockSFX.Play();
			}
			break;
		case BallistaState.PrepareForLaunch:
		{
			playerLaunched = false;
			if (!autoLaunch && cockSFX != null)
			{
				cockSFX.Play();
			}
			if (!instigator.IsLocal)
			{
				break;
			}
			GTPlayer instance = GTPlayer.Instance;
			if (!(Vector3.Distance(GetPlayerBodyCenterPosition(instance.headCollider.transform, instance.scale), launchStart.position) <= prepareForLaunchDistance * myPiece.GetScale()) || (!launchBigMonkes && !((double)GorillaTagger.Instance.offlineVRRig.scaleFactor < 0.99)))
			{
				break;
			}
			ballistaState = BallistaState.PrepareForLaunchLocal;
			foreach (Collider item2 in disableWhileLaunching)
			{
				item2.enabled = false;
			}
			break;
		}
		case BallistaState.Launching:
			playerLaunched = false;
			animator.SetTrigger(fireTriggerHash);
			if (launchSFX != null)
			{
				launchSFX.Play();
			}
			if (hasLaunchParticles)
			{
				launchParticles.Play();
			}
			if (debugDrawTrajectoryOnLaunch)
			{
				StartCoroutine(DebugDrawTrajectory(8f));
			}
			if (instigator.IsLocal && this.ballistaState == BallistaState.PrepareForLaunchLocal)
			{
				ballistaState = BallistaState.LaunchingLocal;
				GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength * 2f, GorillaTagger.Instance.tapHapticDuration * 4f);
				GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength * 2f, GorillaTagger.Instance.tapHapticDuration * 4f);
			}
			break;
		}
		this.ballistaState = ballistaState;
	}

	public bool IsStateValid(byte state)
	{
		return state < 8;
	}

	public void FunctionalPieceUpdate()
	{
		if (!(myPiece == null) && myPiece.state == BuilderPiece.State.AttachedAndPlaced)
		{
			if (NetworkSystem.Instance.IsMasterClient)
			{
				UpdateStateMaster();
			}
			UpdatePlayerPosition();
		}
	}

	private void UpdatePredictionLine()
	{
		float num = 1f / 30f;
		Vector3 position = launchEnd.position;
		Vector3 vector = (launchEnd.position - launchStart.position).normalized * launchSpeed;
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
}
