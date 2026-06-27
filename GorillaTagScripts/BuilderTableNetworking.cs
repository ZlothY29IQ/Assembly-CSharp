using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaGameModes;
using GorillaTagScripts.Builder;
using Ionic.Zlib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace GorillaTagScripts;

public class BuilderTableNetworking : MonoBehaviourPunCallbacks, ITickSystemTick
{
	public class PlayerTableInitState
	{
		public Player player;

		public int numSerializedBytes;

		public int totalSerializedBytes;

		public byte[] serializedTableState;

		public byte[] chunk;

		public float waitForInitTimeRemaining;

		public float sendNextChunkTimeRemaining;

		public PlayerTableInitState()
		{
			serializedTableState = new byte[1048576];
			chunk = new byte[1000];
			Reset();
		}

		public void Reset()
		{
			player = null;
			numSerializedBytes = 0;
			totalSerializedBytes = 0;
		}
	}

	private enum RPC
	{
		PlayerEnterMaster,
		TableDataMaster,
		TableData,
		TableDataStart,
		PlacePieceMaster,
		PlacePiece,
		GrabPieceMaster,
		GrabPiece,
		DropPieceMaster,
		DropPiece,
		RequestFailed,
		PieceDropZone,
		CreatePiece,
		CreatePieceMaster,
		CreateShelfPieceMaster,
		RecyclePieceMaster,
		PlotClaimedMaster,
		ArmShelfCreated,
		ShelfSelection,
		ShelfSelectionMaster,
		SetFunctionalState,
		SetFunctionalStateMaster,
		RequestTerminalControl,
		SetTerminalDriver,
		LoadSharedBlocksMap,
		SharedTableEvent,
		Count
	}

	private enum SharedTableEventTypes
	{
		LOAD_STARTED,
		LOAD_FAILED,
		OUT_OF_BOUNDS,
		COUNT
	}

	public PhotonView tablePhotonView;

	private const int MAX_TABLE_BYTES = 1048576;

	private const int MAX_TABLE_CHUNK_BYTES = 1000;

	private const float DELAY_CLIENT_TABLE_CREATION_TIME = 1f;

	private const float SEND_INIT_DATA_COOLDOWN = 0f;

	private const int PIECE_SYNC_BYTES = 128;

	private BuilderTable currTable;

	private int nextLocalCommandId;

	private List<PlayerTableInitState> masterClientTableInit;

	private List<PlayerTableInitState> masterClientTableValidators;

	private PlayerTableInitState localClientTableInit;

	private PlayerTableInitState localValidationTable;

	[HideInInspector]
	public List<Player> armShelfRequests;

