using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.AI;

[NetworkBehaviourWeaved(5)]
public class HalloweenGhostChaser : NetworkComponent
{
	public enum ChaseState
	{
		Dormant = 1,
		InitialRise = 2,
		Gong = 4,
		Chasing = 8,
		Grabbing = 0x10
	}

	[StructLayout(LayoutKind.Explicit, Size = 20)]
	[NetworkStructWeaved(5)]
	public struct GhostData : INetworkStruct
	{
		[FieldOffset(0)]
		public int TargetActorNumber;

		[FieldOffset(4)]
		public int CurrentState;

		[FieldOffset(8)]
		public int SpawnIndex;

		[FieldOffset(12)]
		[FixedBufferProperty(typeof(float), typeof(UnityValueSurrogate_0040ElementReaderWriterSingle), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_00401 _CurrentSpeed;

		[FieldOffset(16)]
		public NetworkBool IsSummoned;

		[Networked]
		[NetworkedWeaved(3, 1)]
		public unsafe float CurrentSpeed
		{
			readonly get
			{
				return *(float*)Native.ReferenceToPointer(ref _CurrentSpeed);
			}
			set
			{
				*(float*)Native.ReferenceToPointer(ref _CurrentSpeed) = value;
			}
		}
	}

	public float heightAboveNavmesh = 0.5f;

	public Transform followTarget;

	public Transform childGhost;

	public float velocityStep = 1f;

	public float currentSpeed;

	public float velocityIncreaseTime = 20f;

	public float riseDistance = 2f;

	public float summonDistance = 5f;

	public float timeEncircled;

	public float lastSummonCheck;

	public float timeGongStarted;

	public float summoningDuration = 30f;

	public float summoningCheckCountdown = 5f;

	public float gongDuration = 5f;

	public int summonCount = 5;

	public bool wasSurroundedLastCheck;

	public AudioSource laugh;

	public List<NetPlayer> possibleTarget;

	public AudioClip defaultLaugh;

	public AudioClip deepLaugh;

	public AudioClip gong;

	public Vector3 noisyOffset;

	public Vector3 leftArmGrabbingLocal;

	public Vector3 rightArmGrabbingLocal;

	public Vector3 leftHandGrabbingLocal;

	public Vector3 rightHandGrabbingLocal;

	public Vector3 leftHandStartingLocal;

	public Vector3 rightHandStartingLocal;

	public Vector3 ghostOffsetGrabbingLocal;

	public Vector3 ghostStartingEulerRotation;

	public Vector3 ghostGrabbingEulerRotation;

	public float maxTimeToNextHeadAngle;

	public float lastHeadAngleTime;

	public float nextHeadAngleTime;

	public float nextTimeToChasePlayer;

	public float maxNextTimeToChasePlayer;

	public float timeRiseStarted;

	public float totalTimeToRise;

	public float catchDistance;

	public float grabTime;

	public float grabDuration;

	public float grabSpeed = 1f;

	public float minGrabCooldown;

	public float lastSpeedIncreased;

	public Vector3[] headEulerAngles;

	public Transform skullTransform;

	public Transform leftArm;

	public Transform rightArm;

	public Transform leftHand;

	public Transform rightHand;

	public Transform[] spawnTransforms;

	public Transform[] spawnTransformOffsets;

	public NetPlayer targetPlayer;

	public GameObject ghostBody;

	public ChaseState currentState;

	public ChaseState lastState;

	public int spawnIndex;

	public NetPlayer grabbedPlayer;

	public Material ghostMaterial;

	public Color defaultColor;

	public Color summonedColor;

	public bool isSummoned;

	private bool targetIsOnNavMesh;

	private const float navMeshSampleRange = 5f;

	[Tooltip("Haptic vibration when chased by lucy")]
	public float hapticStrength = 1f;

	public float hapticDuration = 1.5f;

	private NavMeshPath path;

	public List<Vector3> points;

	public int currentTargetIdx;

	private float nextPathTimestamp;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 5)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private GhostData _Data;

