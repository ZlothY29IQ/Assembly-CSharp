using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using GorillaExtensions;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;

namespace GorillaTagScripts;

[NetworkBehaviourWeaved(210)]
public class WhackAMole : NetworkComponent
{
	public enum GameState
	{
		Off,
		ContinuePressed,
		Ongoing,
		PickMoles,
		TimesUp,
		LevelStarted
	}

	private enum GameResult
	{
		GameOver,
		Win,
		LevelComplete,
		Unknown
	}

	[StructLayout(LayoutKind.Explicit, Size = 840)]
	[NetworkStructWeaved(210)]
	public struct WhackAMoleData : INetworkStruct
	{
		[FieldOffset(24)]
		[FixedBufferProperty(typeof(NetworkString<_128>), typeof(UnityValueSurrogate_0040ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__128_003E), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_0040129 _HighScorePlayerName;

		[FieldOffset(556)]
		[FixedBufferProperty(typeof(NetworkDictionary<int, int>), typeof(UnityDictionarySurrogate_0040ElementReaderWriterInt32_0040ElementReaderWriterInt32), 17, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_004071 _PickedMolesIndex;

		[field: FieldOffset(0)]
		public GameState CurrentState { get; set; }

		[field: FieldOffset(4)]
		public int CurrentLevelIndex { get; set; }

		[field: FieldOffset(8)]
		public int CurrentScore { get; set; }

		[field: FieldOffset(12)]
		public int TotalScore { get; set; }

		[field: FieldOffset(16)]
		public int BestScore { get; set; }

		[field: FieldOffset(20)]
		public int RightPlayerScore { get; set; }

		[Networked]
		[NetworkedWeaved(6, 129)]
		public unsafe NetworkString<_128> HighScorePlayerName
		{
			readonly get
			{
				return *(NetworkString<_128>*)Native.ReferenceToPointer(ref _HighScorePlayerName);
			}
			set
			{
				*(NetworkString<_128>*)Native.ReferenceToPointer(ref _HighScorePlayerName) = value;
			}
		}

		[field: FieldOffset(540)]
		public float RemainingTime { get; set; }

		[field: FieldOffset(544)]
		public float GameEndedTime { get; set; }

		[field: FieldOffset(548)]
		public int GameId { get; set; }

		[field: FieldOffset(552)]
		public int PickedMolesIndexCount { get; set; }

		[Networked]
		[Capacity(10)]
		[NetworkedWeavedDictionary(17, 1, 1, typeof(Fusion.ElementReaderWriterInt32), typeof(Fusion.ElementReaderWriterInt32))]
		[NetworkedWeaved(139, 71)]
		public unsafe NetworkDictionary<int, int> PickedMolesIndex => new NetworkDictionary<int, int>((int*)Native.ReferenceToPointer(ref _PickedMolesIndex), 17, Fusion.ElementReaderWriterInt32.GetInstance(), Fusion.ElementReaderWriterInt32.GetInstance());

		public WhackAMoleData(GameState state, int currentLevelIndex, int cScore, int tScore, int bScore, int rPScore, string hScorePName, float remainingTime, float endedTime, int gameId, Dictionary<int, int> moleIndexs)
		{
			CurrentState = state;
			CurrentLevelIndex = currentLevelIndex;
			CurrentScore = cScore;
			TotalScore = tScore;
			BestScore = bScore;
			RightPlayerScore = rPScore;
			HighScorePlayerName = hScorePName;
			RemainingTime = remainingTime;
			GameEndedTime = endedTime;
			GameId = gameId;
			PickedMolesIndexCount = moleIndexs.Count;
			foreach (KeyValuePair<int, int> moleIndex in moleIndexs)
			{
				PickedMolesIndex.Set(moleIndex.Key, moleIndex.Value);
			}
		}
	}

	public string machineId = "default";

	public GameObject molesContainerRight;

	[Tooltip("Only for co-op version")]
	public GameObject molesContainerLeft;

	public int betweenLevelPauseDuration = 3;

	public int countdownDuration = 5;

	public WhackAMoleLevelSO[] allLevels;

	[SerializeField]
	private GorillaTimer timer;

	[SerializeField]
	private AudioSource audioSource;

	public GameObject levelArrow;

	public GameObject victoryFX;

	public ZoneBasedObject[] zoneBasedVisuals;

	[SerializeField]
	private MeshRenderer[] zoneBasedMeshRenderers;

	[Space]
	public AudioClip backgroundLoop;

	public AudioClip errorClip;

	public AudioClip counterClip;

	public AudioClip levelCompleteClip;

	public AudioClip winClip;

	public AudioClip gameOverClip;

	public AudioClip[] whackHazardClips;

	public AudioClip[] whackMonkeClips;

	[Space]
	public GameObject welcomeUI;

	public GameObject ongoingGameUI;

	public GameObject levelEndedUI;

	public GameObject ContinuePressedUI;

	public GameObject multiplyareScoresUI;

	[Space]
	public TextMeshPro scoreText;

	public TextMeshPro bestScoreText;

	[Tooltip("Only for co-op version")]
	public TextMeshPro rightPlayerScoreText;

	[Tooltip("Only for co-op version")]
	public TextMeshPro leftPlayerScoreText;

	public TextMeshPro timeText;

	public TextMeshPro counterText;

	public TextMeshPro resultText;

	public TextMeshPro levelEndedOptionsText;

	public TextMeshPro levelEndedCountdownText;

	public TextMeshPro levelEndedTotalScoreText;

	public TextMeshPro levelEndedCurrentScoreText;

	private List<Mole> rightMolesList;

	private List<Mole> leftMolesList;

	private List<Mole> molesList = new List<Mole>();

	private WhackAMoleLevelSO currentLevel;

	private int currentScore;

	private int totalScore;

	private int leftPlayerScore;

	private int rightPlayerScore;

	private int bestScore;

	private float curentTime;

	private int currentLevelIndex;

	private float continuePressedTime;

	private bool resetToFirstLevel;

	private Quaternion arrowTargetRotation;

	private bool arrowRotationNeedsUpdate;

	private List<Mole> potentialMoles = new List<Mole>();

	private Dictionary<int, int> pickedMolesIndex = new Dictionary<int, int>();

	private GameState currentState;

	private GameState lastState;

	private float remainingTime;

	private int previousTime = -1;

	private bool isMultiplayer;

	private float gameEndedTime;

	private GameResult curentGameResult;

	private string playerName = string.Empty;

	private string highScorePlayerName = string.Empty;

	private ParticleSystem[] victoryParticles;

	private int levelHazardMolesPicked;

	private int levelGoodMolesPicked;

	private string playerId;

	private int gameId;

	private int levelHazardMolesHit;

	private static DateTime epoch = new DateTime(2024, 1, 1);

	private static int lastAssignedID;

	private bool wasMasterClient;

	private bool wasLocalPlayerInZone = true;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 210)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private WhackAMoleData _Data;

	[Networked]
	[NetworkedWeaved(0, 210)]
	public unsafe WhackAMoleData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing WhackAMole.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(WhackAMoleData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing WhackAMole.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(WhackAMoleData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	private void UpdateMeshRendererList()
	{
		List<MeshRenderer> list = new List<MeshRenderer>();
		ZoneBasedObject[] array = zoneBasedVisuals;
		for (int i = 0; i < array.Length; i++)
		{
			MeshRenderer[] componentsInChildren = array[i].GetComponentsInChildren<MeshRenderer>(includeInactive: true);
			foreach (MeshRenderer meshRenderer in componentsInChildren)
			{
				if (meshRenderer.enabled)
				{
					list.Add(meshRenderer);
				}
			}
		}
		zoneBasedMeshRenderers = list.ToArray();
	}

	protected override void Awake()
	{
		base.Awake();
		if (molesContainerRight != null)
		{
			rightMolesList = new List<Mole>(molesContainerRight.GetComponentsInChildren<Mole>());
			if (rightMolesList.Count > 0)
			{
				molesList.AddRange(rightMolesList);
			}
		}
		if (molesContainerLeft != null)
		{
			leftMolesList = new List<Mole>(molesContainerLeft.GetComponentsInChildren<Mole>());
			if (leftMolesList.Count > 0)
			{
				molesList.AddRange(leftMolesList);
				foreach (Mole leftMoles in leftMolesList)
				{
					leftMoles.IsLeftSideMole = true;
				}
			}
		}
		currentLevelIndex = -1;
		foreach (Mole moles in molesList)
		{
			moles.OnTapped += OnMoleTapped;
		}
		List<Mole> list = leftMolesList;
		int num;
		if (list != null && list.Count > 0)
		{
			list = rightMolesList;
			num = ((list != null && list.Count > 0) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		isMultiplayer = (byte)num != 0;
		welcomeUI.SetActive(value: false);
		ongoingGameUI.SetActive(value: false);
		levelEndedUI.SetActive(value: false);
		ContinuePressedUI.SetActive(value: false);
		multiplyareScoresUI.SetActive(value: false);
		bestScore = 0;
		bestScoreText.text = string.Empty;
		highScorePlayerName = string.Empty;
		victoryParticles = victoryFX.GetComponentsInChildren<ParticleSystem>();
	}

	protected override void Start()
	{
		base.Start();
		SwitchState(GameState.Off);
		if ((bool)WhackAMoleManager.instance)
		{
			WhackAMoleManager.instance.Register(this);
		}
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		foreach (Mole moles in molesList)
		{
			moles.OnTapped -= OnMoleTapped;
		}
		if ((bool)WhackAMoleManager.instance)
		{
			WhackAMoleManager.instance.Unregister(this);
		}
		molesList.Clear();
	}

	public void InvokeUpdate()
	{
		bool isMasterClient = NetworkSystem.Instance.IsMasterClient;
		bool flag = zoneBasedVisuals[0].IsLocalPlayerInZone();
		if (isMasterClient != wasMasterClient || flag != wasLocalPlayerInZone)
		{
			MeshRenderer[] array = zoneBasedMeshRenderers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = flag;
			}
			bool active = isMasterClient || flag;
			ZoneBasedObject[] array2 = zoneBasedVisuals;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].gameObject.SetActive(active);
			}
			wasMasterClient = isMasterClient;
			wasLocalPlayerInZone = flag;
		}
	}

	private void SwitchState(GameState state)
	{
		lastState = currentState;
		currentState = state;
		switch (currentState)
		{
		case GameState.Off:
			ResetGame();
			currentLevelIndex = -1;
			currentLevel = null;
			UpdateLevelUI(1);
			break;
		case GameState.Ongoing:
			UpdateScoreUI(currentScore, leftPlayerScore, rightPlayerScore);
			break;
		case GameState.TimesUp:
		{
			if (!(currentLevel != null))
			{
				break;
			}
			foreach (Mole moles in molesList)
			{
				moles.HideMole();
			}
			curentGameResult = GetGameResult();
			UpdateResultUI(curentGameResult);
			levelEndedTotalScoreText.text = "SCORE " + totalScore;
			levelEndedCurrentScoreText.text = $"{currentScore}/{currentLevel.GetMinScore(isMultiplayer)}";
			if (totalScore > bestScore)
			{
				bestScore = totalScore;
				highScorePlayerName = playerName;
			}
			bestScoreText.text = (isMultiplayer ? bestScore.ToString() : (highScorePlayerName + "  " + bestScore));
			audioSource.GTStop();
			if (curentGameResult == GameResult.LevelComplete)
			{
				audioSource.GTPlayOneShot(levelCompleteClip);
				if (NetworkSystem.Instance.LocalPlayer.UserId == playerId)
				{
					PlayerGameEvents.MiscEvent("WhackComplete" + currentLevel.levelNumber);
				}
			}
			else if (curentGameResult == GameResult.GameOver)
			{
				audioSource.GTPlayOneShot(gameOverClip);
			}
			else if (curentGameResult == GameResult.Win)
			{
				audioSource.GTPlayOneShot(winClip);
				if ((bool)victoryFX)
				{
					ParticleSystem[] array = victoryParticles;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].Play();
					}
				}
				if (NetworkSystem.Instance.LocalPlayer.UserId == playerId)
				{
					PlayerGameEvents.MiscEvent("WhackComplete" + currentLevel.levelNumber);
				}
			}
			int minScore = currentLevel.GetMinScore(isMultiplayer);
			if (levelGoodMolesPicked < minScore)
			{
				GTDev.LogError($"[WAM] Lvl:{currentLevel.levelNumber} Only Picked {levelGoodMolesPicked}/{minScore} good moles!");
			}
			if (base.IsMine)
			{
				GorillaTelemetry.WamLevelEnd(playerId, gameId, machineId, currentLevel.levelNumber, levelGoodMolesPicked, levelHazardMolesPicked, minScore, currentScore, levelHazardMolesHit, curentGameResult.ToString());
			}
			break;
		}
		case GameState.ContinuePressed:
			continuePressedTime = Time.time;
			audioSource.GTStop();
			audioSource.GTPlayOneShot(counterClip);
			if (base.IsMine)
			{
				pickedMolesIndex.Clear();
			}
			ResetGame();
			if (base.IsMine)
			{
				LoadNextLevel();
			}
			break;
		}
		UpdateScreenData();
	}

	private void UpdateScreenData()
	{
		switch (currentState)
		{
		case GameState.Off:
			welcomeUI.SetActive(value: true);
			ContinuePressedUI.SetActive(value: false);
			ongoingGameUI.SetActive(value: false);
			levelEndedUI.SetActive(value: false);
			multiplyareScoresUI.SetActive(value: false);
			break;
		case GameState.Ongoing:
			ContinuePressedUI.SetActive(value: false);
			welcomeUI.SetActive(value: false);
			ongoingGameUI.SetActive(value: true);
			levelEndedUI.SetActive(value: false);
			if (isMultiplayer)
			{
				multiplyareScoresUI.SetActive(value: true);
			}
			break;
		case GameState.TimesUp:
			welcomeUI.SetActive(value: false);
			ongoingGameUI.SetActive(value: false);
			ContinuePressedUI.SetActive(value: false);
			if (isMultiplayer)
			{
				multiplyareScoresUI.SetActive(value: true);
			}
			levelEndedUI.SetActive(value: true);
			break;
		case GameState.ContinuePressed:
			levelEndedUI.SetActive(value: false);
			welcomeUI.SetActive(value: false);
			ongoingGameUI.SetActive(value: false);
			multiplyareScoresUI.SetActive(value: false);
			ContinuePressedUI.SetActive(value: true);
			break;
		case GameState.PickMoles:
			break;
		}
	}

	public static int CreateNewGameID()
	{
		int num = (int)((DateTime.Now - epoch).TotalSeconds * 8.0 % 2147483646.0) + 1;
		if (num <= lastAssignedID)
		{
			lastAssignedID++;
			return lastAssignedID;
		}
		lastAssignedID = num;
		return num;
	}

	private void OnMoleTapped(MoleTypes moleType, Vector3 position, bool isLocalTap, bool isLeftHand)
	{
		GameState gameState = currentState;
		if (gameState != GameState.Off && gameState != GameState.TimesUp)
		{
			AudioClip clip = (moleType.isHazard ? whackHazardClips[UnityEngine.Random.Range(0, whackHazardClips.Length)] : whackMonkeClips[UnityEngine.Random.Range(0, whackMonkeClips.Length)]);
			if (moleType.isHazard)
			{
				audioSource.GTPlayOneShot(clip);
				levelHazardMolesHit++;
			}
			else
			{
				audioSource.GTPlayOneShot(clip);
			}
			if (moleType.monkeMoleHitMaterial != null)
			{
				moleType.MeshRenderer.material = moleType.monkeMoleHitMaterial;
			}
			currentScore += moleType.scorePoint;
			totalScore += moleType.scorePoint;
			if (moleType.IsLeftSideMoleType)
			{
				leftPlayerScore += moleType.scorePoint;
			}
			else
			{
				rightPlayerScore += moleType.scorePoint;
			}
			UpdateScoreUI(currentScore, leftPlayerScore, rightPlayerScore);
			moleType.MoleContainerParent.HideMole(isHit: true);
		}
	}

	public void HandleOnTimerStopped()
	{
		gameEndedTime = Time.time;
		SwitchState(GameState.TimesUp);
	}

	private IEnumerator PlayHazardAudio(AudioClip clip)
	{
		audioSource.clip = clip;
		audioSource.GTPlay();
		yield return new WaitForSeconds(audioSource.clip.length);
		audioSource.clip = errorClip;
		audioSource.GTPlay();
	}

	private bool PickMoles()
	{
		pickedMolesIndex.Clear();
		float passedTime = timer.GetPassedTime();
		if (passedTime > currentLevel.levelDuration - currentLevel.showMoleDuration)
		{
			return true;
		}
		float t = passedTime / currentLevel.levelDuration;
		float minMoleCount = Mathf.Lerp(currentLevel.minimumMoleCount.x, currentLevel.minimumMoleCount.y, t);
		float maxMoleCount = Mathf.Lerp(currentLevel.maximumMoleCount.x, currentLevel.maximumMoleCount.y, t);
		curentTime = Time.time;
		float hazardMoleChance = Mathf.Lerp(currentLevel.hazardMoleChance.x, currentLevel.hazardMoleChance.y, t);
		if (isMultiplayer)
		{
			PickMolesFrom(rightMolesList);
			PickMolesFrom(leftMolesList);
		}
		else
		{
			PickMolesFrom(molesList);
		}
		return pickedMolesIndex.Count != 0;
		void PickMolesFrom(List<Mole> moles)
		{
			int a = Mathf.RoundToInt(UnityEngine.Random.Range(minMoleCount, maxMoleCount));
			potentialMoles.Clear();
			foreach (Mole mole in moles)
			{
				if (mole.CanPickMole())
				{
					potentialMoles.Add(mole);
				}
			}
			int num = Mathf.Min(a, potentialMoles.Count);
			int num2 = Mathf.CeilToInt((float)num * hazardMoleChance);
			int num3 = 0;
			for (int i = 0; i < num; i++)
			{
				int index = UnityEngine.Random.Range(0, potentialMoles.Count);
				if (PickSingleMole(molesList.IndexOf(potentialMoles[index]), (num3 < num2) ? hazardMoleChance : 0f))
				{
					num3++;
				}
				potentialMoles.RemoveAt(index);
			}
		}
	}

	private void LoadNextLevel()
	{
		if (currentLevel != null)
		{
			resetToFirstLevel = currentScore < currentLevel.GetMinScore(isMultiplayer);
			if (resetToFirstLevel)
			{
				currentLevelIndex = 0;
			}
			else
			{
				currentLevelIndex++;
			}
			if (currentLevelIndex >= allLevels.Length)
			{
				currentLevelIndex = 0;
			}
		}
		else
		{
			currentLevelIndex++;
		}
		currentLevel = allLevels[currentLevelIndex];
		timer.SetTimerDuration(currentLevel.levelDuration);
		timer.RestartTimer();
		curentTime = Time.time;
		currentScore = 0;
		leftPlayerScore = 0;
		rightPlayerScore = 0;
		levelGoodMolesPicked = (levelHazardMolesPicked = 0);
		levelHazardMolesHit = 0;
		if (currentLevelIndex == 0)
		{
			totalScore = 0;
		}
		if (currentLevelIndex == 0 && base.IsMine)
		{
			gameId = CreateNewGameID();
			Debug.LogWarning("GAME ID" + gameId);
		}
	}

	private bool PickSingleMole(int randomMoleIndex, float hazardMoleChance)
	{
		bool flag = hazardMoleChance > 0f && UnityEngine.Random.value <= hazardMoleChance;
		int moleTypeIndex = molesList[randomMoleIndex].GetMoleTypeIndex(flag);
		molesList[randomMoleIndex].ShowMole(currentLevel.showMoleDuration, moleTypeIndex);
		pickedMolesIndex.Add(randomMoleIndex, moleTypeIndex);
		if (flag)
		{
			levelHazardMolesPicked++;
		}
		else
		{
			levelGoodMolesPicked++;
		}
		return flag;
	}

	private void ResetGame()
	{
		foreach (Mole moles in molesList)
		{
			moles.ResetPosition();
		}
	}

	private void UpdateScoreUI(int totalScore, int _leftPlayerScore, int _rightPlayerScore)
	{
		if (currentLevel != null)
		{
			scoreText.text = $"SCORE\n{totalScore}/{currentLevel.GetMinScore(isMultiplayer)}";
			leftPlayerScoreText.text = _leftPlayerScore.ToString();
			rightPlayerScoreText.text = _rightPlayerScore.ToString();
		}
	}

	private void UpdateLevelUI(int levelNumber)
	{
		arrowTargetRotation = Quaternion.Euler(0f, 0f, 18 * (levelNumber - 1));
		arrowRotationNeedsUpdate = true;
	}

	private void UpdateArrowRotation()
	{
		Quaternion quaternion = Quaternion.Slerp(levelArrow.transform.localRotation, arrowTargetRotation, Time.deltaTime * 5f);
		if (Quaternion.Angle(quaternion, arrowTargetRotation) < 0.1f)
		{
			quaternion = arrowTargetRotation;
			arrowRotationNeedsUpdate = false;
		}
		levelArrow.transform.localRotation = quaternion;
	}

	private void UpdateTimerUI(int time)
	{
		if (time != previousTime)
		{
			timeText.text = "TIME " + time;
			previousTime = time;
		}
	}

	private void UpdateResultUI(GameResult gameResult)
	{
		switch (gameResult)
		{
		case GameResult.LevelComplete:
			resultText.text = "LEVEL COMPLETE";
			break;
		case GameResult.Win:
			resultText.text = "YOU WIN!";
			break;
		case GameResult.GameOver:
			resultText.text = "GAME OVER";
			break;
		}
	}

	public void OnStartButtonPressed()
	{
		GameState gameState = currentState;
		if (gameState == GameState.TimesUp || gameState == GameState.Off)
		{
			base.GetView.RPC("WhackAMoleButtonPressed", RpcTarget.All);
		}
	}

	[PunRPC]
	private void WhackAMoleButtonPressed(PhotonMessageInfo info)
	{
		WhackAMoleButtonPressedShared(info);
	}

	[Rpc]
	private unsafe void RPC_WhackAMoleButtonPressed(RpcInfo info = default(RpcInfo))
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
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void GorillaTagScripts.WhackAMole::RPC_WhackAMoleButtonPressed(Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			int num = 8;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.WhackAMole::RPC_WhackAMoleButtonPressed(Fusion.RpcInfo)", num);
				return;
			}
			if (base.Runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 1);
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
		WhackAMoleButtonPressedShared(info);
	}

