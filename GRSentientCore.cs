using System;
using CjLib;
using GorillaLocomotion;
using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;

public class GRSentientCore : MonoBehaviour, IGRSleepableEntity
{
	public enum SentientCoreState
	{
		Asleep,
		Awake,
		JumpInitiated,
		JumpAnticipation,
		Jumping,
		Held,
		HeldAlert,
		AttachedToPlayer,
		Dropped
	}

	public GameEntity gameEntity;

	public Vector2 jumpAngleMinMax = new Vector2(30f, 60f);

	public float jumpSpeed = 3f;

	public float jumpGravityAccel = 10f;

	public float maxSpeed = 5f;

	public float radius = 0.14f;

	public float jumpAnticipationTime = 1f;

	public float jumpCooldownTime = 2f;

	public bool useSurfaceNormalForGravityDirection = true;

	public Vector2 timeRangeBetweenAlerts = new Vector2(7f, 12f);

	public float timeUntilFirstAlert = 0.5f;

	public float alertNoiseEventMagnitude = 1f;

	public AbilitySound jumpSound;

	public AbilitySound landSound;

	public AbilitySound alertEnemiesSound;

	public float wakeupRadius = 3f;

	public bool debugDraw;

	public Transform visualCore;

	public ParticleSystem trailFX;

	private Vector3 surfaceNormal = Vector3.up;

	private Vector3 jumpDirection = Vector3.up;

	private Vector3 jumpStartPosition;

	private Vector3 jumpVelocity;

	private float jumpStartTime;

	private Rigidbody rb;

	private float timeUntilNextAlert = 7f;

	private float enemyAlertDuration = 1f;

	private bool isPlayingAlert;

	private bool sleepRequested;

	[ReadOnly]
	public SentientCoreState localState = SentientCoreState.Awake;

	private float localStateStartTime;

	public Vector3 Position => base.transform.position;

