using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GorillaLocomotion;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Rendering;

public class GamePlayer : MonoBehaviour
{
	public struct SlotData
	{
		public GameEntityId entityId;

		public GameEntityManager entityManager;
	}

	private const string preLog = "[GamePlayer]  ";

	private const string preErr = "[GamePlayer]  ERROR!!!  ";

	public VRRig rig;

	public Transform leftHand;

	public Transform rightHand;

	public SuperInfectionSnapPointManager snapPointManager;

	private readonly Transform[] handTransforms = new Transform[2];

	private readonly SlotData[] slots = new SlotData[4];

	public const int MAX_HANDS = 2;

	public const int LEFT_HAND = 0;

	public const int RIGHT_HAND = 1;

	public const int GRAB_SLOT_FIRST = 0;

	public const int GRAB_SLOT_LAST = 1;

	public const int SNAP_SLOTS_COUNT = 2;

	public const int SNAP_SLOTS_FIRST = 2;

	public const int SNAP_SLOTS_LAST = 3;

	public const int SNAP_SLOT_HAND_L = 2;

	public const int SNAP_SLOT_HAND_R = 3;

	public const int SLOTS_COUNT = 4;

	public CallLimiter newJoinZoneLimiter;

	public CallLimiter netImpulseLimiter;

	public CallLimiter netGrabLimiter;

	public CallLimiter netThrowLimiter;

	public CallLimiter netStateLimiter;

	public CallLimiter netSnapLimiter;

	private int _lastSubscriptionCheck;

	private bool _isSubscribed;

	public Action OnPlayerInitialized;

	public Action OnPlayerLeftZone;

	private bool grabbingDisabled;

	private const bool _k_MATTO__USE_STATIC_CACHE = false;

	[OnEnterPlay_SetNull]
	private static (int, GamePlayer)[] lookupCache_actorNum_to_gamePlayer;

	[OnEnterPlay_SetNull]
	private static (int, GamePlayer)[] lookupCache_rigInstanceId_to_gamePlayer;

	[OnEnterPlay_Set(0)]
	private static int staticLookupCachesCount;

	public const int INVALID_ACTOR_NUMBER = int.MinValue;

	public bool DidJoinWithItems { get; set; }

	public bool AdditionalDataInitialized { get; set; }

	public bool IsSubscribed
	{
		get
		{
			if (Time.frameCount != _lastSubscriptionCheck)
			{
				_isSubscribed = SubscriptionManager.IsPlayerSubscribed(rig);
				_lastSubscriptionCheck = Time.frameCount;
			}
			return _isSubscribed;
		}
	}

