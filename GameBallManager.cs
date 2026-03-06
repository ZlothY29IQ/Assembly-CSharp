using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[NetworkBehaviourWeaved(0)]
public class GameBallManager : NetworkComponent
{
	private enum RPC
	{
		RequestGrabBall,
		GrabBall,
		RequestThrowBall,
		ThrowBall,
		RequestLaunchBall,
		LaunchBall,
		TeleportBall,
		RequestSetBallPosition,
		Count
	}

	[OnEnterPlay_SetNull]
	public static volatile GameBallManager Instance;

	public PhotonView photonView;

	private List<GameBall> gameBalls;

	private List<GameBallData> gameBallData;

	public const float MAX_LOCAL_MAGNITUDE_SQR = 6400f;

	private const float MAX_LAUNCHER_DISTANCE_SQR = 1f;

	public const float MAX_CATCH_DISTANCE_FROM_HAND_SQR = 25f;

	public const float MAX_DISTANCE_FROM_HAND_SQR = 6.25f;

	public const float MAX_THROW_VELOCITY_SQR = 1600f;

	private CallLimiter[] _callLimiters;

	protected override void Awake()
	{
		base.Awake();
		Instance = this;
		gameBalls = new List<GameBall>(64);
		gameBallData = new List<GameBallData>(64);
		_callLimiters = new CallLimiter[8];
		_callLimiters[0] = new CallLimiter(50, 1f);
		_callLimiters[1] = new CallLimiter(50, 1f);
		_callLimiters[2] = new CallLimiter(25, 1f);
		_callLimiters[3] = new CallLimiter(25, 1f);
		_callLimiters[4] = new CallLimiter(10, 1f);
		_callLimiters[5] = new CallLimiter(10, 1f);
		_callLimiters[6] = new CallLimiter(10, 1f);
		_callLimiters[7] = new CallLimiter(25, 1f);
	}

	private bool ValidateCallLimits(RPC rpcCall, PhotonMessageInfo info)
	{
		if (rpcCall < RPC.RequestGrabBall || rpcCall >= RPC.Count)
		{
			return false;
		}
		bool num = _callLimiters[(int)rpcCall].CheckCallTime(Time.time);
		if (!num)
		{
			ReportRPCCall(rpcCall, info, "Too many RPC Calls!");
		}
		return num;
	}

	private void ReportRPCCall(RPC rpcCall, PhotonMessageInfo info, string susReason)
	{
		MonkeAgent.instance.SendReport($"Reason: {susReason}   RPC: {rpcCall}", info.Sender.UserId, info.Sender.NickName);
	}

	public GameBallId AddGameBall(GameBall gameBall)
	{
		int count = gameBallData.Count;
		gameBalls.Add(gameBall);
		gameBallData.Add(default(GameBallData));
		gameBall.id = new GameBallId(count);
		return gameBall.id;
	}

	public GameBall GetGameBall(GameBallId id)
	{
		if (!id.IsValid())
		{
			return null;
		}
		int index = id.index;
		return gameBalls[index];
	}

	public GameBallId TryGrabLocal(Vector3 handPosition, int teamId)
	{
		_ = PhotonNetwork.LocalPlayer.ActorNumber;
		GameBallId result = GameBallId.Invalid;
		float num = float.MaxValue;
		for (int i = 0; i < gameBalls.Count; i++)
		{
			if (gameBalls[i].onlyGrabTeamId == -1 || gameBalls[i].onlyGrabTeamId == teamId)
			{
				float sqrMagnitude = gameBalls[i].GetVelocity().sqrMagnitude;
				double num2 = 0.0625;
				if (sqrMagnitude > 2f)
				{
					num2 = 0.25;
				}
				float sqrMagnitude2 = (handPosition - gameBalls[i].transform.position).sqrMagnitude;
				if ((double)sqrMagnitude2 < num2 && sqrMagnitude2 < num)
				{
					result = gameBalls[i].id;
					num = sqrMagnitude2;
				}
			}
		}
		return result;
	}

	public void RequestGrabBall(GameBallId ballId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation)
	{
		GrabBall(ballId, isLeftHand, localPosition, localRotation, NetPlayer.Get(PhotonNetwork.LocalPlayer));
		long num = BitPackUtils.PackHandPosRotForNetwork(localPosition, localRotation);
		photonView.RPC("RequestGrabBallRPC", RpcTarget.MasterClient, ballId.index, isLeftHand, num);
		PhotonNetwork.SendAllOutgoingCommands();
	}

