using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using GorillaGameModes;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GorillaTagCompetitiveManager : GorillaTagManager
{
	public enum GameState
	{
		None,
		WaitingForPlayers,
		StartingCountdown,
		Playing,
		PostRound
	}

	[SerializeField]
	private float startCountdownDuration = 3f;

	[SerializeField]
	private float roundDuration = 300f;

	[SerializeField]
	private float postRoundDuration = 15f;

	[SerializeField]
	private float waitingForPlayerPingRoomDuration = 60f;

	private GameState gameState;

	private float stateRemainingTime;

	private float lastActiveTime;

	private float lastWaitingForPlayerPingRoomTime;

	private RankedMultiplayerScore scoring;

	private List<GorillaTagCompetitiveForcedLeaveRoomVolume> forceLeaveRoomVolumes = new List<GorillaTagCompetitiveForcedLeaveRoomVolume>();

	private static List<GorillaTagCompetitiveScoreboard> scoreboards = new List<GorillaTagCompetitiveScoreboard>();

	public bool ShowDebugPing { get; set; }

	public static event Action<GameState> onStateChanged;

	public static event Action<float> onUpdateRemainingTime;

	public static event Action<NetPlayer> onPlayerJoined;

	public static event Action<NetPlayer> onPlayerLeft;

	public static event Action onRoundStart;

	public static event Action onRoundEnd;

	public static event Action<NetPlayer, NetPlayer> onTagOccurred;

	public float GetRoundDuration()
	{
		return roundDuration;
	}

	public GameState GetCurrentGameState()
	{
		return gameState;
	}

	public bool IsMatchActive()
	{
		return gameState == GameState.Playing;
	}

	public static void RegisterScoreboard(GorillaTagCompetitiveScoreboard scoreboard)
	{
		scoreboards.Add(scoreboard);
	}

	public static void DeregisterScoreboard(GorillaTagCompetitiveScoreboard scoreboard)
	{
		scoreboards.Remove(scoreboard);
	}

	public override void StartPlaying()
	{
		base.StartPlaying();
		scoring = GetComponentInChildren<RankedMultiplayerScore>();
		if (scoring != null)
		{
			scoring.Initialize();
		}
		VRRig.LocalRig.EnableRankedTimerWatch(on: true);
		for (int i = 0; i < currentNetPlayerArray.Length; i++)
		{
			if (VRRigCache.Instance.TryGetVrrig(currentNetPlayerArray[i], out var playerRig))
			{
				playerRig.Rig.EnableRankedTimerWatch(on: true);
			}
		}
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		VRRig.LocalRig.EnableRankedTimerWatch(on: false);
		if (scoring != null)
		{
			scoring.ResetMatch();
			scoring.Unsubscribe();
		}
		for (int i = 0; i < scoreboards.Count; i++)
		{
			scoreboards[i].UpdateScores(gameState, lastActiveTime, null, scoring.PlayerRankedTiers, scoring.ProjectedEloDeltas, currentInfected, scoring.Progression);
		}
	}

	public override void ResetGame()
	{
		base.ResetGame();
		gameState = GameState.None;
	}

	internal override void NetworkLinkSetup(GameModeSerializer netSerializer)
	{
		base.NetworkLinkSetup(netSerializer);
		netSerializer.AddRPCComponent<GorillaTagCompetitiveRPCs>();
	}

	public override void Tick()
	{
		if (stateRemainingTime > 0f)
		{
			stateRemainingTime -= Time.deltaTime;
			if (stateRemainingTime <= 0f)
			{
				UpdateState();
			}
			GorillaTagCompetitiveManager.onUpdateRemainingTime?.Invoke(stateRemainingTime);
		}
		base.Tick();
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (Time.time - lastWaitingForPlayerPingRoomTime > waitingForPlayerPingRoomDuration)
			{
				PingRoom();
				lastWaitingForPlayerPingRoomTime = Time.time;
			}
			if (Time.time - lastWaitingForPlayerPingRoomTime > 3f)
			{
				ShowDebugPing = false;
			}
		}
		UpdateScoreboards();
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		base.OnMasterClientSwitched(newMasterClient);
		if (NetworkSystem.Instance.IsMasterClient)
		{
			PingRoom();
			lastWaitingForPlayerPingRoomTime = Time.time;
		}
	}

	public override void OnPlayerEnteredRoom(NetPlayer newPlayer)
	{
		base.OnPlayerEnteredRoom(newPlayer);
		if (newPlayer == NetworkSystem.Instance.LocalPlayer)
		{
			foreach (GorillaTagCompetitiveForcedLeaveRoomVolume forceLeaveRoomVolume in forceLeaveRoomVolumes)
			{
				if (forceLeaveRoomVolume.ContainsPoint(VRRig.LocalRig.transform.position))
				{
					NetworkSystem.Instance.ReturnToSinglePlayer();
					return;
				}
			}
			object value;
			if (NetworkSystem.Instance.IsMasterClient)
			{
				GorillaTagCompetitiveServerApi.Instance.RequestCreateMatchId(delegate(string id)
				{
					Hashtable propertiesToSet = new Hashtable { { "matchId", id } };
					PhotonNetwork.CurrentRoom.SetCustomProperties(propertiesToSet);
				});
			}
			else if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("matchId", out value))
			{
				GorillaTagCompetitiveServerApi.Instance.RequestValidateMatchJoin((string)value, delegate(bool valid)
				{
					if (!valid)
					{
						Debug.LogError("ValidateMatchJoin failed. Leaving room!");
						NetworkSystem.Instance.ReturnToSinglePlayer();
					}
				});
			}
		}
		GorillaTagCompetitiveManager.onPlayerJoined?.Invoke(newPlayer);
		if (VRRigCache.Instance.TryGetVrrig(newPlayer, out var playerRig))
		{
			playerRig.Rig.EnableRankedTimerWatch(on: true);
		}
	}

	public override void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		GorillaTagCompetitiveManager.onPlayerLeft?.Invoke(otherPlayer);
		if (VRRigCache.Instance.TryGetVrrig(otherPlayer, out var playerRig))
		{
			playerRig.Rig.EnableRankedTimerWatch(on: false);
		}
	}

	public RankedMultiplayerScore GetScoring()
	{
		return scoring;
	}

	public override bool LocalCanTag(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		if (base.LocalCanTag(myPlayer, otherPlayer) && gameState != GameState.StartingCountdown)
		{
			return gameState != GameState.PostRound;
		}
		return false;
	}

	public override bool LocalIsTagged(NetPlayer player)
	{
		if (gameState == GameState.StartingCountdown || gameState == GameState.PostRound)
		{
			return false;
		}
		return base.LocalIsTagged(player);
	}

	public override void ReportTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		base.ReportTag(taggedPlayer, taggingPlayer);
	}

	public override GameModeType GameType()
	{
		return GameModeType.InfectionCompetitive;
	}

	public override string GameModeName()
	{
		return "COMP-INFECT";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_COMP_INF_ROOM_LABEL", out var result, "(COMP-INFECT GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_COMP_INF_ROOM_LABEL]");
		}
		return result;
	}

	public override bool CanJoinFrienship(NetPlayer player)
	{
		return false;
	}

	public override void UpdateInfectionState()
	{
		if (NetworkSystem.Instance.IsMasterClient && gameState == GameState.Playing && IsEveryoneTagged())
		{
			HandleInfectionRoundComplete();
		}
	}

	public override void HandleTagBroadcast(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		if (!currentInfected.Contains(taggingPlayer) || !VRRigCache.Instance.TryGetVrrig(taggedPlayer, out var playerRig) || !VRRigCache.Instance.TryGetVrrig(taggingPlayer, out var playerRig2))
		{
			return;
		}
		VRRig rig = playerRig2.Rig;
		VRRig rig2 = playerRig.Rig;
		if (rig.IsPositionInRange(rig2.transform.position, 6f) || rig.CheckTagDistanceRollback(rig2, 6f, 0.2f))
		{
			if (!NetworkSystem.Instance.IsMasterClient && gameState == GameState.Playing && !currentInfected.Contains(taggedPlayer))
			{
				AddLastTagged(taggedPlayer, taggingPlayer);
				currentInfected.Add(taggedPlayer);
			}
			GorillaTagCompetitiveManager.onTagOccurred?.Invoke(taggedPlayer, taggingPlayer);
		}
	}

	private void SetState(GameState newState)
	{
		if (newState != this.gameState)
		{
			GameState gameState = this.gameState;
			this.gameState = newState;
			switch (this.gameState)
			{
			case GameState.WaitingForPlayers:
				EnterStateWaitingForPlayers();
				break;
			case GameState.StartingCountdown:
				EnterStateStartingCountdown();
				break;
			case GameState.Playing:
				EnterStatePlaying();
				break;
			case GameState.PostRound:
				EnterStatePostRound();
				break;
			}
			GorillaTagCompetitiveManager.onStateChanged?.Invoke(this.gameState);
			GorillaTagCompetitiveManager.onUpdateRemainingTime?.Invoke(stateRemainingTime);
			if (this.gameState == GameState.Playing)
			{
				GorillaTagCompetitiveManager.onRoundStart?.Invoke();
			}
			else if (gameState == GameState.Playing)
			{
				GorillaTagCompetitiveManager.onRoundEnd?.Invoke();
			}
			GTDev.Log($"!! Competitive SetState: {this.gameState} at: {Time.time}");
		}
	}

	private void EnterStateWaitingForPlayers()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			SetisCurrentlyTag(newTagSetting: true);
			ClearInfectionState();
		}
	}

	private void EnterStateStartingCountdown()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (isCurrentlyTag)
			{
				SetisCurrentlyTag(newTagSetting: false);
			}
			currentIt = null;
			ClearInfectionState();
			GameMode.RefreshPlayers();
			CheckForInfected();
			stateRemainingTime = startCountdownDuration;
		}
	}

	private void EnterStatePlaying()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (isCurrentlyTag)
			{
				SetisCurrentlyTag(newTagSetting: false);
			}
			currentIt = null;
			stateRemainingTime = roundDuration;
			PingRoom();
		}
		DisplayScoreboardPredictedResults(bShow: false);
	}

	private void EnterStatePostRound()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (isCurrentlyTag)
			{
				SetisCurrentlyTag(newTagSetting: false);
			}
			currentIt = null;
			stateRemainingTime = postRoundDuration;
		}
		DisplayScoreboardPredictedResults(bShow: true);
	}

	public override void UpdateState()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			switch (gameState)
			{
			case GameState.None:
				SetState(GameState.WaitingForPlayers);
				break;
			case GameState.WaitingForPlayers:
				UpdateStateWaitingForPlayers();
				break;
			case GameState.StartingCountdown:
				UpdateStateStartingCountdown();
				break;
			case GameState.Playing:
				UpdateStatePlaying();
				break;
			case GameState.PostRound:
				UpdateStatePostRound();
				break;
			}
		}
	}

	private void UpdateStateWaitingForPlayers()
	{
		if (IsInfectionPossible())
		{
			SetState(GameState.StartingCountdown);
		}
		else if (isCurrentlyTag && currentIt == null)
		{
			int index = UnityEngine.Random.Range(0, GameMode.ParticipatingPlayers.Count);
			ChangeCurrentIt(GameMode.ParticipatingPlayers[index], withTagFreeze: false);
		}
	}

	private void UpdateStateStartingCountdown()
	{
		if (!IsInfectionPossible())
		{
			SetState(GameState.WaitingForPlayers);
		}
		else if (stateRemainingTime < 0f)
		{
			SetState(GameState.Playing);
		}
		else
		{
			CheckForInfected();
		}
	}

	private void UpdateStatePlaying()
	{
		if (IsGameInvalid())
		{
			SetState(GameState.WaitingForPlayers);
		}
		else if (stateRemainingTime < 0f)
		{
			HandleInfectionRoundComplete();
		}
		else if (IsEveryoneTagged())
		{
			HandleInfectionRoundComplete();
		}
		else
		{
			CheckForInfected();
		}
	}

	private void HandleInfectionRoundComplete()
	{
		foreach (NetPlayer participatingPlayer in GameMode.ParticipatingPlayers)
		{
			RoomSystem.SendSoundEffectToPlayer(2, 0.25f, participatingPlayer, stopCurrentAudio: true);
		}
		PlayerGameEvents.GameModeCompleteRound();
		GameMode.BroadcastRoundComplete();
		lastTaggedActorNr.Clear();
		waitingToStartNextInfectionGame = true;
		timeInfectedGameEnded = Time.time;
		SetState(GameState.PostRound);
	}

	private void UpdateStatePostRound()
	{
		if (stateRemainingTime < 0f)
		{
			if (IsInfectionPossible())
			{
				SetState(GameState.StartingCountdown);
			}
			else
			{
				SetState(GameState.WaitingForPlayers);
			}
		}
	}

	private void PingRoom()
	{
		if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("matchId", out var value))
		{
			GorillaTagCompetitiveServerApi.Instance.RequestPingRoom((string)value, delegate
			{
				ShowDebugPing = true;
			});
		}
	}

	private bool IsGameInvalid()
	{
		return GameMode.ParticipatingPlayers.Count <= 1;
	}

	private bool IsInfectionPossible()
	{
		return GameMode.ParticipatingPlayers.Count >= infectedModeThreshold;
	}

	private bool IsEveryoneTagged()
	{
		bool result = true;
		foreach (NetPlayer participatingPlayer in GameMode.ParticipatingPlayers)
		{
			if (!currentInfected.Contains(participatingPlayer))
			{
				result = false;
				break;
			}
		}
		return result;
	}

	private void CheckForInfected()
	{
		if (currentInfected.Count == 0)
		{
			int index = UnityEngine.Random.Range(0, GameMode.ParticipatingPlayers.Count);
			AddInfectedPlayer(GameMode.ParticipatingPlayers[index]);
		}
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		base.OnSerializeWrite(stream, info);
		stream.SendNext(gameState);
		stream.SendNext(stateRemainingTime);
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		NetworkSystem.Instance.GetPlayer(info.Sender);
		base.OnSerializeRead(stream, info);
		GameState state = (GameState)stream.ReceiveNext();
		stateRemainingTime = (float)stream.ReceiveNext();
		SetState(state);
	}

	public void UpdateScoreboards()
	{
		List<RankedMultiplayerScore.PlayerScoreInRound> sortedScores = scoring.GetSortedScores();
		if (gameState == GameState.Playing)
		{
			lastActiveTime = Time.time;
		}
		for (int i = 0; i < scoreboards.Count; i++)
		{
			scoreboards[i].UpdateScores(gameState, lastActiveTime, sortedScores, scoring.PlayerRankedTiers, scoring.ProjectedEloDeltas, currentInfected, scoring.Progression);
		}
	}

	public void DisplayScoreboardPredictedResults(bool bShow)
	{
		for (int i = 0; i < scoreboards.Count; i++)
		{
			scoreboards[i].DisplayPredictedResults(bShow);
		}
	}

	public void RegisterForcedLeaveVolume(GorillaTagCompetitiveForcedLeaveRoomVolume volume)
	{
		if (!forceLeaveRoomVolumes.Contains(volume))
		{
			forceLeaveRoomVolumes.Add(volume);
		}
	}

	public void UnregisterForcedLeaveVolume(GorillaTagCompetitiveForcedLeaveRoomVolume volume)
	{
		forceLeaveRoomVolumes.Remove(volume);
	}
}
