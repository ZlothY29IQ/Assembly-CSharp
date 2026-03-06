using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

public class GRShuttle : MonoBehaviour, IGorillaSliceableSimple
{
	public const int InvalidId = -1;

	private const int MAX_DEPTH = 29;

	public GTZone zone;

	public GRShuttleUI shuttleUI;

	public GRDoor entryDoor;

	private GRShuttleGroupLoc location;

	private int employeeIndex;

	public AbilitySound takeOffSound;

	public AbilitySound moveSound;

	public AbilitySound landSound;

	public GorillaFriendCollider friendCollider;

	public GorillaNetworkJoinTrigger joinTrigger;

	public GRShuttle specificDestinationShuttle;

	public int specificFloor = -1;

	public ParticleSystem windowFx;

	public List<GameObject> hideOnMove;

	public List<GameObject> showOnMove;

	public BoxCollider inShuttleVolume;

	public IDCardScanner entryCardScanner;

	public IDCardScanner departCardScanner;

	[NonSerialized]
	public int shuttleId;

	private GhostReactor reactor;

	private int targetSection;

	private GRShuttleState state;

	private double stateStartTime;

	private GRBay shuttleBay;

	private NetPlayer shuttleOwner;

	private double lastCloseTime;

	private static int[] sectionFloors = new int[8] { -1, 0, 4, 9, 14, 19, 24, 29 };

