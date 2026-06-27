using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Video;

[NetworkBehaviourWeaved(0)]
public class GRElevatorManager : NetworkComponent, ITickSystemTick
{
	[Serializable]
	public class GRShuttleGroup
	{
		public GRShuttleGroupLoc location;

		public List<GRShuttle> ghostReactorStagingShuttles;
	}

	public enum ElevatorSystemState
	{
		Dormant,
		InLocation,
		DestinationPressed,
		WaitingToTeleport,
		Teleporting,
		None
	}

	public enum RPC
	{
		RemoteElevatorButtonPress,
		RemoteActivateTeleport
	}

	public enum ElevatorLocation
	{
		Stump,
		City,
		GhostReactor,
		MonkeBlocks,
		VIMExperience1,
		VIMExperience2,
		VIMExperience3,
		VIMExperience4,
		None
	}

	[Serializable]
	public struct DestinationVideo
	{
		public ElevatorLocation Destination;

		public VideoClip VideoClip;
	}

	public PhotonView photonView;

	public static GRElevatorManager _instance;

	public Dictionary<ElevatorLocation, GRElevator> elevatorByLocation;

	public List<GRElevator> allElevators;

	[SerializeField]
	private ElevatorLocation destination;

	[SerializeField]
	private ElevatorLocation currentLocation;

	private ElevatorLocation lastTeleportSource = ElevatorLocation.None;

	public ElevatorSystemState currentState;

	private double timeLastTeleported;

	private bool cosmeticsInitialized;

	[SerializeField]
	private List<GRShuttleGroup> shuttleGroups;

	public GRShuttle mainStagingShuttle;

	public GRShuttle mainDrillShuttle;

	private List<GRShuttle> allShuttles;

	public float destinationButtonlastPressedDelay = 3f;

	public float doorsFullyClosedDelay = 3f;

	public float doorMaxClosingDelay = 12f;

	public double destinationButtonLastPressedTime;

	public double doorsFullyClosedTime;

	public double maxDoorClosingTime;

	private List<int> actorIds;

	public CallLimitersList<CallLimiter, RPC> m_RpcSpamChecks = new CallLimitersList<CallLimiter, RPC>();

	private bool justTeleported;

	private bool waitingForRemoteTeleport;

	private int lastLowestActorNr;

	private RaycastHit[] correctionRaycastHit = new RaycastHit[1];

	public LayerMask correctionRaycastMask;

	private float waitForZoneLoadFallbackTimer;

	public float waitForZoneLoadFallbackMaxTime = 5f;

	[SerializeField]
	private DestinationVideo[] DestinationVideos;

	[SerializeField]
	private VideoPlayer DestinationVideoPlayer;

	[SerializeField]
	private AudioSource DestinationVideoPlayerAudioSource;

	public bool InPrivateRoom => NetworkSystem.Instance.SessionIsPrivate;

	public bool TickRunning { get; set; }

	protected override void Awake()
	{
		base.Awake();
		if (_instance != null)
		{
			Debug.LogError("Multiple elevator managers! This should never happen!");
			return;
		}
		_instance = this;
		currentState = ElevatorSystemState.InLocation;
		currentLocation = ElevatorLocation.Stump;
		destination = ElevatorLocation.Stump;
		elevatorByLocation = new Dictionary<ElevatorLocation, GRElevator>();
		for (int i = 0; i < allElevators.Count; i++)
		{
			elevatorByLocation[allElevators[i].location] = allElevators[i];
		}
		actorIds = new List<int>();
		mainStagingShuttle.specificFloor = -1;
		mainDrillShuttle.specificFloor = 0;
		allShuttles = new List<GRShuttle>(64);
		for (int j = 0; j < shuttleGroups.Count; j++)
		{
			GRShuttleGroup gRShuttleGroup = shuttleGroups[j];
			for (int k = 0; k < gRShuttleGroup.ghostReactorStagingShuttles.Count; k++)
			{
				allShuttles.Add(gRShuttleGroup.ghostReactorStagingShuttles[k]);
				gRShuttleGroup.ghostReactorStagingShuttles[k].SetLocation(gRShuttleGroup.location);
			}
		}
		allShuttles.Add(mainStagingShuttle);
		allShuttles.Add(mainDrillShuttle);
		for (int l = 0; l < allShuttles.Count; l++)
		{
			allShuttles[l].Init(l);
		}
	}

