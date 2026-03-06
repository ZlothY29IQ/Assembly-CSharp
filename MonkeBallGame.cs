using System;
using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[NetworkBehaviourWeaved(0)]
public class MonkeBallGame : NetworkComponent, ITickSystemTick
{
	public enum GameState
	{
		None,
		PreGame,
		Playing,
		PostScore,
		PostGame
	}

	private enum RPC
	{
		SetGameState,
		RequestSetGameState,
		RequestResetGame,
		SetScore,
		RequestSetTeam,
		SetTeam,
		SetRestrictBallToTeam,
		SetResetButton,
		Count
	}

	public static MonkeBallGame Instance;

	public List<MonkeBall> startingBalls;

	public List<MonkeBallScoreboard> scoreboards;

	public List<MonkeBallShotclock> shotclocks;

	public List<MonkeBallGoalZone> goalZones;

	[Space]
	public MonkeBallResetGame resetButton;

	public MonkeBallResetGame centerResetButton;

	[Space]
	public PhotonView photonView;

	public List<MonkeBallTeam> team;

	private int _currentPlayerTotal;

	[Space]
	[Tooltip("The length of the game in seconds.")]
	public float gameDuration;

	[Space]
	[Tooltip("If the ball should be reset to a team starting position after a score. If not set to true then the will reset back to a neutral starting position.")]
	public bool resetBallPositionOnScore = true;

	[Tooltip("The duration in which a team is restricted from grabbing the ball after toss.")]
	public float restrictBallDuration = 5f;

	[Tooltip("The duration in which a team is restricted from grabbing the ball after a score.")]
	public float restrictBallDurationAfterScore = 10f;

	[Header("Neutral Launcher")]
	[SerializeField]
	private Transform _ballLauncher;

	[Tooltip("The min/max random velocity of the ball when launched.")]
	public Vector2 ballLauncherVelocityRange = new Vector2(8f, 15f);

	[Tooltip("The min/max random x-angle of the ball when launched.")]
	public Vector2 ballLaunchAngleXRange = new Vector2(0f, 0f);

	[Tooltip("The min/max random y-angle of the ball when launched.")]
	public Vector2 ballLaunchAngleYRange = new Vector2(0f, 0f);

	[Space]
	[SerializeField]
	private Transform _neutralBallStartLocation;

	[SerializeField]
	private ParticleSystem[] endZoneEffects;

	private GameState gameState;

	public double gameEndTime;

	private int _frameIndex;

	private bool _forceSync;

	private float _forceSyncDelay;

	private bool _forceOrigColorFix;

	private float _forceOrigColorDelay;

	private Color _storedLocalPlayerColor;

	private bool _setStoredLocalPlayerColor;

	private CallLimiter[] _callLimiters;

	public Transform BallLauncher => _ballLauncher;

	public bool TickRunning { get; set; }

	protected override void Awake()
	{
		base.Awake();
		Instance = this;
		gameState = GameState.None;
		_callLimiters = new CallLimiter[8];
		_callLimiters[0] = new CallLimiter(20, 1f);
		_callLimiters[1] = new CallLimiter(20, 10f);
		_callLimiters[2] = new CallLimiter(20, 10f);
		_callLimiters[3] = new CallLimiter(20, 1f);
		_callLimiters[4] = new CallLimiter(20, 1f);
		_callLimiters[5] = new CallLimiter(20, 1f);
		_callLimiters[6] = new CallLimiter(20, 1f);
		_callLimiters[7] = new CallLimiter(5, 10f);
		AssignNetworkListeners();
	}

	private bool ValidateCallLimits(RPC rpcCall, PhotonMessageInfo info)
	{
		if (rpcCall < RPC.SetGameState || rpcCall >= RPC.Count)
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

	protected override void Start()
	{
		base.Start();
		for (int i = 0; i < startingBalls.Count; i++)
		{
			GameBallManager.Instance.AddGameBall(startingBalls[i].gameBall);
		}
		for (int j = 0; j < scoreboards.Count; j++)
		{
			scoreboards[j].Setup(this);
		}
		gameEndTime = -1.0;
	}

	private new void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		TickSystem<object>.AddTickCallback(this);
	}

