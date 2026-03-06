using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts;

[NetworkBehaviourWeaved(6)]
public class LurkerGhost : NetworkComponent
{
	private enum ghostState
	{
		patrol,
		seek,
		charge,
		possess
	}

	[StructLayout(LayoutKind.Explicit, Size = 24)]
	[NetworkStructWeaved(6)]
	private struct LurkerGhostData : INetworkStruct
	{
		[FieldOffset(12)]
		[FixedBufferProperty(typeof(Vector3), typeof(UnityValueSurrogate_0040ElementReaderWriterVector3), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_00403 _TargetPos;

		[field: FieldOffset(0)]
		public ghostState CurrentState { get; set; }

		[field: FieldOffset(4)]
		public int CurrentIndex { get; set; }

		[field: FieldOffset(8)]
		public int TargetActor { get; set; }

		[Networked]
		[NetworkedWeaved(3, 3)]
		public unsafe Vector3 TargetPos
		{
			readonly get
			{
				return *(Vector3*)Native.ReferenceToPointer(ref _TargetPos);
			}
			set
			{
				*(Vector3*)Native.ReferenceToPointer(ref _TargetPos) = value;
			}
		}

		public LurkerGhostData(ghostState state, int index, int actor, Vector3 pos)
		{
			CurrentState = state;
			CurrentIndex = index;
			TargetActor = actor;
			TargetPos = pos;
		}
	}

	public float patrolSpeed = 3f;

	public float seekSpeed = 6f;

	public float chargeSpeed = 6f;

	[Tooltip("Cooldown until the next time the ghost needs to hunt a new player")]
	public float cooldownDuration = 10f;

	[Tooltip("Max Cooldown (randomized)")]
	public float maxCooldownDuration = 10f;

	[Tooltip("How long the possession effects should last")]
	public float PossessionDuration = 15f;

	[Tooltip("Hunted objects within this radius will get triggered ")]
	public float sphereColliderRadius = 2f;

	[Tooltip("Maximum distance to the possible player to get hunted")]
	public float maxHuntDistance = 20f;

	[Tooltip("Minimum distance from the player to start the possession effects")]
	public float minCatchDistance = 2f;

	[Tooltip("Maximum distance to the possible player to get repeat hunted")]
	public float maxRepeatHuntDistance = 5f;

	[Tooltip("Maximum times the lurker can haunt a nearby player before going back on cooldown")]
	public int maxRepeatHuntTimes = 3;

	[Tooltip("Time in seconds before a haunted player can pass the lurker to another player by tagging")]
	public float tagCoolDown = 2f;

	[Tooltip("UP & DOWN, IN & OUT")]
	public Vector3 SpookyMagicNumbers = new Vector3(1f, 1f, 1f);

	[Tooltip("SPIN, SPIN, SPIN, SPIN")]
	public Vector4 HauntedMagicNumbers = new Vector4(1f, 2f, 3f, 1f);

	[Tooltip("Haptic vibration when haunted by the ghost")]
	public float hapticStrength = 1f;

	public float hapticDuration = 1.5f;

	public GameObject waypointsContainer;

	private ZoneBasedObject[] waypointRegions;

	private ZoneBasedObject lastWaypointRegion;

	private List<Transform> waypoints = new List<Transform>();

	private Transform currentWaypoint;

	public Material visibleMaterial;

	public Material scryableMaterial;

	public Material visibleMaterialBones;

	public Material scryableMaterialBones;

	public MeshRenderer meshRenderer;

	public MeshRenderer bonesMeshRenderer;

	[SerializeField]
	private AudioSource audioSource;

	public AudioClip patrolAudio;

	public AudioClip huntAudio;

	public AudioClip possessedAudio;

	public ThrowableSetDressing scryingGlass;

	public float scryingAngerAngle;

	public float scryingAngerDelay;

	public float seekAheadDistance;

	public float seekCloseEnoughDistance;

	private float scryingAngerAfterTimestamp;

	private int currentRepeatHuntTimes;

	public UnityAction<GameObject> TriggerHauntedObjects;

	private int currentIndex;

	private ghostState currentState;

	private float cooldownTimeRemaining;

	private List<NetPlayer> possibleTargets;

	private NetPlayer targetPlayer;

	private Transform targetTransform;

	private float huntedPassedTime;

	private Vector3 targetPosition;

	private Quaternion targetRotation;

	private VRRig targetVRRig;

	private ShaderHashId _BlackAndWhite = "_BlackAndWhite";

	private VRRig lastHauntedVRRig;

	private float nextTagTime;

	private NetPlayer passingPlayer;

	[SerializeField]
	private bool hauntNeighbors = true;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 6)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private LurkerGhostData _Data;