	protected override void Start()
	{
		base.Start();
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnLeftRoom);
		NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(OnPlayerAdded);
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerRemoved);
	}

	protected void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnLeftRoom);
		NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(OnPlayerAdded);
		NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerRemoved);
	}

	private new void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		TickSystem<object>.AddTickCallback(this);
		DestinationVideoPlayer.loopPointReached += DisableVideoScreens;
	}

	private new void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
		DestinationVideoPlayer.loopPointReached -= DisableVideoScreens;
	}

	private void DisableVideoScreens(VideoPlayer source)
	{
		for (int i = 0; i < allElevators.Count; i++)
		{
			allElevators[i].videoDisplay.SetActive(value: false);
		}
	}

	public void Tick()
	{
		if (!cosmeticsInitialized)
		{
			CheckInitializationState();
			return;
		}
		for (int i = 0; i < allElevators.Count; i++)
		{
			allElevators[i].PhysicalElevatorUpdate();
		}
		ProcessElevatorSystemState();
		if (justTeleported)
		{
			justTeleported = false;
			GTPlayer.Instance.disableMovement = false;
		}
	}

	private void CheckInitializationState()
	{
		cosmeticsInitialized = true;
		if (InControlOfElevator())
		{
			UpdateElevatorState(ElevatorSystemState.InLocation, ElevatorLocation.Stump);
		}
	}

	public void ProcessElevatorSystemState()
	{
		switch (currentState)
		{
		case ElevatorSystemState.DestinationPressed:
			if (InControlOfElevator())
			{
				double time = GetTime();
				if (elevatorByLocation[currentLocation].DoorsFullyClosed() && time >= doorsFullyClosedTime + (double)doorsFullyClosedDelay)
				{
					UpdateElevatorState(ElevatorSystemState.WaitingToTeleport);
				}
				else if (time >= destinationButtonLastPressedTime + (double)destinationButtonlastPressedDelay && !elevatorByLocation[currentLocation].DoorIsClosing())
				{
					destinationButtonLastPressedTime = time;
					CloseAllElevators();
				}
			}
			break;
		case ElevatorSystemState.WaitingToTeleport:
			if (InControlOfElevator() && GetTime() >= doorsFullyClosedTime + (double)doorsFullyClosedDelay && !waitingForRemoteTeleport)
			{
				ActivateElevating();
			}
			break;
		case ElevatorSystemState.InLocation:
			if (currentLocation == destination && waitForZoneLoadFallbackTimer >= 0f && elevatorByLocation[currentLocation].DoorIsClosing())
			{
				waitForZoneLoadFallbackTimer += Time.deltaTime;
				if (waitForZoneLoadFallbackTimer >= waitForZoneLoadFallbackMaxTime)
				{
					OnReachedDestination();
				}
			}
			break;
		case ElevatorSystemState.Dormant:
			break;
		}
	}

	public void ActivateElevating()
	{
		if (PhotonNetwork.InRoom)
		{
			photonView.RPC("RemoteActivateTeleport", RpcTarget.All, (int)currentLocation, (int)destination, LowestActorNumberInElevator());
		}
		else
		{
			ActivateTeleport(currentLocation, destination, -1, GetTime());
		}
	}

	public void LeadElevatorJoin()
	{
		LeadElevatorJoin(elevatorByLocation[currentLocation].friendCollider, elevatorByLocation[destination].friendCollider, elevatorByLocation[destination].joinTrigger);
	}

	public static void LeadElevatorJoin(GorillaFriendCollider sourceFriendCollider, GorillaFriendCollider destinationFriendCollider, GorillaNetworkJoinTrigger destinationJoinTrigger)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			sourceFriendCollider.RefreshPlayersWithinBounds();
			destinationFriendCollider.RefreshPlayersWithinBounds();
			PhotonNetworkController.Instance.FriendIDList = new List<string>(sourceFriendCollider.playerIDsCurrentlyTouching);
			PhotonNetworkController.Instance.FriendIDList.AddRange(destinationFriendCollider.playerIDsCurrentlyTouching);
			foreach (string friendID in PhotonNetworkController.Instance.FriendIDList)
			{
				_ = friendID;
			}
			PhotonNetworkController.Instance.shuffler = UnityEngine.Random.Range(0, 99).ToString().PadLeft(2, '0') + UnityEngine.Random.Range(0, 99999999).ToString().PadLeft(8, '0');
			PhotonNetworkController.Instance.keyStr = UnityEngine.Random.Range(0, 99999999).ToString().PadLeft(8, '0');
			RoomSystem.SendElevatorFollowCommand(PhotonNetworkController.Instance.shuffler, PhotonNetworkController.Instance.keyStr, sourceFriendCollider, destinationFriendCollider);
			PhotonNetwork.SendAllOutgoingCommands();
			PhotonNetworkController.Instance.AttemptToJoinPublicRoom(destinationJoinTrigger, JoinType.JoinWithElevator);
		}
		JoinPublicRoom();
	}

	public static void LeadShuttleJoin(GorillaFriendCollider sourceFriendCollider, GorillaFriendCollider destinationFriendCollider, GorillaNetworkJoinTrigger destinationJoinTrigger, int targetLevel)
	{
		sourceFriendCollider.RefreshPlayersWithinBounds();
		destinationFriendCollider.RefreshPlayersWithinBounds();
		GorillaComputer.instance.friendJoinCollider = destinationFriendCollider;
		GorillaComputer.instance.UpdateScreen();
		if (NetworkSystem.Instance.InRoom)
		{
			PhotonNetworkController.Instance.FriendIDList = new List<string>(sourceFriendCollider.playerIDsCurrentlyTouching);
			PhotonNetworkController.Instance.FriendIDList.AddRange(destinationFriendCollider.playerIDsCurrentlyTouching);
			foreach (string friendID in PhotonNetworkController.Instance.FriendIDList)
			{
				_ = friendID;
			}
			PhotonNetworkController.Instance.shuffler = UnityEngine.Random.Range(0, 99).ToString().PadLeft(2, '0') + UnityEngine.Random.Range(0, 99999999).ToString().PadLeft(8, '0');
			PhotonNetworkController.Instance.keyStr = UnityEngine.Random.Range(0, 99999999).ToString().PadLeft(8, '0');
			RoomSystem.SendShuttleFollowCommand(PhotonNetworkController.Instance.shuffler, PhotonNetworkController.Instance.keyStr, sourceFriendCollider, destinationFriendCollider);
			PhotonNetwork.SendAllOutgoingCommands();
			List<(string, string)> additionalCustomProperties = null;
			if (targetLevel >= 0)
			{
				int joinDepthSectionFromLevel = GhostReactor.GetJoinDepthSectionFromLevel(targetLevel);
				additionalCustomProperties = new List<(string, string)> { ("ghostReactorDepth", joinDepthSectionFromLevel.ToString()) };
			}
			PhotonNetworkController.Instance.AttemptToJoinPublicRoom(destinationJoinTrigger, JoinType.JoinWithElevator, additionalCustomProperties);
		}
		PhotonNetworkController.Instance.AttemptToJoinPublicRoom(destinationJoinTrigger);
	}

	public void UpdateElevatorState(ElevatorSystemState newState, ElevatorLocation location = ElevatorLocation.None)
	{
		switch (currentState)
		{
		case ElevatorSystemState.Dormant:
			switch (newState)
			{
			case ElevatorSystemState.DestinationPressed:
			case ElevatorSystemState.WaitingToTeleport:
				maxDoorClosingTime = GetTime();
				destinationButtonLastPressedTime = GetTime();
				doorsFullyClosedTime = GetTime();
				if (destination != currentLocation)
				{
					destination = location;
					PlayDestinationVideo(destination);
				}
				elevatorByLocation[currentLocation].PlayElevatorMoving();
				elevatorByLocation[destination].PlayElevatorMoving();
				break;
			case ElevatorSystemState.InLocation:
				elevatorByLocation[currentLocation].PlayDing();
				OpenElevator(destination);
				break;
			}
			break;
		case ElevatorSystemState.DestinationPressed:
			switch (newState)
			{
			case ElevatorSystemState.Dormant:
				CloseAllElevators();
				break;
			case ElevatorSystemState.DestinationPressed:
				if (location != currentLocation)
				{
					destination = location;
					PlayDestinationVideo(destination);
				}
				break;
			case ElevatorSystemState.WaitingToTeleport:
				doorsFullyClosedTime = GetTime();
				if (currentLocation != ElevatorLocation.None)
				{
					elevatorByLocation[currentLocation].PlayElevatorMoving();
					elevatorByLocation[currentLocation].PlayElevatorMusic();
				}
				elevatorByLocation[destination].PlayElevatorMoving();
				break;
			case ElevatorSystemState.InLocation:
				OpenElevator(location);
				elevatorByLocation[currentLocation].PlayDing();
				break;
			}
			break;
		case ElevatorSystemState.WaitingToTeleport:
			switch (newState)
			{
			case ElevatorSystemState.Dormant:
				CloseAllElevators();
				elevatorByLocation[currentLocation].PlayElevatorStopped();
				elevatorByLocation[destination].PlayElevatorStopped();
				break;
			case ElevatorSystemState.DestinationPressed:
			case ElevatorSystemState.WaitingToTeleport:
				if (location != currentLocation)
				{
					destination = location;
					PlayDestinationVideo(destination);
				}
				else
				{
					OpenElevator(location);
					newState = ElevatorSystemState.InLocation;
				}
				break;
			case ElevatorSystemState.InLocation:
			{
				ZoneManagement instance = ZoneManagement.instance;
				instance.OnSceneLoadsCompleted = (Action)Delegate.Combine(instance.OnSceneLoadsCompleted, new Action(OnReachedDestination));
				waitForZoneLoadFallbackTimer = 0.01f;
				elevatorByLocation[currentLocation].PlayElevatorStopped();
				currentLocation = location;
				break;
			}
			}
			break;
		case ElevatorSystemState.InLocation:
			switch (newState)
			{
			case ElevatorSystemState.Dormant:
				CloseAllElevators();
				break;
			case ElevatorSystemState.DestinationPressed:
				if (location != currentLocation)
				{
					destination = location;
					destinationButtonLastPressedTime = GetTime();
					maxDoorClosingTime = GetTime();
					PlayDestinationVideo(destination);
				}
				else
				{
					if (elevatorByLocation[destination].DoorIsClosing())
					{
						OpenElevator(currentLocation);
					}
					newState = currentState;
				}
				if (currentLocation != ElevatorLocation.None)
				{
					elevatorByLocation[currentLocation].PlayElevatorMoving();
				}
				elevatorByLocation[destination].PlayElevatorMoving();
				break;
			case ElevatorSystemState.WaitingToTeleport:
				if (currentLocation != ElevatorLocation.None)
				{
					elevatorByLocation[currentLocation].PlayElevatorMoving();
				}
				elevatorByLocation[destination].PlayElevatorMoving();
				break;
			case ElevatorSystemState.InLocation:
				if (location == currentLocation)
				{
					OpenElevator(currentLocation);
				}
				else
				{
					CloseAllElevators();
				}
				break;
			}
			break;
		}
		currentState = newState;
		UpdateUI();
	}

	private void PlayDestinationVideo(ElevatorLocation destination)
	{
		VideoClip clipForDestination = getClipForDestination(destination);
		if (DestinationVideoPlayer.isPlaying && DestinationVideoPlayer.clip != clipForDestination)
		{
			DestinationVideoPlayer.Stop();
			DisableVideoScreens(DestinationVideoPlayer);
		}
		if (clipForDestination != null && currentLocation != ElevatorLocation.None)
		{
			DestinationVideoPlayer.clip = clipForDestination;
			DestinationVideoPlayer.SetTargetAudioSource(0, DestinationVideoPlayerAudioSource);
			DestinationVideoPlayer.Play();
			DestinationVideoPlayerAudioSource.transform.position = elevatorByLocation[currentLocation].videoAudio.transform.position;
			elevatorByLocation[currentLocation].videoDisplay.SetActive(value: true);
		}
	}

	private VideoClip getClipForDestination(ElevatorLocation destination)
	{
		for (int i = 0; i < DestinationVideos.Length; i++)
		{
			if (DestinationVideos[i].Destination == destination)
			{
				return DestinationVideos[i].VideoClip;
			}
		}
		return null;
	}

	public void UpdateUI()
	{
		for (int i = 0; i < allElevators.Count; i++)
		{
			allElevators[i].outerText.text = "ELEVATOR LOCATION:\n" + currentLocation.ToString().ToUpper();
			switch (currentState)
			{
			case ElevatorSystemState.Dormant:
			case ElevatorSystemState.InLocation:
				allElevators[i].innerText.text = "CHOOSE DESTINATION";
				break;
			case ElevatorSystemState.DestinationPressed:
			case ElevatorSystemState.WaitingToTeleport:
				if (destination != currentLocation)
				{
					allElevators[i].innerText.text = "NEXT STOP:\n" + destination.ToString().ToUpper();
				}
				else
				{
					allElevators[i].innerText.text = "CHOOSE DESTINATION";
				}
				break;
			}
		}
	}

	public static void RegisterElevator(GRElevator elevator)
	{
		if (!(_instance == null))
		{
			_instance.elevatorByLocation[elevator.location] = elevator;
		}
	}

	public static void DeregisterElevator(GRElevator elevator)
	{
		if (!(_instance == null))
		{
			_instance.elevatorByLocation[elevator.location] = null;
		}
	}

	public static void ElevatorButtonPressed(GRElevator.ButtonType type, ElevatorLocation location)
	{
		if (_instance != null)
		{
			_instance.ElevatorButtonPressedInternal(type, location);
			if (!_instance.IsMine && NetworkSystem.Instance.InRoom)
			{
				_instance.photonView.RPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, (int)type, (int)location);
			}
		}
	}

	private void ElevatorButtonPressedInternal(GRElevator.ButtonType type, ElevatorLocation location)
	{
		if (!elevatorByLocation.TryGetValue(location, out var value) || value == null)
		{
			Debug.LogWarning($"[GRElevatorManager] No elevator registered for location '{location}'. Elevator may not be enabled yet or is missing from allElevators.", this);
			return;
		}
		value.PressButtonVisuals(type);
		value.PlayButtonPress();
		if (base.IsMine)
		{
			ProcessElevatorButtonPress(type, location);
		}
	}

	public void ProcessElevatorButtonPress(GRElevator.ButtonType type, ElevatorLocation location)
	{
		switch (type)
		{
		case GRElevator.ButtonType.Stump:
			if (currentState != ElevatorSystemState.WaitingToTeleport)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, ElevatorLocation.Stump);
			}
			break;
		case GRElevator.ButtonType.City:
			if (currentState != ElevatorSystemState.WaitingToTeleport)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, ElevatorLocation.City);
			}
			break;
		case GRElevator.ButtonType.GhostReactor:
			if (currentState != ElevatorSystemState.WaitingToTeleport)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, ElevatorLocation.GhostReactor);
			}
			break;
		case GRElevator.ButtonType.MonkeBlocks:
			if (currentState != ElevatorSystemState.WaitingToTeleport)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, ElevatorLocation.MonkeBlocks);
			}
			break;
		case GRElevator.ButtonType.VIMExperience1:
			if (currentState != ElevatorSystemState.WaitingToTeleport)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, ElevatorLocation.VIMExperience1);
			}
			break;
		case GRElevator.ButtonType.VIMExperience2:
			if (currentState != ElevatorSystemState.WaitingToTeleport)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, ElevatorLocation.VIMExperience2);
			}
			break;
		case GRElevator.ButtonType.VIMExperience3:
			if (currentState != ElevatorSystemState.WaitingToTeleport)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, ElevatorLocation.VIMExperience3);
			}
			break;
		case GRElevator.ButtonType.VIMExperience4:
			if (currentState != ElevatorSystemState.WaitingToTeleport)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, ElevatorLocation.VIMExperience4);
			}
			break;
		case GRElevator.ButtonType.Summon:
			if (currentState != ElevatorSystemState.WaitingToTeleport && currentState != ElevatorSystemState.DestinationPressed)
			{
				UpdateElevatorState(ElevatorSystemState.DestinationPressed, location);
			}
			break;
		case GRElevator.ButtonType.Open:
			if (currentState == ElevatorSystemState.WaitingToTeleport)
			{
				break;
			}
			if (currentState == ElevatorSystemState.DestinationPressed)
			{
				if (GetTime() >= maxDoorClosingTime + (double)doorMaxClosingDelay)
				{
					break;
				}
				destinationButtonLastPressedTime = GetTime();
				doorsFullyClosedTime = GetTime();
			}
			OpenElevator(location);
			break;
		case GRElevator.ButtonType.Close:
			CloseAllElevators();
			break;
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(doorsFullyClosedTime);
		stream.SendNext(destinationButtonLastPressedTime);
		stream.SendNext(maxDoorClosingTime);
		stream.SendNext((int)currentLocation);
		stream.SendNext((int)destination);
		stream.SendNext((int)currentState);
		for (int i = 0; i < allElevators.Count; i++)
		{
			stream.SendNext((int)allElevators[i].state);
		}
		for (int j = 0; j < allShuttles.Count; j++)
		{
			stream.SendNext((byte)allShuttles[j].GetState());
			bool num = allShuttles[j].specificDestinationShuttle == null;
			NetPlayer owner = allShuttles[j].GetOwner();
			int num2 = ((!num || owner == null) ? (-1) : owner.ActorNumber);
			stream.SendNext(num2);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		double d = (double)stream.ReceiveNext();
		if (!double.IsNaN(d) && !double.IsInfinity(d))
		{
			doorsFullyClosedTime = d;
		}
		d = (double)stream.ReceiveNext();
		if (!double.IsNaN(d) && !double.IsInfinity(d))
		{
			destinationButtonLastPressedTime = d;
		}
		d = (double)stream.ReceiveNext();
		if (!double.IsNaN(d) && !double.IsInfinity(d))
		{
			maxDoorClosingTime = d;
		}
		_ = currentLocation;
		int num = (int)stream.ReceiveNext();
		if (num >= 0 && num <= 8)
		{
			currentLocation = (ElevatorLocation)num;
		}
		_ = destination;
		num = (int)stream.ReceiveNext();
		if (num >= 0 && num <= 8)
		{
			destination = (ElevatorLocation)num;
		}
		num = (int)stream.ReceiveNext();
		if (num >= 0 && num < 5)
		{
			ElevatorSystemState elevatorSystemState = (ElevatorSystemState)num;
			if (elevatorSystemState != currentState && elevatorSystemState == ElevatorSystemState.DestinationPressed)
			{
				PlayDestinationVideo(destination);
			}
			currentState = (ElevatorSystemState)num;
		}
		UpdateUI();
		for (int i = 0; i < allElevators.Count; i++)
		{
			num = (int)stream.ReceiveNext();
			if (num >= 0 && num < 8)
			{
				allElevators[i].UpdateRemoteState((GRElevator.ElevatorState)num);
			}
		}
		for (int j = 0; j < allShuttles.Count; j++)
		{
			byte b = (byte)stream.ReceiveNext();
			int num2 = (int)stream.ReceiveNext();
			if (b >= 0 && b < 7)
			{
				allShuttles[j].SetState((GRShuttleState)b);
			}
			if (allShuttles[j].specificDestinationShuttle == null && num2 != -1)
			{
				NetPlayer owner = NetPlayer.Get(num2);
				allShuttles[j].SetOwner(owner);
			}
		}
	}

	[PunRPC]
	public void RemoteElevatorButtonPress(int elevatorButtonPressed, int elevatorLocation, PhotonMessageInfo info)
	{
		if (base.IsMine && !m_RpcSpamChecks.IsSpamming(RPC.RemoteElevatorButtonPress) && elevatorLocation >= 0 && elevatorLocation < 8 && elevatorButtonPressed >= 0 && elevatorButtonPressed < 12)
		{
			ElevatorButtonPressedInternal((GRElevator.ButtonType)elevatorButtonPressed, (ElevatorLocation)elevatorLocation);
		}
	}

	[PunRPC]
	public void RemoteActivateTeleport(int elevatorStartLocation, int elevatorDestinationLocation, int lowestActorNumber, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient && !m_RpcSpamChecks.IsSpamming(RPC.RemoteActivateTeleport) && elevatorStartLocation >= 0 && elevatorStartLocation < 8 && elevatorDestinationLocation >= 0 && elevatorDestinationLocation < 8 && !waitingForRemoteTeleport)
		{
			StartCoroutine(TeleportDelay((ElevatorLocation)elevatorStartLocation, (ElevatorLocation)elevatorDestinationLocation, lowestActorNumber, info.SentServerTime));
		}
	}

	private IEnumerator TeleportDelay(ElevatorLocation start, ElevatorLocation destination, int lowestActorNumber, double sentServerTime)
	{
		timeLastTeleported = Time.time;
		waitingForRemoteTeleport = true;
		lastTeleportSource = start;
		yield return new WaitForSeconds((float)(PhotonNetwork.Time - (sentServerTime + 0.75)));
		RefreshTeleportingPlayersJoinTime();
		yield return new WaitForSeconds(0.25f);
		waitingForRemoteTeleport = false;
		ActivateTeleport(start, destination, lowestActorNumber, sentServerTime);
	}

	public void ActivateTeleport(ElevatorLocation start, ElevatorLocation destination, int lowestActorNumber, double photonServerTime)
	{
		GRElevator gRElevator = elevatorByLocation[start];
		GRElevator gRElevator2 = elevatorByLocation[destination];
		if (gRElevator == null || gRElevator2 == null)
		{
			return;
		}
		gRElevator.friendCollider.RefreshPlayersWithinBounds();
		if (!PhotonNetwork.InRoom)
		{
			RefreshTeleportingPlayersJoinTime();
		}
		if (!gRElevator.friendCollider.playerIDsCurrentlyTouching.Contains(NetworkSystem.Instance.LocalPlayer.UserId))
		{
			UpdateElevatorState(ElevatorSystemState.InLocation, destination);
			return;
		}
		elevatorByLocation[destination].collidersAndVisuals.SetActive(value: true);
		if (DestinationVideoPlayer.isPlaying)
		{
			elevatorByLocation[destination].videoDisplay.SetActive(value: true);
			DestinationVideoPlayerAudioSource.transform.position = elevatorByLocation[destination].videoAudio.transform.position;
		}
		float num = gRElevator2.transform.rotation.eulerAngles.y - gRElevator.transform.rotation.eulerAngles.y;
		GTPlayer instance = GTPlayer.Instance;
		VRRig localRig = VRRig.LocalRig;
		Vector3 vector = localRig.transform.position - instance.transform.position;
		Vector3 vector2 = instance.headCollider.transform.position - instance.transform.position;
		Vector3 vector3 = gRElevator2.transform.TransformPoint(gRElevator.transform.InverseTransformPoint(instance.transform.position));
		Vector3 vector4 = localRig.transform.position - gRElevator.transform.position;
		vector4.x *= 0.8f;
		vector4.z *= 0.8f;
		vector3 = gRElevator2.transform.position + (Quaternion.Euler(0f, num, 0f) * vector4 - vector) + localRig.headConstraint.rotation * localRig.head.trackingPositionOffset;
		Vector3 vector5 = Vector3.zero;
		Vector3 vector6 = gRElevator2.transform.position + (Quaternion.Euler(0f, num, 0f) * vector4 - vector) + vector2 - gRElevator2.transform.position;
		float magnitude = vector6.magnitude;
		vector6 = vector6.normalized;
		if (Physics.SphereCastNonAlloc(gRElevator2.transform.position, instance.headCollider.radius * 1.5f, vector6, correctionRaycastHit, magnitude * 1.05f, correctionRaycastMask) > 0)
		{
			vector5 = vector6 * instance.headCollider.radius * -1.5f;
		}
		instance.TeleportTo(vector3 + vector5, instance.transform.rotation);
		instance.turnParent.transform.RotateAround(instance.headCollider.transform.position, base.transform.up, num);
		localRig.transform.position = instance.transform.position + vector;
		instance.InitializeValues();
		justTeleported = true;
		instance.disableMovement = true;
		GorillaComputer.instance.allowedMapsToJoin = elevatorByLocation[destination].joinTrigger.myCollider.myAllowedMapsToJoin;
		lastTeleportSource = start;
		lastLowestActorNr = lowestActorNumber;
		if (!InPrivateRoom && lowestActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			LeadElevatorJoin();
		}
		UpdateElevatorState(ElevatorSystemState.InLocation, destination);
		gRElevator2.PlayElevatorMusic(gRElevator.musicAudio.time);
	}

	public void CloseAllElevators()
	{
		for (int i = 0; i < allElevators.Count; i++)
		{
			if (!allElevators[i].DoorIsClosing())
			{
				allElevators[i].UpdateLocalState(GRElevator.ElevatorState.DoorBeginClosing);
			}
		}
	}

	public void OpenElevator(ElevatorLocation location)
	{
		for (int i = 0; i < allElevators.Count; i++)
		{
			allElevators[i].UpdateLocalState((allElevators[i].location == location) ? GRElevator.ElevatorState.DoorBeginOpening : GRElevator.ElevatorState.DoorBeginClosing);
		}
	}

	public double GetTime()
	{
		double num = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		if (doorsFullyClosedTime > num || destinationButtonLastPressedTime > num || maxDoorClosingTime > num || num - doorsFullyClosedTime > 10.0 || num - destinationButtonLastPressedTime > 10.0 || num - maxDoorClosingTime > 20.0)
		{
			doorsFullyClosedTime = num;
			destinationButtonLastPressedTime = num;
			maxDoorClosingTime = num;
		}
		return num;
	}

	public static bool ValidElevatorNetworking(int actorNr)
	{
		if (_instance == null)
		{
			return false;
		}
		if (RoomSystem.WasRoomPrivate)
		{
			return false;
		}
		if (actorNr == _instance.lastLowestActorNr)
		{
			return true;
		}
		if (_instance.lastTeleportSource == ElevatorLocation.None)
		{
			return false;
		}
		GorillaFriendCollider friendCollider = _instance.elevatorByLocation[_instance.destination].friendCollider;
		GorillaFriendCollider friendCollider2 = _instance.elevatorByLocation[_instance.lastTeleportSource].friendCollider;
		if ((double)Time.time < _instance.timeLastTeleported + 3.0)
		{
			friendCollider.RefreshPlayersWithinBounds();
			friendCollider2.RefreshPlayersWithinBounds();
		}
		NetPlayer netPlayer = NetPlayer.Get(actorNr);
		if (netPlayer == null)
		{
			return false;
		}
		if (!friendCollider.playerIDsCurrentlyTouching.Contains(netPlayer.UserId))
		{
			return friendCollider2.playerIDsCurrentlyTouching.Contains(netPlayer.UserId);
		}
		return true;
	}

	public static bool ValidShuttleNetworking(int actorNr)
	{
		if (_instance == null)
		{
			return false;
		}
		if (RoomSystem.WasRoomPrivate)
		{
			return false;
		}
		GRPlayer gRPlayer = GRPlayer.Get(actorNr);
		if (gRPlayer == null)
		{
			return false;
		}
		GRShuttle shuttle = GetShuttle(gRPlayer.shuttleData.currShuttleId);
		GRShuttle shuttle2 = GetShuttle(gRPlayer.shuttleData.targetShuttleId);
		if (shuttle == null)
		{
			return false;
		}
		if (shuttle2 == null)
		{
			shuttle2 = GetShuttle(GRShuttle.CalcTargetShuttleId(gRPlayer.shuttleData.currShuttleId, gRPlayer.shuttleData.ownerUserId));
			if (shuttle2 == null)
			{
				return false;
			}
		}
		NetPlayer netPlayer = NetPlayer.Get(actorNr);
		if (netPlayer == null)
		{
			return false;
		}
		if (netPlayer == shuttle.GetOwner())
		{
			return true;
		}
		GorillaFriendCollider friendCollider = shuttle2.friendCollider;
		GorillaFriendCollider friendCollider2 = shuttle.friendCollider;
		friendCollider.RefreshPlayersWithinBounds();
		friendCollider2.RefreshPlayersWithinBounds();
		if (!friendCollider.playerIDsCurrentlyTouching.Contains(netPlayer.UserId))
		{
			return friendCollider2.playerIDsCurrentlyTouching.Contains(netPlayer.UserId);
		}
		return true;
	}

	public static bool IsPlayerInShuttle(int actorNr, GRShuttle currShuttle, GRShuttle targetShuttle)
	{
		if (_instance == null)
		{
			return false;
		}
		NetPlayer netPlayer = NetPlayer.Get(actorNr);
		if (netPlayer == null)
		{
			return false;
		}
		bool flag = false;
		if (currShuttle != null)
		{
			GorillaFriendCollider friendCollider = currShuttle.friendCollider;
			if (friendCollider != null)
			{
				friendCollider.RefreshPlayersWithinBounds();
			}
			flag = friendCollider.playerIDsCurrentlyTouching.Contains(netPlayer.UserId);
		}
		bool flag2 = false;
		if (targetShuttle != null)
		{
			GorillaFriendCollider friendCollider2 = targetShuttle.friendCollider;
			if (friendCollider2 != null)
			{
				friendCollider2.RefreshPlayersWithinBounds();
			}
			friendCollider2.playerIDsCurrentlyTouching.Contains(netPlayer.UserId);
		}
		return flag || flag2;
	}

	public static int LowestActorNumberInElevator()
	{
		GorillaFriendCollider friendCollider = _instance.elevatorByLocation[_instance.currentLocation].friendCollider;
		GorillaFriendCollider friendCollider2 = _instance.elevatorByLocation[_instance.destination].friendCollider;
		friendCollider.RefreshPlayersWithinBounds();
		friendCollider2.RefreshPlayersWithinBounds();
		int num = int.MaxValue;
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		for (int i = 0; i < allNetPlayers.Length; i++)
		{
			if (num > allNetPlayers[i].ActorNumber && (friendCollider.playerIDsCurrentlyTouching.Contains(allNetPlayers[i].UserId) || friendCollider2.playerIDsCurrentlyTouching.Contains(allNetPlayers[i].UserId)))
			{
				num = allNetPlayers[i].ActorNumber;
			}
		}
		return num;
	}

	public static int LowestActorNumberInElevator(GorillaFriendCollider sourceFriendCollider, GorillaFriendCollider destinationFriendCollider)
	{
		sourceFriendCollider.RefreshPlayersWithinBounds();
		destinationFriendCollider.RefreshPlayersWithinBounds();
		int num = int.MaxValue;
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		for (int i = 0; i < allNetPlayers.Length; i++)
		{
			if (num > allNetPlayers[i].ActorNumber && (sourceFriendCollider.playerIDsCurrentlyTouching.Contains(allNetPlayers[i].UserId) || destinationFriendCollider.playerIDsCurrentlyTouching.Contains(allNetPlayers[i].UserId)))
			{
				num = allNetPlayers[i].ActorNumber;
			}
		}
		return num;
	}

	private void RefreshTeleportingPlayersJoinTime()
	{
		GorillaFriendCollider friendCollider = _instance.elevatorByLocation[_instance.currentLocation].friendCollider;
		actorIds.Clear();
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		for (int i = 0; i < allNetPlayers.Length; i++)
		{
			if (friendCollider.playerIDsCurrentlyTouching.Contains(allNetPlayers[i].UserId) && VRRigCache.Instance.TryGetVrrig(allNetPlayers[i], out var playerRig))
			{
				playerRig.Rig.ResetTimeSpawned();
			}
		}
	}

	public static bool InControlOfElevator()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			return _instance.IsMine;
		}
		return true;
	}

	public static void JoinPublicRoom()
	{
		PhotonNetworkController.Instance.AttemptToJoinPublicRoom(_instance.elevatorByLocation[_instance.destination].joinTrigger);
	}

	public void OnReachedDestination()
	{
		ZoneManagement instance = ZoneManagement.instance;
		instance.OnSceneLoadsCompleted = (Action)Delegate.Remove(instance.OnSceneLoadsCompleted, new Action(OnReachedDestination));
		elevatorByLocation[destination].PlayElevatorStopped();
		if (currentLocation == destination)
		{
			OpenElevator(currentLocation);
			elevatorByLocation[currentLocation].PlayDing();
		}
		waitForZoneLoadFallbackTimer = -1f;
	}

	public static GRShuttle GetShuttle(int shuttleId)
	{
		if (_instance == null)
		{
			return null;
		}
		return _instance.GetShuttleById(shuttleId);
	}

	public void InitShuttles(GhostReactor reactor)
	{
		for (int i = 0; i < allShuttles.Count; i++)
		{
			allShuttles[i].SetReactor(reactor);
		}
	}

	public GRShuttle GetPlayerShuttle(GRShuttleGroupLoc shuttleGroupLoc, int shuttleIndex)
	{
		for (int i = 0; i < shuttleGroups.Count; i++)
		{
			if (shuttleGroups[i].location == shuttleGroupLoc)
			{
				if (shuttleIndex < 0 || shuttleIndex >= shuttleGroups[i].ghostReactorStagingShuttles.Count)
				{
					Debug.LogErrorFormat("Invalid Shuttle Index {0} of {1}", shuttleIndex, shuttleGroups[i].ghostReactorStagingShuttles.Count);
					return null;
				}
				return shuttleGroups[i].ghostReactorStagingShuttles[shuttleIndex];
			}
		}
		return null;
	}

	public GRShuttle GetDrillShuttleForPlayer(int actorNumber)
	{
		return GetShuttleForPlayer(actorNumber, GRShuttleGroupLoc.Drill);
	}

	public GRShuttle GetStagingShuttleForPlayer(int actorNumber)
	{
		return GetShuttleForPlayer(actorNumber, GRShuttleGroupLoc.Staging);
	}

	public GRShuttle GetShuttleForPlayer(int actorNumber, GRShuttleGroupLoc shuttleGroupLoc)
	{
		for (int i = 0; i < shuttleGroups.Count; i++)
		{
			if (shuttleGroups[i].location != shuttleGroupLoc)
			{
				continue;
			}
			for (int j = 0; j < shuttleGroups[i].ghostReactorStagingShuttles.Count; j++)
			{
				GRShuttle gRShuttle = shuttleGroups[i].ghostReactorStagingShuttles[j];
				if (!(gRShuttle == null))
				{
					NetPlayer owner = gRShuttle.GetOwner();
					if (owner != null && owner.ActorNumber == actorNumber)
					{
						return gRShuttle;
					}
				}
			}
		}
		return null;
	}

	public GRShuttle GetShuttleById(int shuttleId)
	{
		for (int i = 0; i < allShuttles.Count; i++)
		{
			if (allShuttles[i].shuttleId == shuttleId)
			{
				return allShuttles[i];
			}
		}
		return null;
	}

	private int AddPlayer(NetPlayer netPlayer)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return -1;
		}
		int num = -1;
		List<GRShuttle> ghostReactorStagingShuttles = shuttleGroups[0].ghostReactorStagingShuttles;
		for (int i = 0; i < ghostReactorStagingShuttles.Count; i++)
		{
			if (ghostReactorStagingShuttles[i].GetOwner() == null)
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			return -1;
		}
		for (int j = 0; j < shuttleGroups.Count; j++)
		{
			shuttleGroups[j].ghostReactorStagingShuttles[num].SetOwner(netPlayer);
		}
		return num;
	}

	private void RemovePlayer(NetPlayer netPlayer)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		int num = -1;
		List<GRShuttle> ghostReactorStagingShuttles = shuttleGroups[0].ghostReactorStagingShuttles;
		for (int i = 0; i < ghostReactorStagingShuttles.Count; i++)
		{
			if (ghostReactorStagingShuttles[i].GetOwner() == netPlayer)
			{
				num = i;
				break;
			}
		}
		if (num >= 0)
		{
			for (int j = 0; j < shuttleGroups.Count; j++)
			{
				shuttleGroups[j].ghostReactorStagingShuttles[num].SetOwner(null);
			}
		}
	}

	public void OnLeftRoom()
	{
		for (int i = 0; i < shuttleGroups.Count; i++)
		{
			for (int j = 0; j < shuttleGroups[i].ghostReactorStagingShuttles.Count; j++)
			{
				GRShuttle gRShuttle = shuttleGroups[i].ghostReactorStagingShuttles[j];
				if (!(gRShuttle == null))
				{
					gRShuttle.SetOwner(null);
				}
			}
		}
	}

	public void OnPlayerAdded(NetPlayer player)
	{
		if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
		{
			AddPlayer(player);
		}
	}

	public void OnPlayerRemoved(NetPlayer player)
	{
		if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
		{
			RemovePlayer(player);
		}
	}

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
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