	[PunRPC]
	private void RequestGrabBallRPC(int gameBallIndex, bool isLeftHand, long packedPosRot, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestGrabBallRPC");
		if (!PhotonNetwork.IsMasterClient || !ValidateCallLimits(RPC.RequestGrabBall, info))
		{
			return;
		}
		if (gameBallIndex < 0 || gameBallIndex > gameBalls.Count)
		{
			ReportRPCCall(RPC.RequestGrabBall, info, "gameBallIndex out of array.");
			return;
		}
		BitPackUtils.UnpackHandPosRotFromNetwork(packedPosRot, out var localPos, out var handRot);
		if (!localPos.IsValid(10000f) || !handRot.IsValid())
		{
			ReportRPCCall(RPC.RequestGrabBall, info, "localPosition or localRotation is invalid.");
			return;
		}
		bool flag = true;
		GameBall gameBall = gameBalls[gameBallIndex];
		if (gameBall != null)
		{
			if (VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig))
			{
				if (!playerRig.Rig.IsPositionInRange(gameBall.transform.position, 25f))
				{
					flag = false;
					ReportRPCCall(RPC.RequestGrabBall, info, "gameBall exceeds max catch distance.");
				}
			}
			else
			{
				flag = false;
				ReportRPCCall(RPC.RequestGrabBall, info, "Cannot find VRRig for grabber.");
			}
			if (localPos.sqrMagnitude > 25f)
			{
				flag = false;
				ReportRPCCall(RPC.RequestGrabBall, info, "gameBall exceeds max catch distance.");
			}
		}
		else
		{
			flag = false;
			ReportRPCCall(RPC.RequestGrabBall, info, "gameBall does not exist.");
		}
		if (flag)
		{
			photonView.RPC("GrabBallRPC", RpcTarget.All, gameBallIndex, isLeftHand, packedPosRot, info.Sender);
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	[PunRPC]
	private void GrabBallRPC(int gameBallIndex, bool isLeftHand, long packedPosRot, Player grabbedBy, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "GrabBallRPC");
		if (!ValidateCallLimits(RPC.GrabBall, info))
		{
			return;
		}
		if (gameBallIndex < 0 || gameBallIndex > gameBalls.Count)
		{
			ReportRPCCall(RPC.GrabBall, info, "gameBallIndex out of array.");
			return;
		}
		BitPackUtils.UnpackHandPosRotFromNetwork(packedPosRot, out var localPos, out var handRot);
		if (!localPos.IsValid(10000f) || !handRot.IsValid())
		{
			ReportRPCCall(RPC.GrabBall, info, "localPosition or localRotation is invalid.");
		}
		else
		{
			GrabBall(new GameBallId(gameBallIndex), isLeftHand, localPos, handRot, NetPlayer.Get(grabbedBy));
		}
	}