	private void WhackAMoleButtonPressedShared(PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "WhackAMoleButtonPressedShared");
		VRRig vRRig = GorillaGameManager.StaticFindRigForPlayer(info.Sender);
		if ((bool)vRRig)
		{
			playerName = vRRig.playerNameVisible;
			if (currentState == GameState.Off)
			{
				playerId = info.Sender.UserId;
				if (NetworkSystem.Instance.LocalPlayer.UserId == playerId)
				{
					PlayerGameEvents.MiscEvent("PlayArcadeGame");
				}
			}
		}
		SwitchState(GameState.ContinuePressed);
	}

	private GameResult GetGameResult()
	{
		if (currentScore >= currentLevel.GetMinScore(isMultiplayer))
		{
			if (currentLevelIndex >= allLevels.Length - 1)
			{
				return GameResult.Win;
			}
			return GameResult.LevelComplete;
		}
		return GameResult.GameOver;
	}

	public int GetCurrentLevel()
	{
		if (currentLevel != null)
		{
			return currentLevel.levelNumber;
		}
		return 0;
	}

	public int GetTotalLevelNumbers()
	{
		if (allLevels != null)
		{
			return allLevels.Length;
		}
		return 0;
	}

	public override void WriteDataFusion()
	{
		Data = new WhackAMoleData(currentState, currentLevelIndex, currentScore, totalScore, bestScore, rightPlayerScore, highScorePlayerName, timer.GetRemainingTime(), gameEndedTime, gameId, pickedMolesIndex);
		pickedMolesIndex.Clear();
	}