	private new void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		base.Despawned(runner, hasState);
		UnassignNetworkListeners();
	}

	public void OnPlayerDestroy()
	{
		if (_setStoredLocalPlayerColor)
		{
			PlayerPrefs.SetFloat("redValue", _storedLocalPlayerColor.r);
			PlayerPrefs.SetFloat("greenValue", _storedLocalPlayerColor.g);
			PlayerPrefs.SetFloat("blueValue", _storedLocalPlayerColor.b);
			PlayerPrefs.Save();
		}
	}

	private void AssignNetworkListeners()
	{
		NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(OnPlayerJoined);
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerLeft);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent += new Action<NetPlayer>(OnMasterClientSwitched);
	}

	private void UnassignNetworkListeners()
	{
		NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(OnPlayerJoined);
		NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerLeft);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent -= new Action<NetPlayer>(OnMasterClientSwitched);
	}

	public void Tick()
	{
		if (IsMasterClient() && gameState != GameState.None && gameEndTime >= 0.0 && PhotonNetwork.Time > gameEndTime)
		{
			gameEndTime = -1.0;
			RequestGameState(GameState.PostGame);
		}
		if (!ZoneManagement.IsInZone(GTZone.arena))
		{
			return;
		}
		RefreshTime();
		if (_forceSync)
		{
			_forceSyncDelay -= Time.deltaTime;
			if (_forceSyncDelay <= 0f)
			{
				_forceSync = false;
				ForceSyncPlayersVisuals();
				RefreshTeamPlayers(playSounds: false);
			}
		}
		if (_forceOrigColorFix)
		{
			_forceOrigColorDelay -= Time.deltaTime;
			if (_forceOrigColorDelay <= 0f)
			{
				_forceOrigColorFix = false;
				ForceOriginalColorSync();
			}
		}
	}

	private void OnPlayerJoined(NetPlayer player)
	{
		_forceSync = true;
		_forceSyncDelay = 5f;
		if (IsMasterClient())
		{
			GetCurrentGameState(out var playerIds, out var playerTeams, out var scores, out var packedBallPosRot, out var packedBallVel);
			photonView.RPC("RequestSetGameStateRPC", player.GetPlayerRef(), (int)gameState, gameEndTime, playerIds, playerTeams, scores, packedBallPosRot, packedBallVel);
		}
	}

	private void OnPlayerLeft(NetPlayer player)
	{
		_forceSync = true;
		_forceSyncDelay = 5f;
		GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(player.ActorNumber);
		if (gamePlayer != null)
		{
			gamePlayer.CleanupPlayer();
		}
		if (IsMasterClient())
		{
			photonView.RPC("SetTeamRPC", RpcTarget.All, -1, player.GetPlayerRef());
		}
	}

	private void OnMasterClientSwitched(NetPlayer player)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			GetCurrentGameState(out var playerIds, out var playerTeams, out var scores, out var packedBallPosRot, out var packedBallVel);
			photonView.RPC("RequestSetGameStateRPC", RpcTarget.Others, (int)gameState, gameEndTime, playerIds, playerTeams, scores, packedBallPosRot, packedBallVel);
		}
	}

	private void GetCurrentGameState(out int[] playerIds, out int[] playerTeams, out int[] scores, out long[] packedBallPosRot, out long[] packedBallVel)
	{
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		playerIds = new int[allNetPlayers.Length];
		playerTeams = new int[allNetPlayers.Length];
		for (int i = 0; i < allNetPlayers.Length; i++)
		{
			playerIds[i] = allNetPlayers[i].ActorNumber;
			GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(allNetPlayers[i].ActorNumber);
			if (gamePlayer != null)
			{
				playerTeams[i] = gamePlayer.teamId;
			}
			else
			{
				playerTeams[i] = -1;
			}
		}
		scores = new int[team.Count];
		for (int j = 0; j < team.Count; j++)
		{
			scores[j] = team[j].score;
		}
		packedBallPosRot = new long[startingBalls.Count];
		packedBallVel = new long[startingBalls.Count];
		for (int k = 0; k < startingBalls.Count; k++)
		{
			packedBallPosRot[k] = BitPackUtils.PackHandPosRotForNetwork(startingBalls[k].transform.position, startingBalls[k].transform.rotation);
			packedBallVel[k] = BitPackUtils.PackWorldPosForNetwork(startingBalls[k].gameBall.GetVelocity());
		}
	}

	private bool IsMasterClient()
	{
		return PhotonNetwork.IsMasterClient;
	}

	public GameState GetGameState()
	{
		return gameState;
	}

	public void RequestGameState(GameState newGameState)
	{
		if (IsMasterClient())
		{
			photonView.RPC("SetGameStateRPC", RpcTarget.All, (int)newGameState);
		}
	}

	[PunRPC]
	private void SetGameStateRPC(int newGameState, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "SetGameStateRPC");
		if (!ValidateCallLimits(RPC.SetGameState, info))
		{
			return;
		}
		if (newGameState < 0 || newGameState > 4)
		{
			ReportRPCCall(RPC.SetGameState, info, "newGameState outside of enum range.");
			return;
		}
		SetGameState((GameState)newGameState);
		if (newGameState == 1)
		{
			gameEndTime = info.SentServerTime + (double)gameDuration;
		}
	}

	private void SetGameState(GameState newGameState)
	{
		gameState = newGameState;
		switch (gameState)
		{
		case GameState.PreGame:
			OnEnterStatePreGame();
			break;
		case GameState.Playing:
			OnEnterStatePlaying();
			break;
		case GameState.PostScore:
			OnEnterStatePostScore();
			break;
		case GameState.PostGame:
			OnEnterStatePostGame();
			break;
		}
	}

	private void OnEnterStatePreGame()
	{
		for (int i = 0; i < scoreboards.Count; i++)
		{
			scoreboards[i].PlayGameStartFx();
		}
	}

	private void OnEnterStatePlaying()
	{
		_forceSync = true;
		_forceSyncDelay = 0.1f;
	}

	private void OnEnterStatePostScore()
	{
	}

	private void OnEnterStatePostGame()
	{
		for (int i = 0; i < scoreboards.Count; i++)
		{
			scoreboards[i].PlayGameEndFx();
		}
	}

	[PunRPC]
	private void RequestSetGameStateRPC(int newGameState, double newGameEndTime, int[] playerIds, int[] playerTeams, int[] scores, long[] packedBallPosRot, long[] packedBallVel, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "RequestSetGameStateRPC");
		if (!ValidateCallLimits(RPC.RequestSetGameState, info))
		{
			return;
		}
		if (playerIds.IsNullOrEmpty() || playerTeams.IsNullOrEmpty() || scores.IsNullOrEmpty() || packedBallPosRot.IsNullOrEmpty() || packedBallVel.IsNullOrEmpty())
		{
			ReportRPCCall(RPC.RequestSetGameState, info, "Array params are null or empty.");
			return;
		}
		if (newGameState < 0 || newGameState > 4)
		{
			ReportRPCCall(RPC.RequestSetGameState, info, "newGameState outside of enum range.");
			return;
		}
		if (playerIds.Length != playerTeams.Length)
		{
			ReportRPCCall(RPC.RequestSetGameState, info, "playerIDs and playerTeams are not the same length.");
			return;
		}
		if (scores.Length > team.Count)
		{
			ReportRPCCall(RPC.RequestSetGameState, info, "scores and team are not the same length.");
			return;
		}
		if (packedBallPosRot.Length != startingBalls.Count || packedBallPosRot.Length != packedBallVel.Length)
		{
			ReportRPCCall(RPC.RequestSetGameState, info, "packedBall arrays are not the same length.");
			return;
		}
		if (double.IsNaN(newGameEndTime) || double.IsInfinity(newGameEndTime))
		{
			ReportRPCCall(RPC.RequestSetGameState, info, "newGameEndTime is not valid.");
			return;
		}
		if (newGameEndTime < -1.0 || newGameEndTime > PhotonNetwork.Time + (double)gameDuration)
		{
			ReportRPCCall(RPC.RequestSetGameState, info, "newGameEndTime exceeds possible time limits.");
			return;
		}
		gameState = (GameState)newGameState;
		gameEndTime = newGameEndTime;
		for (int i = 0; i < playerIds.Length; i++)
		{
			if (VRRigCache.Instance.localRig.Creator.ActorNumber == playerIds[i])
			{
				continue;
			}
			GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(playerIds[i]);
			if (!(gamePlayer == null))
			{
				gamePlayer.teamId = playerTeams[i];
				if (playerTeams[i] >= 0 && playerTeams[i] < team.Count && VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(playerIds[i]), out var playerRig))
				{
					Color color = team[playerTeams[i]].color;
					playerRig.Rig.InitializeNoobMaterialLocal(color.r, color.g, color.b);
					playerRig.Rig.LocalUpdateCosmeticsWithTryon(CosmeticsController.CosmeticSet.EmptySet, CosmeticsController.CosmeticSet.EmptySet, playfx: false);
				}
			}
		}
		RefreshTeamPlayers(playSounds: false);
		for (int j = 0; j < scores.Length; j++)
		{
			SetScore(j, scores[j], playFX: false);
		}
		for (int k = 0; k < packedBallPosRot.Length; k++)
		{
			BitPackUtils.UnpackHandPosRotFromNetwork(packedBallPosRot[k], out var localPos, out var handRot);
			if (localPos.IsValid(10000f) && handRot.IsValid())
			{
				startingBalls[k].transform.position = localPos;
				startingBalls[k].transform.rotation = handRot;
				if ((startingBalls[k].transform.position - base.transform.position).sqrMagnitude > 6400f)
				{
					startingBalls[k].transform.position = _neutralBallStartLocation.transform.position;
				}
				Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork(packedBallVel[k]);
				if (v.IsValid(10000f))
				{
					startingBalls[k].gameBall.SetVelocity(v);
					startingBalls[k].TriggerDelayedResync();
				}
			}
		}
		_forceSync = true;
		_forceSyncDelay = 5f;
	}

	public void RequestResetGame()
	{
		photonView.RPC("RequestResetGameRPC", RpcTarget.All);
	}

	[PunRPC]
	private void RequestResetGameRPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestResetGameRPC");
		if (!IsMasterClient() || !ValidateCallLimits(RPC.RequestResetGame, info))
		{
			return;
		}
		GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(info.Sender.ActorNumber);
		if (!(gamePlayer == null) && gamePlayer.teamId == resetButton.allowedTeamId)
		{
			for (int i = 0; i < startingBalls.Count; i++)
			{
				RequestResetBall(startingBalls[i].gameBall.id, -1);
			}
			for (int j = 0; j < team.Count; j++)
			{
				RequestSetScore(j, 0);
			}
			RequestGameState(GameState.PreGame);
			resetButton.ToggleReset(toggle: false, -1, force: true);
			if (centerResetButton != null)
			{
				centerResetButton.ToggleReset(toggle: false, -1, force: true);
			}
		}
	}

	public void ToggleResetButton(bool toggle, int teamId)
	{
		int otherTeam = GetOtherTeam(teamId);
		photonView.RPC("SetResetButtonRPC", RpcTarget.All, toggle, otherTeam);
	}

	[PunRPC]
	private void SetResetButtonRPC(bool toggleReset, int teamId, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "SetResetButtonRPC");
		if (!ValidateCallLimits(RPC.SetResetButton, info))
		{
			return;
		}
		if (teamId < -1 || teamId >= team.Count)
		{
			ReportRPCCall(RPC.SetResetButton, info, "teamID exceeds possible range.");
			return;
		}
		resetButton.ToggleReset(toggleReset, teamId);
		if (centerResetButton != null)
		{
			centerResetButton.ToggleReset(toggleReset, teamId);
		}
	}

	public void OnBallGrabbed(GameBallId gameBallId)
	{
		if (gameState == GameState.PreGame)
		{
			SetGameState(GameState.Playing);
		}
		if (gameState == GameState.PostScore)
		{
			SetGameState(GameState.Playing);
		}
	}

	private void RefreshTime()
	{
		_frameIndex++;
		if (_frameIndex > 2)
		{
			_frameIndex = 0;
		}
		if (_frameIndex == 0)
		{
			float a = (float)(gameEndTime - PhotonNetwork.Time);
			if (gameEndTime < 0.0)
			{
				a = 0f;
			}
			string timeString = Mathf.Max(a, 0f).ToString("#00.00");
			for (int i = 0; i < scoreboards.Count; i++)
			{
				scoreboards[i].RefreshTime(timeString);
			}
		}
	}

	public void RequestResetBall(GameBallId gameBallId, int teamId)
	{
		if (IsMasterClient())
		{
			if (teamId >= 0)
			{
				LaunchBallWithTeam(gameBallId, teamId, team[teamId].ballLaunchPosition, team[teamId].ballLaunchVelocityRange, team[teamId].ballLaunchAngleXRange, team[teamId].ballLaunchAngleXRange);
			}
			else
			{
				LaunchBallNeutral(gameBallId);
			}
		}
	}

	public void RequestScore(int teamId)
	{
		if (IsMasterClient() && teamId >= 0 && teamId < team.Count)
		{
			photonView.RPC("SetScoreRPC", RpcTarget.All, teamId, team[teamId].score + 1);
			RequestGameState(GameState.PostScore);
		}
	}

	public void RequestSetScore(int teamId, int score)
	{
		if (IsMasterClient())
		{
			photonView.RPC("SetScoreRPC", RpcTarget.All, teamId, score);
		}
	}

	[PunRPC]
	private void SetScoreRPC(int teamId, int score, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "SetScoreRPC");
		if (ValidateCallLimits(RPC.SetScore, info))
		{
			if (teamId < 0 || teamId >= team.Count)
			{
				ReportRPCCall(RPC.SetScore, info, "teamID exceeds possible range.");
			}
			else if (score != 0 && score != team[teamId].score + 1)
			{
				ReportRPCCall(RPC.SetScore, info, "Score is being set to a non-achievable value.");
			}
			else
			{
				SetScore(teamId, Mathf.Clamp(score, 0, 999));
			}
		}
	}

	private void SetScore(int teamId, int score, bool playFX = true)
	{
		if (teamId < 0 || teamId > team.Count)
		{
			return;
		}
		int score2 = team[teamId].score;
		team[teamId].score = score;
		if (playFX && score > score2)
		{
			PlayScoreFx();
			Color color = team[teamId].color;
			for (int i = 0; i < endZoneEffects.Length; i++)
			{
				endZoneEffects[i].startColor = color;
				endZoneEffects[i].Play();
			}
		}
		RefreshScore();
	}

	private void RefreshScore()
	{
		for (int i = 0; i < scoreboards.Count; i++)
		{
			scoreboards[i].RefreshScore();
		}
	}

	private void PlayScoreFx()
	{
		for (int i = 0; i < scoreboards.Count; i++)
		{
			scoreboards[i].PlayScoreFx();
		}
	}

	public MonkeBallTeam GetTeam(int teamId)
	{
		return team[teamId];
	}

	public int GetOtherTeam(int teamId)
	{
		return (teamId + 1) % team.Count;
	}

	public void RequestSetTeam(int teamId)
	{
		if (!ZoneManagement.IsInZone(GTZone.arena))
		{
			return;
		}
		photonView.RPC("RequestSetTeamRPC", RpcTarget.MasterClient, teamId);
		bool flag = false;
		Color white = Color.white;
		if (teamId >= 0 && teamId < team.Count)
		{
			flag = true;
			if (!_setStoredLocalPlayerColor)
			{
				_storedLocalPlayerColor = new Color(PlayerPrefs.GetFloat("redValue", 1f), PlayerPrefs.GetFloat("greenValue", 1f), PlayerPrefs.GetFloat("blueValue", 1f));
				_setStoredLocalPlayerColor = true;
			}
			_forceOrigColorFix = false;
			white = team[teamId].color;
		}
		else
		{
			white = _storedLocalPlayerColor;
			_setStoredLocalPlayerColor = false;
		}
		PlayerPrefs.SetFloat("redValue", white.r);
		PlayerPrefs.SetFloat("greenValue", white.g);
		PlayerPrefs.SetFloat("blueValue", white.b);
		PlayerPrefs.Save();
		GorillaTagger.Instance.UpdateColor(white.r, white.g, white.b);
		GorillaComputer.instance.UpdateColor(white.r, white.g, white.b);
		if (NetworkSystem.Instance.InRoom)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, white.r, white.g, white.b);
			if (flag)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_HideAllCosmetics", RpcTarget.All);
			}
			else
			{
				_forceOrigColorFix = true;
				_forceOrigColorDelay = 3f;
				CosmeticsController.instance.UpdateWornCosmetics(sync: true);
			}
			ForceSyncPlayersVisuals();
		}
	}

	private MonkeBall GetMonkeBall(GameBallId gameBallId)
	{
		GameBall gameBall = GameBallManager.Instance.GetGameBall(gameBallId);
		if (!(gameBall == null))
		{
			return gameBall.GetComponent<MonkeBall>();
		}
		return null;
	}

	[PunRPC]
	private void RequestSetTeamRPC(int teamId, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestSetTeamRPC");
		if (IsMasterClient() && ValidateCallLimits(RPC.RequestSetTeam, info))
		{
			if (teamId < -1 || teamId >= team.Count)
			{
				ReportRPCCall(RPC.RequestSetTeam, info, "teamID exceeds possible range.");
				return;
			}
			photonView.RPC("SetTeamRPC", RpcTarget.All, teamId, info.Sender);
		}
	}

	[PunRPC]
	private void SetTeamRPC(int teamId, Player player, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "SetTeamRPC");
		if (ValidateCallLimits(RPC.SetTeam, info))
		{
			if (teamId < -1 || teamId >= team.Count)
			{
				ReportRPCCall(RPC.SetTeam, info, "teamID exceeds possible range.");
			}
			else
			{
				SetTeamPlayer(teamId, player);
			}
		}
	}

	private void SetTeamPlayer(int teamId, Player player)
	{
		if (player != null)
		{
			GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(player.ActorNumber);
			if (gamePlayer != null)
			{
				gamePlayer.teamId = teamId;
			}
			RefreshTeamPlayers(playSounds: true);
		}
	}

	private void RefreshTeamPlayers(bool playSounds)
	{
		int[] array = new int[team.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = 0;
		}
		int num = 0;
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		for (int j = 0; j < allNetPlayers.Length; j++)
		{
			GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(allNetPlayers[j].ActorNumber);
			if (!(gamePlayer == null))
			{
				int teamId = gamePlayer.teamId;
				if (teamId >= 0)
				{
					array[teamId]++;
					num++;
				}
			}
		}
		for (int k = 0; k < scoreboards.Count; k++)
		{
			for (int l = 0; l < array.Length; l++)
			{
				scoreboards[k].RefreshTeamPlayers(l, array[l]);
			}
			if (playSounds)
			{
				if (_currentPlayerTotal < num)
				{
					scoreboards[k].PlayPlayerJoinFx();
				}
				else if (_currentPlayerTotal > num)
				{
					scoreboards[k].PlayPlayerLeaveFx();
				}
			}
		}
		_currentPlayerTotal = num;
	}

	private void ForceSyncPlayersVisuals()
	{
		for (int i = 0; i < NetworkSystem.Instance.AllNetPlayers.Length; i++)
		{
			int actorNumber = NetworkSystem.Instance.AllNetPlayers[i].ActorNumber;
			if (VRRigCache.Instance.localRig.Creator.ActorNumber != actorNumber)
			{
				GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(actorNumber);
				if (!(gamePlayer == null) && gamePlayer.teamId >= 0 && gamePlayer.teamId < team.Count && VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(actorNumber), out var playerRig))
				{
					Color color = team[gamePlayer.teamId].color;
					playerRig.Rig.InitializeNoobMaterialLocal(color.r, color.g, color.b);
					playerRig.Rig.LocalUpdateCosmeticsWithTryon(CosmeticsController.CosmeticSet.EmptySet, CosmeticsController.CosmeticSet.EmptySet, playfx: false);
				}
			}
		}
	}

	private void ForceOriginalColorSync()
	{
		GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(VRRigCache.Instance.localRig.Creator.ActorNumber);
		if (!(gamePlayer == null) && (gamePlayer.teamId < 0 || gamePlayer.teamId >= team.Count))
		{
			Color storedLocalPlayerColor = _storedLocalPlayerColor;
			if (NetworkSystem.Instance.InRoom)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, storedLocalPlayerColor.r, storedLocalPlayerColor.g, storedLocalPlayerColor.b);
			}
		}
	}

	public void RequestRestrictBallToTeam(GameBallId gameBallId, int teamId)
	{
		RestrictBallToTeam(gameBallId, teamId, restrictBallDuration);
	}

	public void RequestRestrictBallToTeamOnScore(GameBallId gameBallId, int teamId)
	{
		RestrictBallToTeam(gameBallId, teamId, restrictBallDurationAfterScore);
	}

	private void RestrictBallToTeam(GameBallId gameBallId, int teamId, float restrictDuration)
	{
		if (IsMasterClient())
		{
			photonView.RPC("SetRestrictBallToTeam", RpcTarget.All, gameBallId.index, teamId, restrictDuration);
		}
	}

	[PunRPC]
	private void SetRestrictBallToTeam(int gameBallIndex, int teamId, float restrictDuration, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "SetRestrictBallToTeam");
		if (!ValidateCallLimits(RPC.SetRestrictBallToTeam, info))
		{
			return;
		}
		if (gameBallIndex < 0 || gameBallIndex >= startingBalls.Count)
		{
			ReportRPCCall(RPC.SetRestrictBallToTeam, info, "gameBallIndex exceeds possible range.");
			return;
		}
		if (teamId < -1 || teamId >= team.Count)
		{
			ReportRPCCall(RPC.SetRestrictBallToTeam, info, "teamID exceeds possible range.");
			return;
		}
		if (float.IsNaN(restrictDuration) || float.IsInfinity(restrictDuration) || restrictDuration < 0f || restrictDuration > restrictBallDurationAfterScore + restrictBallDuration)
		{
			ReportRPCCall(RPC.SetRestrictBallToTeam, info, "restrictDuration is not a feasible value.");
			return;
		}
		GameBallId gameBallId = new GameBallId(gameBallIndex);
		MonkeBall monkeBall = GetMonkeBall(gameBallId);
		bool flag = false;
		if (monkeBall != null)
		{
			flag = monkeBall.RestrictBallToTeam(teamId, restrictDuration);
		}
		if (flag)
		{
			for (int i = 0; i < shotclocks.Count; i++)
			{
				shotclocks[i].SetTime(teamId, restrictDuration);
			}
		}
	}

	public void LaunchBallNeutral(GameBallId gameBallId)
	{
		LaunchBall(gameBallId, _ballLauncher, ballLauncherVelocityRange.x, ballLauncherVelocityRange.y, ballLaunchAngleXRange.x, ballLaunchAngleXRange.y, ballLaunchAngleYRange.x, ballLaunchAngleYRange.y);
	}

	public void LaunchBallWithTeam(GameBallId gameBallId, int teamId, Transform launcher, Vector2 velocityRange, Vector2 angleXRange, Vector2 angleYRange)
	{
		LaunchBall(gameBallId, launcher, velocityRange.x, velocityRange.y, angleXRange.x, angleXRange.y, angleYRange.x, angleYRange.y);
	}

	private void LaunchBall(GameBallId gameBallId, Transform launcher, float minVelocity, float maxVelocity, float minXAngle, float maxXAngle, float minYAngle, float maxYAngle)
	{
		GameBall gameBall = GameBallManager.Instance.GetGameBall(gameBallId);
		if (!(gameBall == null))
		{
			gameBall.transform.position = launcher.transform.position;
			Quaternion rotation = launcher.transform.rotation;
			launcher.transform.Rotate(Vector3.up, UnityEngine.Random.Range(minXAngle, maxXAngle));
			launcher.transform.Rotate(Vector3.right, UnityEngine.Random.Range(minYAngle, maxYAngle));
			gameBall.transform.rotation = launcher.transform.rotation;
			Vector3 velocity = launcher.transform.forward * UnityEngine.Random.Range(minVelocity, maxVelocity);
			launcher.transform.rotation = rotation;
			GameBallManager.Instance.RequestLaunchBall(gameBallId, velocity);
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