	[Networked]
	[NetworkedWeaved(0, 6)]
	private unsafe LurkerGhostData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing LurkerGhost.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(LurkerGhostData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing LurkerGhost.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(LurkerGhostData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		possibleTargets = new List<NetPlayer>();
		targetPlayer = null;
		targetTransform = null;
		targetVRRig = null;
	}

	protected override void Start()
	{
		base.Start();
		waypointRegions = waypointsContainer.GetComponentsInChildren<ZoneBasedObject>();
		PickNextWaypoint();
		ChangeState(ghostState.patrol);
	}

	private void LateUpdate()
	{
		UpdateState();
		UpdateGhostVisibility();
	}

	private void PickNextWaypoint()
	{
		if (waypoints.Count == 0 || lastWaypointRegion == null || !lastWaypointRegion.IsLocalPlayerInZone())
		{
			ZoneBasedObject zoneBasedObject = ZoneBasedObject.SelectRandomEligible(waypointRegions);
			if (zoneBasedObject == null)
			{
				zoneBasedObject = lastWaypointRegion;
			}
			if (zoneBasedObject == null)
			{
				return;
			}
			lastWaypointRegion = zoneBasedObject;
			waypoints.Clear();
			foreach (Transform item in zoneBasedObject.transform)
			{
				waypoints.Add(item);
			}
		}
		int index = UnityEngine.Random.Range(0, waypoints.Count);
		currentWaypoint = waypoints[index];
		targetRotation = Quaternion.LookRotation(currentWaypoint.position - base.transform.position);
		waypoints.RemoveAt(index);
	}

	private void Patrol()
	{
		Transform transform = currentWaypoint;
		if (transform != null)
		{
			base.transform.position = Vector3.MoveTowards(base.transform.position, transform.position, patrolSpeed * Time.deltaTime);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, targetRotation, 360f * Time.deltaTime);
		}
	}

	private void PlaySound(AudioClip clip, bool loop)
	{
		if ((bool)audioSource && audioSource.isPlaying)
		{
			audioSource.GTStop();
		}
		if ((bool)audioSource && clip != null)
		{
			audioSource.clip = clip;
			audioSource.loop = loop;
			audioSource.GTPlay();
		}
	}

	private bool PickPlayer(float maxDistance)
	{
		if (base.IsMine)
		{
			possibleTargets.Clear();
			for (int i = 0; i < VRRigCache.ActiveRigContainers.Count; i++)
			{
				if ((VRRigCache.ActiveRigContainers[i].transform.position - base.transform.position).magnitude < maxDistance && VRRigCache.ActiveRigContainers[i].Creator != targetPlayer)
				{
					possibleTargets.Add(VRRigCache.ActiveRigContainers[i].Creator);
				}
			}
			targetPlayer = null;
			targetTransform = null;
			targetVRRig = null;
			if (possibleTargets.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, possibleTargets.Count);
				PickPlayer(possibleTargets[index]);
			}
		}
		else
		{
			targetPlayer = null;
			targetTransform = null;
			targetVRRig = null;
		}
		if (targetPlayer != null)
		{
			return targetTransform != null;
		}
		return false;
	}

	private void PickPlayer(NetPlayer player)
	{
		int num = VRRigCache.ActiveRigContainers.FindIndex((RigContainer x) => x.Creator != null && x.Creator == player);
		if (num > -1 && num < VRRigCache.ActiveRigContainers.Count)
		{
			VRRig rig = VRRigCache.ActiveRigContainers[num].Rig;
			targetPlayer = rig.creator;
			targetTransform = rig.head.rigTarget;
			targetVRRig = rig;
		}
	}