	public override void ReadDataFusion()
	{
		ReadDataShared(Data.CurrentState, Data.CurrentLevelIndex, Data.CurrentScore, Data.TotalScore, Data.BestScore, Data.RightPlayerScore, Data.HighScorePlayerName.Value, Data.RemainingTime, Data.GameEndedTime, Data.GameId);
		for (int i = 0; i < Data.PickedMolesIndexCount; i++)
		{
			int randomMoleTypeIndex = Data.PickedMolesIndex[i];
			if (i >= 0 && i < molesList.Count && (bool)currentLevel)
			{
				molesList[i].ShowMole(currentLevel.showMoleDuration, randomMoleTypeIndex);
			}
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	private void ReadDataShared(GameState _currentState, int _currentLevelIndex, int cScore, int tScore, int bScore, int rPScore, string hScorePName, float _remainingTime, float endedTime, int _gameId)
	{
		GameState gameState = currentState;
		if (_currentState != gameState)
		{
			SwitchState(_currentState);
		}
		currentLevelIndex = _currentLevelIndex;
		if (currentLevelIndex >= 0 && currentLevelIndex < allLevels.Length)
		{
			currentLevel = allLevels[currentLevelIndex];
			UpdateLevelUI(currentLevel.levelNumber);
		}
		currentScore = cScore;
		totalScore = tScore;
		bestScore = bScore;
		rightPlayerScore = rPScore;
		leftPlayerScore = currentScore - rightPlayerScore;
		highScorePlayerName = hScorePName;
		bestScoreText.text = (isMultiplayer ? bestScore.ToString() : (highScorePlayerName + "  " + bestScore));
		remainingTime = _remainingTime;
		if (float.IsFinite(remainingTime) && (bool)currentLevel)
		{
			remainingTime = remainingTime.ClampSafe(0f, currentLevel.levelDuration);
			UpdateTimerUI((int)remainingTime);
		}
		if (float.IsFinite(endedTime))
		{
			gameEndedTime = endedTime.ClampSafe(0f, Time.time);
		}
		gameId = _gameId;
	}

	protected override void OnOwnerSwitched(NetPlayer newOwningPlayer)
	{
		base.OnOwnerSwitched(newOwningPlayer);
		if (NetworkSystem.Instance.IsMasterClient)
		{
			timer.RestartTimer();
			timer.SetTimerDuration(remainingTime);
			curentTime = Time.time;
			if (currentLevelIndex >= 0 && currentLevelIndex < allLevels.Length)
			{
				currentLevel = allLevels[currentLevelIndex];
			}
			SwitchState(currentState);
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}

	[NetworkRpcWeavedInvoker(1, 7, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_WhackAMoleButtonPressed_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((WhackAMole)behaviour).RPC_WhackAMoleButtonPressed(info);
	}
}