	[Networked]
	[NetworkedWeaved(0, 5)]
	public unsafe GhostData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing HalloweenGhostChaser.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(GhostData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing HalloweenGhostChaser.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(GhostData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		spawnIndex = 0;
		targetPlayer = null;
		currentState = ChaseState.Dormant;
		grabTime = 0f - minGrabCooldown;
		possibleTarget = new List<NetPlayer>();
	}

	private new void Start()
	{
		NetworkSystem.Instance.RegisterSceneNetworkItem(base.gameObject);
		RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
	}

	private void InitializeGhost()
	{
		if (NetworkSystem.Instance.InRoom && base.IsMine)
		{
			lastHeadAngleTime = 0f;
			nextHeadAngleTime = lastHeadAngleTime + UnityEngine.Random.value * maxTimeToNextHeadAngle;
			nextTimeToChasePlayer = Time.time + UnityEngine.Random.Range(minGrabCooldown, maxNextTimeToChasePlayer);
			ghostBody.transform.localPosition = Vector3.zero;
			base.transform.eulerAngles = Vector3.zero;
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
			{
				if (Time.time >= nextTimeToChasePlayer)
				{
					currentState = ChaseState.InitialRise;
				}
				if (!(Time.time >= lastSummonCheck + summoningDuration))
				{
					break;
				}
				lastSummonCheck = Time.time;
				possibleTarget.Clear();
				int num = 0;
				for (int i = 0; i < spawnTransforms.Length; i++)
				{
					int num2 = 0;
					for (int j = 0; j < VRRigCache.ActiveRigContainers.Count; j++)
					{
						if ((VRRigCache.ActiveRigContainers[j].transform.position - spawnTransforms[i].position).magnitude < summonDistance)
						{
							possibleTarget.Add(VRRigCache.ActiveRigContainers[j].Creator);
							num2++;
							if (num2 >= summonCount)
							{
								break;
							}
						}
					}
					if (num2 >= summonCount)
					{
						if (!wasSurroundedLastCheck)
						{
							wasSurroundedLastCheck = true;
							break;
						}
						wasSurroundedLastCheck = false;
						isSummoned = true;
						currentState = ChaseState.Gong;
						break;
					}
					num++;
				}
				if (num == spawnTransforms.Length)
				{
					wasSurroundedLastCheck = false;
				}
				break;
			}
			case ChaseState.Gong:
				if (Time.time > timeGongStarted + gongDuration)
				{
					currentState = ChaseState.InitialRise;
				}
				break;
			case ChaseState.InitialRise:
				if (Time.time > timeRiseStarted + totalTimeToRise)
				{
					currentState = ChaseState.Chasing;
				}
				break;
			case ChaseState.Chasing:
				if (followTarget == null || targetPlayer == null)
				{
					ChooseRandomTarget();
				}
				if (!(followTarget == null) && (followTarget.position - ghostBody.transform.position).magnitude < catchDistance)
				{
					currentState = ChaseState.Grabbing;
				}
				break;
			case ChaseState.Grabbing:
				if (Time.time > grabTime + grabDuration)
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
		case ChaseState.Dormant:
			isSummoned = false;
			if (ghostMaterial.color == summonedColor)
			{
				ghostMaterial.color = defaultColor;
			}
			break;
		case ChaseState.InitialRise:
			if (NetworkSystem.Instance.InRoom)
			{
				if (base.IsMine)
				{
					RiseHost();
				}
				MoveHead();
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
				MoveHead();
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
				MoveHead();
			}
			break;
		}
	}

	private void OnChangeState(ChaseState newState)
	{
		switch (newState)
		{
		case ChaseState.Dormant:
			if (ghostBody.activeSelf)
			{
				ghostBody.SetActive(value: false);
			}
			if (base.IsMine)
			{
				targetPlayer = null;
				InitializeGhost();
			}
			else
			{
				nextTimeToChasePlayer = Time.time + UnityEngine.Random.Range(minGrabCooldown, maxNextTimeToChasePlayer);
			}
			SetInitialRotations();
			break;
		case ChaseState.Gong:
			if (!ghostBody.activeSelf)
			{
				ghostBody.SetActive(value: true);
			}
			if (base.IsMine)
			{
				ChooseRandomTarget();
				SetInitialSpawnPoint();
				base.transform.position = spawnTransforms[spawnIndex].position;
			}
			timeGongStarted = Time.time;
			laugh.volume = 1f;
			laugh.GTPlayOneShot(gong);
			isSummoned = true;
			break;
		case ChaseState.InitialRise:
			timeRiseStarted = Time.time;
			if (!ghostBody.activeSelf)
			{
				ghostBody.SetActive(value: true);
			}
			if (base.IsMine)
			{
				if (!isSummoned)
				{
					currentSpeed = 0f;
					ChooseRandomTarget();
					SetInitialSpawnPoint();
				}
				else
				{
					currentSpeed = 3f;
				}
			}
			if (isSummoned)
			{
				laugh.volume = 0.25f;
				laugh.GTPlayOneShot(deepLaugh);
				ghostMaterial.color = summonedColor;
			}
			else
			{
				laugh.volume = 0.25f;
				laugh.GTPlay();
				ghostMaterial.color = defaultColor;
			}
			SetInitialRotations();
			break;
		case ChaseState.Grabbing:
		{
			if (!ghostBody.activeSelf)
			{
				ghostBody.SetActive(value: true);
			}
			grabTime = Time.time;
			if (isSummoned)
			{
				laugh.volume = 0.25f;
				laugh.GTPlayOneShot(deepLaugh);
			}
			else
			{
				laugh.volume = 0.25f;
				laugh.GTPlay();
			}
			leftArm.localEulerAngles = leftArmGrabbingLocal;
			rightArm.localEulerAngles = rightArmGrabbingLocal;
			leftHand.localEulerAngles = leftHandGrabbingLocal;
			rightHand.localEulerAngles = rightHandGrabbingLocal;
			ghostBody.transform.localPosition = ghostOffsetGrabbingLocal;
			ghostBody.transform.localEulerAngles = ghostGrabbingEulerRotation;
			VRRig vRRig = GorillaGameManager.StaticFindRigForPlayer(targetPlayer);
			if (vRRig != null)
			{
				followTarget = vRRig.transform;
			}
			break;
		}
		case ChaseState.Chasing:
			if (!ghostBody.activeSelf)
			{
				ghostBody.SetActive(value: true);
			}
			ResetPath();
			break;
		}
	}

	private void SetInitialSpawnPoint()
	{
		float num = 1000f;
		spawnIndex = 0;
		if (followTarget == null)
		{
			return;
		}
		for (int i = 0; i < spawnTransforms.Length; i++)
		{
			float magnitude = (followTarget.position - spawnTransformOffsets[i].position).magnitude;
			if (magnitude < num)
			{
				num = magnitude;
				spawnIndex = i;
			}
		}
	}

	private void ChooseRandomTarget()
	{
		int num = -1;
		if (possibleTarget.Count >= summonCount)
		{
			int randomTarget = UnityEngine.Random.Range(0, possibleTarget.Count);
			num = VRRigCache.ActiveRigContainers.FindIndex((RigContainer x) => x.Creator != null && x.Creator == possibleTarget[randomTarget]);
			currentSpeed = 3f;
		}
		if (num == -1)
		{
			num = UnityEngine.Random.Range(0, VRRigCache.ActiveRigContainers.Count);
		}
		possibleTarget.Clear();
		if (num < VRRigCache.ActiveRigContainers.Count)
		{
			VRRig rig = VRRigCache.ActiveRigContainers[num].Rig;
			targetPlayer = rig.creator;
			followTarget = rig.head.rigTarget;
			targetIsOnNavMesh = NavMesh.SamplePosition(followTarget.position, out var _, 5f, 1);
		}
		else
		{
			targetPlayer = null;
			followTarget = null;
		}
	}

	private void SetInitialRotations()
	{
		leftArm.localEulerAngles = Vector3.zero;
		rightArm.localEulerAngles = Vector3.zero;
		leftHand.localEulerAngles = leftHandStartingLocal;
		rightHand.localEulerAngles = rightHandStartingLocal;
		ghostBody.transform.localPosition = Vector3.zero;
		ghostBody.transform.localEulerAngles = ghostStartingEulerRotation;
	}

	private void MoveHead()
	{
		if (Time.time > nextHeadAngleTime)
		{
			skullTransform.localEulerAngles = headEulerAngles[UnityEngine.Random.Range(0, headEulerAngles.Length)];
			lastHeadAngleTime = Time.time;
			nextHeadAngleTime = lastHeadAngleTime + Mathf.Max(UnityEngine.Random.value * maxTimeToNextHeadAngle, 0.05f);
		}
	}

	private void RiseHost()
	{
		if (Time.time < timeRiseStarted + totalTimeToRise)
		{
			if (spawnIndex == -1)
			{
				spawnIndex = 0;
			}
			base.transform.position = spawnTransforms[spawnIndex].position + Vector3.up * (Time.time - timeRiseStarted) / totalTimeToRise * riseDistance;
			base.transform.rotation = spawnTransforms[spawnIndex].rotation;
		}
	}

	private void RiseGrabbedLocalPlayer()
	{
		if (Time.time > grabTime + minGrabCooldown)
		{
			grabTime = Time.time;
			GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Frozen, GorillaTagger.Instance.tagCooldown);
			GorillaTagger.Instance.StartVibration(forLeftController: true, hapticStrength, hapticDuration);
			GorillaTagger.Instance.StartVibration(forLeftController: false, hapticStrength, hapticDuration);
		}
		if (Time.time < grabTime + grabDuration)
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
		points[points.Count - 1] = destination;
		Vector3 vector = points[currentTargetIdx];
		base.transform.position = Vector3.MoveTowards(base.transform.position, vector, currentSpeed * Time.deltaTime);
		Vector3 eulerAngles = Quaternion.LookRotation(vector - base.transform.position).eulerAngles;
		if (Mathf.Abs(eulerAngles.x) > 45f)
		{
			eulerAngles.x = 0f;
		}
		base.transform.rotation = Quaternion.Euler(eulerAngles);
		if (currentTargetIdx + 1 < points.Count && (base.transform.position - vector).sqrMagnitude < 0.1f)
		{
			if (nextPathTimestamp <= Time.time)
			{
				GetNewPath(destination);
			}
			else
			{
				currentTargetIdx++;
			}
		}
	}

