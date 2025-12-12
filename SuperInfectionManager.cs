using System;
using System.Collections.Generic;
using System.IO;
using GorillaExtensions;
using GorillaGameModes;
using Photon.Pun;
using UnityEngine;

[DefaultExecutionOrder(0)]
public class SuperInfectionManager : MonoBehaviour, IGameEntityZoneComponent, IFactoryItemProvider
{
	public enum ClientToAuthorityRPC
	{
		CombinedTerminalButtonPress,
		CombinedTerminalHandScan,
		ResourceDepositDeposited,
		CallEntityRPC,
		CallEntityRPCData
	}

	public enum AuthorityToClientRPC
	{
		TechPointGranted,
		ResourceDepositTechPointGranted,
		ResourceDepositTechPointRejected,
		CallEntityRPC,
		CallEntityRPCData,
		TriggerMonkeIdolDepositCelebration
	}

	public enum ClientToClientRPC
	{
		BroadcastProgression,
		LaunchDashYoyo,
		CallEntityRPC,
		CallEntityRPCData
	}

	private const string preLog = "[SuperInfectionManager]  ";

	private const string preErr = "[SuperInfectionManager]  ERROR!!!  ";

	public GameEntityManager gameEntityManager;

	public TestSpawnGadget testSpawner;

	public PhotonView photonView;

	public XSceneRef zoneSuperInfectionRef;

	[NonSerialized]
	public SuperInfection zoneSuperInfection;

	[SerializeField]
	private SITechTreeSO techTreeSO;

	[SerializeField]
	private SIProgression progression;

	public static SuperInfectionManager activeSuperInfectionManager;

	public static Dictionary<GTZone, SuperInfectionManager> siManagerByZone = new Dictionary<GTZone, SuperInfectionManager>();

	private static List<VRRig> tempRigs = new List<VRRig>(10);

	private static List<VRRig> tempRigs2 = new List<VRRig>(10);

	private readonly Dictionary<SnapJointType, List<SuperInfectionSnapPoint>> allSnapPoints = new Dictionary<SnapJointType, List<SuperInfectionSnapPoint>>();

	private const float rpcProximityCheckRange = 3f;

	private bool PendingZoneInit;

	private bool PendingTableData;

	private void Awake()
	{
		GameEntityManager obj = gameEntityManager;
		obj.OnEntityRemoved = (Action<GameEntity>)Delegate.Combine(obj.OnEntityRemoved, new Action<GameEntity>(OnEntityRemoved));
	}

	public void OnEnableZoneSuperInfection(SuperInfection zone)
	{
		zoneSuperInfection = zone;
		if (PendingZoneInit)
		{
			PendingZoneInit = false;
			OnZoneInit();
		}
		if (gameEntityManager.PendingTableData)
		{
			gameEntityManager.ResolveTableData();
		}
	}

	private void OnEnable()
	{
		siManagerByZone.TryAdd(gameEntityManager.zone, this);
	}

	private void OnDisable()
	{
		siManagerByZone.Remove(gameEntityManager.zone);
	}

	public static SuperInfectionManager GetSIManagerForZone(GTZone targetZone)
	{
		if (siManagerByZone.TryGetValue(targetZone, out var value))
		{
			return value;
		}
		return null;
	}

	public void OnZoneCreate()
	{
	}