	private void SeekPlayer()
	{
		if (targetTransform.IsNull())
		{
			ChangeState(ghostState.patrol);
			return;
		}
		targetPosition = targetTransform.position + targetTransform.forward.x0z() * seekAheadDistance;
		targetRotation = Quaternion.LookRotation(targetTransform.position - base.transform.position);
		base.transform.position = Vector3.MoveTowards(base.transform.position, targetPosition, seekSpeed * Time.deltaTime);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, targetRotation, 720f * Time.deltaTime);
	}

	private void ChargeAtPlayer()
	{
		base.transform.position = Vector3.MoveTowards(base.transform.position, targetPosition, chargeSpeed * Time.deltaTime);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, targetRotation, 720f * Time.deltaTime);
	}

	private void UpdateGhostVisibility()
	{
		switch (currentState)
		{
		case ghostState.patrol:
			meshRenderer.sharedMaterial = scryableMaterial;
			bonesMeshRenderer.sharedMaterial = scryableMaterialBones;
			break;
		case ghostState.seek:
		case ghostState.charge:
			if (targetPlayer == NetworkSystem.Instance.LocalPlayer || passingPlayer == NetworkSystem.Instance.LocalPlayer)
			{
				meshRenderer.sharedMaterial = visibleMaterial;
				bonesMeshRenderer.sharedMaterial = visibleMaterialBones;
			}
			else
			{
				meshRenderer.sharedMaterial = scryableMaterial;
				bonesMeshRenderer.sharedMaterial = scryableMaterialBones;
			}
			break;
		case ghostState.possess:
			if (targetPlayer == NetworkSystem.Instance.LocalPlayer || passingPlayer == NetworkSystem.Instance.LocalPlayer)
			{
				meshRenderer.sharedMaterial = visibleMaterial;
				bonesMeshRenderer.sharedMaterial = visibleMaterialBones;
			}
			else
			{
				meshRenderer.sharedMaterial = scryableMaterial;
				bonesMeshRenderer.sharedMaterial = scryableMaterialBones;
			}
			break;
		}
	}

	private void HauntObjects()
	{
		Collider[] array = new Collider[20];
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, sphereColliderRadius, array);
		for (int i = 0; i < num; i++)
		{
			if (array[i].CompareTag("HauntedObject"))
			{
				TriggerHauntedObjects?.Invoke(array[i].gameObject);
			}
		}
	}

	private void ChangeState(ghostState newState)
	{
		currentState = newState;
		VRRig vRRig = null;
		switch (currentState)
		{
		case ghostState.patrol:
			PlaySound(patrolAudio, loop: true);
			passingPlayer = null;
			cooldownTimeRemaining = UnityEngine.Random.Range(cooldownDuration, maxCooldownDuration);
			currentRepeatHuntTimes = 0;
			break;
		case ghostState.charge:
			PlaySound(huntAudio, loop: false);
			targetPosition = targetTransform.position;
			targetRotation = Quaternion.LookRotation(targetTransform.position - base.transform.position);
			break;
		case ghostState.possess:
			if (targetPlayer == NetworkSystem.Instance.LocalPlayer)
			{
				PlaySound(possessedAudio, loop: true);
				GorillaTagger.Instance.StartVibration(forLeftController: true, hapticStrength, hapticDuration);
				GorillaTagger.Instance.StartVibration(forLeftController: false, hapticStrength, hapticDuration);
			}
			vRRig = GorillaGameManager.StaticFindRigForPlayer(targetPlayer);
			break;
		}
		Shader.SetGlobalFloat(_BlackAndWhite, (newState == ghostState.possess && targetPlayer == NetworkSystem.Instance.LocalPlayer) ? 1 : 0);
		if (vRRig != lastHauntedVRRig && lastHauntedVRRig != null)
		{
			lastHauntedVRRig.IsHaunted = false;
		}
		if (vRRig != null)
		{
			vRRig.IsHaunted = true;
		}
		lastHauntedVRRig = vRRig;
		UpdateGhostVisibility();
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		Shader.SetGlobalFloat(_BlackAndWhite, 0f);
	}

	private void UpdateState()
	{
		switch (currentState)
		{
		case ghostState.patrol:
			Patrol();
			if (!base.IsMine)
			{
				break;
			}
			if (currentWaypoint == null || Vector3.Distance(base.transform.position, currentWaypoint.position) < 0.2f)
			{
				PickNextWaypoint();
			}
			cooldownTimeRemaining -= Time.deltaTime;
			if (cooldownTimeRemaining <= 0f)
			{
				cooldownTimeRemaining = 0f;
				if (PickPlayer(maxHuntDistance))
				{
					ChangeState(ghostState.seek);
				}
			}
			break;
		case ghostState.seek:
			SeekPlayer();
			if (base.IsMine && (targetPosition - base.transform.position).sqrMagnitude < seekCloseEnoughDistance * seekCloseEnoughDistance)
			{
				ChangeState(ghostState.charge);
			}
			break;
		case ghostState.charge:
			ChargeAtPlayer();
			if (base.IsMine && (targetPosition - base.transform.position).sqrMagnitude < 0.25f)
			{
				if ((targetTransform.position - targetPosition).magnitude < minCatchDistance)
				{
					ChangeState(ghostState.possess);
					break;
				}
				huntedPassedTime = 0f;
				ChangeState(ghostState.patrol);
			}
			break;
		case ghostState.possess:
			if (targetTransform != null)
			{
				float num = SpookyMagicNumbers.x + MathF.Abs(MathF.Sin(Time.time * SpookyMagicNumbers.y));
				float num2 = HauntedMagicNumbers.x * MathF.Sin(Time.time * HauntedMagicNumbers.y) + HauntedMagicNumbers.z * MathF.Sin(Time.time * HauntedMagicNumbers.w);
				float y = 0.5f + 0.5f * MathF.Sin(Time.time * SpookyMagicNumbers.z);
				Vector3 target = targetTransform.position + new Vector3(num * (float)Math.Sin(num2), y, num * (float)Math.Cos(num2));
				base.transform.position = Vector3.MoveTowards(base.transform.position, target, chargeSpeed);
				base.transform.rotation = Quaternion.LookRotation(base.transform.position - targetTransform.position);
			}
			if (!base.IsMine)
			{
				break;
			}
			huntedPassedTime += Time.deltaTime;
			if (huntedPassedTime >= PossessionDuration)
			{
				huntedPassedTime = 0f;
				if (hauntNeighbors && currentRepeatHuntTimes < maxRepeatHuntTimes && PickPlayer(maxRepeatHuntDistance))
				{
					currentRepeatHuntTimes++;
					ChangeState(ghostState.seek);
				}
				else
				{
					ChangeState(ghostState.patrol);
				}
			}
			break;
		}
	}

	public override void WriteDataFusion()
	{
		Data = new LurkerGhostData(currentState, currentIndex, targetPlayer.ActorNumber, targetPosition);
	}

	public override void ReadDataFusion()
	{
		ReadDataShared(Data.CurrentState, Data.CurrentIndex, Data.TargetActor, Data.TargetPos);
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			stream.SendNext(currentState);
			stream.SendNext(currentIndex);
			if (targetPlayer != null)
			{
				stream.SendNext(targetPlayer.ActorNumber);
			}
			else
			{
				stream.SendNext(-1);
			}
			stream.SendNext(targetPosition);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			ghostState state = (ghostState)stream.ReceiveNext();
			int index = (int)stream.ReceiveNext();
			int targetActorNumber = (int)stream.ReceiveNext();
			Vector3 targetPos = (Vector3)stream.ReceiveNext();
			ReadDataShared(state, index, targetActorNumber, targetPos);
		}
	}

	private void ReadDataShared(ghostState state, int index, int targetActorNumber, Vector3 targetPos)
	{
		ghostState num = currentState;
		currentState = state;
		currentIndex = index;
		NetPlayer netPlayer = targetPlayer;
		targetPlayer = NetworkSystem.Instance.GetPlayer(targetActorNumber);
		targetPosition = targetPos;
		if (!targetPosition.IsValid(10000f))
		{
			if (VRRigCache.Instance.TryGetVrrig(targetPlayer, out var playerRig))
			{
				targetPosition = (targetPlayer.IsLocal ? playerRig.Rig.transform.position : playerRig.Rig.syncPos);
			}
			else
			{
				targetPosition = base.transform.position;
			}
		}
		if (targetPlayer != netPlayer)
		{
			PickPlayer(targetPlayer);
		}
		if (num != currentState || targetPlayer != netPlayer)
		{
			ChangeState(currentState);
		}
	}

	public override void OnOwnerChange(Player newOwner, Player previousOwner)
	{
		base.OnOwnerChange(newOwner, previousOwner);
		if (newOwner == PhotonNetwork.LocalPlayer)
		{
			ChangeState(currentState);
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
