using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

public class GRPlayer : MonoBehaviourTick
{
	public enum GRPlayerState
	{
		Alive,
		Ghost,
		Shielded
	}

	public enum GRPlayerShieldFlags
	{
		Light = 1,
		Stealth = 2,
		Heal = 4
	}

	public enum SynchronizedSessionStat
	{
		CoresDeposited,
		EarnedCredits,
		SpentCredits,
		DistanceTraveled,
		Deaths,
		Kills,
		Assists,
		TimeChaosExposure,
		Count
	}

	[Serializable]
	private struct DamageOverlayValues
	{
		public Color tint;

		public float effectDuration;

		public AnimationCurve effectCurve;
	}

	public enum ShuttleState
	{
		Idle,
		Moving,
		WaitForLeaveRoom,
		JoinRoom,
		WaitForLeadPlayer,
		Teleport,
		TeleportToMyShuttleSafety,
		PostTeleport
	}

	public class ShuttleData
	{
		public string ownerUserId;

		public int currShuttleId;

		public int targetShuttleId;

		public int targetLevel;

		public ShuttleState state;

		public double stateStartTime;
	}

	[Serializable]
	public struct ProgressionData
	{
		public int points;

		public int redeemedPoints;
	}

	[Serializable]
	public struct ProgressionLevels
	{
		public int tierId;

		public string tierName;

		public int grades;

		public int pointsPerGrade;
	}

	public const int MAX_CURRENCY = 500;

	public GamePlayer gamePlayer;

	private GRPlayerState state;

	private int shiftCreditCache;

	public int startingShiftCreditCache;

	public int playerJuice;

	public double shiftJoinTime;

	public bool isEmployee;

	public AudioSource audioSource;

	[Header("Hit / Revive Effects")]
	public ParticleSystem playerTurnedGhostEffect;

	public SoundBankPlayer playerTurnedGhostSoundBank;

	public ParticleSystem playerRevivedEffect;

	public AudioClip playerRevivedSound;

	public float playerRevivedVolume = 1f;

	public AudioSource playerDamageAudioSource;

	public Transform bodyCenter;

	public ParticleSystem playerDamageEffect;

	public float playerDamageVolume = 1f;

	public AudioClip playerDamageSound;

	public float playerDamageOffsetDist = 0.25f;

	[ColorUsage(true, true)]
	[SerializeField]
	private Color deathTintColor;

	[ColorUsage(true, true)]
	[SerializeField]
	private Color deathAmbientLightColor;

	public GameLight shieldGameLight;

	[Header("Attach")]
	public Transform attachEnemy;

	[Header("Shield")]
	public Transform shieldHeadVisual;

	public Transform shieldBodyVisual;

	public AudioClip shieldActivatedSound;

	public float shieldActivatedVolume = 0.5f;

	public ParticleSystem shieldDamagedEffect;

	public AudioClip shieldDamagedSound;

	public float shieldDamagedVolume = 0.5f;

	public ParticleSystem shieldDestroyedEffect;

	public AudioClip shieldDestroyedSound;

	public float shieldDestroyedVolume = 0.5f;

	public float shieldStealthModeDuration = 20f;

	private double shieldStealthModeEndTime;

	public Color shieldColorNormal = new Color(36f / 85f, 13f / 51f, 1f, 0.45490196f);

	public Color shieldColorLight = new Color(1f, 1f, 1f, 0.5f);

	public Color shieldColorStealth = new Color(1f, 0.2f, 0f, 0.5f);

	public Color shieldColorHeal = new Color(0f, 1f, 1f, 0.5f);

	public int xRayVisionRefCount;

	[Header("Badge")]
	public Transform badgeBodyAnchor;

	[SerializeField]
	private Transform badgeBodyStringAttach;

	[NonSerialized]
	public double lastLeftWithBadgeAttachedTime;

	[Header("Health")]
	[SerializeField]
	private int maxHp = 1;

	[SerializeField]
	private int maxShieldHp = 1;

	public string mothershipId;

	private int hp;

	private int shieldHp;

	private int shieldFlags;

	private bool inStealthMode;

	[Header("Damage Vignette")]
	[SerializeField]
	[Tooltip("First entry is 1 hp, second entry is 2 hp, etc.")]
	private List<DamageOverlayValues> damageOverlayValues = new List<DamageOverlayValues>();

	[SerializeField]
	private int damageOverlayMaxHp = 1;

	[HideInInspector]
	public GRBadge badge;

	public CallLimiter requestCollectItemLimiter;

	public CallLimiter requestChargeToolLimiter;

	public CallLimiter requestDepositCurrencyLimiter;

	public CallLimiter requestShiftStartLimiter;

	public CallLimiter requestToolPurchaseStationLimiter;

	public CallLimiter applyEnemyHitLimiter;

	public CallLimiter reportLocalHitLimiter;

	public CallLimiter reportBreakableBrokenLimiter;

	public CallLimiter playerStateChangeLimiter;

	public CallLimiter promotionBotLimiter;

	public CallLimiter progressionBroadcastLimiter;

	public CallLimiter scoreboardPageLimiter;

	public CallLimiter fireShieldLimiter;

	private VRRig vrRig;

	private List<VRRig> vrRigs = new List<VRRig>();

	private string gameId;

	public int coresCollectedByPlayer;

	public int coresCollectedByGroup;

	public int coresSpentByPlayer;

	public int coresSpentByGroup;

	public int gatesUnlocked;

	public int deaths;

	public bool caughtByAnomaly;

	public List<string> itemsPurchased;

	public List<string> levelsUnlocked;

	public float timeIntoShiftAtJoin;

	public bool wasPlayerInAtShiftStart;

	public int sentientCoresCollected;

	public int maxNumberOfPlayersInShift;

	public int revives;

	public float[] synchronizedSessionStats = new float[8];

	private HashSet<GameEntityId> itemsHeldThisShift = new HashSet<GameEntityId>();

	private Dictionary<string, int> itemTypesHeldThisShift = new Dictionary<string, int>();

