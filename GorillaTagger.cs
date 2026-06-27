using System;
using System.Collections;
using System.Collections.Generic;
using CjLib;
using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag.Cosmetics;
using GorillaTag.GuidedRefs;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Voice.Unity;
using Steamworks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR;

public class GorillaTagger : MonoBehaviour, IGuidedRefReceiverMono, IGuidedRefMonoBehaviour, IGuidedRefObject
{
	private struct StiltTagData
	{
		public bool isLeftHand;

		public bool hasCurrentPosition;

		public bool hasLastPosition;

		public Vector3 currentPositionForTag;

		public Vector3 lastPositionForTag;

		public bool wasTouching;

		public float lastTap;

		public float lastUpTap;

		public bool canTag;

		public bool canStun;
	}

	public enum StatusEffect
	{
		None,
		Frozen,
		Slowed,
		Dead,
		Infected,
		It
	}

	private class DebouncedBool
	{
		private readonly int _callsUntilStable;

		private int _callsSinceDisable;

		private int _callsSinceEnable;

		private bool _lastValue;

		public bool Value { get; private set; }

		public bool JustEnabled { get; private set; }

		public bool WasStablyEnabled { get; private set; }

		public DebouncedBool(int callsUntilDisable, bool initialValue = false)
		{
			_callsUntilStable = callsUntilDisable;
			Value = initialValue;
			_lastValue = initialValue;
		}

		public void Set(bool value)
		{
			_lastValue = Value;
			if (!value)
			{
				WasStablyEnabled = false;
				_callsSinceDisable++;
				if (_callsSinceDisable == _callsUntilStable)
				{
					Value = false;
				}
			}
			else
			{
				Value = true;
				_callsSinceDisable = 0;
				_callsSinceEnable++;
				if (_callsSinceEnable >= _callsUntilStable)
				{
					WasStablyEnabled = true;
				}
			}
			JustEnabled = Value && !_lastValue;
		}
	}

	[OnEnterPlay_SetNull]
	private static GorillaTagger _instance;

	[OnEnterPlay_Set(false)]
	public static bool hasInstance;

	public static float moderationMutedTime = -1f;

	public int SmoothedFramerate;

	private int _prevSmoothedFramerate;

	public int FramerateHealth;

	private int _prevFramerateHealth;

	private float _framerateHealthTimer;

	private float[] _framerateTracker = new float[30];

	private float _framerateTotal;

	private int _framerateIndex;

	private float _framerateTimer;

	private bool _forcePerfRefreshRate;

	private float _perfRefreshRate = 72f;

	private float _defaultRefreshRate = 90f;

	public bool inCosmeticsRoom;

	public SphereCollider headCollider;

	public CapsuleCollider bodyCollider;

	private Vector3 lastLeftHandPositionForTag;

	private Vector3 lastRightHandPositionForTag;

	private Vector3 lastBodyPositionForTag;

	private Vector3 lastHeadPositionForTag;

	private StiltTagData[] stiltTagData = new StiltTagData[12];

	public Transform rightHandTransform;

	public Transform leftHandTransform;

	public float hapticWaitSeconds = 0.05f;

	public float handTapVolume = 0.1f;

	public float handTapSpeed;

	public float tapCoolDown = 0.15f;

	public float lastLeftTap;

	public float lastLeftUpTap;

	public float lastRightTap;

	public float lastRightUpTap;

	private bool leftHandWasTouching;

	private bool rightHandWasTouching;

	public float tapHapticDuration = 0.05f;

	public float tapHapticStrength = 0.5f;

	public float tagHapticDuration = 0.15f;

	public float tagHapticStrength = 1f;

	public float taggedHapticDuration = 0.35f;

	public float taggedHapticStrength = 1f;

	public float taggedTime;

	public float tagCooldown;

	public float slowCooldown = 3f;

	public float maxTagDistance = 2.2f;

	public float maxStiltTagDistance = 3.2f;

	public VRRig offlineVRRig;

