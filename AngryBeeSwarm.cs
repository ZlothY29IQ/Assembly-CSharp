using System;
using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using GorillaTag.Rendering;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[NetworkBehaviourWeaved(3)]
public class AngryBeeSwarm : NetworkComponent
{
	public enum ChaseState
	{
		Dormant = 1,
		InitialEmerge = 2,
		Chasing = 4,
		Grabbing = 8
	}

	public static AngryBeeSwarm instance;

	public float heightAboveNavmesh = 0.5f;

	public Transform followTarget;

	[SerializeField]
	private float velocityStep = 1f;

	private float currentSpeed;

	[SerializeField]
	private float velocityIncreaseInterval = 20f;

	public Vector3 noisyOffset;

	public Vector3 ghostOffsetGrabbingLocal;

	private float emergeStartedTimestamp;

	private float grabTimestamp;

	private float lastSpeedIncreased;

	[SerializeField]
	private float totalTimeToEmerge;

	[SerializeField]
	private float catchDistance;

	[SerializeField]
	private float grabDuration;

	[SerializeField]
	private float grabSpeed = 1f;

	[SerializeField]
	private float minGrabCooldown;

	[SerializeField]
	private float initialRangeLimit;

	[SerializeField]
	private float finalRangeLimit;

	[SerializeField]
	private float rangeLimitBlendDuration;

	[SerializeField]
	private float boredAfterDuration;

	public NetPlayer targetPlayer;

	public AngryBeeAnimator beeAnimator;

	public ChaseState currentState;

	public ChaseState lastState;

	public NetPlayer grabbedPlayer;

	private bool targetIsOnNavMesh;

	private const float navMeshSampleRange = 5f;

	[Tooltip("Haptic vibration when chased by lucy")]
	public float hapticStrength = 1f;

	public float hapticDuration = 1.5f;

	public float MinHeightAboveWater = 0.5f;

	public float PlayerMinHeightAboveWater = 0.5f;

	public float RefreshClosestPlayerInterval = 1f;

	private float NextRefreshClosestPlayerTimestamp = 1f;

	private float BoredToDeathAtTimestamp = -1f;

	[SerializeField]
	private Transform testEmergeFrom;

	[SerializeField]
	private Transform testEmergeTo;

	private Vector3 emergeFromPosition;

	private Vector3 emergeToPosition;

	private NavMeshPath path;

	public List<Vector3> pathPoints;

	public int currentPathPointIdx;

