using System;
using System.Collections.Generic;
using GorillaGameModes;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

public sealed class GorillaPropHuntGameManager : GorillaTagManager
{
	private enum EPropHuntGameState
	{
		Invalid,
		StoppedGameMode,
		StartingGameMode,
		WaitingForMorePlayers,
		WaitingForRoundToStart,
		Hiding,
		Playing
	}

	private const string preLog = "GorillaPropHuntGameManager: ";

	private const string preLogEd = "(editor only log) GorillaPropHuntGameManager: ";

	private const string preLogBeta = "(beta only log) GorillaPropHuntGameManager: ";

	private const string preErr = "ERROR!!!  GorillaPropHuntGameManager: ";

	private const string preErrEd = "ERROR!!!  (editor only log) GorillaPropHuntGameManager: ";

	private const string preErrBeta = "ERROR!!!  (beta only log) GorillaPropHuntGameManager: ";

	private const bool _k__GT_PROP_HUNT__USE_POOLING__ = true;

	[FormerlySerializedAs("allCosmetics")]
	[SerializeField]
	private AllCosmeticsArraySO m_ph_allCosmetics;

	[FormerlySerializedAs("backupCosmetic")]
	[FormerlySerializedAs("m_ph_backupCosmetic")]
	[SerializeField]
	private CosmeticSO m_ph_fallbackPropCosmeticSO;

	[Tooltip("This us used by PropHuntPools as the parent gameobject that the cosmetic prefab instance will be parented to.")]
	[FormerlySerializedAs("m_ph_propPlacementPrefab")]
	[SerializeField]
	private PropPlacementRB m_ph_propDecoyPrefab;

	[Tooltip("The time that players have to hide before their props can be seen by the tagger monke.")]
	[FormerlySerializedAs("m_propHunt_hideState_duration")]
	[SerializeField]
	private float m_ph_hideState_duration = 10f;

	[Tooltip("Prefab that will be parented to the camera if the current player is not a ghost during hiding state.")]
	[FormerlySerializedAs("m_propHunt_blindfold_1stPersonPrefab")]
	[SerializeField]
	private GameObject m_ph_blindfold_forCameraPrefab;

	private GameObject _ph_blindfold_forCamera_1p;

	private GameObject _ph_blindfold_forCamera_3p;

	private bool _ph_blindfold_forCamera_isInitialized;

	[Tooltip("Prefab to cover the eyes of the non-ghost gorilla's avatar during the hiding state.")]
	[FormerlySerializedAs("m_propHunt_blindfold_3rdPersonPrefab")]
	[SerializeField]
	private GameObject m_ph_blindfold_forAvatarPrefab;

	private readonly Dictionary<int, GameObject> _ph_vrRig_to_blindfolds = new Dictionary<int, GameObject>(20);

	[Tooltip("A randomly picked sound in this soundbank will be played when the hide state starts.")]
	[FormerlySerializedAs("m_propHunt_hideState_startSoundBank")]
	[SerializeField]
	private SoundBankPlayer m_ph_hideState_startSoundBank;

	[FormerlySerializedAs("m_propHunt_hideState_warnSoundBank")]
	[Tooltip("A randomly picked Sound in this Sound Bank will be played to warn players that the hiding period is ending.")]
	[FormerlySerializedAs("m_propHunt_hideState_startSoundBank")]
	[SerializeField]
	private SoundBankPlayer m_ph_hideState_warnSoundBank;

	[FormerlySerializedAs("m_propHunt_hideState_warnSoundBank_playCount")]
	[Tooltip("How many times should the warning sound play before the hiding period ends? Will play every 1 second.")]
	[SerializeField]
	private int m_ph_hideState_warnSoundBank_playCount = 3;

	private int _ph_hideState_warnSounds_timesPlayed;

	[FormerlySerializedAs("m_propHunt_playState_startSoundBank")]
	[Tooltip("A randomly picked sound in this Sound Bank will be played when the hiding state ends and the playing state has started.")]
	[SerializeField]
	private SoundBankPlayer m_ph_playState_startSoundBank;

	[FormerlySerializedAs("m_propHunt_playState_startLightning_manager_ref")]
	[Tooltip("Lightning manager for doing lightning strike strikes when playing starts.")]
	[SerializeField]
	private XSceneRef m_ph_playState_startLightning_manager_ref;

	private LightningManager _ph_playState_startLightning_manager;

	private bool _ph_playState_startLightning_manager_isResolved;

	[Tooltip("How long after the playing starts should the lightning strikes happen?")]
	private float[] m_ph_playState_startLightning_strikeTimes = new float[3] { 1f, 1.5f, 1.8f };

	private int _ph_playState_startLightning_strikeTimes_index;

	[Tooltip("A randomly picked sound in this Sound Bank will be played when the ghost is tagged by the hunter.")]
	[SerializeField]
	private SoundBankPlayer m_ph_playState_taggedSoundBank;

	[Tooltip("Maximum distance prop can be from the center of the player's hand")]
	[SerializeField]
	private float m_ph_hand_follow_distance = 0.35f;

	[FormerlySerializedAs("_playBoundary_xSceneRef")]
	[FormerlySerializedAs("_playZone_xSceneRef")]
	[SerializeField]
	private XSceneRef m_ph_playBoundary_xSceneRef;

	[Tooltip("A list of Transforms representing potential end positions for the playable boundary each round.")]
	[SerializeField]
	private List<Transform> m_ph_playBoundary_endPointTransforms = new List<Transform>();

	private PlayableBoundaryManager _ph_playBoundary;

	private bool _ph_playBoundary_isResolved;

	private Vector3 _ph_playBoundary_initialPosition;

	private bool _ph_playBoundary_initialPosition_isInitialized;

