using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Rendering;

public class GamePlayer : MonoBehaviour
{
	private struct HandData
	{
		public GameEntityId grabbedEntityId;

		public GameEntityManager grabbedEntityManager;

		public GameEntityId snappedEntityId;

		public GameEntityManager snappedEntityManager;
	}

	private const string preLog = "[GamePlayer]  ";

	private const string preErr = "[GamePlayer]  ERROR!!!  ";

	public VRRig rig;

	public Transform leftHand;

	public Transform rightHand;

	public SuperInfectionSnapPointManager snapPointManager;

	private Transform[] handTransforms;

	private HandData[] hands;

	public const int MAX_HANDS = 2;

	public const int LEFT_HAND = 0;

	public const int RIGHT_HAND = 1;

	public CallLimiter newJoinZoneLimiter;

	public CallLimiter netImpulseLimiter;

	public CallLimiter netGrabLimiter;

	public CallLimiter netThrowLimiter;

	public CallLimiter netStateLimiter;

	public CallLimiter netSnapLimiter;

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

	private void Awake()
	{
		handTransforms = new Transform[2];
		handTransforms[0] = leftHand;
		handTransforms[1] = rightHand;
		hands = new HandData[2];
		ResetData();
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
		for (int i = 0; i < 2; i++)
		{
			if (hands[i].grabbedEntityId != GameEntityId.Invalid && hands[i].grabbedEntityManager != null)
			{
				hands[i].grabbedEntityManager.RequestThrowEntity(hands[i].grabbedEntityId, IsLeftHand(i), GTPlayer.Instance.HeadCenterPosition, Vector3.zero, Vector3.zero);
			}
			ClearGrabbed(i);
		}
		for (int j = 0; j < 2; j++)
		{
			if (hands[j].snappedEntityId != GameEntityId.Invalid && hands[j].snappedEntityManager != null)
			{
				GameEntityId snappedEntityId = hands[j].snappedEntityId;
				GameEntityManager snappedEntityManager = hands[j].snappedEntityManager;
				snappedEntityManager.RequestGrabEntity(snappedEntityId, !IsLeftHand(j), Vector3.zero, Quaternion.identity);
				snappedEntityManager.RequestThrowEntity(snappedEntityId, !IsLeftHand(j), GTPlayer.Instance.HeadCenterPosition, Vector3.zero, Vector3.zero);
			}
			ClearSnapped(j);
		}
	}