	public int totalCoresCollectedByPlayer;

	public int totalCoresCollectedByGroup;

	public int totalCoresSpentByPlayer;

	public int totalCoresSpentByGroup;

	public int totalGatesUnlocked;

	public int totalDeaths;

	public List<string> totalItemsPurchased;

	public float timeIntoGameAtJoin;

	public bool wasPlayerInAtGameStart;

	public int maxNumberOfPlayersIngame;

	public int totalRevives;

	public int numShiftsPlayed;

	public float gameStartTime;

	public bool isFirstShift = true;

	private HashSet<GameEntityId> totalItemsHeldThisShift = new HashSet<GameEntityId>();

	private Dictionary<string, int> totalItemTypesHeldThisShift = new Dictionary<string, int>();

	private GRPlayerDamageEffects damageEffects;

	private MaterialPropertyBlock lowHealthVisualPropertyBlock;

	private int lowHealthTintPropertyId;

	private int currentHealthVisualValue;

	private Coroutine lowHeathVisualCoroutine;

	public AudioClip playerFrozenSound;

	public ShuttleData shuttleData;

	private ProgressionData currentProgression;

	private float shiftPlayTime;

	private int lastShiftCut;

	private GhostReactorSoak soak;

	private static List<VRRig> tempRigs = new List<VRRig>(32);

	private float freezeDuration;

	private Vector3 lastPlayerPosition = Vector3.zero;

	private bool saveEquipmentInProgress;

	private bool hasPulledEquipment;

	public int dropPodLevel;

	public int dropPodChasisLevel;

	public GRPlayerState State => state;

	public int Juice => playerJuice;

	public int ShiftCreditCapIncreases { get; set; }

	public int ShiftCreditCapIncreasesMax { get; set; }

	public int ShiftCredits => shiftCreditCache;

	public int MaxHp => maxHp;

	public int MaxShieldHp => maxShieldHp;

	public int Hp => hp;

	public int ShieldHp => shieldHp;

	public int ShieldFlags => shieldFlags;

	public bool InStealthMode => inStealthMode;

	public VRRig MyRig => vrRig;

	public float ShiftPlayTime
	{
		get
		{
			return shiftPlayTime;
		}
		set
		{
			shiftPlayTime = value;
		}
	}

	public int LastShiftCut
	{
		get
		{
			return lastShiftCut;
		}
		set
		{
			lastShiftCut = value;
		}
	}

	public ProgressionData CurrentProgression
	{
		get
		{
			return currentProgression;
		}
		set
		{
			currentProgression = value;
		}
	}

	public bool HasXRayVision()
	{
		return xRayVisionRefCount > 0;
	}

	private void Awake()
	{
		vrRig = GetComponent<VRRig>();
		lowHealthVisualPropertyBlock = new MaterialPropertyBlock();
		damageEffects = GTPlayer.Instance.mainCamera.GetComponent<GRPlayerDamageEffects>();
		lowHealthTintPropertyId = Shader.PropertyToID("_TintColor");
		isEmployee = false;
		SetHp(maxHp);
		SetShieldHp(0);
		state = GRPlayerState.Alive;
		RefreshDamageVignetteVisual();
		shieldHeadVisual.gameObject.SetActive(value: false);
		shieldBodyVisual.gameObject.SetActive(value: false);
		shieldGameLight = shieldBodyVisual.gameObject.GetComponentInChildren<GameLight>(includeInactive: true);
		requestCollectItemLimiter = new CallLimiter(25, 1f);
		requestChargeToolLimiter = new CallLimiter(25, 1f);
		requestDepositCurrencyLimiter = new CallLimiter(25, 1f);
		requestShiftStartLimiter = new CallLimiter(25, 1f);
		requestToolPurchaseStationLimiter = new CallLimiter(25, 1f);
		applyEnemyHitLimiter = new CallLimiter(25, 1f);
		reportLocalHitLimiter = new CallLimiter(25, 1f);
		reportBreakableBrokenLimiter = new CallLimiter(25, 1f);
		playerStateChangeLimiter = new CallLimiter(25, 1f);
		promotionBotLimiter = new CallLimiter(25, 1f);
		progressionBroadcastLimiter = new CallLimiter(25, 1f);
		scoreboardPageLimiter = new CallLimiter(25, 1f);
		fireShieldLimiter = new CallLimiter(25, 1f);
		shuttleData = new ShuttleData();
		lastLeftWithBadgeAttachedTime = -10000.0;
	}

	private void Start()
	{
		if (gamePlayer != null && gamePlayer.IsLocal())
		{
			LoadMyProgression();
			ProgressionManager.Instance.OnGetShiftCredit += OnShiftCreditChanged;
			ProgressionManager.Instance.OnGetShiftCreditCapData += OnShiftCreditCapChanged;
			soak = new GhostReactorSoak();
			soak.Setup(this);
		}
		else
		{
			currentProgression = new ProgressionData
			{
				points = 0,
				redeemedPoints = 0
			};
		}
		if (ProgressionManager.Instance != null)
		{
			ProgressionManager.Instance.OnGetShiftCredit += OnShiftCreditChanged;
			ProgressionManager.Instance.OnGetShiftCreditCapData += OnShiftCreditCapChanged;
		}
	}

	private new void OnDisable()
	{
		Reset();
	}

	public void Reset()
	{
		SetHp(maxHp);
		SetShieldHp(0);
		state = GRPlayerState.Alive;
		RefreshDamageVignetteVisual();
		RefreshPlayerVisuals();
		for (int i = 0; i < 8; i++)
		{
			synchronizedSessionStats[i] = 0f;
		}
	}

	private void SetHp(int newHp)
	{
		hp = Mathf.Max(newHp, 0);
	}

	private void SetShieldHp(int newShieldHp)
	{
		shieldHp = Mathf.Max(newShieldHp, 0);
	}

