using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using GorillaNetworking;
using GorillaTag.CosmeticSystem;
using GorillaTag.Cosmetics;
using GorillaTagScripts;
using KID.Model;
using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using TagEffects;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class VRRig : MonoBehaviour, IWrappedSerializable, INetworkStruct, IPreDisable, IUserCosmeticsCallback, IGorillaSliceableSimple, ITickSystemPost, IEyeScannable
{
	public enum PartyMemberStatus
	{
		NeedsUpdate,
		InLocalParty,
		NotInLocalParty
	}

	public enum WearablePackedStateSlots
	{
		Hat,
		LeftHand,
		RightHand,
		Face,
		Pants1,
		Pants2,
		Badge,
		Fur,
		Shirt
	}

	public struct VelocityTime(Vector3 velocity, double velTime)
	{
		public Vector3 vel = velocity;

		public double time = velTime;
	}

	private bool _isListeningFor_OnPostInstantiateAllPrefabs;

	[OnEnterPlay_SetNull]
	public static Action newPlayerJoined;

	public VRMap head;

	public VRMap rightHand;

	public VRMap leftHand;

	public VRMapThumb leftThumb;

	public VRMapIndex leftIndex;

	public VRMapMiddle leftMiddle;

	public VRMapThumb rightThumb;

	public VRMapIndex rightIndex;

	public VRMapMiddle rightMiddle;

	public CrittersLoudNoise leftHandNoise;

	public CrittersLoudNoise rightHandNoise;

	public CrittersLoudNoise speakingNoise;

	private int previousGrabbedRope = -1;

	private int previousGrabbedRopeBoneIndex;

	private bool previousGrabbedRopeWasLeft;

	private bool previousGrabbedRopeWasBody;

	private GorillaRopeSwing currentRopeSwing;

	private Transform currentHoldParent;

	private Transform currentRopeSwingTarget;

	private float lastRopeGrabTimer;

	private bool shouldLerpToRope;

	[NonSerialized]
	public int grabbedRopeIndex = -1;

	[NonSerialized]
	public int grabbedRopeBoneIndex;

	[NonSerialized]
	public bool grabbedRopeIsLeft;

	[NonSerialized]
	public bool grabbedRopeIsBody;

	[NonSerialized]
	public bool grabbedRopeIsPhotonView;

	[NonSerialized]
	public Vector3 grabbedRopeOffset = Vector3.zero;

	private int prevMovingSurfaceID = -1;

	private bool movingSurfaceWasLeft;

	private bool movingSurfaceWasBody;

	private bool movingSurfaceWasMonkeBlock;

	[NonSerialized]
	public int mountedMovingSurfaceId = -1;

	[NonSerialized]
	private BuilderPiece mountedMonkeBlock;

	[NonSerialized]
	private MovingSurface mountedMovingSurface;

	[NonSerialized]
	public bool mountedMovingSurfaceIsLeft;

	[NonSerialized]
	public bool mountedMovingSurfaceIsBody;

	[NonSerialized]
	public bool movingSurfaceIsMonkeBlock;

	[NonSerialized]
	public Vector3 mountedMonkeBlockOffset = Vector3.zero;

	[NonSerialized]
	public bool InOverrideSubscriptionZone;

	[NonSerialized]
	public Vector3 OverrideSubscriptionZoneLocation = Vector3.zero;

	private float lastMountedSurfaceTimer;

	private bool shouldLerpToMovingSurface;

	[Tooltip("- False in 'Gorilla Player Networked.prefab'.\n- True in 'Local VRRig.prefab/Local Gorilla Player'.\n- False in 'Local VRRig.prefab/Actual Gorilla'")]
	public bool isOfflineVRRig;

	public GameObject mainCamera;

	public Transform playerOffsetTransform;

	public int SDKIndex;

	public bool isMyPlayer;

	public AudioSource leftHandPlayer;

	public AudioSource rightHandPlayer;

	public AudioSource tagSound;

	[SerializeField]
	private float ratio;

	public Transform headConstraint;

	public Vector3 headBodyOffset = Vector3.zero;

	public GameObject headMesh;

	private NetworkVector3 netSyncPos = new NetworkVector3();

	public Vector3 jobPos;

	public Quaternion syncRotation;

	public Quaternion jobRotation;

	public AudioClip[] clipToPlay;

	public AudioClip[] handTapSound;

	public int setMatIndex;

	public float lerpValueFingers;

	public float lerpValueBody;

	public GameObject backpack;

	public Transform leftHandTransform;

	public Transform rightHandTransform;

	public Transform bodyTransform;

	public SkinnedMeshRenderer mainSkin;

	public GorillaSkin defaultSkin;

	public MeshRenderer faceSkin;

	public XRaySkeleton skeleton;

	public GorillaBodyRenderer bodyRenderer;

	public ZoneEntityBSP zoneEntity;

	public Material scoreboardMaterial;

	public GameObject spectatorSkin;

	public int handSync;

	public Material[] materialsToChangeTo;

	public float red;

	public float green;

	public float blue;

	public TextMeshPro playerText1;

	public string playerNameVisible;

	[Tooltip("- True in 'Gorilla Player Networked.prefab'.\n- True in 'Local VRRig.prefab/Local Gorilla Player'.\n- False in 'Local VRRig.prefab/Actual Gorilla'")]
	public bool showName;

	public CosmeticItemRegistry cosmeticsObjectRegistry;

	[NonSerialized]
	public PropHuntHandFollower propHuntHandFollower;

	private int taggedById;

	private readonly HashSet<string> _playerOwnedCosmetics = new HashSet<string>(50);

	private readonly Dictionary<string, int> _playerOwnedCosmeticsAge = new Dictionary<string, int>(50);

	private bool initializedCosmetics;

	private readonly HashSet<string> _temporaryCosmetics = new HashSet<string>();

	public CosmeticsController.CosmeticSet cosmeticSet;

	public CosmeticsController.CosmeticSet tryOnSet;

	public CosmeticsController.CosmeticSet mergedSet;

	public CosmeticsController.CosmeticSet prevSet;

	[NonSerialized]
	public readonly List<GameObject> activeCosmetics = new List<GameObject>(16);

	private int cosmeticRetries = 2;

	private int currentCosmeticTries;

	public SizeManager sizeManager;

	public float pitchScale = 0.3f;

	public float pitchOffset = 1f;

	[NonSerialized]
	public bool IsHaunted;

	public float HauntedVoicePitch = 0.5f;

	public float HauntedHearingVolume = 0.15f;

	[NonSerialized]
	public bool UsingHauntedRing;

	[NonSerialized]
	public float HauntedRingVoicePitch;

	private float cosmeticPitchShift;

	private float cosmeticVolumeShift;

	private bool cosmeticPitchActive;

	private bool cosmeticVolumeActive;

	private bool anyShiftedVoiceCosmetic;

	private bool voiceShiftCosmeticsDirty;

	[NonSerialized]
	public List<VoiceShiftCosmetic> VoiceShiftCosmetics = new List<VoiceShiftCosmetic>();

	public FriendshipBracelet friendshipBraceletLeftHand;

	public NonCosmeticHandItem nonCosmeticLeftHandItem;

	public FriendshipBracelet friendshipBraceletRightHand;

	public NonCosmeticHandItem nonCosmeticRightHandItem;

	public HoverboardVisual hoverboardVisual;

	private int hoverboardEnabledCount;

	public HoldableHand bodyHolds;

	public HoldableHand leftHolds;

	public HoldableHand rightHolds;

	public GorillaClimbable leftHandHoldsPlayer;

	public GorillaClimbable rightHandHoldsPlayer;

	public TakeMyHand_HandLink leftHandLink;

	public TakeMyHand_HandLink rightHandLink;

	public GameObject nameTagAnchor;

	public GameObject frozenEffect;

	public GameObject iceCubeLeft;

	public GameObject iceCubeRight;

	public float frozenEffectMaxY;

	public float frozenEffectMaxHorizontalScale = 0.8f;

	public GameObject FPVEffectsParent;

	public Dictionary<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> TemporaryCosmeticEffects = new Dictionary<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect>();

	private float _nextUpdateTime = -1f;

	public VRRigReliableState reliableState;

	[SerializeField]
	private Transform MouthPosition;

	internal RigContainer rigContainer;

	public Action<RigContainer> OnNameChanged;

	private Vector3 remoteVelocity;

	private double remoteLatestTimestamp;

	private Vector3 remoteCorrectionNeeded;

	private const float REMOTE_CORRECTION_RATE = 5f;

	private const bool USE_NEW_NETCODE = false;

	private float stealthTimer;

	private GorillaAmbushManager stealthManager;

	private LayerChanger layerChanger;

	private float frozenEffectMinY;

	private float frozenEffectMinHorizontalScale;

	private float frozenTimeElapsed;

	public TagEffectPack CosmeticEffectPack;

	private GorillaSnapTurn GorillaSnapTurningComp;

	private bool turningCompInitialized;

	private string turnType = "NONE";

	private int turnFactor;

	private int fps;

	private PartyMemberStatus partyMemberStatus;

	public static readonly GTBitOps.BitWriteInfo[] WearablePackedStatesBitWriteInfos = new GTBitOps.BitWriteInfo[9]
	{
		new GTBitOps.BitWriteInfo(0, 1),
		new GTBitOps.BitWriteInfo(1, 2),
		new GTBitOps.BitWriteInfo(3, 2),
		new GTBitOps.BitWriteInfo(5, 2),
		new GTBitOps.BitWriteInfo(7, 2),
		new GTBitOps.BitWriteInfo(9, 2),
		new GTBitOps.BitWriteInfo(11, 1),
		new GTBitOps.BitWriteInfo(12, 1),
		new GTBitOps.BitWriteInfo(13, 1)
	};

	public bool inTryOnRoom;

	public bool inTempCosmSpace;

	[NonSerialized]
	public Dictionary<string, CosmeticsController.CollectionState> remoteCycleStates = new Dictionary<string, CosmeticsController.CollectionState>();

	private readonly List<CosmeticCollectionDisplay> scratchDisplayList = new List<CosmeticCollectionDisplay>();

	private int[] cycleStatesArray = Array.Empty<int>();

	public bool muted;

	private float lastScaleFactor = 1f;

	private float scaleMultiplier = 1f;

	private float nativeScale = 1f;

	private float timeSpawned;

	public float doNotLerpConstant = 1f;

	public string tempString;

	internal NetPlayer creator;

	private float[] speedArray;

	private double handLerpValues;

	private bool initialized;

	[FormerlySerializedAs("battleBalloons")]
	public PaintbrawlBalloons paintbrawlBalloons;

	private int tempInt;

	public BodyDockPositions myBodyDockPositions;

	public ParticleSystem lavaParticleSystem;

	public ParticleSystem rockParticleSystem;

	public ParticleSystem iceParticleSystem;

	public ParticleSystem snowFlakeParticleSystem;

	public ParticleSystem leftHandGooParticleSystem;

	public ParticleSystem rightHandGooParticleSystem;

	public string tempItemName;

	public CosmeticsController.CosmeticItem tempItem;

	public string tempItemId;

	public int tempItemCost;

	public int leftHandHoldableStatus;

	public int rightHandHoldableStatus;

	[Tooltip("This has to match the drumsAS array in DrumsItem.cs.")]
	[SerializeReference]
	public AudioSource[] musicDrums;

	private List<TransferrableObject> instrumentSelfOnly = new List<TransferrableObject>();

	public AudioSource geodeCrackingSound;

	public float bonkTime;

	public float bonkCooldown = 2f;

	private VRRig tempVRRig;

	public GameObject huntComputer;

	public GameObject builderResizeWatch;

	public BuilderArmShelf builderArmShelfLeft;

	public BuilderArmShelf builderArmShelfRight;

	public GameObject guardianEjectWatch;

	public GameObject vStumpReturnWatch;

	public GameObject rankedTimerWatch;

	public SuperInfectionHandDisplay superInfectionHand;

	public ProjectileWeapon projectileWeapon;

	private PhotonVoiceView myPhotonVoiceView;

	private VRRig senderRig;

	private bool isInitialized;

	private CircularBuffer<VelocityTime> velocityHistoryList = new CircularBuffer<VelocityTime>(200);

	public int velocityHistoryMaxLength = 200;

	private Vector3 lastPosition;

	public const int splashLimitCount = 4;

	public const float splashLimitCooldown = 0.5f;

	private float[] splashEffectTimes = new float[4];

	internal AudioSource voiceAudio;

	public bool remoteUseReplacementVoice;

	public bool localUseReplacementVoice;

	private MicWrapper currentMicWrapper;

	private IAudioDesc audioDesc;

	private float speakingLoudness;

	public bool shouldSendSpeakingLoudness = true;

	public float replacementVoiceLoudnessThreshold = 0.05f;

	public int replacementVoiceDetectionDelay = 128;

	private GorillaMouthFlap myMouthFlap;

	private GorillaSpeakerLoudness mySpeakerLoudness;

	public ReplacementVoice myReplacementVoice;

	private GorillaEyeExpressions myEyeExpressions;

	[SerializeField]
	internal NetworkView netView;

	[SerializeField]
	internal VRRigSerializer rigSerializer;

	[Obsolete("Deprecated, this is unreliable, use Creator", false)]
	public NetPlayer OwningNetPlayer;

	[SerializeField]
	private FXSystemSettings sharedFXSettings;

	[NonSerialized]
	public FXSystemSettings fxSettings;

	[SerializeField]
	private float tapPointDistance = 0.035f;

	[SerializeField]
	private float handSpeedToVolumeModifier = 0.05f;

	[SerializeField]
	private HandEffectContext _leftHandEffect;

	[SerializeField]
	private HandEffectContext _rightHandEffect;

	[SerializeField]
	private HandEffectContext _extraLeftHandEffect;

	[SerializeField]
	private HandEffectContext _extraRightHandEffect;

	[SerializeField]
	private Transform renderTransform;

	private GamePlayer _gamePlayerRef;

	private bool playerWasHaunted;

	private float nonHauntedVolume;

	[SerializeField]
	private AnimationCurve voicePitchForRelativeScale;

	private Vector3 LocalTrajectoryOverridePosition;

	private Vector3 LocalTrajectoryOverrideVelocity;

	private float LocalTrajectoryOverrideBlend;

	[SerializeField]
	private float LocalTrajectoryOverrideDuration = 1f;

	private bool localOverrideIsBody;

	private bool localOverrideIsLeftHand;

	private Transform localOverrideGrabbingHand;

	private float localGrabOverrideBlend;

	[SerializeField]
	private float LocalGrabOverrideDuration = 0.25f;

	private float[] voiceSampleBuffer = new float[128];

	private const int CHECK_LOUDNESS_FREQ_FRAMES = 10;

	private CallbackContainer<ICallBack> lateUpdateCallbacks = new CallbackContainer<ICallBack>(5);

	private float nextLocalVelocityStoreTimestamp;

	private bool IsInvisibleToLocalPlayer;

	private const int remoteUseReplacementVoice_BIT = 512;

	private const int grabbedRope_BIT = 1024;

	private const int grabbedRopeIsPhotonView_BIT = 2048;

	private const int isHoldingHandsWithPlayer_BIT = 4096;

	private const int isHoldingHoverboard_BIT = 8192;

	private const int isHoverboardLeftHanded_BIT = 16384;

	private const int isOnMovingSurface_BIT = 32768;

	private const int isPropHunt_BIT = 65536;

	private const int propHuntLeftHand_BIT = 131072;

	private const int isLeftHandGrabbable_BIT = 262144;

	private const int isRightHandGrabbable_BIT = 524288;

	private const int isLeftHandTentacleHoldingHand_BIT = 1048576;

	private const int isRightHandTentacleHoldingHand_BIT = 2097152;

	private const int showSubscriber_BIT = 4194304;

	private const int speakingLoudnessVal_BITSHIFT = 24;

	private GorillaIK myIk;

	private Vector3 tempVec;

	private Quaternion tempQuat;

	public Action<int, int> OnMaterialIndexChanged;

	[SerializeField]
	private ParticleSystem cosmeticsActivationPS;

	[SerializeField]
	private SoundBankPlayer cosmeticsActivationSBP;

	public Color playerColor;

	public bool colorInitialized;

	private Action<Color> onColorInitialized;

	private bool m_sentRankedScore;

	private int currentQuestScore;

	private bool _scoreUpdated;

	private CallLimiter updateQuestCallLimit = new CallLimiter(1, 0.5f);

	private float currentRankedELO;

	private int currentRankedSubTierQuest;

	private int currentRankedSubTierPC;

	private bool _rankedInfoUpdated;

	internal CallLimiter updateRankedInfoCallLimit = new CallLimiter(2, 60f);

	public const float maxGuardianThrowVelocity = 20f;

	public const float maxRegularThrowVelocity = 3f;

	private RaycastHit[] rayCastNonAllocColliders = new RaycastHit[5];

	private bool inDuplicationZone;

	private RigDuplicationZone duplicationZone;

	private Vector3 cachedRenderTransformPos = new Vector3(0f, -1.65f, 0f);

	private bool pendingCosmeticUpdate = true;

	[NonSerialized]
	private bool showGoldNameTag;

	public List<HandEffectsOverrideCosmetic> CosmeticHandEffectsOverride_Right = new List<HandEffectsOverrideCosmetic>();

	public List<HandEffectsOverrideCosmetic> CosmeticHandEffectsOverride_Left = new List<HandEffectsOverrideCosmetic>();

	private int loudnessCheckFrame;

	private float frameScale;

	private SubscriptionManager.SubscriptionDetails subDataCache;

	private const bool SHOW_SCREENS = false;

	[OnEnterPlay_SetNull]
	private static VRRig gLocalRig;

	public Vector3 syncPos
	{
		get
		{
			return netSyncPos.CurrentSyncTarget;
		}
		set
		{
			netSyncPos.SetNewSyncTarget(value);
		}
	}

	public Material myDefaultSkinMaterialInstance => bodyRenderer.myDefaultSkinMaterialInstance;

	public List<GameObject> cosmetics => CosmeticsV2Spawner_Dirty.RigDataForRig(this).vrRig_cosmetics;

	public List<GameObject> overrideCosmetics => CosmeticsV2Spawner_Dirty.RigDataForRig(this).vrRig_override;

	public HashSet<string> TemporaryCosmetics => _temporaryCosmetics;

	internal bool InitializedCosmetics
	{
		get
		{
			return initializedCosmetics;
		}
		set
		{
			initializedCosmetics = value;
		}
	}

	[field: SerializeField]
	public CosmeticRefRegistry cosmeticReferences { get; private set; }

	public float LastTouchedGroundAtNetworkTime { get; private set; }

	public float LastHandTouchedGroundAtNetworkTime { get; private set; }

	public bool HasBracelet => reliableState.HasBracelet;

	public GorillaSkin CurrentCosmeticSkin { get; set; }

	public GorillaSkin CurrentModeSkin { get; set; }

	public GorillaSkin TemporaryEffectSkin { get; set; }

	public bool PostTickRunning { get; set; }

	public bool IsLocalPartyMember => GetPartyMemberStatus() != PartyMemberStatus.NotInLocalParty;

	public int WearablePackedStates
	{
		get
		{
			return reliableState.wearablesPackedStates;
		}
		set
		{
			if (reliableState.wearablesPackedStates != value)
			{
				reliableState.wearablesPackedStates = value;
				reliableState.SetIsDirty();
			}
		}
	}

	public int LeftThrowableProjectileIndex
	{
		get
		{
			return reliableState.lThrowableProjectileIndex;
		}
		set
		{
			if (reliableState.lThrowableProjectileIndex != value)
			{
				reliableState.lThrowableProjectileIndex = value;
				reliableState.SetIsDirty();
			}
		}
	}

	public int RightThrowableProjectileIndex
	{
		get
		{
			return reliableState.rThrowableProjectileIndex;
		}
		set
		{
			if (reliableState.rThrowableProjectileIndex != value)
			{
				reliableState.rThrowableProjectileIndex = value;
				reliableState.SetIsDirty();
			}
		}
	}

	public Color32 LeftThrowableProjectileColor
	{
		get
		{
			return reliableState.lThrowableProjectileColor;
		}
		set
		{
			if (!reliableState.lThrowableProjectileColor.Equals(value))
			{
				reliableState.lThrowableProjectileColor = value;
				reliableState.SetIsDirty();
			}
		}
	}

	public Color32 RightThrowableProjectileColor
	{
		get
		{
			return reliableState.rThrowableProjectileColor;
		}
		set
		{
			if (!reliableState.rThrowableProjectileColor.Equals(value))
			{
				reliableState.rThrowableProjectileColor = value;
				reliableState.SetIsDirty();
			}
		}
	}

	private int RandomThrowableIndex
	{
		get
		{
			return reliableState.randomThrowableIndex;
		}
		set
		{
			if (reliableState.randomThrowableIndex != value)
			{
				reliableState.randomThrowableIndex = value;
				reliableState.SetIsDirty();
			}
		}
	}

	public bool IsMicEnabled
	{
		get
		{
			return reliableState.isMicEnabled;
		}
		set
		{
			if (reliableState.isMicEnabled != value)
			{
				reliableState.isMicEnabled = value;
				reliableState.SetIsDirty();
			}
		}
	}

	public int SizeLayerMask
	{
		get
		{
			return reliableState.sizeLayerMask;
		}
		set
		{
			if (reliableState.sizeLayerMask != value)
			{
				reliableState.sizeLayerMask = value;
				reliableState.SetIsDirty();
			}
		}
	}

	public float scaleFactor => scaleMultiplier * nativeScale;

	public float ScaleMultiplier
	{
		get
		{
			return scaleMultiplier;
		}
		set
		{
			scaleMultiplier = value;
		}
	}

	public float NativeScale
	{
		get
		{
			return nativeScale;
		}
		set
		{
			nativeScale = value;
		}
	}

	public NetPlayer Creator => creator;

	internal bool Initialized => initialized;

	public float SpeakingLoudness
	{
		get
		{
			return speakingLoudness;
		}
		set
		{
			speakingLoudness = value;
		}
	}

	internal HandEffectContext LeftHandEffect => _leftHandEffect;

	internal HandEffectContext RightHandEffect => _rightHandEffect;

	internal HandEffectContext ExtraLeftHandEffect => _extraLeftHandEffect;

	internal HandEffectContext ExtraRightHandEffect => _extraRightHandEffect;

	public GamePlayer GamePlayerRef
	{
		get
		{
			if (_gamePlayerRef == null)
			{
				_gamePlayerRef = GetComponent<GamePlayer>();
			}
			return _gamePlayerRef;
		}
	}

	public bool IsPlayerMeshHidden => !mainSkin.enabled;

	bool IUserCosmeticsCallback.PendingUpdate
	{
		get
		{
			return pendingCosmeticUpdate;
		}
		set
		{
			pendingCosmeticUpdate = value;
		}
	}

	public bool IsFrozen { get; set; }

	public bool ShowGoldNameTag
	{
		get
		{
			return showGoldNameTag;
		}
		private set
		{
			showGoldNameTag = value;
		}
	}

	public static VRRig LocalRig => gLocalRig;

	public bool isLocal => gLocalRig == this;

	int IEyeScannable.scannableId => base.gameObject.GetInstanceID();

	Vector3 IEyeScannable.Position => base.transform.position;

	Bounds IEyeScannable.Bounds => default(Bounds);

	IList<KeyValueStringPair> IEyeScannable.Entries => buildEntries();

	public event Action<Color> OnColorChanged;

	public event Action OnPlayerNameVisibleChanged;

	public event Action<int> OnQuestScoreChanged;

	public event Action<int, int> OnRankedSubtierChanged;

	public event Action OnDataChange;

	private void CosmeticsV2_Awake()
	{
		CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs = (Action)Delegate.Combine(CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs, new Action(Handle_CosmeticsV2_OnPostInstantiateAllPrefabs_DoEnableAllCosmetics));
	}

	internal void Handle_CosmeticsV2_OnPostInstantiateAllPrefabs_DoEnableAllCosmetics()
	{
		CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs = (Action)Delegate.Remove(CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs, new Action(Handle_CosmeticsV2_OnPostInstantiateAllPrefabs_DoEnableAllCosmetics));
		CheckForEarlyAccess();
		SetCosmeticsActive(playfx: false);
	}

	internal void SetTaggedBy(VRRig taggingRig)
	{
		taggedById = taggingRig.OwningNetPlayer.ActorNumber;
	}

	public int CheckCosmeticAge(string pfID)
	{
		if (_playerOwnedCosmeticsAge.ContainsKey(pfID))
		{
			return _playerOwnedCosmeticsAge[pfID];
		}
		return 0;
	}

	public void SetVoiceShiftCosmeticsDirty()
	{
		voiceShiftCosmeticsDirty = true;
	}

	public void BreakHandLinks()
	{
		leftHandLink.BreakLink();
		rightHandLink.BreakLink();
	}

	public bool IsInHandHoldChainWithOtherPlayer(int otherPlayer)
	{
		if (!TakeMyHand_HandLink.IsHandInChainWithOtherPlayer(leftHandLink, otherPlayer))
		{
			return TakeMyHand_HandLink.IsHandInChainWithOtherPlayer(rightHandLink, otherPlayer);
		}
		return true;
	}

	public Vector3 GetMouthPosition()
	{
		return MouthPosition.position;
	}

	public PartyMemberStatus GetPartyMemberStatus()
	{
		if (partyMemberStatus == PartyMemberStatus.NeedsUpdate)
		{
			partyMemberStatus = (FriendshipGroupDetection.Instance.IsInMyGroup(creator.UserId) ? PartyMemberStatus.InLocalParty : PartyMemberStatus.NotInLocalParty);
		}
		return partyMemberStatus;
	}

	public void ClearPartyMemberStatus()
	{
		partyMemberStatus = PartyMemberStatus.NeedsUpdate;
	}

	public int ActiveTransferrableObjectIndex(int idx)
	{
		return reliableState.activeTransferrableObjectIndex[idx];
	}

	public int ActiveTransferrableObjectIndexLength()
	{
		return reliableState.activeTransferrableObjectIndex.Length;
	}

	public void SetActiveTransferrableObjectIndex(int idx, int v)
	{
		if (reliableState.activeTransferrableObjectIndex[idx] != v)
		{
			reliableState.activeTransferrableObjectIndex[idx] = v;
			reliableState.SetIsDirty();
		}
	}

	public TransferrableObject.PositionState TransferrablePosStates(int idx)
	{
		return reliableState.transferrablePosStates[idx];
	}

	public void SetTransferrablePosStates(int idx, TransferrableObject.PositionState v)
	{
		if (reliableState.transferrablePosStates[idx] != v)
		{
			reliableState.transferrablePosStates[idx] = v;
			reliableState.SetIsDirty();
		}
	}

	public TransferrableObject.ItemStates TransferrableItemStates(int idx)
	{
		return reliableState.transferrableItemStates[idx];
	}

	public void SetTransferrableItemStates(int idx, TransferrableObject.ItemStates v)
	{
		if (reliableState.transferrableItemStates[idx] != v)
		{
			reliableState.transferrableItemStates[idx] = v;
			reliableState.SetIsDirty();
		}
	}

	public void SetTransferrableDockPosition(int idx, BodyDockPositions.DropPositions v)
	{
		if (reliableState.transferableDockPositions[idx] != v)
		{
			reliableState.transferableDockPositions[idx] = v;
			reliableState.SetIsDirty();
		}
	}

	public BodyDockPositions.DropPositions TransferrableDockPosition(int idx)
	{
		return reliableState.transferableDockPositions[idx];
	}

	public Color32 GetThrowableProjectileColor(bool isLeftHand)
	{
		if (!isLeftHand)
		{
			return RightThrowableProjectileColor;
		}
		return LeftThrowableProjectileColor;
	}

	public void SetThrowableProjectileColor(bool isLeftHand, Color32 color)
	{
		if (isLeftHand)
		{
			LeftThrowableProjectileColor = color;
		}
		else
		{
			RightThrowableProjectileColor = color;
		}
	}

	public void SetRandomThrowableModelIndex(int randModelIndex)
	{
		RandomThrowableIndex = randModelIndex;
	}

	public int GetRandomThrowableModelIndex()
	{
		return RandomThrowableIndex;
	}

	public void BuildInitialize()
	{
		fxSettings = UnityEngine.Object.Instantiate(sharedFXSettings);
		fxSettings.forLocalRig = isOfflineVRRig;
		lastPosition = base.transform.position;
		if (!isOfflineVRRig)
		{
			base.transform.parent = null;
		}
		GetComponent<SizeManager>()?.BuildInitialize();
		myMouthFlap = GetComponent<GorillaMouthFlap>();
		mySpeakerLoudness = GetComponent<GorillaSpeakerLoudness>();
		if (myReplacementVoice == null)
		{
			myReplacementVoice = GetComponentInChildren<ReplacementVoice>();
		}
		myEyeExpressions = GetComponent<GorillaEyeExpressions>();
	}

	private void Awake()
	{
		cosmeticsObjectRegistry = new CosmeticItemRegistry(this);
		CosmeticsV2_Awake();
		PlayFabAuthenticator instance = PlayFabAuthenticator.instance;
		instance.OnSafetyUpdate = (Action<bool>)Delegate.Combine(instance.OnSafetyUpdate, new Action<bool>(UpdateNameSafeAccount));
		if (isOfflineVRRig)
		{
			gLocalRig = this;
			BuildInitialize();
		}
		SharedStart();
	}

	private void ApplyColorCode()
	{
		float defaultValue = 0f;
		float num = PlayerPrefs.GetFloat("redValue", defaultValue);
		float num2 = PlayerPrefs.GetFloat("greenValue", defaultValue);
		float num3 = PlayerPrefs.GetFloat("blueValue", defaultValue);
		GorillaTagger.Instance.UpdateColor(num, num2, num3);
	}

	private void SharedStart()
	{
		if (isInitialized)
		{
			return;
		}
		lastScaleFactor = scaleFactor;
		isInitialized = true;
		myBodyDockPositions = GetComponent<BodyDockPositions>();
		reliableState.SharedStart(isOfflineVRRig, myBodyDockPositions);
		bodyRenderer.SharedStart();
		initialized = false;
		if (isOfflineVRRig)
		{
			if (CosmeticsController.hasInstance && CosmeticsController.instance.v2_allCosmeticsInfoAssetRef_isLoaded)
			{
				CosmeticsController.instance.currentWornSet.LoadFromPlayerPreferences(CosmeticsController.instance);
			}
			if (Application.platform == RuntimePlatform.Android && spectatorSkin != null)
			{
				UnityEngine.Object.Destroy(spectatorSkin);
			}
			initialized = true;
		}
		else if (!isOfflineVRRig)
		{
			if (spectatorSkin != null)
			{
				UnityEngine.Object.Destroy(spectatorSkin);
			}
			head.syncPos = -headBodyOffset;
		}
		GorillaSkin.ShowActiveSkin(this);
		Invoke("ApplyColorCode", 1f);
		List<Material> m = new List<Material>();
		mainSkin.GetSharedMaterials(m);
		layerChanger = GetComponent<LayerChanger>();
		if (layerChanger != null)
		{
			layerChanger.InitializeLayers(base.transform);
		}
		frozenEffectMinY = frozenEffect.transform.localScale.y;
		frozenEffectMinHorizontalScale = frozenEffect.transform.localScale.x;
		rightIndex.Initialize();
		rightMiddle.Initialize();
		rightThumb.Initialize();
		leftIndex.Initialize();
		leftMiddle.Initialize();
		leftThumb.Initialize();
		cachedRenderTransformPos = renderTransform.localPosition;
	}

	public void SliceUpdate()
	{
		float time = Time.time;
		if (_nextUpdateTime < 0f)
		{
			_nextUpdateTime = time + 1f;
		}
		else if (!(time < _nextUpdateTime))
		{
			_nextUpdateTime = time + 1f;
			if (RoomSystem.JoinedRoom && NetworkSystem.Instance.IsMasterClient && GorillaGameModes.GameMode.ActiveNetworkHandler.IsNull())
			{
				GorillaGameModes.GameMode.LoadGameModeFromProperty();
			}
		}
	}

	public bool IsItemAllowed(string itemName)
	{
		if (itemName == "Slingshot")
		{
			if (NetworkSystem.Instance.InRoom && GorillaGameManager.instance is GorillaPaintbrawlManager)
			{
				return true;
			}
			return false;
		}
		if (BuilderSetManager.instance.GetStarterSetsConcat().Contains(itemName))
		{
			return true;
		}
		if (_playerOwnedCosmetics.Contains(itemName) || PlayerCosmeticsSystem.IsTemporaryCosmeticAllowed(this, itemName))
		{
			return true;
		}
		bool canTryOn = CosmeticsController.instance.GetItemFromDict(itemName).canTryOn;
		if (inTryOnRoom && canTryOn)
		{
			return true;
		}
		return false;
	}

	public void ApplyLocalTrajectoryOverride(Vector3 overrideVelocity)
	{
		LocalTrajectoryOverrideBlend = 1f;
		LocalTrajectoryOverridePosition = base.transform.position;
		LocalTrajectoryOverrideVelocity = overrideVelocity;
	}

	public bool IsLocalTrajectoryOverrideActive()
	{
		return LocalTrajectoryOverrideBlend > 0f;
	}

	public void ApplyLocalGrabOverride(bool isBody, bool isLeftHand, Transform grabbingHand)
	{
		localOverrideIsBody = isBody;
		localOverrideIsLeftHand = isLeftHand;
		localOverrideGrabbingHand = grabbingHand;
		localGrabOverrideBlend = 1f;
	}

	public void ClearLocalGrabOverride()
	{
		localGrabOverrideBlend = -1f;
	}

	public void RemoteRigUpdate()
	{
		if (scaleFactor != lastScaleFactor)
		{
			ScaleUpdate();
		}
		if (voiceAudio != null)
		{
			float? num = null;
			float? num2 = null;
			if (IsHaunted)
			{
				num = HauntedVoicePitch;
			}
			else if (UsingHauntedRing)
			{
				num = HauntedRingVoicePitch;
			}
			else
			{
				if (voiceShiftCosmeticsDirty)
				{
					cosmeticPitchShift = 0f;
					cosmeticVolumeShift = 0f;
					anyShiftedVoiceCosmetic = false;
					int num3 = 0;
					int num4 = 0;
					for (int i = 0; i < VoiceShiftCosmetics.Count; i++)
					{
						VoiceShiftCosmetic voiceShiftCosmetic = VoiceShiftCosmetics[i];
						if (voiceShiftCosmetic.IsShifted)
						{
							anyShiftedVoiceCosmetic = true;
							if (voiceShiftCosmetic.ModifyPitch)
							{
								cosmeticPitchShift += voiceShiftCosmetic.Pitch;
								num3++;
							}
							if (voiceShiftCosmetic.ModifyVolume)
							{
								cosmeticVolumeShift += voiceShiftCosmetic.Volume;
								num4++;
							}
						}
					}
					cosmeticPitchActive = num3 > 0;
					cosmeticVolumeActive = num4 > 0;
					if (cosmeticPitchActive)
					{
						cosmeticPitchShift /= num3;
					}
					if (cosmeticVolumeActive)
					{
						cosmeticVolumeShift /= num4;
					}
					voiceShiftCosmeticsDirty = false;
				}
				if (anyShiftedVoiceCosmetic)
				{
					if (cosmeticPitchActive)
					{
						num = cosmeticPitchShift;
					}
					if (cosmeticVolumeActive)
					{
						num2 = cosmeticVolumeShift;
					}
				}
				else
				{
					float time = GorillaTagger.Instance.offlineVRRig.scaleFactor / scaleFactor;
					float num5 = voicePitchForRelativeScale.Evaluate(time);
					if (float.IsNaN(num5) || num5 <= 0f)
					{
						Debug.LogError("Voice pitch curve is invalid, please fix!");
					}
					else
					{
						num = num5;
					}
				}
			}
			if (num.HasValue && !Mathf.Approximately(voiceAudio.pitch, num.Value))
			{
				voiceAudio.pitch = num.Value;
			}
			if (num2.HasValue && !Mathf.Approximately(voiceAudio.volume, num2.Value))
			{
				voiceAudio.volume = num2.Value;
			}
		}
		jobPos = base.transform.position;
		if (Time.time > timeSpawned + doNotLerpConstant)
		{
			jobPos = Vector3.Lerp(base.transform.position, SanitizeVector3(syncPos), lerpValueBody * 0.66f);
			if ((bool)currentRopeSwing && (bool)currentRopeSwingTarget)
			{
				Vector3 vector = ((!grabbedRopeIsLeft) ? (currentRopeSwingTarget.position - rightHandTransform.position) : (currentRopeSwingTarget.position - leftHandTransform.position));
				if (shouldLerpToRope)
				{
					jobPos += Vector3.Lerp(Vector3.zero, vector, lastRopeGrabTimer * 4f);
					if (lastRopeGrabTimer < 1f)
					{
						lastRopeGrabTimer += Time.deltaTime;
					}
				}
				else
				{
					jobPos += vector;
				}
			}
			else if ((bool)currentHoldParent)
			{
				Transform transform = (grabbedRopeIsBody ? bodyTransform : ((!grabbedRopeIsLeft) ? rightHandTransform : leftHandTransform));
				jobPos += currentHoldParent.TransformPoint(grabbedRopeOffset) - transform.position;
			}
			else if ((bool)mountedMonkeBlock || (bool)mountedMovingSurface)
			{
				Transform obj = (movingSurfaceIsMonkeBlock ? mountedMonkeBlock.transform : mountedMovingSurface.transform);
				Vector3 zero = Vector3.zero;
				Vector3 vector2 = jobPos - base.transform.position;
				Transform transform2 = (mountedMovingSurfaceIsBody ? bodyTransform : ((!mountedMovingSurfaceIsLeft) ? rightHandTransform : leftHandTransform));
				zero = obj.TransformPoint(mountedMonkeBlockOffset) - (transform2.position + vector2);
				if (shouldLerpToMovingSurface)
				{
					lastMountedSurfaceTimer += Time.deltaTime;
					jobPos += Vector3.Lerp(Vector3.zero, zero, lastMountedSurfaceTimer * 4f);
					if (lastMountedSurfaceTimer * 4f >= 1f)
					{
						shouldLerpToMovingSurface = false;
					}
				}
				else
				{
					jobPos += zero;
				}
			}
			else if (InOverrideSubscriptionZone)
			{
				jobPos = OverrideSubscriptionZoneLocation;
			}
		}
		else
		{
			jobPos = SanitizeVector3(syncPos);
		}
		if (LocalTrajectoryOverrideBlend > 0f)
		{
			LocalTrajectoryOverrideBlend -= Time.deltaTime / LocalTrajectoryOverrideDuration;
			LocalTrajectoryOverrideVelocity += Physics.gravity * Time.deltaTime * 0.5f;
			if (LocalTestMovementCollision(LocalTrajectoryOverridePosition, LocalTrajectoryOverrideVelocity, out var modifiedVelocity, out var finalPosition))
			{
				LocalTrajectoryOverrideVelocity = modifiedVelocity;
				LocalTrajectoryOverridePosition = finalPosition;
			}
			else
			{
				LocalTrajectoryOverridePosition += LocalTrajectoryOverrideVelocity * Time.deltaTime;
			}
			LocalTrajectoryOverrideVelocity += Physics.gravity * Time.deltaTime * 0.5f;
			jobPos = Vector3.Lerp(jobPos, LocalTrajectoryOverridePosition, LocalTrajectoryOverrideBlend);
		}
		else if (localGrabOverrideBlend > 0f)
		{
			localGrabOverrideBlend -= Time.deltaTime / LocalGrabOverrideDuration;
			if (localOverrideGrabbingHand != null)
			{
				Transform transform3 = (localOverrideIsBody ? bodyTransform : ((!localOverrideIsLeftHand) ? rightHandTransform : leftHandTransform));
				jobPos += localOverrideGrabbingHand.TransformPoint(grabbedRopeOffset) - transform3.position;
			}
		}
		if (Time.time > timeSpawned + doNotLerpConstant)
		{
			jobRotation = Quaternion.Lerp(base.transform.rotation, SanitizeQuaternion(syncRotation), lerpValueBody);
		}
		else
		{
			jobRotation = SanitizeQuaternion(syncRotation);
		}
		head.syncPos = base.transform.rotation * -headBodyOffset * scaleFactor;
		head.MapOther(lerpValueBody);
		rightHand.MapOther(lerpValueBody);
		leftHand.MapOther(lerpValueBody);
		rightIndex.MapOtherFinger((float)(handSync % 10) / 10f, lerpValueFingers);
		rightMiddle.MapOtherFinger((float)(handSync % 100) / 100f, lerpValueFingers);
		rightThumb.MapOtherFinger((float)(handSync % 1000) / 1000f, lerpValueFingers);
		leftIndex.MapOtherFinger((float)(handSync % 10000) / 10000f, lerpValueFingers);
		leftMiddle.MapOtherFinger((float)(handSync % 100000) / 100000f, lerpValueFingers);
		leftThumb.MapOtherFinger((float)(handSync % 1000000) / 1000000f, lerpValueFingers);
		leftHandHoldableStatus = handSync % 10000000 / 1000000;
		rightHandHoldableStatus = handSync % 100000000 / 10000000;
	}

	private void ScaleUpdate()
	{
		frameScale = Mathf.MoveTowards(lastScaleFactor, scaleFactor, Time.deltaTime * 4f);
		base.transform.localScale = Vector3.one * frameScale;
		lastScaleFactor = frameScale;
	}

	public void AddLateUpdateCallback(ICallBack action)
	{
		lateUpdateCallbacks.Add(in action);
	}

	public void RemoveLateUpdateCallback(ICallBack action)
	{
		lateUpdateCallbacks.Remove(in action);
	}

	public void PostTick()
	{
		GTPlayer instance = GTPlayer.Instance;
		if (isOfflineVRRig)
		{
			if (GorillaGameManager.instance != null)
			{
				speedArray = GorillaGameManager.instance.LocalPlayerSpeed();
				instance.jumpMultiplier = speedArray[1];
				instance.maxJumpSpeed = speedArray[0];
			}
			else
			{
				instance.jumpMultiplier = 1.1f;
				instance.maxJumpSpeed = 6.5f;
			}
			nativeScale = instance.NativeScale;
			scaleMultiplier = instance.ScaleMultiplier;
			if (scaleFactor != lastScaleFactor)
			{
				ScaleUpdate();
			}
			syncPos = mainCamera.transform.position + headConstraint.rotation * head.trackingPositionOffset * lastScaleFactor + base.transform.rotation * headBodyOffset * lastScaleFactor;
			base.transform.SetPositionAndRotation(syncPos, GTPlayerTransform.BodyRotation);
			head.MapMine(lastScaleFactor, playerOffsetTransform);
			rightHand.MapMine(lastScaleFactor, playerOffsetTransform);
			leftHand.MapMine(lastScaleFactor, playerOffsetTransform);
			rightIndex.MapMyFinger(lerpValueFingers);
			rightMiddle.MapMyFinger(lerpValueFingers);
			rightThumb.MapMyFinger(lerpValueFingers);
			leftIndex.MapMyFinger(lerpValueFingers);
			leftMiddle.MapMyFinger(lerpValueFingers);
			leftThumb.MapMyFinger(lerpValueFingers);
			bool isGroundedHand = instance.IsGroundedHand || instance.IsThrusterActive;
			bool isGroundedButt = instance.IsGroundedButt;
			bool isLeftGrabbing = EquipmentInteractor.instance.isLeftGrabbing;
			bool isReadyForGrabbing = isLeftGrabbing && EquipmentInteractor.instance.CanGrabLeft();
			bool isRightGrabbing = EquipmentInteractor.instance.isRightGrabbing;
			bool isReadyForGrabbing2 = isRightGrabbing && EquipmentInteractor.instance.CanGrabRight();
			LastTouchedGroundAtNetworkTime = instance.LastTouchedGroundAtNetworkTime;
			LastHandTouchedGroundAtNetworkTime = instance.LastHandTouchedGroundAtNetworkTime;
			leftHandLink?.LocalUpdate(isGroundedHand, isGroundedButt, isLeftGrabbing, isReadyForGrabbing);
			rightHandLink?.LocalUpdate(isGroundedHand, isGroundedButt, isRightGrabbing, isReadyForGrabbing2);
			if (GorillaTagger.Instance.loadedDeviceName == "Oculus")
			{
				mainSkin.enabled = (OVRManager.hasInputFocus ? true : false);
			}
			bodyRenderer.ActiveBody.enabled = !instance.inOverlay;
			if (--loudnessCheckFrame < 0)
			{
				SpeakingLoudness = 0f;
				if (shouldSendSpeakingLoudness && (bool)netView)
				{
					PhotonVoiceView component = netView.GetComponent<PhotonVoiceView>();
					if ((bool)component && (bool)component.RecorderInUse && component.RecorderInUse.InputSource is MicWrapper micWrapper)
					{
						int num = replacementVoiceDetectionDelay;
						if (num > voiceSampleBuffer.Length)
						{
							Array.Resize(ref voiceSampleBuffer, num);
						}
						float[] array = voiceSampleBuffer;
						if (micWrapper.Mic != null && micWrapper.Mic.samples >= num && micWrapper.Mic.GetData(array, micWrapper.Mic.samples - num))
						{
							float num2 = 0f;
							for (int i = 0; i < num; i++)
							{
								float num3 = Mathf.Sqrt(array[i]);
								if (num3 > num2)
								{
									num2 = num3;
								}
							}
							SpeakingLoudness = num2;
						}
					}
				}
				loudnessCheckFrame = 10;
			}
			if (PhotonNetwork.InRoom && Time.time > nextLocalVelocityStoreTimestamp)
			{
				AddVelocityToQueue(base.transform.position, PhotonNetwork.Time);
				nextLocalVelocityStoreTimestamp = Time.time + 0.1f;
			}
		}
		if (leftHandLink.IsLinkActive())
		{
			VRRig myRig = leftHandLink.grabbedLink.myRig;
			if (isLocal && myRig.inDuplicationZone && myRig.duplicationZone.IsApplyingDisplacement)
			{
				leftHandLink.BreakLink();
			}
			else
			{
				leftHandLink.VisuallySnapHandsTogether();
			}
		}
		if (rightHandLink.IsLinkActive())
		{
			VRRig myRig2 = rightHandLink.grabbedLink.myRig;
			if (isLocal && myRig2.inDuplicationZone && myRig2.duplicationZone.IsApplyingDisplacement)
			{
				rightHandLink.BreakLink();
			}
			else
			{
				rightHandLink.VisuallySnapHandsTogether();
			}
		}
		if (creator != null)
		{
			if (GorillaGameManager.instance != null)
			{
				GorillaGameManager.instance.UpdatePlayerAppearance(this);
			}
			else if (setMatIndex != 0)
			{
				ChangeMaterialLocal(0);
				ForceResetFrozenEffect();
			}
		}
		if (inDuplicationZone)
		{
			renderTransform.position = base.transform.position + duplicationZone.GetVisualOffsetForRigs(cachedRenderTransformPos);
		}
		if (frozenEffect.activeSelf && GorillaGameManager.instance is GorillaFreezeTagManager gorillaFreezeTagManager)
		{
			UpdateFrozen(Time.deltaTime, gorillaFreezeTagManager.freezeDuration);
		}
		if (TemporaryCosmeticEffects.Count > 0)
		{
			KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect>[] array2 = TemporaryCosmeticEffects.ToArray();
			for (int j = 0; j < array2.Length; j++)
			{
				KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect = array2[j];
				if (Time.time - effect.Value.EffectStartedTime >= effect.Value.EffectDuration)
				{
					RemoveTemporaryCosmeticEffects(effect);
				}
			}
		}
		lateUpdateCallbacks.TryRunCallbacks();
	}

	public void UpdateFrozen(float dt, float freezeDuration)
	{
		Vector3 localScale = frozenEffect.transform.localScale;
		Vector3 vector = localScale;
		vector.y = Mathf.Lerp(frozenEffectMinY, frozenEffectMaxY, frozenTimeElapsed / freezeDuration);
		localScale = new Vector3(localScale.x, vector.y, localScale.z);
		frozenEffect.transform.localScale = localScale;
		frozenTimeElapsed += dt;
	}

	private void RemoveTemporaryCosmeticEffects(KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect)
	{
		if (effect.Key == CosmeticEffectsOnPlayers.EFFECTTYPE.Skin)
		{
			if (effect.Value.newSkin != null && GorillaSkin.GetActiveSkin(this, out var _) == effect.Value.newSkin)
			{
				GorillaSkin.ApplyToRig(this, null, GorillaSkin.SkinType.temporaryEffect);
			}
		}
		else if (effect.Key == CosmeticEffectsOnPlayers.EFFECTTYPE.TagWithKnockback)
		{
			DisableHitWithKnockBack(effect);
		}
		TemporaryCosmeticEffects.Remove(effect.Key);
	}

	public void SpawnSkinEffects(KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect)
	{
		GorillaSkin.ApplyToRig(this, effect.Value.newSkin, GorillaSkin.SkinType.temporaryEffect);
		TemporaryCosmeticEffects.TryAdd(effect.Key, effect.Value);
	}

	public void EnableHitWithKnockBack(KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect)
	{
		TemporaryCosmeticEffects.TryAdd(effect.Key, effect.Value);
	}

	private void DisableHitWithKnockBack(KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect)
	{
		if (TemporaryCosmeticEffects.ContainsKey(effect.Key) && (bool)effect.Value.knockbackVFX)
		{
			GameObject gameObject = ObjectPools.instance.Instantiate(effect.Value.knockbackVFX, base.transform.position);
			if (gameObject != null)
			{
				gameObject.gameObject.transform.SetParent(base.transform);
				gameObject.gameObject.transform.localPosition = Vector3.zero;
			}
		}
	}

	public void DisableHitWithKnockBack()
	{
		KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect>[] array = TemporaryCosmeticEffects.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect = array[i];
			bool useDefaultBodySkin;
			if (effect.Key == CosmeticEffectsOnPlayers.EFFECTTYPE.TagWithKnockback)
			{
				DisableHitWithKnockBack(effect);
				TemporaryCosmeticEffects.Remove(effect.Key);
			}
			else if (effect.Key == CosmeticEffectsOnPlayers.EFFECTTYPE.Skin && effect.Value.newSkin != null && GorillaSkin.GetActiveSkin(this, out useDefaultBodySkin) == effect.Value.newSkin)
			{
				GorillaSkin.ApplyToRig(this, null, GorillaSkin.SkinType.temporaryEffect);
				TemporaryCosmeticEffects.Remove(effect.Key);
			}
		}
	}

	public void ApplyInstanceKnockBack(KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect)
	{
		TemporaryCosmeticEffects.TryAdd(effect.Key, effect.Value);
	}

	public void ActivateVOEffect(KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect)
	{
		TemporaryCosmeticEffects.TryAdd(effect.Key, effect.Value);
	}

	public bool TryGetCosmeticVoiceOverride(CosmeticEffectsOnPlayers.EFFECTTYPE key, out CosmeticEffectsOnPlayers.CosmeticEffect value)
	{
		if (TemporaryCosmeticEffects == null)
		{
			value = null;
			return false;
		}
		return TemporaryCosmeticEffects.TryGetValue(key, out value);
	}

	public void PlayCosmeticEffectSFX(KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect)
	{
		TemporaryCosmeticEffects.TryAdd(effect.Key, effect.Value);
		int index = UnityEngine.Random.Range(0, effect.Value.sfxAudioClip.Count);
		tagSound.PlayOneShot(effect.Value.sfxAudioClip[index]);
	}

	public void SpawnVFXEffect(KeyValuePair<CosmeticEffectsOnPlayers.EFFECTTYPE, CosmeticEffectsOnPlayers.CosmeticEffect> effect)
	{
		GameObject gameObject = ObjectPools.instance.Instantiate(effect.Value.VFXGameObject, base.transform.position);
		if (gameObject != null)
		{
			gameObject.gameObject.transform.SetParent(base.transform);
			gameObject.gameObject.transform.localPosition = Vector3.zero;
		}
	}

	public void SetPlayerMeshHidden(bool hide)
	{
		mainSkin.enabled = !hide;
		faceSkin.enabled = !hide;
		nameTagAnchor.SetActive(!hide);
		UpdateMatParticles(-1);
	}

	public void SetInvisibleToLocalPlayer(bool invisible)
	{
		if (IsInvisibleToLocalPlayer != invisible)
		{
			IsInvisibleToLocalPlayer = invisible;
			nameTagAnchor.SetActive(!invisible);
			UpdateFriendshipBracelet();
		}
	}

	public void ChangeLayer(string layerName)
	{
		if (layerChanger != null)
		{
			layerChanger.ChangeLayer(base.transform.parent, layerName);
		}
		GTPlayer.Instance.ChangeLayer(layerName);
	}

	public void RestoreLayer()
	{
		if (layerChanger != null)
		{
			layerChanger.RestoreOriginalLayers();
		}
		GTPlayer.Instance.RestoreLayer();
	}

	public void SetHeadBodyOffset()
	{
	}

	public void VRRigResize(float ratioVar)
	{
		ratio *= ratioVar;
	}

	public int ReturnHandPosition()
	{
		return 0 + Mathf.FloorToInt(rightIndex.calcT * 9.99f) + Mathf.FloorToInt(rightMiddle.calcT * 9.99f) * 10 + Mathf.FloorToInt(rightThumb.calcT * 9.99f) * 100 + Mathf.FloorToInt(leftIndex.calcT * 9.99f) * 1000 + Mathf.FloorToInt(leftMiddle.calcT * 9.99f) * 10000 + Mathf.FloorToInt(leftThumb.calcT * 9.99f) * 100000 + leftHandHoldableStatus * 1000000 + rightHandHoldableStatus * 10000000;
	}

	public void OnDestroy()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			if ((bool)currentRopeSwingTarget && (bool)currentRopeSwingTarget.gameObject)
			{
				UnityEngine.Object.Destroy(currentRopeSwingTarget.gameObject);
			}
			ClearRopeData();
		}
	}

	private InputStruct SerializeWriteShared()
	{
		if (myIk == null)
		{
			myIk = GetComponent<GorillaIK>();
		}
		InputStruct result = new InputStruct
		{
			headRotation = BitPackUtils.PackQuaternionForNetwork(head.rigTarget.localRotation),
			rotation = BitPackUtils.PackQuaternionForNetwork(base.transform.rotation),
			usingNewIK = ShouldUseNewIKMethod(myIk.usingUpdatedIK)
		};
		if (result.usingNewIK)
		{
			result.bodyRotation = BitPackUtils.PackQuaternionForNetwork(myIk.targetBodyRot);
			result.rightUpperArmRotation = (short)BitPackUtils.PackRelativePos16(myIk.rightElbowDirection, Vector3.zero, 1f);
			result.leftUpperArmRotation = (short)BitPackUtils.PackRelativePos16(myIk.leftElbowDirection, Vector3.zero, 1f);
		}
		result.rightHandLong = BitPackUtils.PackHandPosRotForNetwork(rightHand.rigTarget.localPosition, rightHand.rigTarget.localRotation);
		result.leftHandLong = BitPackUtils.PackHandPosRotForNetwork(leftHand.rigTarget.localPosition, leftHand.rigTarget.localRotation);
		result.position = BitPackUtils.PackWorldPosForNetwork(base.transform.position);
		result.handPosition = ReturnHandPosition();
		result.taggedById = (short)taggedById;
		int num = Mathf.Clamp(Mathf.RoundToInt(base.transform.rotation.eulerAngles.y + 360f) % 360, 0, 360);
		int num2 = Mathf.RoundToInt(Mathf.Clamp01(SpeakingLoudness) * 255f);
		bool flag = leftHandLink.IsLinkActive() || rightHandLink.IsLinkActive();
		GorillaGameManager activeGameMode = GorillaGameModes.GameMode.ActiveGameMode;
		bool flag2 = (object)activeGameMode != null && activeGameMode.GameType() == GameModeType.PropHunt;
		int packedFields = num + (remoteUseReplacementVoice ? 512 : 0) + ((grabbedRopeIndex != -1) ? 1024 : 0) + (grabbedRopeIsPhotonView ? 2048 : 0) + (flag ? 4096 : 0) + (hoverboardVisual.IsHeld ? 8192 : 0) + (hoverboardVisual.IsLeftHanded ? 16384 : 0) + ((mountedMovingSurfaceId != -1) ? 32768 : 0) + (flag2 ? 65536 : 0) + (propHuntHandFollower.IsLeftHand ? 131072 : 0) + (leftHandLink.CanBeGrabbed() ? 262144 : 0) + (rightHandLink.CanBeGrabbed() ? 524288 : 0) + (leftHandLink.IsTentacleGrab ? 1048576 : 0) + (rightHandLink.IsTentacleGrab ? 2097152 : 0) + (ShowGoldNameTag ? 4194304 : 0) + (num2 << 24);
		result.packedFields = packedFields;
		result.packedCompetitiveData = PackCompetitiveData();
		if (grabbedRopeIndex != -1)
		{
			result.grabbedRopeIndex = grabbedRopeIndex;
			result.ropeBoneIndex = grabbedRopeBoneIndex;
			result.ropeGrabIsLeft = grabbedRopeIsLeft;
			result.ropeGrabIsBody = grabbedRopeIsBody;
			result.ropeGrabOffset = grabbedRopeOffset;
		}
		if (grabbedRopeIndex == -1 && mountedMovingSurfaceId != -1)
		{
			result.grabbedRopeIndex = mountedMovingSurfaceId;
			result.ropeGrabIsLeft = mountedMovingSurfaceIsLeft;
			result.ropeGrabIsBody = mountedMovingSurfaceIsBody;
			result.ropeGrabOffset = mountedMonkeBlockOffset;
		}
		if (hoverboardVisual.IsHeld)
		{
			result.hoverboardPosRot = BitPackUtils.PackHandPosRotForNetwork(hoverboardVisual.NominalLocalPosition, hoverboardVisual.NominalLocalRotation);
			result.hoverboardColor = BitPackUtils.PackColorForNetwork(hoverboardVisual.boardColor);
		}
		if (flag2)
		{
			result.propHuntPosRot = propHuntHandFollower.GetRelativePosRotLong();
		}
		if (flag)
		{
			leftHandLink.Write(out result.isGroundedHand, out result.isGroundedButt, out result.leftHandGrabbedActorNumber, out result.leftGrabbedHandIsLeft);
			rightHandLink.Write(out result.isGroundedHand, out result.isGroundedButt, out result.rightHandGrabbedActorNumber, out result.rightGrabbedHandIsLeft);
			result.lastTouchedGroundAtTime = LastTouchedGroundAtNetworkTime;
			result.lastHandTouchedGroundAtTime = LastHandTouchedGroundAtNetworkTime;
		}
		return result;
	}

	private void SerializeReadShared(InputStruct data)
	{
		if (myIk == null)
		{
			myIk = GetComponent<GorillaIK>();
		}
		head.syncRotation.SetValueSafe(BitPackUtils.UnpackQuaternionFromNetwork(data.headRotation));
		bool usingUpdatedIK = ShouldUseNewIKMethod(data.usingNewIK);
		myIk.usingUpdatedIK = usingUpdatedIK;
		if (myIk.usingUpdatedIK)
		{
			myIk.targetBodyRot.SetValueSafe(BitPackUtils.UnpackQuaternionFromNetwork(data.bodyRotation));
			myIk.leftElbowDirection.SetValueSafe(BitPackUtils.UnpackRelativePos16((ushort)data.leftUpperArmRotation, Vector3.zero, 1f));
			myIk.rightElbowDirection.SetValueSafe(BitPackUtils.UnpackRelativePos16((ushort)data.rightUpperArmRotation, Vector3.zero, 1f));
		}
		BitPackUtils.UnpackHandPosRotFromNetwork(data.rightHandLong, out tempVec, out tempQuat);
		rightHand.syncPos = tempVec;
		rightHand.syncRotation.SetValueSafe(in tempQuat);
		BitPackUtils.UnpackHandPosRotFromNetwork(data.leftHandLong, out tempVec, out tempQuat);
		leftHand.syncPos = tempVec;
		leftHand.syncRotation.SetValueSafe(in tempQuat);
		syncPos = BitPackUtils.UnpackWorldPosFromNetwork(data.position);
		handSync = data.handPosition;
		int packedFields = data.packedFields;
		if (GTPlayerTransform.UseNetRotation)
		{
			syncRotation.SetValueSafe(BitPackUtils.UnpackQuaternionFromNetwork(data.rotation));
		}
		else
		{
			int num = packedFields & 0x1FF;
			syncRotation.eulerAngles = SanitizeVector3(new Vector3(0f, num, 0f));
		}
		remoteUseReplacementVoice = (packedFields & 0x200) != 0;
		if ((packedFields & 0x400000) != 0 && SubscriptionManager.GetSubscriptionDetails(this).active)
		{
			playerText1.color = SubscriptionManager.SUBSCRIBER_NAME_COLOR;
		}
		else
		{
			playerText1.color = Color.white;
		}
		int num2 = (packedFields >> 24) & 0xFF;
		SpeakingLoudness = (float)num2 / 255f;
		UpdateReplacementVoice();
		UnpackCompetitiveData(data.packedCompetitiveData);
		taggedById = data.taggedById;
		bool num3 = (packedFields & 0x400) != 0;
		grabbedRopeIsPhotonView = (packedFields & 0x800) != 0;
		if (num3)
		{
			grabbedRopeIndex = data.grabbedRopeIndex;
			grabbedRopeBoneIndex = data.ropeBoneIndex;
			grabbedRopeIsLeft = data.ropeGrabIsLeft;
			grabbedRopeIsBody = data.ropeGrabIsBody;
			grabbedRopeOffset.SetValueSafe(in data.ropeGrabOffset);
		}
		else
		{
			grabbedRopeIndex = -1;
		}
		bool flag = (packedFields & 0x8000) != 0;
		if (!num3 && flag)
		{
			mountedMovingSurfaceId = data.grabbedRopeIndex;
			mountedMovingSurfaceIsLeft = data.ropeGrabIsLeft;
			mountedMovingSurfaceIsBody = data.ropeGrabIsBody;
			mountedMonkeBlockOffset.SetValueSafe(in data.ropeGrabOffset);
			movingSurfaceIsMonkeBlock = data.movingSurfaceIsMonkeBlock;
		}
		else
		{
			mountedMovingSurfaceId = -1;
		}
		bool num4 = (packedFields & 0x2000) != 0;
		bool isHeldLeftHanded = (packedFields & 0x4000) != 0;
		if (num4)
		{
			BitPackUtils.UnpackHandPosRotFromNetwork(data.hoverboardPosRot, out var localPos, out var handRot);
			Color boardColor = BitPackUtils.UnpackColorFromNetwork(data.hoverboardColor);
			if (handRot.IsValid())
			{
				hoverboardVisual.SetIsHeld(isHeldLeftHanded, localPos.ClampMagnitudeSafe(1f), handRot, boardColor);
			}
		}
		else if (hoverboardVisual.gameObject.activeSelf)
		{
			hoverboardVisual.SetNotHeld();
		}
		if ((packedFields & 0x10000) != 0)
		{
			bool isLeftHand = (packedFields & 0x20000) != 0;
			BitPackUtils.UnpackHandPosRotFromNetwork(data.propHuntPosRot, out var localPos2, out var handRot2);
			propHuntHandFollower.SetProp(isLeftHand, localPos2, handRot2);
		}
		if (grabbedRopeIsPhotonView)
		{
			localGrabOverrideBlend = -1f;
		}
		leftHandLink.Read(leftHand.syncPos, syncRotation, syncPos, data.isGroundedHand, data.isGroundedButt, (packedFields & 0x40000) != 0, (packedFields & 0x100000) != 0, data.leftHandGrabbedActorNumber, data.leftGrabbedHandIsLeft);
		rightHandLink.Read(rightHand.syncPos, syncRotation, syncPos, data.isGroundedHand, data.isGroundedButt, (packedFields & 0x80000) != 0, (packedFields & 0x200000) != 0, data.rightHandGrabbedActorNumber, data.rightGrabbedHandIsLeft);
		LastTouchedGroundAtNetworkTime = data.lastTouchedGroundAtTime;
		LastHandTouchedGroundAtNetworkTime = data.lastHandTouchedGroundAtTime;
		UpdateRopeData();
		UpdateMovingMonkeBlockData();
		AddVelocityToQueue(syncPos, data.serverTimeStamp);
	}

	private bool ShouldUseNewIKMethod(bool isReceivingNewIKData)
	{
		if (isOfflineVRRig)
		{
			bool num = SubscriptionManager.IsLocalSubscribed();
			bool subscriptionSettingBool = SubscriptionManager.GetSubscriptionSettingBool(SubscriptionManager.SubscriptionFeatures.IOBT);
			if (num && subscriptionSettingBool && myIk != null)
			{
				return myIk.usingUpdatedIK;
			}
			return false;
		}
		return isReceivingNewIKData;
	}

	void IWrappedSerializable.OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		InputStruct inputStruct = SerializeWriteShared();
		stream.SendNext(inputStruct.headRotation);
		stream.SendNext(inputStruct.rotation);
		stream.SendNext(inputStruct.usingNewIK);
		if (inputStruct.usingNewIK)
		{
			stream.SendNext(inputStruct.bodyRotation);
			stream.SendNext(inputStruct.leftUpperArmRotation);
			stream.SendNext(inputStruct.rightUpperArmRotation);
		}
		stream.SendNext(inputStruct.rightHandLong);
		stream.SendNext(inputStruct.leftHandLong);
		stream.SendNext(inputStruct.position);
		stream.SendNext(inputStruct.handPosition);
		stream.SendNext(inputStruct.packedFields);
		stream.SendNext(inputStruct.packedCompetitiveData);
		if (grabbedRopeIndex != -1)
		{
			stream.SendNext(inputStruct.grabbedRopeIndex);
			stream.SendNext(inputStruct.ropeBoneIndex);
			stream.SendNext(inputStruct.ropeGrabIsLeft);
			stream.SendNext(inputStruct.ropeGrabIsBody);
			stream.SendNext(inputStruct.ropeGrabOffset);
		}
		else if (mountedMovingSurfaceId != -1)
		{
			stream.SendNext(inputStruct.grabbedRopeIndex);
			stream.SendNext(inputStruct.ropeGrabIsLeft);
			stream.SendNext(inputStruct.ropeGrabIsBody);
			stream.SendNext(inputStruct.ropeGrabOffset);
			stream.SendNext(inputStruct.movingSurfaceIsMonkeBlock);
		}
		if ((inputStruct.packedFields & 0x2000) != 0)
		{
			stream.SendNext(inputStruct.hoverboardPosRot);
			stream.SendNext(inputStruct.hoverboardColor);
		}
		if ((inputStruct.packedFields & 0x1000) != 0)
		{
			stream.SendNext(inputStruct.isGroundedHand);
			stream.SendNext(inputStruct.isGroundedButt);
			stream.SendNext(inputStruct.leftHandGrabbedActorNumber);
			stream.SendNext(inputStruct.leftGrabbedHandIsLeft);
			stream.SendNext(inputStruct.rightHandGrabbedActorNumber);
			stream.SendNext(inputStruct.rightGrabbedHandIsLeft);
			stream.SendNext(inputStruct.lastTouchedGroundAtTime);
			stream.SendNext(inputStruct.lastHandTouchedGroundAtTime);
		}
		if ((inputStruct.packedFields & 0x10000) != 0)
		{
			stream.SendNext(inputStruct.propHuntPosRot);
		}
	}

	void IWrappedSerializable.OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		_ = info.SentServerTime;
		InputStruct data = new InputStruct
		{
			headRotation = (int)stream.ReceiveNext(),
			rotation = (int)stream.ReceiveNext(),
			usingNewIK = (bool)stream.ReceiveNext()
		};
		if (data.usingNewIK)
		{
			data.bodyRotation = (int)stream.ReceiveNext();
			data.leftUpperArmRotation = (short)stream.ReceiveNext();
			data.rightUpperArmRotation = (short)stream.ReceiveNext();
		}
		data.rightHandLong = (long)stream.ReceiveNext();
		data.leftHandLong = (long)stream.ReceiveNext();
		data.position = (long)stream.ReceiveNext();
		data.handPosition = (int)stream.ReceiveNext();
		data.packedFields = (int)stream.ReceiveNext();
		data.packedCompetitiveData = (short)stream.ReceiveNext();
		bool num = (data.packedFields & 0x400) != 0;
		bool flag = (data.packedFields & 0x8000) != 0;
		if (num)
		{
			data.grabbedRopeIndex = (int)stream.ReceiveNext();
			data.ropeBoneIndex = (int)stream.ReceiveNext();
			data.ropeGrabIsLeft = (bool)stream.ReceiveNext();
			data.ropeGrabIsBody = (bool)stream.ReceiveNext();
			data.ropeGrabOffset = (Vector3)stream.ReceiveNext();
		}
		else if (flag)
		{
			data.grabbedRopeIndex = (int)stream.ReceiveNext();
			data.ropeGrabIsLeft = (bool)stream.ReceiveNext();
			data.ropeGrabIsBody = (bool)stream.ReceiveNext();
			data.ropeGrabOffset = (Vector3)stream.ReceiveNext();
		}
		if ((data.packedFields & 0x2000) != 0)
		{
			data.hoverboardPosRot = (long)stream.ReceiveNext();
			data.hoverboardColor = (short)stream.ReceiveNext();
		}
		if ((data.packedFields & 0x1000) != 0)
		{
			data.isGroundedHand = (bool)stream.ReceiveNext();
			data.isGroundedButt = (bool)stream.ReceiveNext();
			data.leftHandGrabbedActorNumber = (int)stream.ReceiveNext();
			data.leftGrabbedHandIsLeft = (bool)stream.ReceiveNext();
			data.rightHandGrabbedActorNumber = (int)stream.ReceiveNext();
			data.rightGrabbedHandIsLeft = (bool)stream.ReceiveNext();
			data.lastTouchedGroundAtTime = (float)stream.ReceiveNext();
			data.lastHandTouchedGroundAtTime = (float)stream.ReceiveNext();
		}
		if ((data.packedFields & 0x10000) != 0)
		{
			data.propHuntPosRot = (long)stream.ReceiveNext();
		}
		data.serverTimeStamp = info.SentServerTime;
		SerializeReadShared(data);
	}

	public object OnSerializeWrite()
	{
		InputStruct inputStruct = SerializeWriteShared();
		double serverTimeStamp = (double)(uint)NetworkSystem.Instance.SimTick / 1000.0;
		inputStruct.serverTimeStamp = serverTimeStamp;
		return inputStruct;
	}

	public void OnSerializeRead(object objectData)
	{
		InputStruct data = (InputStruct)objectData;
		SerializeReadShared(data);
	}

	private void UpdateExtrapolationTarget()
	{
		float num = (float)(NetworkSystem.Instance.SimTime - remoteLatestTimestamp);
		num -= 0.15f;
		num = Mathf.Clamp(num, -0.5f, 0.5f);
		syncPos += remoteVelocity * num;
		remoteCorrectionNeeded = syncPos - base.transform.position;
		if (remoteCorrectionNeeded.magnitude > 1.5f && grabbedRopeIndex <= 0)
		{
			base.transform.position = syncPos;
			remoteCorrectionNeeded = Vector3.zero;
		}
	}

	private void UpdateRopeData()
	{
		if (previousGrabbedRope == grabbedRopeIndex && previousGrabbedRopeBoneIndex == grabbedRopeBoneIndex && previousGrabbedRopeWasLeft == grabbedRopeIsLeft && previousGrabbedRopeWasBody == grabbedRopeIsBody)
		{
			return;
		}
		ClearRopeData();
		if (grabbedRopeIndex != -1)
		{
			GorillaRopeSwing result;
			if (grabbedRopeIsPhotonView)
			{
				PhotonView photonView = PhotonView.Find(grabbedRopeIndex);
				HandHoldXSceneRef component2;
				VRRigSerializer component3;
				if (photonView.TryGetComponent<GorillaClimbable>(out var _))
				{
					currentHoldParent = photonView.transform;
				}
				else if (photonView.TryGetComponent<HandHoldXSceneRef>(out component2))
				{
					currentHoldParent = component2.targetObject?.transform;
				}
				else if ((bool)photonView && photonView.TryGetComponent<VRRigSerializer>(out component3))
				{
					currentHoldParent = ((grabbedRopeBoneIndex == 1) ? component3.VRRig.leftHandHoldsPlayer.transform : component3.VRRig.rightHandHoldsPlayer.transform);
				}
			}
			else if (RopeSwingManager.instance.TryGetRope(grabbedRopeIndex, out result) && result != null)
			{
				if (currentRopeSwingTarget == null || currentRopeSwingTarget.gameObject == null)
				{
					currentRopeSwingTarget = new GameObject("RopeSwingTarget").transform;
				}
				if (result.AttachRemotePlayer(creator.ActorNumber, grabbedRopeBoneIndex, currentRopeSwingTarget, grabbedRopeOffset))
				{
					currentRopeSwing = result;
				}
				lastRopeGrabTimer = 0f;
			}
		}
		else if (previousGrabbedRope != -1)
		{
			PhotonView photonView2 = PhotonView.Find(previousGrabbedRope);
			if ((bool)photonView2 && photonView2.TryGetComponent<VRRigSerializer>(out var component4) && component4.VRRig == LocalRig)
			{
				EquipmentInteractor.instance.ForceDropEquipment(bodyHolds);
				EquipmentInteractor.instance.ForceDropEquipment(leftHolds);
				EquipmentInteractor.instance.ForceDropEquipment(rightHolds);
			}
		}
		shouldLerpToRope = true;
		previousGrabbedRope = grabbedRopeIndex;
		previousGrabbedRopeBoneIndex = grabbedRopeBoneIndex;
		previousGrabbedRopeWasLeft = grabbedRopeIsLeft;
		previousGrabbedRopeWasBody = grabbedRopeIsBody;
	}

	private void UpdateMovingMonkeBlockData()
	{
		if (mountedMonkeBlockOffset.sqrMagnitude > 2f)
		{
			mountedMovingSurfaceId = -1;
			mountedMovingSurfaceIsLeft = false;
			mountedMovingSurfaceIsBody = false;
			mountedMonkeBlock = null;
			mountedMovingSurface = null;
		}
		if (prevMovingSurfaceID == mountedMovingSurfaceId && movingSurfaceWasBody == mountedMovingSurfaceIsBody && movingSurfaceWasLeft == mountedMovingSurfaceIsLeft && movingSurfaceWasMonkeBlock == movingSurfaceIsMonkeBlock)
		{
			return;
		}
		if (mountedMovingSurfaceId == -1)
		{
			mountedMovingSurfaceIsLeft = false;
			mountedMovingSurfaceIsBody = false;
			mountedMonkeBlock = null;
			mountedMovingSurface = null;
		}
		else if (movingSurfaceIsMonkeBlock)
		{
			mountedMonkeBlock = null;
			if (BuilderTable.TryGetBuilderTableForZone(zoneEntity.currentZone, out var table))
			{
				mountedMonkeBlock = table.GetPiece(mountedMovingSurfaceId);
			}
			if (mountedMonkeBlock == null)
			{
				mountedMovingSurfaceId = -1;
				mountedMovingSurfaceIsLeft = false;
				mountedMovingSurfaceIsBody = false;
				mountedMonkeBlock = null;
				mountedMovingSurface = null;
			}
		}
		else if (MovingSurfaceManager.instance == null || !MovingSurfaceManager.instance.TryGetMovingSurface(mountedMovingSurfaceId, out mountedMovingSurface))
		{
			mountedMovingSurfaceId = -1;
			mountedMovingSurfaceIsLeft = false;
			mountedMovingSurfaceIsBody = false;
			mountedMonkeBlock = null;
			mountedMovingSurface = null;
		}
		if (mountedMovingSurfaceId != -1 && prevMovingSurfaceID == -1)
		{
			shouldLerpToMovingSurface = true;
			lastMountedSurfaceTimer = 0f;
		}
		prevMovingSurfaceID = mountedMovingSurfaceId;
		movingSurfaceWasLeft = mountedMovingSurfaceIsLeft;
		movingSurfaceWasBody = mountedMovingSurfaceIsBody;
		movingSurfaceWasMonkeBlock = movingSurfaceIsMonkeBlock;
	}

	public static void AttachLocalPlayerToMovingSurface(int blockId, bool isLeft, bool isBody, Vector3 offset, bool isMonkeBlock)
	{
		if (GorillaTagger.hasInstance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.mountedMovingSurfaceId = blockId;
			GorillaTagger.Instance.offlineVRRig.mountedMovingSurfaceIsLeft = isLeft;
			GorillaTagger.Instance.offlineVRRig.mountedMovingSurfaceIsBody = isBody;
			GorillaTagger.Instance.offlineVRRig.movingSurfaceIsMonkeBlock = isMonkeBlock;
			GorillaTagger.Instance.offlineVRRig.mountedMonkeBlockOffset = offset;
		}
	}

	public static void DetachLocalPlayerFromMovingSurface()
	{
		if (GorillaTagger.hasInstance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.mountedMovingSurfaceId = -1;
		}
	}

	public static void AttachLocalPlayerToPhotonView(PhotonView view, XRNode xrNode, Vector3 offset, Vector3 velocity)
	{
		if (GorillaTagger.hasInstance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = view.ViewID;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIsLeft = xrNode == XRNode.LeftHand;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeOffset = offset;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIsPhotonView = true;
		}
	}

	public static void DetachLocalPlayerFromPhotonView()
	{
		if (GorillaTagger.hasInstance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = -1;
		}
	}

	private void ClearRopeData()
	{
		if ((bool)currentRopeSwing)
		{
			currentRopeSwing.DetachRemotePlayer(creator.ActorNumber);
		}
		if ((bool)currentRopeSwingTarget)
		{
			currentRopeSwingTarget.SetParent(null);
		}
		currentRopeSwing = null;
		currentHoldParent = null;
	}

	public void ChangeMaterial(int materialIndex, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			ChangeMaterialLocal(materialIndex);
		}
	}

	public void UpdateFrozenEffect(bool enable)
	{
		if (frozenEffect != null && ((!frozenEffect.activeSelf && enable) || (frozenEffect.activeSelf && !enable)))
		{
			frozenEffect.SetActive(enable);
			if (enable)
			{
				frozenTimeElapsed = 0f;
			}
			else
			{
				Vector3 localScale = frozenEffect.transform.localScale;
				localScale = new Vector3(localScale.x, frozenEffectMinY, localScale.z);
				frozenEffect.transform.localScale = localScale;
			}
		}
		if (iceCubeLeft != null && ((!iceCubeLeft.activeSelf && enable) || (iceCubeLeft.activeSelf && !enable)))
		{
			iceCubeLeft.SetActive(enable);
		}
		if (iceCubeRight != null && ((!iceCubeRight.activeSelf && enable) || (iceCubeRight.activeSelf && !enable)))
		{
			iceCubeRight.SetActive(enable);
		}
	}

	public void ForceResetFrozenEffect()
	{
		frozenEffect.SetActive(value: false);
		iceCubeRight.SetActive(value: false);
		iceCubeLeft.SetActive(value: false);
	}

	public void ChangeMaterialLocal(int materialIndex)
	{
		if (setMatIndex != materialIndex)
		{
			int arg = setMatIndex;
			setMatIndex = materialIndex;
			if (setMatIndex > -1 && setMatIndex < materialsToChangeTo.Length)
			{
				bodyRenderer.SetMaterialIndex(materialIndex);
			}
			UpdateMatParticles(materialIndex);
			if (materialIndex > 0 && LocalRig != this)
			{
				PlayTaggedEffect();
			}
			OnMaterialIndexChanged?.Invoke(arg, setMatIndex);
		}
	}

	public void PlayTaggedEffect()
	{
		TagEffectPack tagEffectPack = null;
		quaternion quaternion2 = base.transform.rotation;
		TagEffectsLibrary.EffectType effectType = ((!(LocalRig == this)) ? TagEffectsLibrary.EffectType.THIRD_PERSON : TagEffectsLibrary.EffectType.FIRST_PERSON);
		if (GorillaGameManager.instance != null && OwningNetPlayer != null)
		{
			GorillaGameManager.instance.lastTaggedActorNr.TryGetValue(OwningNetPlayer.ActorNumber, out taggedById);
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(taggedById);
		if (player != null && VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			tagEffectPack = playerRig.Rig.CosmeticEffectPack;
			if ((bool)tagEffectPack && tagEffectPack.shouldFaceTagger && effectType == TagEffectsLibrary.EffectType.THIRD_PERSON)
			{
				quaternion2 = Quaternion.LookRotation((playerRig.Rig.transform.position - base.transform.position).normalized);
			}
		}
		TagEffectsLibrary.PlayEffect(base.transform, isLeftHand: false, scaleFactor, effectType, CosmeticEffectPack, tagEffectPack, quaternion2);
	}

	public void ToggleMatParticles(bool enabled)
	{
		if (lavaParticleSystem != null)
		{
			ToggleParticleSystem(lavaParticleSystem, enabled);
		}
		if (rockParticleSystem != null)
		{
			ToggleParticleSystem(rockParticleSystem, enabled);
		}
		if (iceParticleSystem != null)
		{
			ToggleParticleSystem(iceParticleSystem, enabled);
		}
		if (snowFlakeParticleSystem != null)
		{
			ToggleParticleSystem(snowFlakeParticleSystem, enabled);
		}
	}

	private void ToggleParticleSystem(ParticleSystem ps, bool enabled)
	{
		ParticleSystem.EmissionModule emission = ps.emission;
		emission.enabled = enabled;
	}

	public void UpdateMatParticles(int materialIndex)
	{
		if (lavaParticleSystem != null)
		{
			if (!isOfflineVRRig && materialIndex == 2 && lavaParticleSystem.isStopped)
			{
				lavaParticleSystem.Play();
			}
			else if (!isOfflineVRRig && lavaParticleSystem.isPlaying)
			{
				lavaParticleSystem.Stop();
			}
		}
		if (rockParticleSystem != null)
		{
			if (!isOfflineVRRig && materialIndex == 1 && rockParticleSystem.isStopped)
			{
				rockParticleSystem.Play();
			}
			else if (!isOfflineVRRig && rockParticleSystem.isPlaying)
			{
				rockParticleSystem.Stop();
			}
		}
		if (iceParticleSystem != null)
		{
			if (!isOfflineVRRig && materialIndex == 3 && rockParticleSystem.isStopped)
			{
				iceParticleSystem.Play();
			}
			else if (!isOfflineVRRig && iceParticleSystem.isPlaying)
			{
				iceParticleSystem.Stop();
			}
		}
		if (snowFlakeParticleSystem != null)
		{
			if (!isOfflineVRRig && materialIndex == 14 && snowFlakeParticleSystem.isStopped)
			{
				snowFlakeParticleSystem.Play();
			}
			else if (!isOfflineVRRig && snowFlakeParticleSystem.isPlaying)
			{
				snowFlakeParticleSystem.Stop();
			}
		}
	}

	public void InitializeNoobMaterial(float red, float green, float blue, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_InitializeNoobMaterial");
		NetworkSystem.Instance.GetPlayer(info.senderID);
		string userID = NetworkSystem.Instance.GetUserID(info.senderID);
		if (info.senderID == NetworkSystem.Instance.GetOwningPlayerID(rigSerializer.gameObject) && (!initialized || (initialized && GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(userID)) || (initialized && CosmeticWardrobeProximityDetector.IsUserNearWardrobe(info.senderID))))
		{
			initialized = true;
			blue = blue.ClampSafe(0f, 1f);
			red = red.ClampSafe(0f, 1f);
			green = green.ClampSafe(0f, 1f);
			InitializeNoobMaterialLocal(red, green, blue);
		}
	}

	public void InitializeNoobMaterialLocal(float red, float green, float blue)
	{
		Color color = new Color(red, green, blue);
		color.r = Mathf.Clamp(color.r, 0f, 1f);
		color.g = Mathf.Clamp(color.g, 0f, 1f);
		color.b = Mathf.Clamp(color.b, 0f, 1f);
		bodyRenderer.UpdateColor(color);
		SetColor(color);
		bool isNamePermissionEnabled = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags);
		UpdateName(isNamePermissionEnabled);
	}

	public void UpdateNameSafeAccount(bool isSafeAccount)
	{
		UpdateName(!isSafeAccount);
	}

	public void UpdateName(bool isNamePermissionEnabled)
	{
		if (!isOfflineVRRig && creator != null)
		{
			string text = ((isNamePermissionEnabled && GorillaComputer.instance.NametagsEnabled) ? creator.NickName : creator.DefaultName);
			playerNameVisible = NormalizeName(doIt: true, text);
		}
		else if (showName && NetworkSystem.Instance != null)
		{
			playerNameVisible = ((isNamePermissionEnabled && GorillaComputer.instance.NametagsEnabled) ? NetworkSystem.Instance.GetMyNickName() : NetworkSystem.Instance.GetMyDefaultName());
		}
		SetNameTagText(playerNameVisible);
		if (creator != null)
		{
			creator.SanitizedNickName = playerNameVisible;
		}
		this.OnPlayerNameVisibleChanged?.Invoke();
	}

	public void SetNameTagText(string name)
	{
		playerNameVisible = name;
		playerText1.text = name;
		OnNameChanged?.Invoke(rigContainer);
	}

	public void UpdateName()
	{
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Custom_Nametags);
		bool isNamePermissionEnabled = (permissionDataByFeature.Enabled || permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PLAYER) && permissionDataByFeature.ManagedBy != Permission.ManagedByEnum.PROHIBITED;
		UpdateName(isNamePermissionEnabled);
	}

	public string NormalizeName(bool doIt, string text)
	{
		if (doIt)
		{
			int length = text.Length;
			text = new string(Array.FindAll(text.ToCharArray(), (char c) => Utils.IsASCIILetterOrDigit(c)));
			int length2 = text.Length;
			if (length2 > 0 && length == length2 && GorillaComputer.instance.CheckAutoBanListForName(text))
			{
				if (text.Length > 12)
				{
					text = text.Substring(0, 12);
				}
				text = text.ToUpper();
			}
			else
			{
				text = "BADGORILLA";
			}
		}
		return text;
	}

	public void SetJumpLimitLocal(float maxJumpSpeed)
	{
		GTPlayer.Instance.maxJumpSpeed = maxJumpSpeed;
	}

	public void SetJumpMultiplierLocal(float jumpMultiplier)
	{
		GTPlayer.Instance.jumpMultiplier = jumpMultiplier;
	}

	public void RequestMaterialColor(int askingPlayerID, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RequestMaterialColor");
		Player playerRef = ((PunNetPlayer)NetworkSystem.Instance.GetPlayer(info.senderID)).PlayerRef;
		if (netView.IsMine)
		{
			netView.GetView.RPC("RPC_InitializeNoobMaterial", playerRef, myDefaultSkinMaterialInstance.color.r, myDefaultSkinMaterialInstance.color.g, myDefaultSkinMaterialInstance.color.b);
		}
	}

	public void RequestCosmetics(PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (!netView.IsMine || !CosmeticsController.hasInstance)
		{
			return;
		}
		if (CosmeticsController.instance.isHidingCosmeticsFromRemotePlayers)
		{
			netView.SendRPC("RPC_HideAllCosmetics", info.Sender);
			return;
		}
		int[] array = CosmeticsController.instance.currentWornSet.ToPackedIDArray();
		int[] array2 = CosmeticsController.instance.tryOnSet.ToPackedIDArray();
		netView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", player, array, array2, false);
		CosmeticCollectionDisplay.GetDisplaysForRig(GorillaTagger.Instance.offlineVRRig, scratchDisplayList);
		if (scratchDisplayList.Count > 0)
		{
			int num = scratchDisplayList.Count * 3;
			if (cycleStatesArray.Length != num)
			{
				cycleStatesArray = new int[num];
			}
			for (int i = 0; i < scratchDisplayList.Count; i++)
			{
				CosmeticCollectionDisplay cosmeticCollectionDisplay = scratchDisplayList[i];
				string parentPlayFabID = cosmeticCollectionDisplay.ParentPlayFabID;
				cycleStatesArray[i * 3] = parentPlayFabID[0] - 65 + 26 * (parentPlayFabID[1] - 65 + 26 * (parentPlayFabID[2] - 65 + 26 * (parentPlayFabID[3] - 65 + 26 * (parentPlayFabID[4] - 65))));
				CosmeticsController.CosmeticItem? activeCollectable = cosmeticCollectionDisplay.ActiveCollectable;
				cycleStatesArray[i * 3 + 1] = ((activeCollectable.HasValue && CosmeticsController.hasInstance) ? CosmeticsController.instance.GetCanonicalCollectableIndex(parentPlayFabID, activeCollectable.Value.itemName) : cosmeticCollectionDisplay.ActiveIndex);
				cycleStatesArray[i * 3 + 2] = cosmeticCollectionDisplay.VisibleMask;
			}
			netView.SendRPC("RPC_UpdateCosmeticsWithCollectablesPacked", player, cycleStatesArray);
		}
	}

	public void PlayTagSoundLocal(int soundIndex, float soundVolume, bool stopCurrentAudio)
	{
		if (soundIndex >= 0 && soundIndex < clipToPlay.Length)
		{
			tagSound.volume = Mathf.Min(0.25f, soundVolume);
			if (stopCurrentAudio)
			{
				tagSound.Stop();
			}
			tagSound.GTPlayOneShot(clipToPlay[soundIndex]);
		}
	}

	public void AssignDrumToMusicDrums(int drumIndex, AudioSource drum)
	{
		if (drumIndex >= 0 && drumIndex < musicDrums.Length && drum != null)
		{
			musicDrums[drumIndex] = drum;
		}
	}

	public void PlayDrum(int drumIndex, float drumVolume, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_PlayDrum");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			senderRig = playerRig.Rig;
		}
		if (senderRig == null || senderRig.muted)
		{
			return;
		}
		if (drumIndex < 0 || drumIndex >= musicDrums.Length || (senderRig.transform.position - base.transform.position).sqrMagnitude > 9f || !float.IsFinite(drumVolume))
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent drum", player.UserId, player.NickName);
			return;
		}
		AudioSource audioSource = (netView.IsMine ? GorillaTagger.Instance.offlineVRRig.musicDrums[drumIndex] : musicDrums[drumIndex]);
		if (audioSource.gameObject.activeInHierarchy)
		{
			float instrumentVolume = GorillaComputer.instance.instrumentVolume;
			audioSource.time = 0f;
			audioSource.volume = Mathf.Max(Mathf.Min(instrumentVolume, drumVolume * instrumentVolume), 0f);
			audioSource.GTPlay();
		}
	}

	public int AssignInstrumentToInstrumentSelfOnly(TransferrableObject instrument)
	{
		if (instrument == null)
		{
			return -1;
		}
		if (!instrumentSelfOnly.Contains(instrument))
		{
			instrumentSelfOnly.Add(instrument);
		}
		return instrumentSelfOnly.IndexOf(instrument);
	}

	public void PlaySelfOnlyInstrument(int selfOnlyIndex, int noteIndex, float instrumentVol, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_PlaySelfOnlyInstrument");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (player != netView.Owner || muted)
		{
			return;
		}
		if (selfOnlyIndex >= 0 && selfOnlyIndex < instrumentSelfOnly.Count && float.IsFinite(instrumentVol))
		{
			if (instrumentSelfOnly[selfOnlyIndex].gameObject.activeSelf)
			{
				instrumentSelfOnly[selfOnlyIndex].PlayNote(noteIndex, Mathf.Max(Mathf.Min(GorillaComputer.instance.instrumentVolume, instrumentVol * GorillaComputer.instance.instrumentVolume), 0f) / 2f);
			}
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent self only instrument", player.UserId, player.NickName);
		}
	}

	public void PlayHandTapLocal(int audioClipIndex, bool isLeftHand, float tapVolume)
	{
		if (audioClipIndex > -1 && audioClipIndex < GTPlayer.Instance.materialData.Count)
		{
			GTPlayer.MaterialData materialData = GTPlayer.Instance.materialData[audioClipIndex];
			AudioSource obj = (isLeftHand ? leftHandPlayer : rightHandPlayer);
			obj.volume = tapVolume;
			AudioClip clip = (materialData.overrideAudio ? materialData.audio : GTPlayer.Instance.materialData[0].audio);
			obj.GTPlayOneShot(clip);
		}
	}

	internal HandEffectContext GetHandEffect(bool isLeftHand, StiltID stiltID)
	{
		if (stiltID == StiltID.None)
		{
			if (!isLeftHand)
			{
				return RightHandEffect;
			}
			return LeftHandEffect;
		}
		if (!isLeftHand)
		{
			return ExtraRightHandEffect;
		}
		return ExtraLeftHandEffect;
	}

	internal void SetHandEffectData(HandEffectContext effectContext, int audioClipIndex, bool isDownTap, bool isLeftHand, StiltID stiltID, float handTapVolume, float handTapSpeed, Vector3 dirFromHitToHand)
	{
		VRMap vRMap = (isLeftHand ? leftHand : rightHand);
		Vector3 vector = dirFromHitToHand * tapPointDistance * scaleFactor;
		if (isOfflineVRRig)
		{
			Vector3 vector2 = vRMap.rigTarget.rotation * vRMap.trackingPositionOffset * scaleFactor;
			Vector3 position = (effectContext.position = ((stiltID != StiltID.None) ? GTPlayer.Instance.GetHandPosition(isLeftHand, stiltID) : (vRMap.rigTarget.position - vector2 + vector)));
			effectContext.handSoundSource.transform.position = position;
		}
		else
		{
			Quaternion obj = vRMap.rigTarget.parent.rotation * vRMap.syncRotation;
			Vector3 vector3 = netSyncPos.GetPredictedFuture() - base.transform.position;
			Vector3 vector2 = obj * vRMap.trackingPositionOffset * scaleFactor;
			effectContext.position = vRMap.rigTarget.parent.TransformPoint(vRMap.netSyncPos.GetPredictedFuture()) - vector2 + vector + vector3;
		}
		GTPlayer.MaterialData handSurfaceData = GetHandSurfaceData(audioClipIndex);
		HandTapOverrides handTapOverrides = (isDownTap ? effectContext.DownTapOverrides : effectContext.UpTapOverrides);
		effectContext.prefabHashes[0] = (handTapOverrides.overrideSurfacePrefab ? handTapOverrides.surfaceTapPrefab : GTPlayer.Instance.materialDatasSO.surfaceEffects[handSurfaceData.surfaceEffectIndex]);
		effectContext.prefabHashes[1] = (handTapOverrides.overrideGamemodePrefab ? ((int)handTapOverrides.gamemodeTapPrefab) : ((RoomSystem.JoinedRoom && GorillaGameModes.GameMode.ActiveGameMode.IsNotNull()) ? GorillaGameModes.GameMode.ActiveGameMode.SpecialHandFX(creator, rigContainer) : (-1)));
		effectContext.soundFX = (handTapOverrides.overrideSound ? handTapOverrides.tapSound : handSurfaceData.audio);
		effectContext.isDownTap = isDownTap;
		effectContext.isLeftHand = isLeftHand;
		effectContext.soundVolume = handTapVolume * handSpeedToVolumeModifier;
		effectContext.soundPitch = 1f;
		effectContext.speed = handTapSpeed;
		effectContext.color = playerColor;
	}

	internal GTPlayer.MaterialData GetHandSurfaceData(int index)
	{
		List<GTPlayer.MaterialData> materialData = GTPlayer.Instance.materialData;
		GTPlayer.MaterialData result = ((index < 0 || index >= materialData.Count) ? materialData[0] : materialData[index]);
		if (!result.overrideAudio)
		{
			result = materialData[0];
		}
		return result;
	}

	public void PlaySplashEffect(Vector3 splashPosition, Quaternion splashRotation, float splashScale, float boundingRadius, bool bigSplash, bool enteringWater, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_PlaySplashEffect");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (player == netView.Owner && splashPosition.IsValid(10000f) && splashRotation.IsValid() && float.IsFinite(splashScale) && float.IsFinite(boundingRadius))
		{
			if (!((base.transform.position - splashPosition).sqrMagnitude < 9f))
			{
				return;
			}
			float time = Time.time;
			int num = -1;
			float num2 = time + 10f;
			for (int i = 0; i < splashEffectTimes.Length; i++)
			{
				if (splashEffectTimes[i] < num2)
				{
					num2 = splashEffectTimes[i];
					num = i;
				}
			}
			if (time - 0.5f > num2)
			{
				splashEffectTimes[num] = time;
				boundingRadius = Mathf.Clamp(boundingRadius, 0.0001f, 0.5f);
				ObjectPools.instance.Instantiate(GTPlayer.Instance.waterParams.rippleEffect, splashPosition, splashRotation, GTPlayer.Instance.waterParams.rippleEffectScale * boundingRadius * 2f);
				splashScale = Mathf.Clamp(splashScale, 1E-05f, 1f);
				ObjectPools.instance.Instantiate(GTPlayer.Instance.waterParams.splashEffect, splashPosition, splashRotation, splashScale).GetComponent<WaterSplashEffect>().PlayEffect(bigSplash, enteringWater, splashScale);
			}
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent splash effect", player.UserId, player.NickName);
		}
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_EnableNonCosmeticHandItem(bool enable, bool isLeftHand, RpcInfo info = default(RpcInfo))
	{
		PhotonMessageInfoWrapped info2 = new PhotonMessageInfoWrapped(info);
		IncrementRPC(info2, "EnableNonCosmeticHandItem");
		if (info2.Sender == creator)
		{
			senderRig = GorillaGameManager.StaticFindRigForPlayer(info2.Sender);
			if (!(senderRig == null))
			{
				if (isLeftHand && (bool)nonCosmeticLeftHandItem)
				{
					senderRig.nonCosmeticLeftHandItem.EnableItem(enable);
				}
				else if (!isLeftHand && (bool)nonCosmeticRightHandItem)
				{
					senderRig.nonCosmeticRightHandItem.EnableItem(enable);
				}
			}
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent Enable Non Cosmetic Hand Item", info2.Sender.UserId, info2.Sender.NickName);
		}
	}

	[PunRPC]
	public void EnableNonCosmeticHandItemRPC(bool enable, bool isLeftHand, PhotonMessageInfoWrapped info)
	{
		NetPlayer sender = info.Sender;
		IncrementRPC(info, "EnableNonCosmeticHandItem");
		if (sender == netView.Owner)
		{
			senderRig = GorillaGameManager.StaticFindRigForPlayer(sender);
			if (!(senderRig == null))
			{
				if (isLeftHand && (bool)nonCosmeticLeftHandItem)
				{
					senderRig.nonCosmeticLeftHandItem.EnableItem(enable);
				}
				else if (!isLeftHand && (bool)nonCosmeticRightHandItem)
				{
					senderRig.nonCosmeticRightHandItem.EnableItem(enable);
				}
			}
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent Enable Non Cosmetic Hand Item", info.Sender.UserId, info.Sender.NickName);
		}
	}

	public bool IsMakingFistLeft()
	{
		if (isOfflineVRRig)
		{
			if (ControllerInputPoller.GripFloat(XRNode.LeftHand) > 0.25f)
			{
				return ControllerInputPoller.TriggerFloat(XRNode.LeftHand) > 0.25f;
			}
			return false;
		}
		if (leftIndex.calcT > 0.25f)
		{
			return leftMiddle.calcT > 0.25f;
		}
		return false;
	}

	public bool IsMakingFistRight()
	{
		if (isOfflineVRRig)
		{
			if (ControllerInputPoller.GripFloat(XRNode.RightHand) > 0.25f)
			{
				return ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.25f;
			}
			return false;
		}
		if (rightIndex.calcT > 0.25f)
		{
			return rightMiddle.calcT > 0.25f;
		}
		return false;
	}

	public bool IsMakingFiveLeft()
	{
		if (isOfflineVRRig)
		{
			if (ControllerInputPoller.GripFloat(XRNode.LeftHand) < 0.25f)
			{
				return ControllerInputPoller.TriggerFloat(XRNode.LeftHand) < 0.25f;
			}
			return false;
		}
		if (leftIndex.calcT < 0.25f)
		{
			return leftMiddle.calcT < 0.25f;
		}
		return false;
	}

	public bool IsMakingFiveRight()
	{
		if (isOfflineVRRig)
		{
			if (ControllerInputPoller.GripFloat(XRNode.RightHand) < 0.25f)
			{
				return ControllerInputPoller.TriggerFloat(XRNode.RightHand) < 0.25f;
			}
			return false;
		}
		if (rightIndex.calcT < 0.25f)
		{
			return rightMiddle.calcT < 0.25f;
		}
		return false;
	}

	public VRMap GetMakingFist(bool debug, out bool isLeftHand)
	{
		if (IsMakingFistRight())
		{
			isLeftHand = false;
			return rightHand;
		}
		if (IsMakingFistLeft())
		{
			isLeftHand = true;
			return leftHand;
		}
		isLeftHand = false;
		return null;
	}

	public void PlayGeodeEffect(Vector3 hitPosition)
	{
		if ((base.transform.position - hitPosition).sqrMagnitude < 9f && (bool)geodeCrackingSound)
		{
			geodeCrackingSound.GTPlay();
		}
	}

	public void PlayClimbSound(AudioClip clip, bool isLeftHand)
	{
		if (isLeftHand)
		{
			leftHandPlayer.volume = 0.1f;
			leftHandPlayer.clip = clip;
			leftHandPlayer.GTPlayOneShot(leftHandPlayer.clip);
		}
		else
		{
			rightHandPlayer.volume = 0.1f;
			rightHandPlayer.clip = clip;
			rightHandPlayer.GTPlayOneShot(rightHandPlayer.clip);
		}
	}

	public void HideAllCosmetics(PhotonMessageInfo info)
	{
		IncrementRPC(info, "HideAllCosmetics");
		if (NetworkSystem.Instance.GetPlayer(info.Sender) == netView.Owner)
		{
			LocalUpdateCosmeticsWithTryon(CosmeticsController.CosmeticSet.EmptySet, CosmeticsController.CosmeticSet.EmptySet, playfx: false);
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent update cosmetics", info.Sender.UserId, info.Sender.NickName);
		}
	}

	public void UpdateCosmeticsWithTryon(string[] currentItems, string[] tryOnItems, bool playfx, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_UpdateCosmeticsWithTryon");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (info.Sender == netView.Owner && currentItems.Length == 16 && tryOnItems.Length == 16)
		{
			CosmeticsController.CosmeticSet newSet = new CosmeticsController.CosmeticSet(currentItems, CosmeticsController.instance);
			CosmeticsController.CosmeticSet newTryOnSet = new CosmeticsController.CosmeticSet(tryOnItems, CosmeticsController.instance);
			LocalUpdateCosmeticsWithTryon(newSet, newTryOnSet, playfx);
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent update cosmetics with tryon", player.UserId, player.NickName);
		}
	}

	public void UpdateCosmeticsWithTryon(int[] currentItemsPacked, int[] tryOnItemsPacked, bool playfx, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_UpdateCosmeticsWithTryon");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (info.Sender == netView.Owner && CosmeticsController.instance.ValidatePackedItems(currentItemsPacked) && CosmeticsController.instance.ValidatePackedItems(tryOnItemsPacked))
		{
			CosmeticsController.CosmeticSet newSet = new CosmeticsController.CosmeticSet(currentItemsPacked, CosmeticsController.instance);
			CosmeticsController.CosmeticSet newTryOnSet = new CosmeticsController.CosmeticSet(tryOnItemsPacked, CosmeticsController.instance);
			LocalUpdateCosmeticsWithTryon(newSet, newTryOnSet, playfx);
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent update cosmetics with tryon", player.UserId, player.NickName);
		}
	}

	public void UpdateCosmeticsWithCollectables(int[] cycleStatesPacked, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_UpdateCosmeticsWithCollectablesPacked");
		if (info.Sender != netView.Owner || cycleStatesPacked == null || cycleStatesPacked.Length % 3 != 0 || cycleStatesPacked.Length > 96)
		{
			return;
		}
		int num = cycleStatesPacked.Length / 3;
		remoteCycleStates.Clear();
		char[] array = new char[6] { '\0', '\0', '\0', '\0', '\0', '.' };
		for (int i = 0; i < num; i++)
		{
			int num2 = cycleStatesPacked[i * 3];
			int num3 = cycleStatesPacked[i * 3 + 1];
			int visibleMask = cycleStatesPacked[i * 3 + 2];
			if (num3 >= 0)
			{
				array[0] = (char)(65 + num2 % 26);
				array[1] = (char)(65 + num2 / 26 % 26);
				array[2] = (char)(65 + num2 / 676 % 26);
				array[3] = (char)(65 + num2 / 17576 % 26);
				array[4] = (char)(65 + num2 / 456976 % 26);
				string text = new string(array);
				CosmeticsController.CollectionState value = new CosmeticsController.CollectionState
				{
					activeIndex = num3,
					visibleMask = visibleMask
				};
				remoteCycleStates[text] = value;
				CosmeticCollectionDisplay cosmeticCollectionDisplay = CosmeticCollectionDisplay.FindForRig(this, text);
				if (cosmeticCollectionDisplay != null)
				{
					cosmeticCollectionDisplay.SetVisibleMask(visibleMask);
					cosmeticCollectionDisplay.SetActiveIndex(num3);
				}
			}
		}
	}

	public void BroadcastSubCosmeticSignal(int packedParentID, int signal, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_BroadcastSubCosmeticSignal");
		if (info.Sender != netView.Owner)
		{
			return;
		}
		char[] array = new char[6];
		array[5] = '.';
		array[0] = (char)(65 + packedParentID % 26);
		array[1] = (char)(65 + packedParentID / 26 % 26);
		array[2] = (char)(65 + packedParentID / 676 % 26);
		array[3] = (char)(65 + packedParentID / 17576 % 26);
		array[4] = (char)(65 + packedParentID / 456976 % 26);
		string parentID = new string(array);
		CosmeticCollectionDisplay cosmeticCollectionDisplay = CosmeticCollectionDisplay.FindForRig(this, parentID);
		if (cosmeticCollectionDisplay != null)
		{
			SubCosmeticCycleController subCosmeticCycleController = cosmeticCollectionDisplay.GetComponent<SubCosmeticCycleController>();
			if (subCosmeticCycleController == null)
			{
				subCosmeticCycleController = cosmeticCollectionDisplay.GetComponentInChildren<SubCosmeticCycleController>(includeInactive: true);
			}
			if (subCosmeticCycleController != null)
			{
				subCosmeticCycleController.ReceiveNetworkSignal(signal);
			}
		}
	}

	public void SetCollectionCycleIndex(int packedParentID, int activeIndex, int visibleMask, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "RPC_SetCollectionCycleIndex");
		if (info.Sender != netView.Owner)
		{
			return;
		}
		char[] array = new char[6];
		array[5] = '.';
		array[0] = (char)(65 + packedParentID % 26);
		array[1] = (char)(65 + packedParentID / 26 % 26);
		array[2] = (char)(65 + packedParentID / 676 % 26);
		array[3] = (char)(65 + packedParentID / 17576 % 26);
		array[4] = (char)(65 + packedParentID / 456976 % 26);
		string text = new string(array);
		if (remoteCycleStates.Count < 64 || remoteCycleStates.ContainsKey(text))
		{
			CosmeticsController.CollectionState value = new CosmeticsController.CollectionState
			{
				activeIndex = activeIndex,
				visibleMask = visibleMask
			};
			remoteCycleStates[text] = value;
			CosmeticCollectionDisplay cosmeticCollectionDisplay = CosmeticCollectionDisplay.FindForRig(this, text);
			if (cosmeticCollectionDisplay != null)
			{
				cosmeticCollectionDisplay.SetVisibleMask(visibleMask);
				cosmeticCollectionDisplay.SetActiveIndex(activeIndex);
			}
		}
	}

	public void LocalUpdateCosmeticsWithTryon(CosmeticsController.CosmeticSet newSet, CosmeticsController.CosmeticSet newTryOnSet, bool playfx)
	{
		cosmeticSet = newSet;
		tryOnSet = newTryOnSet;
		if (initializedCosmetics)
		{
			SetCosmeticsActive(playfx);
		}
	}

	private void CheckForEarlyAccess()
	{
		CosmeticInfoV2 info = CosmeticsController.instance.EarlyAccessSupporterPackCosmeticSO.info;
		if (_playerOwnedCosmetics.Contains(info.playFabID))
		{
			CosmeticSO[] setCosmetics = info.setCosmetics;
			for (int i = 0; i < setCosmetics.Length; i++)
			{
				CosmeticInfoV2 info2 = setCosmetics[i].info;
				_playerOwnedCosmetics.Add(info2.playFabID);
			}
		}
		InitializedCosmetics = true;
	}

	public void SetCosmeticsActive(bool playfx)
	{
		if (CosmeticsController.instance == null)
		{
			return;
		}
		prevSet.CopyItems(mergedSet);
		mergedSet.MergeSets(inTryOnRoom ? tryOnSet : null, cosmeticSet);
		BodyDockPositions component = GetComponent<BodyDockPositions>();
		mergedSet.ActivateCosmetics(prevSet, this, component, cosmeticsObjectRegistry);
		if (playfx)
		{
			if (cosmeticsActivationPS != null)
			{
				cosmeticsActivationPS.Play();
			}
			if (cosmeticsActivationSBP != null)
			{
				cosmeticsActivationSBP.Play();
			}
		}
	}

	public void RefreshCosmetics()
	{
		mergedSet.ActivateCosmetics(mergedSet, this, myBodyDockPositions, cosmeticsObjectRegistry);
	}

	public void GetCosmeticsPlayFabCatalogData()
	{
		if (CosmeticsController.instance != null)
		{
			PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), delegate(GetUserInventoryResult result)
			{
				foreach (ItemInstance item in result.Inventory)
				{
					if (item.CatalogVersion == CosmeticsController.instance.catalog)
					{
						if (item.PurchaseDate.HasValue)
						{
							int daysOwned = (int)(DateTime.UtcNow - item.PurchaseDate.Value).TotalDays;
							AddCosmetic(item.ItemId, daysOwned);
						}
						else
						{
							AddCosmetic(item.ItemId);
						}
					}
				}
			}, delegate
			{
				initializedCosmetics = true;
			});
		}
		AddCosmetic("Slingshot");
		foreach (BuilderPieceSet startPieceSet in BuilderSetManager.instance.StartPieceSets)
		{
			AddCosmetic(startPieceSet.playfabID);
		}
	}

	public void GenerateFingerAngleLookupTables()
	{
		GenerateTableIndex(ref leftIndex);
		GenerateTableIndex(ref rightIndex);
		GenerateTableMiddle(ref leftMiddle);
		GenerateTableMiddle(ref rightMiddle);
		GenerateTableThumb(ref leftThumb);
		GenerateTableThumb(ref rightThumb);
	}

	private void GenerateTableThumb(ref VRMapThumb thumb)
	{
		thumb.angle1Table = new Quaternion[11];
		thumb.angle2Table = new Quaternion[11];
		for (int i = 0; i < thumb.angle1Table.Length; i++)
		{
			thumb.angle1Table[i] = Quaternion.Lerp(thumb.startingAngle1Quat, thumb.closedAngle1Quat, (float)i / 10f);
			thumb.angle2Table[i] = Quaternion.Lerp(thumb.startingAngle2Quat, thumb.closedAngle2Quat, (float)i / 10f);
		}
	}

	private void GenerateTableIndex(ref VRMapIndex index)
	{
		index.angle1Table = new Quaternion[11];
		index.angle2Table = new Quaternion[11];
		index.angle3Table = new Quaternion[11];
		for (int i = 0; i < index.angle1Table.Length; i++)
		{
			index.angle1Table[i] = Quaternion.Lerp(index.startingAngle1Quat, index.closedAngle1Quat, (float)i / 10f);
			index.angle2Table[i] = Quaternion.Lerp(index.startingAngle2Quat, index.closedAngle2Quat, (float)i / 10f);
			index.angle3Table[i] = Quaternion.Lerp(index.startingAngle3Quat, index.closedAngle3Quat, (float)i / 10f);
		}
	}

	private void GenerateTableMiddle(ref VRMapMiddle middle)
	{
		middle.angle1Table = new Quaternion[11];
		middle.angle2Table = new Quaternion[11];
		middle.angle3Table = new Quaternion[11];
		for (int i = 0; i < middle.angle1Table.Length; i++)
		{
			middle.angle1Table[i] = Quaternion.Lerp(middle.startingAngle1Quat, middle.closedAngle1Quat, (float)i / 10f);
			middle.angle2Table[i] = Quaternion.Lerp(middle.startingAngle2Quat, middle.closedAngle2Quat, (float)i / 10f);
			middle.angle3Table[i] = Quaternion.Lerp(middle.startingAngle3Quat, middle.closedAngle3Quat, (float)i / 10f);
		}
	}

	private Quaternion SanitizeQuaternion(Quaternion quat)
	{
		if (float.IsNaN(quat.w) || float.IsNaN(quat.x) || float.IsNaN(quat.y) || float.IsNaN(quat.z) || float.IsInfinity(quat.w) || float.IsInfinity(quat.x) || float.IsInfinity(quat.y) || float.IsInfinity(quat.z))
		{
			return Quaternion.identity;
		}
		return quat;
	}

	private Vector3 SanitizeVector3(Vector3 vec)
	{
		if (float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z) || float.IsInfinity(vec.x) || float.IsInfinity(vec.y) || float.IsInfinity(vec.z))
		{
			return Vector3.zero;
		}
		return Vector3.ClampMagnitude(vec, 5000f);
	}

	private void IncrementRPC(PhotonMessageInfoWrapped info, string sourceCall)
	{
		if (GorillaGameManager.instance != null)
		{
			MonkeAgent.IncrementRPCCall(info, sourceCall);
		}
	}

	private void IncrementRPC(PhotonMessageInfo info, string sourceCall)
	{
		if (GorillaGameManager.instance != null)
		{
			MonkeAgent.IncrementRPCCall(info, sourceCall);
		}
	}

	private void AddVelocityToQueue(Vector3 position, double serverTime)
	{
		Vector3 velocity = Vector3.zero;
		if (velocityHistoryList.Count > 0)
		{
			double num = Utils.CalculateNetworkDeltaTime(velocityHistoryList[0].time, serverTime);
			if (num == 0.0)
			{
				return;
			}
			velocity = (position - lastPosition) / (float)num;
		}
		velocityHistoryList.Add(new VelocityTime(velocity, serverTime));
		lastPosition = position;
	}

	private Vector3 ReturnVelocityAtTime(double timeToReturn)
	{
		if (velocityHistoryList.Count <= 1)
		{
			return Vector3.zero;
		}
		int num = 0;
		int num2 = velocityHistoryList.Count - 1;
		int num3 = 0;
		if (num2 == num)
		{
			return velocityHistoryList[num].vel;
		}
		while (num2 - num > 1 && num3 < 1000)
		{
			num3++;
			int num4 = (num2 - num) / 2;
			if (velocityHistoryList[num4].time > timeToReturn)
			{
				num2 = num4;
			}
			else
			{
				num = num4;
			}
		}
		float num5 = (float)(velocityHistoryList[num].time - timeToReturn);
		double num6 = velocityHistoryList[num].time - velocityHistoryList[num2].time;
		if (num6 == 0.0)
		{
			num6 = 0.001;
		}
		num5 /= (float)num6;
		num5 = Mathf.Clamp(num5, 0f, 1f);
		return Vector3.Lerp(velocityHistoryList[num].vel, velocityHistoryList[num2].vel, num5);
	}

	public Vector3 LatestVelocity()
	{
		if (velocityHistoryList.Count > 0)
		{
			return velocityHistoryList[0].vel;
		}
		return Vector3.zero;
	}

	public bool IsPositionInRange(Vector3 position, float range)
	{
		return (syncPos - position).IsShorterThan(range * scaleFactor);
	}

	public bool CheckTagDistanceRollback(VRRig otherRig, float max, float timeInterval)
	{
		GorillaMath.LineSegClosestPoints(syncPos, -LatestVelocity() * timeInterval, otherRig.syncPos, -otherRig.LatestVelocity() * timeInterval, out var lineAPoint, out var lineBPoint);
		return Vector3.SqrMagnitude(lineAPoint - lineBPoint) < max * max * scaleFactor;
	}

	public Vector3 ClampVelocityRelativeToPlayerSafe(Vector3 inVel, float max, float teleportSpeedThreshold = 100f)
	{
		max *= scaleFactor;
		Vector3 v = Vector3.zero;
		v.SetValueSafe(in inVel);
		Vector3 vector = ((velocityHistoryList.Count > 0) ? velocityHistoryList[0].vel : Vector3.zero);
		if (vector.sqrMagnitude > teleportSpeedThreshold * teleportSpeedThreshold)
		{
			vector = Vector3.zero;
		}
		Vector3 vector2 = v - vector;
		vector2 = Vector3.ClampMagnitude(vector2, max);
		return vector + vector2;
	}

	public void SetColor(Color color)
	{
		this.OnColorChanged?.Invoke(color);
		onColorInitialized?.Invoke(color);
		onColorInitialized = delegate
		{
		};
		colorInitialized = true;
		playerColor = color;
		if (this.OnDataChange != null)
		{
			this.OnDataChange();
		}
	}

	public void OnColorInitialized(Action<Color> action)
	{
		if (colorInitialized)
		{
			action(playerColor);
		}
		else
		{
			onColorInitialized = (Action<Color>)Delegate.Combine(onColorInitialized, action);
		}
	}

	private void SendScoresToRoom()
	{
		if (netView != null && _scoreUpdated)
		{
			netView.SendRPC("RPC_UpdateQuestScore", RpcTarget.Others, currentQuestScore);
		}
	}

	private void SendScoresToGameModeRoom(GameModeType newGameModeType)
	{
		if (netView != null && _rankedInfoUpdated && newGameModeType != GameModeType.InfectionCompetitive && !m_sentRankedScore)
		{
			m_sentRankedScore = true;
			netView.SendRPC("RPC_UpdateRankedInfo", RpcTarget.Others, currentRankedELO, currentRankedSubTierQuest, currentRankedSubTierPC);
		}
	}

	private void SendScoresToNewPlayer(NetPlayer player)
	{
		if (netView != null)
		{
			if (_scoreUpdated)
			{
				netView.SendRPC("RPC_UpdateQuestScore", player, currentQuestScore);
			}
			if (_rankedInfoUpdated && !IsInRankedMode())
			{
				netView.SendRPC("RPC_UpdateRankedInfo", player, currentRankedELO, currentRankedSubTierQuest, currentRankedSubTierPC);
			}
		}
	}

	public void SetQuestScore(int score)
	{
		SetQuestScoreLocal(score);
		this.OnQuestScoreChanged?.Invoke(currentQuestScore);
		if (netView != null)
		{
			netView.SendRPC("RPC_UpdateQuestScore", RpcTarget.Others, currentQuestScore);
		}
	}

	public int GetCurrentQuestScore()
	{
		if (!_scoreUpdated)
		{
			SetQuestScoreLocal(ProgressionController.TotalPoints);
		}
		return currentQuestScore;
	}

	private void SetQuestScoreLocal(int score)
	{
		currentQuestScore = score;
		_scoreUpdated = true;
	}

	public void UpdateQuestScore(int score, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "UpdateQuestScore");
		NetworkSystem.Instance.GetPlayer(info.senderID);
		if (info.senderID == creator.ActorNumber && updateQuestCallLimit.CheckCallTime(Time.time) && score >= currentQuestScore)
		{
			SetQuestScoreLocal(score);
			this.OnQuestScoreChanged?.Invoke(currentQuestScore);
		}
	}

	public void SetRankedInfo(float rankedELO, int rankedSubtierQuest, int rankedSubtierPC, bool broadcastToOtherClients = true)
	{
		SetRankedInfoLocal(rankedELO, rankedSubtierQuest, rankedSubtierPC);
		this.OnRankedSubtierChanged?.Invoke(rankedSubtierQuest, rankedSubtierPC);
		if (netView != null && broadcastToOtherClients)
		{
			netView.SendRPC("RPC_UpdateRankedInfo", RpcTarget.Others, currentRankedELO, currentRankedSubTierQuest, currentRankedSubTierPC);
		}
	}

	public int GetCurrentRankedSubTier(bool getPC)
	{
		if (!_rankedInfoUpdated)
		{
			return -1;
		}
		if (!getPC)
		{
			return currentRankedSubTierQuest;
		}
		return currentRankedSubTierPC;
	}

	private void SetRankedInfoLocal(float rankedELO, int rankedSubTierQuest, int rankedSubTierPC)
	{
		currentRankedELO = rankedELO;
		currentRankedSubTierQuest = rankedSubTierQuest;
		currentRankedSubTierPC = rankedSubTierPC;
		_rankedInfoUpdated = true;
	}

	private bool IsInRankedMode()
	{
		if (GorillaGameModes.GameMode.ActiveGameMode != null)
		{
			return GorillaGameModes.GameMode.ActiveGameMode.GameType() == GameModeType.InfectionCompetitive;
		}
		return false;
	}

	public void UpdateRankedInfo(float rankedELO, int rankedSubtierQuest, int rankedSubtierPC, PhotonMessageInfoWrapped info)
	{
		IncrementRPC(info, "UpdateRankedInfo");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && playerRig.Rig.updateRankedInfoCallLimit.CheckCallTime(Time.time) && info.senderID == creator.ActorNumber && float.IsFinite(rankedELO) && !IsInRankedMode() && !(RankedProgressionManager.Instance == null) && RankedProgressionManager.Instance.AreValuesValid(rankedELO, rankedSubtierQuest, rankedSubtierPC))
		{
			SetRankedInfoLocal(rankedELO, rankedSubtierQuest, rankedSubtierPC);
			this.OnRankedSubtierChanged?.Invoke(rankedSubtierQuest, rankedSubtierPC);
			int num = rankedSubtierQuest;
			num = rankedSubtierPC;
			RankedProgressionManager.Instance.HandlePlayerRankedInfoReceived(creator.ActorNumber, rankedELO, num);
		}
	}

	public void OnEnable()
	{
		EyeScannerMono.Register(this);
		SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Combine(SubscriptionManager.OnLocalSubscriptionData, new Action(OnSubscriptionData));
		GorillaComputer.RegisterOnNametagSettingChanged(UpdateName);
		if (currentRopeSwingTarget != null)
		{
			currentRopeSwingTarget.SetParent(null);
		}
		if (!isOfflineVRRig)
		{
			PlayerCosmeticsSystem.RegisterCosmeticCallback(creator.ActorNumber, this);
		}
		bodyRenderer.SetDefaults();
		SetInvisibleToLocalPlayer(invisible: false);
		if (isOfflineVRRig)
		{
			HandHold.HandPositionRequestOverride += HandHold_HandPositionRequestOverride;
			HandHold.HandPositionReleaseOverride += HandHold_HandPositionReleaseOverride;
			GorillaGameModes.GameMode.OnStartGameMode += SendScoresToGameModeRoom;
			RoomSystem.JoinedRoomEvent += new Action(SendScoresToRoom);
			RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(SendScoresToNewPlayer);
		}
		else
		{
			VRRigJobManager.Instance.RegisterVRRig(this);
		}
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		TickSystem<object>.AddPostTickCallback(this);
	}

	public void OnSubscriptionData()
	{
		if (isOfflineVRRig)
		{
			showGoldNameTag = SubscriptionManager.IsLocalSubscribed() && PlayerPrefs.GetInt(SubscriptionManager.GetSubsFeatureKey(SubscriptionManager.SubscriptionFeatures.GoldenName)) > 0;
			if (showGoldNameTag)
			{
				playerText1.color = SubscriptionManager.SUBSCRIBER_NAME_COLOR;
			}
			else
			{
				playerText1.color = Color.white;
			}
		}
	}

	void IPreDisable.PreDisable()
	{
		try
		{
			ClearRopeData();
			if ((bool)currentRopeSwingTarget)
			{
				currentRopeSwingTarget.SetParent(base.transform);
			}
			EnableHuntWatch(on: false);
			EnablePaintbrawlCosmetics(on: false);
			EnableSuperInfectionHands(on: false);
			ClearPartyMemberStatus();
			_playerOwnedCosmetics.Clear();
			_playerOwnedCosmeticsAge.Clear();
			if (cosmeticSet != null)
			{
				mergedSet.DeactivateAllCosmetcs(myBodyDockPositions, CosmeticsController.instance.nullItem, cosmeticsObjectRegistry);
				mergedSet.ClearSet(CosmeticsController.instance.nullItem);
				prevSet.ClearSet(CosmeticsController.instance.nullItem);
				tryOnSet.ClearSet(CosmeticsController.instance.nullItem);
				cosmeticSet.ClearSet(CosmeticsController.instance.nullItem);
			}
			if (!isOfflineVRRig)
			{
				PlayerCosmeticsSystem.RemoveCosmeticCallback(creator.ActorNumber);
				pendingCosmeticUpdate = true;
				LocalRig.leftHandLink.BreakLinkTo(leftHandLink);
				LocalRig.leftHandLink.BreakLinkTo(rightHandLink);
				LocalRig.rightHandLink.BreakLinkTo(leftHandLink);
				LocalRig.rightHandLink.BreakLinkTo(rightHandLink);
			}
		}
		catch (Exception)
		{
		}
	}

	public void OnDisable()
	{
		SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnLocalSubscriptionData, new Action(OnSubscriptionData));
		try
		{
			GorillaSkin.ApplyToRig(this, null, GorillaSkin.SkinType.gameMode);
			ChangeMaterialLocal(0);
			GorillaComputer.UnregisterOnNametagSettingChanged(UpdateName);
			netView = null;
			voiceAudio = null;
			muted = false;
			initialized = false;
			initializedCosmetics = false;
			inTryOnRoom = false;
			inTempCosmSpace = false;
			timeSpawned = 0f;
			setMatIndex = 0;
			currentCosmeticTries = 0;
			velocityHistoryList.Clear();
			netSyncPos.Reset();
			rightHand.netSyncPos.Reset();
			leftHand.netSyncPos.Reset();
			ForceResetFrozenEffect();
			nativeScale = (frameScale = (lastScaleFactor = 1f));
			base.transform.localScale = Vector3.one;
			currentQuestScore = 0;
			_scoreUpdated = false;
			currentRankedELO = 0f;
			currentRankedSubTierQuest = 0;
			currentRankedSubTierPC = 0;
			_rankedInfoUpdated = false;
			TemporaryCosmeticEffects.Clear();
			m_sentRankedScore = false;
			if (inDuplicationZone)
			{
				ClearDuplicationZone(duplicationZone);
			}
			try
			{
				CallLimitType<CallLimiter>[] callSettings = fxSettings.callSettings;
				for (int i = 0; i < callSettings.Length; i++)
				{
					callSettings[i].CallLimitSettings.Reset();
				}
			}
			catch
			{
				Debug.LogError("fxtype missing in fxSettings, please fix or remove this");
			}
		}
		catch (Exception)
		{
		}
		if (isOfflineVRRig)
		{
			HandHold.HandPositionRequestOverride -= HandHold_HandPositionRequestOverride;
			HandHold.HandPositionReleaseOverride -= HandHold_HandPositionReleaseOverride;
			GorillaGameModes.GameMode.OnStartGameMode -= SendScoresToGameModeRoom;
			RoomSystem.JoinedRoomEvent -= new Action(SendScoresToRoom);
			RoomSystem.PlayerJoinedEvent -= new Action<NetPlayer>(SendScoresToNewPlayer);
		}
		else
		{
			VRRigJobManager.Instance.DeregisterVRRig(this);
		}
		EyeScannerMono.Unregister(this);
		creator = null;
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		TickSystem<object>.RemovePostTickCallback(this);
	}

	private void HandHold_HandPositionReleaseOverride(HandHold hh, bool leftHand)
	{
		if (leftHand)
		{
			this.leftHand.handholdOverrideTarget = null;
		}
		else
		{
			rightHand.handholdOverrideTarget = null;
		}
	}

	private void HandHold_HandPositionRequestOverride(HandHold hh, bool leftHand, Vector3 pos)
	{
		if (leftHand)
		{
			this.leftHand.handholdOverrideTarget = hh.transform;
			this.leftHand.handholdOverrideTargetOffset = pos;
		}
		else
		{
			rightHand.handholdOverrideTarget = hh.transform;
			rightHand.handholdOverrideTargetOffset = pos;
		}
	}

	public void NetInitialize()
	{
		timeSpawned = Time.time;
		if (NetworkSystem.Instance.InRoom)
		{
			GorillaGameManager instance = GorillaGameManager.instance;
			if (instance != null)
			{
				if (instance is GorillaHuntManager || instance.GameModeName() == "HUNT")
				{
					EnableHuntWatch(on: true);
				}
				else if (instance is GorillaPaintbrawlManager || instance.GameModeName() == "PAINTBRAWL")
				{
					EnablePaintbrawlCosmetics(on: true);
				}
			}
			else
			{
				string gameModeString = NetworkSystem.Instance.GameModeString;
				if (!gameModeString.IsNullOrEmpty())
				{
					string text = gameModeString;
					if (text.Contains("HUNT"))
					{
						EnableHuntWatch(on: true);
					}
					else if (text.Contains("PAINTBRAWL"))
					{
						EnablePaintbrawlCosmetics(on: true);
					}
				}
			}
			UpdateFriendshipBracelet();
			if (IsLocalPartyMember && !isOfflineVRRig)
			{
				FriendshipGroupDetection.Instance.SendVerifyPartyMember(creator);
			}
		}
		if (netView != null)
		{
			base.transform.position = netView.gameObject.transform.position;
			base.transform.rotation = netView.gameObject.transform.rotation;
		}
		try
		{
			newPlayerJoined?.Invoke();
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	public void GrabbedByPlayer(VRRig grabbedByRig, bool grabbedBody, bool grabbedLeftHand, bool grabbedWithLeftHand)
	{
		GorillaClimbable climbable = (grabbedWithLeftHand ? grabbedByRig.leftHandHoldsPlayer : grabbedByRig.rightHandHoldsPlayer);
		GorillaHandClimber gorillaHandClimber = (grabbedBody ? EquipmentInteractor.instance.BodyClimber : ((!grabbedLeftHand) ? EquipmentInteractor.instance.RightClimber : EquipmentInteractor.instance.LeftClimber));
		gorillaHandClimber.SetCanRelease(canRelease: false);
		GTPlayer.Instance.BeginClimbing(climbable, gorillaHandClimber);
		grabbedRopeIsBody = grabbedBody;
		grabbedRopeIsLeft = grabbedLeftHand;
		grabbedRopeIndex = grabbedByRig.netView.ViewID;
		grabbedRopeBoneIndex = (grabbedWithLeftHand ? 1 : 0);
		grabbedRopeOffset = Vector3.zero;
		grabbedRopeIsPhotonView = true;
	}

	public void DroppedByPlayer(VRRig grabbedByRig, Vector3 throwVelocity)
	{
		GorillaClimbable currentClimbable = GTPlayer.Instance.CurrentClimbable;
		if (GTPlayer.Instance.isClimbing && (currentClimbable == grabbedByRig.leftHandHoldsPlayer || currentClimbable == grabbedByRig.rightHandHoldsPlayer))
		{
			throwVelocity = Vector3.ClampMagnitude(throwVelocity, 20f);
			GorillaHandClimber currentClimber = GTPlayer.Instance.CurrentClimber;
			GTPlayer.Instance.EndClimbing(currentClimber, startingNewClimb: false);
			GTPlayer.Instance.SetVelocity(throwVelocity);
			grabbedRopeIsBody = false;
			grabbedRopeIsLeft = false;
			grabbedRopeIndex = -1;
			grabbedRopeBoneIndex = 0;
			grabbedRopeOffset = Vector3.zero;
			grabbedRopeIsPhotonView = false;
		}
		else if (LocalRig.leftHandLink.IsLinkActive() && LocalRig.leftHandLink.grabbedLink.myRig == grabbedByRig)
		{
			throwVelocity = Vector3.ClampMagnitude(throwVelocity, 3f);
			LocalRig.leftHandLink.BreakLink();
			LocalRig.leftHandLink.RejectGrabsFor(1f);
			GTPlayer.Instance.SetVelocity(throwVelocity);
		}
		else if (LocalRig.rightHandLink.IsLinkActive() && LocalRig.rightHandLink.grabbedLink.myRig == grabbedByRig)
		{
			throwVelocity = Vector3.ClampMagnitude(throwVelocity, 3f);
			LocalRig.rightHandLink.BreakLink();
			LocalRig.rightHandLink.RejectGrabsFor(1f);
			GTPlayer.Instance.SetVelocity(throwVelocity);
		}
	}

	public bool IsOnGround(float headCheckDistance, float handCheckDistance, out Vector3 groundNormal)
	{
		GTPlayer instance = GTPlayer.Instance;
		Vector3 position = base.transform.position;
		if (LocalCheckCollision(position, Vector3.down * headCheckDistance * scaleFactor, instance.headCollider.radius * scaleFactor, out var finalPosition, out var hit))
		{
			groundNormal = hit.normal;
			return true;
		}
		Vector3 position2 = leftHand.rigTarget.position;
		if (LocalCheckCollision(position2, Vector3.down * handCheckDistance * scaleFactor, instance.minimumRaycastDistance * scaleFactor, out finalPosition, out hit))
		{
			groundNormal = hit.normal;
			return true;
		}
		Vector3 position3 = rightHand.rigTarget.position;
		if (LocalCheckCollision(position3, Vector3.down * handCheckDistance * scaleFactor, instance.minimumRaycastDistance * scaleFactor, out finalPosition, out hit))
		{
			groundNormal = hit.normal;
			return true;
		}
		groundNormal = Vector3.up;
		return false;
	}

	private bool LocalTestMovementCollision(Vector3 startPosition, Vector3 startVelocity, out Vector3 modifiedVelocity, out Vector3 finalPosition)
	{
		GTPlayer instance = GTPlayer.Instance;
		Vector3 vector = startVelocity * Time.deltaTime;
		finalPosition = startPosition + vector;
		modifiedVelocity = startVelocity;
		Vector3 finalPosition2;
		RaycastHit hit;
		bool num = LocalCheckCollision(startPosition, vector, instance.headCollider.radius * scaleFactor, out finalPosition2, out hit);
		if (num)
		{
			finalPosition = finalPosition2 - vector.normalized * 0.01f;
			modifiedVelocity = startVelocity - hit.normal * Vector3.Dot(hit.normal, startVelocity);
		}
		Vector3 position = leftHand.rigTarget.position;
		Vector3 finalPosition3;
		RaycastHit hit2;
		bool flag = LocalCheckCollision(position, vector, instance.minimumRaycastDistance * scaleFactor, out finalPosition3, out hit2);
		if (flag)
		{
			finalPosition = finalPosition3 - (leftHand.rigTarget.position - startPosition) - vector.normalized * 0.01f;
			modifiedVelocity = Vector3.zero;
		}
		Vector3 position2 = rightHand.rigTarget.position;
		Vector3 finalPosition4;
		RaycastHit hit3;
		bool flag2 = LocalCheckCollision(position2, vector, instance.minimumRaycastDistance * scaleFactor, out finalPosition4, out hit3);
		if (flag2)
		{
			finalPosition = finalPosition4 - (rightHand.rigTarget.position - startPosition) - vector.normalized * 0.01f;
			modifiedVelocity = Vector3.zero;
		}
		return num || flag || flag2;
	}

	public void TrySweptMoveTo(Vector3 targetPosition, out bool handCollided, out bool buttCollided)
	{
		Vector3 position = base.transform.position;
		TrySweptOffsetMove(targetPosition - position, out handCollided, out buttCollided);
	}

	public void TrySweptOffsetMove(Vector3 movement, out bool handCollided, out bool buttCollided)
	{
		GTPlayer instance = GTPlayer.Instance;
		Vector3 position = base.transform.position;
		Vector3 vector = position + movement;
		Vector3 startPosition = position;
		handCollided = false;
		buttCollided = false;
		if (LocalCheckCollision(startPosition, movement, instance.headCollider.radius * scaleFactor, out var finalPosition, out var _))
		{
			vector = ((!movement.IsShorterThan(0.01f)) ? (finalPosition - movement.normalized * 0.01f) : position);
			movement = vector - position;
			buttCollided = true;
		}
		Vector3 position2 = leftHand.rigTarget.position;
		if (LocalCheckCollision(position2, movement, instance.minimumRaycastDistance * scaleFactor, out var finalPosition2, out var _))
		{
			vector = ((!movement.IsShorterThan(0.01f)) ? (finalPosition2 - (leftHand.rigTarget.position - position) - movement.normalized * 0.01f) : position);
			movement = vector - position;
			handCollided = true;
		}
		Vector3 position3 = rightHand.rigTarget.position;
		if (LocalCheckCollision(position3, movement, instance.minimumRaycastDistance * scaleFactor, out var finalPosition3, out var _))
		{
			vector = ((!movement.IsShorterThan(0.01f)) ? (finalPosition3 - (rightHand.rigTarget.position - position) - movement.normalized * 0.01f) : position);
			movement = vector - position;
			handCollided = true;
		}
		base.transform.position = vector;
	}

	private bool LocalCheckCollision(Vector3 startPosition, Vector3 movement, float radius, out Vector3 finalPosition, out RaycastHit hit)
	{
		GTPlayer instance = GTPlayer.Instance;
		finalPosition = startPosition + movement;
		RaycastHit raycastHit = default(RaycastHit);
		bool flag = false;
		Vector3 normalized = movement.normalized;
		int num = Physics.SphereCastNonAlloc(startPosition, radius, normalized, rayCastNonAllocColliders, movement.magnitude, instance.locomotionEnabledLayers.value);
		if (num > 0)
		{
			raycastHit = rayCastNonAllocColliders[0];
			for (int i = 0; i < num; i++)
			{
				if (!(raycastHit.distance <= 0f) && (!flag || rayCastNonAllocColliders[i].distance < raycastHit.distance))
				{
					flag = true;
					raycastHit = rayCastNonAllocColliders[i];
				}
			}
		}
		hit = raycastHit;
		if (flag)
		{
			finalPosition = startPosition + normalized * (raycastHit.distance - 0.01f);
			return true;
		}
		return false;
	}

	public void UpdateFriendshipBracelet()
	{
		bool flag = false;
		if (isOfflineVRRig)
		{
			bool flag2 = false;
			switch (GetPartyMemberStatus())
			{
			case PartyMemberStatus.InLocalParty:
				flag2 = true;
				reliableState.isBraceletLeftHanded = FriendshipGroupDetection.Instance.DidJoinLeftHanded && !huntComputer.activeSelf;
				break;
			case PartyMemberStatus.NotInLocalParty:
				flag2 = false;
				reliableState.isBraceletLeftHanded = false;
				break;
			}
			if (reliableState.HasBracelet != flag2 || reliableState.braceletBeadColors.Count != FriendshipGroupDetection.Instance.myBeadColors.Count)
			{
				reliableState.SetIsDirty();
				flag = reliableState.HasBracelet == flag2;
			}
			reliableState.braceletBeadColors.Clear();
			if (flag2)
			{
				reliableState.braceletBeadColors.AddRange(FriendshipGroupDetection.Instance.myBeadColors);
			}
			reliableState.braceletSelfIndex = FriendshipGroupDetection.Instance.MyBraceletSelfIndex;
		}
		if ((object)nonCosmeticLeftHandItem != null)
		{
			bool flag3 = reliableState.HasBracelet && reliableState.isBraceletLeftHanded && !IsInvisibleToLocalPlayer;
			nonCosmeticLeftHandItem.EnableItem(flag3);
			if (flag3)
			{
				friendshipBraceletLeftHand.UpdateBeads(reliableState.braceletBeadColors, reliableState.braceletSelfIndex);
				if (flag)
				{
					friendshipBraceletLeftHand.PlayAppearEffects();
				}
			}
		}
		if ((object)nonCosmeticRightHandItem == null)
		{
			return;
		}
		bool flag4 = reliableState.HasBracelet && !reliableState.isBraceletLeftHanded && !IsInvisibleToLocalPlayer;
		nonCosmeticRightHandItem.EnableItem(flag4);
		if (flag4)
		{
			friendshipBraceletRightHand.UpdateBeads(reliableState.braceletBeadColors, reliableState.braceletSelfIndex);
			if (flag)
			{
				friendshipBraceletRightHand.PlayAppearEffects();
			}
		}
	}

	public void EnableHuntWatch(bool on)
	{
		huntComputer.SetActive(on);
		if (builderResizeWatch != null)
		{
			MeshRenderer component = builderResizeWatch.GetComponent<MeshRenderer>();
			if (component != null)
			{
				component.enabled = !on;
			}
		}
	}

	public void EnablePaintbrawlCosmetics(bool on)
	{
		paintbrawlBalloons.gameObject.SetActive(on);
	}

	public void EnableBuilderResizeWatch(bool on)
	{
		if (builderResizeWatch != null && builderResizeWatch.activeSelf != on)
		{
			builderResizeWatch.SetActive(on);
			if (builderArmShelfLeft != null)
			{
				builderArmShelfLeft.gameObject.SetActive(on);
			}
			if (builderArmShelfRight != null)
			{
				builderArmShelfRight.gameObject.SetActive(on);
			}
		}
		if (isOfflineVRRig)
		{
			bool num = reliableState.isBuilderWatchEnabled != on;
			reliableState.isBuilderWatchEnabled = on;
			if (num)
			{
				reliableState.SetIsDirty();
			}
		}
	}

	public void EnableGuardianEjectWatch(bool on)
	{
		if (guardianEjectWatch != null && guardianEjectWatch.activeSelf != on)
		{
			guardianEjectWatch.SetActive(on);
		}
	}

	public void EnableVStumpReturnWatch(bool on)
	{
		if (vStumpReturnWatch != null && vStumpReturnWatch.activeSelf != on)
		{
			vStumpReturnWatch.SetActive(on);
		}
	}

	public void EnableRankedTimerWatch(bool on)
	{
		if (rankedTimerWatch != null && rankedTimerWatch.activeSelf != on)
		{
			rankedTimerWatch.SetActive(on);
		}
	}

	public void EnableSuperInfectionHands(bool on)
	{
		if (superInfectionHand != null)
		{
			superInfectionHand.EnableHands(on);
		}
	}

	private void UpdateReplacementVoice()
	{
		if (remoteUseReplacementVoice || localUseReplacementVoice || GorillaComputer.instance.voiceChatOn != "TRUE")
		{
			voiceAudio.mute = true;
		}
		else
		{
			voiceAudio.mute = false;
		}
	}

	public bool ShouldPlayReplacementVoice()
	{
		if (!netView || netView.IsMine)
		{
			return false;
		}
		if (GorillaComputer.instance.voiceChatOn == "OFF")
		{
			return false;
		}
		if (remoteUseReplacementVoice || localUseReplacementVoice || GorillaComputer.instance.voiceChatOn == "FALSE")
		{
			return SpeakingLoudness > replacementVoiceLoudnessThreshold;
		}
		return false;
	}

	public void SetDuplicationZone(RigDuplicationZone duplicationZone)
	{
		this.duplicationZone = duplicationZone;
		inDuplicationZone = duplicationZone != null;
	}

	public void ClearDuplicationZone(RigDuplicationZone duplicationZone)
	{
		if (this.duplicationZone == duplicationZone)
		{
			SetDuplicationZone(null);
			renderTransform.localPosition = cachedRenderTransformPos;
		}
	}

	public void ResetTimeSpawned()
	{
		timeSpawned = Time.time;
	}

	public void SetGooParticleSystemStatus(bool isLeftHand, bool isEnabled)
	{
		if (isLeftHand)
		{
			if (leftHandGooParticleSystem.gameObject.activeSelf != isEnabled)
			{
				leftHandGooParticleSystem.gameObject.SetActive(isEnabled);
			}
		}
		else if (rightHandGooParticleSystem.gameObject.activeSelf != isEnabled)
		{
			rightHandGooParticleSystem.gameObject.SetActive(isEnabled);
		}
	}

	bool IUserCosmeticsCallback.OnGetUserCosmetics(string cosmeticsString)
	{
		if (cosmeticsString == "BANNED")
		{
			_playerOwnedCosmetics.Clear();
			_playerOwnedCosmeticsAge.Clear();
			return true;
		}
		Dictionary<string, ItemInstance> dictionary;
		try
		{
			dictionary = JsonConvert.DeserializeObject<Dictionary<string, ItemInstance>>(cosmeticsString);
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to deserialize cosmetics for " + creator?.NickName + ": " + ex.Message);
			dictionary = null;
		}
		if (currentCosmeticTries < cosmeticRetries && (dictionary == null || _playerOwnedCosmetics.SetEquals(dictionary.Keys)))
		{
			currentCosmeticTries++;
			return false;
		}
		if (dictionary == null)
		{
			dictionary = new Dictionary<string, ItemInstance>();
		}
		currentCosmeticTries = 0;
		SaveOwnedCosmetics(dictionary);
		InitializedCosmetics = true;
		SetCosmeticsActive(playfx: false);
		myBodyDockPositions.RefreshTransferrableItems();
		netView?.SendRPC("RPC_RequestCosmetics", creator);
		return true;
	}

	private void SaveOwnedCosmetics(Dictionary<string, ItemInstance> cosmetics)
	{
		if (cosmetics.Count == 0)
		{
			return;
		}
		_playerOwnedCosmetics.Clear();
		_playerOwnedCosmeticsAge.Clear();
		foreach (var (text2, itemInstance2) in cosmetics)
		{
			_playerOwnedCosmetics.Add(text2);
			if (itemInstance2 != null && itemInstance2.PurchaseDate.HasValue)
			{
				_playerOwnedCosmeticsAge[text2] = (int)(DateTime.UtcNow - itemInstance2.PurchaseDate.Value).TotalDays;
			}
		}
		CheckForEarlyAccess();
	}

	internal void AddCosmetic(string cosmeticId, int daysOwned = 0)
	{
		bool flag = _playerOwnedCosmetics.Add(cosmeticId);
		if (daysOwned >= 1)
		{
			int num = daysOwned;
			if (!flag && _playerOwnedCosmeticsAge.TryGetValue(cosmeticId, out var value))
			{
				num = Mathf.Max(value, num);
				_playerOwnedCosmeticsAge[cosmeticId] = num;
			}
			else
			{
				_playerOwnedCosmeticsAge[cosmeticId] = num;
			}
		}
	}

	internal bool HasCosmetic(string cosmeticId)
	{
		return _playerOwnedCosmetics.Contains(cosmeticId);
	}

	private short PackCompetitiveData()
	{
		if (!turningCompInitialized)
		{
			GorillaSnapTurningComp = GorillaTagger.Instance.GetComponent<GorillaSnapTurn>();
			turningCompInitialized = true;
		}
		fps = Mathf.Min(Mathf.RoundToInt(1f / Time.smoothDeltaTime), 255);
		int num = 0;
		if (GorillaSnapTurningComp != null)
		{
			turnFactor = GorillaSnapTurningComp.turnFactor;
			turnType = GorillaSnapTurningComp.turnType;
			string text = turnType;
			if (!(text == "SNAP"))
			{
				if (text == "SMOOTH")
				{
					num = 2;
				}
			}
			else
			{
				num = 1;
			}
			num *= 10;
			num += turnFactor;
		}
		return (short)(fps + (num << 8));
	}

	private void UnpackCompetitiveData(short packed)
	{
		int num = 255;
		fps = packed & num;
		int num2 = 31;
		int num3 = (packed >> 8) & num2;
		turnFactor = num3 % 10;
		switch (num3 / 10)
		{
		case 1:
			turnType = "SNAP";
			break;
		case 2:
			turnType = "SMOOTH";
			break;
		default:
			turnType = "NONE";
			break;
		}
	}

	private void OnKIDSessionUpdated(bool showCustomNames, Permission.ManagedByEnum managedBy)
	{
		bool flag = (showCustomNames || managedBy == Permission.ManagedByEnum.PLAYER) && managedBy != Permission.ManagedByEnum.PROHIBITED;
		GorillaComputer.instance.SetComputerSettingsBySafety(!flag, new GorillaComputer.ComputerState[1] { GorillaComputer.ComputerState.Name }, shouldHide: false);
		bool flag2 = PlayerPrefs.GetInt("nameTagsOn", -1) > 0;
		switch (managedBy)
		{
		case Permission.ManagedByEnum.PLAYER:
			flag = GorillaComputer.instance.NametagsEnabled;
			break;
		case Permission.ManagedByEnum.PROHIBITED:
			flag = false;
			break;
		case Permission.ManagedByEnum.GUARDIAN:
			flag = showCustomNames && flag2;
			break;
		}
		UpdateName(flag);
		Debug.Log("[KID] On Session Update - Custom Names Permission changed - Has enabled customNames? [" + flag + "]");
	}

	private IList<KeyValueStringPair> buildEntries()
	{
		return new KeyValueStringPair[2]
		{
			new KeyValueStringPair("Name", playerNameVisible),
			new KeyValueStringPair("Color", $"{Mathf.RoundToInt(playerColor.r * 9f)}, {Mathf.RoundToInt(playerColor.g * 9f)}, {Mathf.RoundToInt(playerColor.b * 9f)}")
		};
	}
}