	public void ResetData()
	{
		for (int i = 0; i < 2; i++)
		{
			ClearGrabbed(i);
			ClearSnapped(i);
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
		for (int i = 0; i < 2; i++)
		{
			if (hands[i].grabbedEntityManager != null)
			{
				GameEntity gameEntity = hands[i].grabbedEntityManager.GetGameEntity(hands[i].grabbedEntityId);
				if (gameEntity != null)
				{
					gameEntity.MigrateHeldBy(actorNumber);
				}
			}
			if (hands[i].snappedEntityManager != null)
			{
				GameEntity gameEntity2 = hands[i].snappedEntityManager.GetGameEntity(hands[i].snappedEntityId);
				if (gameEntity2 != null)
				{
					gameEntity2.MigrateSnappedBy(actorNumber);
				}
			}
		}
	}

	public void SetGrabbed(GameEntityId gameBallId, int handIndex, GameEntityManager gameEntityManager)
	{
		if (gameBallId.IsValid())
		{
			ClearGrabbedIfHeld(gameBallId);
		}
		HandData handData = hands[handIndex];
		handData.grabbedEntityId = gameBallId;
		handData.grabbedEntityManager = gameEntityManager;
		hands[handIndex] = handData;
	}

	public void SetSnapped(GameEntityId gameBallId, int handIndex, GameEntityManager gameEntityManager)
	{
		if (gameBallId.IsValid())
		{
			ClearSnappedIfSnapped(gameBallId);
			ClearGrabbedIfHeld(gameBallId);
		}
		HandData handData = hands[handIndex];
		handData.snappedEntityId = gameBallId;
		handData.snappedEntityManager = gameEntityManager;
		hands[handIndex] = handData;
	}

	public void ClearZone(GameEntityManager manager)
	{
		for (int i = 0; i < 2; i++)
		{
			if (hands[i].grabbedEntityId != GameEntityId.Invalid && hands[i].grabbedEntityManager == manager)
			{
				hands[i].grabbedEntityManager.GetGameEntity(hands[i].grabbedEntityId)?.OnReleased?.Invoke();
				ClearGrabbed(i);
			}
			if (hands[i].snappedEntityId != GameEntityId.Invalid && hands[i].snappedEntityManager == manager)
			{
				hands[i].snappedEntityManager.GetGameEntity(hands[i].snappedEntityId)?.OnReleased?.Invoke();
				ClearSnapped(i);
			}
		}
		if (NetworkSystem.Instance.SessionIsPrivate)
		{
			DidJoinWithItems = false;
		}
	}

	public void ClearGrabbedIfHeld(GameEntityId gameBallId)
	{
		for (int i = 0; i < 2; i++)
		{
			if (hands[i].grabbedEntityId == gameBallId)
			{
				ClearGrabbed(i);
			}
		}
	}

	public void ClearSnappedIfSnapped(GameEntityId gameBallId)
	{
		for (int i = 0; i < 2; i++)
		{
			if (hands[i].snappedEntityId == gameBallId)
			{
				ClearSnapped(i);
			}
		}
	}

	public void ClearGrabbed(int handIndex)
	{
		SetGrabbed(GameEntityId.Invalid, handIndex, null);
	}

	public void ClearSnapped(int handIndex)
	{
		SetSnapped(GameEntityId.Invalid, handIndex, null);
	}

	public bool IsGrabbingDisabled()
	{
		return grabbingDisabled;
	}

	public void DisableGrabbing(bool disable)
	{
		grabbingDisabled = disable;
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
		while (i < 2)
		{
			if (hands[i].grabbedEntityId != GameEntityId.Invalid && hands[i].grabbedEntityManager == manager)
			{
				yield return hands[i].grabbedEntityId;
			}
			if (hands[i].snappedEntityId != GameEntityId.Invalid && hands[i].snappedEntityManager == manager)
			{
				yield return hands[i].snappedEntityId;
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
		while (i < 2)
		{
			if (hands[i].grabbedEntityId != GameEntityId.Invalid && hands[i].grabbedEntityManager != null && hands[i].grabbedEntityManager != ignoreEntitiesInManager)
			{
				yield return hands[i].grabbedEntityManager.GetGameEntity(hands[i].grabbedEntityId);
			}
			if (hands[i].snappedEntityId != GameEntityId.Invalid && hands[i].snappedEntityManager != null && hands[i].snappedEntityManager != ignoreEntitiesInManager)
			{
				yield return hands[i].snappedEntityManager.GetGameEntity(hands[i].snappedEntityId);
			}
			int num = i + 1;
			i = num;
		}
	}

	public void DeleteGrabbedEntityLocal(int handIndex)
	{
		if (hands[handIndex].grabbedEntityId != GameEntityId.Invalid && hands[handIndex].grabbedEntityManager != null)
		{
			GameEntity gameEntity = hands[handIndex].grabbedEntityManager.GetGameEntity(hands[handIndex].grabbedEntityId);
			if (gameEntity != null)
			{
				gameEntity?.OnReleased?.Invoke();
				hands[handIndex].grabbedEntityManager.DestroyItemLocal(hands[handIndex].grabbedEntityId);
			}
		}
	}

	public int MigrateToEntityManager(GameEntityManager newEntityManager)
	{
		int num = 0;
		for (int i = 0; i < hands.Length; i++)
		{
			GameEntityId grabbedEntityId = hands[i].grabbedEntityId;
			if (grabbedEntityId != GameEntityId.Invalid && hands[i].grabbedEntityManager != newEntityManager)
			{
				GameEntity gameEntity = hands[i].grabbedEntityManager.GetGameEntity(grabbedEntityId);
				if (gameEntity != null && gameEntity.IsValidToMigrate())
				{
					GameEntityId grabbedEntityId2 = gameEntity.MigrateToEntityManager(newEntityManager);
					hands[i].grabbedEntityManager = newEntityManager;
					hands[i].grabbedEntityId = grabbedEntityId2;
					num++;
				}
			}
			GameEntityId snappedEntityId = hands[i].snappedEntityId;
			if (snappedEntityId != GameEntityId.Invalid && hands[i].snappedEntityManager != newEntityManager)
			{
				GameEntity gameEntity2 = hands[i].snappedEntityManager.GetGameEntity(snappedEntityId);
				if (gameEntity2 != null && gameEntity2.IsValidToMigrate())
				{
					GameEntityId snappedEntityId2 = gameEntity2.MigrateToEntityManager(newEntityManager);
					hands[i].snappedEntityManager = newEntityManager;
					hands[i].snappedEntityId = snappedEntityId2;
					num++;
				}
			}
		}
		return num;
	}

	public GameEntityId GetGameEntityId(bool isLeftHand)
	{
		return GetGrabbedGameEntityId(GetHandIndex(isLeftHand));
	}

	public GameEntityId GetGrabbedGameEntityId(int handIndex)
	{
		if (handIndex < 0 || handIndex >= hands.Length)
		{
			return GameEntityId.Invalid;
		}
		return hands[handIndex].grabbedEntityId;
	}

	public GameEntityId GetGrabbedGameEntityIdAndManager(int handIndex, out GameEntityManager manager)
	{
		if (handIndex < 0 || handIndex >= hands.Length)
		{
			manager = null;
			return GameEntityId.Invalid;
		}
		manager = hands[handIndex].grabbedEntityManager;
		return hands[handIndex].grabbedEntityId;
	}

	public GameEntity GetGrabbedGameEntity(int handIndex)
	{
		if (handIndex < 0 || handIndex >= hands.Length || hands[handIndex].grabbedEntityManager == null)
		{
			return null;
		}
		return hands[handIndex].grabbedEntityManager.GetGameEntity(GetGrabbedGameEntityId(handIndex));
	}

	public int FindHandIndex(GameEntityId gameBallId)
	{
		for (int i = 0; i < hands.Length; i++)
		{
			if (hands[i].grabbedEntityId == gameBallId)
			{
				return i;
			}
		}
		return -1;
	}

	public int FindSnapIndex(GameEntityId gameBallId)
	{
		for (int i = 0; i < hands.Length; i++)
		{
			if (hands[i].snappedEntityId == gameBallId)
			{
				return i;
			}
		}
		return -1;
	}

	public GameEntityId GetGameBallId()
	{
		for (int i = 0; i < hands.Length; i++)
		{
			if (hands[i].grabbedEntityId.IsValid())
			{
				return hands[i].grabbedEntityId;
			}
		}
		return GameEntityId.Invalid;
	}

	public static bool IsLeftHand(int handIndex)
	{
		return handIndex == 0;
	}

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
		if (player == null || !VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			out_gamePlayer = null;
			return false;
		}
		return TryGetGamePlayer(playerRig.Rig, out out_gamePlayer);
	}

	[Obsolete("Method `GamePlayer.GetGamePlayer(VRRig)` is obsolete, use `TryGetGamePlayer(VRRig, out GamePlayer)` instead.")]
	public static GamePlayer GetGamePlayer(VRRig rig)
	{
		TryGetGamePlayer(rig, out var out_gamePlayer);
		return out_gamePlayer;
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

	public static Transform GetHandTransform(VRRig rig, int handIndex)
	{
		if (handIndex >= 0 && handIndex < 2 && TryGetGamePlayer(rig, out var out_gamePlayer))
		{
			return out_gamePlayer.handTransforms[handIndex];
		}
		return null;
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
		for (int i = 0; i < 2; i++)
		{
			if (hands[i].grabbedEntityManager == manager)
			{
				int netIdFromEntityId = manager.GetNetIdFromEntityId(hands[i].grabbedEntityId);
				writer.Write(netIdFromEntityId);
				long value = 0L;
				if (netIdFromEntityId != -1)
				{
					GameEntity gameEntity = manager.GetGameEntity(hands[i].grabbedEntityId);
					if (gameEntity != null)
					{
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
			if (hands[i].snappedEntityManager == manager)
			{
				int netIdFromEntityId2 = manager.GetNetIdFromEntityId(hands[i].snappedEntityId);
				writer.Write(netIdFromEntityId2);
				long value2 = 0L;
				if (netIdFromEntityId2 != -1)
				{
					GameEntity gameEntity2 = manager.GetGameEntity(hands[i].snappedEntityId);
					if (gameEntity2 != null)
					{
						value2 = BitPackUtils.PackHandPosRotForNetwork(gameEntity2.transform.localPosition, gameEntity2.transform.localRotation);
					}
				}
				writer.Write(value2);
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
		for (int i = 0; i < 2; i++)
		{
			int num = reader.ReadInt32();
			long num2 = reader.ReadInt64();
			int num3 = reader.ReadInt32();
			long num4 = reader.ReadInt64();
			if (num != -1)
			{
				GameEntityId entityIdFromNetId = manager.GetEntityIdFromNetId(num);
				if (entityIdFromNetId.IsValid())
				{
					GameEntity gameEntity = manager.GetGameEntity(entityIdFromNetId);
					if (num2 != 0L && !(gameEntity == null))
					{
						BitPackUtils.UnpackHandPosRotFromNetwork(num2, out var localPos, out var handRot);
						if (gamePlayer != null && gamePlayer.rig.OwningNetPlayer != null)
						{
							manager.GrabEntityOnCreate(entityIdFromNetId, IsLeftHand(i), localPos, handRot, gamePlayer.rig.OwningNetPlayer);
						}
					}
				}
			}
			if (num3 == -1)
			{
				continue;
			}
			GameEntityId entityIdFromNetId2 = manager.GetEntityIdFromNetId(num3);
			if (!entityIdFromNetId2.IsValid())
			{
				continue;
			}
			GameEntity gameEntity2 = manager.GetGameEntity(entityIdFromNetId2);
			if (num4 != 0L && !(gameEntity2 == null))
			{
				BitPackUtils.UnpackHandPosRotFromNetwork(num4, out var localPos2, out var handRot2);
				if (gamePlayer != null && gamePlayer.rig.OwningNetPlayer != null)
				{
					SnapJointType jointType = (IsLeftHand(i) ? SnapJointType.HandL : SnapJointType.HandR);
					manager.SnapEntityOnCreate(entityIdFromNetId2, IsLeftHand(i), localPos2, handRot2, (int)jointType, gamePlayer.rig.OwningNetPlayer);
				}
			}
		}
		bool initializePlayer = reader.ReadBoolean();
		if (gamePlayer != null)
		{
			gamePlayer.SetInitializePlayer(initializePlayer);
		}
	}

	internal static void InitializeStaticLookupCaches()
	{
		lookupCache_actorNum_to_gamePlayer = new(int, GamePlayer)[10];
		lookupCache_rigInstanceId_to_gamePlayer = new(int, GamePlayer)[10];
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
			if (value.Capacity < 10)
			{
				value.Capacity = 10;
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