	private void GetNewPath(Vector3 destination)
	{
		path = new NavMeshPath();
		NavMesh.SamplePosition(base.transform.position, out var hit, 5f, 1);
		targetIsOnNavMesh = NavMesh.SamplePosition(destination, out var hit2, 5f, 1);
		NavMesh.CalculatePath(hit.position, hit2.position, -1, path);
		points = new List<Vector3>();
		Vector3[] corners = path.corners;
		foreach (Vector3 vector in corners)
		{
			points.Add(vector + Vector3.up * heightAboveNavmesh);
		}
		points.Add(destination);
		currentTargetIdx = 0;
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
			if (Time.time > lastSpeedIncreased + velocityIncreaseTime)
			{
				lastSpeedIncreased = Time.time;
				currentSpeed += velocityStep;
			}
			if (targetIsOnNavMesh)
			{
				UpdateFollowPath(followTarget.position, currentSpeed);
				return;
			}
			base.transform.position = Vector3.MoveTowards(base.transform.position, followTarget.position, currentSpeed * Time.deltaTime);
			base.transform.rotation = Quaternion.LookRotation(followTarget.position - base.transform.position, Vector3.up);
		}
	}

	private void MoveBodyShared()
	{
		noisyOffset = new Vector3(Mathf.PerlinNoise(Time.time, 0f) - 0.5f, Mathf.PerlinNoise(Time.time, 10f) - 0.5f, Mathf.PerlinNoise(Time.time, 20f) - 0.5f);
		childGhost.localPosition = noisyOffset;
		leftArm.localEulerAngles = noisyOffset * 20f;
		rightArm.localEulerAngles = noisyOffset * -20f;
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
		Data = new GhostData
		{
			TargetActorNumber = (targetPlayer?.ActorNumber ?? (-1)),
			CurrentState = (int)currentState,
			SpawnIndex = spawnIndex,
			CurrentSpeed = currentSpeed,
			IsSummoned = isSummoned
		};
	}