	private float nextPathTimestamp;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 3)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private BeeSwarmData _Data;

	public bool isDormant => currentState == ChaseState.Dormant;

	[Networked]
	[NetworkedWeaved(0, 3)]
	public unsafe BeeSwarmData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing AngryBeeSwarm.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(BeeSwarmData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing AngryBeeSwarm.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(BeeSwarmData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		instance = this;
		targetPlayer = null;
		currentState = ChaseState.Dormant;
		grabTimestamp = 0f - minGrabCooldown;
		RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
	}

	private void InitializeSwarm()
	{
		if (NetworkSystem.Instance.InRoom && base.IsMine)
		{
			beeAnimator.transform.localPosition = Vector3.zero;
			lastSpeedIncreased = 0f;
			currentSpeed = 0f;
		}
	}

	private void LateUpdate()
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			currentState = ChaseState.Dormant;
			UpdateState();
			return;
		}
		if (base.IsMine)
		{
			switch (currentState)
			{
			case ChaseState.Dormant:
				if (Application.isEditor && Keyboard.current[Key.Space].wasPressedThisFrame)
				{
					currentState = ChaseState.InitialEmerge;
				}
				break;
			case ChaseState.InitialEmerge:
				if (Time.time > emergeStartedTimestamp + totalTimeToEmerge)
				{
					currentState = ChaseState.Chasing;
				}
				break;
			case ChaseState.Chasing:
				if (followTarget == null || targetPlayer == null || Time.time > NextRefreshClosestPlayerTimestamp)
				{
					ChooseClosestTarget();
					if (followTarget != null)
					{
						BoredToDeathAtTimestamp = -1f;
					}
					else if (BoredToDeathAtTimestamp < 0f)
					{
						BoredToDeathAtTimestamp = Time.time + boredAfterDuration;
					}
				}
				if (BoredToDeathAtTimestamp >= 0f && Time.time > BoredToDeathAtTimestamp)
				{
					currentState = ChaseState.Dormant;
				}
				else if (!(followTarget == null) && (followTarget.position - beeAnimator.transform.position).magnitude < catchDistance)
				{
					float num = ZoneShaderSettings.GetWaterY() + PlayerMinHeightAboveWater;
					if (followTarget.position.y > num)
					{
						currentState = ChaseState.Grabbing;
					}
				}
				break;
			case ChaseState.Grabbing:
				if (Time.time > grabTimestamp + grabDuration)
				{
					currentState = ChaseState.Dormant;
				}
				break;
			}
		}
		if (lastState != currentState)
		{
			OnChangeState(currentState);
			lastState = currentState;
		}
		UpdateState();
	}

	public void UpdateState()
	{
		switch (currentState)
		{
		case ChaseState.InitialEmerge:
			if (NetworkSystem.Instance.InRoom)
			{
				SwarmEmergeUpdateShared();
			}
			break;
		case ChaseState.Chasing:
			if (NetworkSystem.Instance.InRoom)
			{
				if (base.IsMine)
				{
					ChaseHost();
				}
				MoveBodyShared();
			}
			break;
		case ChaseState.Grabbing:
			if (NetworkSystem.Instance.InRoom)
			{
				if (targetPlayer == NetworkSystem.Instance.LocalPlayer)
				{
					RiseGrabbedLocalPlayer();
				}
				GrabBodyShared();
			}
			break;
		}
	}

	public void Emerge(Vector3 fromPosition, Vector3 toPosition)
	{
		base.transform.position = fromPosition;
		emergeFromPosition = fromPosition;
		emergeToPosition = toPosition;
		currentState = ChaseState.InitialEmerge;
		emergeStartedTimestamp = Time.time;
	}

	private void OnChangeState(ChaseState newState)
	{
		switch (newState)
		{
		case ChaseState.Dormant:
			if (beeAnimator.gameObject.activeSelf)
			{
				beeAnimator.gameObject.SetActive(value: false);
			}
			if (base.IsMine)
			{
				targetPlayer = null;
				base.transform.position = new Vector3(0f, -9999f, 0f);
				InitializeSwarm();
			}
			SetInitialRotations();
			break;
		case ChaseState.InitialEmerge:
			emergeStartedTimestamp = Time.time;
			if (!beeAnimator.gameObject.activeSelf)
			{
				beeAnimator.gameObject.SetActive(value: true);
			}
			beeAnimator.SetEmergeFraction(0f);
			if (base.IsMine)
			{
				currentSpeed = 0f;
				ChooseClosestTarget();
			}
			SetInitialRotations();
			break;
		case ChaseState.Chasing:
			if (!beeAnimator.gameObject.activeSelf)
			{
				beeAnimator.gameObject.SetActive(value: true);
			}
			beeAnimator.SetEmergeFraction(1f);
			ResetPath();
			NextRefreshClosestPlayerTimestamp = Time.time + RefreshClosestPlayerInterval;
			BoredToDeathAtTimestamp = -1f;
			break;
		case ChaseState.Grabbing:
		{
			if (!beeAnimator.gameObject.activeSelf)
			{
				beeAnimator.gameObject.SetActive(value: true);
			}
			grabTimestamp = Time.time;
			beeAnimator.transform.localPosition = ghostOffsetGrabbingLocal;
			VRRig vRRig = GorillaGameManager.StaticFindRigForPlayer(targetPlayer);
			if (vRRig != null)
			{
				followTarget = vRRig.transform;
			}
			break;
		}
		}
	}

	private void ChooseClosestTarget()
	{
		float num = Mathf.Lerp(initialRangeLimit, finalRangeLimit, (Time.time + totalTimeToEmerge - emergeStartedTimestamp) / rangeLimitBlendDuration);
		float num2 = num * num;
		VRRig vRRig = null;
		float num3 = ZoneShaderSettings.GetWaterY() + PlayerMinHeightAboveWater;
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			if (rig.head != null && !(rig.head.rigTarget == null) && !(rig.head.rigTarget.position.y <= num3))
			{
				float sqrMagnitude = (base.transform.position - rig.head.rigTarget.transform.position).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					vRRig = rig;
				}
			}
		}
		if (vRRig.IsNotNull())
		{
			targetPlayer = vRRig.creator;
			followTarget = vRRig.head.rigTarget;
			targetIsOnNavMesh = NavMesh.SamplePosition(followTarget.position, out var _, 5f, 1);
		}
		else
		{
			targetPlayer = null;
			followTarget = null;
		}
		NextRefreshClosestPlayerTimestamp = Time.time + RefreshClosestPlayerInterval;
	}

	private void SetInitialRotations()
	{
		beeAnimator.transform.localPosition = Vector3.zero;
	}

	private void SwarmEmergeUpdateShared()
	{
		if (Time.time < emergeStartedTimestamp + totalTimeToEmerge)
		{
			float emergeFraction = (Time.time - emergeStartedTimestamp) / totalTimeToEmerge;
			if (base.IsMine)
			{
				base.transform.position = Vector3.Lerp(emergeFromPosition, emergeToPosition, (Time.time - emergeStartedTimestamp) / totalTimeToEmerge);
			}
			beeAnimator.SetEmergeFraction(emergeFraction);
		}
	}

	private void RiseGrabbedLocalPlayer()
	{
		if (Time.time > grabTimestamp + minGrabCooldown)
		{
			grabTimestamp = Time.time;
			GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Frozen, GorillaTagger.Instance.tagCooldown);
			GorillaTagger.Instance.StartVibration(forLeftController: true, hapticStrength, hapticDuration);
			GorillaTagger.Instance.StartVibration(forLeftController: false, hapticStrength, hapticDuration);
		}
		if (Time.time < grabTimestamp + grabDuration)
		{
			GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.up * grabSpeed;
			EquipmentInteractor.instance.ForceStopClimbing();
		}
	}

	public void UpdateFollowPath(Vector3 destination, float currentSpeed)
	{
		if (path == null)
		{
			GetNewPath(destination);
		}
		pathPoints[pathPoints.Count - 1] = destination;
		Vector3 vector = pathPoints[currentPathPointIdx];
		base.transform.position = Vector3.MoveTowards(base.transform.position, vector, currentSpeed * Time.deltaTime);
		Vector3 eulerAngles = Quaternion.LookRotation(vector - base.transform.position).eulerAngles;
		if (Mathf.Abs(eulerAngles.x) > 45f)
		{
			eulerAngles.x = 0f;
		}
		base.transform.rotation = Quaternion.Euler(eulerAngles);
		if (currentPathPointIdx + 1 < pathPoints.Count && (base.transform.position - vector).sqrMagnitude < 0.1f)
		{
			if (nextPathTimestamp <= Time.time)
			{
				GetNewPath(destination);
			}
			else
			{
				currentPathPointIdx++;
			}
		}
	}

	private void GetNewPath(Vector3 destination)
	{
		path = new NavMeshPath();
		NavMesh.SamplePosition(base.transform.position, out var hit, 5f, 1);
		targetIsOnNavMesh = NavMesh.SamplePosition(destination, out var hit2, 5f, 1);
		NavMesh.CalculatePath(hit.position, hit2.position, -1, path);
		pathPoints = new List<Vector3>();
		Vector3[] corners = path.corners;
		foreach (Vector3 vector in corners)
		{
			pathPoints.Add(vector + Vector3.up * heightAboveNavmesh);
		}
		pathPoints.Add(destination);
		currentPathPointIdx = 0;
		nextPathTimestamp = Time.time + 2f;
	}

	public void ResetPath()
	{
		path = null;
	}

	private void ChaseHost()
	{
		if (followTarget != null)
		{
			if (Time.time > lastSpeedIncreased + velocityIncreaseInterval)
			{
				lastSpeedIncreased = Time.time;
				currentSpeed += velocityStep;
			}
			float num = ZoneShaderSettings.GetWaterY() + MinHeightAboveWater;
			Vector3 position = followTarget.position;
			if (position.y < num)
			{
				position.y = num;
			}
			if (targetIsOnNavMesh)
			{
				UpdateFollowPath(position, currentSpeed);
			}
			else
			{
				base.transform.position = Vector3.MoveTowards(base.transform.position, position, currentSpeed * Time.deltaTime);
			}
		}
	}

	private void MoveBodyShared()
	{
		noisyOffset = new Vector3(Mathf.PerlinNoise(Time.time, 0f) - 0.5f, Mathf.PerlinNoise(Time.time, 10f) - 0.5f, Mathf.PerlinNoise(Time.time, 20f) - 0.5f);
		beeAnimator.transform.localPosition = noisyOffset;
	}

	private void GrabBodyShared()
	{
		if (followTarget != null)
		{
			base.transform.rotation = followTarget.rotation;
			base.transform.position = followTarget.position;
		}
	}

	public override void WriteDataFusion()
	{
		Data = new BeeSwarmData(targetPlayer.ActorNumber, (int)currentState, currentSpeed);
	}

	public override void ReadDataFusion()
	{
		targetPlayer = NetworkSystem.Instance.GetPlayer(Data.TargetActorNumber);
		currentState = (ChaseState)Data.CurrentState;
		if (float.IsFinite(Data.CurrentSpeed))
		{
			currentSpeed = Data.CurrentSpeed;
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender != null && info.Sender.Equals(PhotonNetwork.MasterClient))
		{
			stream.SendNext(targetPlayer?.ActorNumber ?? (-1));
			stream.SendNext(currentState);
			stream.SendNext(currentSpeed);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			int playerID = (int)stream.ReceiveNext();
			targetPlayer = NetworkSystem.Instance.GetPlayer(playerID);
			currentState = (ChaseState)stream.ReceiveNext();
			float f = (float)stream.ReceiveNext();
			if (float.IsFinite(f))
			{
				currentSpeed = f;
			}
		}
	}

	public override void OnOwnerChange(Player newOwner, Player previousOwner)
	{
		base.OnOwnerChange(newOwner, previousOwner);
		if (newOwner == PhotonNetwork.LocalPlayer)
		{
			OnChangeState(currentState);
		}
	}

	public void OnJoinedRoom()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			InitializeSwarm();
		}
	}

	private void TestEmerge()
	{
		Emerge(testEmergeFrom.transform.position, testEmergeTo.transform.position);
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}
}