	public void OnShiftCreditCapChanged(string targetMothershipId, int newCap, int newCapMax)
	{
		if (mothershipId != null && targetMothershipId == mothershipId)
		{
			if (gamePlayer.IsLocal() && (newCap != ShiftCreditCapIncreases || newCapMax != ShiftCreditCapIncreasesMax) && GhostReactor.instance != null)
			{
				GhostReactor.instance.grManager.RefreshShiftCredit();
			}
			ShiftCreditCapIncreases = newCap;
			ShiftCreditCapIncreasesMax = newCapMax;
		}
	}

	public void OnShiftCreditChanged(string targetMothershipId, int newShiftCredits)
	{
		if (mothershipId != null && targetMothershipId == mothershipId)
		{
			int num = shiftCreditCache;
			shiftCreditCache = newShiftCredits;
			if (GhostReactor.instance != null && gamePlayer.IsLocal() && num != newShiftCredits && GhostReactor.instance != null)
			{
				if (GhostReactor.instance.promotionBot != null)
				{
					GhostReactor.instance.promotionBot.Refresh();
				}
				if (GhostReactor.instance.grManager != null)
				{
					GhostReactor.instance.grManager.RefreshShiftCredit();
				}
			}
		}
		if (GhostReactor.instance != null)
		{
			GhostReactor.instance.RefreshScoreboards();
		}
	}

	public void OnShiftCreditCapData(string targetMothershipId, int shiftCreditCapNumberOfIncreases, int shiftCreditMaxNumberOfIncreases)
	{
		if (mothershipId != null)
		{
			_ = targetMothershipId == mothershipId;
		}
	}

	public void SubtractShiftCredit(int shiftCreditDelta)
	{
		if (gamePlayer.IsLocal())
		{
			ProgressionManager.Instance.SubtractShiftCredit(shiftCreditDelta);
		}
	}

	public void OnPlayerHit(Vector3 hitPosition, Vector3 hitImpulse, GhostReactorManager manager, GameEntityId hitByEntityId)
	{
		GameEntity gameEntity = manager.gameEntityManager.GetGameEntity(hitByEntityId);
		int num = 1;
		if (gamePlayer.IsLocal())
		{
			GTPlayer instance = GTPlayer.Instance;
			float magnitude = hitImpulse.magnitude;
			if (magnitude > 0f)
			{
				instance.ApplyKnockback(hitImpulse / magnitude, magnitude, forceOffTheGround: true);
			}
		}
		if (State != GRPlayerState.Alive)
		{
			return;
		}
		if (shieldHp > 0)
		{
			if (gameEntity != null)
			{
				GRAttributes component = gameEntity.GetComponent<GRAttributes>();
				if (component != null)
				{
					num = component.CalculateFinalValueForAttribute(GRAttributeType.PlayerShieldDamage);
				}
			}
			SetShieldHp(shieldHp - num);
			if (shieldHp > 0)
			{
				if (shieldDamagedSound != null)
				{
					audioSource.PlayOneShot(shieldDamagedSound, shieldDamagedVolume);
				}
				shieldDamagedEffect.Play();
			}
			else
			{
				if (shieldDestroyedSound != null)
				{
					audioSource.PlayOneShot(shieldDestroyedSound, shieldDestroyedVolume);
				}
				shieldDestroyedEffect.Play();
			}
			RefreshPlayerVisuals();
			return;
		}
		if (gameEntity != null)
		{
			GRAttributes component2 = gameEntity.GetComponent<GRAttributes>();
			if (component2 != null)
			{
				num = component2.CalculateFinalValueForAttribute(GRAttributeType.PlayerDamage);
			}
		}
		Debug.Log($"GRPlayer OnPlayerHit, hit by: {hitByEntityId.index} damage: {num}, state: {state}, hp: {hp}, shield hp: {shieldHp}");
		PlayHitFx(hitPosition);
		SetHp(hp - num);
		RefreshDamageVignetteVisual();
		if (hp <= 0)
		{
			ChangePlayerState(GRPlayerState.Ghost, manager);
		}
	}

	public void OnPlayerRevive(GhostReactorManager manager)
	{
		SetHp(maxHp);
		RefreshDamageVignetteVisual();
		ChangePlayerState(GRPlayerState.Alive, manager);
	}