	public float WakeUpRadius => wakeupRadius;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		GhostReactor.instance.sleepableEntities.Add(this);
		gameEntity.OnStateChanged += OnStateChanged;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnReleased = (Action)Delegate.Combine(obj2.OnReleased, new Action(OnReleased));
		GameEntity obj3 = gameEntity;
		obj3.OnSnapped = (Action)Delegate.Combine(obj3.OnSnapped, new Action(OnSnapped));
		GameEntity obj4 = gameEntity;
		obj4.OnDetached = (Action)Delegate.Combine(obj4.OnDetached, new Action(OnDetached));
		Sleep();
	}

	private void OnDestroy()
	{
		if (GhostReactor.instance != null)
		{
			GhostReactor.instance.sleepableEntities.Remove(this);
		}
		if (gameEntity != null)
		{
			gameEntity.OnStateChanged -= OnStateChanged;
			GameEntity obj = gameEntity;
			obj.OnGrabbed = (Action)Delegate.Remove(obj.OnGrabbed, new Action(OnGrabbed));
			GameEntity obj2 = gameEntity;
			obj2.OnReleased = (Action)Delegate.Remove(obj2.OnReleased, new Action(OnReleased));
			GameEntity obj3 = gameEntity;
			obj3.OnSnapped = (Action)Delegate.Remove(obj3.OnSnapped, new Action(OnSnapped));
			GameEntity obj4 = gameEntity;
			obj4.OnDetached = (Action)Delegate.Remove(obj4.OnDetached, new Action(OnDetached));
		}
	}

	public bool IsSleeping()
	{
		return gameEntity.GetState() == 0;
	}

	public void WakeUp()
	{
		if (gameEntity.IsAuthority() && IsSleeping())
		{
			gameEntity.RequestState(gameEntity.id, 1L);
		}
		if (localState == SentientCoreState.Asleep)
		{
			localState = SentientCoreState.Awake;
			localStateStartTime = Time.time;
		}
		sleepRequested = false;
		base.enabled = true;
	}

	public void Sleep()
	{
		sleepRequested = true;
	}

	private void OnStateChanged(long prevState, long nextState)
	{
		if ((int)nextState == 0)
		{
			sleepRequested = false;
		}
		else if (!base.enabled)
		{
			WakeUp();
		}
		SetState((SentientCoreState)nextState);
	}

	private void OnGrabbed()
	{
		WakeUp();
		SetState(SentientCoreState.Held);
		timeUntilNextAlert = Mathf.Min(timeUntilFirstAlert, timeUntilNextAlert);
	}

	private void OnReleased()
	{
		SetState(SentientCoreState.Dropped);
	}

	private void OnSnapped()
	{
		SetState(SentientCoreState.AttachedToPlayer);
	}

	private void OnDetached()
	{
		SetState(SentientCoreState.Dropped);
	}

	private void Update()
	{
		if (debugDraw)
		{
			DebugUtil.DrawSphere(base.transform.position, 0.15f, 12, 12, Color.cyan);
		}
		if (gameEntity.IsAuthority())
		{
			AuthorityUpdate();
		}
		SharedUpdate();
	}

	private void AuthorityUpdate()
	{
		if (trailFX != null)
		{
			if (gameEntity.snappedByActorNumber != -1 || gameEntity.heldByActorNumber != -1)
			{
				if (trailFX.isPlaying)
				{
					trailFX.Stop();
				}
			}
			else if (!trailFX.isPlaying)
			{
				trailFX.Play();
			}
		}
		switch (localState)
		{
		case SentientCoreState.Awake:
			if (sleepRequested)
			{
				sleepRequested = false;
				SetState(SentientCoreState.Asleep);
			}
			if (gameEntity.heldByActorNumber != -1)
			{
				SetState(SentientCoreState.Held);
			}
			else if (!sleepRequested && Time.time > localStateStartTime + jumpCooldownTime)
			{
				AuthorityInitiateJump();
			}
			break;
		case SentientCoreState.JumpInitiated:
			if (sleepRequested)
			{
				sleepRequested = false;
				SetState(SentientCoreState.Asleep);
			}
			break;
		case SentientCoreState.Held:
			timeUntilNextAlert -= Time.deltaTime;
			if (timeUntilNextAlert < 0f)
			{
				timeUntilNextAlert = UnityEngine.Random.Range(timeRangeBetweenAlerts.x, timeRangeBetweenAlerts.y);
				SetState(SentientCoreState.HeldAlert);
			}
			break;
		case SentientCoreState.AttachedToPlayer:
			timeUntilNextAlert -= Time.deltaTime;
			if (timeUntilNextAlert < 0f)
			{
				timeUntilNextAlert = UnityEngine.Random.Range(timeRangeBetweenAlerts.x, timeRangeBetweenAlerts.y);
				alertEnemiesSound.Play(null);
				GRNoiseEventManager.instance.AddNoiseEvent(base.transform.position, alertNoiseEventMagnitude, enemyAlertDuration);
			}
			break;
		case SentientCoreState.Asleep:
		case SentientCoreState.JumpAnticipation:
		case SentientCoreState.Jumping:
		case SentientCoreState.HeldAlert:
		case SentientCoreState.Dropped:
			break;
		}
	}

	private void SharedUpdate()
	{
		switch (localState)
		{
		default:
			base.enabled = false;
			break;
		case SentientCoreState.Awake:
			if (visualCore != null && visualCore.transform.localScale != Vector3.one)
			{
				visualCore.transform.localScale = Vector3.one;
				visualCore.transform.localPosition = Vector3.zero;
				visualCore.transform.localRotation = Quaternion.identity;
			}
			break;
		case SentientCoreState.JumpAnticipation:
			if (debugDraw)
			{
				DrawJumpPath(Color.yellow);
			}
			if (Time.time > jumpStartTime)
			{
				SetState(SentientCoreState.Jumping);
				jumpSound.Play(null);
				if (visualCore != null)
				{
					visualCore.transform.localScale = Vector3.one;
					visualCore.transform.localPosition = Vector3.zero;
					visualCore.transform.localRotation = Quaternion.identity;
				}
			}
			else
			{
				Vector3 normalized = (surfaceNormal + jumpDirection).normalized;
				float num = (jumpStartTime - Time.time) / jumpAnticipationTime * 0.25f + 0.75f;
				float num2 = Mathf.Sqrt(1f / num);
				visualCore.transform.localScale = new Vector3(num2, num, num2);
				visualCore.transform.position = visualCore.parent.position - normalized * (1f - num) * radius;
				visualCore.transform.rotation = Quaternion.FromToRotation(Vector3.up, normalized);
			}
			break;
		case SentientCoreState.Jumping:
		{
			if (debugDraw)
			{
				DrawJumpPath(Color.yellow);
			}
			float deltaTime2 = Time.deltaTime;
			Vector3 vector3 = base.transform.position + jumpVelocity * deltaTime2;
			Vector3 vector4 = (useSurfaceNormalForGravityDirection ? (-surfaceNormal) : Vector3.down);
			jumpVelocity += vector4 * (jumpGravityAccel * deltaTime2);
			float magnitude2 = jumpVelocity.magnitude;
			if (magnitude2 > maxSpeed && maxSpeed > 0f)
			{
				jumpVelocity *= maxSpeed / magnitude2;
			}
			float magnitude3 = (vector3 - base.transform.position).magnitude;
			Vector3 vector5 = ((magnitude3 > 0.001f) ? ((vector3 - base.transform.position) / magnitude3) : Vector3.zero);
			if (Physics.SphereCast(new Ray(base.transform.position, vector5), radius, out var hitInfo2, magnitude3, GTPlayer.Instance.locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
			{
				vector3 = base.transform.position + vector5 * hitInfo2.distance;
				surfaceNormal = hitInfo2.normal;
				SetState(SentientCoreState.Awake);
				landSound.Play(null);
			}
			base.transform.position = vector3;
			break;
		}
		case SentientCoreState.Held:
		{
			GRPlayer gRPlayer = GRPlayer.Get(gameEntity.heldByActorNumber);
			if (gRPlayer != null)
			{
				gRPlayer.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.TimeChaosExposure, Time.deltaTime);
			}
			isPlayingAlert = false;
			break;
		}
		case SentientCoreState.HeldAlert:
			if (!isPlayingAlert)
			{
				isPlayingAlert = true;
				alertEnemiesSound.Play(null);
				GRNoiseEventManager.instance.AddNoiseEvent(base.transform.position, alertNoiseEventMagnitude, enemyAlertDuration);
			}
			if (Time.time - localStateStartTime > enemyAlertDuration)
			{
				SetState(SentientCoreState.Held);
			}
			break;
		case SentientCoreState.AttachedToPlayer:
		{
			GRPlayer gRPlayer2 = GRPlayer.Get(gameEntity.snappedByActorNumber);
			if (gRPlayer2 != null)
			{
				gRPlayer2.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.TimeChaosExposure, Time.deltaTime);
			}
			break;
		}
		case SentientCoreState.Dropped:
		{
			float deltaTime = Time.deltaTime;
			Vector3 vector = base.transform.position + rb.linearVelocity * deltaTime;
			float magnitude = (vector - base.transform.position).magnitude;
			Vector3 vector2 = ((magnitude > 0.001f) ? ((vector - base.transform.position) / magnitude) : Vector3.zero);
			if (Physics.SphereCast(new Ray(base.transform.position, vector2), radius, out var hitInfo, magnitude, GTPlayer.Instance.locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
			{
				vector = base.transform.position + vector2 * hitInfo.distance;
				surfaceNormal = hitInfo.normal;
				base.transform.position = vector;
				rb.isKinematic = true;
				SetState(SentientCoreState.Awake);
			}
			break;
		}
		}
	}

	private void SetState(SentientCoreState nextState)
	{
		if (localState != nextState)
		{
			localState = nextState;
			localStateStartTime = Time.time;
			if (gameEntity.IsAuthority())
			{
				gameEntity.RequestState(gameEntity.id, (long)nextState);
			}
		}
	}

	public void PerformJump(Vector3 startPos, Vector3 normal, Vector3 direction, double jumpNetworkTime)
	{
		if (PhotonNetwork.InRoom)
		{
			if (!base.enabled || IsSleeping())
			{
				WakeUp();
			}
			base.transform.position = startPos;
			float num = Mathf.Clamp((float)(jumpNetworkTime - PhotonNetwork.Time), 0f, jumpAnticipationTime);
			jumpStartTime = Time.time + num;
			jumpDirection = direction;
			jumpDirection.Normalize();
			jumpStartPosition = startPos;
			surfaceNormal = normal;
			jumpVelocity = jumpDirection * jumpSpeed;
			SetState(SentientCoreState.JumpAnticipation);
		}
	}

	private void DrawJumpPath(Color pathColor)
	{
		DebugUtil.DrawLine(jumpStartPosition, jumpStartPosition + surfaceNormal * 0.15f, Color.cyan);
		float num = 0.016666f;
		int num2 = 100;
		Vector3 vector = jumpStartPosition;
		Vector3 vector2 = jumpDirection * jumpSpeed;
		for (int i = 0; i < num2; i++)
		{
			Vector3 vector3 = vector + vector2 * num;
			vector2 += -surfaceNormal * (jumpGravityAccel * num);
			float magnitude = (vector3 - vector).magnitude;
			Vector3 direction = ((magnitude > 0.001f) ? ((vector3 - vector) / magnitude) : Vector3.zero);
			if (Physics.SphereCast(new Ray(vector, direction), radius, out var hitInfo, magnitude, GTPlayer.Instance.locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
			{
				vector3 = hitInfo.point;
				DebugUtil.DrawLine(vector, vector3, pathColor);
				DebugUtil.DrawLine(vector3, vector3 + hitInfo.normal * 0.15f, Color.cyan);
				DebugUtil.DrawSphere(hitInfo.point, 0.1f, 12, 12, pathColor);
				break;
			}
			DebugUtil.DrawLine(vector, vector3, pathColor);
			vector = vector3;
		}
	}

	public void AuthorityInitiateJump()
	{
		if (gameEntity.IsAuthority())
		{
			Vector3 vector = UnityEngine.Random.insideUnitSphere;
			if (Vector3.Dot(vector, surfaceNormal) > 0.99f)
			{
				vector = new Vector3(surfaceNormal.y, surfaceNormal.z, surfaceNormal.x);
			}
			float num = UnityEngine.Random.Range(jumpAngleMinMax.x, jumpAngleMinMax.y);
			Vector3 direction = Quaternion.AngleAxis(90f - num, Vector3.Cross(surfaceNormal, vector)) * surfaceNormal;
			direction.Normalize();
			SetState(SentientCoreState.JumpInitiated);
			gameEntity.manager.ghostReactorManager.RequestSentientCorePerformJump(gameEntity, base.transform.position, surfaceNormal, direction, jumpAnticipationTime);
		}
	}
}