	public void Awake()
	{
		shuttleUI.Setup(null, null);
		if (entryCardScanner != null)
		{
			entryCardScanner.requireSpecificPlayer = true;
			entryCardScanner.restrictToPlayer = null;
		}
		if (departCardScanner != null)
		{
			departCardScanner.requireSpecificPlayer = true;
			departCardScanner.restrictToPlayer = null;
		}
		state = GRShuttleState.Docked;
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void Init(int shuttleId)
	{
		this.shuttleId = shuttleId;
		StopMoveFx();
	}

	public void SetBay(GRBay bay)
	{
		shuttleBay = bay;
	}

	public void SetReactor(GhostReactor reactor)
	{
		this.reactor = reactor;
	}

	public void SetLocation(GRShuttleGroupLoc location)
	{
		this.location = location;
		targetSection = ClampTargetSection(targetSection);
	}

	public void Setup(GhostReactor reactor, GRShuttleGroupLoc location, int employeeIndex)
	{
		this.reactor = reactor;
		this.location = location;
		this.employeeIndex = employeeIndex;
		SetOwner(null);
		targetSection = ClampTargetSection(targetSection);
	}

	public int GetTargetFloor()
	{
		if (specificDestinationShuttle != null)
		{
			return specificDestinationShuttle.specificFloor;
		}
		if (targetSection < 0 || targetSection >= sectionFloors.Length)
		{
			return 0;
		}
		return sectionFloors[targetSection];
	}

	public GRShuttleState GetState()
	{
		return state;
	}

	public NetPlayer GetOwner()
	{
		return shuttleOwner;
	}

	public void SetOwner(NetPlayer player)
	{
		shuttleOwner = player;
		shuttleUI.Setup(reactor, player);
		entryCardScanner.restrictToPlayer = player;
		departCardScanner.restrictToPlayer = player;
		if (shuttleBay != null)
		{
			shuttleBay.Refresh();
		}
	}

	public void SliceUpdate()
	{
		UpdateState();
	}

	public void Refresh()
	{
		shuttleUI.RefreshUI();
	}

	public void JoinShuttleRoomLocalPlayer(GRShuttle sourceShuttle, GRShuttle destShuttle)
	{
	}

	public static void TeleportLocalPlayer(GRShuttle sourceShuttle, GRShuttle destShuttle)
	{
		sourceShuttle.friendCollider.RefreshPlayersWithinBounds();
		if (sourceShuttle.friendCollider.playerIDsCurrentlyTouching.Contains(NetworkSystem.Instance.LocalPlayer.UserId))
		{
			GTPlayer instance = GTPlayer.Instance;
			VRRig localRig = VRRig.LocalRig;
			float angle = destShuttle.transform.rotation.eulerAngles.y - sourceShuttle.transform.rotation.eulerAngles.y;
			Vector3 vector = localRig.transform.position - instance.transform.position;
			Vector3 position = sourceShuttle.transform.InverseTransformPoint(instance.transform.position);
			position.x *= 0.8f;
			position.z *= 0.8f;
			Vector3 position2 = destShuttle.transform.TransformPoint(position);
			instance.TeleportTo(position2, instance.transform.rotation);
			instance.turnParent.transform.RotateAround(instance.headCollider.transform.position, sourceShuttle.transform.up, angle);
			localRig.transform.position = instance.transform.position + vector;
			instance.InitializeValues();
		}
	}

	public void SetState(GRShuttleState newState, bool force = false)
	{
		if (state == newState && !force)
		{
			return;
		}
		switch (state)
		{
		case GRShuttleState.PostMove:
			if (specificDestinationShuttle != null)
			{
				OpenDoorLocal();
			}
			else
			{
				CloseDoorLocal();
			}
			break;
		case GRShuttleState.PostArrive:
			OpenDoorLocal();
			break;
		case GRShuttleState.Docked:
			if (shuttleBay != null)
			{
				shuttleBay.Refresh();
			}
			break;
		}
		state = newState;
		stateStartTime = Time.timeAsDouble;
		switch (state)
		{
		case GRShuttleState.PreMove:
			CloseDoorLocal();
			takeOffSound.Play(null);
			if (specificDestinationShuttle != null)
			{
				GRPlayer gRPlayer = GRPlayer.Get(GRElevatorManager.LowestActorNumberInElevator(friendCollider, specificDestinationShuttle.friendCollider));
				shuttleOwner = gRPlayer.gamePlayer.rig.OwningNetPlayer;
			}
			TryStartLocalPlayerShuttleMove(shuttleId, shuttleOwner);
			StartMoveFx();
			break;
		case GRShuttleState.Moving:
			moveSound.Play(null);
			break;
		case GRShuttleState.Arriving:
			CloseDoorLocal();
			moveSound.Play(null);
			break;
		case GRShuttleState.PostArrive:
			landSound.Play(null);
			break;
		case GRShuttleState.Docked:
			if (shuttleBay != null)
			{
				shuttleBay.Refresh();
			}
			StopMoveFx();
			break;
		case GRShuttleState.PostMove:
			break;
		}
	}

	private void UpdateState()
	{
		double timeAsDouble = Time.timeAsDouble;
		switch (state)
		{
		case GRShuttleState.PreMove:
			if (timeAsDouble > stateStartTime + 1.0)
			{
				SetState(GRShuttleState.Moving);
			}
			break;
		case GRShuttleState.Moving:
			if (timeAsDouble > stateStartTime + 5.0)
			{
				SetState(GRShuttleState.PostMove);
			}
			break;
		case GRShuttleState.PostMove:
			if (timeAsDouble > stateStartTime + 1.0)
			{
				SetState(GRShuttleState.Docked);
			}
			break;
		case GRShuttleState.Arriving:
			if (timeAsDouble > stateStartTime + 2.0)
			{
				SetState(GRShuttleState.PostArrive);
			}
			break;
		case GRShuttleState.PostArrive:
			if (timeAsDouble > stateStartTime + 1.0)
			{
				SetState(GRShuttleState.Docked);
			}
			break;
		}
	}

	public void RequestArrival()
	{
		reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.ShuttleArrive, shuttleId);
	}

	private void StartMoveFx()
	{
		if (windowFx != null)
		{
			windowFx.Play();
		}
		for (int i = 0; i < hideOnMove.Count; i++)
		{
			hideOnMove[i].SetActive(value: false);
		}
		for (int j = 0; j < showOnMove.Count; j++)
		{
			showOnMove[j].SetActive(value: true);
		}
	}