	public void ChangePlayerState(GRPlayerState newState, GhostReactorManager manager)
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			newState = GRPlayerState.Alive;
		}
		if (state == newState)
		{
			return;
		}
		state = newState;
		switch (state)
		{
		case GRPlayerState.Ghost:
			SetHp(0);
			SetShieldHp(0);
			RefreshDamageVignetteVisual();
			if (playerTurnedGhostEffect != null)
			{
				playerTurnedGhostEffect.Play();
			}
			playerTurnedGhostSoundBank.Play();
			manager.ReportPlayerDeath(this);
			IncrementDeaths(1);
			break;
		case GRPlayerState.Alive:
			SetHp(maxHp);
			RefreshDamageVignetteVisual();
			IncrementRevives(1);
			if (playerRevivedEffect != null)
			{
				playerRevivedEffect.Play();
			}
			if (audioSource != null && playerRevivedSound != null)
			{
				audioSource.PlayOneShot(playerRevivedSound, playerRevivedVolume);
			}
			break;
		}
		RefreshPlayerVisuals();
		if (vrRig.isLocal)
		{
			vrRigs.Clear();
			VRRigCache.Instance.GetAllUsedRigs(vrRigs);
			for (int i = 0; i < vrRigs.Count; i++)
			{
				vrRigs[i].GetComponent<GRPlayer>().RefreshPlayerVisuals();
			}
		}
	}

	public void RefreshPlayerVisuals()
	{
		RefreshDamageVignetteVisual();
		switch (state)
		{
		case GRPlayerState.Alive:
			gamePlayer.DisableGrabbing(disable: false);
			if (badge != null)
			{
				badge.UnHide();
			}
			vrRig.ChangeMaterialLocal(0);
			vrRig.bodyRenderer.SetGameModeBodyType(GorillaBodyType.Default);
			vrRig.SetInvisibleToLocalPlayer(invisible: false);
			if (vrRig.isLocal)
			{
				CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(hideCosmetics: false);
				GameLightingManager.instance.SetDesaturateAndTintEnabled(enable: false, Color.black);
				Color ambientLightDynamic = Color.black;
				GhostReactor instance = GhostReactor.instance;
				if (instance != null && instance.zone != GTZone.customMaps)
				{
					ambientLightDynamic = instance.GetCurrLevelGenConfig().ambientLight;
				}
				GameLightingManager.instance.SetAmbientLightDynamic(ambientLightDynamic);
			}
			if (shieldHp > 0)
			{
				shieldHeadVisual.gameObject.SetActive(value: true);
				shieldBodyVisual.gameObject.SetActive(value: true);
				Color value = shieldColorNormal;
				if ((shieldFlags & 1) != 0)
				{
					value = shieldColorLight;
				}
				else if ((shieldFlags & 2) != 0)
				{
					value = shieldColorStealth;
				}
				else if ((shieldFlags & 4) != 0)
				{
					value = shieldColorHeal;
				}
				Renderer component = shieldBodyVisual.GetComponent<Renderer>();
				if (component != null)
				{
					component.material.SetColor("_BaseColor", value);
				}
				Renderer component2 = shieldHeadVisual.GetComponent<Renderer>();
				if (component2 != null)
				{
					component2.material.SetColor("_BaseColor", value);
				}
			}
			else
			{
				shieldHeadVisual.gameObject.SetActive(value: false);
				shieldBodyVisual.gameObject.SetActive(value: false);
			}
			shieldGameLight.gameObject.SetActive((shieldFlags & 1) != 0);
			break;
		case GRPlayerState.Ghost:
			if (vrRig.isLocal)
			{
				gamePlayer.RequestDropAllSnapped();
			}
			gamePlayer.DisableGrabbing(disable: true);
			shieldHeadVisual.gameObject.SetActive(value: false);
			shieldBodyVisual.gameObject.SetActive(value: false);
			shieldGameLight.gameObject.SetActive(value: false);
			if (badge != null)
			{
				badge.Hide();
			}
			if (vrRig.isLocal)
			{
				GamePlayerLocal.instance.OnUpdateInteract();
				vrRig.bodyRenderer.SetGameModeBodyType(GorillaBodyType.Skeleton);
				vrRig.ChangeMaterialLocal(13);
				vrRig.SetInvisibleToLocalPlayer(invisible: false);
				CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(hideCosmetics: true);
				GameLightingManager.instance.SetDesaturateAndTintEnabled(enable: true, deathTintColor);
				GameLightingManager.instance.SetAmbientLightDynamic(deathAmbientLightColor);
			}
			else if (VRRigCache.Instance.localRig.GetComponent<GRPlayer>().State == GRPlayerState.Ghost)
			{
				vrRig.ChangeMaterialLocal(13);
				vrRig.bodyRenderer.SetGameModeBodyType(GorillaBodyType.Skeleton);
				vrRig.SetInvisibleToLocalPlayer(invisible: false);
			}
			else
			{
				vrRig.bodyRenderer.SetGameModeBodyType(GorillaBodyType.Invisible);
				vrRig.SetInvisibleToLocalPlayer(invisible: true);
			}
			break;
		}
	}

	public static GRPlayer Get(int actorNumber)
	{
		if (!GamePlayer.TryGetGamePlayer(actorNumber, out var out_gamePlayer))
		{
			return null;
		}
		return out_gamePlayer.GetComponent<GRPlayer>();
	}

	public static GRPlayer Get(NetPlayer player)
	{
		if (player == null)
		{
			return null;
		}
		return Get(player.ActorNumber);
	}

	public static GRPlayer Get(VRRig vrRig)
	{
		if (!(vrRig != null))
		{
			return null;
		}
		return vrRig.GetComponent<GRPlayer>();
	}

	public static GRPlayer GetLocal()
	{
		return Get(VRRig.LocalRig);
	}

	public void AttachBadge(GRBadge grBadge)
	{
		badge = grBadge;
		badge.transform.SetParent(badgeBodyAnchor);
		badge.GetComponent<Rigidbody>().isKinematic = true;
		badge.StartRetracting();
	}

	public bool CanActivateShield(int shieldHitPoints)
	{
		if (state == GRPlayerState.Alive)
		{
			return shieldHitPoints > 0;
		}
		return false;
	}

	public bool TryActivateShield(int shieldHitpoints, int shieldFlags)
	{
		if (state == GRPlayerState.Alive)
		{
			if (shieldHp <= 0 && shieldActivatedSound != null)
			{
				audioSource.PlayOneShot(shieldActivatedSound, shieldActivatedVolume);
			}
			SetShieldHp(Mathf.Min(shieldHitpoints, maxShieldHp));
			this.shieldFlags = shieldFlags;
			inStealthMode = (shieldFlags & 2) != 0;
			if (inStealthMode)
			{
				if (damageEffects.stealthModeVisualRenderer != null)
				{
					damageEffects.stealthModeVisualRenderer.gameObject.SetActive(value: true);
				}
				shieldStealthModeEndTime = Time.timeAsDouble + (double)shieldStealthModeDuration;
			}
			if ((shieldFlags & 4) != 0)
			{
				SetHp(maxHp);
			}
			RefreshPlayerVisuals();
			return true;
		}
		return false;
	}

	public void ClearStealthMode()
	{
		inStealthMode = false;
		if (damageEffects.stealthModeVisualRenderer != null)
		{
			damageEffects.stealthModeVisualRenderer.gameObject.SetActive(value: false);
		}
	}

	public void SerializeNetworkState(BinaryWriter writer, NetPlayer player)
	{
		writer.Write((byte)state);
		writer.Write(hp);
		writer.Write(shieldHp);
		writer.Write(shiftJoinTime);
		writer.Write((byte)(isEmployee ? 1u : 0u));
		writer.Write(CurrentProgression.points);
		writer.Write(CurrentProgression.redeemedPoints);
		writer.Write(dropPodLevel);
		writer.Write(dropPodChasisLevel);
		for (int i = 0; i < 8; i++)
		{
			writer.Write(synchronizedSessionStats[i]);
		}
	}

	public static void DeserializeNetworkStateAndBurn(BinaryReader reader, GRPlayer player, GhostReactorManager grManager)
	{
		GRPlayerState newState = (GRPlayerState)reader.ReadByte();
		int num = reader.ReadInt32();
		int num2 = reader.ReadInt32();
		double num3 = reader.ReadDouble();
		bool flag = reader.ReadByte() != 0;
		int points = reader.ReadInt32();
		int redeemedPoints = reader.ReadInt32();
		int num4 = reader.ReadInt32();
		int num5 = reader.ReadInt32();
		for (int i = 0; i < 8; i++)
		{
			player.synchronizedSessionStats[i] = reader.ReadSingle();
		}
		if (player != null)
		{
			player.SetHp(num);
			player.SetShieldHp(num2);
			player.isEmployee = flag;
			player.ChangePlayerState(newState, grManager);
			player.RefreshPlayerVisuals();
			if (!player.gamePlayer.IsLocal())
			{
				player.SetProgressionData(points, redeemedPoints);
				player.dropPodLevel = num4;
				player.dropPodChasisLevel = num5;
			}
			if (double.IsNaN(num3) || double.IsInfinity(num3))
			{
				player.shiftJoinTime = PhotonNetwork.Time;
			}
			else
			{
				player.shiftJoinTime = Math.Min(num3, PhotonNetwork.Time);
			}
		}
		if (grManager != null)
		{
			grManager.SendMothershipId();
		}
	}

	public void PlayHitFx(Vector3 attackLocation)
	{
		if (playerDamageAudioSource != null)
		{
			playerDamageAudioSource.PlayOneShot(playerDamageSound, playerDamageVolume);
		}
		if (bodyCenter != null)
		{
			Vector3 vector = attackLocation - bodyCenter.position;
			vector.y = 0f;
			Vector3 vector2 = vector.normalized * playerDamageOffsetDist;
			if (playerDamageEffect != null)
			{
				playerDamageEffect.transform.position = bodyCenter.position + vector2;
				playerDamageEffect.Play();
			}
			if (vrRig.isLocal)
			{
				Vector3 normalized = Vector3.ProjectOnPlane(GTPlayer.Instance.mainCamera.transform.forward, Vector3.up).normalized;
				vector = Vector3.ProjectOnPlane(vector, Vector3.up).normalized;
				float num = Vector3.SignedAngle(normalized, vector, Vector3.up);
				damageEffects.radialDamageEffect.transform.localRotation = Quaternion.Euler(0f, 0f, 0f - num);
				damageEffects.radialDamageEffect.Play();
			}
		}
		if (gamePlayer == GamePlayerLocal.instance.gamePlayer)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength, 0.5f);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength, 0.5f);
		}
	}

	public void SendGameStartedTelemetry(float timeIntoShift, bool wasPlayerInAtStart, int currentFloor)
	{
		vrRigs.Clear();
		VRRigCache.Instance.GetAllUsedRigs(vrRigs);
		string titleNameFromLevel = GhostReactorProgression.GetTitleNameFromLevel(GhostReactorProgression.GetTitleLevel(CurrentProgression.redeemedPoints));
		GorillaTelemetry.GhostReactorShiftStart(gameId, ShiftCredits, timeIntoShift, wasPlayerInAtStart, vrRigs.Count + 1, currentFloor, titleNameFromLevel);
		wasPlayerInAtShiftStart = wasPlayerInAtStart;
		ResetGameTelemetryTracking();
	}

	public void SendGameEndedTelemetry(bool isShiftActuallyEnding, ZoneClearReason zoneClearReason)
	{
		vrRigs.Clear();
		VRRigCache.Instance.GetAllUsedRigs(vrRigs);
		GorillaTelemetry.GhostReactorGameEnd(gameId, ShiftCredits, totalCoresCollectedByPlayer, totalCoresCollectedByGroup, totalCoresSpentByPlayer, totalCoresSpentByGroup, totalGatesUnlocked, totalDeaths, totalItemsPurchased, lastShiftCut, isShiftActuallyEnding, timeIntoShiftAtJoin, (float)(PhotonNetwork.Time - (double)gameStartTime), wasPlayerInAtShiftStart, zoneClearReason, maxNumberOfPlayersInShift, vrRigs.Count + 1, totalItemTypesHeldThisShift, totalRevives, numShiftsPlayed);
		isFirstShift = true;
	}

	public void SendFloorStartedTelemetry(float timeIntoShift, bool wasPlayerInAtStart, int currentFloor, string floorPreset, string floorModifier)
	{
		vrRigs.Clear();
		VRRigCache.Instance.GetAllUsedRigs(vrRigs);
		string titleNameFromLevel = GhostReactorProgression.GetTitleNameFromLevel(GhostReactorProgression.GetTitleLevel(CurrentProgression.redeemedPoints));
		GorillaTelemetry.GhostReactorFloorStart(gameId, ShiftCredits, timeIntoShift, wasPlayerInAtStart, vrRigs.Count + 1, titleNameFromLevel, currentFloor, floorPreset, floorModifier);
		wasPlayerInAtShiftStart = wasPlayerInAtStart;
	}

	public void SendFloorEndedTelemetry(bool isShiftActuallyEnding, float shiftStartTime, ZoneClearReason zoneClearReason, int currentFloor, string floorPreset, string floorModifier, bool objectivesCompleted, string section, int xpGained)
	{
		vrRigs.Clear();
		VRRigCache.Instance.GetAllUsedRigs(vrRigs);
		GorillaTelemetry.GhostReactorFloorComplete(gameId, ShiftCredits, coresCollectedByPlayer, coresCollectedByGroup, coresSpentByPlayer, coresSpentByGroup, gatesUnlocked, deaths, itemsPurchased, lastShiftCut, isShiftActuallyEnding, timeIntoShiftAtJoin, (float)(PhotonNetwork.Time - (double)(timeIntoShiftAtJoin + shiftStartTime)), wasPlayerInAtShiftStart, zoneClearReason, maxNumberOfPlayersInShift, vrRigs.Count + 1, itemTypesHeldThisShift, revives, currentFloor, floorPreset, floorModifier, sentientCoresCollected, objectivesCompleted, section, xpGained);
	}

	public void SendToolPurchasedTelemetry(string toolName, int toolLevel, int coresSpent, int shinyRocksSpent)
	{
		int floor = -1;
		string preset = "";
		GhostReactor instance = GhostReactor.instance;
		if (instance != null && instance.zone != GTZone.customMaps)
		{
			floor = instance.GetDepthLevel();
			preset = instance.GetCurrLevelGenConfig().name;
		}
		GorillaTelemetry.GhostReactorToolPurchased(gameId, toolName, toolLevel, coresSpent, shinyRocksSpent, floor, preset);
	}

	public void SendRankUpTelemetry(string newRank)
	{
		int floor = -1;
		string preset = "";
		GhostReactor instance = GhostReactor.instance;
		if (instance != null && instance.zone != GTZone.customMaps)
		{
			floor = instance.GetDepthLevel();
			preset = instance.GetCurrLevelGenConfig().name;
		}
		GorillaTelemetry.GhostReactorRankUp(gameId, newRank, floor, preset);
	}

	public void SendToolUpgradeTelemetry(string upgradeType, string toolName, int newLevel, int juiceSpent, int griftSpent, int coresSpent)
	{
		int floor = -1;
		string preset = "";
		GhostReactor instance = GhostReactor.instance;
		if (instance != null && instance.zone != GTZone.customMaps)
		{
			floor = instance.GetDepthLevel();
			preset = instance.GetCurrLevelGenConfig().name;
		}
		GorillaTelemetry.GhostReactorToolUpgrade(gameId, upgradeType, toolName, newLevel, juiceSpent, griftSpent, coresSpent, floor, preset);
	}

	public void SendSeedDepositedTelemetry(string unlockTime, int seedsInQueue)
	{
		int floor = -1;
		string preset = "";
		GhostReactor instance = GhostReactor.instance;
		if (instance != null && instance.zone != GTZone.customMaps)
		{
			floor = instance.GetDepthLevel();
			preset = instance.GetCurrLevelGenConfig().name;
		}
		GorillaTelemetry.GhostReactorChaosSeedStart(gameId, unlockTime, seedsInQueue, floor, preset);
	}

	public void SendJuiceCollectedTelemetry(int juiceCollected, int coresProcessedByOverdrive)
	{
		GorillaTelemetry.GhostReactorChaosJuiceCollected(gameId, juiceCollected, coresProcessedByOverdrive);
	}

	public void SendOverdrivePurchasedTelemetry(int shinyRocksUsed, int seedsInQueue)
	{
		int floor = -1;
		string preset = "";
		GhostReactor instance = GhostReactor.instance;
		if (instance != null && instance.zone != GTZone.customMaps)
		{
			floor = instance.GetDepthLevel();
			preset = instance.GetCurrLevelGenConfig().name;
		}
		GorillaTelemetry.GhostReactorOverdrivePurchased(gameId, shinyRocksUsed, seedsInQueue, floor, preset);
	}

	public void SendPodUpgradeTelemetry(string toolName, int level, int shinyRocksSpent, int juiceSpent)
	{
		GorillaTelemetry.GhostReactorPodUpgradePurchased(gameId, toolName, level, shinyRocksSpent, juiceSpent);
	}

	public void SendCreditsRefilledTelemetry(int shinyRocksSpent, int finalCredits)
	{
		int floor = -1;
		string preset = "";
		GhostReactor instance = GhostReactor.instance;
		if (instance != null && instance.zone != GTZone.customMaps)
		{
			floor = instance.GetDepthLevel();
			preset = instance.GetCurrLevelGenConfig().name;
		}
		GorillaTelemetry.GhostReactorCreditsRefillPurchased(gameId, shinyRocksSpent, finalCredits, floor, preset);
	}

	public void ResetTelemetryTracking(string newGameId, float timeSinceShiftStart)
	{
		gameId = newGameId;
		coresCollectedByPlayer = 0;
		coresCollectedByGroup = 0;
		gatesUnlocked = 0;
		deaths = 0;
		caughtByAnomaly = false;
		itemsPurchased = new List<string>();
		levelsUnlocked = new List<string>();
		sentientCoresCollected = 0;
		vrRigs.Clear();
		VRRigCache.Instance.GetAllUsedRigs(vrRigs);
		maxNumberOfPlayersInShift = vrRigs.Count + 1;
		timeIntoShiftAtJoin = timeSinceShiftStart;
		itemsHeldThisShift.Clear();
		itemTypesHeldThisShift.Clear();
	}

	public void ResetGameTelemetryTracking()
	{
		totalCoresCollectedByPlayer = 0;
		totalCoresCollectedByGroup = 0;
		totalGatesUnlocked = 0;
		totalDeaths = 0;
		totalItemsPurchased = new List<string>();
		vrRigs.Clear();
		VRRigCache.Instance.GetAllUsedRigs(vrRigs);
		maxNumberOfPlayersIngame = vrRigs.Count + 1;
		totalItemsHeldThisShift.Clear();
		totalItemTypesHeldThisShift.Clear();
		numShiftsPlayed = 0;
		isFirstShift = false;
	}

	public void IncrementCoresCollectedPlayer(int coreValue)
	{
		totalCoresCollectedByPlayer += coreValue;
		coresCollectedByPlayer += coreValue;
	}

	public void IncrementCoresCollectedGroup(int coreValue)
	{
		totalCoresCollectedByGroup += coreValue;
		coresCollectedByGroup += coreValue;
	}

	public void IncrementCoresSpentPlayer(int coreValue)
	{
		totalCoresSpentByPlayer += coreValue;
		coresSpentByPlayer += coreValue;
	}

	public void IncrementCoresSpentGroup(int coreValue)
	{
		totalCoresSpentByGroup += coreValue;
		coresSpentByGroup += coreValue;
	}

	public void IncrementChaosSeedsCollected(int numSeeds)
	{
		sentientCoresCollected += numSeeds;
	}

	public void IncrementGatesUnlocked(int numGatesUnlocked)
	{
		gatesUnlocked += numGatesUnlocked;
		totalGatesUnlocked += numGatesUnlocked;
	}

	public void IncrementDeaths(int numDeaths)
	{
		deaths += numDeaths;
		totalDeaths += numDeaths;
	}

	public void IncrementRevives(int numRevives)
	{
		revives += numRevives;
		totalRevives += numRevives;
	}

	public void IncrementShiftsPlayed(int numShifts)
	{
		numShiftsPlayed += numShifts;
	}

	public void AddItemPurchased(string newItemPurchased)
	{
		itemsPurchased.Add(newItemPurchased);
		totalItemsPurchased.Add(newItemPurchased);
	}

	public void GrabbedItem(GameEntityId id, string itemName)
	{
		if (itemsHeldThisShift.Contains(id))
		{
			return;
		}
		itemsHeldThisShift.Add(id);
		if (itemTypesHeldThisShift.ContainsKey(itemName))
		{
			itemTypesHeldThisShift[itemName] += 1;
		}
		else
		{
			itemTypesHeldThisShift[itemName] = 1;
		}
		if (!totalItemsHeldThisShift.Contains(id))
		{
			totalItemsHeldThisShift.Add(id);
			if (totalItemTypesHeldThisShift.ContainsKey(itemName))
			{
				totalItemTypesHeldThisShift[itemName] += 1;
			}
			else
			{
				totalItemTypesHeldThisShift[itemName] = 1;
			}
		}
	}

	public GRShuttle GetAssignedShuttle(bool isOnDrillovator)
	{
		_ = GhostReactor.instance;
		GRShuttle drillShuttleForPlayer = GRElevatorManager._instance.GetDrillShuttleForPlayer(gamePlayer.rig.OwningNetPlayer.ActorNumber);
		GRShuttle stagingShuttleForPlayer = GRElevatorManager._instance.GetStagingShuttleForPlayer(gamePlayer.rig.OwningNetPlayer.ActorNumber);
		if (!isOnDrillovator)
		{
			return stagingShuttleForPlayer;
		}
		return drillShuttleForPlayer;
	}

	public void RefreshShuttles()
	{
		GRShuttle assignedShuttle = GetAssignedShuttle(isOnDrillovator: true);
		if (assignedShuttle != null)
		{
			assignedShuttle.Refresh();
		}
		assignedShuttle = GetAssignedShuttle(isOnDrillovator: false);
		if (assignedShuttle != null)
		{
			assignedShuttle.Refresh();
		}
	}

	public static GRPlayer GetFromUserId(string userId)
	{
		tempRigs.Clear();
		tempRigs.Add(VRRig.LocalRig);
		VRRigCache.Instance.GetAllUsedRigs(tempRigs);
		for (int i = 0; i < tempRigs.Count; i++)
		{
			if (tempRigs[i].OwningNetPlayer != null && tempRigs[i].OwningNetPlayer.UserId == userId)
			{
				return Get(tempRigs[i].OwningNetPlayer);
			}
		}
		return null;
	}

	[ContextMenu("Refresh Damage Vignette Visual")]
	public void RefreshDamageVignetteVisual()
	{
		if (!vrRig.isLocal || currentHealthVisualValue == hp)
		{
			return;
		}
		currentHealthVisualValue = hp;
		if (hp <= damageOverlayMaxHp && hp > 0)
		{
			if (lowHeathVisualCoroutine != null)
			{
				StopCoroutine(lowHeathVisualCoroutine);
			}
			damageEffects.lowHealthVisualRenderer.gameObject.SetActive(value: true);
			lowHeathVisualCoroutine = StartCoroutine(LowHeathVisualCoroutine());
		}
		else
		{
			damageEffects.lowHealthVisualRenderer.gameObject.SetActive(value: false);
		}
	}

	private IEnumerator LowHeathVisualCoroutine()
	{
		int index = hp - 1;
		if (index >= 0 && index < damageOverlayValues.Count)
		{
			float startTime = Time.time;
			while (Time.time - startTime < damageOverlayValues[index].effectDuration)
			{
				float time = Mathf.Clamp01((Time.time - startTime) / damageOverlayValues[index].effectDuration);
				float num = damageOverlayValues[index].effectCurve.Evaluate(time);
				Color tint = damageOverlayValues[index].tint;
				tint.a *= num;
				damageEffects.lowHealthVisualRenderer.GetPropertyBlock(lowHealthVisualPropertyBlock);
				lowHealthVisualPropertyBlock.SetColor(lowHealthTintPropertyId, tint);
				damageEffects.lowHealthVisualRenderer.SetPropertyBlock(lowHealthVisualPropertyBlock);
				yield return null;
			}
		}
	}

	public void SetGooParticleSystemEnabled(bool bIsLeftHand, bool newEnableState)
	{
		if (vrRig != null)
		{
			vrRig.SetGooParticleSystemStatus(bIsLeftHand, newEnableState);
		}
	}

	public void SetAsFrozen(float duration)
	{
		if (GorillaTagger.Instance.currentStatus == GorillaTagger.StatusEffect.Frozen)
		{
			return;
		}
		freezeDuration = duration;
		if (gamePlayer.rig.OwningNetPlayer.IsLocal)
		{
			GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Frozen, duration);
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
			GorillaTagger.Instance.offlineVRRig.PlayTaggedEffect();
			if (damageEffects.frozenVisualRenderer != null)
			{
				damageEffects.frozenVisualRenderer.gameObject.SetActive(value: true);
			}
			playerDamageAudioSource.PlayOneShot(playerFrozenSound, 1f);
		}
		gamePlayer.rig.UpdateFrozenEffect(enable: true);
		Invoke("RemoveFrozen", duration);
	}

	public void RemoveFrozen()
	{
		gamePlayer.rig.UpdateFrozenEffect(enable: false);
		freezeDuration = 0f;
		if (damageEffects.frozenVisualRenderer != null)
		{
			damageEffects.frozenVisualRenderer.gameObject.SetActive(value: false);
		}
	}

	public override void Tick()
	{
		if (lastPlayerPosition != Vector3.zero)
		{
			Vector3 position = vrRig.transform.position;
			float magnitude = (lastPlayerPosition - position).magnitude;
			IncrementSynchronizedSessionStat(SynchronizedSessionStat.DistanceTraveled, magnitude);
		}
		lastPlayerPosition = vrRig.transform.position;
		if (freezeDuration > 0f)
		{
			gamePlayer.rig.UpdateFrozen(Time.deltaTime, freezeDuration);
		}
		if (inStealthMode && Time.timeAsDouble > shieldStealthModeEndTime)
		{
			ClearStealthMode();
		}
		GRShuttle.UpdateGRPlayerShuttle(this);
		if (soak != null && soak.IsSoaking())
		{
			soak.OnUpdate();
		}
	}

	public void SetSynchronizedSessionStat(SynchronizedSessionStat stat, float amt)
	{
		synchronizedSessionStats[(int)stat] = amt;
	}

	public void IncrementSynchronizedSessionStat(SynchronizedSessionStat stat, float amt)
	{
		synchronizedSessionStats[(int)stat] += amt;
	}

	public void ResetSynchronizedSessionStats()
	{
		for (int i = 0; i < 8; i++)
		{
			synchronizedSessionStats[i] = 0f;
		}
	}

	private void RequestSetMothershipUserData(string keyName, string value)
	{
		if (saveEquipmentInProgress)
		{
			Debug.LogError("SharedBlocksManager RequestSetMothershipUserData: request already in progress");
			return;
		}
		saveEquipmentInProgress = true;
		try
		{
			if (!MothershipClientApiUnity.SetUserDataValue(keyName, value, OnSetMothershipUserDataSuccess, OnSetMothershipUserDataFail))
			{
				Debug.LogError("SharedBlocksManager RequestSetMothershipUserData: SetUserDataValue Fail");
				OnSetMothershipDataComplete(success: false);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("SharedBlocksManager RequestSetMothershipUserData: exception " + ex.Message);
			OnSetMothershipDataComplete(success: false);
		}
	}

	private void OnSetMothershipUserDataSuccess(SetUserDataResponse response)
	{
		GTDev.Log("GRPlayer OnSetMothershipUserDataSuccess");
		OnSetMothershipDataComplete(success: true);
		response.Dispose();
	}

	private void OnSetMothershipUserDataFail(MothershipError error, int status)
	{
		string text = ((error == null) ? status.ToString() : error.Message);
		GTDev.LogError("GRPlayer OnSetMothershipUserDataFail: " + text);
		OnSetMothershipDataComplete(success: false);
		error?.Dispose();
	}

	private void OnSetMothershipDataComplete(bool success)
	{
		saveEquipmentInProgress = false;
	}

	public void RequestFetchMothershipUserData(string key)
	{
		if (hasPulledEquipment)
		{
			return;
		}
		try
		{
			if (!MothershipClientApiUnity.GetUserDataValue(key, OnGetMothershipFetchUserDataSuccess, OnGetMothershipFetchUserDataFail))
			{
				Debug.LogError("GRPlayer RequestFetchMothershipUserData failed ");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("GRPlayer RequestFetchMothershipUserData exception " + ex.Message);
		}
	}

	private void OnGetMothershipFetchUserDataSuccess(MothershipUserData response)
	{
		GTDev.Log("GRPlayer OnGetMothershipFetchUserDataSuccess");
		bool flag = response != null && response.value != null && response.value.Length > 0;
		if (response != null)
		{
		}
		response?.Dispose();
	}

	private void OnGetMothershipFetchUserDataFail(MothershipError error, int status)
	{
		string text = ((error == null) ? status.ToString() : error.Message);
		GTDev.LogError("GRPlayer OnGetMothershipFetchUserDataFail: " + text);
		error?.Dispose();
	}

	public bool IsDropPodUnlocked()
	{
		return dropPodLevel > 0;
	}

	public int GetMaxDropFloor()
	{
		return (dropPodChasisLevel + dropPodLevel) switch
		{
			0 => 1, 
			1 => 5, 
			2 => 10, 
			3 => 15, 
			4 => 20, 
			_ => 0, 
		};
	}

	public void CollectShiftCut()
	{
		SetProgressionData(currentProgression.points + LastShiftCut, currentProgression.redeemedPoints, saveProgression: true);
	}

	public bool AttemptPromotion()
	{
		(int tier, int grade, int totalPointsToNextLevel, int partialPointsToNextLevel) gradePointDetails = GhostReactorProgression.GetGradePointDetails(CurrentProgression.redeemedPoints);
		int item = gradePointDetails.totalPointsToNextLevel;
		int item2 = gradePointDetails.partialPointsToNextLevel;
		if (item - item2 < CurrentProgression.points - CurrentProgression.redeemedPoints)
		{
			SetProgressionData(currentProgression.points, currentProgression.points);
			return true;
		}
		return false;
	}

	public void SetProgressionData(int _points, int _redeemedPoints, bool saveProgression = false)
	{
		if (_points >= 0 && _redeemedPoints >= 0)
		{
			currentProgression = new ProgressionData
			{
				points = _points,
				redeemedPoints = _redeemedPoints
			};
			if (gamePlayer.IsLocal() && saveProgression)
			{
				SaveMyProgression();
			}
		}
	}

	public void LoadMyProgression()
	{
		GhostReactorProgression.instance.GetStartingProgression(this);
	}

	public void SaveMyProgression()
	{
		GhostReactorProgression.instance.SetProgression(LastShiftCut, this);
	}
}