	private void GrabBall(GameBallId gameBallId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, NetPlayer grabbedByPlayer)
	{
		if (!VRRigCache.Instance.TryGetVrrig(grabbedByPlayer, out var playerRig))
		{
			return;
		}
		_ = gameBallData[gameBallId.index];
		GameBall gameBall = gameBalls[gameBallId.index];
		GameBallPlayer gameBallPlayer = ((gameBall.heldByActorNumber < 0) ? null : GameBallPlayer.GetGamePlayer(gameBall.heldByActorNumber));
		int num = ((gameBallPlayer == null) ? (-1) : gameBallPlayer.FindHandIndex(gameBallId));
		bool flag = gameBall.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
		int num2 = -1;
		if (gameBallPlayer != null)
		{
			gameBallPlayer.ClearGrabbedIfHeld(gameBallId);
			num2 = gameBallPlayer.teamId;
			if (num != -1 && flag)
			{
				GameBallPlayerLocal.instance.ClearGrabbed(num);
			}
		}
		BodyDockPositions myBodyDockPositions = playerRig.Rig.myBodyDockPositions;
		Transform parent = (isLeftHand ? myBodyDockPositions.leftHandTransform : myBodyDockPositions.rightHandTransform);
		if (!grabbedByPlayer.IsLocal)
		{
			gameBall.SetVisualOffset(detach: true);
		}
		gameBall.transform.SetParent(parent);
		gameBall.transform.SetLocalPositionAndRotation(localPosition, localRotation);
		Rigidbody component = gameBall.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = true;
		}
		GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(grabbedByPlayer.ActorNumber);
		bool flag2 = num2 == gamePlayer.teamId;
		bool flag3 = gameBall.lastHeldByActorNumber != grabbedByPlayer.ActorNumber;
		MonkeBall component2 = gameBall.GetComponent<MonkeBall>();
		if (component2 != null)
		{
			component2.OnGrabbed();
			if (!flag2 && flag3)
			{
				component2.OnSwitchHeldByTeam(gamePlayer.teamId);
			}
		}
		gameBall.heldByActorNumber = grabbedByPlayer.ActorNumber;
		gameBall.lastHeldByActorNumber = gameBall.heldByActorNumber;
		gameBall.SetHeldByTeamId(gamePlayer.teamId);
		int handIndex = GameBallPlayer.GetHandIndex(isLeftHand);
		gamePlayer.SetGrabbed(gameBallId, handIndex);
		if (grabbedByPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			GameBallPlayerLocal.instance.SetGrabbed(gameBallId, GameBallPlayer.GetHandIndex(isLeftHand));
			GameBallPlayerLocal.instance.PlayCatchFx(isLeftHand);
		}
		gameBall.PlayCatchFx();
		if (component2 != null)
		{
			MonkeBallGame.Instance.OnBallGrabbed(gameBallId);
		}
	}

	public void RequestThrowBall(GameBallId ballId, bool isLeftHand, Vector3 velocity, Vector3 angVelocity)
	{
		GameBall gameBall = GetGameBall(ballId);
		if (!(gameBall == null))
		{
			Vector3 position = gameBall.transform.position;
			Quaternion rotation = gameBall.transform.rotation;
			ThrowBall(ballId, isLeftHand, position, rotation, velocity, angVelocity, NetPlayer.Get(PhotonNetwork.LocalPlayer));
			photonView.RPC("RequestThrowBallRPC", RpcTarget.MasterClient, ballId.index, isLeftHand, position, rotation, velocity, angVelocity);
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	[PunRPC]
	private void RequestThrowBallRPC(int gameBallIndex, bool isLeftHand, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestThrowBallRPC");
		if (!PhotonNetwork.IsMasterClient || !ValidateCallLimits(RPC.RequestThrowBall, info))
		{
			return;
		}
		if (!ValidateThrowBallParams(gameBallIndex, position, rotation, velocity, angVelocity))
		{
			ReportRPCCall(RPC.RequestThrowBall, info, "ValidateThrowBallParams are invalid.");
			return;
		}
		if (gameBalls[gameBallIndex].heldByActorNumber != info.Sender.ActorNumber && gameBalls[gameBallIndex].lastHeldByActorNumber != info.Sender.ActorNumber && (gameBalls[gameBallIndex].heldByActorNumber != -1 || gameBalls[gameBallIndex].lastHeldByActorNumber != -1))
		{
			ReportRPCCall(RPC.RequestThrowBall, info, "gameBall is not held by the thrower.");
			return;
		}
		bool flag = true;
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender.ActorNumber), out var playerRig))
		{
			if ((playerRig.Rig.transform.position - position).sqrMagnitude > 6.25f)
			{
				flag = false;
				ReportRPCCall(RPC.RequestThrowBall, info, "gameBall distance exceeds max distance from hand.");
			}
		}
		else
		{
			flag = false;
			ReportRPCCall(RPC.RequestThrowBall, info, "Player rig cannot be found for thrower.");
		}
		if (flag)
		{
			photonView.RPC("ThrowBallRPC", RpcTarget.All, gameBallIndex, isLeftHand, position, rotation, velocity, angVelocity, info.Sender, info.SentServerTime);
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	[PunRPC]
	private void ThrowBallRPC(int gameBallIndex, bool isLeftHand, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, Player thrownBy, double throwTime, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "ThrowBallRPC");
		if (!ValidateCallLimits(RPC.ThrowBall, info))
		{
			return;
		}
		if (!ValidateThrowBallParams(gameBallIndex, position, rotation, velocity, angVelocity))
		{
			ReportRPCCall(RPC.ThrowBall, info, "ValidateThrowBallParams are invalid.");
			return;
		}
		if ((base.transform.position - position).sqrMagnitude > 6400f)
		{
			ReportRPCCall(RPC.ThrowBall, info, "gameBall distance exceeds max distance from arena.");
			return;
		}
		if (double.IsNaN(throwTime) || double.IsInfinity(throwTime))
		{
			ReportRPCCall(RPC.ThrowBall, info, "throwTime is not a valid value.");
			return;
		}
		float num = (float)(PhotonNetwork.Time - throwTime);
		if (num < -3f || num > 3f)
		{
			ReportRPCCall(RPC.ThrowBall, info, "Throw time delta exceeds range.");
			return;
		}
		GameBall gameBall = gameBalls[gameBallIndex];
		position = 0.5f * Physics.gravity * gameBall.gravityMult * num * num + velocity * num + position;
		velocity = Physics.gravity * gameBall.gravityMult * num + velocity;
		rotation *= Quaternion.Euler(angVelocity * Time.deltaTime);
		ThrowBall(new GameBallId(gameBallIndex), isLeftHand, position, rotation, velocity, angVelocity, NetPlayer.Get(thrownBy));
	}

	private bool ValidateThrowBallParams(int gameBallIndex, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity)
	{
		if (gameBallIndex < 0 || gameBallIndex >= gameBalls.Count)
		{
			return false;
		}
		if (!position.IsValid(10000f) || !rotation.IsValid() || !velocity.IsValid(10000f) || !angVelocity.IsValid(10000f))
		{
			return false;
		}
		if (velocity.sqrMagnitude > 1600f)
		{
			return false;
		}
		return true;
	}

	private void ThrowBall(GameBallId gameBallId, bool isLeftHand, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, NetPlayer thrownByPlayer)
	{
		GameBall gameBall = gameBalls[gameBallId.index];
		if (!thrownByPlayer.IsLocal)
		{
			gameBall.SetVisualOffset(detach: true);
		}
		gameBall.transform.SetParent(null);
		gameBall.transform.SetLocalPositionAndRotation(position, rotation);
		Rigidbody component = gameBall.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.position = position;
			component.rotation = rotation;
			component.linearVelocity = velocity;
			component.angularVelocity = angVelocity;
		}
		gameBall.heldByActorNumber = -1;
		MonkeBall monkeBall = MonkeBall.Get(gameBall);
		if (monkeBall != null)
		{
			monkeBall.ClearCannotGrabTeamId();
		}
		bool num = thrownByPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
		int handIndex = GameBallPlayer.GetHandIndex(isLeftHand);
		RigContainer playerRig;
		if (num)
		{
			GameBallPlayerLocal.instance.gamePlayer.ClearGrabbed(handIndex);
			GameBallPlayerLocal.instance.ClearGrabbed(handIndex);
			GameBallPlayerLocal.instance.PlayThrowFx(isLeftHand);
		}
		else if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(thrownByPlayer.ActorNumber), out playerRig))
		{
			GameBallPlayer component2 = playerRig.Rig.GetComponent<GameBallPlayer>();
			if (component2 != null)
			{
				component2.ClearGrabbedIfHeld(gameBallId);
			}
		}
		gameBall.PlayThrowFx();
	}

	public void RequestLaunchBall(GameBallId ballId, Vector3 velocity)
	{
		GameBall gameBall = GetGameBall(ballId);
		if (!(gameBall == null))
		{
			Vector3 position = gameBall.transform.position;
			Quaternion rotation = gameBall.transform.rotation;
			LaunchBall(ballId, position, rotation, velocity);
			photonView.RPC("RequestLaunchBallRPC", RpcTarget.MasterClient, ballId.index, position, rotation, velocity);
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	[PunRPC]
	private void RequestLaunchBallRPC(int gameBallIndex, Vector3 position, Quaternion rotation, Vector3 velocity, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestLaunchBallRPC");
		if (!PhotonNetwork.IsMasterClient || !ValidateCallLimits(RPC.RequestLaunchBall, info))
		{
			return;
		}
		if (!ValidateThrowBallParams(gameBallIndex, position, rotation, velocity, Vector3.zero))
		{
			ReportRPCCall(RPC.RequestLaunchBall, info, "ValidateThrowBallParams are invalid.");
			return;
		}
		bool flag = true;
		if ((MonkeBallGame.Instance.BallLauncher.position - position).sqrMagnitude > 1f)
		{
			flag = false;
			ReportRPCCall(RPC.RequestLaunchBall, info, "gameBall distance exceeds max distance from launcher.");
		}
		if (flag)
		{
			photonView.RPC("LaunchBallRPC", RpcTarget.All, gameBallIndex, position, rotation, velocity, info.SentServerTime);
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	[PunRPC]
	private void LaunchBallRPC(int gameBallIndex, Vector3 position, Quaternion rotation, Vector3 velocity, double throwTime, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "LaunchBallRPC");
		if (!ValidateCallLimits(RPC.ThrowBall, info))
		{
			return;
		}
		if (!ValidateThrowBallParams(gameBallIndex, position, rotation, velocity, Vector3.zero))
		{
			ReportRPCCall(RPC.LaunchBall, info, "ValidateThrowBallParams are invalid.");
			return;
		}
		float num = (float)(PhotonNetwork.Time - throwTime);
		if (num < -3f || num > 3f)
		{
			ReportRPCCall(RPC.LaunchBall, info, "Throw time delta exceeds range.");
			return;
		}
		GameBall gameBall = gameBalls[gameBallIndex];
		position = 0.5f * Physics.gravity * gameBall.gravityMult * num * num + velocity * num + position;
		velocity = Physics.gravity * gameBall.gravityMult * num + velocity;
		LaunchBall(new GameBallId(gameBallIndex), position, rotation, velocity);
	}

	private void LaunchBall(GameBallId gameBallId, Vector3 position, Quaternion rotation, Vector3 velocity)
	{
		GameBall gameBall = gameBalls[gameBallId.index];
		gameBall.transform.SetParent(null);
		gameBall.transform.SetLocalPositionAndRotation(position, rotation);
		Rigidbody component = gameBall.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.position = position;
			component.rotation = rotation;
			component.linearVelocity = velocity;
			component.angularVelocity = Vector3.zero;
		}
		gameBall.heldByActorNumber = -1;
		gameBall.lastHeldByActorNumber = -1;
		gameBall.WasLaunched();
		MonkeBall monkeBall = MonkeBall.Get(gameBall);
		if (monkeBall != null)
		{
			monkeBall.ClearCannotGrabTeamId();
			monkeBall.TriggerDelayedResync();
		}
		gameBall.PlayThrowFx();
		GameBallPlayerLocal.instance.gamePlayer.ClearAllGrabbed();
		GameBallPlayerLocal.instance.ClearAllGrabbed();
	}

	public void RequestTeleportBall(GameBallId id, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			photonView.RPC("TeleportBallRPC", RpcTarget.All, id.index, position, rotation, velocity, angularVelocity);
		}
	}

	[PunRPC]
	private void TeleportBallRPC(int gameBallIndex, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "TeleportBallRPC");
		if (ValidateCallLimits(RPC.TeleportBall, info))
		{
			if (gameBallIndex < 0 || gameBallIndex >= gameBalls.Count)
			{
				ReportRPCCall(RPC.TeleportBall, info, "gameBallIndex is out of range.");
				return;
			}
			if (!position.IsValid(10000f) || !rotation.IsValid() || !velocity.IsValid(10000f) || !angularVelocity.IsValid(10000f))
			{
				ReportRPCCall(RPC.TeleportBall, info, "Ball params are invalid.");
				return;
			}
			if ((base.transform.position - position).sqrMagnitude > 6400f)
			{
				ReportRPCCall(RPC.ThrowBall, info, "gameBall distance exceeds max distance from arena.");
				return;
			}
			GameBallId gameBallId = new GameBallId(gameBallIndex);
			TeleportBall(gameBallId, position, rotation, velocity, angularVelocity);
		}
	}

	private void TeleportBall(GameBallId gameBallId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
	{
		int index = gameBallId.index;
		if (index < 0 || index >= gameBallData.Count)
		{
			return;
		}
		_ = gameBallData[index];
		GameBall gameBall = gameBalls[index];
		if (!(gameBall == null))
		{
			gameBall.SetVisualOffset(detach: false);
			gameBall.transform.SetLocalPositionAndRotation(position, rotation);
			Rigidbody component = gameBall.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.isKinematic = false;
				component.position = position;
				component.rotation = rotation;
				component.linearVelocity = velocity;
				component.angularVelocity = angularVelocity;
			}
		}
	}

	public void RequestSetBallPosition(GameBallId ballId)
	{
		if (!(GetGameBall(ballId) == null) && NetworkSystem.Instance.InRoom)
		{
			photonView.RPC("RequestSetBallPositionRPC", RpcTarget.MasterClient, ballId.index);
			PhotonNetwork.SendAllOutgoingCommands();
		}
	}

	[PunRPC]
	private void RequestSetBallPositionRPC(int gameBallIndex, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestSetBallPositionRPC");
		if (!PhotonNetwork.IsMasterClient || !ValidateCallLimits(RPC.RequestSetBallPosition, info))
		{
			return;
		}
		if (gameBallIndex < 0 || gameBallIndex >= gameBalls.Count)
		{
			ReportRPCCall(RPC.RequestSetBallPosition, info, "gameBallIndex is out of range.");
			return;
		}
		GameBall gameBall = gameBalls[gameBallIndex];
		if (gameBall == null)
		{
			return;
		}
		if ((gameBall.transform.position - base.transform.position).sqrMagnitude > 6400f)
		{
			ReportRPCCall(RPC.RequestSetBallPosition, info, "Ball position is outside of arena.");
			return;
		}
		Rigidbody component = gameBall.GetComponent<Rigidbody>();
		if (!(component == null))
		{
			photonView.RPC("TeleportBallRPC", info.Sender, gameBallIndex, gameBall.transform.position, gameBall.transform.rotation, component.linearVelocity, component.angularVelocity);
		}
	}

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}