	private void Awake()
	{
		handTransforms[0] = leftHand;
		handTransforms[1] = rightHand;
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i].entityId = GameEntityId.Invalid;
		}
		newJoinZoneLimiter = new CallLimiter(10, 10f);
		netImpulseLimiter = new CallLimiter(25, 1f);
		netGrabLimiter = new CallLimiter(25, 1f);
		netThrowLimiter = new CallLimiter(25, 1f);
		netStateLimiter = new CallLimiter(25, 1f);
		netSnapLimiter = new CallLimiter(25, 1f);
		if (snapPointManager == null)
		{
			snapPointManager = GetComponentInChildren<SuperInfectionSnapPointManager>(includeInactive: true);
			if (snapPointManager == null)
			{
				Debug.LogError("[GamePlayer]  ERROR!!!  Snappoints cannot function because the required `SuperInfectionSnapPointManager` could found in children.", this);
			}
		}
	}

	public void Clear()
	{
		for (int i = 0; i <= 1; i++)
		{
			if (slots[i].entityId != GameEntityId.Invalid && slots[i].entityManager != null)
			{
				slots[i].entityManager.RequestThrowEntity(slots[i].entityId, IsLeftHand(i), GTPlayer.Instance.HeadCenterPosition, Vector3.zero, Vector3.zero);
			}
			ClearGrabbed(i);
		}
		for (int j = 2; j <= 3; j++)
		{
			if (slots[j].entityId != GameEntityId.Invalid && slots[j].entityManager != null)
			{
				bool isLeftHand = j != 2;
				GameEntityId entityId = slots[j].entityId;
				GameEntityManager entityManager = slots[j].entityManager;
				entityManager.RequestGrabEntity(entityId, isLeftHand, Vector3.zero, Quaternion.identity);
				entityManager.RequestThrowEntity(entityId, isLeftHand, GTPlayer.Instance.HeadCenterPosition, Vector3.zero, Vector3.zero);
			}
			ClearSlot(j);
		}
	}

	public void ResetData()
	{
		for (int i = 0; i < 4; i++)
		{
			ClearSlot(i);
		}
		DidJoinWithItems = false;
		AdditionalDataInitialized = false;
		SetInitializePlayer(initialized: false);
	}

	private void OnEnable()
	{
	}

	private void Start()
	{
		InitializeStaticLookupCaches();
	}

	public void MigrateHeldActorNumbers()
	{
		int actorNumber = rig.OwningNetPlayer.ActorNumber;
		for (int i = 0; i < 4; i++)
		{
			if (!(slots[i].entityManager != null))
			{
				continue;
			}
			GameEntity gameEntity = slots[i].entityManager.GetGameEntity(slots[i].entityId);
			if (gameEntity != null)
			{
				if (i <= 1)
				{
					gameEntity.MigrateHeldBy(actorNumber);
				}
				else
				{
					gameEntity.MigrateSnappedBy(actorNumber);
				}
			}
		}
	}

	public void SetGrabbed(GameEntityId gameBallId, int handIndex, GameEntityManager gameEntityManager)
	{
		if (handIndex >= 0 && handIndex <= 1)
		{
			SetSlot(handIndex, gameBallId, gameEntityManager);
		}
	}

	public void SetSnapped(GameEntityId entityId, int slotIndex, GameEntityManager gameEntityManager)
	{
		if (entityId.IsValid())
		{
			ClearSnappedIfSnapped(entityId, gameEntityManager);
			ClearGrabbedIfHeld(entityId, gameEntityManager);
		}
		SetSlot(slotIndex, entityId, gameEntityManager);
	}

	public void SetSlot(int slotIndex, GameEntityId entityId, GameEntityManager manager)
	{
		if (slotIndex >= 0 && slotIndex < 4)
		{
			if (entityId.IsValid())
			{
				manager.GetGameEntity(entityId);
			}
			SlotData slotData = slots[slotIndex];
			slotData.entityId = entityId;
			slotData.entityManager = manager;
			slots[slotIndex] = slotData;
		}
	}

	public void ClearZone(GameEntityManager manager)
	{
		for (int i = 0; i < 4; i++)
		{
			if (slots[i].entityId != GameEntityId.Invalid && slots[i].entityManager == manager)
			{
				slots[i].entityManager.GetGameEntity(slots[i].entityId)?.OnReleased?.Invoke();
				ClearSlot(i);
			}
		}
		if (NetworkSystem.Instance.SessionIsPrivate)
		{
			DidJoinWithItems = false;
		}
	}

	public void ClearGrabbedIfHeld(GameEntityId gameBallId, GameEntityManager manager)
	{
		for (int i = 0; i <= 1; i++)
		{
			if (slots[i].entityId == gameBallId && slots[i].entityManager == manager)
			{
				ClearGrabbed(i);
			}
		}
	}

	public void ClearSnappedIfSnapped(GameEntityId gameBallId, GameEntityManager manager)
	{
		for (int i = 2; i <= 3; i++)
		{
			if (slots[i].entityId == gameBallId && slots[i].entityManager == manager)
			{
				ClearSlot(i);
			}
		}
	}

	public void ClearGrabbed(int handIndex)
	{
		SetGrabbed(GameEntityId.Invalid, handIndex, null);
	}

	public void ClearSlot(int slotIndex)
	{
		SetSlot(slotIndex, GameEntityId.Invalid, null);
	}

	public bool IsGrabbingDisabled()
	{
		return grabbingDisabled;
	}

	public void DisableGrabbing(bool disable)
	{
		grabbingDisabled = disable;
	}

	internal bool IsSlotOccupied(int slotIndex)
	{
		return slots[slotIndex].entityId.index != -1;
	}

	public bool IsHoldingEntity(GameEntityId gameEntityId, bool isLeftHand)
	{
		return GetGrabbedGameEntityId(GetHandIndex(isLeftHand)) == gameEntityId;
	}

	public bool IsHoldingEntity(GameEntityManager gameEntityManager, bool isLeftHand)
	{
		return gameEntityManager.GetGameEntity(GetGrabbedGameEntityId(GetHandIndex(isLeftHand))) != null;
	}

	public bool IsHoldingEntity(GameEntityId gameEntityId)
	{
		if (!(GetGrabbedGameEntityId(GetHandIndex(leftHand: true)) == gameEntityId))
		{
			return GetGrabbedGameEntityId(GetHandIndex(leftHand: false)) == gameEntityId;
		}
		return true;
	}

	public void RequestDropAllSnapped()
	{
		Clear();
		snapPointManager.DropAllSnappedAuthority();
	}

	public List<GameEntityId> HeldAndSnappedItems(GameEntityManager manager)
	{
		return IterateHeldAndSnappedItems(manager).ToList();
	}

	public IEnumerable<GameEntityId> IterateHeldAndSnappedItems(GameEntityManager manager)
	{
		int i = 0;
		while (i < 4)
		{
			if (slots[i].entityId != GameEntityId.Invalid && slots[i].entityManager == manager)
			{
				yield return slots[i].entityId;
			}
			int num = i + 1;
			i = num;
		}
	}

	public List<GameEntity> HeldAndSnappedEntities(GameEntityManager ignoreEntitiesInManager = null)
	{
		return IterateHeldAndSnappedEntities(ignoreEntitiesInManager).ToList();
	}

	public IEnumerable<GameEntity> IterateHeldAndSnappedEntities(GameEntityManager ignoreEntitiesInManager = null)
	{
		int i = 0;
		while (i < 4)
		{
			if (slots[i].entityId != GameEntityId.Invalid && slots[i].entityManager != null)
			{
				if (slots[i].entityManager != ignoreEntitiesInManager)
				{
					yield return slots[i].entityManager.GetGameEntity(slots[i].entityId);
				}
				else
				{
					slots[i].entityManager.GetGameEntity(slots[i].entityId);
				}
			}
			int num = i + 1;
			i = num;
		}
	}

	public void DeleteGrabbedEntityLocal(int handIndex)
	{
		if (slots[handIndex].entityId != GameEntityId.Invalid && slots[handIndex].entityManager != null)
		{
			GameEntity gameEntity = slots[handIndex].entityManager.GetGameEntity(slots[handIndex].entityId);
			if (gameEntity != null)
			{
				gameEntity?.OnReleased?.Invoke();
				slots[handIndex].entityManager.DestroyItemLocal(slots[handIndex].entityId);
			}
		}
	}

	public int AuthorityMigrateToEntityManager(GameEntityManager newEntityManager)
	{
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			GameEntityId entityId = slots[i].entityId;
			if (!(entityId != GameEntityId.Invalid) || !(slots[i].entityManager != newEntityManager))
			{
				continue;
			}
			GameEntity gameEntity = slots[i].entityManager.GetGameEntity(entityId);
			if (gameEntity != null)
			{
				if (gameEntity.IsScenePlaced)
				{
					slots[i].entityManager?.ReleaseScenePlacedHold(gameEntity);
					ClearSlot(i);
					continue;
				}
				GameEntityId entityId2 = gameEntity.MigrateToEntityManager(newEntityManager);
				SlotData slotData = slots[i];
				slotData.entityManager = newEntityManager;
				slotData.entityId = entityId2;
				slots[i] = slotData;
				num++;
			}
		}
		return num;
	}

	internal bool IsInSlot(int slotIndex, int entityIndex, GameEntityManager manager)
	{
		if (slots[slotIndex].entityId.index == entityIndex)
		{
			return slots[slotIndex].entityManager == manager;
		}
		return false;
	}

	internal bool TryGetSlotData(int slotIndex, out SlotData out_slotData)
	{
		out_slotData = slots[slotIndex];
		return out_slotData.entityId.index != -1;
	}

	internal bool TryGetSlotEntity(int slotIndex, out GameEntity out_entity)
	{
		if (!TryGetSlotData(slotIndex, out var out_slotData))
		{
			out_entity = null;
			return false;
		}
		out_entity = out_slotData.entityManager.GetGameEntity(out_slotData.entityId);
		return out_entity != null;
	}

	public GameEntityId GetGameEntityId(bool isLeftHand)
	{
		return GetGrabbedGameEntityId(GetHandIndex(isLeftHand));
	}

	public GameEntityId GetGrabbedGameEntityId(int handIndex)
	{
		if (handIndex < 0 || handIndex > 1)
		{
			return GameEntityId.Invalid;
		}
		return slots[handIndex].entityId;
	}

	public GameEntityId GetGrabbedGameEntityIdAndManager(int handIndex, out GameEntityManager manager)
	{
		if (handIndex < 0 || handIndex > 1)
		{
			manager = null;
			return GameEntityId.Invalid;
		}
		manager = slots[handIndex].entityManager;
		return slots[handIndex].entityId;
	}

	public GameEntity GetGrabbedGameEntity(int handIndex)
	{
		if (handIndex < 0 || handIndex > 1 || slots[handIndex].entityManager == null)
		{
			return null;
		}
		return slots[handIndex].entityManager.GetGameEntity(GetGrabbedGameEntityId(handIndex));
	}

	public int FindSlotIndex(GameEntityId entityId)
	{
		int num = -1;
		for (int i = 0; i < 4; i++)
		{
			if (num != -1)
			{
				break;
			}
			num = ((slots[i].entityId == entityId) ? i : (-1));
		}
		return num;
	}

	public int FindHandIndex(GameEntityId entityId)
	{
		for (int i = 0; i <= 1; i++)
		{
			if (slots[i].entityId == entityId)
			{
				return i;
			}
		}
		return -1;
	}

	public int FindSnapIndex(GameEntityId entityId)
	{
		for (int i = 2; i <= 3; i++)
		{
			if (slots[i].entityId == entityId)
			{
				return i;
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsLeftHand(int handIndex)
	{
		return handIndex == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetHandIndex(bool leftHand)
	{
		if (!leftHand)
		{
			return 1;
		}
		return 0;
	}

	[Obsolete("Method `GamePlayer.TryGetGamePlayer(Player)` is obsolete, use `TryGetGamePlayer(Player, out GamePlayer)` instead.")]
	public static VRRig GetRig(int actorNumber)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(actorNumber);
		if (player == null)
		{
			return null;
		}
		Room currentRoom = PhotonNetwork.CurrentRoom;
		if (currentRoom != null && currentRoom.GetPlayer(actorNumber) == null)
		{
			return null;
		}
		if (!VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			return null;
		}
		return playerRig.Rig;
	}

	public static GamePlayer GetGamePlayer(Player player)
	{
		TryGetGamePlayer(player, out var gamePlayer);
		return gamePlayer;
	}

	public static bool TryGetGamePlayer(Player player, out GamePlayer gamePlayer)
	{
		if (player == null)
		{
			gamePlayer = null;
			return false;
		}
		return TryGetGamePlayer(player.ActorNumber, out gamePlayer);
	}

	[Obsolete("Method `GamePlayer.GetGamePlayer(actorNum)` is obsolete, use `TryGetGamePlayer(actorNum, out GamePlayer)` instead.")]
	public static GamePlayer GetGamePlayer(int actorNumber)
	{
		TryGetGamePlayer(actorNumber, out var out_gamePlayer);
		return out_gamePlayer;
	}

	public static bool TryGetGamePlayer(int actorNumber, out GamePlayer out_gamePlayer)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(actorNumber);
		if (player == null || VRRigCache.Instance == null || !VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			out_gamePlayer = null;
			return false;
		}
		return TryGetGamePlayer(playerRig.Rig, out out_gamePlayer);
	}

	public static bool TryGetGamePlayer(VRRig rig, out GamePlayer out_gamePlayer)
	{
		if (rig == null)
		{
			out_gamePlayer = null;
			return false;
		}
		out_gamePlayer = rig.GetComponent<GamePlayer>();
		return out_gamePlayer != null;
	}

	public static GamePlayer GetGamePlayer(Collider collider, bool bodyOnly = false)
	{
		Transform parent = collider.transform;
		while (parent != null)
		{
			GamePlayer component = parent.GetComponent<GamePlayer>();
			if (component != null)
			{
				return component;
			}
			if (bodyOnly)
			{
				break;
			}
			parent = parent.parent;
		}
		return null;
	}

	public Transform GetHandTransform(int handIndex)
	{
		if (handIndex < 0 || handIndex > 1)
		{
			return null;
		}
		return handTransforms[handIndex];
	}

	public bool TryGetSlotXform(int slotIndex, out Transform slotXform)
	{
		if (IsGrabSlot(slotIndex))
		{
			slotXform = handTransforms[slotIndex];
		}
		else if (IsSnapSlot(slotIndex))
		{
			SnapJointType snapIndexToJoint = GameSnappable.GetSnapIndexToJoint(slotIndex);
			SuperInfectionSnapPoint superInfectionSnapPoint = ((snapPointManager != null) ? snapPointManager.FindSnapPoint(snapIndexToJoint) : null);
			slotXform = ((superInfectionSnapPoint != null) ? superInfectionSnapPoint.transform : null);
		}
		else
		{
			slotXform = null;
		}
		return slotXform != null;
	}

	public bool IsLocal()
	{
		if (GamePlayerLocal.instance != null)
		{
			return GamePlayerLocal.instance.gamePlayer == this;
		}
		return false;
	}

	public void SerializeNetworkState(BinaryWriter writer, NetPlayer player, GameEntityManager manager)
	{
		string text = "";
		for (int i = 0; i < 4; i++)
		{
			if (slots[i].entityManager == manager)
			{
				int netIdFromEntityId = manager.GetNetIdFromEntityId(slots[i].entityId);
				writer.Write(netIdFromEntityId);
				long value = 0L;
				if (netIdFromEntityId != -1)
				{
					GameEntity gameEntity = manager.GetGameEntity(slots[i].entityId);
					if (gameEntity != null)
					{
						text += $" [{i}: {gameEntity.gameObject.name}/{netIdFromEntityId}]";
						value = BitPackUtils.PackHandPosRotForNetwork(gameEntity.transform.localPosition, gameEntity.transform.localRotation);
					}
				}
				writer.Write(value);
			}
			else
			{
				writer.Write(-1);
				writer.Write(0L);
			}
		}
		writer.Write(AdditionalDataInitialized);
	}

	public static void DeserializeNetworkState(BinaryReader reader, GamePlayer gamePlayer, GameEntityManager manager)
	{
		for (int i = 0; i < 4; i++)
		{
			int num = reader.ReadInt32();
			long num2 = reader.ReadInt64();
			if (num == -1)
			{
				continue;
			}
			GameEntityId entityIdFromNetId = manager.GetEntityIdFromNetId(num);
			if (!entityIdFromNetId.IsValid())
			{
				continue;
			}
			GameEntity gameEntity = manager.GetGameEntity(entityIdFromNetId);
			if (num2 == 0L || gameEntity == null)
			{
				continue;
			}
			BitPackUtils.UnpackHandPosRotFromNetwork(num2, out var localPos, out var handRot);
			if (!(gamePlayer != null) || gamePlayer.rig.OwningNetPlayer == null)
			{
				continue;
			}
			if (IsGrabSlot(i))
			{
				manager.GrabEntityOnCreate(entityIdFromNetId, IsLeftHand(i), localPos, handRot, gamePlayer.rig.OwningNetPlayer);
				continue;
			}
			int jointType = -1;
			switch (i)
			{
			case 2:
				jointType = 1;
				break;
			case 3:
				jointType = 4;
				break;
			}
			manager.SnapEntityOnCreate(entityIdFromNetId, i == 2, localPos, handRot, jointType, gamePlayer.rig.OwningNetPlayer);
		}
		bool initializePlayer = reader.ReadBoolean();
		if (gamePlayer != null)
		{
			gamePlayer.SetInitializePlayer(initializePlayer);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsSlot(int i)
	{
		if (i >= 0)
		{
			return i < 4;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsGrabSlot(int i)
	{
		if (i >= 0)
		{
			return i <= 1;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsSnapSlot(int i)
	{
		if (i >= 2)
		{
			return i <= 3;
		}
		return false;
	}

	internal static void InitializeStaticLookupCaches()
	{
		lookupCache_actorNum_to_gamePlayer = new(int, GamePlayer)[20];
		lookupCache_rigInstanceId_to_gamePlayer = new(int, GamePlayer)[20];
		if (VRRigCache.isInitialized)
		{
			UpdateStaticLookupCaches();
		}
	}

	internal static void UpdateStaticLookupCaches()
	{
		if (lookupCache_actorNum_to_gamePlayer == null)
		{
			return;
		}
		List<VRRig> value;
		using (ListPool<VRRig>.Get(out value))
		{
			if (value.Capacity < 20)
			{
				value.Capacity = 20;
			}
			VRRigCache.Instance.GetActiveRigs(value);
			if (value.Count > lookupCache_actorNum_to_gamePlayer.Length)
			{
				int newSize = value.Count * 2;
				Array.Resize(ref lookupCache_actorNum_to_gamePlayer, newSize);
				Array.Resize(ref lookupCache_rigInstanceId_to_gamePlayer, newSize);
			}
			staticLookupCachesCount = value.Count;
			if (staticLookupCachesCount >= 1)
			{
				VRRig vRRig = value[0];
				if (vRRig == null)
				{
					throw new NullReferenceException("[GT/GamePlayer::_VRRigCache_OnActiveRigsChanged]  ERROR!!!  (should never happen) The VRRig at index 0 is expected to be the local rig but is null.");
				}
				int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
				GamePlayer gamePlayer = GamePlayerLocal.instance.gamePlayer;
				lookupCache_actorNum_to_gamePlayer[0] = (actorNumber, gamePlayer);
				lookupCache_rigInstanceId_to_gamePlayer[0] = (vRRig.GetInstanceID(), gamePlayer);
			}
			for (int i = 1; i < staticLookupCachesCount; i++)
			{
				VRRig vRRig2 = value[i];
				if (vRRig2 == null)
				{
					throw new NullReferenceException("[GT/GamePlayer::_VRRigCache_OnActiveRigsChanged]  ERROR!!!  (should never happen) An entry from `VRRigCache.Instance.GetActiveRigs(activeRigs)` is null but is expected to be ready and all entries not null at this stage.");
				}
				GamePlayer component = vRRig2.GetComponent<GamePlayer>();
				if (component == null)
				{
					throw new NullReferenceException("[GT/GamePlayer::_VRRigCache_OnActiveRigsChanged]  ERROR!!!  (should never happen) Could not get GamePlayer from rig which is expected to be ready at this stage.");
				}
				int item = vRRig2.OwningNetPlayer?.ActorNumber ?? int.MinValue;
				lookupCache_actorNum_to_gamePlayer[i] = (item, component);
				lookupCache_rigInstanceId_to_gamePlayer[i] = (vRRig2.GetInstanceID(), component);
			}
			for (int j = staticLookupCachesCount; j < lookupCache_actorNum_to_gamePlayer.Length; j++)
			{
				lookupCache_actorNum_to_gamePlayer[j] = (0, null);
				lookupCache_rigInstanceId_to_gamePlayer[j] = (0, null);
			}
		}
	}

	public void SetInitializePlayer(bool initialized)
	{
		bool additionalDataInitialized = AdditionalDataInitialized;
		AdditionalDataInitialized = initialized;
		if (!additionalDataInitialized && AdditionalDataInitialized)
		{
			OnPlayerInitialized?.Invoke();
		}
	}
}
