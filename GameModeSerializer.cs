using System;
using Fusion;
using GorillaExtensions;
using GorillaGameModes;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Scripting;

[NetworkBehaviourWeaved(1)]
internal class GameModeSerializer : GorillaSerializerMasterOnly, IStateAuthorityChanged, IPublicFacingInterface
{
	[WeaverGenerated]
	[DefaultForProperty("gameModeKeyInt", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private int _gameModeKeyInt;

	private GameModeType gameModeKey;

	private GorillaGameManager gameModeInstance;

	private FusionGameModeData gameModeData;

	private Type currentGameDataType;

	private CallLimiter broadcastTagCallLimit = new CallLimiter(12, 5f);

	public static Action<NetPlayer> FusionGameModeOwnerChanged;

	[Networked]
	[NetworkedWeaved(0, 1)]
	private unsafe int gameModeKeyInt
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing GameModeSerializer.gameModeKeyInt. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(int*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing GameModeSerializer.gameModeKeyInt. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(int*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	public GorillaGameManager GameModeInstance => gameModeInstance;

	protected override bool OnSpawnSetupCheck(PhotonMessageInfoWrapped wrappedInfo, out GameObject outTargetObject, out Type outTargetType)
	{
		outTargetObject = null;
		outTargetType = null;
		NetPlayer player = NetworkSystem.Instance.GetPlayer(wrappedInfo.senderID);
		if (player != null)
		{
			MonkeAgent.IncrementRPCCall(wrappedInfo, "OnSpawnSetupCheck");
		}
		GameModeSerializer activeNetworkHandler = GorillaGameModes.GameMode.ActiveNetworkHandler;
		if (player != null && player.InRoom)
		{
			if (!player.IsMasterClient)
			{
				GTDev.LogError("SPAWN FAIL NOT MASTER :" + player.UserId + player.NickName);
				MonkeAgent.instance.SendReport("trying to inappropriately create game managers", player.UserId, player.NickName);
				return false;
			}
			if (!netView.IsRoomView)
			{
				GTDev.LogError("SPAWN FAIL ROOM VIEW" + player.UserId + player.NickName);
				MonkeAgent.instance.SendReport("creating game manager as player object", player.UserId, player.NickName);
				return false;
			}
			if (activeNetworkHandler.IsNotNull() && activeNetworkHandler != this)
			{
				GTDev.LogError("DUPLICATE CHECK" + player.UserId + player.NickName);
				MonkeAgent.instance.SendReport("trying to create multiple game managers", player.UserId, player.NickName);
				return false;
			}
		}
		else if ((activeNetworkHandler.IsNotNull() && activeNetworkHandler != this) || !netView.IsRoomView)
		{
			GTDev.LogError("ACTIVE HANDLER CHECK FAIL" + player?.UserId + player?.NickName);
			GTDev.LogError("existing game manager! destroying newly created manager");
			return false;
		}
		object[] instantiationData = wrappedInfo.punInfo.photonView.InstantiationData;
		if (instantiationData == null || instantiationData.Length < 1 || !(instantiationData[0] is int num))
		{
			GTDev.LogError("missing instantiation data");
			return false;
		}
		gameModeKey = (GameModeType)num;
		gameModeInstance = GorillaGameModes.GameMode.GetGameModeInstance(gameModeKey);
		if (gameModeInstance.IsNull() || !gameModeInstance.ValidGameMode())
		{
			return false;
		}
		serializeTarget = gameModeInstance;
		base.transform.parent = VRRigCache.Instance.NetworkParent;
		return true;
	}

	internal void Init(int gameModeType)
	{
		Debug.Log("<color=red>Init called</color>");
		gameModeKeyInt = gameModeType;
	}

	protected override void OnSuccesfullySpawned(PhotonMessageInfoWrapped info)
	{
		netView.GetView.AddCallbackTarget(this);
		GorillaGameModes.GameMode.SetupGameModeRemote(this);
	}

	protected override void OnBeforeDespawn()
	{
		GorillaGameModes.GameMode.RemoveNetworkLink(this);
	}

	protected override void OnFailedSpawn()
	{
	}

	[PunRPC]
	internal void RPC_ReportTag(int taggedPlayer, PhotonMessageInfo info)
	{
		ReportTag(NetworkSystem.Instance.GetPlayer(taggedPlayer), new PhotonMessageInfoWrapped(info));
	}

	[PunRPC]
	internal void RPC_ReportHit(PhotonMessageInfo info)
	{
		ReportHit(new PhotonMessageInfoWrapped(info));
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	internal unsafe void RPC_ReportTag(int taggedPlayer, RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void GameModeSerializer::RPC_ReportTag(System.Int32,Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			int num = 8;
			num += 4;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GameModeSerializer::RPC_ReportTag(System.Int32,Fusion.RpcInfo)", num);
				return;
			}
			if (base.Runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 1);
				int num2 = 8;
				*(int*)(ptr2 + num2) = taggedPlayer;
				num2 += 4;
				ptr->Offset = num2 * 8;
				base.Runner.SendRpc(ptr);
			}
			if ((localAuthorityMask & 7) == 0)
			{
				return;
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		ReportTag(NetworkSystem.Instance.GetPlayer(taggedPlayer), new PhotonMessageInfoWrapped(info));
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	internal unsafe void RPC_ReportHit(RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void GameModeSerializer::RPC_ReportHit(Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			int num = 8;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GameModeSerializer::RPC_ReportHit(Fusion.RpcInfo)", num);
				return;
			}
			if (base.Runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 2);
				int num2 = 8;
				ptr->Offset = num2 * 8;
				base.Runner.SendRpc(ptr);
			}
			if ((localAuthorityMask & 7) == 0)
			{
				return;
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		ReportHit(new PhotonMessageInfoWrapped(info));
	}

	private void ReportTag(NetPlayer taggedPlayer, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "ReportTag");
		NetPlayer sender = info.Sender;
		gameModeInstance.ReportTag(taggedPlayer, sender);
	}

	private void ReportHit(PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "ReportContactWithLavaRPC");
		bool num = ZoneManagement.instance.IsZoneActive(GTZone.customMaps);
		bool flag = false;
		if (VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig))
		{
			InfectionLavaController infectionLavaController = null;
			if (playerRig.Rig.zoneEntity != null)
			{
				infectionLavaController = InfectionLavaController.GetControllerForZone(playerRig.Rig.zoneEntity.currentZone);
			}
			flag = infectionLavaController != null && infectionLavaController.LavaCurrentlyActivated && (infectionLavaController.SurfaceCenter - playerRig.Rig.syncPos).sqrMagnitude < 2500f && infectionLavaController.LavaPlane.GetDistanceToPoint(playerRig.Rig.syncPos) < 5f;
		}
		if (num || flag)
		{
			GameModeInstance.HitPlayer(info.Sender);
		}
	}

	[PunRPC]
	internal void RPC_BroadcastRoundComplete(PhotonMessageInfo info)
	{
		BroadcastRoundComplete(info);
	}

	private void BroadcastRoundComplete(PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "BroadcastRoundComplete");
		if (info.Sender.IsMasterClient)
		{
			gameModeInstance.HandleRoundComplete();
		}
	}

	[PunRPC]
	internal void RPC_BroadcastTag(int taggedPlayer, int taggingPlayer, PhotonMessageInfo info)
	{
		BroadcastTag(NetworkSystem.Instance.GetPlayer(taggedPlayer), NetworkSystem.Instance.GetPlayer(taggingPlayer), info);
	}

	private void BroadcastTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "BroadcastTag");
		if (info.Sender.IsMasterClient && taggedPlayer != null && taggingPlayer != null && broadcastTagCallLimit.CheckCallTime(Time.time))
		{
			gameModeInstance.HandleTagBroadcast(taggedPlayer, taggingPlayer);
		}
	}

	protected override void FusionDataRPC(string method, NetPlayer targetPlayer, params object[] parameters)
	{
		Debug.Log(gameModeData.GetType().Name);
	}

	protected override void FusionDataRPC(string method, RpcTarget target, params object[] parameters)
	{
		base.FusionDataRPC(method, target, parameters);
	}

	void IStateAuthorityChanged.StateAuthorityChanged()
	{
		FusionGameModeOwnerChanged(NetworkSystem.Instance.GetPlayer(base.Object.StateAuthority));
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		gameModeKeyInt = _gameModeKeyInt;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_gameModeKeyInt = gameModeKeyInt;
	}

	[NetworkRpcWeavedInvoker(1, 7, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_ReportTag_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int taggedPlayer = num2;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((GameModeSerializer)behaviour).RPC_ReportTag(taggedPlayer, info);
	}

	[NetworkRpcWeavedInvoker(2, 7, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_ReportHit_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((GameModeSerializer)behaviour).RPC_ReportHit(info);
	}
}