	[FormerlySerializedAs("offlineVRRig_guidedRef")]
	public GuidedRefReceiverFieldInfo offlineVRRig_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: false);

	public GameObject thirdPersonCamera;

	public GameObject mainCamera;

	public bool testTutorial;

	public bool disableTutorial;

	private bool _framerateUpdated;

	private bool _performanceOn;

	public GameObject leftHandTriggerCollider;

	public GameObject rightHandTriggerCollider;

	public AudioSource leftHandSlideSource;

	public AudioSource rightHandSlideSource;

	public AudioSource bodySlideSource;

	public bool overrideNotInFocus;

	private Vector3 leftRaycastSweep;

	private Vector3 leftHeadRaycastSweep;

	private Vector3 rightRaycastSweep;

	private Vector3 rightHeadRaycastSweep;

	private Vector3 headRaycastSweep;

	private Vector3 bodyRaycastSweep;

	private UnityEngine.XR.InputDevice rightDevice;

	private UnityEngine.XR.InputDevice leftDevice;

	private bool primaryButtonPressRight;

	private bool secondaryButtonPressRight;

	private bool primaryButtonPressLeft;

	private bool secondaryButtonPressLeft;

	private RaycastHit hitInfo;

	public NetPlayer otherPlayer;

	private NetPlayer tryPlayer;

	private NetPlayer touchedPlayer;

	private Vector3 topVector;

	private Vector3 bottomVector;

	private Vector3 bodyVector;

	private Vector3 dirFromHitToHand;

	private int audioClipIndex;

	private UnityEngine.XR.InputDevice inputDevice;

	private bool wasInOverlay;

	private PhotonView tempView;

	private NetPlayer tempCreator;

	private float cacheHandTapVolume;

	public StatusEffect currentStatus;

	public float statusStartTime;

	public float statusEndTime;

	private float refreshRate;

	private float baseSlideControl;

	private int gorillaTagColliderLayerMask;

	private RaycastHit[] nonAllocRaycastHits = new RaycastHit[30];

	private Collider[] colliderOverlaps = new Collider[30];

	private Dictionary<Collider, VRRig> tagRigDict = new Dictionary<Collider, VRRig>();

	private int nonAllocHits;

	private bool xrSubsystemIsActive;

	public string loadedDeviceName = "";

	private bool _forceFramerateCheck = true;

	[SerializeField]
	private int _framesForHandTrigger = 5;

	private DebouncedBool _leftHandDown;

	private DebouncedBool _rightHandDown;

	[SerializeField]
	private LayerMask BaseMirrorCameraCullingMask;

	public Watchable<int> MirrorCameraCullingMask;

	private float[] leftHapticsBuffer;

	private float[] rightHapticsBuffer;

	private Coroutine leftHapticsRoutine;

	private Coroutine rightHapticsRoutine;

	private Callback<GameOverlayActivated_t> gameOverlayActivatedCb;

	private bool isGameOverlayActive;

	private float? tagRadiusOverride;

	private int tagRadiusOverrideFrame = -1;

	public XRDisplaySubsystem activeXRDisplay;

	private static Action onPlayerSpawnedRootCallback;

	public static GorillaTagger Instance => _instance;

	public bool ForcePerfRefreshRate => _forcePerfRefreshRate;

	public NetworkView myVRRig => offlineVRRig.netView;

	internal VRRigSerializer rigSerializer => offlineVRRig.rigSerializer;

	public bool PerformanceOn => _performanceOn;

	public Rigidbody rigidbody { get; private set; }

	public float DefaultHandTapVolume => cacheHandTapVolume;

	public Recorder myRecorder { get; private set; }

	public float sphereCastRadius
	{
		get
		{
			if (!tagRadiusOverride.HasValue)
			{
				return 0.03f;
			}
			return tagRadiusOverride.Value;
		}
	}

	public bool hasTappedSurface { get; private set; }

	int IGuidedRefReceiverMono.GuidedRefsWaitingToResolveCount { get; set; }

	public event Action<bool, Vector3, Vector3> OnHandTap;

	public void SetExtraHandPosition(StiltID stiltID, Vector3 position, bool canTag, bool canStun)
	{
		stiltTagData[(int)stiltID].currentPositionForTag = position;
		stiltTagData[(int)stiltID].hasCurrentPosition = true;
		stiltTagData[(int)stiltID].canTag = canTag;
		stiltTagData[(int)stiltID].canStun = canStun;
	}

	public void ResetTappedSurfaceCheck()
	{
		hasTappedSurface = false;
	}

	public void SetTagRadiusOverrideThisFrame(float radius)
	{
		tagRadiusOverride = radius;
		tagRadiusOverrideFrame = Time.frameCount;
	}

	protected void Awake()
	{
		GuidedRefInitialize();
		RecoverMissingRefs();
		MirrorCameraCullingMask = new Watchable<int>(BaseMirrorCameraCullingMask);
		stiltTagData[0].isLeftHand = true;
		stiltTagData[4].isLeftHand = true;
		stiltTagData[5].isLeftHand = true;
		stiltTagData[2].isLeftHand = true;
		stiltTagData[6].isLeftHand = true;
		stiltTagData[7].isLeftHand = true;
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
			hasInstance = true;
			onPlayerSpawnedRootCallback?.Invoke();
		}
		GRFirstTimeUserExperience gRFirstTimeUserExperience = UnityEngine.Object.FindAnyObjectByType<GRFirstTimeUserExperience>(FindObjectsInactive.Include);
		GameObject gameObject = ((gRFirstTimeUserExperience != null) ? gRFirstTimeUserExperience.gameObject : null);
		if (!disableTutorial && (testTutorial || (PlayerPrefs.GetString("tutorial") != "done" && PlayerPrefs.GetString("didTutorial") != "done" && NetworkSystemConfig.AppVersion != "dev")))
		{
			base.transform.parent.position = new Vector3(-140f, 28f, -102f);
			base.transform.parent.eulerAngles = new Vector3(0f, 180f, 0f);
			GTPlayer.Instance.InitializeValues();
			PlayerPrefs.SetFloat("redValue", UnityEngine.Random.value);
			PlayerPrefs.SetFloat("greenValue", UnityEngine.Random.value);
			PlayerPrefs.SetFloat("blueValue", UnityEngine.Random.value);
			PlayerPrefs.Save();
		}
		else
		{
			ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
			hashtable.Add("didTutorial", true);
			PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
			PlayerPrefs.SetString("didTutorial", "done");
			PlayerPrefs.Save();
			bool flag = true;
			if (gameObject != null && PlayerPrefs.GetString("spawnInWrongStump") == "flagged" && flag)
			{
				gameObject.SetActive(value: true);
				if (gameObject.TryGetComponent<GRFirstTimeUserExperience>(out var component) && component.spawnPoint != null)
				{
					GTPlayer.Instance.TeleportTo(component.spawnPoint.position, component.spawnPoint.rotation);
					GTPlayer.Instance.InitializeValues();
					PlayerPrefs.DeleteKey("spawnInWrongStump");
					PlayerPrefs.Save();
				}
			}
		}
		thirdPersonCamera.SetActive(Application.platform != RuntimePlatform.Android);
		inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		wasInOverlay = false;
		baseSlideControl = GTPlayer.Instance.slideControl;
		gorillaTagColliderLayerMask = UnityLayer.GorillaTagCollider.ToLayerMask();
		rigidbody = GetComponent<Rigidbody>();
		cacheHandTapVolume = handTapVolume;
		OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.Medium;
		_leftHandDown = new DebouncedBool(_framesForHandTrigger);
		_rightHandDown = new DebouncedBool(_framesForHandTrigger);
		ClearFramerateTracker();
	}

	protected void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
			hasInstance = false;
		}
	}

	private async void IsXRSubsystemActive()
	{
		loadedDeviceName = XRSettings.loadedDeviceName;
		while (!xrSubsystemIsActive)
		{
			List<XRDisplaySubsystem> list = new List<XRDisplaySubsystem>();
			SubsystemManager.GetSubsystems(list);
			foreach (XRDisplaySubsystem item in list)
			{
				if (item.running)
				{
					xrSubsystemIsActive = true;
					activeXRDisplay = item;
					return;
				}
			}
			await Awaitable.WaitForSecondsAsync(0.1f);
		}
	}

	public bool IsOculusQuest2()
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			return OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Quest_2;
		}
		return false;
	}

	protected void Start()
	{
		IsXRSubsystemActive();
		if (loadedDeviceName == "OpenVR Display")
		{
			Quaternion rotation = Quaternion.Euler(new Vector3(-90f, 180f, -20f));
			Quaternion rotation2 = Quaternion.Euler(new Vector3(-90f, 180f, 20f));
			Quaternion quaternion = Quaternion.Euler(new Vector3(-141f, 204f, -27f));
			Quaternion quaternion2 = Quaternion.Euler(new Vector3(-141f, 156f, 27f));
			GTPlayer.Instance.SetHandOffsets(isLeftHand: true, new Vector3(-0.02f, 0f, -0.07f), quaternion * Quaternion.Inverse(rotation));
			GTPlayer.Instance.SetHandOffsets(isLeftHand: false, new Vector3(0.02f, 0f, -0.07f), quaternion2 * Quaternion.Inverse(rotation2));
		}
		bodyVector = new Vector3(0f, bodyCollider.height / 2f - bodyCollider.radius, 0f);
		if (SteamManager.Initialized)
		{
			gameOverlayActivatedCb = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
		}
	}

	private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
	{
		isGameOverlayActive = pCallback.m_bActive != 0;
	}

	[ContextMenu("Toggle Performance Refresh Rate")]
	public void ToggleForcedPerformanceRefresh()
	{
		SetForcedRefreshRate(forcePerf: true, 72f);
	}

	public void ToggleDefaultPerformanceRefresh()
	{
		SetForcedRefreshRate(forcePerf: false, _defaultRefreshRate);
	}

	public void ToggleForcedRefreshRate(float newRefreshRate = 90f)
	{
		SetForcedRefreshRate(!_forcePerfRefreshRate, newRefreshRate);
	}

	public void SetForcedRefreshRate(bool forcePerf, float newRefreshRate = 90f)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			Debug.Log($"GorillaTagger - SetForcedRefreshRate - {forcePerf} / {newRefreshRate}");
			_framerateUpdated = false;
			_forceFramerateCheck = true;
			_forcePerfRefreshRate = forcePerf;
			_perfRefreshRate = Mathf.Clamp(newRefreshRate, 32f, 144f);
			_performanceOn = newRefreshRate <= 72f;
			Debug.Log($"GorillaTagger - SetForcedRefreshRate - New refresh {_perfRefreshRate} with perf {_performanceOn}");
			UpdateResolutionScale(_performanceOn);
			if (forcePerf)
			{
				DebugHudStats.FPS_THRESHOLD = (int)_perfRefreshRate - 1;
			}
			else
			{
				DebugHudStats.FPS_THRESHOLD = (int)_defaultRefreshRate - 1;
			}
			Debug.Log($"GorillaTagger - SetForcedRefreshRate - New DebugHudStats FPS threshold {DebugHudStats.FPS_THRESHOLD}");
		}
	}

	private void ClearFramerateTracker()
	{
		_framerateIndex = 0;
		_framerateTotal = 0f;
		for (int i = 0; i < _framerateTracker.Length; i++)
		{
			_framerateTracker[i] = 0f;
		}
	}

	private void UpdateResolutionScale(bool performanceMode)
	{
		float num = 1f;
		if (performanceMode)
		{
			num = 0.975f;
			if (Application.platform == RuntimePlatform.Android)
			{
				num = 0.95f;
				if (OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Quest_2)
				{
					num = 0.9f;
				}
			}
		}
		else if (Application.platform == RuntimePlatform.Android)
		{
			num = 0.975f;
			if (OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Quest_2)
			{
				num = 0.925f;
			}
		}
		XRSettings.eyeTextureResolutionScale = num;
		XRSettings.renderViewportScale = num;
		Debug.Log($"GorillaTagger - UpdateResolutionScale - {num}");
	}

	protected void LateUpdate()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (isGameOverlayActive)
		{
			if (leftHandTriggerCollider.activeSelf)
			{
				leftHandTriggerCollider.SetActive(value: false);
				rightHandTriggerCollider.SetActive(value: true);
			}
			GTPlayer.Instance.inOverlay = true;
		}
		else
		{
			if (!leftHandTriggerCollider.activeSelf)
			{
				leftHandTriggerCollider.SetActive(value: true);
				rightHandTriggerCollider.SetActive(value: true);
			}
			GTPlayer.Instance.inOverlay = false;
		}
		_framerateTimer -= Time.deltaTime;
		if (_framerateTimer <= 0f)
		{
			_framerateTimer += 0.1f;
			if (Time.smoothDeltaTime > 0f)
			{
				float num = 1f / Time.smoothDeltaTime;
				_framerateTotal -= _framerateTracker[_framerateIndex];
				_framerateTracker[_framerateIndex] = num;
				_framerateTotal += num;
				_framerateIndex++;
				if (_framerateIndex >= _framerateTracker.Length)
				{
					_framerateIndex = 0;
				}
				_prevSmoothedFramerate = SmoothedFramerate;
				SmoothedFramerate = Mathf.RoundToInt(_framerateTotal / (float)_framerateTracker.Length);
				_ = SmoothedFramerate;
				_ = DebugHudStats.FPS_THRESHOLD;
			}
		}
		if (xrSubsystemIsActive && Application.platform != RuntimePlatform.Android && activeXRDisplay != null && activeXRDisplay.TryGetDisplayRefreshRate(out _defaultRefreshRate))
		{
			float num2 = (_forcePerfRefreshRate ? _perfRefreshRate : _defaultRefreshRate);
			float num3 = 1f / num2;
			if (num2 > 0f)
			{
				DebugHudStats.FPS_THRESHOLD = (int)num2 - 1;
			}
			if (_forceFramerateCheck || Mathf.Abs(Time.fixedDeltaTime - num3) > 0.0001f)
			{
				_forceFramerateCheck = false;
				Debug.Log(" =========== Adjusting refresh size =========");
				Debug.Log(" fixedDeltaTime before:\t" + Time.fixedDeltaTime);
				Debug.Log(" Refresh rate         :\t" + num2);
				Time.fixedDeltaTime = num3;
				UpdateResolutionScale(num2 < _defaultRefreshRate);
				Debug.Log(" fixedDeltaTime after :\t" + Time.fixedDeltaTime);
				Debug.Log(" History size before  :\t" + GTPlayer.Instance.velocityHistorySize);
				GTPlayer.Instance.velocityHistorySize = Mathf.Max(Mathf.Min(Mathf.FloorToInt(num2 * (1f / 12f)), 10), 6);
				if (GTPlayer.Instance.velocityHistorySize > 9)
				{
					GTPlayer.Instance.velocityHistorySize--;
				}
				Debug.Log("New history size: " + GTPlayer.Instance.velocityHistorySize);
				Debug.Log(" ============================================");
				GTPlayer.Instance.slideControl = 1f - CalcSlideControl(num2);
				GTPlayer.Instance.InitializeValues();
			}
		}
		else if (Application.platform != RuntimePlatform.Android && OVRManager.instance != null && OVRManager.OVRManagerinitialized && OVRManager.instance.gameObject != null && OVRManager.instance.gameObject.activeSelf)
		{
			UnityEngine.Object.Destroy(OVRManager.instance.gameObject);
		}
		else if ((_forceFramerateCheck && OVRManager.instance != null) || (!_framerateUpdated && Application.platform == RuntimePlatform.Android && OVRManager.instance.gameObject.activeSelf))
		{
			InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
			int num4 = OVRManager.display.displayFrequenciesAvailable.Length - 1;
			float num5 = OVRManager.display.displayFrequenciesAvailable[num4];
			float systemDisplayFrequency = OVRPlugin.systemDisplayFrequency;
			while (num5 > 90f)
			{
				num4--;
				if (num4 < 0)
				{
					break;
				}
				num5 = OVRManager.display.displayFrequenciesAvailable[num4];
			}
			_defaultRefreshRate = num5;
			if (_forcePerfRefreshRate)
			{
				num5 = _perfRefreshRate;
			}
			float num6 = 1f;
			float num7 = 1f / num5;
			if (_forceFramerateCheck || Mathf.Abs(Time.fixedDeltaTime - num7 * num6) > 0.0001f)
			{
				_forceFramerateCheck = false;
				float num8 = Time.fixedDeltaTime - num7 * num6;
				Debug.Log(" =========== ADJUSTING REFRESH SIZE ========= ");
				Debug.Log($"!!!! Time.fixedDeltaTime - (1f / newRefreshRate) * {num6}) {num8}");
				Debug.Log($"Old Refresh rate: {systemDisplayFrequency}");
				Debug.Log($"New Refresh rate: {num5}");
				Debug.Log($"   fixedDeltaTime before:\t{Time.fixedDeltaTime}");
				Debug.Log($"   fixedDeltaTime after :\t{num7}");
				Application.targetFrameRate = (int)num5;
				Time.fixedDeltaTime = num7 * num6;
				OVRPlugin.systemDisplayFrequency = num5;
				UpdateResolutionScale(num5 <= 72f);
				GTPlayer.Instance.velocityHistorySize = Mathf.FloorToInt(num5 * (1f / 12f));
				if (GTPlayer.Instance.velocityHistorySize > 9)
				{
					GTPlayer.Instance.velocityHistorySize--;
				}
				Debug.Log($"   FixedDeltaTime after :\t{Time.fixedDeltaTime}");
				Debug.Log($"   History size before  :\t{GTPlayer.Instance.velocityHistorySize}");
				Debug.Log($"New history size: {GTPlayer.Instance.velocityHistorySize}");
				Debug.Log(" ============================================ ");
				GTPlayer.Instance.slideControl = 1f - CalcSlideControl(XRDevice.refreshRate);
				GTPlayer.Instance.InitializeValues();
				OVRManager.instance.gameObject.SetActive(value: false);
				_framerateUpdated = true;
				ConfirmUpdatedFrameRate();
			}
		}
		else if (!xrSubsystemIsActive && Application.platform != RuntimePlatform.Android)
		{
			_defaultRefreshRate = 144f;
			int num9 = (_forcePerfRefreshRate ? ((int)_perfRefreshRate) : ((int)_defaultRefreshRate));
			float num10 = 1f / (float)num9;
			if (_forceFramerateCheck || Mathf.Abs(Time.fixedDeltaTime - num10) > 0.0001f)
			{
				_forceFramerateCheck = false;
				Debug.Log($"Updating delta time. Was: {Time.fixedDeltaTime}. Now it's {num10} at framerate {num9}.");
				Application.targetFrameRate = num9;
				Time.fixedDeltaTime = num10;
				UpdateResolutionScale((float)num9 < _defaultRefreshRate);
				GTPlayer.Instance.velocityHistorySize = Mathf.Min(Mathf.FloorToInt((float)num9 * (1f / 12f)), 10);
				if (GTPlayer.Instance.velocityHistorySize > 9)
				{
					GTPlayer.Instance.velocityHistorySize--;
				}
				Debug.Log($"New history size: {GTPlayer.Instance.velocityHistorySize}");
				GTPlayer.Instance.slideControl = 1f - CalcSlideControl(num9);
				GTPlayer.Instance.InitializeValues();
			}
		}
		otherPlayer = null;
		touchedPlayer = null;
		NetPlayer otherTouchedPlayer = null;
		if (tagRadiusOverrideFrame < Time.frameCount)
		{
			tagRadiusOverride = null;
		}
		Vector3 position = leftHandTransform.position;
		Vector3 position2 = rightHandTransform.position;
		Vector3 position3 = headCollider.transform.position;
		Vector3 position4 = bodyCollider.transform.position;
		float scale = GTPlayer.Instance.scale;
		float num11 = sphereCastRadius * scale;
		bool bodyHit = false;
		bool leftHandHit = false;
		bool canTagHit = false;
		bool canStunHit = false;
		if (!(GorillaGameManager.instance is CasualGameMode))
		{
			nonAllocHits = Physics.OverlapCapsuleNonAlloc(lastLeftHandPositionForTag, position, num11, colliderOverlaps, gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
			TryTaggingAllHitsOverlap(isLeftHand: true, maxTagDistance);
			nonAllocHits = Physics.OverlapCapsuleNonAlloc(position3, position, num11, colliderOverlaps, gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
			TryTaggingAllHitsOverlap(isLeftHand: true, maxTagDistance);
			nonAllocHits = Physics.OverlapCapsuleNonAlloc(lastRightHandPositionForTag, position2, num11, colliderOverlaps, gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
			TryTaggingAllHitsOverlap(isLeftHand: false, maxTagDistance);
			nonAllocHits = Physics.OverlapCapsuleNonAlloc(position3, position2, num11, colliderOverlaps, gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
			TryTaggingAllHitsOverlap(isLeftHand: false, maxTagDistance);
			for (int i = 0; i < 12; i++)
			{
				StiltTagData stiltTagData = this.stiltTagData[i];
				if (stiltTagData.hasLastPosition && stiltTagData.hasCurrentPosition && (stiltTagData.canTag || stiltTagData.canStun))
				{
					nonAllocHits = Physics.OverlapCapsuleNonAlloc(stiltTagData.currentPositionForTag, stiltTagData.lastPositionForTag, num11, colliderOverlaps, gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
					TryTaggingAllHitsOverlap(i == 0 || i == 2, maxStiltTagDistance, stiltTagData.canTag, stiltTagData.canStun);
				}
			}
			topVector = lastHeadPositionForTag;
			bottomVector = lastBodyPositionForTag - bodyVector;
			nonAllocHits = Physics.CapsuleCastNonAlloc(topVector, bottomVector, bodyCollider.radius * 2f * GTPlayer.Instance.scale, bodyRaycastSweep.normalized, nonAllocRaycastHits, Mathf.Max(bodyRaycastSweep.magnitude, num11), gorillaTagColliderLayerMask, QueryTriggerInteraction.Collide);
			TryTaggingAllHitsCapsulecast(maxTagDistance);
		}
		if (otherPlayer != null)
		{
			if (canTagHit && (!canStunHit || GorillaGameManager.instance.LocalCanTag(NetworkSystem.Instance.LocalPlayer, otherPlayer)))
			{
				GameMode.ActiveGameMode.LocalTag(otherPlayer, NetworkSystem.Instance.LocalPlayer, bodyHit, leftHandHit);
				GameMode.ReportTag(otherPlayer);
			}
			if (canStunHit)
			{
				RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.TaggedTime, otherPlayer);
			}
		}
		if (otherTouchedPlayer != null && GorillaGameManager.instance != null)
		{
			CustomGameMode.TouchPlayer(otherTouchedPlayer);
		}
		if (otherTouchedPlayer != null)
		{
			HitWithKnockBack(otherTouchedPlayer, NetworkSystem.Instance.LocalPlayer, leftHandHit);
		}
		ProcessHandTapping(true, StiltID.None, ref lastLeftTap, ref lastLeftUpTap, ref leftHandWasTouching, in leftHandSlideSource);
		ProcessHandTapping(false, StiltID.None, ref lastRightTap, ref lastRightUpTap, ref rightHandWasTouching, in rightHandSlideSource);
		for (int j = 0; j < 12; j++)
		{
			StiltTagData stiltTagData2 = this.stiltTagData[j];
			if (stiltTagData2.hasLastPosition && stiltTagData2.hasCurrentPosition)
			{
				ref bool isLeftHand = ref stiltTagData2.isLeftHand;
				StiltID stiltID = (StiltID)j;
				ProcessHandTapping(in isLeftHand, in stiltID, ref stiltTagData2.lastTap, ref stiltTagData2.lastUpTap, ref stiltTagData2.wasTouching, in leftHandSlideSource);
				this.stiltTagData[j] = stiltTagData2;
			}
		}
		CheckEndStatusEffect();
		lastLeftHandPositionForTag = position;
		lastRightHandPositionForTag = position2;
		lastBodyPositionForTag = position4;
		lastHeadPositionForTag = position3;
		for (int k = 0; k < 12; k++)
		{
			StiltTagData stiltTagData3 = this.stiltTagData[k];
			if (stiltTagData3.hasLastPosition || stiltTagData3.hasCurrentPosition)
			{
				stiltTagData3.lastPositionForTag = stiltTagData3.currentPositionForTag;
				stiltTagData3.hasLastPosition = stiltTagData3.hasCurrentPosition;
				stiltTagData3.hasCurrentPosition = false;
				this.stiltTagData[k] = stiltTagData3;
			}
		}
		if (GTPlayer.Instance.IsBodySliding && (double)GTPlayer.Instance.RigidbodyVelocity.magnitude >= 0.15)
		{
			if (!bodySlideSource.isPlaying)
			{
				bodySlideSource.Play();
			}
		}
		else
		{
			bodySlideSource.Stop();
		}
		if (GorillaComputer.instance == null || NetworkSystem.Instance.LocalRecorder == null)
		{
			return;
		}
		if (float.IsFinite(moderationMutedTime) && moderationMutedTime >= 0f)
		{
			moderationMutedTime -= Time.deltaTime;
		}
		if (GorillaComputer.instance.voiceChatOn == "TRUE")
		{
			myRecorder = NetworkSystem.Instance.LocalRecorder;
			if (offlineVRRig.remoteUseReplacementVoice)
			{
				offlineVRRig.remoteUseReplacementVoice = false;
			}
			if (moderationMutedTime > 0f)
			{
				myRecorder.TransmitEnabled = false;
			}
			if (GorillaComputer.instance.pttType != "OPEN MIC")
			{
				primaryButtonPressRight = false;
				secondaryButtonPressRight = false;
				primaryButtonPressLeft = false;
				secondaryButtonPressLeft = false;
				primaryButtonPressRight = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand);
				secondaryButtonPressRight = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
				primaryButtonPressLeft = ControllerInputPoller.PrimaryButtonPress(XRNode.LeftHand);
				secondaryButtonPressLeft = ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
				if (primaryButtonPressRight || secondaryButtonPressRight || primaryButtonPressLeft || secondaryButtonPressLeft)
				{
					if (GorillaComputer.instance.pttType == "PUSH TO MUTE")
					{
						offlineVRRig.shouldSendSpeakingLoudness = false;
						_ = myRecorder.TransmitEnabled;
						myRecorder.TransmitEnabled = false;
					}
					else if (GorillaComputer.instance.pttType == "PUSH TO TALK")
					{
						offlineVRRig.shouldSendSpeakingLoudness = true;
						if (moderationMutedTime <= 0f && !myRecorder.TransmitEnabled)
						{
							myRecorder.TransmitEnabled = true;
						}
					}
				}
				else if (GorillaComputer.instance.pttType == "PUSH TO MUTE")
				{
					offlineVRRig.shouldSendSpeakingLoudness = true;
					if (moderationMutedTime <= 0f && !myRecorder.TransmitEnabled)
					{
						myRecorder.TransmitEnabled = true;
					}
				}
				else if (GorillaComputer.instance.pttType == "PUSH TO TALK")
				{
					offlineVRRig.shouldSendSpeakingLoudness = false;
					_ = myRecorder.TransmitEnabled;
					myRecorder.TransmitEnabled = false;
				}
			}
			else
			{
				if (moderationMutedTime <= 0f && !myRecorder.TransmitEnabled)
				{
					myRecorder.TransmitEnabled = true;
				}
				if (!offlineVRRig.shouldSendSpeakingLoudness)
				{
					offlineVRRig.shouldSendSpeakingLoudness = true;
				}
			}
		}
		else if (GorillaComputer.instance.voiceChatOn == "FALSE")
		{
			myRecorder = NetworkSystem.Instance.LocalRecorder;
			if (!offlineVRRig.remoteUseReplacementVoice)
			{
				offlineVRRig.remoteUseReplacementVoice = true;
			}
			if (myRecorder.TransmitEnabled)
			{
				myRecorder.TransmitEnabled = false;
			}
			if (GorillaComputer.instance.pttType != "OPEN MIC")
			{
				primaryButtonPressRight = false;
				secondaryButtonPressRight = false;
				primaryButtonPressLeft = false;
				secondaryButtonPressLeft = false;
				primaryButtonPressRight = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand);
				secondaryButtonPressRight = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
				primaryButtonPressLeft = ControllerInputPoller.PrimaryButtonPress(XRNode.LeftHand);
				secondaryButtonPressLeft = ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
				if (primaryButtonPressRight || secondaryButtonPressRight || primaryButtonPressLeft || secondaryButtonPressLeft)
				{
					if (GorillaComputer.instance.pttType == "PUSH TO MUTE")
					{
						offlineVRRig.shouldSendSpeakingLoudness = false;
					}
					else if (GorillaComputer.instance.pttType == "PUSH TO TALK")
					{
						offlineVRRig.shouldSendSpeakingLoudness = true;
					}
				}
				else if (GorillaComputer.instance.pttType == "PUSH TO MUTE")
				{
					offlineVRRig.shouldSendSpeakingLoudness = true;
				}
				else if (GorillaComputer.instance.pttType == "PUSH TO TALK")
				{
					offlineVRRig.shouldSendSpeakingLoudness = false;
				}
			}
			else if (!offlineVRRig.shouldSendSpeakingLoudness)
			{
				offlineVRRig.shouldSendSpeakingLoudness = true;
			}
		}
		else
		{
			myRecorder = NetworkSystem.Instance.LocalRecorder;
			if (offlineVRRig.remoteUseReplacementVoice)
			{
				offlineVRRig.remoteUseReplacementVoice = false;
			}
			if (offlineVRRig.shouldSendSpeakingLoudness)
			{
				offlineVRRig.shouldSendSpeakingLoudness = false;
			}
			if (myRecorder.TransmitEnabled)
			{
				myRecorder.TransmitEnabled = false;
			}
		}
		void TryTaggingAllHitsCapsulecast(float maxTagDistance, bool canTag = true, bool canStun = false)
		{
			for (int l = 0; l < nonAllocHits; l++)
			{
				if (nonAllocRaycastHits[l].collider.gameObject.activeSelf && (!tagRigDict.TryGetValue(nonAllocRaycastHits[l].collider, out var value) || !(value == VRRig.LocalRig)))
				{
					if (TryToTag(nonAllocRaycastHits[l].collider, isBodyTag: false, canStun, maxTagDistance, out tryPlayer, out touchedPlayer))
					{
						otherPlayer = tryPlayer;
						bodyHit = true;
						canTagHit = canTag;
						canStunHit = canStun;
						break;
					}
					if (touchedPlayer != null)
					{
						otherTouchedPlayer = touchedPlayer;
					}
				}
			}
		}
		void TryTaggingAllHitsOverlap(bool flag, float maxTagDistance, bool canTag = true, bool canStun = false)
		{
			for (int l = 0; l < nonAllocHits; l++)
			{
				if (colliderOverlaps[l].gameObject.activeSelf && (!tagRigDict.TryGetValue(colliderOverlaps[l], out var value) || !(value == VRRig.LocalRig)))
				{
					if (TryToTag(colliderOverlaps[l], isBodyTag: true, canStun, maxTagDistance, out tryPlayer, out touchedPlayer))
					{
						otherPlayer = tryPlayer;
						bodyHit = false;
						leftHandHit = flag;
						canTagHit = canTag;
						canStunHit = canStun;
						break;
					}
					if (touchedPlayer != null)
					{
						otherTouchedPlayer = touchedPlayer;
					}
				}
			}
		}
	}

	private bool TryToTag(VRRig rig, Vector3 hitObjectPos, bool isBodyTag, bool canStun, float maxTagDistance, out NetPlayer taggedPlayer, out NetPlayer touchedPlayer)
	{
		taggedPlayer = null;
		touchedPlayer = null;
		if (NetworkSystem.Instance.InRoom)
		{
			tempCreator = rig?.creator;
			if (tempCreator != null && NetworkSystem.Instance.LocalPlayer != tempCreator)
			{
				touchedPlayer = tempCreator;
				if (GorillaGameManager.instance != null && Time.time > taggedTime + tagCooldown && (canStun || GorillaGameManager.instance.LocalCanTag(NetworkSystem.Instance.LocalPlayer, tempCreator)) && (headCollider.transform.position - hitObjectPos).sqrMagnitude < maxTagDistance * maxTagDistance * GTPlayer.Instance.scale)
				{
					if (!isBodyTag)
					{
						StartVibration(((leftHandTransform.position - hitObjectPos).magnitude < (rightHandTransform.position - hitObjectPos).magnitude) ? true : false, tagHapticStrength, tagHapticDuration);
					}
					else
					{
						StartVibration(forLeftController: true, tagHapticStrength, tagHapticDuration);
						StartVibration(forLeftController: false, tagHapticStrength, tagHapticDuration);
					}
					taggedPlayer = tempCreator;
					return true;
				}
			}
		}
		return false;
	}

	private bool TryToTag(Collider hitCollider, bool isBodyTag, bool canStun, float maxTagDistance, out NetPlayer taggedPlayer, out NetPlayer touchedNetPlayer)
	{
		if (!tagRigDict.TryGetValue(hitCollider, out var value))
		{
			value = hitCollider.GetComponentInParent<VRRig>();
			tagRigDict.Add(hitCollider, value);
		}
		if (value == null)
		{
			PropHuntTaggableProp componentInParent = hitCollider.GetComponentInParent<PropHuntTaggableProp>();
			if (!(componentInParent != null))
			{
				taggedPlayer = null;
				touchedNetPlayer = null;
				return false;
			}
			value = componentInParent.ownerRig;
		}
		else if (GorillaGameManager.instance != null && GorillaGameManager.instance.GameType() == GameModeType.PropHunt)
		{
			taggedPlayer = null;
			touchedNetPlayer = null;
			return false;
		}
		return TryToTag(value, hitCollider.transform.position, isBodyTag, canStun, maxTagDistance, out taggedPlayer, out touchedNetPlayer);
	}

	private void HitWithKnockBack(NetPlayer taggedPlayer, NetPlayer taggingPlayer, bool leftHand)
	{
		Vector3 averageVelocity = GTPlayer.Instance.GetHandVelocityTracker(leftHand).GetAverageVelocity(worldSpace: true);
		if (VRRigCache.Instance.TryGetVrrig(taggingPlayer, out var playerRig))
		{
			VRMap vRMap = (leftHand ? playerRig.Rig.leftHand : playerRig.Rig.rightHand);
			Vector3 vector = (leftHand ? (-vRMap.rigTarget.right) : vRMap.rigTarget.right);
			if (VRRigCache.Instance.TryGetVrrig(taggedPlayer, out var playerRig2) && playerRig2.Rig.TemporaryCosmeticEffects.TryGetValue(CosmeticEffectsOnPlayers.EFFECTTYPE.TagWithKnockback, out var _))
			{
				RoomSystem.HitPlayer(taggedPlayer, vector.normalized, averageVelocity.magnitude);
			}
		}
	}

	public void StartVibration(bool forLeftController, float amplitude, float duration)
	{
		StartCoroutine(HapticPulses(forLeftController, amplitude, duration));
	}

	private IEnumerator HapticPulses(bool forLeftController, float amplitude, float duration)
	{
		float startTime = Time.time;
		uint channel = 0u;
		UnityEngine.XR.InputDevice device = ((!forLeftController) ? ControllerInputPoller.instance.rightControllerDevice : ControllerInputPoller.instance.leftControllerDevice);
		while (Time.time < startTime + duration)
		{
			device.SendHapticImpulse(channel, amplitude, hapticWaitSeconds);
			yield return new WaitForSeconds(hapticWaitSeconds * 0.9f);
		}
	}

	public void PlayHapticClip(bool forLeftController, AudioClip clip, float strength)
	{
		if (forLeftController)
		{
			if (leftHapticsRoutine != null)
			{
				StopCoroutine(leftHapticsRoutine);
			}
			leftHapticsRoutine = StartCoroutine(AudioClipHapticPulses(forLeftController, clip, strength));
		}
		else
		{
			if (rightHapticsRoutine != null)
			{
				StopCoroutine(rightHapticsRoutine);
			}
			rightHapticsRoutine = StartCoroutine(AudioClipHapticPulses(forLeftController, clip, strength));
		}
	}

	public void StopHapticClip(bool forLeftController)
	{
		if (forLeftController)
		{
			if (leftHapticsRoutine != null)
			{
				StopCoroutine(leftHapticsRoutine);
				leftHapticsRoutine = null;
			}
		}
		else if (rightHapticsRoutine != null)
		{
			StopCoroutine(rightHapticsRoutine);
			rightHapticsRoutine = null;
		}
	}

	private IEnumerator AudioClipHapticPulses(bool forLeftController, AudioClip clip, float strength)
	{
		uint channel = 0u;
		int bufferSize = 8192;
		int sampleWindowSize = 256;
		float[] audioData;
		UnityEngine.XR.InputDevice device;
		if (forLeftController)
		{
			audioData = leftHapticsBuffer ?? (leftHapticsBuffer = new float[bufferSize]);
			device = ControllerInputPoller.instance.leftControllerDevice;
		}
		else
		{
			audioData = rightHapticsBuffer ?? (rightHapticsBuffer = new float[bufferSize]);
			device = ControllerInputPoller.instance.rightControllerDevice;
		}
		int sampleOffset = -bufferSize;
		float startTime = Time.time;
		float length = clip.length;
		float endTime = Time.time + length;
		float sampleRate = clip.samples;
		while (Time.time <= endTime)
		{
			float num = (Time.time - startTime) / length;
			int num2 = (int)(sampleRate * num);
			if (Mathf.Max(num2 + sampleWindowSize - 1, audioData.Length - 1) >= sampleOffset + bufferSize)
			{
				clip.GetData(audioData, num2);
				sampleOffset = num2;
			}
			float num3 = 0f;
			int num4 = Mathf.Min(clip.samples - num2, sampleWindowSize);
			for (int i = 0; i < num4; i++)
			{
				float num5 = audioData[num2 - sampleOffset + i];
				num3 += num5 * num5;
			}
			float amplitude = Mathf.Clamp01(((num4 > 0) ? Mathf.Sqrt(num3 / (float)num4) : 0f) * strength);
			device.SendHapticImpulse(channel, amplitude, Time.fixedDeltaTime);
			yield return null;
		}
		if (forLeftController)
		{
			leftHapticsRoutine = null;
		}
		else
		{
			rightHapticsRoutine = null;
		}
	}

	public void DoVibration(XRNode node, float amplitude, float duration)
	{
		UnityEngine.XR.InputDevice deviceAtXRNode = InputDevices.GetDeviceAtXRNode(node);
		if (deviceAtXRNode.isValid)
		{
			deviceAtXRNode.SendHapticImpulse(0u, amplitude, duration);
		}
	}

	public void UpdateColor(float red, float green, float blue)
	{
		offlineVRRig.InitializeNoobMaterialLocal(red, green, blue);
		if (NetworkSystem.Instance != null && !NetworkSystem.Instance.InRoom)
		{
			offlineVRRig.bodyRenderer.ResetBodyMaterial();
		}
	}

	protected void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent<GorillaTriggerBox>(out var component))
		{
			component.OnBoxTriggered();
		}
	}

	protected void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent<GorillaTriggerBox>(out var component))
		{
			component.OnBoxExited();
		}
	}

	public void ShowCosmeticParticles(bool showParticles)
	{
		if (showParticles)
		{
			mainCamera.GetComponent<Camera>().cullingMask |= UnityLayer.GorillaCosmeticParticle.ToLayerMask();
			MirrorCameraCullingMask.value |= UnityLayer.GorillaCosmeticParticle.ToLayerMask();
		}
		else
		{
			mainCamera.GetComponent<Camera>().cullingMask &= ~UnityLayer.GorillaCosmeticParticle.ToLayerMask();
			MirrorCameraCullingMask.value &= ~UnityLayer.GorillaCosmeticParticle.ToLayerMask();
		}
	}

	public void ApplyStatusEffect(StatusEffect newStatus, float duration)
	{
		EndStatusEffect(currentStatus);
		currentStatus = newStatus;
		statusEndTime = Time.time + duration;
		switch (newStatus)
		{
		case StatusEffect.Frozen:
			GTPlayer.Instance.disableMovement = true;
			break;
		case StatusEffect.None:
		case StatusEffect.Slowed:
			break;
		}
	}

	private void CheckEndStatusEffect()
	{
		if (Time.time > statusEndTime)
		{
			EndStatusEffect(currentStatus);
		}
	}

	private void EndStatusEffect(StatusEffect effectToEnd)
	{
		switch (effectToEnd)
		{
		case StatusEffect.Frozen:
			GTPlayer.Instance.disableMovement = false;
			currentStatus = StatusEffect.None;
			break;
		case StatusEffect.Slowed:
			currentStatus = StatusEffect.None;
			break;
		case StatusEffect.None:
			break;
		}
	}

	private float CalcSlideControl(float fps)
	{
		return Mathf.Pow(Mathf.Pow(1f - baseSlideControl, 120f), 1f / fps);
	}

	public static void OnPlayerSpawned(Action action)
	{
		if ((bool)_instance)
		{
			action();
		}
		else
		{
			onPlayerSpawnedRootCallback = (Action)Delegate.Combine(onPlayerSpawnedRootCallback, action);
		}
	}

	private void ProcessHandTapping(in bool isLeftHand, in StiltID stiltID, ref float lastTapTime, ref float lastTapUpTime, ref bool wasHandTouching, in AudioSource handSlideSource)
	{
		GTPlayer.Instance.GetHandTapData(isLeftHand, stiltID, out var wasHandTouching2, out var wasSliding, out var handMatIndex, out var surfaceOverride, out var handHitInfo, out var handPosition, out var handVelocityTracker);
		DebouncedBool debouncedBool = (isLeftHand ? _leftHandDown : _rightHandDown);
		if (GTPlayer.Instance.inOverlay)
		{
			handSlideSource.GTStop();
			return;
		}
		if (wasSliding)
		{
			StartVibration(isLeftHand, tapHapticStrength / 5f, Time.fixedDeltaTime);
			if (!handSlideSource.isPlaying)
			{
				handSlideSource.GTPlay();
			}
			return;
		}
		handSlideSource.GTStop();
		bool wasStablyEnabled = debouncedBool.WasStablyEnabled;
		debouncedBool.Set(wasHandTouching2);
		bool flag = !wasHandTouching && wasHandTouching2 && debouncedBool.JustEnabled;
		bool flag2 = wasHandTouching && !wasHandTouching2 && wasStablyEnabled;
		wasHandTouching = wasHandTouching2;
		if (!flag2 && !flag)
		{
			return;
		}
		Tappable component = null;
		bool flag3 = surfaceOverride != null && surfaceOverride.TryGetComponent<Tappable>(out component);
		HandEffectContext handEffect = offlineVRRig.GetHandEffect(isLeftHand, stiltID);
		if ((!flag3 || !component.overrideTapCooldown) && (!(handEffect.SeparateUpTapCooldown && flag2) || !(Time.time > lastTapUpTime + tapCoolDown)) && (!flag || !(Time.time > lastTapTime + tapCoolDown)))
		{
			return;
		}
		float sqrMagnitude = (handVelocityTracker.GetAverageVelocity(worldSpace: true, 0.03f) / GTPlayer.Instance.scale).sqrMagnitude;
		float sqrMagnitude2 = handVelocityTracker.GetAverageVelocity(worldSpace: false, 0.03f).sqrMagnitude;
		handTapSpeed = Mathf.Sqrt(Mathf.Max(sqrMagnitude, sqrMagnitude2));
		if (handEffect.SeparateUpTapCooldown && flag2)
		{
			lastTapUpTime = Time.time;
		}
		else
		{
			lastTapTime = Time.time;
		}
		dirFromHitToHand = Vector3.Normalize(handHitInfo.point - handPosition);
		if (GameMode.ActiveGameMode is GorillaAmbushManager gorillaAmbushManager && gorillaAmbushManager.IsInfected(NetworkSystem.Instance.LocalPlayer))
		{
			handTapVolume = Mathf.Clamp(handTapSpeed, 0f, gorillaAmbushManager.crawlingSpeedForMaxVolume);
		}
		else
		{
			handTapVolume = cacheHandTapVolume;
		}
		if (GameMode.ActiveGameMode is GorillaFreezeTagManager gorillaFreezeTagManager && gorillaFreezeTagManager.IsFrozen(NetworkSystem.Instance.LocalPlayer))
		{
			audioClipIndex = gorillaFreezeTagManager.GetFrozenHandTapAudioIndex();
		}
		else if (surfaceOverride != null)
		{
			audioClipIndex = surfaceOverride.overrideIndex;
		}
		else
		{
			audioClipIndex = handMatIndex;
		}
		if (surfaceOverride != null)
		{
			if (surfaceOverride.sendOnTapEvent)
			{
				IBuilderTappable component2;
				if (flag3)
				{
					component.OnTap(handTapVolume);
				}
				else if (surfaceOverride.TryGetComponent<IBuilderTappable>(out component2))
				{
					component2.OnTapLocal(handTapVolume);
				}
			}
			PlayerGameEvents.TapObject(surfaceOverride.name);
		}
		Vector3 averageVelocity = handVelocityTracker.GetAverageVelocity(worldSpace: true, 0.03f);
		if (GameMode.ActiveGameMode != null)
		{
			GameMode.ActiveGameMode.HandleHandTap(NetworkSystem.Instance.LocalPlayer, component, isLeftHand, averageVelocity, handHitInfo.normal);
		}
		StartVibration(isLeftHand, tapHapticStrength, tapHapticDuration);
		offlineVRRig.SetHandEffectData(handEffect, audioClipIndex, flag, isLeftHand, stiltID, handTapVolume, handTapSpeed, dirFromHitToHand);
		FXSystem.PlayFX(handEffect);
		this.OnHandTap?.Invoke(isLeftHand, handHitInfo.point, handHitInfo.normal);
		hasTappedSurface = true;
		if (CrittersManager.instance.IsNotNull() && CrittersManager.instance.LocalAuthority())
		{
			CrittersRigActorSetup crittersRigActorSetup = CrittersManager.instance.rigSetupByRig[offlineVRRig];
			if (crittersRigActorSetup.IsNotNull())
			{
				CrittersLoudNoise crittersLoudNoise = (CrittersLoudNoise)crittersRigActorSetup.rigActors[(!isLeftHand) ? 2 : 0].actorSet;
				if (crittersLoudNoise.IsNotNull())
				{
					crittersLoudNoise.PlayHandTapLocal(isLeftHand);
				}
			}
		}
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(offlineVRRig.zoneEntity.currentZone);
		if (managerForZone.IsNotNull() && managerForZone.ghostReactorManager.IsNotNull() && !averageVelocity.AlmostZero())
		{
			Transform handFollower = GTPlayer.Instance.GetHandFollower(isLeftHand);
			if (Physics.Raycast(new Ray(handFollower.position, averageVelocity.normalized), out var raycastHit, 10f))
			{
				Vector3 vector = Vector3.ProjectOnPlane(-handFollower.forward, raycastHit.normal);
				managerForZone.ghostReactorManager.OnTapLocal(isLeftHand, raycastHit.point + raycastHit.normal * 0.005f, Quaternion.LookRotation(vector.normalized, isLeftHand ? (-raycastHit.normal) : raycastHit.normal), surfaceOverride, averageVelocity);
			}
		}
		if (NetworkSystem.Instance.InRoom && myVRRig.IsNotNull() && myVRRig != null)
		{
			myVRRig.GetView.RPC("OnHandTapRPC", RpcTarget.Others, audioClipIndex, flag, isLeftHand, stiltID, handTapSpeed, Utils.PackVector3ToLong(dirFromHitToHand));
		}
	}

	public async void ConfirmUpdatedFrameRate()
	{
		await Awaitable.WaitForSecondsAsync(1f);
		if (Mathf.RoundToInt(OVRPlugin.systemDisplayFrequency) != Application.targetFrameRate)
		{
			float systemDisplayFrequency = OVRPlugin.systemDisplayFrequency;
			float fixedDeltaTime = 1f / systemDisplayFrequency;
			Debug.Log("Thinger: =========== Force Re-adjusting, presumably overwritten =========");
			Debug.Log(" fixedDeltaTime before:\t" + Time.fixedDeltaTime);
			Debug.Log(" Refresh rate         :\t" + systemDisplayFrequency);
			Application.targetFrameRate = Mathf.RoundToInt(OVRPlugin.systemDisplayFrequency);
			Time.fixedDeltaTime = fixedDeltaTime;
			UpdateResolutionScale(systemDisplayFrequency < _defaultRefreshRate);
			Debug.Log(" fixedDeltaTime after :\t" + Time.fixedDeltaTime);
			Debug.Log(" History size before  :\t" + GTPlayer.Instance.velocityHistorySize);
			GTPlayer.Instance.velocityHistorySize = Mathf.Max(Mathf.Min(Mathf.FloorToInt(systemDisplayFrequency * (1f / 12f)), 10), 6);
			if (GTPlayer.Instance.velocityHistorySize > 9)
			{
				GTPlayer.Instance.velocityHistorySize--;
			}
			Debug.Log("New history size: " + GTPlayer.Instance.velocityHistorySize);
			Debug.Log(" ============================================");
			GTPlayer.Instance.slideControl = 1f - CalcSlideControl(systemDisplayFrequency);
			GTPlayer.Instance.InitializeValues();
		}
	}

	public void DebugDrawTagCasts(Color color)
	{
		float num = sphereCastRadius * GTPlayer.Instance.scale;
		DrawSphereCast(lastLeftHandPositionForTag, leftRaycastSweep.normalized, num, Mathf.Max(leftRaycastSweep.magnitude, num), color);
		DrawSphereCast(headCollider.transform.position, leftHeadRaycastSweep.normalized, num, Mathf.Max(leftHeadRaycastSweep.magnitude, num), color);
		DrawSphereCast(lastRightHandPositionForTag, rightRaycastSweep.normalized, num, Mathf.Max(rightRaycastSweep.magnitude, num), color);
		DrawSphereCast(headCollider.transform.position, rightHeadRaycastSweep.normalized, num, Mathf.Max(rightHeadRaycastSweep.magnitude, num), color);
	}

	private void DrawSphereCast(Vector3 start, Vector3 dir, float radius, float dist, Color color)
	{
		DebugUtil.DrawCapsule(start, start + dir * dist, radius, 16, 16, color);
	}

	private void RecoverMissingRefs()
	{
		if (!offlineVRRig)
		{
			RecoverMissingRefs_Asdf(ref leftHandSlideSource, "leftHandSlideSource", "./**/Left Arm IK/SlideAudio");
			RecoverMissingRefs_Asdf(ref rightHandSlideSource, "rightHandSlideSource", "./**/Right Arm IK/SlideAudio");
		}
	}

	private void RecoverMissingRefs_Asdf<T>(ref T objRef, string objFieldName, string recoveryPath) where T : UnityEngine.Object
	{
		if (!objRef)
		{
			if (!offlineVRRig.transform.TryFindByPath(recoveryPath, out var result))
			{
				Debug.LogError("`" + objFieldName + "` reference missing and could not find by path: \"" + recoveryPath + "\"", this);
			}
			objRef = result.GetComponentInChildren<T>();
			if (!objRef)
			{
				Debug.LogError("`" + objFieldName + "` reference is missing. Found transform with recover path, but did not find the component. Recover path: \"" + recoveryPath + "\"", this);
			}
		}
	}

	public void GuidedRefInitialize()
	{
		GuidedRefHub.RegisterReceiverField(this, "offlineVRRig", ref offlineVRRig_gRef);
		GuidedRefHub.ReceiverFullyRegistered(this);
	}

	bool IGuidedRefReceiverMono.GuidedRefTryResolveReference(GuidedRefTryResolveInfo target)
	{
		if (offlineVRRig_gRef.fieldId == target.fieldId && offlineVRRig == null)
		{
			offlineVRRig = target.targetMono.GuidedRefTargetObject as VRRig;
			return offlineVRRig != null;
		}
		return false;
	}

	void IGuidedRefReceiverMono.OnAllGuidedRefsResolved()
	{
	}

	void IGuidedRefReceiverMono.OnGuidedRefTargetDestroyed(int fieldId)
	{
	}

	int IGuidedRefObject.GetInstanceID()
	{
		return GetInstanceID();
	}
}