	public void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!(zoneSuperInfection == null) && gameEntityManager.IsAuthority())
		{
			for (int i = 0; i < zoneSuperInfection.siTerminals.Length; i++)
			{
				zoneSuperInfection.siTerminals[i].WriteDataPUN(stream, info);
			}
			for (int j = 0; j < zoneSuperInfection.siDeposits.Length; j++)
			{
				zoneSuperInfection.siDeposits[j].WriteDataPUN(stream, info);
			}
			zoneSuperInfection.questBoard.WriteDataPUN(stream, info);
		}
	}

	public void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!(zoneSuperInfection == null) && gameEntityManager.IsAuthorityPlayer(info.Sender))
		{
			for (int i = 0; i < zoneSuperInfection.siTerminals.Length; i++)
			{
				zoneSuperInfection.siTerminals[i].ReadDataPUN(stream, info);
			}
			for (int j = 0; j < zoneSuperInfection.siDeposits.Length; j++)
			{
				zoneSuperInfection.siDeposits[j].ReadDataPUN(stream, info);
			}
			zoneSuperInfection.questBoard.ReadDataPUN(stream, info);
		}
	}

	void IGameEntityZoneComponent.SerializeZoneData(BinaryWriter writer)
	{
		if (!(zoneSuperInfection == null))
		{
			for (int i = 0; i < zoneSuperInfection.siTerminals.Length; i++)
			{
				zoneSuperInfection.siTerminals[i].SerializeZoneData(writer);
			}
		}
	}

	void IGameEntityZoneComponent.DeserializeZoneData(BinaryReader reader)
	{
		if (!(zoneSuperInfection == null))
		{
			for (int i = 0; i < zoneSuperInfection.siTerminals.Length; i++)
			{
				zoneSuperInfection.siTerminals[i].DeserializeZoneData(reader);
			}
		}
	}

	public void SerializeZoneEntityData(BinaryWriter writer, GameEntity entity)
	{
	}

	public void DeserializeZoneEntityData(BinaryReader reader, GameEntity entity)
	{
	}

	void IGameEntityZoneComponent.SerializeZonePlayerData(BinaryWriter writer, int actorNumber)
	{
		SIPlayer sIPlayer = SIPlayer.Get(actorNumber);
		sIPlayer.SerializeNetworkState(writer, sIPlayer.gamePlayer.rig.OwningNetPlayer);
	}

	void IGameEntityZoneComponent.DeserializeZonePlayerData(BinaryReader reader, int actorNumber)
	{
		SIPlayer player = SIPlayer.Get(actorNumber);
		SIPlayer.DeserializeNetworkStateAndBurn(reader, player, this);
	}

	public bool IsZoneReady()
	{
		if (NetworkSystem.Instance.InRoom && IsInSuperInfectionMode() && zoneSuperInfection.IsNotNull())
		{
			return VRRig.LocalRig.zoneEntity.currentZone == gameEntityManager.zone;
		}
		return false;
	}

	public bool ShouldClearZone()
	{
		if (GameMode.ActiveGameMode != null)
		{
			return GameMode.ActiveGameMode.GameType() != GameModeType.SuperInfect;
		}
		return false;
	}

	public bool IsInSuperInfectionMode()
	{
		if (GameMode.ActiveGameMode != null)
		{
			return GameMode.ActiveGameMode.GameType() == GameModeType.SuperInfect;
		}
		return false;
	}

	public void OnCreateGameEntity(GameEntity entity)
	{
		SIGadget component = entity.GetComponent<SIGadget>();
		if (component != null)
		{
			SIPlayer sIPlayer = SIPlayer.Get((int)(entity.createData & 0xFFFFFFFFu));
			if (sIPlayer != null && !sIPlayer.activePlayerGadgets.Contains(entity.GetNetId()))
			{
				sIPlayer.activePlayerGadgets.Add(entity.GetNetId());
			}
			SIUpgradeSet upgrades = new SIUpgradeSet((int)(entity.createData >> 32));
			upgrades = component.FilterUpgradeNodes(upgrades);
			component.ApplyUpgradeNodes(upgrades);
			component.RefreshUpgradeVisuals(upgrades);
			if (zoneSuperInfection != null)
			{
				zoneSuperInfection.AddGadget(component);
			}
		}
		SuperInfectionSnapPoint[] componentsInChildren = entity.GetComponentsInChildren<SuperInfectionSnapPoint>(includeInactive: true);
		foreach (SuperInfectionSnapPoint snapPoint in componentsInChildren)
		{
			RegisterSnapPoint(snapPoint);
		}
	}

	public void OnZoneClear(ZoneClearReason reason)
	{
		zoneSuperInfection?.OnZoneClear(reason);
		SIPlayer.LocalPlayer?.Reset();
		SIPlayer.ClearPlayerCache();
		allSnapPoints.Clear();
	}

	public void OnZoneInit()
	{
		if ((object)zoneSuperInfection == null)
		{
			PendingZoneInit = true;
			return;
		}
		activeSuperInfectionManager = this;
		if (gameEntityManager.IsAuthority())
		{
			TestSpawnGadget();
		}
		zoneSuperInfection.OnZoneInit();
		if (SIPlayer.Get(NetworkSystem.Instance.LocalPlayer.ActorNumber) != null)
		{
			progression.Init();
			if (progression.ClientReady)
			{
				SIPlayer.SetAndBroadcastProgression();
			}
			else
			{
				progression.OnClientReady += WhenReady;
			}
		}
		allSnapPoints.Clear();
		foreach (GameEntity gameEntity in gameEntityManager.GetGameEntities())
		{
			if (!(gameEntity == null))
			{
				SuperInfectionSnapPoint[] componentsInChildren = gameEntity.GetComponentsInChildren<SuperInfectionSnapPoint>(includeInactive: true);
				foreach (SuperInfectionSnapPoint snapPoint in componentsInChildren)
				{
					RegisterSnapPoint(snapPoint);
				}
			}
		}
		void WhenReady()
		{
			progression.OnClientReady -= WhenReady;
			SIPlayer.SetAndBroadcastProgression();
		}
	}

	public void RegisterSnapPoint(SuperInfectionSnapPoint snapPoint)
	{
		if (!allSnapPoints.TryGetValue(snapPoint.jointType, out var value))
		{
			value = (allSnapPoints[snapPoint.jointType] = new List<SuperInfectionSnapPoint>());
		}
		value.Add(snapPoint);
	}

	public void UnregisterSnapPoint(SuperInfectionSnapPoint snapPoint)
	{
		if (allSnapPoints.ContainsKey(snapPoint.jointType))
		{
			allSnapPoints[snapPoint.jointType].Remove(snapPoint);
			if (allSnapPoints[snapPoint.jointType].Count == 0)
			{
				allSnapPoints.Remove(snapPoint.jointType);
			}
		}
	}

	public IEnumerable<SuperInfectionSnapPoint> GetPoints(SnapJointType jointType)
	{
		foreach (KeyValuePair<SnapJointType, List<SuperInfectionSnapPoint>> allSnapPoint in allSnapPoints)
		{
			if ((allSnapPoint.Key & jointType) == 0)
			{
				continue;
			}
			foreach (SuperInfectionSnapPoint item in allSnapPoint.Value)
			{
				yield return item;
			}
		}
	}

	public SuperInfectionSnapPoint FindNearestSnapPoint(SnapJointType jointType, Vector3 origin, float maxDist, bool includeOccupied = false)
	{
		SuperInfectionSnapPoint result = null;
		float num = maxDist * maxDist;
		foreach (SuperInfectionSnapPoint point in GetPoints(jointType))
		{
			if (!(point == null) && point.isActiveAndEnabled && (includeOccupied || !point.HasSnapped()))
			{
				float sqrMagnitude = (point.transform.position - origin).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					result = point;
					num = sqrMagnitude;
				}
			}
		}
		return result;
	}

	public void CallRPC(ClientToAuthorityRPC clientToAuthorityRPC, object[] data)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			photonView.RPC("SIClientToAuthorityRPC", gameEntityManager.GetAuthorityPlayer(), (int)clientToAuthorityRPC, data);
		}
	}

	public void CallRPC(ClientToClientRPC clientToClientRPC, object[] data)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			photonView.RPC("SIClientToClientRPC", RpcTarget.Others, (int)clientToClientRPC, data);
		}
	}

	public void CallRPC(AuthorityToClientRPC authorityToClientRPC, object[] data)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			photonView.RPC("SIAuthorityToClientRPC", RpcTarget.Others, (int)authorityToClientRPC, data);
		}
	}

	public void CallRPC(AuthorityToClientRPC authorityToClientRPC, int actorNr, object[] data)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			photonView.RPC("SIAuthorityToClientRPC", NetworkSystem.Instance.GetNetPlayerByID(actorNr).GetPlayerRef(), (int)authorityToClientRPC, data);
		}
	}

	[PunRPC]
	public void SIClientToAuthorityRPC(int clientToAuthorityRPCEnum, object[] data, PhotonMessageInfo info)
	{
		if (gameEntityManager.IsValidAuthorityRPC(info.Sender) && data != null)
		{
			SIPlayer sIPlayer = SIPlayer.Get(info.Sender.ActorNumber);
			if (!sIPlayer.IsNull() && sIPlayer.clientToAuthorityRPCLimiter.CheckCallTime(Time.unscaledTime))
			{
				ProcessClientToAuthorityRPC(clientToAuthorityRPCEnum, data, info);
			}
		}
	}

	public void ProcessClientToAuthorityRPC(int clientToAuthorityRPCEnum, object[] data, PhotonMessageInfo info)
	{
		switch ((ClientToAuthorityRPC)clientToAuthorityRPCEnum)
		{
		case ClientToAuthorityRPC.CombinedTerminalButtonPress:
		{
			if (data.Length == 4 && GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType4) && GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType5) && GameEntityManager.ValidateDataType<int>(data[2], out var dataAsType6) && GameEntityManager.ValidateDataType<int>(data[3], out var dataAsType7) && dataAsType7 >= 0 && dataAsType7 < zoneSuperInfection.siTerminals.Length && Enum.IsDefined(typeof(SITouchscreenButton.SITouchscreenButtonType), (SITouchscreenButton.SITouchscreenButtonType)dataAsType4) && Enum.IsDefined(typeof(SICombinedTerminal.TerminalSubFunction), (SICombinedTerminal.TerminalSubFunction)dataAsType6))
			{
				zoneSuperInfection.siTerminals[dataAsType7].TouchscreenButtonPressed((SITouchscreenButton.SITouchscreenButtonType)dataAsType4, dataAsType5, info.Sender.ActorNumber, (SICombinedTerminal.TerminalSubFunction)dataAsType6);
			}
			break;
		}
		case ClientToAuthorityRPC.CombinedTerminalHandScan:
		{
			if (data.Length != 1 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType12) || dataAsType12 < 0 || dataAsType12 >= zoneSuperInfection.siTerminals.Length)
			{
				break;
			}
			SIPlayer sIPlayer = SIPlayer.Get(info.Sender.ActorNumber);
			if (!(sIPlayer == null))
			{
				SICombinedTerminal sICombinedTerminal = zoneSuperInfection.siTerminals[dataAsType12];
				if (sIPlayer.gamePlayer.rig.IsPositionInRange(sICombinedTerminal.transform.position, 3f))
				{
					sICombinedTerminal.PlayerHandScanned(info.Sender.ActorNumber);
				}
			}
			break;
		}
		case ClientToAuthorityRPC.ResourceDepositDeposited:
		{
			if (data.Length != 2 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType8) || !GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType9) || dataAsType9 < 0 || dataAsType9 >= zoneSuperInfection.siDeposits.Length)
			{
				break;
			}
			GameEntity gameEntityFromNetId2 = gameEntityManager.GetGameEntityFromNetId(dataAsType8);
			if (gameEntityFromNetId2 == null)
			{
				break;
			}
			SIResourceDeposit sIResourceDeposit = zoneSuperInfection.siDeposits[dataAsType9];
			if (!(gameEntityFromNetId2.transform.position - sIResourceDeposit.transform.position).IsLongerThan(3f))
			{
				SIResource component2 = gameEntityFromNetId2.GetComponent<SIResource>();
				if (component2 != null)
				{
					sIResourceDeposit.ResourceDeposited(component2);
				}
			}
			break;
		}
		case ClientToAuthorityRPC.CallEntityRPC:
		{
			if (data.Length != 2 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType10) || !GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType11))
			{
				break;
			}
			GameEntity gameEntityFromNetId3 = gameEntityManager.GetGameEntityFromNetId(dataAsType10);
			if ((bool)gameEntityFromNetId3)
			{
				SIGadget component3 = gameEntityFromNetId3.GetComponent<SIGadget>();
				if ((bool)component3)
				{
					component3.ProcessClientToAuthorityRPC(info, dataAsType11, null);
				}
			}
			break;
		}
		case ClientToAuthorityRPC.CallEntityRPCData:
		{
			if (data.Length != 3 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType) || !GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType2) || !GameEntityManager.ValidateDataType<object[]>(data[2], out var dataAsType3))
			{
				break;
			}
			GameEntity gameEntityFromNetId = gameEntityManager.GetGameEntityFromNetId(dataAsType);
			if ((bool)gameEntityFromNetId)
			{
				SIGadget component = gameEntityFromNetId.GetComponent<SIGadget>();
				if ((bool)component)
				{
					component.ProcessClientToAuthorityRPC(info, dataAsType2, dataAsType3);
				}
			}
			break;
		}
		}
	}

	[PunRPC]
	public void SIAuthorityToClientRPC(int authorityToClientRPCEnum, object[] data, PhotonMessageInfo info)
	{
		if (gameEntityManager.IsValidClientRPC(info.Sender) && data != null)
		{
			SIPlayer sIPlayer = SIPlayer.Get(info.Sender.ActorNumber);
			if (!sIPlayer.IsNull() && sIPlayer.authorityToClientRPCLimiter.CheckCallTime(Time.unscaledTime))
			{
				ProcessAuthorityToClientRPC(authorityToClientRPCEnum, data, info);
			}
		}
	}

	public void ProcessAuthorityToClientRPC(int authorityToClientRPCEnum, object[] data, PhotonMessageInfo info)
	{
		switch ((AuthorityToClientRPC)authorityToClientRPCEnum)
		{
		case AuthorityToClientRPC.CallEntityRPC:
		{
			if (data.Length != 2 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType2) || !GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType3))
			{
				break;
			}
			GameEntity gameEntityFromNetId = gameEntityManager.GetGameEntityFromNetId(dataAsType2);
			if ((bool)gameEntityFromNetId)
			{
				SIGadget component = gameEntityFromNetId.GetComponent<SIGadget>();
				if ((bool)component)
				{
					component.ProcessAuthorityToClientRPC(info, dataAsType3, null);
				}
			}
			break;
		}
		case AuthorityToClientRPC.CallEntityRPCData:
		{
			if (data.Length != 3 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType4) || !GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType5) || !GameEntityManager.ValidateDataType<object[]>(data[2], out var dataAsType6))
			{
				break;
			}
			GameEntity gameEntityFromNetId2 = gameEntityManager.GetGameEntityFromNetId(dataAsType4);
			if ((bool)gameEntityFromNetId2)
			{
				SIGadget component2 = gameEntityFromNetId2.GetComponent<SIGadget>();
				if ((bool)component2)
				{
					component2.ProcessAuthorityToClientRPC(info, dataAsType5, dataAsType6);
				}
			}
			break;
		}
		case AuthorityToClientRPC.TriggerMonkeIdolDepositCelebration:
		{
			if (data.Length == 1 && GameEntityManager.ValidateDataType<Vector3>(data[0], out var dataAsType) && (bool)SIPlayer.LocalPlayer)
			{
				SIPlayer.LocalPlayer.TriggerIdolDepositedCelebration(dataAsType);
			}
			break;
		}
		}
	}

	[PunRPC]
	public void SIClientToClientRPC(int clientToClientRPCEnum, object[] data, PhotonMessageInfo info)
	{
		if (data != null)
		{
			SIPlayer sIPlayer = SIPlayer.Get(info.Sender.ActorNumber);
			if (!sIPlayer.IsNull() && sIPlayer.clientToClientRPCLimiter.CheckCallTime(Time.unscaledTime))
			{
				ProcessClientToClientRPC(clientToClientRPCEnum, data, info);
			}
		}
	}

	public void ProcessClientToClientRPC(int clientToClientRPCEnum, object[] data, PhotonMessageInfo info)
	{
		switch ((ClientToClientRPC)clientToClientRPCEnum)
		{
		case ClientToClientRPC.BroadcastProgression:
		{
			SIPlayer sIPlayer = SIPlayer.Get(info.Sender.ActorNumber);
			if (!(sIPlayer == null) && data.Length == 8 && GameEntityManager.ValidateDataType<int[]>(data[0], out var dataAsType6) && GameEntityManager.ValidateDataType<int[]>(data[1], out var dataAsType7) && GameEntityManager.ValidateDataType<bool[][]>(data[2], out var dataAsType8) && GameEntityManager.ValidateDataType<int>(data[3], out var dataAsType9) && GameEntityManager.ValidateDataType<int>(data[4], out var dataAsType10) && GameEntityManager.ValidateDataType<int>(data[5], out var dataAsType11) && GameEntityManager.ValidateDataType<int[]>(data[6], out var dataAsType12) && GameEntityManager.ValidateDataType<int[]>(data[7], out var dataAsType13))
			{
				sIPlayer.UpdateProgression(dataAsType6, dataAsType7, dataAsType8, dataAsType9, dataAsType10, dataAsType11, dataAsType12, dataAsType13);
				if (zoneSuperInfection != null)
				{
					zoneSuperInfection.RefreshStations(info.Sender.ActorNumber);
				}
			}
			break;
		}
		case ClientToClientRPC.LaunchDashYoyo:
		{
			if (data.Length != 5 || SIPlayer.Get(info.Sender.ActorNumber) == null || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType14) || !GameEntityManager.ValidateDataType<Vector3>(data[1], out var dataAsType15) || !dataAsType15.IsValid(10000f) || !GameEntityManager.ValidateDataType<Vector3>(data[2], out var dataAsType16) || !dataAsType16.IsValid(10000f) || !GameEntityManager.ValidateDataType<Vector3>(data[3], out var dataAsType17) || !dataAsType17.IsValid(10000f) || !GameEntityManager.ValidateDataType<Quaternion>(data[4], out var dataAsType18) || !dataAsType18.IsValid())
			{
				break;
			}
			GameEntity gameEntityFromNetId3 = gameEntityManager.GetGameEntityFromNetId(dataAsType14);
			if (!(gameEntityFromNetId3 == null) && (gameEntityFromNetId3.heldByActorNumber == info.Sender.ActorNumber || gameEntityFromNetId3.snappedByActorNumber == info.Sender.ActorNumber))
			{
				SIGadgetDashYoyo component3 = gameEntityFromNetId3.GetComponent<SIGadgetDashYoyo>();
				if (!(component3 == null))
				{
					component3.RemoteThrowYoYoTarget(dataAsType15, dataAsType16, dataAsType17, dataAsType18);
				}
			}
			break;
		}
		case ClientToClientRPC.CallEntityRPC:
		{
			if (data.Length != 2 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType4) || !GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType5))
			{
				break;
			}
			GameEntity gameEntityFromNetId2 = gameEntityManager.GetGameEntityFromNetId(dataAsType4);
			if ((bool)gameEntityFromNetId2)
			{
				SIGadget component2 = gameEntityFromNetId2.GetComponent<SIGadget>();
				if ((bool)component2)
				{
					component2.ProcessClientToClientRPC(info, dataAsType5, null);
				}
			}
			break;
		}
		case ClientToClientRPC.CallEntityRPCData:
		{
			if (data.Length != 3 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType) || !GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType2) || !GameEntityManager.ValidateDataType<object[]>(data[2], out var dataAsType3))
			{
				break;
			}
			GameEntity gameEntityFromNetId = gameEntityManager.GetGameEntityFromNetId(dataAsType);
			if ((bool)gameEntityFromNetId)
			{
				SIGadget component = gameEntityFromNetId.GetComponent<SIGadget>();
				if ((bool)component)
				{
					component.ProcessClientToClientRPC(info, dataAsType2, dataAsType3);
				}
			}
			break;
		}
		}
	}

	[ContextMenu("Spawn Debug Object")]
	private void TestSpawnGadget()
	{
		testSpawner.Spawn(gameEntityManager);
	}

	public IEnumerable<GameEntity> GetFactoryItems()
	{
		return techTreeSO.SpawnableEntities;
	}

	private void OnEntityRemoved(GameEntity entity)
	{
		entity.TryGetComponent<SIGadget>(out var component);
		if (zoneSuperInfection != null && component != null)
		{
			zoneSuperInfection.RemoveGadget(component);
		}
		if (!(component == null))
		{
			SIPlayer sIPlayer = SIPlayer.Get((int)(entity.createData & 0xFFFFFFFFu));
			if (sIPlayer != null && sIPlayer.activePlayerGadgets.Contains(entity.GetNetId()))
			{
				Debug.Log($"GadgetDebug: removing gadget grom list {sIPlayer.gameObject.name} {entity.GetNetId()}");
				sIPlayer.activePlayerGadgets.Remove(entity.GetNetId());
			}
		}
	}

	public long ProcessMigratedGameEntityCreateData(GameEntity entity, long createData)
	{
		if (entity.GetComponent<SIGadget>() == null)
		{
			return createData;
		}
		return (createData >> 32 << 32) | SIPlayer.LocalPlayer.ActorNr;
	}

	public bool ValidateMigratedGameEntity(int netId, int entityTypeId, Vector3 position, Quaternion rotation, long createData, int actorNr)
	{
		GameObject gameObject = gameEntityManager.FactoryPrefabById(entityTypeId);
		if (gameObject == null)
		{
			return false;
		}
		if (gameObject.GetComponent<SIGadget>() == null)
		{
			return false;
		}
		SIPlayer sIPlayer = SIPlayer.Get(actorNr);
		if (sIPlayer == null)
		{
			return false;
		}
		SIPlayer sIPlayer2 = SIPlayer.Get((int)(createData & 0xFFFFFFFFu));
		if (sIPlayer != sIPlayer2)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < sIPlayer.activePlayerGadgets.Count; i++)
		{
			if (gameEntityManager.GetGameEntityFromNetId(sIPlayer.activePlayerGadgets[i])?.GetComponent<SIGadget>() != null)
			{
				num++;
			}
		}
		if (num > sIPlayer.totalGadgetLimit)
		{
			return false;
		}
		bool flag = false;
		for (int j = 0; j < sIPlayer.CurrentProgression.techTreeData.Length; j++)
		{
			if (flag)
			{
				break;
			}
			for (int k = 0; k < sIPlayer.CurrentProgression.techTreeData[j].Length; k++)
			{
				if (sIPlayer.CurrentProgression.techTreeData[j][k] && sIPlayer.progressionSORef.IsValidNode(j, k))
				{
					SITechTreeNode treeNode = sIPlayer.progressionSORef.GetTreeNode(j, k);
					if (treeNode != null && treeNode.IsDispensableGadget && treeNode.unlockedGadgetPrefab.gameObject.name.GetStaticHash() == entityTypeId)
					{
						flag = true;
						break;
					}
				}
			}
		}
		if (!flag)
		{
			return false;
		}
		return true;
	}

	public void ClearPlayerGadgets(SIPlayer siPlayer)
	{
		for (int num = siPlayer.activePlayerGadgets.Count - 1; num >= 0; num--)
		{
			if (num < siPlayer.activePlayerGadgets.Count && siPlayer.activePlayerGadgets[num] >= 0)
			{
				GameEntity gameEntityFromNetId = gameEntityManager.GetGameEntityFromNetId(siPlayer.activePlayerGadgets[num]);
				if (!(gameEntityFromNetId == null) && !(gameEntityFromNetId.id == GameEntityId.Invalid))
				{
					gameEntityManager.RequestDestroyItem(gameEntityFromNetId.id);
				}
			}
		}
		siPlayer.activePlayerGadgets.Clear();
	}
}