	private void StopMoveFx()
	{
		if (windowFx != null)
		{
			windowFx.Stop();
		}
		for (int i = 0; i < hideOnMove.Count; i++)
		{
			hideOnMove[i].SetActive(value: true);
		}
		for (int j = 0; j < showOnMove.Count; j++)
		{
			showOnMove[j].SetActive(value: false);
		}
	}

	public bool IsPodUnlocked()
	{
		if (specificDestinationShuttle != null)
		{
			return true;
		}
		if (shuttleOwner == null)
		{
			return false;
		}
		GRPlayer gRPlayer = GRPlayer.Get(shuttleOwner);
		if (gRPlayer == null)
		{
			return false;
		}
		return gRPlayer.IsDropPodUnlocked();
	}

	public int GetMaxDropFloor()
	{
		if (shuttleOwner == null)
		{
			return 0;
		}
		GRPlayer gRPlayer = GRPlayer.Get(shuttleOwner);
		if (gRPlayer == null)
		{
			return 0;
		}
		return gRPlayer.GetMaxDropFloor();
	}

	public void OnShuttleMove()
	{
		if (state == GRShuttleState.Docked)
		{
			reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.ShuttleLaunch, shuttleId);
		}
	}

	public void OnShuttleMoveActorNr(int actorNr)
	{
		if (state != GRShuttleState.Docked || actorNr != shuttleOwner.ActorNumber || GetTargetFloor() > GetMaxDropFloor())
		{
			departCardScanner.onFailed.Invoke();
			return;
		}
		departCardScanner.onSucceeded.Invoke();
		reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.ShuttleLaunch, shuttleId);
	}

	public void TargetLevelUp()
	{
		if (state == GRShuttleState.Docked)
		{
			reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.ShuttleTargetLevelUp, shuttleId);
		}
	}

	public void TargetLevelDown()
	{
		if (state == GRShuttleState.Docked)
		{
			reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.ShuttleTargetLevelDown, shuttleId);
		}
	}

	private GRShuttle GetTargetShuttle()
	{
		if (specificDestinationShuttle != null)
		{
			return specificDestinationShuttle;
		}
		if (shuttleOwner == null)
		{
			return null;
		}
		GRShuttle drillShuttleForPlayer = GRElevatorManager._instance.GetDrillShuttleForPlayer(shuttleOwner.ActorNumber);
		GRShuttle stagingShuttleForPlayer = GRElevatorManager._instance.GetStagingShuttleForPlayer(shuttleOwner.ActorNumber);
		if (location != GRShuttleGroupLoc.Drill)
		{
			return drillShuttleForPlayer;
		}
		return stagingShuttleForPlayer;
	}

	public bool IsPlayerOwner(GRPlayer player)
	{
		return GRPlayer.Get(GetOwner()) == player;
	}

	public bool IsShuttleInteractableByPlayer(GRPlayer player, bool ignoreOwnership)
	{
		if (!ignoreOwnership && !IsPlayerOwner(player) && specificDestinationShuttle == null)
		{
			return false;
		}
		if (entryCardScanner == null)
		{
			return true;
		}
		if (departCardScanner == null)
		{
			return true;
		}
		bool num = GameEntityManager.IsPlayerHandNearPosition(player.gamePlayer, entryCardScanner.transform.position, isLeftHand: false, checkBothHands: true);
		bool flag = GameEntityManager.IsPlayerHandNearPosition(player.gamePlayer, departCardScanner.transform.position, isLeftHand: false, checkBothHands: true);
		return num || flag;
	}

	public bool IsPlayerOwner(NetPlayer player)
	{
		return GetOwner() == player;
	}

	public void ToggleDoor()
	{
		if (state != GRShuttleState.Docked)
		{
			return;
		}
		if (entryDoor.doorState == GRDoor.DoorState.Closed)
		{
			reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.ShuttleOpen, shuttleId);
		}
		else if (entryDoor.doorState == GRDoor.DoorState.Open)
		{
			double timeAsDouble = Time.timeAsDouble;
			if (timeAsDouble > lastCloseTime + 5.0)
			{
				lastCloseTime = timeAsDouble;
				reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.ShuttleClose, shuttleId);
			}
		}
	}

	public void ToggleDoorActorNr(int actorNr)
	{
		if (state != GRShuttleState.Docked || GetOwner() == null || GetOwner().ActorNumber != actorNr || !GRPlayer.Get(shuttleOwner).IsDropPodUnlocked())
		{
			entryCardScanner?.onFailed.Invoke();
			return;
		}
		entryCardScanner?.onSucceeded.Invoke();
		ToggleDoor();
	}

	public void EmergencyOpenDoor()
	{
		if (state == GRShuttleState.Docked)
		{
			if (PhotonNetwork.InRoom)
			{
				reactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.ShuttleOpen, shuttleId);
			}
			else
			{
				OpenDoorLocal();
			}
		}
	}

	public void OnOpenDoor()
	{
		if (entryDoor.doorState == GRDoor.DoorState.Closed && entryCardScanner != null)
		{
			entryCardScanner.onSucceeded.Invoke();
		}
		OpenDoorLocal();
	}

	public void OpenDoorLocal()
	{
		if (entryDoor != null && entryDoor.doorState == GRDoor.DoorState.Closed)
		{
			entryDoor.SetDoorState(GRDoor.DoorState.Open);
		}
		if (shuttleBay != null)
		{
			shuttleBay.SetOpen(open: true);
		}
	}

	public void CloseDoorLocal()
	{
		if (entryDoor != null && entryDoor.doorState == GRDoor.DoorState.Open)
		{
			entryDoor.SetDoorState(GRDoor.DoorState.Closed);
		}
	}

	public void OnCloseDoor()
	{
		if (entryDoor.doorState == GRDoor.DoorState.Open && entryCardScanner != null)
		{
			entryCardScanner.onSucceeded.Invoke();
		}
		CloseDoorLocal();
	}

	public void OnLaunch()
	{
		if (GetTargetFloor() <= GetMaxDropFloor())
		{
			SetState(GRShuttleState.PreMove);
			if (departCardScanner != null)
			{
				departCardScanner.onSucceeded.Invoke();
			}
		}
	}

	public void OnArrive()
	{
		SetState(GRShuttleState.Arriving);
	}

	public void OnTargetLevelUp()
	{
		targetSection = ClampTargetSection(targetSection - 1);
		if (shuttleUI != null)
		{
			shuttleUI.RefreshUI();
		}
	}

	public void OnTargetLevelDown()
	{
		targetSection = ClampTargetSection(targetSection + 1);
		if (shuttleUI != null)
		{
			shuttleUI.RefreshUI();
		}
	}

	private int ClampTargetSection(int newTargetSection)
	{
		newTargetSection = ((location == GRShuttleGroupLoc.Staging) ? Mathf.Clamp(newTargetSection, 1, sectionFloors.Length - 1) : 0);
		return newTargetSection;
	}

	public static void TryStartLocalPlayerShuttleMove(int currShuttleId, NetPlayer shuttleOwner)
	{
		GRPlayer local = GRPlayer.GetLocal();
		if (local == null)
		{
			return;
		}
		GRShuttle shuttle = GRElevatorManager.GetShuttle(currShuttleId);
		if (!(shuttle == null) && GRElevatorManager.IsPlayerInShuttle(local.gamePlayer.rig.OwningNetPlayer.ActorNumber, shuttle, null))
		{
			if (shuttleOwner != null && shuttleOwner.GetPlayerRef() != null)
			{
				local.shuttleData.ownerUserId = shuttleOwner.UserId;
			}
			else
			{
				local.shuttleData.ownerUserId = VRRig.LocalRig.OwningNetPlayer.UserId;
			}
			local.shuttleData.currShuttleId = currShuttleId;
			local.shuttleData.targetShuttleId = -1;
			local.shuttleData.targetLevel = shuttle.GetTargetFloor();
			SetPlayerShuttleState(local, GRPlayer.ShuttleState.Moving);
		}
	}

	public static void UpdateGRPlayerShuttle(GRPlayer player)
	{
		if (player == null)
		{
			return;
		}
		GRPlayer.ShuttleData shuttleData = player.shuttleData;
		if (shuttleData == null || shuttleData.state == GRPlayer.ShuttleState.Idle || !player.gamePlayer.IsLocal())
		{
			return;
		}
		double timeAsDouble = Time.timeAsDouble;
		double num = shuttleData.stateStartTime;
		if (shuttleData.state != GRPlayer.ShuttleState.Idle && timeAsDouble > num + 10.0)
		{
			CancelPlayerShuttle(player);
			return;
		}
		switch (shuttleData.state)
		{
		case GRPlayer.ShuttleState.Moving:
			if (timeAsDouble > num + 3.0)
			{
				SetPlayerShuttleState(player, GRPlayer.ShuttleState.JoinRoom);
			}
			break;
		case GRPlayer.ShuttleState.JoinRoom:
			if (NetworkSystem.Instance.SessionIsPrivate)
			{
				SetPlayerShuttleState(player, GRPlayer.ShuttleState.WaitForLeadPlayer);
			}
			else
			{
				SetPlayerShuttleState(player, GRPlayer.ShuttleState.WaitForLeaveRoom);
			}
			break;
		case GRPlayer.ShuttleState.WaitForLeaveRoom:
			if (!PhotonNetwork.InRoom)
			{
				SetPlayerShuttleState(player, GRPlayer.ShuttleState.WaitForLeadPlayer);
			}
			break;
		case GRPlayer.ShuttleState.WaitForLeadPlayer:
			player.shuttleData.targetShuttleId = -1;
			if (PhotonNetwork.InRoom)
			{
				player.shuttleData.targetShuttleId = CalcTargetShuttleId(player.shuttleData.currShuttleId, player.shuttleData.ownerUserId);
			}
			if (player.shuttleData.targetShuttleId != -1)
			{
				SetPlayerShuttleState(player, GRPlayer.ShuttleState.Teleport);
			}
			break;
		case GRPlayer.ShuttleState.Teleport:
		{
			GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(GRElevatorManager.GetShuttle(player.shuttleData.targetShuttleId).zone);
			if (timeAsDouble > num + 1.0 && (managerForZone == null || managerForZone.IsZoneActive()))
			{
				int num2 = CalcTargetShuttleId(player.shuttleData.currShuttleId, player.shuttleData.ownerUserId);
				if (num2 == player.shuttleData.targetShuttleId)
				{
					SetPlayerShuttleState(player, GRPlayer.ShuttleState.PostTeleport);
				}
				else if (num2 != -1)
				{
					player.shuttleData.currShuttleId = player.shuttleData.targetShuttleId;
					player.shuttleData.targetShuttleId = num2;
					SetPlayerShuttleState(player, GRPlayer.ShuttleState.TeleportToMyShuttleSafety);
				}
			}
			break;
		}
		case GRPlayer.ShuttleState.TeleportToMyShuttleSafety:
			SetPlayerShuttleState(player, GRPlayer.ShuttleState.PostTeleport);
			break;
		case GRPlayer.ShuttleState.PostTeleport:
			if (timeAsDouble > num + 1.0)
			{
				SetPlayerShuttleState(player, GRPlayer.ShuttleState.Idle);
			}
			break;
		}
	}

	public static int CalcTargetShuttleId(int currShuttleId, string ownerUserId)
	{
		GRShuttle shuttle = GRElevatorManager.GetShuttle(currShuttleId);
		if (shuttle.specificDestinationShuttle != null)
		{
			return shuttle.specificDestinationShuttle.shuttleId;
		}
		GRPlayer fromUserId = GRPlayer.GetFromUserId(ownerUserId);
		if (fromUserId != null)
		{
			bool isOnDrillovator = shuttle.GetTargetFloor() >= 0;
			GRShuttle assignedShuttle = fromUserId.GetAssignedShuttle(isOnDrillovator);
			if (assignedShuttle != null)
			{
				return assignedShuttle.shuttleId;
			}
		}
		return -1;
	}

	public static void CancelPlayerShuttle(GRPlayer player)
	{
		switch (player.shuttleData.state)
		{
		case GRPlayer.ShuttleState.Moving:
		case GRPlayer.ShuttleState.WaitForLeaveRoom:
		case GRPlayer.ShuttleState.JoinRoom:
		case GRPlayer.ShuttleState.WaitForLeadPlayer:
		{
			GRShuttle shuttle2 = GRElevatorManager.GetShuttle(player.shuttleData.currShuttleId);
			if (shuttle2 != null)
			{
				shuttle2.OpenDoorLocal();
			}
			break;
		}
		case GRPlayer.ShuttleState.Teleport:
		case GRPlayer.ShuttleState.TeleportToMyShuttleSafety:
		case GRPlayer.ShuttleState.PostTeleport:
		{
			GRShuttle shuttle = GRElevatorManager.GetShuttle(player.shuttleData.targetShuttleId);
			if (shuttle != null)
			{
				shuttle.OpenDoorLocal();
			}
			break;
		}
		}
		SetPlayerShuttleState(player, GRPlayer.ShuttleState.Idle);
	}

	public static void SetPlayerShuttleState(GRPlayer player, GRPlayer.ShuttleState newState)
	{
		GRPlayer.ShuttleData shuttleData = player.shuttleData;
		shuttleData.state = newState;
		shuttleData.stateStartTime = Time.timeAsDouble;
		switch (shuttleData.state)
		{
		case GRPlayer.ShuttleState.JoinRoom:
		{
			GRShuttle shuttle6 = GRElevatorManager.GetShuttle(player.shuttleData.currShuttleId);
			GRShuttle targetShuttle = shuttle6.GetTargetShuttle();
			if (targetShuttle != null && !NetworkSystem.Instance.SessionIsPrivate && shuttle6.shuttleOwner.ActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
			{
				GRElevatorManager.LeadShuttleJoin(shuttle6.friendCollider, targetShuttle.friendCollider, targetShuttle.joinTrigger, shuttle6.GetTargetFloor());
			}
			break;
		}
		case GRPlayer.ShuttleState.Teleport:
		{
			GRShuttle shuttle2 = GRElevatorManager.GetShuttle(player.shuttleData.currShuttleId);
			GRShuttle shuttle3 = GRElevatorManager.GetShuttle(player.shuttleData.targetShuttleId);
			if (shuttle3 != null)
			{
				TeleportLocalPlayer(shuttle2, shuttle3);
				shuttle3.CloseDoorLocal();
			}
			break;
		}
		case GRPlayer.ShuttleState.TeleportToMyShuttleSafety:
		{
			GRShuttle shuttle4 = GRElevatorManager.GetShuttle(player.shuttleData.currShuttleId);
			GRShuttle shuttle5 = GRElevatorManager.GetShuttle(player.shuttleData.targetShuttleId);
			if (shuttle5 != null)
			{
				TeleportLocalPlayer(shuttle4, shuttle5);
				shuttle5.CloseDoorLocal();
			}
			break;
		}
		case GRPlayer.ShuttleState.PostTeleport:
		{
			GRShuttle shuttle = GRElevatorManager.GetShuttle(player.shuttleData.targetShuttleId);
			if (shuttle != null)
			{
				shuttle.RequestArrival();
			}
			break;
		}
		case GRPlayer.ShuttleState.Moving:
		case GRPlayer.ShuttleState.WaitForLeaveRoom:
		case GRPlayer.ShuttleState.WaitForLeadPlayer:
			break;
		}
	}
}
