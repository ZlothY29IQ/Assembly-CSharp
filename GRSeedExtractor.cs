using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class GRSeedExtractor : MonoBehaviour
{
	public struct PlayerData
	{
		public int actorNumber;

		public int coreCount;

		public float coreProcessingPercentage;

		public float overdriveSupply;

		public int coresProcessedByOverdrive;

		public int coresPendingOverdriveProcessing;

		public int researchPoints;

		public float latestRefreshTime;
	}

	private struct ScreenDisplayData
	{
		public int playerActorNumber;

		public int coreCount;

		public float overdriveSupply;

		public int researchPoints;

		public int juiceSecondsLeft;
	}

	private struct SeedProcessingVisualState
	{
		public int poolIndex;

		public float speed;

		public float rollAngle;

		public float rampProgress;

		public float dropProgress;
	}

	private float PROCESSING_TIME_SECONDS = 600f;

	private int MAX_OVERDRIVE_USES = 6;

	[SerializeField]
	private GTZone zone;

	[SerializeField]
	private TriggerEventNotifier triggerNotifier;

	[SerializeField]
	private TriggerEventNotifier coreDepositTriggerNotifier;

	[SerializeField]
	private TMP_Text screenText;

	[SerializeField]
	private IDCardScanner idCardScanner;

	[SerializeField]
	private GameObject chaosSeedVisualPrefab;

	[Header("Overdrive Purchase Buttons")]
	[SerializeField]
	private GorillaPressableButton overdrivePurchaseButton;

	[SerializeField]
	private GorillaPressableButton overdriveConfirmButton;

	[SerializeField]
	private Material defaultButtonMaterial;

	[SerializeField]
	private Material redButtonMaterial;

	[SerializeField]
	private Material greenButtonMaterial;

	[Header("Shutter Door Visual")]
	[SerializeField]
	private Transform shutterDoorParent;

	[SerializeField]
	private Vector2 shutterDoorLiftRange = new Vector2(1.245f, 2.07f);

	[SerializeField]
	private float shutterDoorAnimTime;

	[Header("Seed Processing Visual")]
	[SerializeField]
	private Transform processingLiquidScaleParent;

	[SerializeField]
	[Range(0f, 1f)]
	public float processingAmount;

	private float processingAmountVisual;

	[SerializeField]
	private Transform seedTubeStart;

	[SerializeField]
	private Transform seedTubeEnd;

	[SerializeField]
	private Transform seedProcessingPosition;

	[SerializeField]
	private AnimationCurve tubeEndToProcessingPathY = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve tubeEndToProcessingPathX = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float visualChaosSeedRadius = 1f;

	[SerializeField]
	private int maxVisualChaosSeedCount = 6;

	[SerializeField]
	private float seedVisualRollTime = 2f;

	[SerializeField]
	private float seedVisualDropTime = 0.5f;

	[SerializeField]
	private Vector2 seedVisualScaleRange = new Vector2(0.1f, 1.25f);

	[Header("Overdrive Visual")]
	[SerializeField]
	private Transform overdriveLiquidScaleParent;

	[SerializeField]
	private Transform overdriveLightSpinnerOff;

	[SerializeField]
	private Transform overdriveLightSpinnerOn;

	[SerializeField]
	private List<Transform> enableDuringOverdrive = new List<Transform>();

	[SerializeField]
	private List<Transform> disableDuringOverdrive = new List<Transform>();

	[SerializeField]
	private float overdriveLightSpinRate = 1f;

	[SerializeField]
	[Range(0f, 1f)]
	public float overdriveAmount;

	private float overdriveAmountVisual;

	[Header("VFX")]
	[SerializeField]
	private ParticleSystem depositorParticles;

	[SerializeField]
	private ParticleSystem juicerSlowParticles;

	[SerializeField]
	private ParticleSystem juicerOverdriveParticles;

	[Header("Audio")]
	[SerializeField]
	private AudioSource depositorAudioSource;

	[SerializeField]
	private AudioSource doorAudioSource;

	[SerializeField]
	private AudioSource seedTubeAudioSource;

	[SerializeField]
	private AudioSource juicerAudioSource;

	[SerializeField]
	private AudioSource machineHumAudioSource;

	[SerializeField]
	private AudioSource overdriveMeterAudioSource;

	[SerializeField]
	private AudioSource overdriveBeepAudioSource;

	[SerializeField]
	private AudioClip seedDepositAudio;

	[SerializeField]
	private float seedDepositVolume = 0.5f;

	[SerializeField]
	private AudioClip seedDepositFailedAudio;

	[SerializeField]
	private float seedDepositFailedVolume = 0.5f;

	[SerializeField]
	private AudioClip seedDepositAttemptAudio;

	[SerializeField]
	private float seedDepositAttemptVolume = 0.5f;

	[SerializeField]
	private AudioClip seedMovementAudio;

	[SerializeField]
	private float seedMovementVolume = 0.5f;

	[SerializeField]
	private AudioClip seedDropAudio;

	[SerializeField]
	private float seedDropVolume = 0.5f;

	[SerializeField]
	private AudioClip seedJuicingAudio;

	[SerializeField]
	private float seedJuicingVolume = 0.5f;

	[SerializeField]
	private AudioClip doorOpenAudio;

	[SerializeField]
	private float doorOpenVolume = 0.5f;

	[SerializeField]
	private AudioClip doorCloseAudio;

	[SerializeField]
	private float doorCloseVolume = 0.5f;

	[SerializeField]
	private AudioClip processingHumAudio;

	[SerializeField]
	private float processingHumVolume = 0.5f;

	[SerializeField]
	private AudioClip overdriveFillAudio;

	[SerializeField]
	private float overdriveFillVolume = 0.5f;

	[SerializeField]
	private AudioClip overdriveEngineAudio;

	[SerializeField]
	private float overdriveEngineVolume = 0.5f;

	[SerializeField]
	private AudioClip overdriveBeepingAudio;

	[SerializeField]
	private float overdriveBeepingVolume = 0.5f;

	private PlayerData localPlayerData;

	private PlayerData currentPlayerData;

	private ScreenDisplayData currentDisplayData;

	private bool stationOpen;

	private float stationOpenRequestTime;

	private int currentPlayerActorNumber = -1;

	private float shutterDoorOpenAmount;

	private List<GameObject> chaosSeedVisuals = new List<GameObject>();

	private bool overdrivePurchasePending;

	private bool overdriveServerConfirmationPending;

	private float overdrivePurchaseTime;

	private bool overdriveActive;

	private bool drainingProcessingBeaker;

	private float estimatedJuiceTimeRemaining;

	private float processingLiquidFollowRate = Mathf.Exp(2f);

	private List<(int, int, float, bool)> seedDepositsPending = new List<(int, int, float, bool)>();

	private Coroutine overdrivePurchaseAnimationRoutine;

	private List<SeedProcessingVisualState> seedProcessingStates = new List<SeedProcessingVisualState>();

	private float timeBetweenServerRequests = 3f;

	private float lastServerRequestTime;

	private GhostReactor ghostReactor;

	private GRToolProgressionManager toolProgressionManager;

	private StringBuilder UpdateScreenSB = new StringBuilder(256);

	[Header("Debug Animation")]
	public int debugSeedCount;

	public float debugSeedProcessingTime = 10f;

	public float overdriveFillTime = 2f;

	public float overdriveProcessTime = 1.5f;

	public float juiceDepositTime = 0.75f;

	public bool StationOpen => stationOpen;

	public bool StationOpenForLocalPlayer
	{
		get
		{
			if (stationOpen)
			{
				return currentPlayerActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber;
			}
			return false;
		}
	}

	public int CurrentPlayerActorNumber => currentPlayerActorNumber;

	private void Awake()
	{
		triggerNotifier.TriggerEnterEvent += TriggerEntered;
		triggerNotifier.TriggerExitEvent += TriggerExited;
		coreDepositTriggerNotifier.TriggerEnterEvent += DepositorTriggerEntered;
		idCardScanner.OnPlayerCardSwipe += OnPlayerCardSwipe;
		for (int i = 0; i < maxVisualChaosSeedCount; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(chaosSeedVisualPrefab, base.transform);
			gameObject.SetActive(value: false);
			chaosSeedVisuals.Add(gameObject);
		}
		UpdateOverdrivePurchaseButtons();
		base.enabled = false;
	}

	public void Init(GRToolProgressionManager progression, GhostReactor gr)
	{
		ghostReactor = gr;
		toolProgressionManager = progression;
		toolProgressionManager.OnProgressionUpdated += OnResearchPointsUpdated;
		ProgressionManager.Instance.OnJucierStatusUpdated += OnPlayerStatusReceived;
		ProgressionManager.Instance.OnPurchaseOverdrive += OnPurchaseOverdrive;
		ProgressionManager.Instance.OnChaosDepositSuccess += TryDepositSeedServerResponse;
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		ClearSeedVisuals();
		machineHumAudioSource.gameObject.SetActive(value: false);
		juicerSlowParticles.gameObject.SetActive(value: false);
		StopAllCoroutines();
		for (int i = 0; i < disableDuringOverdrive.Count; i++)
		{
			disableDuringOverdrive[i].gameObject.SetActive(value: true);
		}
		for (int j = 0; j < enableDuringOverdrive.Count; j++)
		{
			enableDuringOverdrive[j].gameObject.SetActive(value: false);
		}
		overdriveLightSpinnerOff.localRotation = overdriveLightSpinnerOn.localRotation;
		overdriveBeepAudioSource.Stop();
		overdriveActive = false;
		processingAmount = 0f;
		processingAmountVisual = 0f;
		overdriveAmount = 0f;
		overdriveAmountVisual = 0f;
		currentPlayerData = default(PlayerData);
		overdriveLiquidScaleParent.transform.localScale = new Vector3(1f, Mathf.Clamp01(overdriveAmountVisual), 1f);
		processingLiquidScaleParent.transform.localScale = new Vector3(1f, Mathf.Clamp01(processingAmountVisual), 1f);
	}

	private void Update()
	{
		ValidateCurrentPlayer();
		if (stationOpen && shutterDoorOpenAmount < 1f)
		{
			float num = Time.time - currentPlayerData.latestRefreshTime;
			if (!(Time.time - stationOpenRequestTime < 1f) || !(num > 5f))
			{
				float num2 = 1f / shutterDoorAnimTime;
				shutterDoorOpenAmount = Mathf.MoveTowards(shutterDoorOpenAmount, 1f, num2 * Time.deltaTime);
				Vector3 localPosition = shutterDoorParent.transform.localPosition;
				localPosition.y = Mathf.Lerp(shutterDoorLiftRange.x, shutterDoorLiftRange.y, shutterDoorOpenAmount);
				shutterDoorParent.transform.localPosition = localPosition;
			}
		}
		else if (!stationOpen && shutterDoorOpenAmount > 0f)
		{
			float num3 = 1f / shutterDoorAnimTime;
			shutterDoorOpenAmount = Mathf.MoveTowards(shutterDoorOpenAmount, 0f, num3 * Time.deltaTime);
			Vector3 localPosition2 = shutterDoorParent.transform.localPosition;
			localPosition2.y = Mathf.Lerp(shutterDoorLiftRange.x, shutterDoorLiftRange.y, shutterDoorOpenAmount);
			shutterDoorParent.transform.localPosition = localPosition2;
			if (shutterDoorOpenAmount <= 0f)
			{
				processingAmount = 0f;
				overdriveAmount = 0f;
			}
		}
		bool flag = seedProcessingStates.Count > 0 && seedProcessingStates[0].dropProgress >= 1f;
		if (overdriveActive)
		{
			overdriveLightSpinnerOn.Rotate(Vector3.forward, 360f * overdriveLightSpinRate * Time.deltaTime, Space.Self);
			overdriveAmountVisual = overdriveAmount;
			overdriveLiquidScaleParent.transform.localScale = new Vector3(1f, Mathf.Clamp01(overdriveAmountVisual), 1f);
			processingAmountVisual = processingAmount;
			processingLiquidScaleParent.transform.localScale = new Vector3(1f, Mathf.Clamp01(processingAmountVisual), 1f);
		}
		else
		{
			float num4 = 1f / overdriveFillTime;
			if (flag || overdriveAmount > overdriveAmountVisual || !stationOpen)
			{
				overdriveAmountVisual = Mathf.MoveTowards(overdriveAmountVisual, overdriveAmount, num4 * Time.deltaTime);
			}
			overdriveLiquidScaleParent.transform.localScale = new Vector3(1f, Mathf.Clamp01(overdriveAmountVisual), 1f);
			if (stationOpen)
			{
				float num5 = Mathf.Max(Time.time - currentPlayerData.latestRefreshTime, 0f);
				float num6 = currentPlayerData.coreProcessingPercentage + num5 / PROCESSING_TIME_SECONDS;
				processingAmount = Mathf.Clamp01(num6);
				estimatedJuiceTimeRemaining = (1f - processingAmount) * PROCESSING_TIME_SECONDS;
				if (StationOpenForLocalPlayer && num6 >= 1f && Time.time - lastServerRequestTime > timeBetweenServerRequests)
				{
					lastServerRequestTime = Time.time;
					ProgressionManager.Instance.GetJuicerStatus();
				}
			}
			if (flag)
			{
				machineHumAudioSource.gameObject.SetActive(value: true);
				juicerSlowParticles.gameObject.SetActive(value: true);
				processingAmountVisual = Mathf.MoveTowards(processingAmountVisual, processingAmount, num4 * Time.deltaTime);
			}
			else
			{
				processingAmountVisual = Mathf.MoveTowards(processingAmountVisual, 0f, num4 * Time.deltaTime);
				machineHumAudioSource.gameObject.SetActive(value: false);
				juicerSlowParticles.gameObject.SetActive(value: false);
			}
			processingLiquidScaleParent.transform.localScale = new Vector3(1f, Mathf.Clamp01(processingAmountVisual), 1f);
		}
		StepSeedVisualAnimation(Time.deltaTime);
		UpdateScreenDisplay();
		if (!stationOpen && shutterDoorOpenAmount <= 0f && overdriveAmountVisual <= 0f)
		{
			base.enabled = false;
		}
	}

	private void ValidateCurrentPlayer()
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			CloseStation();
		}
		else
		{
			if (!ghostReactor.grManager.IsAuthority() || !stationOpen)
			{
				return;
			}
			bool flag = false;
			NetPlayer player = NetworkSystem.Instance.GetPlayer(currentPlayerActorNumber);
			if (player != null && VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
			{
				float num = 5f;
				if (playerRig.Rig != null && playerRig.Rig.OwningNetPlayer == player && (playerRig.Rig.GetMouthPosition() - base.transform.position).sqrMagnitude < num * num)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				ghostReactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SeedExtractorCloseStation, NetworkSystem.Instance.LocalPlayer.ActorNumber, 0);
			}
		}
	}

	public void TriggerEntered(TriggerEventNotifier notifier, Collider other)
	{
		VRRig component = other.GetComponent<VRRig>();
		if (component != null && component.OwningNetPlayer != null && component.OwningNetPlayer.ActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber && NetworkSystem.Instance.InRoom)
		{
			ProgressionManager.Instance.GetJuicerStatus();
		}
	}

	public void TriggerExited(TriggerEventNotifier notifier, Collider other)
	{
		VRRig component = other.GetComponent<VRRig>();
		if (component != null && component.OwningNetPlayer != null)
		{
			if (component.OwningNetPlayer.ActorNumber == currentPlayerActorNumber && stationOpen && ghostReactor.grManager.IsAuthority() && NetworkSystem.Instance.InRoom)
			{
				ghostReactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SeedExtractorCloseStation, NetworkSystem.Instance.LocalPlayer.ActorNumber, 0);
			}
			if (component.OwningNetPlayer.ActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
			{
				localPlayerData = default(PlayerData);
			}
		}
	}

	public void OnPlayerCardSwipe(int playerActorNumber)
	{
		if (playerActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber && NetworkSystem.Instance.InRoom)
		{
			ghostReactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SeedExtractorOpenStation, NetworkSystem.Instance.LocalPlayer.ActorNumber, 0);
			ProgressionManager.Instance.GetJuicerStatus();
		}
	}

	public void DepositorTriggerEntered(TriggerEventNotifier notifier, Collider other)
	{
		if (ghostReactor == null || ghostReactor.grManager == null || other == null || !NetworkSystem.Instance.InRoom || !ghostReactor.grManager.IsAuthority() || !(other.attachedRigidbody != null))
		{
			return;
		}
		GRCollectible component = other.attachedRigidbody.GetComponent<GRCollectible>();
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(zone);
		if (!(managerForZone != null) || !(component != null) || component.type != ProgressionManager.CoreType.ChaosSeed)
		{
			return;
		}
		int netIdFromEntityId = managerForZone.GetNetIdFromEntityId(component.entity.id);
		int lastHeldByActorNumber = component.entity.lastHeldByActorNumber;
		NetPlayer player = NetworkSystem.Instance.GetPlayer(lastHeldByActorNumber);
		float time = Time.time;
		if (player == null)
		{
			return;
		}
		bool flag = false;
		for (int num = seedDepositsPending.Count - 1; num >= 0; num--)
		{
			if (time - seedDepositsPending[num].Item3 > 5f || managerForZone.GetGameEntityFromNetId(seedDepositsPending[num].Item1) == null || NetworkSystem.Instance.GetPlayer(seedDepositsPending[num].Item2) == null)
			{
				seedDepositsPending.RemoveAt(num);
			}
			else if (seedDepositsPending[num].Item1 == netIdFromEntityId)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			seedDepositsPending.Add((netIdFromEntityId, lastHeldByActorNumber, Time.time, false));
			ghostReactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SeedExtractorTryDepositSeed, lastHeldByActorNumber, netIdFromEntityId);
		}
	}

	public void OverdrivePurchaseButtonPressed()
	{
		if (overdrivePurchasePending)
		{
			overdrivePurchasePending = false;
		}
		else if (LocalPlayerCanPurchaseOverdrive())
		{
			overdrivePurchasePending = true;
		}
		UpdateOverdrivePurchaseButtons();
	}

	private bool LocalPlayerCanPurchaseOverdrive()
	{
		if (Time.time - overdrivePurchaseTime > 5f)
		{
			overdriveServerConfirmationPending = false;
		}
		if (StationOpenForLocalPlayer && !overdriveServerConfirmationPending && CosmeticsController.instance.CurrencyBalance >= 250)
		{
			return localPlayerData.overdriveSupply <= 0f;
		}
		return false;
	}

	public void OverdrivePurchaseConfirmButtonPressed()
	{
		if (overdrivePurchasePending)
		{
			overdrivePurchasePending = false;
			if (stationOpen && currentPlayerActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
			{
				overdriveServerConfirmationPending = true;
				overdrivePurchaseTime = Time.time;
				ProgressionManager.Instance.PurchaseOverdrive();
			}
		}
		UpdateOverdrivePurchaseButtons();
	}

	public void OnPlayerStatusReceived(ProgressionManager.JuicerStatusResponse statusResponse)
	{
		if (statusResponse.MothershipId == GRPlayer.GetLocal().mothershipId && statusResponse.RefreshJuice)
		{
			toolProgressionManager.UpdateInventory();
		}
		PROCESSING_TIME_SECONDS = statusResponse.CoreProcessingTimeSec;
		MAX_OVERDRIVE_USES = statusResponse.OverdriveCap / 100;
		float num = Mathf.Clamp01((float)statusResponse.OverdriveSupply / (float)statusResponse.OverdriveCap);
		int num2 = 0;
		bool flag = num < localPlayerData.overdriveSupply;
		bool flag2 = localPlayerData.overdriveSupply == 0f && localPlayerData.coreCount > statusResponse.CurrentCoreCount;
		if (statusResponse.CoresProcessedByOverdrive > 0 && (flag || flag2))
		{
			num2 = statusResponse.CoresProcessedByOverdrive;
		}
		localPlayerData.actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		localPlayerData.coreCount = statusResponse.CurrentCoreCount;
		localPlayerData.coreProcessingPercentage = Mathf.Clamp01(statusResponse.CoreProcessingPercent);
		localPlayerData.overdriveSupply = num;
		localPlayerData.coresProcessedByOverdrive = statusResponse.CoresProcessedByOverdrive;
		localPlayerData.coresPendingOverdriveProcessing += num2;
		localPlayerData.latestRefreshTime = Time.time;
		localPlayerData.researchPoints = toolProgressionManager.GetNumberOfResearchPoints();
		if (overdriveServerConfirmationPending && (localPlayerData.overdriveSupply > 0f || localPlayerData.coresProcessedByOverdrive > 0))
		{
			overdriveServerConfirmationPending = false;
		}
		if (stationOpen && currentPlayerActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber && NetworkSystem.Instance.InRoom)
		{
			currentPlayerData = localPlayerData;
			ghostReactor.grManager.RequestApplySeedExtractorState(localPlayerData.coreCount, localPlayerData.coresProcessedByOverdrive, localPlayerData.researchPoints, localPlayerData.coreProcessingPercentage, localPlayerData.overdriveSupply);
			OnStateUpdated();
		}
	}

	private void TryDepositSeedServerResponse(bool succeeded)
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			return;
		}
		int num = -1;
		int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		for (int i = 0; i < seedDepositsPending.Count; i++)
		{
			if (seedDepositsPending[i].Item2 == actorNumber)
			{
				num = seedDepositsPending[i].Item1;
			}
		}
		if (num != -1)
		{
			if (succeeded)
			{
				ghostReactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SeedExtractorDepositSeedSucceeded, actorNumber, num);
				RemovePendingSeedDeposit(num);
				GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
				gRPlayer.SendSeedDepositedTelemetry(PROCESSING_TIME_SECONDS.ToString(), currentPlayerData.coreCount);
				gRPlayer.IncrementChaosSeedsCollected(1);
			}
			else
			{
				ghostReactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SeedExtractorDepositSeedFailed, actorNumber, num);
			}
		}
	}

	public void CardSwipeSuccess()
	{
		idCardScanner.onSucceeded.Invoke();
	}

	public void CardSwipeFail()
	{
		idCardScanner.onFailed.Invoke();
	}

	public void TryDepositSeed(int playerActorNumber, int seedNetId)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(playerActorNumber);
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(zone);
		if (player == null || managerForZone == null)
		{
			return;
		}
		depositorAudioSource.PlayOneShot(seedDepositAttemptAudio, seedDepositAttemptVolume);
		if (player.ActorNumber != NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			return;
		}
		bool flag = false;
		float time = Time.time;
		for (int num = seedDepositsPending.Count - 1; num >= 0; num--)
		{
			if (time - seedDepositsPending[num].Item3 > 5f || managerForZone.GetGameEntityFromNetId(seedDepositsPending[num].Item1) == null || NetworkSystem.Instance.GetPlayer(seedDepositsPending[num].Item2) == null)
			{
				seedDepositsPending.RemoveAt(num);
			}
			else if (seedDepositsPending[num].Item1 == seedNetId)
			{
				flag = true;
				if (seedDepositsPending[num].Item2 == NetworkSystem.Instance.LocalPlayer.ActorNumber && !seedDepositsPending[num].Item4)
				{
					(int, int, float, bool) value = seedDepositsPending[num];
					value.Item4 = true;
					seedDepositsPending[num] = value;
					ProgressionManager.Instance.DepositCore(ProgressionManager.CoreType.ChaosSeed);
				}
			}
		}
		if (!flag)
		{
			seedDepositsPending.Add((seedNetId, playerActorNumber, Time.time, true));
			ProgressionManager.Instance.DepositCore(ProgressionManager.CoreType.ChaosSeed);
		}
	}

	public bool ValidateSeedDepositSucceeded(int playerActorNumber, int entityNetId)
	{
		if (ghostReactor.grManager.IsAuthority())
		{
			bool result = false;
			for (int i = 0; i < seedDepositsPending.Count; i++)
			{
				if (seedDepositsPending[i].Item1 == entityNetId && seedDepositsPending[i].Item2 == playerActorNumber)
				{
					result = true;
				}
			}
			return result;
		}
		return false;
	}

	public void SeedDepositSucceeded(int playerActorNumber, int entityNetId)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			depositorParticles.Play();
			depositorAudioSource.PlayOneShot(seedDepositAudio, seedDepositVolume);
			RemovePendingSeedDeposit(entityNetId);
			if (playerActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
			{
				ProgressionManager.Instance.GetJuicerStatus();
			}
			if (!stationOpen && ghostReactor.grManager.IsAuthority())
			{
				ghostReactor.grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SeedExtractorOpenStation, playerActorNumber, 0);
			}
		}
	}

	public void SeedDepositFailed(int playerActorNumber, int entityNetId)
	{
		depositorAudioSource.PlayOneShot(seedDepositFailedAudio, seedDepositFailedVolume);
		RemovePendingSeedDeposit(entityNetId);
	}

	private void RemovePendingSeedDeposit(int entityId)
	{
		for (int num = seedDepositsPending.Count - 1; num >= 0; num--)
		{
			if (seedDepositsPending[num].Item1 == entityId)
			{
				seedDepositsPending.RemoveAt(num);
			}
		}
	}

	public void ApplyState(int playerActorNumber, int coreCount, int coresProcessedByOverdrive, int researchPoints, float coreProcessingPercentage, float overdriveSupply)
	{
		if (playerActorNumber == currentPlayerActorNumber)
		{
			if (currentPlayerData.actorNumber != playerActorNumber)
			{
				currentPlayerData = default(PlayerData);
			}
			coreCount = Mathf.Clamp(coreCount, 0, maxVisualChaosSeedCount);
			coresProcessedByOverdrive = Mathf.Clamp(coresProcessedByOverdrive, 0, MAX_OVERDRIVE_USES);
			coreProcessingPercentage = Mathf.Clamp(coreProcessingPercentage, 0f, 1f);
			overdriveSupply = Mathf.Clamp(overdriveSupply, 0f, 1f);
			bool flag = overdriveSupply < currentPlayerData.overdriveSupply;
			bool flag2 = currentPlayerData.overdriveSupply == 0f && currentPlayerData.coreCount > coreCount;
			if (playerActorNumber != NetworkSystem.Instance.LocalPlayer.ActorNumber && coresProcessedByOverdrive > 0 && (flag || flag2))
			{
				currentPlayerData.coresPendingOverdriveProcessing += coresProcessedByOverdrive;
			}
			currentPlayerData.actorNumber = playerActorNumber;
			currentPlayerData.coreCount = coreCount;
			currentPlayerData.coresProcessedByOverdrive = coresProcessedByOverdrive;
			currentPlayerData.coreProcessingPercentage = coreProcessingPercentage;
			currentPlayerData.overdriveSupply = overdriveSupply;
			currentPlayerData.latestRefreshTime = Time.time;
			currentPlayerData.researchPoints = researchPoints;
			OnStateUpdated();
		}
	}

	public void OpenStation(int playerActorNumber)
	{
		if (NetworkSystem.Instance.GetPlayer(playerActorNumber) != null)
		{
			if (!stationOpen)
			{
				doorAudioSource.PlayOneShot(doorOpenAudio, doorOpenVolume);
			}
			base.enabled = true;
			currentPlayerActorNumber = playerActorNumber;
			stationOpen = true;
			stationOpenRequestTime = Time.time;
			UpdateOverdrivePurchaseButtons();
		}
	}

	public void CloseStation()
	{
		if (stationOpen)
		{
			doorAudioSource.PlayOneShot(doorCloseAudio, doorCloseVolume);
		}
		currentPlayerActorNumber = -1;
		stationOpen = false;
		UpdateOverdrivePurchaseButtons();
	}

	private void UpdateOverdrivePurchaseButtons()
	{
		if (LocalPlayerCanPurchaseOverdrive())
		{
			if (overdrivePurchasePending)
			{
				overdrivePurchaseButton.myTmpText.text = "CANCEL";
				overdrivePurchaseButton.buttonRenderer.material = redButtonMaterial;
				overdriveConfirmButton.myTmpText.text = "CONFIRM";
				overdriveConfirmButton.buttonRenderer.material = greenButtonMaterial;
			}
			else
			{
				overdrivePurchaseButton.myTmpText.text = "BUY";
				overdrivePurchaseButton.buttonRenderer.material = defaultButtonMaterial;
				overdriveConfirmButton.myTmpText.text = "";
				overdriveConfirmButton.buttonRenderer.material = defaultButtonMaterial;
			}
		}
		else
		{
			overdrivePurchaseButton.myTmpText.text = "";
			overdrivePurchaseButton.buttonRenderer.material = defaultButtonMaterial;
			overdriveConfirmButton.myTmpText.text = "";
			overdriveConfirmButton.buttonRenderer.material = defaultButtonMaterial;
		}
	}

	public void OnStateUpdated()
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(currentPlayerActorNumber);
		if (player == null)
		{
			CloseStation();
		}
		UpdateOverdrivePurchaseButtons();
		if (stationOpen && player != null)
		{
			if (overdriveActive)
			{
				return;
			}
			if (currentPlayerData.coresPendingOverdriveProcessing > 0)
			{
				int coresPendingOverdriveProcessing = currentPlayerData.coresPendingOverdriveProcessing;
				currentPlayerData.coresPendingOverdriveProcessing = 0;
				if (StationOpenForLocalPlayer)
				{
					localPlayerData.coresPendingOverdriveProcessing = 0;
				}
				overdrivePurchaseAnimationRoutine = StartCoroutine(OverdrivePurchaseAnimationVisual(coresPendingOverdriveProcessing));
				return;
			}
			processingAmount = currentPlayerData.coreProcessingPercentage;
			overdriveAmount = currentPlayerData.overdriveSupply;
			int num = Mathf.Clamp(currentPlayerData.coreCount, 0, maxVisualChaosSeedCount) - seedProcessingStates.Count;
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					DepositSeedVisual();
				}
			}
			else if (num < 0)
			{
				for (int num2 = 0; num2 > num; num2--)
				{
					CompleteSeedVisual();
				}
			}
		}
		else
		{
			screenText.text = "Player Data Lookup Failed.";
			overdriveAmount = 0f;
			processingAmount = 0f;
		}
	}

	private void DepositSeedVisual()
	{
		for (int i = 0; i < chaosSeedVisuals.Count; i++)
		{
			if (!chaosSeedVisuals[i].activeSelf)
			{
				SeedProcessingVisualState item = new SeedProcessingVisualState
				{
					poolIndex = i,
					rollAngle = 0f,
					speed = 0f,
					rampProgress = 0f,
					dropProgress = 0f
				};
				seedProcessingStates.Add(item);
				chaosSeedVisuals[i].SetActive(value: true);
				chaosSeedVisuals[i].transform.localPosition = seedTubeStart.localPosition;
				chaosSeedVisuals[i].transform.localRotation = Quaternion.identity;
				chaosSeedVisuals[i].transform.localScale = Vector3.one * seedVisualScaleRange.y;
				seedTubeAudioSource.PlayOneShot(seedMovementAudio, seedMovementVolume);
				break;
			}
		}
	}

	private void CompleteSeedVisual()
	{
		if (seedProcessingStates.Count > 0)
		{
			SeedProcessingVisualState seedProcessingVisualState = seedProcessingStates[0];
			chaosSeedVisuals[seedProcessingVisualState.poolIndex].SetActive(value: false);
			seedProcessingStates.RemoveAt(0);
		}
	}

	private void ClearSeedVisuals()
	{
		int count = seedProcessingStates.Count;
		for (int i = 0; i < count; i++)
		{
			CompleteSeedVisual();
		}
	}

	private void UpdateScreenDisplay()
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(currentPlayerActorNumber);
		if (player == null || !stationOpen)
		{
			return;
		}
		int num = (int)estimatedJuiceTimeRemaining;
		if (currentPlayerActorNumber == currentDisplayData.playerActorNumber && currentPlayerData.coreCount == currentDisplayData.coreCount && currentPlayerData.overdriveSupply == currentDisplayData.overdriveSupply && currentPlayerData.researchPoints == currentDisplayData.researchPoints && num == currentDisplayData.juiceSecondsLeft)
		{
			return;
		}
		currentDisplayData.playerActorNumber = currentPlayerActorNumber;
		currentDisplayData.coreCount = currentPlayerData.coreCount;
		currentDisplayData.overdriveSupply = currentPlayerData.overdriveSupply;
		currentDisplayData.researchPoints = currentPlayerData.researchPoints;
		currentDisplayData.juiceSecondsLeft = num;
		UpdateScreenSB.Clear();
		UpdateScreenSB.Append(player.SanitizedNickName + "\n");
		UpdateScreenSB.Append($"JUICE: <color=purple>⑮ {currentDisplayData.researchPoints}</color>\n\n");
		if (currentDisplayData.coreCount > 0)
		{
			UpdateScreenSB.Append($"Processing {currentDisplayData.coreCount} Seeds");
			switch (currentDisplayData.juiceSecondsLeft % 3)
			{
			case 2:
				UpdateScreenSB.Append(".");
				break;
			case 1:
				UpdateScreenSB.Append("..");
				break;
			default:
				UpdateScreenSB.Append("...");
				break;
			}
			int num2 = num / 3600;
			int num3 = num / 60 % 60;
			int num4 = num % 60;
			if (num2 > 0)
			{
				UpdateScreenSB.Append($"\nNext <color=purple>⑮</color> in {num2}:{num3:00}:{num4:00}\n");
			}
			else
			{
				UpdateScreenSB.Append($"\nNext <color=purple>⑮</color> in {num3}:{num4:00}\n");
			}
		}
		else
		{
			UpdateScreenSB.Append("Deposit Chaos Seed\nFor Juice Processing\n");
		}
		screenText.text = UpdateScreenSB.ToString();
	}

	private void StepSeedVisualAnimation(float dt)
	{
		float magnitude = (seedTubeStart.position - seedTubeEnd.position).magnitude;
		float num = magnitude / seedVisualRollTime;
		for (int i = 0; i < seedProcessingStates.Count; i++)
		{
			SeedProcessingVisualState value = seedProcessingStates[i];
			float num2 = 2f;
			if (i > 0)
			{
				num2 = seedProcessingStates[i - 1].rampProgress - 2f * visualChaosSeedRadius / magnitude;
			}
			if (value.rampProgress < 1f)
			{
				GameObject obj = chaosSeedVisuals[value.poolIndex];
				value.speed = Mathf.MoveTowards(value.speed, num, num * dt);
				float num3 = value.speed * dt;
				float num4 = num3 / magnitude;
				value.rampProgress = Mathf.Clamp01(value.rampProgress + num4);
				if (value.rampProgress >= num2)
				{
					value.rampProgress = num2;
					value.speed = 0f;
					num3 = (num4 = 0f);
				}
				obj.transform.localPosition = Vector3.Lerp(seedTubeStart.localPosition, seedTubeEnd.localPosition, value.rampProgress);
				value.rollAngle += num3 / visualChaosSeedRadius;
				obj.transform.localRotation = Quaternion.AngleAxis(value.rollAngle * 57.29578f, Vector3.forward);
			}
			if (i == 0 && value.rampProgress >= 1f)
			{
				GameObject gameObject = chaosSeedVisuals[value.poolIndex];
				if (value.dropProgress < 1f)
				{
					value.dropProgress += 1f / seedVisualDropTime * dt;
					value.rampProgress = 1f + value.dropProgress;
					float t = tubeEndToProcessingPathY.Evaluate(value.dropProgress);
					float t2 = tubeEndToProcessingPathX.Evaluate(value.dropProgress);
					Vector3 localPosition = gameObject.transform.localPosition;
					localPosition.y = Mathf.Lerp(seedTubeEnd.localPosition.y, seedProcessingPosition.localPosition.y, t);
					localPosition.x = Mathf.Lerp(seedTubeEnd.localPosition.x, seedProcessingPosition.localPosition.x, t2);
					gameObject.transform.localPosition = localPosition;
					float num5 = value.speed * dt;
					value.rollAngle += num5 / visualChaosSeedRadius;
					gameObject.transform.localRotation = Quaternion.AngleAxis(value.rollAngle * 57.29578f, Vector3.forward);
					if (value.dropProgress >= 1f)
					{
						juicerAudioSource.PlayOneShot(seedDropAudio, seedDropVolume);
					}
				}
				if (value.dropProgress >= 1f && !drainingProcessingBeaker)
				{
					gameObject.transform.localScale = Vector3.one * Mathf.Lerp(seedVisualScaleRange.y, seedVisualScaleRange.x, processingAmountVisual);
				}
			}
			seedProcessingStates[i] = value;
		}
	}

	private IEnumerator OverdrivePurchaseAnimationVisual(int coresToProcess)
	{
		overdriveActive = true;
		overdriveBeepAudioSource.loop = true;
		overdriveBeepAudioSource.volume = overdriveBeepingVolume;
		overdriveBeepAudioSource.clip = overdriveBeepingAudio;
		overdriveBeepAudioSource.Play();
		int num = Math.Min(coresToProcess + currentPlayerData.coreCount, maxVisualChaosSeedCount);
		while (seedProcessingStates.Count < num)
		{
			DepositSeedVisual();
		}
		for (int i = 0; i < disableDuringOverdrive.Count; i++)
		{
			disableDuringOverdrive[i].gameObject.SetActive(value: false);
		}
		for (int j = 0; j < enableDuringOverdrive.Count; j++)
		{
			enableDuringOverdrive[j].gameObject.SetActive(value: true);
		}
		overdriveMeterAudioSource.PlayOneShot(overdriveFillAudio, overdriveFillVolume);
		float overdriveFillRate = 1f / overdriveFillTime;
		float maxOverdriveFill = Mathf.Clamp01(currentPlayerData.overdriveSupply + (float)coresToProcess / (float)MAX_OVERDRIVE_USES);
		while (overdriveAmount < maxOverdriveFill)
		{
			overdriveAmount = Mathf.MoveTowards(overdriveAmount, maxOverdriveFill, overdriveFillRate * Time.deltaTime);
			yield return null;
		}
		overdriveMeterAudioSource.Stop();
		int i2 = 0;
		while (i2 < coresToProcess)
		{
			float waitForSeedDepositStartTime = Time.time;
			bool flag = seedProcessingStates.Count > 0 && seedProcessingStates[0].dropProgress >= 1f;
			while (!flag && Time.time - waitForSeedDepositStartTime < 3f)
			{
				yield return null;
				flag = seedProcessingStates.Count > 0 && seedProcessingStates[0].dropProgress >= 1f;
			}
			juicerAudioSource.PlayOneShot(seedJuicingAudio, seedJuicingVolume);
			juicerOverdriveParticles.gameObject.SetActive(value: true);
			float num2 = Mathf.Clamp01(1f - processingAmount);
			float timeToProcess = num2 * overdriveProcessTime;
			float startingProcessingAmount = processingAmount;
			float num3 = num2 / (float)MAX_OVERDRIVE_USES;
			float startingOverdrive = overdriveAmount;
			float resultingOverdrive = Mathf.Clamp01(overdriveAmount - num3);
			float timeProcessing = 0f;
			while (timeProcessing < timeToProcess)
			{
				timeProcessing += Time.deltaTime;
				float t = timeProcessing / timeToProcess;
				overdriveAmount = Mathf.Lerp(startingOverdrive, resultingOverdrive, t);
				processingAmount = Mathf.Lerp(startingProcessingAmount, 1f, t);
				estimatedJuiceTimeRemaining = timeToProcess - timeProcessing;
				yield return null;
			}
			CompleteSeedVisual();
			juicerOverdriveParticles.gameObject.SetActive(value: false);
			drainingProcessingBeaker = true;
			float timeDepositing = 0f;
			while (timeDepositing < juiceDepositTime)
			{
				timeDepositing += Time.deltaTime;
				float t2 = timeDepositing / juiceDepositTime;
				processingAmount = Mathf.Lerp(1f, 0f, t2);
				yield return null;
			}
			drainingProcessingBeaker = false;
			int num4 = i2 + 1;
			i2 = num4;
		}
		if (currentPlayerData.coresPendingOverdriveProcessing == 0 && currentPlayerData.coreCount == 1)
		{
			if (seedProcessingStates.Count == 0)
			{
				DepositSeedVisual();
			}
			float timeDepositing = Time.time;
			bool flag2 = seedProcessingStates.Count > 0 && seedProcessingStates[0].dropProgress >= 1f;
			while (!flag2 && Time.time - timeDepositing < 3f)
			{
				yield return null;
				flag2 = seedProcessingStates.Count > 0 && seedProcessingStates[0].dropProgress >= 1f;
			}
			float timeProcessing = 0f;
			float resultingOverdrive = processingAmount;
			float startingOverdrive = overdriveAmount;
			float startingProcessingAmount = Mathf.Clamp01(currentPlayerData.coreProcessingPercentage - resultingOverdrive) * overdriveProcessTime;
			while (timeProcessing < startingProcessingAmount)
			{
				timeProcessing += Time.deltaTime;
				float t3 = timeProcessing / startingProcessingAmount;
				processingAmount = Mathf.Clamp01(Mathf.Lerp(resultingOverdrive, currentPlayerData.coreProcessingPercentage, t3));
				overdriveAmount = Mathf.Clamp01(Mathf.Lerp(startingOverdrive, currentPlayerData.overdriveSupply, t3));
				yield return null;
			}
		}
		for (int k = 0; k < disableDuringOverdrive.Count; k++)
		{
			disableDuringOverdrive[k].gameObject.SetActive(value: true);
		}
		for (int l = 0; l < enableDuringOverdrive.Count; l++)
		{
			enableDuringOverdrive[l].gameObject.SetActive(value: false);
		}
		overdriveLightSpinnerOff.localRotation = overdriveLightSpinnerOn.localRotation;
		overdriveBeepAudioSource.Stop();
		overdriveActive = false;
		if (StationOpenForLocalPlayer)
		{
			ProgressionManager.Instance.GetJuicerStatus();
		}
		OnStateUpdated();
	}

	public void OnResearchPointsUpdated()
	{
		int numberOfResearchPoints = toolProgressionManager.GetNumberOfResearchPoints();
		if (numberOfResearchPoints > localPlayerData.researchPoints)
		{
			GRPlayer.GetLocal().SendJuiceCollectedTelemetry(numberOfResearchPoints - localPlayerData.researchPoints, localPlayerData.coresProcessedByOverdrive);
		}
		localPlayerData.researchPoints = numberOfResearchPoints;
		if (StationOpenForLocalPlayer)
		{
			bool num = currentPlayerData.researchPoints != localPlayerData.researchPoints;
			currentPlayerData.researchPoints = localPlayerData.researchPoints;
			if (num)
			{
				ghostReactor.grManager.RequestApplySeedExtractorState(localPlayerData.coreCount, localPlayerData.coresProcessedByOverdrive, localPlayerData.researchPoints, localPlayerData.coreProcessingPercentage, localPlayerData.overdriveSupply);
				OnStateUpdated();
			}
		}
	}

	public void OnPurchaseOverdrive(bool success)
	{
		overdriveServerConfirmationPending = false;
		if (success)
		{
			GRPlayer.GetLocal().SendOverdrivePurchasedTelemetry(250, localPlayerData.coreCount);
		}
	}
}