	private CallLimiter[] callLimiters;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		masterClientTableInit = new List<PlayerTableInitState>(10);
		masterClientTableValidators = new List<PlayerTableInitState>(10);
		localClientTableInit = new PlayerTableInitState();
		localValidationTable = new PlayerTableInitState();
		callLimiters = new CallLimiter[26];
		callLimiters[0] = new CallLimiter(20, 30f);
		callLimiters[1] = new CallLimiter(200, 1f);
		callLimiters[2] = new CallLimiter(50, 1f);
		callLimiters[3] = new CallLimiter(2, 1f);
		callLimiters[4] = new CallLimiter(50, 1f);
		callLimiters[5] = new CallLimiter(50, 1f);
		callLimiters[6] = new CallLimiter(50, 1f);
		callLimiters[7] = new CallLimiter(50, 1f);
		callLimiters[8] = new CallLimiter(50, 1f);
		callLimiters[9] = new CallLimiter(50, 1f);
		callLimiters[10] = new CallLimiter(50, 1f);
		callLimiters[11] = new CallLimiter(50, 1f);
		callLimiters[12] = new CallLimiter(50, 1f);
		callLimiters[13] = new CallLimiter(50, 1f);
		callLimiters[14] = new CallLimiter(100, 1f);
		callLimiters[15] = new CallLimiter(100, 1f);
		callLimiters[16] = new CallLimiter(50, 1f);
		callLimiters[17] = new CallLimiter(50, 1f);
		callLimiters[18] = new CallLimiter(50, 1f);
		callLimiters[19] = new CallLimiter(50, 1f);
		callLimiters[20] = new CallLimiter(50, 1f);
		callLimiters[21] = new CallLimiter(50, 1f);
		callLimiters[22] = new CallLimiter(20, 1f);
		callLimiters[23] = new CallLimiter(20, 1f);
		callLimiters[24] = new CallLimiter(1, 1f);
		callLimiters[25] = new CallLimiter(10, 1f);
		armShelfRequests = new List<Player>(10);
	}

	private new void OnEnable()
	{
		base.OnEnable();
		TickSystem<object>.AddTickCallback(this);
	}

	private new void OnDisable()
	{
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void SetTable(BuilderTable table)
	{
		currTable = table;
	}

	private BuilderTable GetTable()
	{
		return currTable;
	}

	private int CreateLocalCommandId()
	{
		int result = nextLocalCommandId;
		nextLocalCommandId++;
		return result;
	}

	public PlayerTableInitState GetLocalTableInit()
	{
		return localClientTableInit;
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		if (newMasterClient.IsLocal)
		{
			masterClientTableInit.Clear();
			localClientTableInit.Reset();
			BuilderTable table = GetTable();
			bool flag = RoomSystem.WasRoomPrivate || table.IsInBuilderZone();
			BuilderTable.TableState tableState = table.GetTableState();
			bool flag2 = (tableState != BuilderTable.TableState.Ready && tableState != BuilderTable.TableState.WaitingForZoneAndRoom && tableState != BuilderTable.TableState.WaitForMasterResync && tableState != BuilderTable.TableState.ReceivingMasterResync) || table.pieces.Count <= 0 || !flag;
			if (!flag2)
			{
				flag2 |= table.pieces.Count <= 0;
			}
			if (flag2)
			{
				table.ClearTable();
				table.ClearQueuedCommands();
				table.SetTableState(flag ? BuilderTable.TableState.WaitForInitialBuildMaster : BuilderTable.TableState.WaitingForZoneAndRoom);
				return;
			}
			for (int i = 0; i < table.pieces.Count; i++)
			{
				BuilderPiece builderPiece = table.pieces[i];
				Player player = PhotonNetwork.CurrentRoom.GetPlayer(builderPiece.heldByPlayerActorNumber);
				if (table.pieces[i].state == BuilderPiece.State.Grabbed && player == null)
				{
					Vector3 position = builderPiece.transform.position;
					Quaternion rotation = builderPiece.transform.rotation;
					Debug.LogErrorFormat("We have a piece {0} {1} held by an invalid player {2} dropping", builderPiece.name, builderPiece.pieceId, builderPiece.heldByPlayerActorNumber);
					CreateLocalCommandId();
					builderPiece.ClearParentHeld();
					builderPiece.ClearParentPiece();
					builderPiece.transform.localScale = Vector3.one;
					builderPiece.SetState(BuilderPiece.State.Dropped);
					builderPiece.transform.SetLocalPositionAndRotation(position, rotation);
					if (builderPiece.rigidBody != null)
					{
						builderPiece.rigidBody.position = position;
						builderPiece.rigidBody.rotation = rotation;
						builderPiece.rigidBody.linearVelocity = Vector3.zero;
						builderPiece.rigidBody.angularVelocity = Vector3.zero;
					}
				}
			}
			table.ClearQueuedCommands();
			table.SetTableState(BuilderTable.TableState.Ready);
			return;
		}
		localClientTableInit.Reset();
		BuilderTable table2 = GetTable();
		if (table2.GetTableState() != BuilderTable.TableState.WaitingForZoneAndRoom)
		{
			if (table2.GetTableState() == BuilderTable.TableState.Ready)
			{
				table2.SetTableState(BuilderTable.TableState.WaitForMasterResync);
			}
			else if (table2.GetTableState() == BuilderTable.TableState.WaitForMasterResync || table2.GetTableState() == BuilderTable.TableState.ReceivingMasterResync)
			{
				table2.SetTableState(BuilderTable.TableState.WaitForMasterResync);
			}
			else
			{
				table2.SetTableState(BuilderTable.TableState.WaitingForInitalBuild);
			}
			PlayerEnterBuilder();
		}
	}

	public override void OnPlayerLeftRoom(Player player)
	{
		BuilderTable table = GetTable();
		if (table.GetTableState() != BuilderTable.TableState.WaitingForZoneAndRoom)
		{
			if (table.isTableMutable)
			{
				if (!PhotonNetwork.IsMasterClient)
				{
					table.DropAllPiecesForPlayerLeaving(player.ActorNumber);
				}
				else
				{
					table.RecycleAllPiecesForPlayerLeaving(player.ActorNumber);
				}
			}
			table.PlayerLeftRoom(player.ActorNumber);
		}
		if (!table.isTableMutable && table.linkedTerminal != null && table.linkedTerminal.IsPlayerDriver(player))
		{
			table.linkedTerminal.ResetTerminalControl();
			if (NetworkSystem.Instance.IsMasterClient)
			{
				base.photonView.RPC("SetBlocksTerminalDriverRPC", RpcTarget.All, -2);
			}
		}
		if (PhotonNetwork.IsMasterClient)
		{
			table.RemoveArmShelfForPlayer(player);
			table.VerifySetSelections();
			if (player != PhotonNetwork.LocalPlayer)
			{
				DestroyPlayerTableInit(player);
			}
		}
	}

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		BuilderTable table = GetTable();
		table.SetPendingMap(null);
		table.SetInRoom(inRoom: true);
	}

	public override void OnLeftRoom()
	{
		PlayerExitBuilder();
		BuilderTable table = GetTable();
		table.SetPendingMap(null);
		table.SetInRoom(inRoom: false);
		armShelfRequests.Clear();
	}

	public void Tick()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			UpdateNewPlayerInit();
		}
	}

	public void PlayerEnterBuilder()
	{
		tablePhotonView.RPC("PlayerEnterBuilderRPC", PhotonNetwork.MasterClient, PhotonNetwork.LocalPlayer, true);
		if (GameMode.ActiveGameMode is GorillaGuardianManager { isPlaying: not false } gorillaGuardianManager && gorillaGuardianManager.IsPlayerGuardian(NetworkSystem.Instance.LocalPlayer))
		{
			gorillaGuardianManager.RequestEjectGuardian(NetworkSystem.Instance.LocalPlayer);
		}
	}

	[PunRPC]
	public void PlayerEnterBuilderRPC(Player player, bool entered, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PlayerEnterBuilderRPC");
		if (!PhotonNetwork.IsMasterClient || !ValidateCallLimits(RPC.PlayerEnterMaster, info) || player == null || !player.Equals(info.Sender))
		{
			return;
		}
		BuilderTable table = GetTable();
		if (entered)
		{
			BuilderTable.TableState tableState = table.GetTableState();
			if (tableState == BuilderTable.TableState.WaitingForInitalBuild || (IsPrivateMasterClient() && tableState == BuilderTable.TableState.WaitingForZoneAndRoom))
			{
				table.SetTableState(BuilderTable.TableState.WaitForInitialBuildMaster);
			}
			if (player != PhotonNetwork.LocalPlayer)
			{
				CreateSerializedTableForNewPlayerInit(player);
			}
			if (table.isTableMutable)
			{
				RequestCreateArmShelfForPlayer(player);
			}
			else if (table.linkedTerminal != null)
			{
				base.photonView.RPC("SetBlocksTerminalDriverRPC", player, table.linkedTerminal.GetDriverID);
			}
		}
		else
		{
			if (player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
			{
				DestroyPlayerTableInit(player);
			}
			if (table.isTableMutable)
			{
				table.RemoveArmShelfForPlayer(player);
			}
		}
	}

	public void PlayerExitBuilder()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			tablePhotonView.RPC("PlayerEnterBuilderRPC", PhotonNetwork.MasterClient, PhotonNetwork.LocalPlayer, false);
		}
		BuilderTable table = GetTable();
		table.ClearTable();
		table.ClearQueuedCommands();
		localClientTableInit.Reset();
		armShelfRequests.Clear();
		masterClientTableInit.Clear();
	}

	public bool IsPrivateMasterClient()
	{
		if (PhotonNetwork.LocalPlayer == PhotonNetwork.MasterClient)
		{
			return NetworkSystem.Instance.SessionIsPrivate;
		}
		return false;
	}

	private void UpdateNewPlayerInit()
	{
		if (GetTable().GetTableState() != BuilderTable.TableState.Ready)
		{
			return;
		}
		for (int i = 0; i < masterClientTableInit.Count; i++)
		{
			if (masterClientTableInit[i].waitForInitTimeRemaining >= 0f)
			{
				masterClientTableInit[i].waitForInitTimeRemaining -= Time.deltaTime;
				if (masterClientTableInit[i].waitForInitTimeRemaining <= 0f)
				{
					StartCreatingSerializedTable(masterClientTableInit[i].player);
					masterClientTableInit[i].waitForInitTimeRemaining = -1f;
					masterClientTableInit[i].sendNextChunkTimeRemaining = 0f;
				}
			}
			else
			{
				if (!(masterClientTableInit[i].sendNextChunkTimeRemaining >= 0f))
				{
					continue;
				}
				masterClientTableInit[i].sendNextChunkTimeRemaining -= Time.deltaTime;
				if (masterClientTableInit[i].sendNextChunkTimeRemaining <= 0f)
				{
					SendNextTableData(masterClientTableInit[i].player);
					if (masterClientTableInit[i].numSerializedBytes < masterClientTableInit[i].totalSerializedBytes)
					{
						masterClientTableInit[i].sendNextChunkTimeRemaining = 0f;
					}
					else
					{
						masterClientTableInit[i].sendNextChunkTimeRemaining = -1f;
					}
				}
			}
		}
	}

	private void StartCreatingSerializedTable(Player newPlayer)
	{
		BuilderTable table = GetTable();
		PlayerTableInitState playerTableInit = GetPlayerTableInit(newPlayer);
		playerTableInit.totalSerializedBytes = table.SerializeTableState(playerTableInit.serializedTableState, 1048576);
		byte[] array = GZipStream.CompressBuffer(playerTableInit.serializedTableState);
		playerTableInit.totalSerializedBytes = array.Length;
		Array.Copy(array, 0, playerTableInit.serializedTableState, 0, playerTableInit.totalSerializedBytes);
		playerTableInit.numSerializedBytes = 0;
		tablePhotonView.RPC("StartBuildTableRPC", newPlayer, playerTableInit.totalSerializedBytes);
	}

	[PunRPC]
	public void StartBuildTableRPC(int totalBytes, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "StartBuildTableRPC");
		if (!info.Sender.IsMasterClient || PhotonNetwork.IsMasterClient || !ValidateCallLimits(RPC.TableDataStart, info) || totalBytes <= 0 || totalBytes > 1048576)
		{
			return;
		}
		BuilderTable table = GetTable();
		if (!table.IsInBuilderZone())
		{
			return;
		}
		GTDev.Log("StartBuildTableRPC with current state " + table.GetTableState());
		if (table.GetTableState() == BuilderTable.TableState.WaitForMasterResync || table.GetTableState() == BuilderTable.TableState.WaitingForInitalBuild)
		{
			if (table.GetTableState() == BuilderTable.TableState.WaitForMasterResync)
			{
				table.SetTableState(BuilderTable.TableState.ReceivingMasterResync);
			}
			else
			{
				table.SetTableState(BuilderTable.TableState.ReceivingInitialBuild);
			}
			localClientTableInit.Reset();
			PlayerTableInitState playerTableInitState = localClientTableInit;
			playerTableInitState.player = PhotonNetwork.LocalPlayer;
			playerTableInitState.totalSerializedBytes = totalBytes;
			table.ClearQueuedCommands();
		}
	}

	private void SendNextTableData(Player requestingPlayer)
	{
		PlayerTableInitState playerTableInit = GetPlayerTableInit(requestingPlayer);
		if (playerTableInit == null)
		{
			Debug.LogErrorFormat("No Table init found for player {0}", requestingPlayer.ActorNumber);
			return;
		}
		int num = Mathf.Min(1000, playerTableInit.totalSerializedBytes - playerTableInit.numSerializedBytes);
		if (num > 0)
		{
			Array.Copy(playerTableInit.serializedTableState, playerTableInit.numSerializedBytes, playerTableInit.chunk, 0, num);
			playerTableInit.numSerializedBytes += num;
			tablePhotonView.RPC("SendTableDataRPC", requestingPlayer, num, playerTableInit.chunk);
		}
	}

	[PunRPC]
	public void SendTableDataRPC(int numBytes, byte[] bytes, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SendTableDataRPC");
		if (!info.Sender.IsMasterClient || localClientTableInit.player == null)
		{
			return;
		}
		if (numBytes <= 0 || numBytes > 1000 || numBytes > bytes.Length)
		{
			Debug.LogErrorFormat("Builder Table Send Data numBytes is too large {0}", numBytes);
		}
		else
		{
			if (bytes.Length > 1000 || PhotonNetwork.IsMasterClient || !ValidateCallLimits(RPC.TableData, info))
			{
				return;
			}
			PlayerTableInitState playerTableInitState = localClientTableInit;
			if (playerTableInitState.numSerializedBytes + numBytes > 1048576)
			{
				Debug.LogErrorFormat("Builder Table serialized bytes is larger than buffer {0}", playerTableInitState.numSerializedBytes + numBytes);
				return;
			}
			Array.Copy(bytes, 0, playerTableInitState.serializedTableState, playerTableInitState.numSerializedBytes, numBytes);
			playerTableInitState.numSerializedBytes += numBytes;
			if (playerTableInitState.numSerializedBytes >= playerTableInitState.totalSerializedBytes)
			{
				GetTable().SetTableState(BuilderTable.TableState.InitialBuild);
			}
		}
	}

	private bool DoesTableInitExist(Player player)
	{
		for (int i = 0; i < masterClientTableInit.Count; i++)
		{
			if (masterClientTableInit[i].player.ActorNumber == player.ActorNumber)
			{
				return true;
			}
		}
		return false;
	}

	private PlayerTableInitState CreatePlayerTableInit(Player player)
	{
		for (int i = 0; i < masterClientTableInit.Count; i++)
		{
			if (masterClientTableInit[i].player.ActorNumber == player.ActorNumber)
			{
				masterClientTableInit[i].Reset();
				return masterClientTableInit[i];
			}
		}
		PlayerTableInitState playerTableInitState = new PlayerTableInitState();
		playerTableInitState.player = player;
		masterClientTableInit.Add(playerTableInitState);
		return playerTableInitState;
	}

	public void ResetSerializedTableForAllPlayers()
	{
		for (int i = 0; i < masterClientTableInit.Count; i++)
		{
			masterClientTableInit[i].waitForInitTimeRemaining = 1f;
			masterClientTableInit[i].sendNextChunkTimeRemaining = -1f;
			masterClientTableInit[i].numSerializedBytes = 0;
			masterClientTableInit[i].totalSerializedBytes = 0;
		}
	}

	private void CreateSerializedTableForNewPlayerInit(Player newPlayer)
	{
		if (!DoesTableInitExist(newPlayer))
		{
			PlayerTableInitState playerTableInitState = CreatePlayerTableInit(newPlayer);
			playerTableInitState.waitForInitTimeRemaining = 1f;
			playerTableInitState.sendNextChunkTimeRemaining = -1f;
		}
	}

	private void DestroyPlayerTableInit(Player player)
	{
		for (int i = 0; i < masterClientTableInit.Count; i++)
		{
			if (masterClientTableInit[i].player.ActorNumber == player.ActorNumber)
			{
				masterClientTableInit.RemoveAt(i);
				i--;
			}
		}
	}

	private PlayerTableInitState GetPlayerTableInit(Player player)
	{
		for (int i = 0; i < masterClientTableInit.Count; i++)
		{
			if (masterClientTableInit[i].player.ActorNumber == player.ActorNumber)
			{
				return masterClientTableInit[i];
			}
		}
		return null;
	}

	private bool ValidateMasterClientIsReady(Player player)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return false;
		}
		if (player != null && !player.IsMasterClient)
		{
			PlayerTableInitState playerTableInit = GetPlayerTableInit(player);
			if (playerTableInit != null && playerTableInit.numSerializedBytes < playerTableInit.totalSerializedBytes)
			{
				return false;
			}
		}
		if (GetTable().GetTableState() != BuilderTable.TableState.Ready)
		{
			return false;
		}
		return true;
	}

	private bool ValidateCallLimits(RPC rpcCall, PhotonMessageInfo info)
	{
		if (rpcCall < RPC.PlayerEnterMaster || rpcCall >= RPC.Count)
		{
			return false;
		}
		return callLimiters[(int)rpcCall].CheckCallTime(Time.time);
	}

	[PunRPC]
	public void RequestFailedRPC(int localCommandId, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestFailedRPC");
		if (info.Sender.IsMasterClient && ValidateCallLimits(RPC.RequestFailed, info))
		{
			GetTable().RollbackFailedCommand(localCommandId);
		}
	}

	public void RequestCreatePiece(int newPieceType, Vector3 position, Quaternion rotation, int materialType)
	{
	}

	public void RequestCreatePieceRPC(int newPieceType, long packedPosition, int packedRotation, int materialType, PhotonMessageInfo info)
	{
	}

	public void PieceCreatedRPC(int pieceType, int pieceId, long packedPosition, int packedRotation, int materialType, Player creatingPlayer, PhotonMessageInfo info)
	{
	}

	public void CreateShelfPiece(int pieceType, Vector3 position, Quaternion rotation, int materialType, BuilderPiece.State state, int shelfID)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		BuilderTable table = GetTable();
		if (!table.isTableMutable || table.GetTableState() != BuilderTable.TableState.Ready)
		{
			return;
		}
		BuilderPiece piecePrefab = table.GetPiecePrefab(pieceType);
		if (!table.HasEnoughResources(piecePrefab))
		{
			Debug.Log("Not Enough Resources");
			return;
		}
		switch (state)
		{
		default:
			return;
		case BuilderPiece.State.OnShelf:
			if (shelfID < 0 || shelfID >= table.dispenserShelves.Count)
			{
				return;
			}
			break;
		case BuilderPiece.State.OnConveyor:
			if (shelfID < 0 || shelfID >= table.conveyors.Count)
			{
				return;
			}
			break;
		}
		int num = table.CreatePieceId();
		long num2 = BitPackUtils.PackWorldPosForNetwork(position);
		int num3 = BitPackUtils.PackQuaternionForNetwork(rotation);
		base.photonView.RPC("PieceCreatedByShelfRPC", RpcTarget.All, pieceType, num, num2, num3, materialType, (byte)state, shelfID, PhotonNetwork.LocalPlayer);
	}

	[PunRPC]
	public void PieceCreatedByShelfRPC(int pieceType, int pieceId, long packedPosition, int packedRotation, int materialType, byte state, int shelfID, Player creatingPlayer, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		BuilderTable table = GetTable();
		if ((!table.IsInBuilderZone() && !info.Sender.IsLocal) || !ValidateCallLimits(RPC.CreateShelfPieceMaster, info) || !table.isTableMutable)
		{
			return;
		}
		Vector3 position = BitPackUtils.UnpackWorldPosFromNetwork(packedPosition);
		Quaternion rotation = BitPackUtils.UnpackQuaternionFromNetwork(packedRotation);
		if (table.ValidatePieceWorldTransform(position, rotation))
		{
			switch ((BuilderPiece.State)state)
			{
			case BuilderPiece.State.OnShelf:
				table.CreateDispenserShelfPiece(pieceType, pieceId, position, rotation, materialType, shelfID);
				break;
			case BuilderPiece.State.OnConveyor:
				table.CreateConveyorPiece(pieceType, pieceId, position, rotation, materialType, shelfID, info.SentServerTimestamp);
				break;
			}
		}
	}

	public void RequestRecyclePiece(int pieceId, Vector3 position, Quaternion rotation, bool playFX, int recyclerID)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			BuilderTable table = GetTable();
			if (table.GetTableState() == BuilderTable.TableState.Ready && table.isTableMutable && position.IsValid(10000f) && rotation.IsValid() && recyclerID <= 32767 && recyclerID >= -1)
			{
				long num = BitPackUtils.PackWorldPosForNetwork(position);
				int num2 = BitPackUtils.PackQuaternionForNetwork(rotation);
				base.photonView.RPC("PieceDestroyedRPC", RpcTarget.All, pieceId, num, num2, playFX, (short)recyclerID);
			}
		}
	}

	[PunRPC]
	public void PieceDestroyedRPC(int pieceId, long packedPosition, int packedRotation, bool playFX, short recyclerID, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PieceDestroyedRPC");
		if (!info.Sender.IsMasterClient || !ValidateCallLimits(RPC.RecyclePieceMaster, info))
		{
			return;
		}
		BuilderTable table = GetTable();
		if ((table.IsInBuilderZone() || info.Sender.IsLocal) && table.isTableMutable)
		{
			Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork(packedPosition);
			Quaternion q = BitPackUtils.UnpackQuaternionFromNetwork(packedRotation);
			if (v.IsValid(10000f) && q.IsValid())
			{
				table.RecyclePiece(pieceId, v, q, playFX, recyclerID, info.Sender);
			}
		}
	}

	public void RequestPlacePiece(BuilderPiece piece, BuilderPiece attachPiece, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, BuilderPiece parentPiece, int attachIndex, int parentAttachIndex)
	{
		if (piece == null)
		{
			return;
		}
		int pieceId = piece.pieceId;
		int num = ((parentPiece != null) ? parentPiece.pieceId : (-1));
		int num2 = ((attachPiece != null) ? attachPiece.pieceId : (-1));
		BuilderTable table = GetTable();
		if (table.isTableMutable && table.ValidatePlacePieceParams(pieceId, num2, bumpOffsetX, bumpOffsetZ, twist, num, attachIndex, parentAttachIndex, NetPlayer.Get(PhotonNetwork.LocalPlayer)))
		{
			int num3 = CreateLocalCommandId();
			attachPiece.requestedParentPiece = parentPiece;
			table.UpdatePieceData(attachPiece);
			table.PlacePiece(num3, pieceId, num2, bumpOffsetX, bumpOffsetZ, twist, num, attachIndex, parentAttachIndex, NetPlayer.Get(PhotonNetwork.LocalPlayer), PhotonNetwork.ServerTimestamp, force: true);
			int num4 = BuilderTable.PackPiecePlacement(twist, bumpOffsetX, bumpOffsetZ);
			if (table.GetTableState() == BuilderTable.TableState.Ready)
			{
				base.photonView.RPC("RequestPlacePieceRPC", RpcTarget.MasterClient, num3, pieceId, num2, num4, num, attachIndex, parentAttachIndex, PhotonNetwork.LocalPlayer);
			}
		}
	}

	[PunRPC]
	public void RequestPlacePieceRPC(int localCommandId, int pieceId, int attachPieceId, int placement, int parentPieceId, int attachIndex, int parentAttachIndex, Player placedByPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestPlacePieceRPC");
		if (!PhotonNetwork.IsMasterClient || !ValidateMasterClientIsReady(info.Sender) || !ValidateCallLimits(RPC.PlacePieceMaster, info) || placedByPlayer == null || !placedByPlayer.Equals(info.Sender))
		{
			return;
		}
		BuilderTable table = GetTable();
		if ((!RoomSystem.WasRoomPrivate && !table.IsInBuilderZone()) || !table.isTableMutable)
		{
			return;
		}
		bool isMasterClient = info.Sender.IsMasterClient;
		BuilderTable.UnpackPiecePlacement(placement, out var twist, out var xOffset, out var zOffset);
		bool flag = isMasterClient || table.ValidatePlacePieceParams(pieceId, attachPieceId, xOffset, zOffset, twist, parentPieceId, attachIndex, parentAttachIndex, NetPlayer.Get(placedByPlayer));
		if (flag)
		{
			flag &= isMasterClient || table.ValidatePlacePieceState(pieceId, attachPieceId, xOffset, zOffset, twist, parentPieceId, attachIndex, parentAttachIndex, placedByPlayer);
		}
		if (flag)
		{
			BuilderPiece piece = table.GetPiece(parentPieceId);
			if (piece != null && piece.TryGetPlotComponent(out var plot) && !plot.IsPlotClaimed())
			{
				base.photonView.RPC("PlotClaimedRPC", RpcTarget.All, parentPieceId, placedByPlayer, true);
			}
			base.photonView.RPC("PiecePlacedRPC", RpcTarget.All, localCommandId, pieceId, attachPieceId, placement, parentPieceId, attachIndex, parentAttachIndex, placedByPlayer, info.SentServerTimestamp);
		}
		else
		{
			base.photonView.RPC("RequestFailedRPC", info.Sender, localCommandId);
		}
	}

	[PunRPC]
	public void PiecePlacedRPC(int localCommandId, int pieceId, int attachPieceId, int placement, int parentPieceId, int attachIndex, int parentAttachIndex, Player placedByPlayer, int timeStamp, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PiecePlacedRPC");
		if (!info.Sender.IsMasterClient || !ValidateCallLimits(RPC.PlacePiece, info))
		{
			return;
		}
		BuilderTable table = GetTable();
		if ((table.IsInBuilderZone() || info.Sender.IsLocal) && table.isTableMutable && placedByPlayer != null)
		{
			if ((uint)(PhotonNetwork.ServerTimestamp - info.SentServerTimestamp) > PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout || (uint)(info.SentServerTimestamp - timeStamp) > PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout)
			{
				timeStamp = PhotonNetwork.ServerTimestamp;
			}
			BuilderTable.UnpackPiecePlacement(placement, out var twist, out var xOffset, out var zOffset);
			table.PlacePiece(localCommandId, pieceId, attachPieceId, xOffset, zOffset, twist, parentPieceId, attachIndex, parentAttachIndex, NetPlayer.Get(placedByPlayer), timeStamp, force: false);
		}
	}

	public void RequestGrabPiece(BuilderPiece piece, bool isLefHand, Vector3 localPosition, Quaternion localRotation)
	{
		if (piece == null)
		{
			return;
		}
		BuilderTable table = GetTable();
		if (table.isTableMutable && table.ValidateGrabPieceParams(piece.pieceId, isLefHand, localPosition, localRotation, NetPlayer.Get(PhotonNetwork.LocalPlayer)))
		{
			if (PhotonNetwork.IsMasterClient)
			{
				CheckForFreedPlot(piece.pieceId, PhotonNetwork.LocalPlayer);
			}
			int num = CreateLocalCommandId();
			table.GrabPiece(num, piece.pieceId, isLefHand, localPosition, localRotation, NetPlayer.Get(PhotonNetwork.LocalPlayer), force: true);
			if (table.GetTableState() == BuilderTable.TableState.Ready)
			{
				long num2 = BitPackUtils.PackHandPosRotForNetwork(localPosition, localRotation);
				base.photonView.RPC("RequestGrabPieceRPC", RpcTarget.MasterClient, num, piece.pieceId, isLefHand, num2, PhotonNetwork.LocalPlayer);
			}
		}
	}

	[PunRPC]
	public void RequestGrabPieceRPC(int localCommandId, int pieceId, bool isLeftHand, long packedPosRot, Player grabbedByPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestGrabPieceRPC");
		if (!PhotonNetwork.IsMasterClient || !ValidateMasterClientIsReady(info.Sender) || !ValidateCallLimits(RPC.GrabPieceMaster, info) || !grabbedByPlayer.Equals(info.Sender))
		{
			return;
		}
		BuilderTable table = GetTable();
		if ((!RoomSystem.WasRoomPrivate && !table.IsInBuilderZone()) || !table.isTableMutable)
		{
			return;
		}
		BitPackUtils.UnpackHandPosRotFromNetwork(packedPosRot, out var localPos, out var handRot);
		if (table.GetTableState() != BuilderTable.TableState.Ready)
		{
			return;
		}
		bool isMasterClient = info.Sender.IsMasterClient;
		bool flag = isMasterClient || table.ValidateGrabPieceParams(pieceId, isLeftHand, localPos, handRot, NetPlayer.Get(grabbedByPlayer));
		if (flag)
		{
			flag &= isMasterClient || table.ValidateGrabPieceState(pieceId, isLeftHand, localPos, handRot, grabbedByPlayer);
		}
		if (flag)
		{
			if (!info.Sender.IsMasterClient)
			{
				CheckForFreedPlot(pieceId, grabbedByPlayer);
			}
			base.photonView.RPC("PieceGrabbedRPC", RpcTarget.All, localCommandId, pieceId, isLeftHand, packedPosRot, grabbedByPlayer);
		}
		else
		{
			base.photonView.RPC("RequestFailedRPC", info.Sender, localCommandId);
		}
	}

	private void CheckForFreedPlot(int pieceId, Player grabbedByPlayer)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			BuilderPiece piece = GetTable().GetPiece(pieceId);
			if (piece != null && piece.parentPiece != null && piece.parentPiece.IsPrivatePlot() && piece.parentPiece.firstChildPiece.Equals(piece) && piece.nextSiblingPiece == null)
			{
				base.photonView.RPC("PlotClaimedRPC", RpcTarget.All, piece.parentPiece.pieceId, grabbedByPlayer, false);
			}
		}
	}

	[PunRPC]
	public void PieceGrabbedRPC(int localCommandId, int pieceId, bool isLeftHand, long packedPosRot, Player grabbedByPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PieceGrabbedRPC");
		if (info.Sender.IsMasterClient && ValidateCallLimits(RPC.GrabPiece, info))
		{
			BuilderTable table = GetTable();
			if ((table.IsInBuilderZone() || info.Sender.IsLocal) && table.isTableMutable)
			{
				BitPackUtils.UnpackHandPosRotFromNetwork(packedPosRot, out var localPos, out var handRot);
				table.GrabPiece(localCommandId, pieceId, isLeftHand, localPos, handRot, NetPlayer.Get(grabbedByPlayer), force: false);
			}
		}
	}

	public void RequestDropPiece(BuilderPiece piece, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity)
	{
		if (piece == null)
		{
			return;
		}
		int pieceId = piece.pieceId;
		if (velocity.IsValid(10000f) && velocity.sqrMagnitude > BuilderTable.MAX_DROP_VELOCITY * BuilderTable.MAX_DROP_VELOCITY)
		{
			velocity = velocity.normalized * BuilderTable.MAX_DROP_VELOCITY;
		}
		if (angVelocity.IsValid(10000f) && angVelocity.sqrMagnitude > BuilderTable.MAX_DROP_ANG_VELOCITY * BuilderTable.MAX_DROP_ANG_VELOCITY)
		{
			angVelocity = angVelocity.normalized * BuilderTable.MAX_DROP_ANG_VELOCITY;
		}
		BuilderTable table = GetTable();
		if (table.isTableMutable && table.ValidateDropPieceParams(pieceId, position, rotation, velocity, angVelocity, NetPlayer.Get(PhotonNetwork.LocalPlayer)))
		{
			int num = CreateLocalCommandId();
			table.DropPiece(num, pieceId, position, rotation, velocity, angVelocity, NetPlayer.Get(PhotonNetwork.LocalPlayer), force: true);
			if (table.GetTableState() == BuilderTable.TableState.Ready)
			{
				base.photonView.RPC("RequestDropPieceRPC", RpcTarget.MasterClient, num, pieceId, position, rotation, velocity, angVelocity, PhotonNetwork.LocalPlayer);
			}
		}
	}

	[PunRPC]
	public void RequestDropPieceRPC(int localCommandId, int pieceId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, Player droppedByPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestDropPieceRPC");
		if (!PhotonNetwork.IsMasterClient || !ValidateMasterClientIsReady(info.Sender) || !ValidateCallLimits(RPC.DropPieceMaster, info) || !droppedByPlayer.Equals(info.Sender))
		{
			return;
		}
		BuilderTable table = GetTable();
		if ((RoomSystem.WasRoomPrivate || table.IsInBuilderZone()) && table.isTableMutable && table.GetTableState() == BuilderTable.TableState.Ready)
		{
			bool isMasterClient = info.Sender.IsMasterClient;
			bool flag = isMasterClient || table.ValidateDropPieceParams(pieceId, position, rotation, velocity, angVelocity, NetPlayer.Get(droppedByPlayer));
			if (flag)
			{
				flag &= isMasterClient || table.ValidateDropPieceState(pieceId, position, rotation, velocity, angVelocity, droppedByPlayer);
			}
			if (flag)
			{
				base.photonView.RPC("PieceDroppedRPC", RpcTarget.All, localCommandId, pieceId, position, rotation, velocity, angVelocity, droppedByPlayer);
			}
			else
			{
				base.photonView.RPC("RequestFailedRPC", info.Sender, localCommandId);
			}
		}
	}

	[PunRPC]
	public void PieceDroppedRPC(int localCommandId, int pieceId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, Player droppedByPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PieceDroppedRPC");
		if (info.Sender.IsMasterClient && ValidateCallLimits(RPC.DropPiece, info) && position.IsValid(10000f) && rotation.IsValid() && velocity.IsValid(10000f) && angVelocity.IsValid(10000f))
		{
			BuilderTable table = GetTable();
			if ((table.IsInBuilderZone() || info.Sender.IsLocal) && table.isTableMutable)
			{
				table.DropPiece(localCommandId, pieceId, position, rotation, velocity, angVelocity, NetPlayer.Get(droppedByPlayer), force: false);
			}
		}
	}

	public void PieceEnteredDropZone(BuilderPiece piece, BuilderDropZone.DropType dropType, int dropZoneId)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		BuilderTable table = GetTable();
		if (table.isTableMutable)
		{
			BuilderPiece rootPiece = piece.GetRootPiece();
			if (table.ValidateRepelPiece(rootPiece))
			{
				long num = BitPackUtils.PackWorldPosForNetwork(rootPiece.transform.position);
				int num2 = BitPackUtils.PackQuaternionForNetwork(rootPiece.transform.rotation);
				base.photonView.RPC("PieceEnteredDropZoneRPC", RpcTarget.All, rootPiece.pieceId, num, num2, dropZoneId);
			}
		}
	}

	[PunRPC]
	public void PieceEnteredDropZoneRPC(int pieceId, long position, int rotation, int dropZoneId, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PieceEnteredDropZoneRPC");
		if (!info.Sender.IsMasterClient || !ValidateCallLimits(RPC.PieceDropZone, info))
		{
			return;
		}
		Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork(position);
		if (!v.IsValid(10000f))
		{
			return;
		}
		Quaternion q = BitPackUtils.UnpackQuaternionFromNetwork(rotation);
		if (q.IsValid())
		{
			BuilderTable table = GetTable();
			if ((table.IsInBuilderZone() || info.Sender.IsLocal) && table.isTableMutable)
			{
				table.PieceEnteredDropZone(pieceId, v, q, dropZoneId);
			}
		}
	}

	[PunRPC]
	public void PlotClaimedRPC(int pieceId, Player claimingPlayer, bool claimed, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PlotClaimedRPC");
		if (!info.Sender.IsMasterClient || !ValidateCallLimits(RPC.PlotClaimedMaster, info))
		{
			return;
		}
		BuilderTable table = GetTable();
		if (table.isTableMutable)
		{
			if (claimed)
			{
				table.PlotClaimed(pieceId, claimingPlayer);
			}
			else
			{
				table.PlotFreed(pieceId, claimingPlayer);
			}
		}
	}

	public void RequestCreateArmShelfForPlayer(Player player)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		BuilderTable table = GetTable();
		if (!table.isTableMutable)
		{
			return;
		}
		if (table.GetTableState() != BuilderTable.TableState.Ready)
		{
			if (!armShelfRequests.Contains(player))
			{
				armShelfRequests.Add(player);
			}
		}
		else if (!table.playerToArmShelfLeft.ContainsKey(player.ActorNumber))
		{
			int num = table.CreatePieceId();
			int num2 = table.CreatePieceId();
			int staticHash = table.armShelfPieceType.name.GetStaticHash();
			base.photonView.RPC("ArmShelfCreatedRPC", RpcTarget.All, num, num2, staticHash, player);
		}
	}

	[PunRPC]
	public void ArmShelfCreatedRPC(int pieceIdLeft, int pieceIdRight, int pieceType, Player owningPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ArmShelfCreatedRPC");
		if (info.Sender.IsMasterClient && ValidateCallLimits(RPC.ArmShelfCreated, info))
		{
			BuilderTable table = GetTable();
			if ((table.IsInBuilderZone() || info.Sender.IsLocal) && table.isTableMutable && pieceType == table.armShelfPieceType.name.GetStaticHash())
			{
				table.CreateArmShelf(pieceIdLeft, pieceIdRight, pieceType, owningPlayer);
			}
		}
	}

	public void RequestShelfSelection(int shelfID, int groupID, bool isConveyor)
	{
		BuilderTable table = GetTable();
		if (!table.isTableMutable)
		{
			return;
		}
		if (isConveyor)
		{
			if (shelfID < 0 || shelfID >= table.conveyors.Count)
			{
				return;
			}
		}
		else if (shelfID < 0 || shelfID >= table.dispenserShelves.Count)
		{
			return;
		}
		if (table.GetTableState() == BuilderTable.TableState.Ready)
		{
			base.photonView.RPC("RequestShelfSelectionRPC", RpcTarget.MasterClient, shelfID, groupID, isConveyor);
		}
	}

	[PunRPC]
	public void RequestShelfSelectionRPC(int shelfId, int setId, bool isConveyor, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestShelfSelectionRPC");
		if (PhotonNetwork.IsMasterClient && ValidateCallLimits(RPC.ShelfSelection, info) && ValidateMasterClientIsReady(info.Sender))
		{
			BuilderTable table = GetTable();
			if ((RoomSystem.WasRoomPrivate || table.IsInBuilderZone()) && table.isTableMutable && table.ValidateShelfSelectionParams(shelfId, setId, isConveyor, info.Sender))
			{
				base.photonView.RPC("ShelfSelectionChangedRPC", RpcTarget.All, shelfId, setId, isConveyor, info.Sender);
			}
		}
	}

	[PunRPC]
	public void ShelfSelectionChangedRPC(int shelfId, int setId, bool isConveyor, Player caller, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ShelfSelectionChangedRPC");
		if (info.Sender.IsMasterClient && ValidateCallLimits(RPC.ShelfSelectionMaster, info))
		{
			BuilderTable table = GetTable();
			if ((table.IsInBuilderZone() || info.Sender.IsLocal) && table.isTableMutable && shelfId >= 0 && ((isConveyor && shelfId < table.conveyors.Count) || (!isConveyor && shelfId < table.dispenserShelves.Count)))
			{
				table.ChangeSetSelection(shelfId, setId, isConveyor);
			}
		}
	}

	public void RequestFunctionalPieceStateChange(int pieceID, byte state)
	{
		BuilderTable table = GetTable();
		if (table.ValidateFunctionalPieceState(pieceID, state, NetworkSystem.Instance.LocalPlayer) && table.GetTableState() == BuilderTable.TableState.Ready)
		{
			base.photonView.RPC("RequestFunctionalPieceStateChangeRPC", RpcTarget.MasterClient, pieceID, state);
		}
	}

	[PunRPC]
	public void RequestFunctionalPieceStateChangeRPC(int pieceID, byte state, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestFunctionalPieceStateChangeRPC");
		if (PhotonNetwork.IsMasterClient && ValidateMasterClientIsReady(info.Sender) && ValidateCallLimits(RPC.SetFunctionalState, info))
		{
			BuilderTable table = GetTable();
			if ((RoomSystem.WasRoomPrivate || table.IsInBuilderZone()) && table.GetTableState() == BuilderTable.TableState.Ready && table.ValidateFunctionalPieceState(pieceID, state, NetPlayer.Get(info.Sender)))
			{
				table.OnFunctionalStateRequest(pieceID, state, NetPlayer.Get(info.Sender), info.SentServerTimestamp);
			}
		}
	}

	public void FunctionalPieceStateChangeMaster(int pieceID, byte state, Player instigator, int timeStamp)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			BuilderTable table = GetTable();
			if (table.ValidateFunctionalPieceState(pieceID, state, NetPlayer.Get(instigator)) && state != table.GetPiece(pieceID).functionalPieceState)
			{
				base.photonView.RPC("FunctionalPieceStateChangeRPC", RpcTarget.All, pieceID, state, instigator, timeStamp);
			}
		}
	}

	[PunRPC]
	public void FunctionalPieceStateChangeRPC(int pieceID, byte state, Player caller, int timeStamp, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "FunctionalPieceStateChangeRPC");
		if (info.Sender.IsMasterClient && ValidateCallLimits(RPC.SetFunctionalStateMaster, info) && caller != null)
		{
			if ((uint)(PhotonNetwork.ServerTimestamp - info.SentServerTimestamp) > PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout || (uint)(info.SentServerTimestamp - timeStamp) > PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout)
			{
				timeStamp = PhotonNetwork.ServerTimestamp;
			}
			BuilderTable table = GetTable();
			if ((table.IsInBuilderZone() || info.Sender.IsLocal) && table.ValidateFunctionalPieceState(pieceID, state, NetPlayer.Get(info.Sender)))
			{
				table.SetFunctionalPieceState(pieceID, state, NetPlayer.Get(caller), timeStamp);
			}
		}
	}

	public void RequestBlocksTerminalControl(bool locked)
	{
		BuilderTable table = GetTable();
		if (!table.isTableMutable && !(table.linkedTerminal == null) && table.linkedTerminal.IsTerminalLocked != locked)
		{
			base.photonView.RPC("RequestBlocksTerminalControlRPC", RpcTarget.MasterClient, locked);
		}
	}

	[PunRPC]
	private void RequestBlocksTerminalControlRPC(bool lockedStatus, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestBlocksTerminalControlRPC");
		if (NetworkSystem.Instance.IsMasterClient && ValidateCallLimits(RPC.RequestTerminalControl, info) && info.Sender != null)
		{
			BuilderTable table = GetTable();
			if ((RoomSystem.WasRoomPrivate || table.IsInBuilderZone()) && !table.isTableMutable && !(table.linkedTerminal == null) && VRRigCache.Instance != null && VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig) && !((table.linkedTerminal.transform.position - playerRig.Rig.bodyTransform.position).sqrMagnitude > 9f) && table.linkedTerminal.ValidateTerminalControlRequest(lockedStatus, info.Sender.ActorNumber))
			{
				int num = (lockedStatus ? info.Sender.ActorNumber : (-2));
				base.photonView.RPC("SetBlocksTerminalDriverRPC", RpcTarget.All, num);
			}
		}
	}

	[PunRPC]
	private void SetBlocksTerminalDriverRPC(int driver, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SetBlocksTerminalDriverRPC");
		if (info.Sender != null && info.Sender.IsMasterClient && (driver == -2 || NetworkSystem.Instance.GetPlayer(driver) != null) && ValidateCallLimits(RPC.SetTerminalDriver, info))
		{
			BuilderTable table = GetTable();
			if (!table.isTableMutable && !(table.linkedTerminal == null))
			{
				table.linkedTerminal.SetTerminalDriver(driver);
			}
		}
	}

	public void RequestLoadSharedBlocksMap(string mapID)
	{
		base.photonView.RPC("LoadSharedBlocksMapRPC", RpcTarget.MasterClient, mapID);
	}

	[PunRPC]
	private void LoadSharedBlocksMapRPC(string mapID, PhotonMessageInfo info)
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "LoadSharedBlocksMapRPC");
		if (!ValidateCallLimits(RPC.LoadSharedBlocksMap, info) || info.Sender == null || mapID.IsNullOrEmpty())
		{
			return;
		}
		BuilderTable table = GetTable();
		if (table.isTableMutable || table.linkedTerminal == null)
		{
			return;
		}
		if (!table.linkedTerminal.ValidateLoadMapRequest(mapID, info.Sender.ActorNumber))
		{
			GTDev.LogWarning("SharedBlocks ValidateLoadMapRequest fail");
			return;
		}
		BuilderTable.TableState tableState = table.GetTableState();
		if (tableState == BuilderTable.TableState.Ready || tableState == BuilderTable.TableState.BadData)
		{
			table.SetPendingMap(mapID);
			base.photonView.RPC("SharedTableEventRPC", RpcTarget.Others, (byte)0, mapID);
			localClientTableInit.Reset();
			table.OnMapCleared?.Invoke();
			table.SetTableState(BuilderTable.TableState.WaitingForSharedMapLoad);
			table.FindAndLoadSharedBlocksMap(mapID);
		}
		else
		{
			GTDev.LogWarning("SharedBlocks Invalid state " + tableState);
			LoadSharedBlocksFailedMaster(mapID);
		}
	}

	public void LoadSharedBlocksFailedMaster(string mapID)
	{
		if (NetworkSystem.Instance.IsMasterClient && mapID.Length <= 8)
		{
			base.photonView.RPC("SharedTableEventRPC", RpcTarget.All, (byte)1, mapID);
		}
	}

	public void SharedBlocksOutOfBoundsMaster(string mapID)
	{
		if (NetworkSystem.Instance.IsMasterClient && mapID.Length <= 8)
		{
			base.photonView.RPC("SharedTableEventRPC", RpcTarget.All, (byte)2, mapID);
		}
	}

	[PunRPC]
	private void SharedTableEventRPC(byte eventType, string mapID, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SharedTableEventRPC");
		if (eventType >= 3)
		{
			return;
		}
		if (!SharedBlocksManager.IsMapIDValid(mapID) && eventType != 1)
		{
			GTDev.LogWarning("BuilderTableNetworking SharedTableEventRPC Invalid Map ID");
		}
		else
		{
			if (info.Sender == null || !info.Sender.IsMasterClient)
			{
				return;
			}
			if (!ValidateCallLimits(RPC.SharedTableEvent, info))
			{
				GTDev.LogError("SharedTableEventRPC Failed call limits");
				return;
			}
			BuilderTable table = GetTable();
			if ((table.IsInBuilderZone() || info.Sender.IsLocal) && !table.isTableMutable)
			{
				switch ((SharedTableEventTypes)eventType)
				{
				case SharedTableEventTypes.LOAD_STARTED:
					OnSharedBlocksLoadStarted(mapID);
					break;
				case SharedTableEventTypes.LOAD_FAILED:
					OnLoadSharedBlocksFailed(mapID);
					break;
				case SharedTableEventTypes.OUT_OF_BOUNDS:
					OnSharedBlocksOutOfBounds(mapID);
					break;
				}
			}
		}
	}

	private void OnSharedBlocksLoadStarted(string mapID)
	{
		localClientTableInit.Reset();
		BuilderTable table = GetTable();
		if (table.GetTableState() != BuilderTable.TableState.WaitingForZoneAndRoom)
		{
			table.ClearTable();
			table.ClearQueuedCommands();
			table.SetPendingMap(mapID);
			table.SetTableState(BuilderTable.TableState.WaitingForInitalBuild);
			PlayerEnterBuilder();
		}
	}

	private void OnLoadSharedBlocksFailed(string mapID)
	{
		BuilderTable table = GetTable();
		string pendingMap = table.GetPendingMap();
		if (!pendingMap.IsNullOrEmpty() && !pendingMap.Equals(mapID))
		{
			GTDev.LogWarning("BuilderTableNetworking OnLoadSharedBlocksFailed Unexpected map ID " + mapID);
		}
		BuilderTable.TableState tableState = table.GetTableState();
		if (!NetworkSystem.Instance.IsMasterClient && tableState != BuilderTable.TableState.WaitForMasterResync && tableState != BuilderTable.TableState.WaitingForInitalBuild && tableState != BuilderTable.TableState.Ready && tableState != BuilderTable.TableState.BadData)
		{
			GTDev.LogWarning($"BuilderTableNetworking OnLoadSharedBlocksFailed Unexpected table state {tableState}");
			return;
		}
		if (NetworkSystem.Instance.IsMasterClient && tableState != BuilderTable.TableState.WaitingForSharedMapLoad && tableState != BuilderTable.TableState.WaitForInitialBuildMaster && tableState != BuilderTable.TableState.Ready && tableState != BuilderTable.TableState.BadData)
		{
			GTDev.LogWarning($"BuilderTableNetworking OnLoadSharedBlocksFailed Unexpected table state {tableState}");
			return;
		}
		table.SetPendingMap(null);
		if (table != null && !table.isTableMutable && table.linkedTerminal != null)
		{
			if (!SharedBlocksManager.IsMapIDValid(mapID))
			{
				table.OnMapLoadFailed?.Invoke("BAD MAP ID");
			}
			else
			{
				table.OnMapLoadFailed?.Invoke("LOAD FAILED");
			}
		}
	}

	private void OnSharedBlocksOutOfBounds(string mapID)
	{
		BuilderTable table = GetTable();
		string pendingMap = table.GetPendingMap();
		if (!pendingMap.IsNullOrEmpty() && !pendingMap.Equals(mapID))
		{
			GTDev.LogWarning("BuilderTableNetworking OnSharedBlocksOutOfBounds Unexpected map ID " + mapID);
		}
		BuilderTable.TableState tableState = table.GetTableState();
		if (!NetworkSystem.Instance.IsMasterClient && tableState != BuilderTable.TableState.WaitForMasterResync && tableState != BuilderTable.TableState.WaitingForInitalBuild)
		{
			GTDev.LogWarning($"BuilderTableNetworking OnSharedBlocksOutOfBounds Unexpected table state {tableState}");
			return;
		}
		if (NetworkSystem.Instance.IsMasterClient && tableState != BuilderTable.TableState.WaitForInitialBuildMaster && tableState != BuilderTable.TableState.BadData)
		{
			GTDev.LogWarning($"BuilderTableNetworking OnSharedBlocksOutOfBounds Unexpected table state {tableState}");
			return;
		}
		table.SetPendingMap(null);
		if (table != null && !table.isTableMutable && table.linkedTerminal != null)
		{
			table.OnMapLoadFailed?.Invoke("BLOCKS ARE OUT OF BOUNDS FOR SHARED BLOCKS ROOM");
		}
	}

	public void RequestPaintPiece(int pieceID, int materialType)
	{
	}
}