	private Vector3 _ph_playBoundary_currentTargetPosition;

	private bool _ph_playBoundary_hasTargetPositionForRound;

	[Tooltip("The maximum time a player can be outside of the boundary before being tagged.")]
	[SerializeField]
	private float m_ph_playBoundary_timeLimit = 15f;

	[Tooltip("On the What does 1.0 on the X axis")]
	[FormerlySerializedAs("_playBoundary_radiusScaleOverRoundTime_maxTime")]
	[SerializeField]
	private float m_ph_playBoundary_radiusScaleOverRoundTime_maxTime = 180f;

	[FormerlySerializedAs("_playBoundary_radiusScaleOverRoundTime_curve")]
	[FormerlySerializedAs("_playZoneRadiusOverRoundTime")]
	[SerializeField]
	private AnimationCurve m_ph_playBoundary_radiusScaleOverRoundTime_curve = new AnimationCurve(new Keyframe(0f, 1f, 1f, 1f, 0f, 0f), new Keyframe(0.9f, 0.01f, 1f, 0f, 0f, 0f), new Keyframe(1f, 0.01f, 1f, 0f, 0f, 0f));

	[FormerlySerializedAs("_ph_gorillaGhostBodyMaterial")]
	[FormerlySerializedAs("gorillaGhostBodyMaterial")]
	[SerializeField]
	private Material m_ph_gorillaGhostBodyMaterial;

	private int _ph_gorillaGhostBodyMaterialIndex = -1;

	[Tooltip("A randomly picked sound in this Sound Bank will be played when the spectral plane border is crossed.")]
	[SerializeField]
	private SoundBankPlayer m_ph_planeCrossingSoundBank;

	[Tooltip("This AudioSource will only be heard by the local player and is non directional.")]
	[FormerlySerializedAs("m_soundNearBorder_audioSource")]
	[FormerlySerializedAs("soundNearBorderAudioSource")]
	[FormerlySerializedAs("soundNearBoundaryAudioSource")]
	[SerializeField]
	private AudioSource m_ph_soundNearBorder_audioSource;

	[FormerlySerializedAs("m_soundNearBorder_maxDistance")]
	[FormerlySerializedAs("soundNearBorderMaxDistance")]
	[FormerlySerializedAs("soundNearBoundaryMaxDistance")]
	[SerializeField]
	private float m_ph_soundNearBorder_maxDistance = 2f;

	[FormerlySerializedAs("m_soundNearBorder_volumeCurve")]
	[FormerlySerializedAs("soundNearBorderVolumeCurve")]
	[FormerlySerializedAs("soundNearBoundaryVolumeCurve")]
	[SerializeField]
	private AnimationCurve m_ph_soundNearBorder_volumeCurve = AnimationCurves.Linear;

	[Tooltip("The resulting volume curve value is multiplied by this.")]
	[FormerlySerializedAs("m_soundNearBorder_baseVolume")]
	[SerializeField]
	private float m_ph_soundNearBorder_baseVolume = 0.5f;

	[FormerlySerializedAs("m_hapticsNearBorder_borderProximity")]
	[SerializeField]
	private float m_ph_hapticsNearBorder_borderProximity = 2f;

	[FormerlySerializedAs("m_hapticsNearBorder_ampCurve")]
	[SerializeField]
	private AnimationCurve m_ph_hapticsNearBorder_ampCurve = AnimationCurves.Linear;

	[FormerlySerializedAs("m_hapticsNearBorder_baseAmp")]
	[SerializeField]
	private float m_ph_hapticsNearBorder_baseAmp = 1f;

	private bool _ph_isLocalPlayerSkeleton;

	[OnEnterPlay_Clear]
	private static readonly Dictionary<int, PlayableBoundaryTracker> _g_ph_rig_to_propHuntZoneTrackers = new Dictionary<int, PlayableBoundaryTracker>(10);

	[OnEnterPlay_Set(0f)]
	private static float _g_ph_hapticsLastImpulseEndTime;

	[OnEnterPlay_Clear]
	private static readonly List<VRRig> _g_ph_activePlayerRigs = new List<VRRig>(20);

	[OnEnterPlay_Clear]
	private static readonly List<PropHuntPropZone> _g_ph_allPropZones = new List<PropHuntPropZone>();

	[OnEnterPlay_Clear]
	private static readonly List<PropHuntHandFollower> _g_ph_allHandFollowers = new List<PropHuntHandFollower>();

	private static readonly string[] _g_ph_titleDataSeparators = new string[3] { "\"", " ", "\\n" };

	[OnEnterPlay_Set(-1)]
	private static int _g_ph_defaultStencilRefOfSkeletonMat = -1;

	[DebugReadout]
	private EPropHuntGameState _ph_gameState;

	private EPropHuntGameState _ph_gameState_lastUpdate;

	private bool _roundIsPlaying;

	private string[] _ph_allPropIDs_noPool;

	[DebugReadout]
	private float _ph_roundTime;

	private long __ph_timeRoundStartedMillis__;

	private int _ph_randomSeed;

	private bool _ph_isLocalPlayerParticipating;

	private bool _isListeningTo_Pools_OnReady;

	private bool _isListeningForXSceneRefLoadCallbacks;

	public new static GorillaPropHuntGameManager instance { get; private set; }

	public PropPlacementRB PropDecoyPrefab => m_ph_propDecoyPrefab;

	public float HandFollowDistance => m_ph_hand_follow_distance;

	public bool RoundIsPlaying => _roundIsPlaying;

	public string[] AllPropIDs_NoPool => PropHuntPools.AllPropCosmeticIds;

	[DebugReadout]
	private long _ph_timeRoundStartedMillis
	{
		get
		{
			return __ph_timeRoundStartedMillis__;
		}
		set
		{
			__ph_timeRoundStartedMillis__ = value;
		}
	}