	public override void ReadDataFusion()
	{
		int targetActorNumber = Data.TargetActorNumber;
		targetPlayer = NetworkSystem.Instance.GetPlayer(targetActorNumber);
		currentState = (ChaseState)Data.CurrentState;
		spawnIndex = Data.SpawnIndex;
		float f = Data.CurrentSpeed;
		isSummoned = Data.IsSummoned;
		if (float.IsFinite(f))
		{
			currentSpeed = f;
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (NetworkSystem.Instance.GetPlayer(info.Sender) == NetworkSystem.Instance.MasterClient)
		{
			if (targetPlayer == null)
			{
				stream.SendNext(-1);
			}
			else
			{
				stream.SendNext(targetPlayer.ActorNumber);
			}
			stream.SendNext(currentState);
			stream.SendNext(spawnIndex);
			stream.SendNext(currentSpeed);
			stream.SendNext(isSummoned);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (NetworkSystem.Instance.GetPlayer(info.Sender) == NetworkSystem.Instance.MasterClient)
		{
			int playerID = (int)stream.ReceiveNext();
			targetPlayer = NetworkSystem.Instance.GetPlayer(playerID);
			currentState = (ChaseState)stream.ReceiveNext();
			spawnIndex = (int)stream.ReceiveNext();
			float f = (float)stream.ReceiveNext();
			isSummoned = (bool)stream.ReceiveNext();
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
			InitializeGhost();
		}
		else
		{
			nextTimeToChasePlayer = Time.time + UnityEngine.Random.Range(minGrabCooldown, maxNextTimeToChasePlayer);
		}
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