	public bool IsReadyToSpawnProps_NoPool => PropHuntPools.IsReady;

	public override GameModeType GameType()
	{
		return GameModeType.PropHunt;
	}

	public override string GameModeName()
	{
		return "PROP HUNT";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_PROP_HUNT_ROOM_LABEL", out var result, "(PROP HUNT GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_PROP_HUNT_ROOM_LABEL]");
		}
		return result;
	}

	public int GetSeed()
	{
		return _ph_randomSeed;
	}

	public override void Awake()
	{
		instance = this;
		PhotonNetwork.AddCallbackTarget(this);
		base.Awake();
	}

	private void Start()
	{
		PropHuntPools.StartInitializingPropsList(m_ph_allCosmetics, m_ph_fallbackPropCosmeticSO);
		if (_ph_gorillaGhostBodyMaterialIndex == -1)
		{
			_Initialize_gorillaGhostBodyMaterialIndex();
		}
		_Initialize_defaultStencilRefOfSkeletonMat();
	}

	private void _ProcessPropsList_NoPool(string titleDataPropsLines)
	{
		_ph_allPropIDs_noPool = titleDataPropsLines.Split(_g_ph_titleDataSeparators, StringSplitOptions.RemoveEmptyEntries);
	}

	public override void StartPlaying()
	{
		base.StartPlaying();
		_ = PhotonNetwork.IsMasterClient;
		_ResolveXSceneRefs();
		GameMode.ParticipatingPlayersChanged += _OnParticipatingPlayersChanged;
		_UpdateParticipatingPlayers();
		if (m_ph_soundNearBorder_audioSource != null)
		{
			m_ph_soundNearBorder_audioSource.volume = 0f;
		}
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		_ph_gameState = EPropHuntGameState.StoppedGameMode;
		GameMode.ParticipatingPlayersChanged -= _OnParticipatingPlayersChanged;
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			GorillaSkin.ApplyToRig(activeRig, null, GorillaSkin.SkinType.gameMode);
			_ResetRigAppearance(activeRig);
		}
		CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(hideCosmetics: false);
		EquipmentInteractor.instance.ForceDropAnyEquipment();
		if (m_ph_soundNearBorder_audioSource != null)
		{
			m_ph_soundNearBorder_audioSource.volume = 0f;
		}
		if (_ph_playBoundary_isResolved)
		{
			_ph_playBoundary.enabled = false;
			if (_ph_playBoundary_initialPosition_isInitialized)
			{
				_ph_playBoundary.transform.position = _ph_playBoundary_initialPosition;
			}
		}
		_ph_playBoundary_hasTargetPositionForRound = false;
	}

	public override bool CanPlayerParticipate(NetPlayer player)
	{
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			VRRig rig = playerRig.Rig;
			if (rig.zoneEntity.currentZone == GTZone.bayou)
			{
				return rig.zoneEntity.currentSubZone != GTSubZone.entrance_tunnel;
			}
			return false;
		}
		return true;
	}

	private void _OnParticipatingPlayersChanged(List<NetPlayer> addedPlayers, List<NetPlayer> removedPlayers)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			for (int i = 0; i < addedPlayers.Count; i++)
			{
				NetPlayer infectedPlayer = addedPlayers[i];
				AddInfectedPlayer(infectedPlayer);
			}
		}
		for (int j = 0; j < removedPlayers.Count; j++)
		{
			NetPlayer netPlayer = removedPlayers[j];
			if (!VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig))
			{
				continue;
			}
			if (PhotonNetwork.IsMasterClient)
			{
				while (currentInfected.Contains(netPlayer))
				{
					currentInfected.Remove(netPlayer);
				}
			}
			VRRig rig = playerRig.Rig;
			_ResetRigAppearance(rig);
		}
		if (PhotonNetwork.IsMasterClient)
		{
			UpdateInfectionState();
		}
	}

	public override void NewVRRig(NetPlayer player, int vrrigPhotonViewID, bool didTutorial)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			bool num = isCurrentlyTag;
			UpdateState();
			if (!num && !isCurrentlyTag)
			{
				UpdateInfectionState();
			}
		}
	}

	public override void Tick()
	{
		base.Tick();
		_UpdateParticipatingPlayers();
		_UpdateGameState();
		if (_ph_playBoundary_isResolved)
		{
			_ph_playBoundary.enabled = _ph_isLocalPlayerParticipating;
			float num = ((_ph_gameState != EPropHuntGameState.Playing) ? 0f : Mathf.Clamp01(_ph_roundTime / m_ph_playBoundary_radiusScaleOverRoundTime_maxTime));
			_ph_playBoundary.radiusScale = m_ph_playBoundary_radiusScaleOverRoundTime_curve.Evaluate(num);
			if (_ph_playBoundary_hasTargetPositionForRound)
			{
				Vector3 position = Vector3.Lerp(_ph_playBoundary_initialPosition, _ph_playBoundary_currentTargetPosition, num);
				_ph_playBoundary.transform.position = position;
			}
			if (_ph_isLocalPlayerParticipating || (PhotonNetwork.IsMasterClient && GameMode.ParticipatingPlayers.Count > 0))
			{
				_ph_playBoundary.UpdateSim();
			}
		}
	}

	public void _UpdateParticipatingPlayers()
	{
		VRRigCache.Instance.GetActiveRigs(_g_ph_activePlayerRigs);
		for (int i = 0; i < _g_ph_activePlayerRigs.Count; i++)
		{
			VRRig vRRig = _g_ph_activePlayerRigs[i];
			bool flag = vRRig.zoneEntity.currentZone == GTZone.bayou && vRRig.zoneEntity.currentSubZone != GTSubZone.entrance_tunnel;
			bool flag2 = GameMode.ParticipatingPlayers.Contains(vRRig.OwningNetPlayer);
			if (flag && !flag2)
			{
				GameMode.OptIn(vRRig.OwningNetPlayer.ActorNumber);
			}
			else if (!flag && flag2)
			{
				GameMode.OptOut(vRRig.OwningNetPlayer.ActorNumber);
				_SetPlayerBlindfoldVisibility(vRRig, vRRig.OwningNetPlayer, shouldEnable: false);
			}
		}
		_ph_isLocalPlayerParticipating = GameMode.ParticipatingPlayers.Contains(VRRig.LocalRig.OwningNetPlayer);
		m_ph_soundNearBorder_audioSource.gameObject.SetActive(_ph_isLocalPlayerParticipating);
	}

	private void _UpdateGameState()
	{
		_ph_gameState_lastUpdate = _ph_gameState;
		long num = GTTime.TimeAsMilliseconds();
		if (GameMode.ParticipatingPlayers.Count < infectedModeThreshold)
		{
			_ph_gameState = EPropHuntGameState.WaitingForMorePlayers;
			_ph_roundTime = 0f;
		}
		else if (_ph_timeRoundStartedMillis <= 0 || num < _ph_timeRoundStartedMillis)
		{
			_ph_gameState = EPropHuntGameState.WaitingForRoundToStart;
			_ph_roundTime = 0f;
		}
		else
		{
			_ph_roundTime = (float)(num - _ph_timeRoundStartedMillis) / 1000f;
			_ph_gameState = ((_ph_roundTime < m_ph_hideState_duration) ? EPropHuntGameState.Hiding : EPropHuntGameState.Playing);
		}
		if (_ph_gameState != _ph_gameState_lastUpdate)
		{
			foreach (PlayableBoundaryTracker value2 in _g_ph_rig_to_propHuntZoneTrackers.Values)
			{
				value2.ResetValues();
			}
		}
		if (!_ph_isLocalPlayerParticipating && _g_ph_rig_to_propHuntZoneTrackers.TryGetValue(VRRig.LocalRig.GetInstanceID(), out var value))
		{
			value.ResetValues();
		}
		switch (_ph_gameState)
		{
		case EPropHuntGameState.Invalid:
			Debug.LogError("ERROR!!!  GorillaPropHuntGameManager: " + $"Game state was `{EPropHuntGameState.Invalid}` but should only be that when the app " + "starts and then assigned during `StartPlaying` call.");
			break;
		case EPropHuntGameState.StoppedGameMode:
		case EPropHuntGameState.StartingGameMode:
		case EPropHuntGameState.WaitingForMorePlayers:
			if (_ph_gameState != _ph_gameState_lastUpdate)
			{
				_ph_hideState_warnSounds_timesPlayed = 0;
				VRRig rig = VRRigCache.Instance.localRig.Rig;
				_ph_timeRoundStartedMillis = -1000L;
				_ResetRigAppearance(rig);
			}
			break;
		case EPropHuntGameState.WaitingForRoundToStart:
			_ph_hideState_warnSounds_timesPlayed = 0;
			if (PhotonNetwork.IsMasterClient && !waitingToStartNextInfectionGame)
			{
				ClearInfectionState();
				InfectionRoundEnd();
			}
			break;
		case EPropHuntGameState.Hiding:
		{
			if (_ph_gameState != _ph_gameState_lastUpdate && m_ph_hideState_startSoundBank != null && ZoneManagement.IsInZone(GTZone.bayou))
			{
				m_ph_hideState_startSoundBank.Play();
				if (!_ph_isLocalPlayerSkeleton)
				{
					m_ph_soundNearBorder_audioSource.volume = 0f;
				}
			}
			for (int j = 0; j < GameMode.ParticipatingPlayers.Count; j++)
			{
				NetPlayer netPlayer = GameMode.ParticipatingPlayers[j];
				if (currentInfected.Contains(netPlayer))
				{
					_SetPlayerBlindfoldVisibility(netPlayer, shouldEnable: true);
				}
			}
			int num5 = m_ph_hideState_warnSoundBank_playCount - _ph_hideState_warnSounds_timesPlayed;
			if (num5 <= 0)
			{
				break;
			}
			float num6 = m_ph_hideState_duration - (float)num5;
			if (_ph_roundTime > num6 && ZoneManagement.IsInZone(GTZone.bayou))
			{
				if (m_ph_hideState_warnSoundBank != null)
				{
					m_ph_hideState_warnSoundBank.Play();
				}
				_ph_hideState_warnSounds_timesPlayed++;
			}
			break;
		}
		case EPropHuntGameState.Playing:
		{
			if (_ph_gameState_lastUpdate != EPropHuntGameState.Playing)
			{
				_ph_hideState_warnSounds_timesPlayed = 0;
				_ph_playState_startLightning_strikeTimes_index = 0;
				if (m_ph_playState_startSoundBank != null && ZoneManagement.IsInZone(GTZone.bayou))
				{
					m_ph_playState_startSoundBank.Play();
				}
				for (int i = 0; i < _g_ph_activePlayerRigs.Count; i++)
				{
					VRRig vRRig = _g_ph_activePlayerRigs[i];
					_SetPlayerBlindfoldVisibility(vRRig, vRRig.OwningNetPlayer, shouldEnable: false);
				}
			}
			int num2 = m_ph_playState_startLightning_strikeTimes.Length;
			int num3 = math.min(_ph_playState_startLightning_strikeTimes_index, num2 - 1);
			if (num3 < num2 && _ph_playState_startLightning_manager_isResolved)
			{
				float num4 = _ph_roundTime - m_ph_hideState_duration;
				if (m_ph_playState_startLightning_strikeTimes[num3] <= num4)
				{
					_ph_playState_startLightning_strikeTimes_index++;
					_ph_playState_startLightning_manager.DoLightningStrike();
				}
			}
			break;
		}
		}
	}

	public override void UpdatePlayerAppearance(VRRig rig)
	{
		if (rig.zoneEntity.currentZone != GTZone.bayou || (rig.zoneEntity.currentZone == GTZone.bayou && rig.zoneEntity.currentSubZone == GTSubZone.entrance_tunnel))
		{
			return;
		}
		List<NetPlayer> participatingPlayers = GameMode.ParticipatingPlayers;
		bool flag = _GetRigShouldBeSkeleton(rig, participatingPlayers);
		_ph_isLocalPlayerSkeleton = _ph_isLocalPlayerParticipating && !IsInfected(NetworkSystem.Instance.LocalPlayer);
		GorillaBodyType gorillaBodyType = (flag ? GorillaBodyType.Skeleton : GorillaBodyType.Default);
		int num = (flag ? _ph_gorillaGhostBodyMaterialIndex : 0);
		if (gorillaBodyType != rig.bodyRenderer.gameModeBodyType)
		{
			rig.bodyRenderer.SetGameModeBodyType(gorillaBodyType);
			if (rig.setMatIndex != num)
			{
				rig.ChangeMaterialLocal(num);
			}
		}
		if (PropHuntPools.IsReady)
		{
			bool flag2 = flag;
			if (rig.propHuntHandFollower.hasProp != flag2)
			{
				if (flag2)
				{
					rig.propHuntHandFollower.CreateProp();
				}
				else
				{
					rig.propHuntHandFollower.DestroyProp();
				}
			}
		}
		float signedDistToBoundary = _UpdateBoundaryProximityState(rig, flag);
		bool flag3 = _ShouldRigBeVisible(rig, flag, signedDistToBoundary);
		if (!rig.isOfflineVRRig)
		{
			rig.SetInvisibleToLocalPlayer(!flag3);
			if (flag || GorillaBodyRenderer.ForceSkeleton)
			{
				rig.bodyRenderer.SetSkeletonBodyActive(flag3);
			}
		}
	}

	private bool _GetRigShouldBeSkeleton(VRRig rig, List<NetPlayer> participatingPlayers)
	{
		if (rig.zoneEntity.currentZone == GTZone.bayou && participatingPlayers.Count >= 2 && participatingPlayers.Contains(rig.OwningNetPlayer))
		{
			return !IsInfected(rig.Creator);
		}
		return false;
	}

	private bool _ShouldRigBeVisible(VRRig rig, bool shouldBeSkeleton, float signedDistToBoundary)
	{
		if (_ph_gameState != EPropHuntGameState.Hiding)
		{
			if (!rig.isOfflineVRRig && shouldBeSkeleton && !(signedDistToBoundary > 0f))
			{
				return _ph_isLocalPlayerSkeleton;
			}
			return true;
		}
		return false;
	}

	private float _UpdateBoundaryProximityState(VRRig rig, bool isSkeleton)
	{
		float num = float.MinValue;
		float num2 = float.MinValue;
		if (isSkeleton)
		{
			if (!_g_ph_rig_to_propHuntZoneTrackers.TryGetValue(rig.GetInstanceID(), out var value))
			{
				rig.bodyTransform.GetOrAddComponent<PlayableBoundaryTracker>(out value);
				_g_ph_rig_to_propHuntZoneTrackers[rig.GetInstanceID()] = value;
				if (_ph_playBoundary_isResolved)
				{
					_ph_playBoundary.tracked.AddIfNew(value);
				}
			}
			num = value.signedDistanceToBoundary;
			num2 = value.prevSignedDistanceToBoundary;
			if (PhotonNetwork.IsMasterClient && !value.IsInsideZone() && value.timeSinceCrossingBorder > m_ph_playBoundary_timeLimit)
			{
				AddInfectedPlayer(rig.OwningNetPlayer);
			}
		}
		if (rig.isOfflineVRRig)
		{
			CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(isSkeleton);
			if (isSkeleton)
			{
				float time = 1f - math.saturate((0f - num) / m_ph_soundNearBorder_maxDistance);
				AudioSource ph_soundNearBorder_audioSource = m_ph_soundNearBorder_audioSource;
				EPropHuntGameState ph_gameState = _ph_gameState;
				ph_soundNearBorder_audioSource.volume = ((ph_gameState == EPropHuntGameState.Hiding || ph_gameState == EPropHuntGameState.Playing) ? (m_ph_soundNearBorder_baseVolume * m_ph_soundNearBorder_volumeCurve.Evaluate(time)) : 0f);
				if (num >= 0f && num2 < 0f && !m_ph_planeCrossingSoundBank.isPlaying)
				{
					m_ph_planeCrossingSoundBank.Play();
				}
				_UpdateControllerHaptics(num);
			}
			else
			{
				m_ph_soundNearBorder_audioSource.volume = 0f;
			}
		}
		return num;
	}

	private void _UpdateControllerHaptics(float signedDistToBoundary)
	{
		if (!(Time.unscaledTime < _g_ph_hapticsLastImpulseEndTime) && !(math.abs(signedDistToBoundary) > m_ph_hapticsNearBorder_borderProximity))
		{
			float time = 1f - math.saturate((0f - signedDistToBoundary) / m_ph_hapticsNearBorder_borderProximity);
			float num = m_ph_hapticsNearBorder_ampCurve.Evaluate(time);
			float amplitude = math.saturate(m_ph_hapticsNearBorder_baseAmp * num * (GorillaTagger.Instance.tapHapticStrength * 2f));
			_g_ph_hapticsLastImpulseEndTime = Time.unscaledTime + 0.1f;
			InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0u, amplitude, 0.1f);
			InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0u, amplitude, 0.1f);
		}
	}

	private void _Initialize_defaultStencilRefOfSkeletonMat()
	{
		if (_g_ph_defaultStencilRefOfSkeletonMat == -1 && _ph_gorillaGhostBodyMaterialIndex != -1)
		{
			Material[] materialsToChangeTo = VRRig.LocalRig.materialsToChangeTo;
			if (materialsToChangeTo != null && materialsToChangeTo.Length >= 1 && VRRig.LocalRig.materialsToChangeTo[0] != null)
			{
				_g_ph_defaultStencilRefOfSkeletonMat = (int)VRRig.LocalRig.materialsToChangeTo[_ph_gorillaGhostBodyMaterialIndex].GetFloat(ShaderProps._StencilReference);
			}
		}
		else
		{
			_g_ph_defaultStencilRefOfSkeletonMat = 7;
		}
	}

	private void _Initialize_gorillaGhostBodyMaterialIndex()
	{
		_ph_gorillaGhostBodyMaterialIndex = -1;
		Material[] materialsToChangeTo = VRRig.LocalRig.materialsToChangeTo;
		for (int i = 0; i < materialsToChangeTo.Length; i++)
		{
			if (materialsToChangeTo[i].name.StartsWith(m_ph_gorillaGhostBodyMaterial.name))
			{
				_ph_gorillaGhostBodyMaterialIndex = i;
				break;
			}
		}
		if (_ph_gorillaGhostBodyMaterialIndex == -1)
		{
			_ph_gorillaGhostBodyMaterialIndex = 15;
		}
	}

	public override int MyMatIndex(NetPlayer forPlayer)
	{
		EPropHuntGameState ph_gameState = _ph_gameState;
		if ((ph_gameState != EPropHuntGameState.Playing && ph_gameState != EPropHuntGameState.Hiding) || !GameMode.ParticipatingPlayers.Contains(forPlayer) || IsInfected(forPlayer))
		{
			return 0;
		}
		return _ph_gorillaGhostBodyMaterialIndex;
	}

	protected override void InfectionRoundEnd()
	{
		base.InfectionRoundEnd();
		InfectionRoundEndCheck();
	}

	private void InfectionRoundEndCheck()
	{
		_roundIsPlaying = false;
		if (PhotonNetwork.IsMasterClient)
		{
			PH_OnRoundEnd();
		}
	}

	public override bool LocalCanTag(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		if (_ph_gameState == EPropHuntGameState.Playing)
		{
			return base.LocalCanTag(myPlayer, otherPlayer);
		}
		return false;
	}

	public override bool LocalIsTagged(NetPlayer player)
	{
		if (_ph_gameState != EPropHuntGameState.Playing)
		{
			return false;
		}
		return base.LocalIsTagged(player);
	}

	private void _ResetRigAppearance(VRRig rig)
	{
		rig.bodyRenderer.SetSkeletonBodyActive(active: true);
		rig.bodyRenderer.SetGameModeBodyType(GorillaBodyType.Default);
		_SetPlayerBlindfoldVisibility(rig, rig.OwningNetPlayer, shouldEnable: false);
		rig.ChangeMaterialLocal(0);
		rig.SetInvisibleToLocalPlayer(invisible: false);
		if (rig == VRRig.LocalRig)
		{
			CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(hideCosmetics: false);
		}
		for (int i = 0; i < _g_ph_allHandFollowers.Count; i++)
		{
			PropHuntHandFollower propHuntHandFollower = _g_ph_allHandFollowers[i];
			if (propHuntHandFollower.attachedToRig == rig && propHuntHandFollower.hasProp)
			{
				propHuntHandFollower.DestroyProp();
			}
		}
	}

	protected override void InfectionRoundStart()
	{
		base.InfectionRoundStart();
		InfectionRoundStartCheck();
	}

	private void InfectionRoundStartCheck()
	{
		_roundIsPlaying = true;
		if (PhotonNetwork.IsMasterClient)
		{
			_ph_randomSeed = UnityEngine.Random.Range(1, int.MaxValue);
			PH_OnRoundStartRPC(GTTime.TimeAsMilliseconds(), _ph_randomSeed);
		}
	}

	public override void AddInfectedPlayer(NetPlayer infectedPlayer, bool withTagStop = true)
	{
		base.AddInfectedPlayer(infectedPlayer, withTagStop);
		if (infectedPlayer.IsLocal)
		{
			m_ph_playState_taggedSoundBank.Play();
		}
	}

	private void _ResolveXSceneRefs()
	{
		if (!_isListeningForXSceneRefLoadCallbacks)
		{
			m_ph_playBoundary_xSceneRef.AddCallbackOnLoad(_OnXSceneRefLoaded_PlayBoundary);
			m_ph_playBoundary_xSceneRef.AddCallbackOnUnload(_OnXSceneRefUnloaded_PlayBoundary);
			m_ph_playState_startLightning_manager_ref.AddCallbackOnLoad(_OnXSceneRefLoaded_LightningManager);
			m_ph_playState_startLightning_manager_ref.AddCallbackOnUnload(_OnXSceneRefUnloaded_LightningManager);
		}
		_OnXSceneRefLoaded_PlayBoundary();
		if (VRRig.LocalRig.zoneEntity.currentZone == GTZone.bayou)
		{
			_OnXSceneRefLoaded_LightningManager();
		}
	}

	private void _OnXSceneRefLoaded_PlayBoundary()
	{
		if (_ph_playBoundary_isResolved)
		{
			return;
		}
		_ph_playBoundary_isResolved = m_ph_playBoundary_xSceneRef.TryResolve(out _ph_playBoundary) && _ph_playBoundary != null;
		if (_ph_playBoundary_isResolved)
		{
			PlayableBoundaryManager ph_playBoundary = _ph_playBoundary;
			if (ph_playBoundary.tracked == null)
			{
				ph_playBoundary.tracked = new List<PlayableBoundaryTracker>(10);
			}
			_ph_playBoundary.tracked.Clear();
			if (!_ph_playBoundary_initialPosition_isInitialized)
			{
				_ph_playBoundary_initialPosition_isInitialized = true;
				_ph_playBoundary_initialPosition = _ph_playBoundary.transform.position;
				_ph_playBoundary_hasTargetPositionForRound = false;
			}
		}
	}

	private void _OnXSceneRefUnloaded_PlayBoundary()
	{
		_ph_playBoundary_isResolved = false;
		_ph_playBoundary = null;
		_ph_playBoundary_hasTargetPositionForRound = false;
	}

	private void _OnXSceneRefLoaded_LightningManager()
	{
		_ph_playState_startLightning_manager_isResolved = m_ph_playState_startLightning_manager_ref.TryResolve(out _ph_playState_startLightning_manager) && _ph_playState_startLightning_manager != null;
	}

	private void _OnXSceneRefUnloaded_LightningManager()
	{
		_ph_playState_startLightning_manager_isResolved = false;
		_ph_playState_startLightning_manager = null;
	}

	public void PH_OnRoundEnd()
	{
		VRRigCache.Instance.GetActiveRigs(_g_ph_activePlayerRigs);
		for (int i = 0; i < _g_ph_activePlayerRigs.Count; i++)
		{
			_ResetRigAppearance(_g_ph_activePlayerRigs[i]);
		}
		CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(hideCosmetics: false);
		EquipmentInteractor.instance.ForceDropAnyEquipment();
		if (LckSocialCameraManager.Instance != null)
		{
			LckSocialCameraManager.Instance.SetForceHidden(hidden: false);
		}
		_ph_timeRoundStartedMillis = -1000L;
		if (m_ph_soundNearBorder_audioSource != null)
		{
			m_ph_soundNearBorder_audioSource.volume = 0f;
		}
		if (_ph_playBoundary_isResolved && _ph_playBoundary_initialPosition_isInitialized)
		{
			_ph_playBoundary.transform.position = _ph_playBoundary_initialPosition;
		}
		_ph_playBoundary_hasTargetPositionForRound = false;
	}

	public void PH_OnRoundStartRPC(long timeRoundStartedMillis, int seed)
	{
		_ph_isLocalPlayerParticipating = GameMode.ParticipatingPlayers.Contains(VRRig.LocalRig.OwningNetPlayer);
		_ph_timeRoundStartedMillis = timeRoundStartedMillis;
		_ph_randomSeed = seed;
		_PH_OnRoundStart();
	}

	private void _PH_OnRoundStart()
	{
		if (_ph_playBoundary_isResolved)
		{
			int index = new SRand(_ph_randomSeed).NextInt(m_ph_playBoundary_endPointTransforms.Count);
			Transform transform = m_ph_playBoundary_endPointTransforms[index];
			if (transform != null)
			{
				_ph_playBoundary_currentTargetPosition = transform.position;
				_ph_playBoundary_hasTargetPositionForRound = true;
				_ph_playBoundary.transform.position = _ph_playBoundary_initialPosition;
			}
		}
		else if (_ph_playBoundary_isResolved && _ph_playBoundary_initialPosition_isInitialized)
		{
			_ph_playBoundary.transform.position = _ph_playBoundary_initialPosition;
		}
		if (PropHuntPools.IsReady)
		{
			SpawnProps();
		}
		else if (!_isListeningTo_Pools_OnReady)
		{
			PropHuntPools.OnReady = (Action)Delegate.Combine(PropHuntPools.OnReady, new Action(_Pools_OnReady));
		}
		if (_ph_isLocalPlayerParticipating)
		{
			CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(hideCosmetics: false);
			if (LckSocialCameraManager.Instance != null)
			{
				LckSocialCameraManager.Instance.SetForceHidden(hidden: true);
			}
		}
	}

	private void _Pools_OnReady()
	{
		if (PhotonNetwork.IsMasterClient || _ph_isLocalPlayerParticipating)
		{
			SpawnProps();
		}
	}

	public static void RegisterPropZone(PropHuntPropZone propZone)
	{
		_g_ph_allPropZones.Add(propZone);
		if ((object)instance != null && PropHuntPools.IsReady)
		{
			propZone.OnRoundStart();
		}
	}

	public static void UnregisterPropZone(PropHuntPropZone propZone)
	{
		_g_ph_allPropZones.Remove(propZone);
	}

	public static void RegisterPropHandFollower(PropHuntHandFollower hand)
	{
		_g_ph_allHandFollowers.Add(hand);
		if ((object)instance != null)
		{
			hand.OnRoundStart();
		}
	}

	public static void UnregisterPropHandFollower(PropHuntHandFollower hand)
	{
		_g_ph_allHandFollowers.Remove(hand);
	}

	public void SpawnProps()
	{
		if (!PropHuntPools.IsReady)
		{
			if (!_isListeningTo_Pools_OnReady)
			{
				PropHuntPools.OnReady = (Action)Delegate.Combine(PropHuntPools.OnReady, new Action(_Pools_OnReady));
			}
			return;
		}
		foreach (PropHuntPropZone g_ph_allPropZone in _g_ph_allPropZones)
		{
			g_ph_allPropZone.OnRoundStart();
		}
		foreach (PropHuntHandFollower g_ph_allHandFollower in _g_ph_allHandFollowers)
		{
			if (GameMode.ParticipatingPlayers.Contains(g_ph_allHandFollower.attachedToRig.OwningNetPlayer))
			{
				g_ph_allHandFollower.OnRoundStart();
			}
		}
	}

	public string GetCosmeticId(uint randomUInt)
	{
		if (PropHuntPools.AllPropCosmeticIds == null)
		{
			return m_ph_fallbackPropCosmeticSO.info.playFabID;
		}
		return PropHuntPools.AllPropCosmeticIds[randomUInt % PropHuntPools.AllPropCosmeticIds.Length];
	}

	public GTAssetRef<GameObject> GetPropRef_NoPool(uint randomUInt, out CosmeticSO out_debugCosmeticSO)
	{
		if (AllPropIDs_NoPool == null)
		{
			out_debugCosmeticSO = m_ph_fallbackPropCosmeticSO;
			return m_ph_fallbackPropCosmeticSO.info.wardrobeParts[0].prefabAssetRef;
		}
		string cosmeticID = AllPropIDs_NoPool[randomUInt % AllPropIDs_NoPool.Length];
		return GetPropRefByCosmeticID_NoPool(cosmeticID, out out_debugCosmeticSO);
	}

	public GTAssetRef<GameObject> GetPropRefByCosmeticID_NoPool(string cosmeticID, out CosmeticSO out_debugCosmeticSO)
	{
		CosmeticSO cosmeticSO = m_ph_allCosmetics.SearchForCosmeticSO(cosmeticID);
		if (cosmeticSO == null)
		{
			GTDev.LogError("ERROR!!!  GorillaPropHuntGameManager.GetPropRefByCosmeticID_NoPool: Got cosmetic id from title data, but could not find \"" + cosmeticID + "\".");
			out_debugCosmeticSO = m_ph_fallbackPropCosmeticSO;
			return m_ph_fallbackPropCosmeticSO.info.wardrobeParts[0].prefabAssetRef;
		}
		if (cosmeticSO.info.wardrobeParts.Length == 0)
		{
			Debug.LogError("Invalid prop " + cosmeticID + " " + cosmeticSO.info.displayName + " has no wardrobeParts");
			out_debugCosmeticSO = m_ph_fallbackPropCosmeticSO;
			return m_ph_fallbackPropCosmeticSO.info.wardrobeParts[0].prefabAssetRef;
		}
		out_debugCosmeticSO = cosmeticSO;
		return cosmeticSO.info.wardrobeParts[0].prefabAssetRef;
	}

	private void _SetPlayerBlindfoldVisibility(NetPlayer netPlayer, bool shouldEnable)
	{
		VRRig vRRig = FindPlayerVRRig(netPlayer);
		if (!(vRRig == null) || !netPlayer.InRoom)
		{
			_SetPlayerBlindfoldVisibility(vRRig, netPlayer, shouldEnable);
		}
	}

	private void _SetPlayerBlindfoldVisibility(VRRig vrRig, NetPlayer netPlayer, bool shouldEnable)
	{
		if (netPlayer == VRRig.LocalRig.OwningNetPlayer)
		{
			if (!_ph_blindfold_forCamera_isInitialized)
			{
				_InitializeBlindfoldForCamera();
			}
			if (_ph_blindfold_forCamera_isInitialized)
			{
				_ph_blindfold_forCamera_1p.SetActive(shouldEnable);
				_ph_blindfold_forCamera_3p.SetActive(shouldEnable);
			}
			return;
		}
		if (!_ph_vrRig_to_blindfolds.TryGetValue(vrRig.GetInstanceID(), out var value))
		{
			if (!GTHardCodedBones.TryGetBoneXforms(vrRig, out var outBoneXforms, out var _) || !GTHardCodedBones.TryGetBoneXform(outBoneXforms, GTHardCodedBones.EBone.head, out var boneXform) || m_ph_blindfold_forAvatarPrefab == null)
			{
				return;
			}
			value = UnityEngine.Object.Instantiate(m_ph_blindfold_forAvatarPrefab, boneXform);
			_ph_vrRig_to_blindfolds[vrRig.GetInstanceID()] = value;
		}
		value.SetActive(shouldEnable);
	}

	private void _InitializeBlindfoldForCamera()
	{
		if (GorillaTagger.Instance == null)
		{
			return;
		}
		GameObject mainCamera = GorillaTagger.Instance.mainCamera;
		if (!(mainCamera == null) && !(m_ph_blindfold_forCameraPrefab == null))
		{
			_ph_blindfold_forCamera_1p = UnityEngine.Object.Instantiate(m_ph_blindfold_forCameraPrefab, mainCamera.transform);
			Camera camera = null;
			if (GorillaTagger.Instance.thirdPersonCamera != null)
			{
				camera = GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<Camera>(includeInactive: true);
			}
			if (!(camera == null))
			{
				_ph_blindfold_forCamera_3p = UnityEngine.Object.Instantiate(m_ph_blindfold_forCameraPrefab, camera.transform);
				_ph_blindfold_forCamera_isInitialized = _ph_blindfold_forCamera_1p != null;
			}
		}
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		base.OnSerializeRead(stream, info);
		_ph_randomSeed = (int)stream.ReceiveNext();
		long ph_timeRoundStartedMillis = _ph_timeRoundStartedMillis;
		_ph_timeRoundStartedMillis = (long)stream.ReceiveNext();
		if (ph_timeRoundStartedMillis != _ph_timeRoundStartedMillis)
		{
			if (_ph_timeRoundStartedMillis > 0)
			{
				_PH_OnRoundStart();
			}
			else
			{
				PH_OnRoundEnd();
			}
		}
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		base.OnSerializeWrite(stream, info);
		stream.SendNext(_ph_randomSeed);
		stream.SendNext(_ph_timeRoundStartedMillis);
	}
}
