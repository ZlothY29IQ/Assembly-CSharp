using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AA;
using BoingKit;
using GorillaExtensions;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using GorillaLocomotion.Swimming;
using GorillaTag;
using GorillaTagScripts;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaLocomotion;

public class GTPlayer : MonoBehaviour
{
	[Serializable]
	public struct HandState
	{
		[NonSerialized]
		public Vector3 lastPosition;

		[NonSerialized]
		public Quaternion lastRotation;

		[NonSerialized]
		public bool isLeftHand;

		[NonSerialized]
		public bool wasColliding;

		[NonSerialized]
		public bool isColliding;

		[NonSerialized]
		public bool wasSliding;

		[NonSerialized]
		public bool isSliding;

		[NonSerialized]
		public bool isHolding;

		[NonSerialized]
		public Vector3 slideNormal;

		[NonSerialized]
		public float slipPercentage;

		[NonSerialized]
		public Vector3 hitPoint;

		[NonSerialized]
		private Vector3 boostVectorThisFrame;

		[NonSerialized]
		public Vector3 finalPositionThisFrame;

		[NonSerialized]
		public int slipSetToMaxFrameIdx;

		[NonSerialized]
		public int materialTouchIndex;

		[NonSerialized]
		public GorillaSurfaceOverride surfaceOverride;

		[NonSerialized]
		public RaycastHit hitInfo;

		[NonSerialized]
		public RaycastHit lastHitInfo;

		[NonSerialized]
		private GTPlayer gtPlayer;

		[SerializeField]
		public Transform handFollower;

		[SerializeField]
		public Transform controllerTransform;

		[SerializeField]
		public GorillaVelocityTracker velocityTracker;

		[SerializeField]
		public GorillaVelocityTracker interactPointVelocityTracker;

		[SerializeField]
		public Vector3 handOffset;

		[SerializeField]
		public Quaternion handRotOffset;

		[NonSerialized]
		public float tempFreezeUntilTimestamp;

		[NonSerialized]
		public bool canTag;

		[NonSerialized]
		public bool canStun;

		private float maxArmLength;

		[NonSerialized]
		public bool isActive;

		[NonSerialized]
		public float customBoostFactor;

		[NonSerialized]
		public bool hasCustomBoost;

		public void Init(GTPlayer gtPlayer, bool isLeftHand, float maxArmLength)
		{
			this.gtPlayer = gtPlayer;
			this.isLeftHand = isLeftHand;
			this.maxArmLength = maxArmLength;
			lastPosition = controllerTransform.position;
			lastRotation = controllerTransform.rotation;
			if (handFollower != null)
			{
				handFollower.transform.position = lastPosition;
				handFollower.transform.rotation = lastRotation;
			}
			wasColliding = false;
			slipSetToMaxFrameIdx = -1;
		}

		public void OnTeleport()
		{
			wasColliding = false;
			isColliding = false;
			isSliding = false;
			wasSliding = false;
			if (handFollower != null)
			{
				handFollower.position = controllerTransform.position;
				handFollower.rotation = controllerTransform.rotation;
			}
			lastPosition = controllerTransform.position;
			lastRotation = controllerTransform.rotation;
		}

		public Vector3 GetLastPosition()
		{
			return lastPosition + gtPlayer.MovingSurfaceMovement();
		}

		public bool SlipOverriddenToMax()
		{
			return slipSetToMaxFrameIdx == Time.frameCount;
		}

		public void FirstIteration(ref Vector3 totalMove, ref int divisor, float paddleBoostFactor)
		{
			if (hasCustomBoost)
			{
				boostVectorThisFrame = gtPlayer.turnParent.transform.rotation * -velocityTracker.GetAverageVelocity() * customBoostFactor;
			}
			else
			{
				boostVectorThisFrame = (gtPlayer.enableHoverMode ? (gtPlayer.turnParent.transform.rotation * -velocityTracker.GetAverageVelocity() * paddleBoostFactor) : Vector3.zero);
			}
			Vector3 vector = GetCurrentHandPosition() + gtPlayer.movingSurfaceOffset;
			Vector3 vector2 = GetLastPosition();
			Vector3 vector3 = vector - vector2;
			bool num = gtPlayer.lastMovingSurfaceContact == MovingSurfaceContactPoint.LEFT;
			if (!gtPlayer.didAJump && wasSliding && Vector3.Dot(gtPlayer.slideAverageNormal, GTPlayerTransform.PhysicsUp) > 0f)
			{
				vector3 += Vector3.Project(-gtPlayer.slideAverageNormal * gtPlayer.stickDepth * gtPlayer.scale, GTPlayerTransform.PhysicsDown);
			}
			float num2 = gtPlayer.minimumRaycastDistance * gtPlayer.scale;
			if (gtPlayer.IsFrozen && GorillaGameManager.instance is GorillaFreezeTagManager)
			{
				num2 = (gtPlayer.minimumRaycastDistance + VRRig.LocalRig.iceCubeRight.transform.localScale.y / 2f) * gtPlayer.scale;
			}
			Vector3 vector4 = Vector3.zero;
			if (num && !gtPlayer.exitMovingSurface)
			{
				vector4 = Vector3.Project(-gtPlayer.lastMovingSurfaceHit.normal * (gtPlayer.stickDepth * gtPlayer.scale), GTPlayerTransform.PhysicsDown);
				if (gtPlayer.scale < 0.5f)
				{
					Vector3 normalized = gtPlayer.MovingSurfaceMovement().normalized;
					if (normalized != Vector3.zero)
					{
						float num3 = Vector3.Dot(GTPlayerTransform.PhysicsUp, normalized);
						if ((double)num3 > 0.9 || (double)num3 < -0.9)
						{
							vector4 *= 6f;
							num2 *= 1.1f;
						}
					}
				}
			}
			Vector3 vector5;
			if (gtPlayer.IterativeCollisionSphereCast(vector2, num2, vector3 + vector4, boostVectorThisFrame, out var endPosition, singleHand: true, out slipPercentage, out var iterativeHitInfo, SlipOverriddenToMax()) && !isHolding && !gtPlayer.InReportMenu)
			{
				vector5 = ((!wasColliding || !(slipPercentage <= gtPlayer.defaultSlideFactor) || boostVectorThisFrame.IsLongerThan(0f)) ? (endPosition - vector) : (vector2 - vector));
				isSliding = slipPercentage > gtPlayer.iceThreshold;
				slideNormal = gtPlayer.tempHitInfo.normal;
				isColliding = true;
				materialTouchIndex = gtPlayer.currentMaterialIndex;
				surfaceOverride = gtPlayer.currentOverride;
				gtPlayer.lastHitInfoHand = iterativeHitInfo;
				lastHitInfo = iterativeHitInfo;
			}
			else
			{
				vector5 = Vector3.zero;
				slipPercentage = 0f;
				isSliding = false;
				slideNormal = GTPlayerTransform.PhysicsUp;
				isColliding = false;
				materialTouchIndex = 0;
				surfaceOverride = null;
			}
			bool flag = (isLeftHand ? gtPlayer.controllerState.LeftValid : gtPlayer.controllerState.RightValid);
			isColliding &= flag;
			isSliding &= flag;
			if (isColliding)
			{
				gtPlayer.anyHandIsColliding = true;
				if (isSliding)
				{
					gtPlayer.anyHandIsSliding = true;
				}
				else
				{
					gtPlayer.anyHandIsSticking = true;
				}
			}
			if (isColliding || wasColliding)
			{
				if (!surfaceOverride || !surfaceOverride.disablePushBackEffect)
				{
					totalMove += vector5;
				}
				divisor++;
			}
		}

		public void FinalizeHandPosition()
		{
			Vector3 vector = GetLastPosition();
			if (Time.time < tempFreezeUntilTimestamp)
			{
				finalPositionThisFrame = vector;
			}
			else
			{
				Vector3 movementVector = GetCurrentHandPosition() - vector;
				float sphereRadius = gtPlayer.minimumRaycastDistance * gtPlayer.scale;
				if (gtPlayer.IsFrozen && GorillaGameManager.instance is GorillaFreezeTagManager)
				{
					sphereRadius = (gtPlayer.minimumRaycastDistance + VRRig.LocalRig.iceCubeRight.transform.localScale.y / 2f) * gtPlayer.scale;
				}
				if (gtPlayer.IterativeCollisionSphereCast(vector, sphereRadius, movementVector, boostVectorThisFrame, out var endPosition, gtPlayer.areBothTouching, out var num, out var iterativeHitInfo, fullSlide: false) && !isHolding)
				{
					isColliding = true;
					isSliding = num > gtPlayer.iceThreshold;
					materialTouchIndex = gtPlayer.currentMaterialIndex;
					surfaceOverride = gtPlayer.currentOverride;
					gtPlayer.lastHitInfoHand = iterativeHitInfo;
					lastHitInfo = iterativeHitInfo;
					finalPositionThisFrame = endPosition;
				}
				else
				{
					finalPositionThisFrame = GetCurrentHandPosition();
				}
			}
			bool flag = (isLeftHand ? gtPlayer.controllerState.LeftValid : gtPlayer.controllerState.RightValid);
			isColliding &= flag;
			isSliding &= flag;
			if (isColliding)
			{
				gtPlayer.anyHandIsColliding = true;
				if (isSliding)
				{
					gtPlayer.anyHandIsSliding = true;
				}
				else
				{
					gtPlayer.anyHandIsSticking = true;
				}
			}
		}

		public bool IsSlipOverriddenToMax()
		{
			return slipSetToMaxFrameIdx == Time.frameCount;
		}

		public Vector3 GetCurrentHandPosition()
		{
			Vector3 position = gtPlayer.headCollider.transform.position;
			if (gtPlayer.inOverlay)
			{
				return position + gtPlayer.headCollider.transform.up * -0.5f * gtPlayer.scale;
			}
			Vector3 vector = gtPlayer.PositionWithOffset(controllerTransform, handOffset);
			if ((vector - position).IsShorterThan(maxArmLength * gtPlayer.scale))
			{
				return vector;
			}
			return position + (vector - position).normalized * maxArmLength * gtPlayer.scale;
		}

		public void PositionHandFollower()
		{
			handFollower.position = finalPositionThisFrame;
			handFollower.rotation = lastRotation;
		}

		public void OnEndOfFrame()
		{
			wasColliding = isColliding;
			wasSliding = isSliding;
			lastPosition = finalPositionThisFrame;
			if (Time.time > tempFreezeUntilTimestamp)
			{
				lastRotation = controllerTransform.rotation * handRotOffset;
			}
		}

		public void TempFreezeHand(float freezeDuration)
		{
			tempFreezeUntilTimestamp = Math.Max(tempFreezeUntilTimestamp, Time.time + freezeDuration);
		}

		public void GetHandTapData(out bool wasHandTouching, out bool wasSliding, out int handMatIndex, out GorillaSurfaceOverride surfaceOverride, out RaycastHit handHitInfo, out Vector3 handPosition, out GorillaVelocityTracker handVelocityTracker)
		{
			wasHandTouching = wasColliding;
			wasSliding = this.wasSliding;
			handMatIndex = materialTouchIndex;
			surfaceOverride = this.surfaceOverride;
			handHitInfo = lastHitInfo;
			handPosition = finalPositionThisFrame;
			handVelocityTracker = velocityTracker;
		}
	}

	private enum MovingSurfaceContactPoint
	{
		NONE,
		RIGHT,
		LEFT,
		BODY
	}

	[Serializable]
	public struct MaterialData
	{
		public string matName;

		public bool overrideAudio;

		public AudioClip audio;

		public bool overrideSlidePercent;

		public float slidePercent;

		public int surfaceEffectIndex;
	}

	[Serializable]
	public struct LiquidProperties
	{
		[Range(0f, 2f)]
		[Tooltip("0: no resistance just like air, 1: full resistance like solid geometry")]
		public float resistance;

		[Range(0f, 3f)]
		[Tooltip("0: no buoyancy. 1: Fully compensates gravity. 2: net force is upwards equal to gravity")]
		public float buoyancy;

		[Range(0f, 3f)]
		[Tooltip("Damping Half-life Multiplier")]
		public float dampingFactor;

		[Range(0f, 1f)]
		public float surfaceJumpFactor;
	}

	public enum LiquidType
	{
		Water,
		Lava,
		SwimInAir
	}

	private struct HoverBoardCast
	{
		public Vector3 localOrigin;

		public Vector3 localDirection;

		public float sphereRadius;

		public float distance;

		public float intersectToVelocityCap;

		public bool isSolid;

		public bool didHit;

		public Vector3 pointHit;

		public Vector3 normalHit;
	}

	private struct HandHoldState
	{
		public GorillaGrabber grabber;

		public Transform objectHeld;

		public Vector3 localPositionHeld;

		public float localRotationalOffset;

		public bool applyRotation;
	}

	public static LayerMask LocomotionEnabledLayers = 201327105;

	private static GTPlayer _instance;

	public static bool hasInstance = false;

	public Camera mainCamera;

	public SphereCollider headCollider;

	public CapsuleCollider bodyCollider;

	private float bodyInitialRadius;

	private float _bodyInitialHeight;

	private float currentBodyHeight;

	private double frameCount;

	private RaycastHit bodyHitInfo;

	private RaycastHit lastHitInfoHand;

	public GorillaVelocityTracker bodyVelocityTracker;

	public PlayerAudioManager audioManager;

	[SerializeField]
	private HandState leftHand;

	[SerializeField]
	private HandState rightHand;

	private HandState[] stiltStates = new HandState[12];

	private bool anyHandIsColliding;

	private bool anyHandWasColliding;

	private bool anyHandIsSliding;

	private bool anyHandWasSliding;

	private bool anyHandIsSticking;

	private bool anyHandWasSticking;

	private bool forceRBSync;

	public Vector3 lastHeadPosition;

	private Vector3 lastRigidbodyPosition;

	private RigidbodyInterpolation playerRigidbodyInterpolationDefault;

	public int velocityHistorySize;

	public float maxArmLength = 1f;

	public float unStickDistance = 1f;

	public float velocityLimit;

	public float slideVelocityLimit;

	public float maxJumpSpeed;

	private float _jumpMultiplier;

	public float minimumRaycastDistance = 0.05f;

	public float defaultSlideFactor = 0.03f;

	public float slidingMinimum = 0.9f;

	public float defaultPrecision = 0.995f;

	public float teleportThresholdNoVel = 1f;

	public float frictionConstant = 1f;

	public float slideControl = 0.00425f;

	public float stickDepth = 0.01f;

	private Vector3[] velocityHistory;

	private Vector3[] slideAverageHistory;

	private int velocityIndex;

	private Vector3 currentVelocity;

	private Vector3 averagedVelocity;

	private Vector3 lastPosition;

	public Vector3 bodyOffset;

	public LayerMask locomotionEnabledLayers;

	public LayerMask waterLayer;

	public bool wasHeadTouching;

	public int currentMaterialIndex;

	public Vector3 headSlideNormal;

	public float headSlipPercentage;

	[SerializeField]
	private Transform cosmeticsHeadTarget;

	[SerializeField]
	private float nativeScale = 1f;

	[SerializeField]
	private float scaleMultiplier = 1f;

	private NativeSizeChangerSettings activeSizeChangerSettings;

	public bool debugMovement;

	public bool disableMovement;

	[NonSerialized]
	public bool inOverlay;

	[NonSerialized]
	public bool isUserPresent;

	public GameObject turnParent;

	[SerializeField]
	public GameObject RecordingRig;

	public GorillaSurfaceOverride currentOverride;

	public MaterialDatasSO materialDatasSO;

	private float degreesTurnedThisFrame;

	private Vector3 bodyOffsetVector;

	private Vector3 movementToProjectedAboveCollisionPlane;

	private MeshCollider meshCollider;

	private Mesh collidedMesh;

	private MaterialData foundMatData;

	private string findMatName;

	private int vertex1;

	private int vertex2;

	private int vertex3;

	private List<int> trianglesList = new List<int>(1000000);

	private Dictionary<Mesh, int[]> meshTrianglesDict = new Dictionary<Mesh, int[]>(128);

	private int[] sharedMeshTris;

	private float lastRealTime;

	private float calcDeltaTime;

	private float tempRealTime;

	private Vector3 slideVelocity;

	private Vector3 slideAverageNormal;

	private RaycastHit tempHitInfo;

	private RaycastHit junkHit;

	private Vector3 firstPosition;

	private RaycastHit tempIterativeHit;

	private float maxSphereSize1;

	private float maxSphereSize2;

	private Collider[] overlapColliders = new Collider[10];

	private int overlapAttempts;

	private float averageSlipPercentage;

	private Vector3 surfaceDirection;

	public float iceThreshold = 0.9f;

	private float bodyMaxRadius;

	public float bodyLerp = 0.17f;

	private bool areBothTouching;

	private float slideFactor;

	[DebugOption]
	public bool didAJump;

	private bool updateRB;

	private Renderer slideRenderer;

	private RaycastHit[] rayCastNonAllocColliders;

	private Vector3[] crazyCheckVectors;

	private RaycastHit emptyHit;

	private int bufferCount;

	private Vector3 lastOpenHeadPosition;

	private List<Material> tempMaterialArray = new List<Material>(16);

	private Vector3? antiDriftLastPosition;

	private const float CameraFarClipDefault = 500f;

	private const float CameraNearClipDefault = 0.01f;

	private const float CameraNearClipTiny = 0.002f;

	private Dictionary<GameObject, PhysicsMaterial> bodyTouchedSurfaces;

	private bool primaryButtonPressed = true;

	[Header("Swimming")]
	public PlayerSwimmingParameters swimmingParams;

	public WaterParameters waterParams;

	public List<LiquidProperties> liquidPropertiesList = new List<LiquidProperties>(16);

	public bool debugDrawSwimming;

	[Header("Slam/Hit effects")]
	public GameObject wizardStaffSlamEffects;

	public GameObject geodeHitEffects;

	[Header("Freeze Tag")]
	public float freezeTagHandSlidePercent = 0.88f;

	public bool debugFreezeTag;

	public float frozenBodyBuoyancyFactor = 1.5f;

	[Space]
	private WaterVolume leftHandWaterVolume;

	private WaterVolume rightHandWaterVolume;

	private WaterVolume.SurfaceQuery leftHandWaterSurface;

	private WaterVolume.SurfaceQuery rightHandWaterSurface;

	private Vector3 swimmingVelocity = Vector3.zero;

	private WaterVolume.SurfaceQuery waterSurfaceForHead;

	private bool bodyInWater;

	private bool headInWater;

	private bool audioSetToUnderwater;

	private float buoyancyExtension;

	private float lastWaterSurfaceJumpTimeLeft = -1f;

	private float lastWaterSurfaceJumpTimeRight = -1f;

	private float waterSurfaceJumpCooldown = 0.1f;

	private float leftHandNonDiveHapticsAmount;

	private float rightHandNonDiveHapticsAmount;

	private List<WaterVolume> headOverlappingWaterVolumes = new List<WaterVolume>(16);

	private List<WaterVolume> bodyOverlappingWaterVolumes = new List<WaterVolume>(16);

	private List<WaterCurrent> activeWaterCurrents = new List<WaterCurrent>(16);

	private Quaternion playerRotationOverride = Quaternion.identity;

	private int playerRotationOverrideFrame = -1;

	private float playerRotationOverrideDecayRate = Mathf.Exp(1.5f);

	private ContactPoint[] bodyCollisionContacts = new ContactPoint[8];

	private int bodyCollisionContactsCount;

	private ContactPoint bodyGroundContact;

	private float bodyGroundContactTime;

	private const float movingSurfaceVelocityLimit = 40f;

	private bool exitMovingSurface;

	private float exitMovingSurfaceThreshold = 6f;

	private bool isClimbableMoving;

	private Quaternion lastClimbableRotation;

	private int lastAttachedToMovingSurfaceFrame;

	private const int MIN_FRAMES_OFF_SURFACE_TO_DETACH = 3;

	private bool isHandHoldMoving;

	private Quaternion lastHandHoldRotation;

	private Vector3 movingHandHoldReleaseVelocity;

	private MovingSurfaceContactPoint lastMovingSurfaceContact;

	private int lastMovingSurfaceID = -1;

	private BuilderPiece lastMonkeBlock;

	private Quaternion lastMovingSurfaceRot;

	private RaycastHit lastMovingSurfaceHit;

	private Vector3 lastMovingSurfaceTouchLocal;

	private Vector3 lastMovingSurfaceTouchWorld;

	private Vector3 movingSurfaceOffset;

	private bool wasMovingSurfaceMonkeBlock;

	private Vector3 lastMovingSurfaceVelocity;

	private bool wasBodyOnGround;

	private BasePlatform currentPlatform;

	private BasePlatform lastPlatformTouched;

	private Vector3 lastFrameTouchPosLocal;

	private Vector3 lastFrameTouchPosWorld;

	private bool lastFrameHasValidTouchPos;

	private Vector3 refMovement = Vector3.zero;

	private Vector3 platformTouchOffset;

	private Vector3 debugLastRightHandPosition;

	private Vector3 debugPlatformDeltaPosition;

	public double tempFreezeRightHandEnableTime;

	public double tempFreezeLeftHandEnableTime;

	private const float climbingMaxThrowSpeed = 5.5f;

	private const float climbHelperSmoothSnapSpeed = 12f;

	[NonSerialized]
	public bool isClimbing;

	private GorillaClimbable currentClimbable;

	private GorillaHandClimber currentClimber;

	private Vector3 climbHelperTargetPos = Vector3.zero;

	private Transform climbHelper;

	private GorillaRopeSwing currentSwing;

	private GorillaZipline currentZipline;

	[SerializeField]
	private ConnectedControllerHandler controllerState;

	public int sizeLayerMask;

	public bool InReportMenu;

	private LayerChanger layerChanger;

	private bool hasCorrectedForTracking;

	private float halloweenLevitationStrength;

	private float halloweenLevitationFullStrengthDuration;

	private float halloweenLevitationTotalDuration = 1f;

	private float halloweenLevitationBonusStrength;

	private float halloweenLevitateBonusOffAtYSpeed;

	private float halloweenLevitateBonusFullAtYSpeed = 1f;

	private float lastTouchedGroundTimestamp;

	private bool teleportToTrain;

	public bool isAttachedToTrain;

	private bool stuckLeft;

	private bool stuckRight;

	private float lastScale;

	private Vector3 currentSlopDirection;

	private Vector3 lastSlopeDirection = Vector3.zero;

	private readonly Dictionary<UnityEngine.Object, Action<GTPlayer>> gravityOverrides = new Dictionary<UnityEngine.Object, Action<GTPlayer>>();

	private int hoverAllowedCount;

	[Header("Hoverboard")]
	[SerializeField]
	private float hoverIdealHeight = 0.5f;

	[SerializeField]
	private float hoverCarveSidewaysSpeedLossFactor = 1f;

	[SerializeField]
	private AnimationCurve hoverCarveAngleResponsiveness;

	[SerializeField]
	private HoverboardVisual hoverboardVisual;

	[SerializeField]
	private float sidewaysDrag = 0.1f;

	[SerializeField]
	private float hoveringSlowSpeed = 0.1f;

	[SerializeField]
	private float hoveringSlowStoppingFactor = 0.95f;

	[SerializeField]
	private float hoverboardPaddleBoostMultiplier = 0.1f;

	[SerializeField]
	private float hoverboardPaddleBoostMax = 10f;

	[SerializeField]
	private float hoverboardBoostGracePeriod = 1f;

	[SerializeField]
	private float hoverBodyHasCollisionsOutsideRadius = 0.5f;

	[SerializeField]
	private float hoverBodyCollisionRadiusUpOffset = 0.2f;

	[SerializeField]
	private float hoverGeneralUpwardForce = 8f;

	[SerializeField]
	private float hoverTiltAdjustsForwardFactor = 0.2f;

	[SerializeField]
	private float hoverMinGrindSpeed = 1f;

	[SerializeField]
	private float hoverSlamJumpStrengthFactor = 25f;

	[SerializeField]
	private float hoverMaxPaddleSpeed = 35f;

	[SerializeField]
	private HoverboardAudio hoverboardAudio;

	private bool hasHoverPoint;

	private float boostEnabledUntilTimestamp;

	private HoverBoardCast[] hoverboardCasts = new HoverBoardCast[3]
	{
		new HoverBoardCast
		{
			localOrigin = new Vector3(0f, 1f, 0.36f),
			localDirection = Vector3.down,
			distance = 1f,
			sphereRadius = 0.2f,
			intersectToVelocityCap = 0.1f
		},
		new HoverBoardCast
		{
			localOrigin = new Vector3(0f, 0.05f, 0.36f),
			localDirection = Vector3.forward,
			distance = 0.25f,
			sphereRadius = 0.01f,
			intersectToVelocityCap = 0f,
			isSolid = true
		},
		new HoverBoardCast
		{
			localOrigin = new Vector3(0f, 0.05f, -0.1f),
			localDirection = -Vector3.forward,
			distance = 0.24f,
			sphereRadius = 0.01f,
			intersectToVelocityCap = 0f,
			isSolid = true
		}
	};

	private Vector3 hoverboardPlayerLocalPos;

	private Quaternion hoverboardPlayerLocalRot;

	private bool didHoverLastFrame;

	private bool hasLeftHandTentacleMove;

	private bool hasRightHandTentacleMove;

	private Vector3 leftHandTentacleMove;

	private Vector3 rightHandTentacleMove;

	private HandHoldState activeHandHold;

	private HandHoldState secondaryHandHold;

	public PhysicsMaterial slipperyMaterial;

	private bool wasHoldingHandhold;

	private Vector3 secondLastPreHandholdVelocity;

	private Vector3 lastPreHandholdVelocity;

	[Header("Native Scale Adjustment")]
	[SerializeField]
	private AnimationCurve nativeScaleMagnitudeAdjustmentFactor;

	public static GTPlayer Instance => _instance;

	private float bodyInitialHeight
	{
		get
		{
			if (GorillaIK.playerIK == null || !GorillaIK.playerIK.usingUpdatedIK)
			{
				return _bodyInitialHeight;
			}
			return Mathf.Max(0.2f, Vector3.Dot(GorillaIK.playerIK.bodyBone.up, GTPlayerTransform.Up)) * _bodyInitialHeight;
		}
	}

	public HandState LeftHand => leftHand;

	public ref readonly HandState LeftHandRef => ref leftHand;

	public HandState RightHand => rightHand;

	public ref readonly HandState RightHandRef => ref rightHand;

	public Rigidbody playerRigidBody { get; private set; }

	public Vector3 LastPosition => lastPosition;

	public Vector3 InstantaneousVelocity => currentVelocity;

	public Vector3 AveragedVelocity => averagedVelocity;

	public Transform CosmeticsHeadTarget => cosmeticsHeadTarget;

	public float scale => scaleMultiplier * nativeScale;

	public float NativeScale => nativeScale;

	public float ScaleMultiplier => scaleMultiplier;

	public bool IsDefaultScale => Mathf.Abs(1f - scale) < 0.001f;

	public bool turnedThisFrame => degreesTurnedThisFrame != 0f;

	public List<MaterialData> materialData => materialDatasSO.datas;

	protected bool IsFrozen { get; set; }

	public bool forcedUnderwater { get; set; }

	public float siJumpMultiplier { get; set; } = 1f;

	public List<WaterVolume> HeadOverlappingWaterVolumes => headOverlappingWaterVolumes;

	public bool InWater => bodyInWater;

	public bool HeadInWater => headInWater;

	public WaterVolume CurrentWaterVolume
	{
		get
		{
			if (bodyOverlappingWaterVolumes.Count <= 0)
			{
				return null;
			}
			return bodyOverlappingWaterVolumes[0];
		}
	}

	public WaterVolume.SurfaceQuery WaterSurfaceForHead => waterSurfaceForHead;

	public WaterVolume LeftHandWaterVolume => leftHandWaterVolume;

	public WaterVolume RightHandWaterVolume => rightHandWaterVolume;

	public WaterVolume.SurfaceQuery LeftHandWaterSurface => leftHandWaterSurface;

	public WaterVolume.SurfaceQuery RightHandWaterSurface => rightHandWaterSurface;

	public Vector3 LastLeftHandPosition => leftHand.lastPosition;

	public Vector3 LastRightHandPosition => rightHand.lastPosition;

	public Vector3 RigidbodyVelocity => playerRigidBody.linearVelocity;

	public Vector3 HeadCenterPosition => headCollider.transform.position + headCollider.transform.rotation * new Vector3(0f, 0f, -0.11f);

	public bool HandContactingSurface
	{
		get
		{
			if (!leftHand.isColliding)
			{
				return rightHand.isColliding;
			}
			return true;
		}
	}

	public bool BodyOnGround => bodyGroundContactTime >= Time.time - 0.05f;

	public bool IsGroundedHand
	{
		get
		{
			if (!HandContactingSurface && !isClimbing && !leftHand.isHolding)
			{
				return rightHand.isHolding;
			}
			return true;
		}
	}

	public bool IsGroundedButt => BodyOnGround;

	public int TentacleActiveAtFrame { get; set; }

	public bool IsTentacleActive => TentacleActiveAtFrame >= Time.frameCount;

	public int LaserZiplineActiveAtFrame { get; set; }

	public bool IsLaserZiplineActive => LaserZiplineActiveAtFrame >= Time.frameCount;

	public int ThrusterActiveAtFrame { get; set; }

	public bool IsThrusterActive => ThrusterActiveAtFrame >= Time.frameCount;

	public Quaternion PlayerRotationOverride
	{
		set
		{
			playerRotationOverride = value;
			playerRotationOverrideFrame = Time.frameCount;
		}
	}

	public bool IsBodySliding { get; set; }

	public bool bodyGroundIsSlippery { get; private set; }

	public GorillaClimbable CurrentClimbable => currentClimbable;

	public GorillaHandClimber CurrentClimber => currentClimber;

	public float jumpMultiplier
	{
		get
		{
			return _jumpMultiplier;
		}
		set
		{
			_jumpMultiplier = value;
		}
	}

	public float LastTouchedGroundAtNetworkTime { get; private set; }

	public float LastHandTouchedGroundAtNetworkTime { get; private set; }

	public int GravityOverrideCount => gravityOverrides.Count;

	public bool isHoverAllowed { get; private set; }

	public bool enableHoverMode { get; private set; }

	public RigidbodyInterpolation RigidbodyInterpolation
	{
		get
		{
			return playerRigidBody.interpolation;
		}
		set
		{
			playerRigidBody.interpolation = value;
		}
	}

	public int GetMaterialTouchIndex(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.materialTouchIndex;
	}

	public GorillaSurfaceOverride GetSurfaceOverride(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.surfaceOverride;
	}

	public RaycastHit GetTouchHitInfo(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.hitInfo;
	}

	public bool IsHandTouching(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.wasColliding;
	}

	public GorillaVelocityTracker GetHandVelocityTracker(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.velocityTracker;
	}

	public GorillaVelocityTracker GetInteractPointVelocityTracker(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.interactPointVelocityTracker;
	}

	public Transform GetControllerTransform(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.controllerTransform;
	}

	public Transform GetHandFollower(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.handFollower;
	}

	public Vector3 GetHandOffset(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.handOffset;
	}

	public Quaternion GetHandRotOffset(bool isLeftHand)
	{
		HandState obj = (isLeftHand ? leftHand : rightHand);
		return obj.handRotOffset;
	}

	public Vector3 GetHandPosition(bool isLeftHand, StiltID stiltID = StiltID.None)
	{
		HandState obj = ((stiltID != StiltID.None) ? stiltStates[(int)stiltID] : (isLeftHand ? leftHand : rightHand));
		return obj.lastPosition;
	}

	public void GetHandTapData(bool isLeftHand, StiltID stiltID, out bool wasHandTouching, out bool wasSliding, out int handMatIndex, out GorillaSurfaceOverride surfaceOverride, out RaycastHit handHitInfo, out Vector3 handPosition, out GorillaVelocityTracker handVelocityTracker)
	{
		((stiltID != StiltID.None) ? stiltStates[(int)stiltID] : (isLeftHand ? leftHand : rightHand)).GetHandTapData(out wasHandTouching, out wasSliding, out handMatIndex, out surfaceOverride, out handHitInfo, out handPosition, out handVelocityTracker);
	}

	public void SetHandOffsets(bool isLeftHand, Vector3 handOffset, Quaternion handRotOffset)
	{
		if (isLeftHand)
		{
			leftHand.handOffset = handOffset;
			leftHand.handRotOffset = handRotOffset;
		}
		else
		{
			rightHand.handOffset = handOffset;
			rightHand.handRotOffset = handRotOffset;
		}
	}

	public void SetScaleMultiplier(float s)
	{
		scaleMultiplier = s;
	}

	public void SetNativeScale(NativeSizeChangerSettings s)
	{
		float num = nativeScale;
		if (s != null && s.playerSizeScale > 0f && s.playerSizeScale != 1f)
		{
			activeSizeChangerSettings = s;
		}
		else
		{
			activeSizeChangerSettings = null;
		}
		if (activeSizeChangerSettings == null)
		{
			nativeScale = 1f;
		}
		else
		{
			nativeScale = activeSizeChangerSettings.playerSizeScale;
		}
		if (num != nativeScale && NetworkSystem.Instance.InRoom)
		{
			_ = GorillaTagger.Instance.myVRRig != null;
		}
	}

	public void EnableStilt(StiltID stiltID, bool isLeftHand, Vector3 currentTipWorldPos, float maxArmLength, bool canTag, bool canStun, float customBoostFactor = 0f, GorillaVelocityTracker velocityTracker = null)
	{
		HandState[] array = stiltStates;
		HandState handState = new HandState
		{
			isActive = true
		};
		HandState obj = (isLeftHand ? leftHand : rightHand);
		handState.controllerTransform = obj.controllerTransform;
		GorillaVelocityTracker velocityTracker2;
		if (!(velocityTracker != null))
		{
			HandState obj2 = (isLeftHand ? leftHand : rightHand);
			velocityTracker2 = obj2.velocityTracker;
		}
		else
		{
			velocityTracker2 = velocityTracker;
		}
		handState.velocityTracker = velocityTracker2;
		handState.handRotOffset = Quaternion.identity;
		handState.canTag = canTag;
		handState.canStun = canStun;
		handState.customBoostFactor = customBoostFactor;
		handState.hasCustomBoost = customBoostFactor > 0f;
		array[(int)stiltID] = handState;
		stiltStates[(int)stiltID].Init(this, isLeftHand, maxArmLength);
		UpdateStiltOffset(stiltID, currentTipWorldPos);
	}

	public void DisableStilt(StiltID stiltID)
	{
		stiltStates[(int)stiltID].isActive = false;
	}

	public void UpdateStiltOffset(StiltID stiltID, Vector3 currentTipWorldPos)
	{
		stiltStates[(int)stiltID].handOffset = stiltStates[(int)stiltID].controllerTransform.InverseTransformPoint(currentTipWorldPos);
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
			hasInstance = true;
		}
		InitializeValues();
		playerRigidbodyInterpolationDefault = playerRigidBody.interpolation;
		playerRigidBody.maxAngularVelocity = 0f;
		bodyOffsetVector = new Vector3(0f, (0f - bodyCollider.height) / 2f, 0f);
		_bodyInitialHeight = bodyCollider.height;
		bodyInitialRadius = bodyCollider.radius;
		rayCastNonAllocColliders = new RaycastHit[5];
		crazyCheckVectors = new Vector3[7];
		emptyHit = default(RaycastHit);
		crazyCheckVectors[0] = Vector3.up;
		crazyCheckVectors[1] = Vector3.down;
		crazyCheckVectors[2] = Vector3.left;
		crazyCheckVectors[3] = Vector3.right;
		crazyCheckVectors[4] = Vector3.forward;
		crazyCheckVectors[5] = Vector3.back;
		crazyCheckVectors[6] = Vector3.zero;
		if (controllerState == null)
		{
			controllerState = GetComponent<ConnectedControllerHandler>();
		}
		layerChanger = GetComponent<LayerChanger>();
		bodyTouchedSurfaces = new Dictionary<GameObject, PhysicsMaterial>();
		if (Application.isPlaying)
		{
			Application.onBeforeRender += OnBeforeRenderInit;
		}
	}

	protected void Start()
	{
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
		}
		mainCamera.farClipPlane = 500f;
		lastScale = scale;
		layerChanger.InitializeLayers(base.transform);
		float degrees = Quaternion.Angle(Quaternion.identity, GorillaTagger.Instance.offlineVRRig.transform.rotation) * Mathf.Sign(Vector3.Dot(Vector3.up, GorillaTagger.Instance.offlineVRRig.transform.right));
		Turn(degrees);
	}

	protected void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
			hasInstance = false;
		}
		if ((bool)climbHelper)
		{
			UnityEngine.Object.Destroy(climbHelper.gameObject);
		}
	}

	public void InitializeValues()
	{
		Physics.SyncTransforms();
		playerRigidBody = GetComponent<Rigidbody>();
		velocityHistory = new Vector3[velocityHistorySize];
		slideAverageHistory = new Vector3[velocityHistorySize];
		for (int i = 0; i < velocityHistory.Length; i++)
		{
			velocityHistory[i] = Vector3.zero;
			slideAverageHistory[i] = Vector3.zero;
		}
		leftHand.Init(this, isLeftHand: true, maxArmLength);
		rightHand.Init(this, isLeftHand: false, maxArmLength);
		lastHeadPosition = headCollider.transform.position;
		velocityIndex = 0;
		averagedVelocity = Vector3.zero;
		slideVelocity = Vector3.zero;
		lastPosition = base.transform.position;
		lastRealTime = Time.realtimeSinceStartup;
		lastOpenHeadPosition = headCollider.transform.position;
		bodyCollider.transform.position = PositionWithOffset(headCollider.transform, bodyOffset) + bodyOffsetVector;
		bodyCollider.transform.eulerAngles = new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);
		ForceRigidBodySync();
	}

	public void SetHalloweenLevitation(float levitateStrength, float levitateDuration, float levitateBlendOutDuration, float levitateBonusStrength, float levitateBonusOffAtYSpeed, float levitateBonusFullAtYSpeed)
	{
		halloweenLevitationStrength = levitateStrength;
		halloweenLevitationFullStrengthDuration = levitateDuration;
		halloweenLevitationTotalDuration = levitateDuration + levitateBlendOutDuration;
		halloweenLevitateBonusFullAtYSpeed = levitateBonusFullAtYSpeed;
		halloweenLevitateBonusOffAtYSpeed = levitateBonusFullAtYSpeed;
		halloweenLevitationBonusStrength = levitateBonusStrength;
	}

	public void TeleportToTrain(bool enable)
	{
		teleportToTrain = enable;
	}

	public void TeleportTo(Vector3 position, Quaternion rotation, bool keepVelocity = false, bool center = false)
	{
		if (center)
		{
			Vector3 position2 = base.transform.position;
			Vector3 vector = mainCamera.transform.position - position2;
			position -= vector;
		}
		ClearHandHolds();
		if (playerRigidBody != null)
		{
			playerRigidBody.isKinematic = true;
			playerRigidBody.position = position;
			playerRigidBody.rotation = rotation;
			playerRigidBody.isKinematic = false;
		}
		playerRigidBody.position = position;
		playerRigidBody.rotation = rotation;
		base.transform.position = position;
		base.transform.rotation = rotation;
		lastHeadPosition = headCollider.transform.position;
		lastPosition = position;
		lastOpenHeadPosition = headCollider.transform.position;
		leftHand.OnTeleport();
		rightHand.OnTeleport();
		for (int i = 0; i < 12; i++)
		{
			if (stiltStates[i].isActive)
			{
				stiltStates[i].OnTeleport();
			}
		}
		if (!keepVelocity)
		{
			playerRigidBody.linearVelocity = Vector3.zero;
		}
		bodyCollider.transform.position = PositionWithOffset(headCollider.transform, bodyOffset) + bodyOffsetVector;
		bodyCollider.transform.eulerAngles = new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);
		Physics.SyncTransforms();
		GorillaTagger.Instance.offlineVRRig.transform.position = position;
		GorillaTagger.Instance.offlineVRRig.leftHandLink.BreakLink();
		GorillaTagger.Instance.offlineVRRig.rightHandLink.BreakLink();
		ForceRigidBodySync();
	}

	public void TeleportTo(Transform destination, bool matchDestinationRotation = true, bool maintainVelocity = true)
	{
		Vector3 position = base.transform.position;
		Vector3 vector = mainCamera.transform.position - position;
		Vector3 position2 = destination.position - vector;
		float num = destination.rotation.eulerAngles.y - mainCamera.transform.rotation.eulerAngles.y;
		Vector3 playerVelocity = currentVelocity;
		if (!maintainVelocity)
		{
			SetPlayerVelocity(Vector3.zero);
		}
		else if (matchDestinationRotation)
		{
			playerVelocity = Quaternion.AngleAxis(num, base.transform.up) * currentVelocity;
			SetPlayerVelocity(playerVelocity);
		}
		if (matchDestinationRotation)
		{
			Turn(num);
		}
		TeleportTo(position2, base.transform.rotation);
		if (maintainVelocity)
		{
			SetPlayerVelocity(playerVelocity);
		}
		ForceRigidBodySync();
	}

	public void AddForce(Vector3 force, ForceMode mode)
	{
		if (mode == ForceMode.VelocityChange)
		{
			playerRigidBody.AddForce(force * playerRigidBody.mass, ForceMode.Impulse);
		}
		else
		{
			playerRigidBody.AddForce(force, mode);
		}
	}

	public void SetPlayerVelocity(Vector3 newVelocity)
	{
		for (int i = 0; i < velocityHistory.Length; i++)
		{
			velocityHistory[i] = newVelocity;
		}
		playerRigidBody.AddForce(newVelocity - playerRigidBody.linearVelocity, ForceMode.VelocityChange);
	}

	public void SetGravityOverride(UnityEngine.Object caller, Action<GTPlayer> gravityFunction)
	{
		gravityOverrides[caller] = gravityFunction;
	}

	public void UnsetGravityOverride(UnityEngine.Object caller)
	{
		gravityOverrides.Remove(caller);
	}

	private void ApplyGravityOverrides()
	{
		foreach (KeyValuePair<UnityEngine.Object, Action<GTPlayer>> gravityOverride in gravityOverrides)
		{
			gravityOverride.Value(this);
		}
	}

	public void ApplyKnockback(Vector3 direction, float speed, bool forceOffTheGround = false)
	{
		if (forceOffTheGround)
		{
			if (leftHand.wasColliding || rightHand.wasColliding)
			{
				leftHand.wasColliding = false;
				rightHand.wasColliding = false;
				playerRigidBody.transform.position += minimumRaycastDistance * scale * Vector3.up;
			}
			didAJump = true;
			SetMaximumSlipThisFrame();
		}
		if (speed > 0.01f)
		{
			float num = Vector3.Dot(averagedVelocity, direction);
			float num2 = Mathf.InverseLerp(1.5f, 0.5f, num / speed);
			Vector3 vector = averagedVelocity + direction * speed * num2;
			playerRigidBody.linearVelocity = vector;
			for (int i = 0; i < velocityHistory.Length; i++)
			{
				velocityHistory[i] = vector;
			}
		}
	}

	public void ApplyClampedKnockback(Vector3 direction, float speed, float boostMultiplier, bool forceOffTheGround = false)
	{
		if (forceOffTheGround)
		{
			if (leftHand.wasColliding || rightHand.wasColliding)
			{
				leftHand.wasColliding = false;
				rightHand.wasColliding = false;
				playerRigidBody.transform.position += minimumRaycastDistance * scale * Vector3.up;
			}
			didAJump = true;
			SetMaximumSlipThisFrame();
		}
		if (!(speed > 0.01f))
		{
			return;
		}
		float num = Vector3.Dot(playerRigidBody.linearVelocity, direction.normalized);
		if (!(num >= speed))
		{
			float num2 = Mathf.Clamp(speed - num, 0f, speed * boostMultiplier);
			Vector3 vector = playerRigidBody.linearVelocity + direction.normalized * num2;
			playerRigidBody.linearVelocity = vector;
			for (int i = 0; i < velocityHistory.Length; i++)
			{
				velocityHistory[i] = vector;
			}
		}
	}

	public void FixedUpdate()
	{
		AntiTeleportTechnology();
		IsFrozen = GorillaTagger.Instance.offlineVRRig.IsFrozen || debugFreezeTag;
		bool isDefaultScale = IsDefaultScale;
		playerRigidBody.useGravity = false;
		if (gravityOverrides.Count > 0)
		{
			ApplyGravityOverrides();
		}
		else if (halloweenLevitationBonusStrength > 0f || halloweenLevitationStrength > 0f)
		{
			float num = Time.time - lastTouchedGroundTimestamp;
			if (num < halloweenLevitationTotalDuration)
			{
				playerRigidBody.AddForce(Vector3.up * (halloweenLevitationStrength * Mathf.InverseLerp(halloweenLevitationFullStrengthDuration, halloweenLevitationTotalDuration, num)), ForceMode.Acceleration);
			}
			float y = playerRigidBody.linearVelocity.y;
			if (y <= halloweenLevitateBonusFullAtYSpeed)
			{
				playerRigidBody.AddForce(Vector3.up * halloweenLevitationBonusStrength, ForceMode.Acceleration);
			}
			else if (y <= halloweenLevitateBonusOffAtYSpeed)
			{
				float num2 = Mathf.InverseLerp(halloweenLevitateBonusOffAtYSpeed, halloweenLevitateBonusFullAtYSpeed, playerRigidBody.linearVelocity.y);
				playerRigidBody.AddForce(Vector3.up * (halloweenLevitationBonusStrength * num2), ForceMode.Acceleration);
			}
		}
		if (enableHoverMode)
		{
			playerRigidBody.linearVelocity = HoverboardFixedUpdate(playerRigidBody.linearVelocity);
		}
		else
		{
			didHoverLastFrame = false;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		bodyInWater = false;
		Vector3 lhs = swimmingVelocity;
		swimmingVelocity = Vector3.MoveTowards(swimmingVelocity, Vector3.zero, swimmingParams.swimmingVelocityOutOfWaterDrainRate * fixedDeltaTime);
		leftHandNonDiveHapticsAmount = 0f;
		rightHandNonDiveHapticsAmount = 0f;
		if (bodyOverlappingWaterVolumes.Count > 0 || forcedUnderwater)
		{
			WaterVolume waterVolume = null;
			float num3 = float.MinValue;
			Vector3 vector = headCollider.transform.position + GTPlayerTransform.PhysicsDown * swimmingParams.floatingWaterLevelBelowHead * scale;
			activeWaterCurrents.Clear();
			for (int i = 0; i < bodyOverlappingWaterVolumes.Count; i++)
			{
				if (bodyOverlappingWaterVolumes[i].GetSurfaceQueryForPoint(vector, out var result))
				{
					float num4 = Vector3.Dot(result.surfacePoint - vector, result.surfaceNormal);
					if (num4 > num3)
					{
						num3 = num4;
						waterVolume = bodyOverlappingWaterVolumes[i];
						waterSurfaceForHead = result;
					}
					WaterCurrent current = bodyOverlappingWaterVolumes[i].Current;
					if (current != null && num4 > 0f && !activeWaterCurrents.Contains(current))
					{
						activeWaterCurrents.Add(current);
					}
				}
			}
			if (forcedUnderwater && waterVolume == null)
			{
				waterSurfaceForHead = new WaterVolume.SurfaceQuery
				{
					surfacePoint = headCollider.transform.position + GTPlayerTransform.PhysicsUp * 1000f,
					surfaceNormal = GTPlayerTransform.PhysicsUp,
					maxDepth = 2000f
				};
				num3 = 1000f;
			}
			if (waterVolume != null || forcedUnderwater)
			{
				Vector3 linearVelocity = playerRigidBody.linearVelocity;
				float magnitude = linearVelocity.magnitude;
				bool flag = headInWater;
				float num5 = Vector3.Dot(waterSurfaceForHead.surfacePoint - headCollider.transform.position, waterSurfaceForHead.surfaceNormal);
				float num6 = Vector3.Dot(headCollider.transform.position - (waterSurfaceForHead.surfacePoint - waterSurfaceForHead.surfaceNormal * waterSurfaceForHead.maxDepth), waterSurfaceForHead.surfaceNormal);
				headInWater = (forcedUnderwater || (num5 > 0f && num6 > 0f)) && waterVolume.LiquidType != LiquidType.SwimInAir;
				if (headInWater && !flag)
				{
					audioSetToUnderwater = true;
					audioManager.SetMixerSnapshot(audioManager.underwaterSnapshot);
				}
				else if (!headInWater && flag)
				{
					audioSetToUnderwater = false;
					audioManager.UnsetMixerSnapshot();
				}
				float num7 = Vector3.Dot(waterSurfaceForHead.surfacePoint - vector, waterSurfaceForHead.surfaceNormal);
				float num8 = Vector3.Dot(vector - (waterSurfaceForHead.surfacePoint - waterSurfaceForHead.surfaceNormal * waterSurfaceForHead.maxDepth), waterSurfaceForHead.surfaceNormal);
				bodyInWater = forcedUnderwater || (num7 > 0f && num8 > 0f);
				if (bodyInWater)
				{
					LiquidProperties liquidProperties = liquidPropertiesList[(int)((waterVolume != null) ? waterVolume.LiquidType : LiquidType.Water)];
					float num10;
					if (swimmingParams.extendBouyancyFromSpeed)
					{
						float time = Mathf.Clamp(Vector3.Dot(linearVelocity / scale, waterSurfaceForHead.surfaceNormal), swimmingParams.speedToBouyancyExtensionMinMax.x, swimmingParams.speedToBouyancyExtensionMinMax.y);
						float b = swimmingParams.speedToBouyancyExtension.Evaluate(time);
						buoyancyExtension = Mathf.Max(buoyancyExtension, b);
						float num9 = Mathf.InverseLerp(0f, swimmingParams.buoyancyFadeDist + buoyancyExtension, num3 / scale + buoyancyExtension);
						buoyancyExtension = Spring.DamperDecayExact(buoyancyExtension, swimmingParams.buoyancyExtensionDecayHalflife, fixedDeltaTime);
						num10 = num9;
					}
					else
					{
						num10 = Mathf.InverseLerp(0f, swimmingParams.buoyancyFadeDist, num3 / scale);
					}
					Vector3 force = -(GTPlayerTransform.PhysicsDown * Physics.gravity.magnitude * scale) * (liquidProperties.buoyancy * num10);
					if (IsFrozen && GorillaGameManager.instance is GorillaFreezeTagManager)
					{
						force *= frozenBodyBuoyancyFactor;
					}
					playerRigidBody.AddForce(force, ForceMode.Acceleration);
					Vector3 zero = Vector3.zero;
					Vector3 zero2 = Vector3.zero;
					for (int j = 0; j < activeWaterCurrents.Count; j++)
					{
						if (activeWaterCurrents[j].GetCurrentAtPoint(startingVelocity: linearVelocity + zero, worldPoint: bodyCollider.transform.position, dt: fixedDeltaTime, currentVelocity: out var vector2, velocityChange: out var velocityChange))
						{
							zero2 += vector2;
							zero += velocityChange;
						}
					}
					if (magnitude > Mathf.Epsilon)
					{
						float num11 = 0.01f;
						Vector3 vector3 = linearVelocity / magnitude;
						Vector3 right = leftHand.handFollower.right;
						Vector3 dir = -rightHand.handFollower.right;
						Vector3 forward = leftHand.handFollower.forward;
						Vector3 forward2 = rightHand.handFollower.forward;
						Vector3 vector4 = vector3;
						float num12 = 0f;
						float num13 = 0f;
						float num14 = 0f;
						if (swimmingParams.applyDiveSteering && !disableMovement && isDefaultScale)
						{
							float value = Vector3.Dot(linearVelocity - zero2, vector3);
							float time2 = Mathf.Clamp(value, swimmingParams.swimSpeedToRedirectAmountMinMax.x, swimmingParams.swimSpeedToRedirectAmountMinMax.y);
							float b2 = swimmingParams.swimSpeedToRedirectAmount.Evaluate(time2);
							time2 = Mathf.Clamp(value, swimmingParams.swimSpeedToMaxRedirectAngleMinMax.x, swimmingParams.swimSpeedToMaxRedirectAngleMinMax.y);
							float num15 = swimmingParams.swimSpeedToMaxRedirectAngle.Evaluate(time2);
							float value2 = Mathf.Acos(Vector3.Dot(vector3, forward)) / MathF.PI * -2f + 1f;
							float value3 = Mathf.Acos(Vector3.Dot(vector3, forward2)) / MathF.PI * -2f + 1f;
							float num16 = Mathf.Clamp(value2, swimmingParams.palmFacingToRedirectAmountMinMax.x, swimmingParams.palmFacingToRedirectAmountMinMax.y);
							float num17 = Mathf.Clamp(value3, swimmingParams.palmFacingToRedirectAmountMinMax.x, swimmingParams.palmFacingToRedirectAmountMinMax.y);
							float a = ((!float.IsNaN(num16)) ? swimmingParams.palmFacingToRedirectAmount.Evaluate(num16) : 0f);
							float a2 = ((!float.IsNaN(num17)) ? swimmingParams.palmFacingToRedirectAmount.Evaluate(num17) : 0f);
							Vector3 vector5 = Vector3.ProjectOnPlane(vector3, right);
							Vector3 vector6 = Vector3.ProjectOnPlane(vector3, right);
							float num18 = Mathf.Min(vector5.magnitude, 1f);
							float num19 = Mathf.Min(vector6.magnitude, 1f);
							float magnitude2 = leftHand.velocityTracker.GetAverageVelocity(worldSpace: false, swimmingParams.diveVelocityAveragingWindow).magnitude;
							float magnitude3 = rightHand.velocityTracker.GetAverageVelocity(worldSpace: false, swimmingParams.diveVelocityAveragingWindow).magnitude;
							float time3 = Mathf.Clamp(magnitude2, swimmingParams.handSpeedToRedirectAmountMinMax.x, swimmingParams.handSpeedToRedirectAmountMinMax.y);
							float time4 = Mathf.Clamp(magnitude3, swimmingParams.handSpeedToRedirectAmountMinMax.x, swimmingParams.handSpeedToRedirectAmountMinMax.y);
							float a3 = swimmingParams.handSpeedToRedirectAmount.Evaluate(time3);
							float a4 = swimmingParams.handSpeedToRedirectAmount.Evaluate(time4);
							float averageSpeedChangeMagnitudeInDirection = leftHand.velocityTracker.GetAverageSpeedChangeMagnitudeInDirection(right, worldSpace: false, swimmingParams.diveVelocityAveragingWindow);
							float averageSpeedChangeMagnitudeInDirection2 = rightHand.velocityTracker.GetAverageSpeedChangeMagnitudeInDirection(dir, worldSpace: false, swimmingParams.diveVelocityAveragingWindow);
							float time5 = Mathf.Clamp(averageSpeedChangeMagnitudeInDirection, swimmingParams.handAccelToRedirectAmountMinMax.x, swimmingParams.handAccelToRedirectAmountMinMax.y);
							float time6 = Mathf.Clamp(averageSpeedChangeMagnitudeInDirection2, swimmingParams.handAccelToRedirectAmountMinMax.x, swimmingParams.handAccelToRedirectAmountMinMax.y);
							float b3 = swimmingParams.handAccelToRedirectAmount.Evaluate(time5);
							float b4 = swimmingParams.handAccelToRedirectAmount.Evaluate(time6);
							num12 = Mathf.Min(a, Mathf.Min(a3, b3));
							float num20 = ((Vector3.Dot(vector3, forward) > 0f) ? (Mathf.Min(num12, b2) * num18) : 0f);
							num13 = Mathf.Min(a2, Mathf.Min(a4, b4));
							float num21 = ((Vector3.Dot(vector3, forward2) > 0f) ? (Mathf.Min(num13, b2) * num19) : 0f);
							if (swimmingParams.reduceDiveSteeringBelowVelocityPlane)
							{
								Vector3 rhs = ((!(Vector3.Dot(headCollider.transform.up, vector3) > 0.95f)) ? Vector3.Cross(Vector3.Cross(vector3, headCollider.transform.up), vector3).normalized : (-headCollider.transform.forward));
								Vector3 position = headCollider.transform.position;
								Vector3 lhs2 = position - leftHand.handFollower.position;
								Vector3 lhs3 = position - rightHand.handFollower.position;
								float reduceDiveSteeringBelowPlaneFadeStartDist = swimmingParams.reduceDiveSteeringBelowPlaneFadeStartDist;
								float reduceDiveSteeringBelowPlaneFadeEndDist = swimmingParams.reduceDiveSteeringBelowPlaneFadeEndDist;
								float f = Vector3.Dot(lhs2, GTPlayerTransform.PhysicsUp);
								float f2 = Vector3.Dot(lhs3, GTPlayerTransform.PhysicsUp);
								float f3 = Vector3.Dot(lhs2, rhs);
								float f4 = Vector3.Dot(lhs3, rhs);
								float num22 = 1f - Mathf.InverseLerp(reduceDiveSteeringBelowPlaneFadeStartDist, reduceDiveSteeringBelowPlaneFadeEndDist, Mathf.Min(Mathf.Abs(f), Mathf.Abs(f3)));
								float num23 = 1f - Mathf.InverseLerp(reduceDiveSteeringBelowPlaneFadeStartDist, reduceDiveSteeringBelowPlaneFadeEndDist, Mathf.Min(Mathf.Abs(f2), Mathf.Abs(f4)));
								num20 *= num22;
								num21 *= num23;
							}
							float num24 = num21 + num20;
							Vector3 zero3 = Vector3.zero;
							if (swimmingParams.applyDiveSteering && num24 > num11)
							{
								zero3 = ((num20 * vector5 + num21 * vector6) / num24).normalized;
								zero3 = Vector3.Lerp(vector3, zero3, num24);
								vector4 = Vector3.RotateTowards(vector3, zero3, MathF.PI / 180f * num15 * fixedDeltaTime, 0f);
							}
							else
							{
								vector4 = vector3;
							}
							num14 = Mathf.Clamp01((num12 + num13) * 0.5f);
						}
						float num25 = Mathf.Clamp(Vector3.Dot(lhs, vector3), 0f, magnitude);
						float num26 = magnitude - num25;
						if (swimmingParams.applyDiveSwimVelocityConversion && !disableMovement && num14 > num11 && num25 < swimmingParams.diveMaxSwimVelocityConversion)
						{
							float num27 = Mathf.Min(swimmingParams.diveSwimVelocityConversionRate * fixedDeltaTime, num26) * num14;
							num25 += num27;
							num26 -= num27;
						}
						float halflife = swimmingParams.swimUnderWaterDampingHalfLife * liquidProperties.dampingFactor;
						float halflife2 = swimmingParams.baseUnderWaterDampingHalfLife * liquidProperties.dampingFactor;
						float num28 = Spring.DamperDecayExact(num25 / scale, halflife, fixedDeltaTime) * scale;
						float num29 = Spring.DamperDecayExact(num26 / scale, halflife2, fixedDeltaTime) * scale;
						if (swimmingParams.applyDiveDampingMultiplier && !disableMovement)
						{
							float t = Mathf.Lerp(1f, swimmingParams.diveDampingMultiplier, num14);
							num28 = Mathf.Lerp(num25, num28, t);
							num29 = Mathf.Lerp(num26, num29, t);
							float time7 = Mathf.Clamp((1f - num12) * (num25 + num26), swimmingParams.nonDiveDampingHapticsAmountMinMax.x + num11, swimmingParams.nonDiveDampingHapticsAmountMinMax.y - num11);
							float time8 = Mathf.Clamp((1f - num13) * (num25 + num26), swimmingParams.nonDiveDampingHapticsAmountMinMax.x + num11, swimmingParams.nonDiveDampingHapticsAmountMinMax.y - num11);
							leftHandNonDiveHapticsAmount = swimmingParams.nonDiveDampingHapticsAmount.Evaluate(time7);
							rightHandNonDiveHapticsAmount = swimmingParams.nonDiveDampingHapticsAmount.Evaluate(time8);
						}
						swimmingVelocity = num28 * vector4 + zero * scale;
						playerRigidBody.linearVelocity = swimmingVelocity + num29 * vector4;
					}
				}
			}
		}
		else if (audioSetToUnderwater)
		{
			audioSetToUnderwater = false;
			audioManager.UnsetMixerSnapshot();
		}
		handleClimbing(Time.fixedDeltaTime);
		stuckHandsCheckFixedUpdate();
		FixedUpdate_HandHolds(Time.fixedDeltaTime);
	}

	public void SetHoverboardPosRot(Vector3 worldPos, Quaternion worldRot)
	{
		hoverboardPlayerLocalPos = headCollider.transform.InverseTransformPoint(worldPos);
		hoverboardPlayerLocalRot = headCollider.transform.InverseTransformRotation(worldRot);
	}

	private void HoverboardLateUpdate()
	{
		_ = headCollider.transform.eulerAngles;
		bool flag = false;
		for (int i = 0; i < hoverboardCasts.Length; i++)
		{
			HoverBoardCast hoverBoardCast = hoverboardCasts[i];
			hoverBoardCast.didHit = Physics.SphereCast(new Ray(hoverboardVisual.transform.TransformPoint(hoverBoardCast.localOrigin), hoverboardVisual.transform.rotation * hoverBoardCast.localDirection), hoverBoardCast.sphereRadius, out var hitInfo, hoverBoardCast.distance, locomotionEnabledLayers);
			if (hoverBoardCast.didHit)
			{
				if (hitInfo.collider.TryGetComponent<HoverboardCantHover>(out var _))
				{
					hoverBoardCast.didHit = false;
				}
				else
				{
					hoverBoardCast.pointHit = hitInfo.point;
					hoverBoardCast.normalHit = hitInfo.normal;
				}
			}
			hoverboardCasts[i] = hoverBoardCast;
			if (hoverBoardCast.didHit)
			{
				flag = true;
			}
		}
		hasHoverPoint = flag;
		bodyCollider.enabled = (bodyCollider.transform.position - hoverboardVisual.transform.TransformPoint(GTPlayerTransform.Up * hoverBodyCollisionRadiusUpOffset)).IsLongerThan(hoverBodyHasCollisionsOutsideRadius);
	}

	private Vector3 HoverboardFixedUpdate(Vector3 velocity)
	{
		hoverboardVisual.transform.position = headCollider.transform.TransformPoint(hoverboardPlayerLocalPos);
		hoverboardVisual.transform.rotation = headCollider.transform.TransformRotation(hoverboardPlayerLocalRot);
		if (didHoverLastFrame)
		{
			velocity += Vector3.up * hoverGeneralUpwardForce * Time.fixedDeltaTime;
		}
		Vector3 position = hoverboardVisual.transform.position;
		Vector3 vector = position + velocity * Time.fixedDeltaTime;
		Vector3 forward = hoverboardVisual.transform.forward;
		Vector3 vector2 = (hoverboardCasts[0].didHit ? hoverboardCasts[0].normalHit : Vector3.up);
		bool flag = false;
		for (int i = 0; i < hoverboardCasts.Length; i++)
		{
			HoverBoardCast hoverBoardCast = hoverboardCasts[i];
			if (!hoverBoardCast.didHit)
			{
				continue;
			}
			Vector3 vector3 = position + Vector3.Project(hoverBoardCast.pointHit - position, forward);
			Vector3 vector4 = vector + Vector3.Project(hoverBoardCast.pointHit - position, forward);
			bool num = hoverBoardCast.isSolid || Vector3.Dot(hoverBoardCast.normalHit, hoverBoardCast.pointHit - vector4) + hoverIdealHeight > 0f;
			float num2 = (hoverBoardCast.isSolid ? (Vector3.Dot(hoverBoardCast.normalHit, hoverBoardCast.pointHit - hoverboardVisual.transform.TransformPoint(hoverBoardCast.localOrigin + hoverBoardCast.localDirection * hoverBoardCast.distance)) + hoverBoardCast.sphereRadius) : (Vector3.Dot(hoverBoardCast.normalHit, hoverBoardCast.pointHit - vector3) + hoverIdealHeight));
			if (num)
			{
				flag = true;
				boostEnabledUntilTimestamp = Time.time + hoverboardBoostGracePeriod;
				if (Vector3.Dot(velocity, hoverBoardCast.normalHit) < 0f)
				{
					velocity = Vector3.ProjectOnPlane(velocity, hoverBoardCast.normalHit);
				}
				playerRigidBody.transform.position += hoverBoardCast.normalHit * num2;
				Vector3 vector5 = turnParent.transform.rotation * (hoverboardVisual.IsLeftHanded ? leftHand.velocityTracker : rightHand.velocityTracker).GetAverageVelocity();
				if (Vector3.Dot(vector5, hoverBoardCast.normalHit) < 0f)
				{
					velocity -= Vector3.Project(vector5, hoverBoardCast.normalHit) * hoverSlamJumpStrengthFactor * Time.fixedDeltaTime;
				}
				vector = position + velocity * Time.fixedDeltaTime;
			}
		}
		float time = Mathf.Abs(Mathf.DeltaAngle(0f, Mathf.Acos(Vector3.Dot(hoverboardVisual.transform.up, Vector3.ProjectOnPlane(vector2, forward).normalized)) * 57.29578f));
		float num3 = hoverCarveAngleResponsiveness.Evaluate(time);
		forward = (forward + Vector3.ProjectOnPlane(hoverboardVisual.transform.up, vector2) * hoverTiltAdjustsForwardFactor).normalized;
		if (!flag)
		{
			didHoverLastFrame = false;
			num3 = 0f;
		}
		Vector3 vector6 = velocity;
		if (enableHoverMode && hasHoverPoint)
		{
			Vector3 vector7 = Vector3.ProjectOnPlane(velocity, vector2);
			Vector3 vector8 = velocity - vector7;
			Vector3 vector9 = Vector3.Project(vector7, forward);
			float num4 = vector7.magnitude;
			if (num4 <= hoveringSlowSpeed)
			{
				num4 *= hoveringSlowStoppingFactor;
			}
			Vector3 vector10 = vector7 - vector9;
			float num5 = 0f;
			bool flag2 = false;
			if (num3 > 0f)
			{
				if (vector10.IsLongerThan(vector9))
				{
					num5 = Mathf.Min((vector10.magnitude - vector9.magnitude) * hoverCarveSidewaysSpeedLossFactor * num3, num4);
					if (num5 > 0f && num4 > hoverMinGrindSpeed)
					{
						flag2 = true;
						hoverboardVisual.PlayGrindHaptic();
					}
					num4 -= num5;
				}
				vector10 *= 1f - num3 * sidewaysDrag;
				if (!leftHand.isColliding && !rightHand.isColliding)
				{
					velocity = (vector9 + vector10).normalized * num4 + vector8;
				}
			}
			else
			{
				velocity = vector7.normalized * num4 + vector8;
			}
			float magnitude = (velocity - vector6).magnitude;
			hoverboardAudio.UpdateAudioLoop(velocity.magnitude, bodyVelocityTracker.GetAverageVelocity(worldSpace: true).magnitude, magnitude, flag2 ? num5 : 0f);
			if (magnitude > 0f && !flag2)
			{
				hoverboardVisual.PlayCarveHaptic(magnitude);
			}
		}
		else
		{
			hoverboardAudio.UpdateAudioLoop(0f, bodyVelocityTracker.GetAverageVelocity(worldSpace: true).magnitude, 0f, 0f);
		}
		return velocity;
	}

	public void GrabPersonalHoverboard(bool isLeftHand, Vector3 pos, Quaternion rot, Color col)
	{
		if (hoverboardVisual.IsHeld)
		{
			hoverboardVisual.DropFreeBoard();
		}
		hoverboardVisual.SetIsHeld(isLeftHand, pos, rot, col);
		hoverboardVisual.ProxyGrabHandle(isLeftHand);
		FreeHoverboardManager.instance.PreserveMaxHoverboardsConstraint(NetworkSystem.Instance.LocalPlayer.ActorNumber);
	}

	public void SetHoverAllowed(bool allowed, bool force = false)
	{
		if (allowed)
		{
			hoverAllowedCount++;
			isHoverAllowed = true;
			return;
		}
		hoverAllowedCount = ((!force && hoverAllowedCount != 0) ? (hoverAllowedCount - 1) : 0);
		if (hoverAllowedCount == 0 && isHoverAllowed)
		{
			isHoverAllowed = false;
			if (enableHoverMode)
			{
				SetHoverActive(enable: false);
				VRRig.LocalRig.hoverboardVisual.SetNotHeld();
			}
		}
	}

	public void SetHoverActive(bool enable)
	{
		if (enable && !isHoverAllowed)
		{
			return;
		}
		enableHoverMode = enable;
		if (!enable)
		{
			bodyCollider.enabled = true;
			hasHoverPoint = false;
			didHoverLastFrame = false;
			for (int i = 0; i < hoverboardCasts.Length; i++)
			{
				hoverboardCasts[i].didHit = false;
			}
			hoverboardAudio.Stop();
		}
	}

	private void BodyCollider()
	{
		if (MaxSphereSizeForNoOverlap(bodyInitialRadius * scale, PositionWithOffset(headCollider.transform, bodyOffset), ignoreOneWay: false, out bodyMaxRadius))
		{
			if (scale > 0f)
			{
				bodyCollider.radius = bodyMaxRadius / scale;
			}
			if (Physics.SphereCast(PositionWithOffset(headCollider.transform, bodyOffset), bodyMaxRadius, GTPlayerTransform.Down, out bodyHitInfo, bodyInitialHeight * scale - bodyMaxRadius, locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
			{
				bodyCollider.height = (bodyHitInfo.distance + bodyMaxRadius) / scale;
			}
			else
			{
				bodyHitInfo = emptyHit;
				bodyCollider.height = bodyInitialHeight;
			}
			if (!bodyCollider.gameObject.activeSelf)
			{
				bodyCollider.gameObject.SetActive(value: true);
			}
		}
		else
		{
			bodyCollider.gameObject.SetActive(value: false);
		}
		bodyCollider.height = Mathf.Lerp(bodyCollider.height, bodyInitialHeight, bodyLerp);
		bodyCollider.radius = Mathf.Lerp(bodyCollider.radius, bodyInitialRadius, bodyLerp);
		bodyOffsetVector = GTPlayerTransform.Down * bodyCollider.height / 2f;
		bodyCollider.transform.position = PositionWithOffset(headCollider.transform, bodyOffset) + bodyOffsetVector * scale;
		bodyCollider.transform.rotation = Quaternion.FromToRotation(headCollider.transform.up, GTPlayerTransform.Up) * headCollider.transform.rotation;
	}

	private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
	{
		return transformToModify.position + transformToModify.rotation * offsetVector * scale;
	}

	public void ScaleAwayFromPoint(float oldScale, float newScale, Vector3 scaleCenter)
	{
		if (oldScale < newScale)
		{
			lastHeadPosition = ScalePointAwayFromCenter(lastHeadPosition, headCollider.radius, oldScale, newScale, scaleCenter);
			leftHand.lastPosition = ScalePointAwayFromCenter(leftHand.lastPosition, minimumRaycastDistance, oldScale, newScale, scaleCenter);
			rightHand.lastPosition = ScalePointAwayFromCenter(rightHand.lastPosition, minimumRaycastDistance, oldScale, newScale, scaleCenter);
		}
	}

	private static Vector3 ScalePointAwayFromCenter(Vector3 point, float baseRadius, float oldScale, float newScale, Vector3 scaleCenter)
	{
		float magnitude = (point - scaleCenter).magnitude;
		float num = magnitude + Mathf.Epsilon + baseRadius * (newScale - oldScale);
		return scaleCenter + (point - scaleCenter) * num / magnitude;
	}

	private void OnBeforeRenderInit()
	{
		if (Application.isPlaying && !hasCorrectedForTracking && mainCamera != null && mainCamera.transform.localPosition != Vector3.zero)
		{
			ForceRigidBodySync();
			base.transform.position -= mainCamera.transform.localPosition;
			hasCorrectedForTracking = true;
		}
		Application.onBeforeRender -= OnBeforeRenderInit;
	}

	private void LateUpdate()
	{
		Vector3 valueOrDefault = antiDriftLastPosition.GetValueOrDefault();
		if (!antiDriftLastPosition.HasValue)
		{
			valueOrDefault = base.transform.position;
			antiDriftLastPosition = valueOrDefault;
		}
		if ((double)(antiDriftLastPosition.Value - base.transform.position).sqrMagnitude < 1E-08)
		{
			base.transform.position = antiDriftLastPosition.Value;
		}
		else
		{
			antiDriftLastPosition = base.transform.position;
		}
		if (!hasCorrectedForTracking && mainCamera.transform.localPosition != Vector3.zero)
		{
			base.transform.position -= mainCamera.transform.localPosition;
			hasCorrectedForTracking = true;
			Application.onBeforeRender -= OnBeforeRenderInit;
		}
		if (playerRigidBody.isKinematic)
		{
			return;
		}
		float time = Time.time;
		Vector3 position = headCollider.transform.position;
		turnParent.transform.localScale = VRRig.LocalRig.transform.localScale;
		playerRigidBody.MovePosition(playerRigidBody.position + position - headCollider.transform.position);
		if (Mathf.Abs(lastScale - scale) > 0.001f)
		{
			if ((object)mainCamera == null)
			{
				mainCamera = Camera.main;
			}
			mainCamera.nearClipPlane = ((scale > 0.5f) ? 0.01f : 0.002f);
		}
		lastScale = scale;
		debugLastRightHandPosition = rightHand.lastPosition;
		debugPlatformDeltaPosition = MovingSurfaceMovement();
		if (debugMovement)
		{
			tempRealTime = Time.time;
			calcDeltaTime = Time.deltaTime;
			lastRealTime = tempRealTime;
		}
		else
		{
			tempRealTime = Time.realtimeSinceStartup;
			calcDeltaTime = tempRealTime - lastRealTime;
			lastRealTime = tempRealTime;
			if (calcDeltaTime > 0.1f)
			{
				calcDeltaTime = 0.05f;
			}
		}
		if (lastFrameHasValidTouchPos && lastPlatformTouched != null && ComputeWorldHitPoint(lastHitInfoHand, lastFrameTouchPosLocal, out var worldHitPoint))
		{
			refMovement = worldHitPoint - lastFrameTouchPosWorld;
		}
		else
		{
			refMovement = Vector3.zero;
		}
		Vector3 vector = Vector3.zero;
		Quaternion quaternion = Quaternion.identity;
		Vector3 pivot = headCollider.transform.position;
		if (lastMovingSurfaceContact != MovingSurfaceContactPoint.NONE && ComputeWorldHitPoint(lastMovingSurfaceHit, lastMovingSurfaceTouchLocal, out var worldHitPoint2))
		{
			if (wasMovingSurfaceMonkeBlock && (lastMonkeBlock == null || lastMonkeBlock.state != BuilderPiece.State.AttachedAndPlaced))
			{
				movingSurfaceOffset = Vector3.zero;
			}
			else
			{
				movingSurfaceOffset = worldHitPoint2 - lastMovingSurfaceTouchWorld;
				vector = movingSurfaceOffset / calcDeltaTime;
				quaternion = lastMovingSurfaceHit.collider.transform.rotation * Quaternion.Inverse(lastMovingSurfaceRot);
				pivot = worldHitPoint2;
			}
		}
		else
		{
			movingSurfaceOffset = Vector3.zero;
		}
		float num = 40f * scale;
		if (vector.sqrMagnitude >= num * num)
		{
			movingSurfaceOffset = Vector3.zero;
			vector = Vector3.zero;
			quaternion = Quaternion.identity;
		}
		if (!didAJump && (leftHand.wasColliding || rightHand.wasColliding))
		{
			base.transform.position = base.transform.position + 4.9f * GTPlayerTransform.PhysicsDown * calcDeltaTime * calcDeltaTime * scale;
			if (Vector3.Dot(averagedVelocity, slideAverageNormal) <= 0f && Vector3.Dot(GTPlayerTransform.PhysicsUp, slideAverageNormal) > 0f)
			{
				base.transform.position = base.transform.position - Vector3.Project(Mathf.Min(stickDepth * scale, Vector3.Project(averagedVelocity, slideAverageNormal).magnitude * calcDeltaTime) * slideAverageNormal, GTPlayerTransform.PhysicsDown);
			}
		}
		if (!didAJump && anyHandWasSliding)
		{
			base.transform.position = base.transform.position + slideVelocity * calcDeltaTime;
			slideVelocity += 9.8f * GTPlayerTransform.PhysicsDown * calcDeltaTime * scale;
		}
		float paddleBoostFactor = ((Time.time > boostEnabledUntilTimestamp) ? 0f : (Time.deltaTime * Mathf.Clamp(playerRigidBody.linearVelocity.magnitude * hoverboardPaddleBoostMultiplier, 0f, hoverboardPaddleBoostMax)));
		int divisor = 0;
		Vector3 totalMove = Vector3.zero;
		anyHandIsColliding = false;
		anyHandIsSliding = false;
		anyHandIsSticking = false;
		leftHand.FirstIteration(ref totalMove, ref divisor, paddleBoostFactor);
		rightHand.FirstIteration(ref totalMove, ref divisor, paddleBoostFactor);
		for (int i = 0; i < 12; i++)
		{
			if (stiltStates[i].isActive)
			{
				stiltStates[i].FirstIteration(ref totalMove, ref divisor, 0f);
			}
		}
		if (divisor != 0)
		{
			totalMove /= (float)divisor;
		}
		if (lastMovingSurfaceContact == MovingSurfaceContactPoint.RIGHT || lastMovingSurfaceContact == MovingSurfaceContactPoint.LEFT)
		{
			totalMove += movingSurfaceOffset;
		}
		else if (lastMovingSurfaceContact == MovingSurfaceContactPoint.BODY)
		{
			Vector3 vector2 = lastHeadPosition + movingSurfaceOffset - headCollider.transform.position;
			totalMove += vector2;
		}
		if (!MaxSphereSizeForNoOverlap(headCollider.radius * 0.9f * scale, lastHeadPosition, ignoreOneWay: true, out maxSphereSize1) && !CrazyCheck2(headCollider.radius * 0.9f * 0.75f * scale, lastHeadPosition))
		{
			lastHeadPosition = lastOpenHeadPosition;
		}
		if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius * 0.9f * scale, headCollider.transform.position + totalMove - lastHeadPosition, Vector3.zero, out var endPosition, singleHand: false, out var _, out junkHit, fullSlide: true))
		{
			totalMove = endPosition - headCollider.transform.position;
		}
		if (!MaxSphereSizeForNoOverlap(headCollider.radius * 0.9f * scale, lastHeadPosition + totalMove, ignoreOneWay: true, out maxSphereSize1) || !CrazyCheck2(headCollider.radius * 0.9f * 0.75f * scale, lastHeadPosition + totalMove))
		{
			lastHeadPosition = lastOpenHeadPosition;
			totalMove = lastHeadPosition - headCollider.transform.position;
		}
		else if (headCollider.radius * 0.9f * 0.825f * scale < maxSphereSize1)
		{
			lastOpenHeadPosition = headCollider.transform.position + totalMove;
		}
		if (totalMove != Vector3.zero)
		{
			base.transform.position += totalMove;
		}
		if (lastMovingSurfaceContact != MovingSurfaceContactPoint.NONE && quaternion != Quaternion.identity && !isClimbing && !rightHand.isHolding && !leftHand.isHolding)
		{
			RotateWithSurface(quaternion, pivot);
		}
		lastHeadPosition = headCollider.transform.position;
		areBothTouching = (!leftHand.isColliding && !leftHand.wasColliding) || (!rightHand.isColliding && !rightHand.wasColliding);
		TakeMyHand_ProcessMovement();
		HandleTentacleMovement();
		anyHandIsColliding = false;
		anyHandIsSliding = false;
		anyHandIsSticking = false;
		leftHand.FinalizeHandPosition();
		rightHand.FinalizeHandPosition();
		for (int j = 0; j < 12; j++)
		{
			if (stiltStates[j].isActive)
			{
				stiltStates[j].FinalizeHandPosition();
				HandState handState = stiltStates[j];
				GorillaTagger.Instance.SetExtraHandPosition((StiltID)j, handState.finalPositionThisFrame, handState.canTag, handState.canStun);
			}
		}
		Vector3 vector3 = lastPosition;
		MovingSurfaceContactPoint movingSurfaceContactPoint = MovingSurfaceContactPoint.NONE;
		int movingSurfaceId = -1;
		int movingSurfaceId2 = -1;
		bool sideTouch = false;
		bool isMonkeBlock = false;
		bool isMonkeBlock2 = false;
		bool flag = rightHand.isColliding && IsTouchingMovingSurface(rightHand.GetLastPosition(), rightHand.lastHitInfo, out movingSurfaceId, out sideTouch, out isMonkeBlock);
		if (flag && !sideTouch)
		{
			movingSurfaceContactPoint = MovingSurfaceContactPoint.RIGHT;
			lastMovingSurfaceHit = rightHand.lastHitInfo;
		}
		else
		{
			bool sideTouch2 = false;
			BuilderPiece builderPiece = (flag ? lastMonkeBlock : null);
			if (leftHand.isColliding && IsTouchingMovingSurface(leftHand.GetLastPosition(), leftHand.lastHitInfo, out movingSurfaceId2, out sideTouch2, out isMonkeBlock2))
			{
				if (sideTouch2 && isMonkeBlock == isMonkeBlock2)
				{
					if (sideTouch && movingSurfaceId2.Equals(movingSurfaceId) && (double)Vector3.Dot(leftHand.lastHitInfo.point - leftHand.GetLastPosition(), rightHand.lastHitInfo.point - rightHand.GetLastPosition()) < 0.3)
					{
						movingSurfaceContactPoint = MovingSurfaceContactPoint.RIGHT;
						lastMovingSurfaceHit = rightHand.lastHitInfo;
						lastMonkeBlock = builderPiece;
					}
				}
				else
				{
					movingSurfaceContactPoint = MovingSurfaceContactPoint.LEFT;
					lastMovingSurfaceHit = leftHand.lastHitInfo;
				}
			}
		}
		StoreVelocities();
		if (InWater)
		{
			PlayerGameEvents.PlayerSwam((lastPosition - vector3).magnitude, currentVelocity.magnitude);
		}
		else
		{
			PlayerGameEvents.PlayerMoved((lastPosition - vector3).magnitude, currentVelocity.magnitude);
		}
		didAJump = false;
		bool flag2 = exitMovingSurface;
		exitMovingSurface = false;
		if (leftHand.IsSlipOverriddenToMax() && rightHand.IsSlipOverriddenToMax())
		{
			didAJump = true;
			exitMovingSurface = true;
		}
		else if (anyHandIsSliding)
		{
			slideAverageNormal = Vector3.zero;
			int num2 = 0;
			averageSlipPercentage = 0f;
			bool flag3 = false;
			if (leftHand.isSliding)
			{
				slideAverageNormal += leftHand.slideNormal.normalized;
				averageSlipPercentage += leftHand.slipPercentage;
				num2++;
			}
			if (rightHand.isSliding)
			{
				flag3 = true;
				slideAverageNormal += rightHand.slideNormal.normalized;
				averageSlipPercentage += rightHand.slipPercentage;
				num2++;
			}
			for (int k = 0; k < stiltStates.Length; k++)
			{
				if (stiltStates[k].isActive && stiltStates[k].isSliding)
				{
					if (!stiltStates[k].isLeftHand)
					{
						flag3 = true;
					}
					slideAverageNormal += stiltStates[k].slideNormal.normalized;
					averageSlipPercentage += stiltStates[k].slipPercentage;
					num2++;
				}
			}
			slideAverageNormal = slideAverageNormal.normalized;
			averageSlipPercentage /= num2;
			if (num2 == 1)
			{
				surfaceDirection = (flag3 ? Vector3.ProjectOnPlane(rightHand.handFollower.forward, rightHand.slideNormal) : Vector3.ProjectOnPlane(leftHand.handFollower.forward, leftHand.slideNormal));
				if (Vector3.Dot(slideVelocity, surfaceDirection) > 0f)
				{
					slideVelocity = Vector3.Project(slideVelocity, Vector3.Slerp(slideVelocity, surfaceDirection.normalized * slideVelocity.magnitude, slideControl));
				}
				else
				{
					slideVelocity = Vector3.Project(slideVelocity, Vector3.Slerp(slideVelocity, -surfaceDirection.normalized * slideVelocity.magnitude, slideControl));
				}
			}
			if (!anyHandWasSliding)
			{
				slideVelocity = ((Vector3.Dot(playerRigidBody.linearVelocity, slideAverageNormal) <= 0f) ? Vector3.ProjectOnPlane(playerRigidBody.linearVelocity, slideAverageNormal) : playerRigidBody.linearVelocity);
			}
			else
			{
				slideVelocity = ((Vector3.Dot(slideVelocity, slideAverageNormal) <= 0f) ? Vector3.ProjectOnPlane(slideVelocity, slideAverageNormal) : slideVelocity);
			}
			slideVelocity = slideVelocity.normalized * Mathf.Min(slideVelocity.magnitude, Mathf.Max(0.5f, averagedVelocity.magnitude * 2f));
			playerRigidBody.linearVelocity = Vector3.zero;
		}
		else if (anyHandIsColliding)
		{
			if (!turnedThisFrame)
			{
				playerRigidBody.linearVelocity = Vector3.zero;
			}
			else
			{
				playerRigidBody.linearVelocity = playerRigidBody.linearVelocity.normalized * Mathf.Min(2f, playerRigidBody.linearVelocity.magnitude);
			}
		}
		else if (anyHandWasSliding)
		{
			playerRigidBody.linearVelocity = ((Vector3.Dot(slideVelocity, slideAverageNormal) <= 0f) ? Vector3.ProjectOnPlane(slideVelocity, slideAverageNormal) : slideVelocity);
		}
		if (anyHandIsColliding && !disableMovement && !turnedThisFrame && !didAJump)
		{
			if (anyHandIsSliding)
			{
				if (Vector3.Project(averagedVelocity, slideAverageNormal).magnitude > slideVelocityLimit * scale && Vector3.Dot(averagedVelocity, slideAverageNormal) > 0f && Vector3.Project(averagedVelocity, slideAverageNormal).magnitude > Vector3.Project(slideVelocity, slideAverageNormal).magnitude)
				{
					leftHand.isSliding = false;
					rightHand.isSliding = false;
					for (int l = 0; l < stiltStates.Length; l++)
					{
						stiltStates[l].isSliding = false;
					}
					anyHandIsSliding = false;
					didAJump = true;
					float num3 = ApplyNativeScaleAdjustment(Mathf.Min(maxJumpSpeed * ExtraVelMaxMultiplier(), jumpMultiplier * ExtraVelMultiplier() * Vector3.Project(averagedVelocity, slideAverageNormal).magnitude));
					playerRigidBody.linearVelocity = num3 * siJumpMultiplier * slideAverageNormal.normalized + Vector3.ProjectOnPlane(slideVelocity, slideAverageNormal);
					if (num3 > slideVelocityLimit * scale * exitMovingSurfaceThreshold)
					{
						exitMovingSurface = true;
					}
				}
			}
			else if (averagedVelocity.magnitude > velocityLimit * scale)
			{
				float num4 = ((InWater && CurrentWaterVolume != null) ? liquidPropertiesList[(int)CurrentWaterVolume.LiquidType].surfaceJumpFactor : 1f);
				float num5 = ApplyNativeScaleAdjustment(enableHoverMode ? Mathf.Min(hoverMaxPaddleSpeed, averagedVelocity.magnitude) : Mathf.Min(maxJumpSpeed * ExtraVelMaxMultiplier(), jumpMultiplier * ExtraVelMultiplier() * num4 * averagedVelocity.magnitude));
				Vector3 vector4 = num5 * siJumpMultiplier * averagedVelocity.normalized;
				didAJump = true;
				playerRigidBody.linearVelocity = vector4;
				if (InWater)
				{
					swimmingVelocity += vector4 * swimmingParams.underwaterJumpsAsSwimVelocityFactor;
				}
				if (num5 > velocityLimit * scale * exitMovingSurfaceThreshold)
				{
					exitMovingSurface = true;
				}
			}
		}
		stuckHandsCheckLateUpdate(ref leftHand.finalPositionThisFrame, ref rightHand.finalPositionThisFrame);
		if (lastPlatformTouched != null && currentPlatform == null)
		{
			if (!playerRigidBody.isKinematic)
			{
				playerRigidBody.linearVelocity += refMovement / calcDeltaTime;
			}
			refMovement = Vector3.zero;
		}
		if (lastMovingSurfaceContact == MovingSurfaceContactPoint.NONE)
		{
			if (!playerRigidBody.isKinematic)
			{
				playerRigidBody.linearVelocity += lastMovingSurfaceVelocity;
			}
			lastMovingSurfaceVelocity = Vector3.zero;
		}
		if (enableHoverMode)
		{
			HoverboardLateUpdate();
		}
		else
		{
			hasHoverPoint = false;
		}
		Vector3 zero = Vector3.zero;
		float a = 0f;
		float a2 = 0f;
		if (bodyInWater)
		{
			if (GetSwimmingVelocityForHand(leftHand.lastPosition, leftHand.finalPositionThisFrame, leftHand.controllerTransform.right, calcDeltaTime, ref leftHandWaterVolume, ref leftHandWaterSurface, out var swimmingVelocityChange) && !turnedThisFrame)
			{
				a = Mathf.InverseLerp(0f, 0.2f, swimmingVelocityChange.magnitude) * swimmingParams.swimmingHapticsStrength;
				zero += swimmingVelocityChange;
			}
			if (GetSwimmingVelocityForHand(rightHand.lastPosition, rightHand.finalPositionThisFrame, -rightHand.controllerTransform.right, calcDeltaTime, ref rightHandWaterVolume, ref rightHandWaterSurface, out var swimmingVelocityChange2) && !turnedThisFrame)
			{
				a2 = Mathf.InverseLerp(0f, 0.15f, swimmingVelocityChange2.magnitude) * swimmingParams.swimmingHapticsStrength;
				zero += swimmingVelocityChange2;
			}
		}
		Vector3 zero2 = Vector3.zero;
		if (swimmingParams.allowWaterSurfaceJumps && time - lastWaterSurfaceJumpTimeLeft > waterSurfaceJumpCooldown && CheckWaterSurfaceJump(leftHand.lastPosition, leftHand.finalPositionThisFrame, leftHand.controllerTransform.right, leftHand.velocityTracker.GetAverageVelocity(worldSpace: false, 0.1f) * scale, swimmingParams, leftHandWaterVolume, leftHandWaterSurface, out var jumpVelocity))
		{
			if (time - lastWaterSurfaceJumpTimeRight > waterSurfaceJumpCooldown)
			{
				zero2 += jumpVelocity;
			}
			lastWaterSurfaceJumpTimeLeft = Time.time;
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength, GorillaTagger.Instance.tapHapticDuration);
		}
		if (swimmingParams.allowWaterSurfaceJumps && time - lastWaterSurfaceJumpTimeRight > waterSurfaceJumpCooldown && CheckWaterSurfaceJump(rightHand.lastPosition, rightHand.finalPositionThisFrame, -rightHand.controllerTransform.right, rightHand.velocityTracker.GetAverageVelocity(worldSpace: false, 0.1f) * scale, swimmingParams, rightHandWaterVolume, rightHandWaterSurface, out var jumpVelocity2))
		{
			if (time - lastWaterSurfaceJumpTimeLeft > waterSurfaceJumpCooldown)
			{
				zero2 += jumpVelocity2;
			}
			lastWaterSurfaceJumpTimeRight = Time.time;
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength, GorillaTagger.Instance.tapHapticDuration);
		}
		zero2 = Vector3.ClampMagnitude(zero2, swimmingParams.waterSurfaceJumpMaxSpeed * scale);
		float num6 = Mathf.Max(a, leftHandNonDiveHapticsAmount);
		if (num6 > 0.001f && time - lastWaterSurfaceJumpTimeLeft > GorillaTagger.Instance.tapHapticDuration)
		{
			GorillaTagger.Instance.DoVibration(XRNode.LeftHand, num6, calcDeltaTime);
		}
		float num7 = Mathf.Max(a2, rightHandNonDiveHapticsAmount);
		if (num7 > 0.001f && time - lastWaterSurfaceJumpTimeRight > GorillaTagger.Instance.tapHapticDuration)
		{
			GorillaTagger.Instance.DoVibration(XRNode.RightHand, num7, calcDeltaTime);
		}
		if (!disableMovement)
		{
			swimmingVelocity += zero;
			if (!playerRigidBody.isKinematic)
			{
				playerRigidBody.linearVelocity += zero + zero2;
			}
		}
		else
		{
			swimmingVelocity = Vector3.zero;
		}
		if (GorillaGameManager.instance is GorillaFreezeTagManager)
		{
			if (!IsFrozen || !primaryButtonPressed)
			{
				IsBodySliding = false;
				lastSlopeDirection = Vector3.zero;
				if (bodyTouchedSurfaces.Count > 0)
				{
					foreach (KeyValuePair<GameObject, PhysicsMaterial> bodyTouchedSurface in bodyTouchedSurfaces)
					{
						if (bodyTouchedSurface.Key.TryGetComponent<MeshCollider>(out var component))
						{
							component.material = bodyTouchedSurface.Value;
						}
					}
					bodyTouchedSurfaces.Clear();
				}
			}
			else if (BodyOnGround && primaryButtonPressed)
			{
				float y = bodyInitialHeight / 2f - bodyInitialRadius;
				if (Physics.SphereCast(bodyCollider.transform.position - new Vector3(0f, y, 0f), bodyInitialRadius - 0.01f, Vector3.down, out var hitInfo, 1f, ~LayerMask.GetMask("Gorilla Body Collider", "GorillaInteractable"), QueryTriggerInteraction.Ignore))
				{
					IsBodySliding = true;
					if (!bodyTouchedSurfaces.ContainsKey(hitInfo.transform.gameObject) && hitInfo.transform.gameObject.TryGetComponent<MeshCollider>(out var component2))
					{
						bodyTouchedSurfaces.Add(hitInfo.transform.gameObject, component2.material);
						hitInfo.transform.gameObject.GetComponent<MeshCollider>().material = slipperyMaterial;
					}
				}
			}
			else
			{
				IsBodySliding = false;
				lastSlopeDirection = Vector3.zero;
			}
		}
		else
		{
			IsBodySliding = false;
			if (bodyTouchedSurfaces.Count > 0)
			{
				foreach (KeyValuePair<GameObject, PhysicsMaterial> bodyTouchedSurface2 in bodyTouchedSurfaces)
				{
					if (bodyTouchedSurface2.Key.TryGetComponent<MeshCollider>(out var component3))
					{
						component3.material = bodyTouchedSurface2.Value;
					}
				}
				bodyTouchedSurfaces.Clear();
			}
		}
		leftHand.OnEndOfFrame();
		rightHand.OnEndOfFrame();
		for (int m = 0; m < 12; m++)
		{
			if (stiltStates[m].isActive)
			{
				stiltStates[m].OnEndOfFrame();
			}
		}
		leftHand.PositionHandFollower();
		rightHand.PositionHandFollower();
		anyHandWasSliding = anyHandIsSliding;
		anyHandWasColliding = anyHandIsColliding;
		anyHandWasSticking = anyHandIsSticking;
		if (anyHandIsSticking)
		{
			lastTouchedGroundTimestamp = Time.time;
		}
		if (PhotonNetwork.InRoom)
		{
			if (IsGroundedHand || IsTentacleActive || IsThrusterActive)
			{
				LastHandTouchedGroundAtNetworkTime = (float)PhotonNetwork.Time;
				LastTouchedGroundAtNetworkTime = (float)PhotonNetwork.Time;
			}
			else if (IsGroundedButt || IsLaserZiplineActive)
			{
				LastTouchedGroundAtNetworkTime = (float)PhotonNetwork.Time;
			}
		}
		else
		{
			LastHandTouchedGroundAtNetworkTime = 0f;
			LastTouchedGroundAtNetworkTime = 0f;
		}
		degreesTurnedThisFrame = 0f;
		lastPlatformTouched = currentPlatform;
		currentPlatform = null;
		lastMovingSurfaceVelocity = vector;
		if (ComputeLocalHitPoint(lastHitInfoHand, out var localHitPoint))
		{
			lastFrameHasValidTouchPos = true;
			lastFrameTouchPosLocal = localHitPoint;
			lastFrameTouchPosWorld = lastHitInfoHand.point;
		}
		else
		{
			lastFrameHasValidTouchPos = false;
			lastFrameTouchPosLocal = Vector3.zero;
			lastFrameTouchPosWorld = Vector3.zero;
		}
		lastRigidbodyPosition = playerRigidBody.transform.position;
		RaycastHit raycastHit = emptyHit;
		BodyCollider();
		if (bodyHitInfo.collider != null)
		{
			wasBodyOnGround = true;
			raycastHit = bodyHitInfo;
		}
		else if (movingSurfaceContactPoint == MovingSurfaceContactPoint.NONE && bodyCollider.gameObject.activeSelf)
		{
			bool flag4 = false;
			ClearRaycasthitBuffer(ref rayCastNonAllocColliders);
			Vector3 origin = PositionWithOffset(headCollider.transform, bodyOffset) + (bodyInitialHeight * scale - bodyMaxRadius) * GTPlayerTransform.Down;
			bufferCount = Physics.SphereCastNonAlloc(origin, bodyMaxRadius, GTPlayerTransform.Down, rayCastNonAllocColliders, minimumRaycastDistance * scale, locomotionEnabledLayers.value);
			if (bufferCount > 0)
			{
				tempHitInfo = rayCastNonAllocColliders[0];
				for (int n = 0; n < bufferCount; n++)
				{
					if (!(tempHitInfo.distance <= 0f) && (!flag4 || rayCastNonAllocColliders[n].distance < tempHitInfo.distance))
					{
						flag4 = true;
						raycastHit = rayCastNonAllocColliders[n];
					}
				}
			}
			wasBodyOnGround = flag4;
		}
		int movingSurfaceId3 = -1;
		bool isMonkeBlock3 = false;
		if (wasBodyOnGround && movingSurfaceContactPoint == MovingSurfaceContactPoint.NONE && IsTouchingMovingSurface(PositionWithOffset(headCollider.transform, bodyOffset), raycastHit, out movingSurfaceId3, out var sideTouch3, out isMonkeBlock3) && !sideTouch3)
		{
			movingSurfaceContactPoint = MovingSurfaceContactPoint.BODY;
			lastMovingSurfaceHit = raycastHit;
		}
		if (movingSurfaceContactPoint != MovingSurfaceContactPoint.NONE && ComputeLocalHitPoint(lastMovingSurfaceHit, out var localHitPoint2))
		{
			lastMovingSurfaceTouchLocal = localHitPoint2;
			lastMovingSurfaceTouchWorld = lastMovingSurfaceHit.point;
			lastMovingSurfaceRot = lastMovingSurfaceHit.collider.transform.rotation;
			lastAttachedToMovingSurfaceFrame = Time.frameCount;
		}
		else
		{
			movingSurfaceContactPoint = MovingSurfaceContactPoint.NONE;
			lastMovingSurfaceTouchLocal = Vector3.zero;
			lastMovingSurfaceTouchWorld = Vector3.zero;
			lastMovingSurfaceRot = Quaternion.identity;
		}
		Vector3 position2 = lastMovingSurfaceTouchWorld;
		int num8 = -1;
		bool flag5 = false;
		switch (movingSurfaceContactPoint)
		{
		case MovingSurfaceContactPoint.RIGHT:
			num8 = movingSurfaceId;
			flag5 = isMonkeBlock;
			position2 = GorillaTagger.Instance.offlineVRRig.rightHandTransform.position;
			break;
		case MovingSurfaceContactPoint.LEFT:
			num8 = movingSurfaceId2;
			flag5 = isMonkeBlock2;
			position2 = GorillaTagger.Instance.offlineVRRig.leftHandTransform.position;
			break;
		case MovingSurfaceContactPoint.BODY:
			num8 = movingSurfaceId3;
			flag5 = isMonkeBlock3;
			position2 = GorillaTagger.Instance.offlineVRRig.bodyTransform.position;
			break;
		case MovingSurfaceContactPoint.NONE:
			if (flag2)
			{
				exitMovingSurface = true;
			}
			num8 = -1;
			break;
		}
		if (!flag5)
		{
			lastMonkeBlock = null;
		}
		if (num8 != lastMovingSurfaceID || lastMovingSurfaceContact != movingSurfaceContactPoint || flag5 != wasMovingSurfaceMonkeBlock)
		{
			if (num8 == -1)
			{
				if (Time.frameCount - lastAttachedToMovingSurfaceFrame > 3)
				{
					VRRig.DetachLocalPlayerFromMovingSurface();
					lastMovingSurfaceID = -1;
				}
			}
			else if (flag5)
			{
				if (lastMonkeBlock != null)
				{
					VRRig.AttachLocalPlayerToMovingSurface(num8, movingSurfaceContactPoint == MovingSurfaceContactPoint.LEFT, movingSurfaceContactPoint == MovingSurfaceContactPoint.BODY, lastMonkeBlock.transform.InverseTransformPoint(position2), flag5);
					lastMovingSurfaceID = num8;
				}
				else
				{
					VRRig.DetachLocalPlayerFromMovingSurface();
					lastMovingSurfaceID = -1;
				}
			}
			else if (MovingSurfaceManager.instance != null)
			{
				if (MovingSurfaceManager.instance.TryGetMovingSurface(num8, out var result))
				{
					VRRig.AttachLocalPlayerToMovingSurface(num8, movingSurfaceContactPoint == MovingSurfaceContactPoint.LEFT, movingSurfaceContactPoint == MovingSurfaceContactPoint.BODY, result.transform.InverseTransformPoint(position2), flag5);
					lastMovingSurfaceID = num8;
				}
				else
				{
					VRRig.DetachLocalPlayerFromMovingSurface();
					lastMovingSurfaceID = -1;
				}
			}
			else
			{
				VRRig.DetachLocalPlayerFromMovingSurface();
				lastMovingSurfaceID = -1;
			}
		}
		if (lastMovingSurfaceContact == MovingSurfaceContactPoint.NONE && movingSurfaceContactPoint != MovingSurfaceContactPoint.NONE)
		{
			SetPlayerVelocity(Vector3.zero);
		}
		lastMovingSurfaceContact = movingSurfaceContactPoint;
		wasMovingSurfaceMonkeBlock = flag5;
		if (activeSizeChangerSettings != null)
		{
			if (activeSizeChangerSettings.ExpireOnDistance > 0f && Vector3.Distance(base.transform.position, activeSizeChangerSettings.WorldPosition) > activeSizeChangerSettings.ExpireOnDistance)
			{
				SetNativeScale(null);
			}
			if (activeSizeChangerSettings.ExpireAfterSeconds > 0f && Time.time - activeSizeChangerSettings.ActivationTime > activeSizeChangerSettings.ExpireAfterSeconds)
			{
				SetNativeScale(null);
			}
		}
		TakeMyHand_HandLink grabbedLink = VRRig.LocalRig.leftHandLink.grabbedLink;
		if (grabbedLink != null)
		{
			_ = PhotonNetwork.Time;
			_ = LastHandTouchedGroundAtNetworkTime;
			_ = PhotonNetwork.Time;
			_ = grabbedLink.myRig.LastHandTouchedGroundAtNetworkTime;
		}
		if (didAJump || anyHandIsColliding || anyHandIsSliding || anyHandIsSticking || IsGroundedHand || forceRBSync)
		{
			playerRigidBody.position = base.transform.position;
			playerRigidBody.rotation = base.transform.rotation;
			forceRBSync = false;
		}
	}

	private float ApplyNativeScaleAdjustment(float adjustedMagnitude)
	{
		if (nativeScale > 0f && nativeScale != 1f)
		{
			return adjustedMagnitude *= nativeScaleMagnitudeAdjustmentFactor.Evaluate(nativeScale);
		}
		return adjustedMagnitude;
	}

	private float RotateWithSurface(Quaternion rotationDelta, Vector3 pivot)
	{
		QuaternionUtil.DecomposeSwingTwist(rotationDelta, GTPlayerTransform.PhysicsUp, out var _, out var twist);
		float num = twist.eulerAngles.y;
		if (num > 270f)
		{
			num -= 360f;
		}
		else if (num > 90f)
		{
			num -= 180f;
		}
		if (Mathf.Abs(num) < 90f * calcDeltaTime)
		{
			turnParent.transform.RotateAround(pivot, base.transform.up, num);
			return num;
		}
		return 0f;
	}

	private void stuckHandsCheckFixedUpdate()
	{
		Vector3 currentHandPosition = leftHand.GetCurrentHandPosition();
		stuckLeft = !controllerState.LeftValid || (leftHand.isColliding && (currentHandPosition - leftHand.GetLastPosition()).magnitude > unStickDistance * scale && !Physics.Raycast(headCollider.transform.position, (currentHandPosition - headCollider.transform.position).normalized, (currentHandPosition - headCollider.transform.position).magnitude, locomotionEnabledLayers.value));
		Vector3 currentHandPosition2 = rightHand.GetCurrentHandPosition();
		stuckRight = !controllerState.RightValid || (rightHand.isColliding && (currentHandPosition2 - rightHand.GetLastPosition()).magnitude > unStickDistance * scale && !Physics.Raycast(headCollider.transform.position, (currentHandPosition2 - headCollider.transform.position).normalized, (currentHandPosition2 - headCollider.transform.position).magnitude, locomotionEnabledLayers.value));
	}

	private void stuckHandsCheckLateUpdate(ref Vector3 finalLeftHandPosition, ref Vector3 finalRightHandPosition)
	{
		if (stuckLeft)
		{
			finalLeftHandPosition = leftHand.GetCurrentHandPosition();
			stuckLeft = (leftHand.isColliding = false);
		}
		if (stuckRight)
		{
			finalRightHandPosition = rightHand.GetCurrentHandPosition();
			stuckRight = (rightHand.isColliding = false);
		}
	}

	private void handleClimbing(float deltaTime)
	{
		if (isClimbing && (inOverlay || climbHelper == null || currentClimbable == null || !currentClimbable.isActiveAndEnabled))
		{
			EndClimbing(currentClimber, startingNewClimb: false);
		}
		Vector3 zero = Vector3.zero;
		if (isClimbing && (currentClimber.transform.position - climbHelper.position).magnitude > 1f)
		{
			EndClimbing(currentClimber, startingNewClimb: false);
		}
		if (isClimbing)
		{
			playerRigidBody.linearVelocity = Vector3.zero;
			climbHelper.localPosition = Vector3.MoveTowards(climbHelper.localPosition, climbHelperTargetPos, deltaTime * 12f);
			zero = currentClimber.transform.position - climbHelper.position;
			zero = ((zero.sqrMagnitude > maxArmLength * maxArmLength) ? (zero.normalized * maxArmLength) : zero);
			if (isClimbableMoving)
			{
				Quaternion rotationDelta = currentClimbable.transform.rotation * Quaternion.Inverse(lastClimbableRotation);
				RotateWithSurface(rotationDelta, currentClimber.handRoot.position);
				lastClimbableRotation = currentClimbable.transform.rotation;
			}
			playerRigidBody.position -= zero;
			if ((bool)currentSwing)
			{
				currentSwing.lastGrabTime = Time.time;
			}
		}
	}

	public void RequestTentacleMove(bool isLeftHand, Vector3 move)
	{
		if (isLeftHand)
		{
			hasLeftHandTentacleMove = true;
			leftHandTentacleMove = move;
		}
		else
		{
			hasRightHandTentacleMove = true;
			rightHandTentacleMove = move;
		}
	}

	public void HandleTentacleMovement()
	{
		Vector3 vector;
		if (hasLeftHandTentacleMove)
		{
			if (hasRightHandTentacleMove)
			{
				vector = (leftHandTentacleMove + rightHandTentacleMove) * 0.5f;
				hasRightHandTentacleMove = (hasLeftHandTentacleMove = false);
			}
			else
			{
				vector = leftHandTentacleMove;
				hasLeftHandTentacleMove = false;
			}
		}
		else
		{
			if (!hasRightHandTentacleMove)
			{
				return;
			}
			vector = rightHandTentacleMove;
			hasRightHandTentacleMove = false;
		}
		playerRigidBody.transform.position += vector;
		playerRigidBody.linearVelocity = Vector3.zero;
	}

	public HandLinkAuthorityStatus TakeMyHand_GetSelfHandLinkAuthority()
	{
		int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
		if (IsGroundedHand)
		{
			return new HandLinkAuthorityStatus(HandLinkAuthorityType.HandGrounded);
		}
		if ((double)(LastHandTouchedGroundAtNetworkTime + 1f) > PhotonNetwork.Time)
		{
			return new HandLinkAuthorityStatus(HandLinkAuthorityType.ResidualHandGrounded, LastHandTouchedGroundAtNetworkTime, actorNumber);
		}
		if (IsGroundedButt)
		{
			return new HandLinkAuthorityStatus(HandLinkAuthorityType.ButtGrounded);
		}
		return new HandLinkAuthorityStatus(HandLinkAuthorityType.None, LastTouchedGroundAtNetworkTime, actorNumber);
	}

	private void TakeMyHand_ProcessMovement()
	{
		TakeMyHand_HandLink leftHandLink = VRRig.LocalRig.leftHandLink;
		TakeMyHand_HandLink rightHandLink = VRRig.LocalRig.rightHandLink;
		bool flag = leftHandLink.grabbedLink != null;
		bool flag2 = rightHandLink.grabbedLink != null;
		if (!flag && !flag2)
		{
			return;
		}
		HandLinkAuthorityStatus handLinkAuthorityStatus = TakeMyHand_GetSelfHandLinkAuthority();
		int stepsToAuth = -1;
		HandLinkAuthorityStatus b = new HandLinkAuthorityStatus(HandLinkAuthorityType.None);
		if (flag)
		{
			b = leftHandLink.GetChainAuthority(out stepsToAuth);
		}
		int stepsToAuth2 = -1;
		HandLinkAuthorityStatus b2 = new HandLinkAuthorityStatus(HandLinkAuthorityType.None);
		if (flag2)
		{
			b2 = rightHandLink.GetChainAuthority(out stepsToAuth2);
		}
		if (flag && flag2)
		{
			if (leftHandLink.grabbedPlayer == rightHandLink.grabbedPlayer)
			{
				switch (handLinkAuthorityStatus.CompareTo(b))
				{
				case 1:
					TakeMyHand_PositionChild_RemotePlayer_BothHands(leftHandLink, rightHandLink);
					break;
				case -1:
					TakeMyHand_PositionChild_LocalPlayer(leftHandLink, rightHandLink);
					break;
				case 0:
					TakeMyHand_PositionBoth_BothHands(leftHandLink, rightHandLink);
					break;
				}
				return;
			}
			int num = handLinkAuthorityStatus.CompareTo(b);
			int num2 = handLinkAuthorityStatus.CompareTo(b2);
			switch (num * 3 + num2)
			{
			case 4:
				TakeMyHand_PositionChild_RemotePlayer(leftHandLink);
				TakeMyHand_PositionChild_RemotePlayer(rightHandLink);
				return;
			case 3:
				TakeMyHand_PositionBoth(rightHandLink);
				TakeMyHand_PositionChild_RemotePlayer(leftHandLink);
				return;
			case 1:
				TakeMyHand_PositionBoth(leftHandLink);
				TakeMyHand_PositionChild_RemotePlayer(rightHandLink);
				return;
			case 0:
				TakeMyHand_PositionTriple(leftHandLink, rightHandLink);
				return;
			case -1:
			case 2:
				TakeMyHand_PositionChild_LocalPlayer(rightHandLink);
				TakeMyHand_PositionChild_RemotePlayer(leftHandLink);
				return;
			case -3:
			case -2:
				TakeMyHand_PositionChild_LocalPlayer(leftHandLink);
				TakeMyHand_PositionChild_RemotePlayer(rightHandLink);
				return;
			}
			switch (b.CompareTo(b2))
			{
			case 1:
				TakeMyHand_PositionChild_LocalPlayer(leftHandLink);
				TakeMyHand_PositionChild_RemotePlayer(rightHandLink);
				break;
			case -1:
				TakeMyHand_PositionChild_LocalPlayer(rightHandLink);
				TakeMyHand_PositionChild_RemotePlayer(leftHandLink);
				break;
			case 0:
				if (stepsToAuth > stepsToAuth2)
				{
					TakeMyHand_PositionChild_LocalPlayer(rightHandLink);
					TakeMyHand_PositionChild_RemotePlayer(leftHandLink);
				}
				else if (stepsToAuth < stepsToAuth2)
				{
					TakeMyHand_PositionChild_LocalPlayer(leftHandLink);
					TakeMyHand_PositionChild_RemotePlayer(rightHandLink);
				}
				else
				{
					TakeMyHand_PositionChild_LocalPlayer(leftHandLink, rightHandLink);
				}
				break;
			}
		}
		else if (flag)
		{
			switch (handLinkAuthorityStatus.CompareTo(b))
			{
			case 1:
				TakeMyHand_PositionChild_RemotePlayer(leftHandLink);
				break;
			case -1:
				TakeMyHand_PositionChild_LocalPlayer(leftHandLink);
				break;
			case 0:
				TakeMyHand_PositionBoth(leftHandLink);
				break;
			}
		}
		else
		{
			switch (handLinkAuthorityStatus.CompareTo(b2))
			{
			case 1:
				TakeMyHand_PositionChild_RemotePlayer(rightHandLink);
				break;
			case -1:
				TakeMyHand_PositionChild_LocalPlayer(rightHandLink);
				break;
			case 0:
				TakeMyHand_PositionBoth(rightHandLink);
				break;
			}
		}
	}

	private void TakeMyHand_PositionTriple(TakeMyHand_HandLink linkA, TakeMyHand_HandLink linkB)
	{
		Vector3 vector = linkA.LinkPosition - linkA.grabbedLink.LinkPosition;
		Vector3 vector2 = linkB.LinkPosition - linkB.grabbedLink.LinkPosition;
		Vector3 vector3 = (vector + vector2) * 0.33f;
		linkA.grabbedLink.myRig.TrySweptOffsetMove(vector - vector3, out var _, out var _);
		linkB.grabbedLink.myRig.TrySweptOffsetMove(vector2 - vector3, out var _, out var _);
		playerRigidBody.MovePosition(playerRigidBody.position - vector3);
		playerRigidBody.linearVelocity = Vector3.zero;
	}

	private void TakeMyHand_PositionBoth(TakeMyHand_HandLink link)
	{
		Vector3 vector = (link.grabbedLink.LinkPosition - link.LinkPosition) * 0.5f;
		link.grabbedLink.myRig.TrySweptOffsetMove(-vector, out var handCollided, out var buttCollided);
		if (handCollided || buttCollided)
		{
			TakeMyHand_PositionChild_LocalPlayer(link);
		}
		else
		{
			playerRigidBody.transform.position += vector;
		}
		playerRigidBody.linearVelocity = Vector3.zero;
	}

	private void TakeMyHand_PositionBoth_BothHands(TakeMyHand_HandLink link1, TakeMyHand_HandLink link2)
	{
		Vector3 vector = (link1.grabbedLink.LinkPosition - link1.LinkPosition) * 0.5f;
		Vector3 vector2 = (link2.grabbedLink.LinkPosition - link2.LinkPosition) * 0.5f;
		Vector3 vector3 = (vector + vector2) * 0.5f;
		link1.grabbedLink.myRig.TrySweptOffsetMove(-vector3, out var handCollided, out var buttCollided);
		if (handCollided || buttCollided)
		{
			TakeMyHand_PositionChild_LocalPlayer(link1, link2);
		}
		else
		{
			playerRigidBody.transform.position += vector3;
		}
		playerRigidBody.linearVelocity = Vector3.zero;
	}

	private void TakeMyHand_PositionChild_LocalPlayer(TakeMyHand_HandLink parentLink)
	{
		Vector3 vector = parentLink.grabbedLink.LinkPosition - parentLink.LinkPosition;
		playerRigidBody.transform.position += vector;
		playerRigidBody.linearVelocity = Vector3.zero;
	}

	private void TakeMyHand_PositionChild_LocalPlayer(TakeMyHand_HandLink linkA, TakeMyHand_HandLink linkB)
	{
		Vector3 vector = linkA.grabbedLink.LinkPosition - linkA.LinkPosition;
		Vector3 vector2 = linkB.grabbedLink.LinkPosition - linkB.LinkPosition;
		playerRigidBody.transform.position += (vector + vector2) * 0.5f;
		playerRigidBody.linearVelocity = Vector3.zero;
	}

	private void TakeMyHand_PositionChild_RemotePlayer(TakeMyHand_HandLink childLink)
	{
		Vector3 movement = childLink.LinkPosition - childLink.grabbedLink.LinkPosition;
		childLink.grabbedLink.myRig.TrySweptOffsetMove(movement, out var handCollided, out var buttCollided);
		if (handCollided || buttCollided)
		{
			TakeMyHand_PositionChild_LocalPlayer(childLink);
		}
	}

	private void TakeMyHand_PositionChild_RemotePlayer_BothHands(TakeMyHand_HandLink childLink1, TakeMyHand_HandLink childLink2)
	{
		Vector3 vector = childLink1.LinkPosition - childLink1.grabbedLink.LinkPosition;
		Vector3 vector2 = childLink2.LinkPosition - childLink2.grabbedLink.LinkPosition;
		Vector3 movement = (vector + vector2) * 0.5f;
		childLink1.grabbedLink.myRig.TrySweptOffsetMove(movement, out var handCollided, out var buttCollided);
		if (handCollided || buttCollided)
		{
			TakeMyHand_PositionChild_LocalPlayer(childLink1, childLink2);
		}
	}

	private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, Vector3 boostVector, out Vector3 endPosition, bool singleHand, out float slipPercentage, out RaycastHit iterativeHitInfo, bool fullSlide)
	{
		slipPercentage = defaultSlideFactor;
		if (CollisionsSphereCast(startPosition, sphereRadius, movementVector, out endPosition, out tempIterativeHit))
		{
			firstPosition = endPosition;
			iterativeHitInfo = tempIterativeHit;
			slideFactor = GetSlidePercentage(iterativeHitInfo);
			slipPercentage = ((slideFactor != defaultSlideFactor) ? slideFactor : ((!singleHand) ? defaultSlideFactor : 0.001f));
			if (fullSlide)
			{
				slipPercentage = 1f;
			}
			movementToProjectedAboveCollisionPlane = Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, iterativeHitInfo.normal) * slipPercentage;
			Vector3 vector = Vector3.zero;
			if (boostVector.IsLongerThan(0f))
			{
				vector = Vector3.ProjectOnPlane(boostVector, iterativeHitInfo.normal);
				movementToProjectedAboveCollisionPlane += vector;
				CollisionsSphereCast(firstPosition, sphereRadius, vector, out endPosition, out tempIterativeHit);
				firstPosition = endPosition;
			}
			if (CollisionsSphereCast(firstPosition, sphereRadius, movementToProjectedAboveCollisionPlane, out endPosition, out tempIterativeHit))
			{
				iterativeHitInfo = tempIterativeHit;
				return true;
			}
			if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius, startPosition + movementVector + vector - (movementToProjectedAboveCollisionPlane + firstPosition), out endPosition, out tempIterativeHit))
			{
				iterativeHitInfo = tempIterativeHit;
				return true;
			}
			endPosition = Vector3.zero;
			return false;
		}
		iterativeHitInfo = tempIterativeHit;
		endPosition = Vector3.zero;
		return false;
	}

	private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, out Vector3 finalPosition, out RaycastHit collisionsHitInfo)
	{
		MaxSphereSizeForNoOverlap(sphereRadius, startPosition, ignoreOneWay: false, out maxSphereSize1);
		bool flag = false;
		ClearRaycasthitBuffer(ref rayCastNonAllocColliders);
		bufferCount = Physics.SphereCastNonAlloc(startPosition, maxSphereSize1, movementVector.normalized, rayCastNonAllocColliders, movementVector.magnitude, locomotionEnabledLayers.value);
		if (bufferCount > 0)
		{
			tempHitInfo = rayCastNonAllocColliders[0];
			for (int i = 0; i < bufferCount; i++)
			{
				if (!(tempHitInfo.distance <= 0f) && (!flag || rayCastNonAllocColliders[i].distance < tempHitInfo.distance))
				{
					flag = true;
					tempHitInfo = rayCastNonAllocColliders[i];
				}
			}
		}
		if (flag)
		{
			collisionsHitInfo = tempHitInfo;
			finalPosition = collisionsHitInfo.point + collisionsHitInfo.normal * sphereRadius;
			ClearRaycasthitBuffer(ref rayCastNonAllocColliders);
			bufferCount = Physics.RaycastNonAlloc(startPosition, (finalPosition - startPosition).normalized, rayCastNonAllocColliders, (finalPosition - startPosition).magnitude, locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore);
			if (bufferCount > 0)
			{
				tempHitInfo = rayCastNonAllocColliders[0];
				for (int j = 0; j < bufferCount; j++)
				{
					if ((bool)rayCastNonAllocColliders[j].collider && rayCastNonAllocColliders[j].distance < tempHitInfo.distance)
					{
						tempHitInfo = rayCastNonAllocColliders[j];
					}
				}
				finalPosition = startPosition + movementVector.normalized * tempHitInfo.distance;
			}
			MaxSphereSizeForNoOverlap(sphereRadius, finalPosition, ignoreOneWay: false, out maxSphereSize2);
			ClearRaycasthitBuffer(ref rayCastNonAllocColliders);
			bufferCount = Physics.SphereCastNonAlloc(startPosition, Mathf.Min(maxSphereSize1, maxSphereSize2), (finalPosition - startPosition).normalized, rayCastNonAllocColliders, (finalPosition - startPosition).magnitude, locomotionEnabledLayers.value);
			if (bufferCount > 0)
			{
				tempHitInfo = rayCastNonAllocColliders[0];
				for (int k = 0; k < bufferCount; k++)
				{
					if (rayCastNonAllocColliders[k].collider != null && rayCastNonAllocColliders[k].distance < tempHitInfo.distance)
					{
						tempHitInfo = rayCastNonAllocColliders[k];
					}
				}
				finalPosition = startPosition + tempHitInfo.distance * (finalPosition - startPosition).normalized;
				collisionsHitInfo = tempHitInfo;
			}
			return true;
		}
		ClearRaycasthitBuffer(ref rayCastNonAllocColliders);
		bufferCount = Physics.RaycastNonAlloc(startPosition, movementVector.normalized, rayCastNonAllocColliders, movementVector.magnitude, locomotionEnabledLayers.value);
		if (bufferCount > 0)
		{
			tempHitInfo = rayCastNonAllocColliders[0];
			for (int l = 0; l < bufferCount; l++)
			{
				if (rayCastNonAllocColliders[l].collider != null && rayCastNonAllocColliders[l].distance < tempHitInfo.distance)
				{
					tempHitInfo = rayCastNonAllocColliders[l];
				}
			}
			collisionsHitInfo = tempHitInfo;
			finalPosition = startPosition;
			return true;
		}
		finalPosition = startPosition + movementVector;
		collisionsHitInfo = default(RaycastHit);
		return false;
	}

	public float GetSlidePercentage(RaycastHit raycastHit)
	{
		if (IsFrozen && GorillaGameManager.instance is GorillaFreezeTagManager)
		{
			return FreezeTagSlidePercentage();
		}
		currentOverride = raycastHit.collider.gameObject.GetComponent<GorillaSurfaceOverride>();
		BasePlatform component = raycastHit.collider.gameObject.GetComponent<BasePlatform>();
		if (component != null)
		{
			currentPlatform = component;
		}
		if (currentOverride != null)
		{
			if (currentOverride.slidePercentageOverride >= 0f)
			{
				return currentOverride.slidePercentageOverride;
			}
			currentMaterialIndex = currentOverride.overrideIndex;
			if (currentMaterialIndex >= 0 && currentMaterialIndex < materialData.Count)
			{
				if (!materialData[currentMaterialIndex].overrideSlidePercent)
				{
					return defaultSlideFactor;
				}
				return materialData[currentMaterialIndex].slidePercent;
			}
			return defaultSlideFactor;
		}
		meshCollider = raycastHit.collider as MeshCollider;
		if (meshCollider == null || meshCollider.sharedMesh == null || meshCollider.convex)
		{
			return defaultSlideFactor;
		}
		collidedMesh = meshCollider.sharedMesh;
		if (!meshTrianglesDict.TryGetValue(collidedMesh, out sharedMeshTris))
		{
			sharedMeshTris = collidedMesh.triangles;
			meshTrianglesDict.Add(collidedMesh, (int[])sharedMeshTris.Clone());
		}
		vertex1 = sharedMeshTris[raycastHit.triangleIndex * 3];
		vertex2 = sharedMeshTris[raycastHit.triangleIndex * 3 + 1];
		vertex3 = sharedMeshTris[raycastHit.triangleIndex * 3 + 2];
		slideRenderer = raycastHit.collider.GetComponent<Renderer>();
		if (slideRenderer != null)
		{
			slideRenderer.GetSharedMaterials(tempMaterialArray);
		}
		else
		{
			tempMaterialArray.Clear();
		}
		if (tempMaterialArray.Count > 1)
		{
			for (int i = 0; i < tempMaterialArray.Count; i++)
			{
				collidedMesh.GetTriangles(trianglesList, i);
				for (int j = 0; j < trianglesList.Count; j += 3)
				{
					if (trianglesList[j] == vertex1 && trianglesList[j + 1] == vertex2 && trianglesList[j + 2] == vertex3)
					{
						findMatName = tempMaterialArray[i].name;
						if (findMatName.EndsWith("Uber"))
						{
							string text = findMatName;
							findMatName = text.Substring(0, text.Length - 4);
						}
						foundMatData = materialData.Find((MaterialData matData) => matData.matName == findMatName);
						currentMaterialIndex = materialData.FindIndex((MaterialData matData) => matData.matName == findMatName);
						if (currentMaterialIndex == -1)
						{
							currentMaterialIndex = 0;
						}
						if (!foundMatData.overrideSlidePercent)
						{
							return defaultSlideFactor;
						}
						return foundMatData.slidePercent;
					}
				}
			}
		}
		else if (tempMaterialArray.Count > 0)
		{
			return defaultSlideFactor;
		}
		currentMaterialIndex = 0;
		return defaultSlideFactor;
	}

	public bool IsTouchingMovingSurface(Vector3 rayOrigin, RaycastHit raycastHit, out int movingSurfaceId, out bool sideTouch, out bool isMonkeBlock)
	{
		movingSurfaceId = -1;
		sideTouch = false;
		isMonkeBlock = false;
		float num = Vector3.Dot(rayOrigin - raycastHit.point, Vector3.up);
		if (num < -0.3f)
		{
			return false;
		}
		if (num < 0f)
		{
			sideTouch = true;
		}
		if (raycastHit.collider == null)
		{
			return false;
		}
		MovingSurface component = raycastHit.collider.GetComponent<MovingSurface>();
		if (component != null)
		{
			isMonkeBlock = false;
			movingSurfaceId = component.GetID();
			return true;
		}
		if (!BuilderTable.IsLocalPlayerInBuilderZone())
		{
			return false;
		}
		BuilderPiece builderPieceFromCollider = BuilderPiece.GetBuilderPieceFromCollider(raycastHit.collider);
		if (builderPieceFromCollider != null && builderPieceFromCollider.IsPieceMoving())
		{
			isMonkeBlock = true;
			movingSurfaceId = builderPieceFromCollider.pieceId;
			lastMonkeBlock = builderPieceFromCollider;
			return true;
		}
		sideTouch = false;
		return false;
	}

	public void Turn(float degrees)
	{
		Vector3 position = headCollider.transform.position;
		bool flag = rightHand.isColliding || rightHand.isHolding;
		bool flag2 = leftHand.isColliding || leftHand.isHolding;
		if (flag != flag2 && flag)
		{
			position = rightHand.controllerTransform.position;
		}
		if (flag != flag2 && flag2)
		{
			position = leftHand.controllerTransform.position;
		}
		turnParent.transform.RotateAround(position, GTPlayerTransform.Up, degrees);
		degreesTurnedThisFrame = degrees;
		averagedVelocity = Vector3.zero;
		Quaternion quaternion = Quaternion.AngleAxis(degrees, GTPlayerTransform.Up);
		for (int i = 0; i < velocityHistory.Length; i++)
		{
			velocityHistory[i] = quaternion * velocityHistory[i];
			averagedVelocity += velocityHistory[i];
		}
		averagedVelocity /= (float)velocityHistorySize;
	}

	public void BeginClimbing(GorillaClimbable climbable, GorillaHandClimber hand, GorillaClimbableRef climbableRef = null)
	{
		if (currentClimber != null)
		{
			EndClimbing(currentClimber, startingNewClimb: true);
		}
		try
		{
			climbable.onBeforeClimb?.Invoke(hand, climbableRef);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		climbable.TryGetComponent<Rigidbody>(out var _);
		VerifyClimbHelper();
		climbHelper.SetParent(climbable.transform);
		climbHelper.position = hand.transform.position;
		Vector3 localPosition = climbHelper.localPosition;
		if (climbable.snapX)
		{
			SnapAxis(ref localPosition.x, climbable.maxDistanceSnap);
		}
		if (climbable.snapY)
		{
			SnapAxis(ref localPosition.y, climbable.maxDistanceSnap);
		}
		if (climbable.snapZ)
		{
			SnapAxis(ref localPosition.z, climbable.maxDistanceSnap);
		}
		climbHelperTargetPos = localPosition;
		climbable.isBeingClimbed = true;
		hand.isClimbing = true;
		currentClimbable = climbable;
		currentClimber = hand;
		isClimbing = true;
		if (climbable.climbOnlyWhileSmall)
		{
			BuilderPiece componentInParent = climbable.GetComponentInParent<BuilderPiece>();
			if (componentInParent != null && componentInParent.IsPieceMoving())
			{
				isClimbableMoving = true;
				lastClimbableRotation = climbable.transform.rotation;
			}
			else
			{
				isClimbableMoving = false;
			}
		}
		else
		{
			isClimbableMoving = false;
		}
		GorillaZipline component3;
		PhotonView component4;
		PhotonViewXSceneRef component5;
		if (climbable.TryGetComponent<GorillaRopeSegment>(out var component2) && (bool)component2.swing)
		{
			currentSwing = component2.swing;
			currentSwing.AttachLocalPlayer(hand.xrNode, climbable.transform, climbHelperTargetPos, averagedVelocity);
		}
		else if ((bool)climbable.transform.parent && climbable.transform.parent.TryGetComponent<GorillaZipline>(out component3))
		{
			currentZipline = component3;
		}
		else if (climbable.TryGetComponent<PhotonView>(out component4))
		{
			VRRig.AttachLocalPlayerToPhotonView(component4, hand.xrNode, climbHelperTargetPos, averagedVelocity);
		}
		else if (climbable.TryGetComponent<PhotonViewXSceneRef>(out component5))
		{
			VRRig.AttachLocalPlayerToPhotonView(component5.photonView, hand.xrNode, climbHelperTargetPos, averagedVelocity);
		}
		GorillaTagger.Instance.StartVibration(currentClimber.xrNode == XRNode.LeftHand, 0.6f, 0.06f);
		if ((bool)climbable.clip)
		{
			GorillaTagger.Instance.offlineVRRig.PlayClimbSound(climbable.clip, hand.xrNode == XRNode.LeftHand);
		}
		static void SnapAxis(ref float val, float maxDist)
		{
			if (val > maxDist)
			{
				val = maxDist;
			}
			else if (val < 0f - maxDist)
			{
				val = 0f - maxDist;
			}
		}
	}

	private void VerifyClimbHelper()
	{
		if (climbHelper == null || climbHelper.gameObject == null)
		{
			climbHelper = new GameObject("Climb Helper").transform;
		}
	}

	public void EndClimbing(GorillaHandClimber hand, bool startingNewClimb, bool doDontReclimb = false)
	{
		if (hand != currentClimber)
		{
			return;
		}
		hand.SetCanRelease(canRelease: true);
		if (!startingNewClimb)
		{
			enablePlayerGravity(useGravity: true);
		}
		Rigidbody component = null;
		if ((bool)currentClimbable)
		{
			currentClimbable.TryGetComponent<Rigidbody>(out component);
			currentClimbable.isBeingClimbed = false;
		}
		Vector3 force = Vector3.zero;
		if ((bool)currentClimber)
		{
			currentClimber.isClimbing = false;
			if (doDontReclimb)
			{
				currentClimber.dontReclimbLast = currentClimbable;
			}
			else
			{
				currentClimber.dontReclimbLast = null;
			}
			currentClimber.queuedToBecomeValidToGrabAgain = true;
			currentClimber.lastAutoReleasePos = currentClimber.handRoot.localPosition;
			if (!startingNewClimb && (bool)currentClimbable)
			{
				GorillaVelocityTracker interactPointVelocityTracker = GetInteractPointVelocityTracker(currentClimber.xrNode == XRNode.LeftHand);
				if ((bool)component)
				{
					playerRigidBody.linearVelocity = component.linearVelocity;
				}
				else if ((bool)currentSwing)
				{
					playerRigidBody.linearVelocity = currentSwing.velocityTracker.GetAverageVelocity(worldSpace: true, 0.25f);
				}
				else if ((bool)currentZipline)
				{
					playerRigidBody.linearVelocity = currentZipline.GetCurrentDirection() * currentZipline.currentSpeed;
				}
				else
				{
					playerRigidBody.linearVelocity = Vector3.zero;
				}
				force = turnParent.transform.rotation * -interactPointVelocityTracker.GetAverageVelocity(worldSpace: false, 0.1f, doMagnitudeCheck: true) * scale;
				force = Vector3.ClampMagnitude(force, 5.5f * scale);
				playerRigidBody.AddForce(force, ForceMode.VelocityChange);
			}
		}
		if ((bool)currentSwing)
		{
			currentSwing.DetachLocalPlayer();
		}
		if (currentClimbable.TryGetComponent<PhotonView>(out var _) || currentClimbable.TryGetComponent<PhotonViewXSceneRef>(out var _) || currentClimbable.IsPlayerAttached)
		{
			VRRig.DetachLocalPlayerFromPhotonView();
		}
		if (!startingNewClimb && force.magnitude > 2f && (bool)currentClimbable && (bool)currentClimbable.clipOnFullRelease)
		{
			GorillaTagger.Instance.offlineVRRig.PlayClimbSound(currentClimbable.clipOnFullRelease, hand.xrNode == XRNode.LeftHand);
		}
		currentClimbable = null;
		currentClimber = null;
		currentSwing = null;
		currentZipline = null;
		isClimbing = false;
	}

	public void ResetRigidbodyInterpolation()
	{
		playerRigidBody.interpolation = playerRigidbodyInterpolationDefault;
	}

	private void enablePlayerGravity(bool useGravity)
	{
		playerRigidBody.useGravity = useGravity;
	}

	public void SetVelocity(Vector3 velocity)
	{
		playerRigidBody.linearVelocity = velocity;
	}

	internal void RigidbodyMovePosition(Vector3 pos)
	{
		playerRigidBody.MovePosition(pos);
	}

	public void TempFreezeHand(bool isLeft, float freezeDuration)
	{
		(isLeft ? leftHand : rightHand).TempFreezeHand(freezeDuration);
	}

	private void StoreVelocities()
	{
		velocityIndex = (velocityIndex + 1) % velocityHistorySize;
		currentVelocity = (base.transform.position - lastPosition - GTPlayerTransform.RotationPosOffsetChange - MovingSurfaceMovement()) / calcDeltaTime;
		velocityHistory[velocityIndex] = currentVelocity;
		averagedVelocity = velocityHistory.Average();
		lastPosition = base.transform.position;
		GTPlayerTransform.ResetRotationPositionOffset();
	}

	private void AntiTeleportTechnology()
	{
		if ((headCollider.transform.position - lastHeadPosition).magnitude >= teleportThresholdNoVel + playerRigidBody.linearVelocity.magnitude * calcDeltaTime)
		{
			ForceRigidBodySync();
			base.transform.position = base.transform.position + lastHeadPosition - headCollider.transform.position;
		}
	}

	private bool MaxSphereSizeForNoOverlap(float testRadius, Vector3 checkPosition, bool ignoreOneWay, out float overlapRadiusTest)
	{
		overlapRadiusTest = testRadius;
		overlapAttempts = 0;
		int num = 100;
		while (overlapAttempts < num && overlapRadiusTest > testRadius * 0.75f)
		{
			ClearColliderBuffer(ref overlapColliders);
			bufferCount = Physics.OverlapSphereNonAlloc(checkPosition, overlapRadiusTest, overlapColliders, locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore);
			if (ignoreOneWay)
			{
				int num2 = 0;
				for (int i = 0; i < bufferCount; i++)
				{
					if (overlapColliders[i].CompareTag("NoCrazyCheck"))
					{
						num2++;
					}
				}
				if (num2 == bufferCount)
				{
					return true;
				}
			}
			if (bufferCount > 0)
			{
				overlapRadiusTest = Mathf.Lerp(testRadius, 0f, (float)overlapAttempts / (float)num);
				overlapAttempts++;
				continue;
			}
			overlapRadiusTest *= 0.995f;
			return true;
		}
		return false;
	}

	private bool CrazyCheck2(float sphereSize, Vector3 startPosition)
	{
		for (int i = 0; i < crazyCheckVectors.Length; i++)
		{
			if (NonAllocRaycast(startPosition, startPosition + crazyCheckVectors[i] * sphereSize) > 0)
			{
				return false;
			}
		}
		return true;
	}

	private int NonAllocRaycast(Vector3 startPosition, Vector3 endPosition)
	{
		Vector3 direction = endPosition - startPosition;
		int num = Physics.RaycastNonAlloc(startPosition, direction, rayCastNonAllocColliders, direction.magnitude, locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			if (!rayCastNonAllocColliders[i].collider.gameObject.CompareTag("NoCrazyCheck"))
			{
				num2++;
			}
		}
		return num2;
	}

	private void ClearColliderBuffer(ref Collider[] colliders)
	{
		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i] = null;
		}
	}

	private void ClearRaycasthitBuffer(ref RaycastHit[] raycastHits)
	{
		for (int i = 0; i < raycastHits.Length; i++)
		{
			raycastHits[i] = emptyHit;
		}
	}

	private Vector3 MovingSurfaceMovement()
	{
		return refMovement + movingSurfaceOffset;
	}

	private static bool ComputeLocalHitPoint(RaycastHit hit, out Vector3 localHitPoint)
	{
		if (hit.collider == null || hit.point.sqrMagnitude < 0.001f)
		{
			localHitPoint = Vector3.zero;
			return false;
		}
		localHitPoint = hit.collider.transform.InverseTransformPoint(hit.point);
		return true;
	}

	private static bool ComputeWorldHitPoint(RaycastHit hit, Vector3 localPoint, out Vector3 worldHitPoint)
	{
		if (hit.collider == null)
		{
			worldHitPoint = Vector3.zero;
			return false;
		}
		worldHitPoint = hit.collider.transform.TransformPoint(localPoint);
		return true;
	}

	private float ExtraVelMultiplier()
	{
		float num = 1f;
		if (leftHand.surfaceOverride != null)
		{
			num = Mathf.Max(num, leftHand.surfaceOverride.extraVelMultiplier);
		}
		if (rightHand.surfaceOverride != null)
		{
			num = Mathf.Max(num, rightHand.surfaceOverride.extraVelMultiplier);
		}
		return num;
	}

	private float ExtraVelMaxMultiplier()
	{
		float num = 1f;
		if (leftHand.surfaceOverride != null)
		{
			num = Mathf.Max(num, leftHand.surfaceOverride.extraVelMaxMultiplier);
		}
		if (rightHand.surfaceOverride != null)
		{
			num = Mathf.Max(num, rightHand.surfaceOverride.extraVelMaxMultiplier);
		}
		return num * scale;
	}

	public void SetMaximumSlipThisFrame()
	{
		leftHand.slipSetToMaxFrameIdx = Time.frameCount;
		rightHand.slipSetToMaxFrameIdx = Time.frameCount;
	}

	public void SetLeftMaximumSlipThisFrame()
	{
		leftHand.slipSetToMaxFrameIdx = Time.frameCount;
	}

	public void SetRightMaximumSlipThisFrame()
	{
		rightHand.slipSetToMaxFrameIdx = Time.frameCount;
	}

	public void ChangeLayer(string layerName)
	{
		if (layerChanger != null)
		{
			layerChanger.ChangeLayer(base.transform.parent, layerName);
		}
	}

	public void RestoreLayer()
	{
		if (layerChanger != null)
		{
			layerChanger.RestoreOriginalLayers();
		}
	}

	public void OnEnterWaterVolume(Collider playerCollider, WaterVolume volume)
	{
		if (activeSizeChangerSettings != null && activeSizeChangerSettings.ExpireInWater)
		{
			SetNativeScale(null);
		}
		if (playerCollider == headCollider)
		{
			if (!headOverlappingWaterVolumes.Contains(volume))
			{
				headOverlappingWaterVolumes.Add(volume);
			}
		}
		else if (playerCollider == bodyCollider && !bodyOverlappingWaterVolumes.Contains(volume))
		{
			bodyOverlappingWaterVolumes.Add(volume);
		}
	}

	public void OnExitWaterVolume(Collider playerCollider, WaterVolume volume)
	{
		if (playerCollider == headCollider)
		{
			headOverlappingWaterVolumes.Remove(volume);
		}
		else if (playerCollider == bodyCollider)
		{
			bodyOverlappingWaterVolumes.Remove(volume);
		}
	}

	private bool GetSwimmingVelocityForHand(Vector3 startingHandPosition, Vector3 endingHandPosition, Vector3 palmForwardDirection, float dt, ref WaterVolume contactingWaterVolume, ref WaterVolume.SurfaceQuery waterSurface, out Vector3 swimmingVelocityChange)
	{
		contactingWaterVolume = null;
		bufferCount = Physics.OverlapSphereNonAlloc(endingHandPosition, minimumRaycastDistance, overlapColliders, waterLayer.value, QueryTriggerInteraction.Collide);
		if (bufferCount > 0)
		{
			float num = float.MinValue;
			for (int i = 0; i < bufferCount; i++)
			{
				WaterVolume component = overlapColliders[i].GetComponent<WaterVolume>();
				if (component != null && component.GetSurfaceQueryForPoint(endingHandPosition, out var result))
				{
					float num2 = Vector3.Dot(result.surfacePoint, GTPlayerTransform.PhysicsUp);
					if (num2 > num)
					{
						num = num2;
						contactingWaterVolume = component;
						waterSurface = result;
					}
				}
			}
		}
		if (forcedUnderwater || contactingWaterVolume != null)
		{
			Vector3 vector = endingHandPosition - startingHandPosition;
			Vector3 vector2 = Vector3.zero;
			Vector3 vector3 = playerRigidBody.transform.position - lastRigidbodyPosition;
			if (turnedThisFrame)
			{
				Vector3 vector4 = startingHandPosition - headCollider.transform.position;
				vector2 = Quaternion.AngleAxis(degreesTurnedThisFrame, GTPlayerTransform.PhysicsUp) * vector4 - vector4;
			}
			float num3 = Vector3.Dot(vector - vector2 - vector3, palmForwardDirection);
			float num4 = 0f;
			if (num3 > 0f)
			{
				float num5 = -1f;
				float num6 = -1f;
				if (!forcedUnderwater)
				{
					Plane surfacePlane = waterSurface.surfacePlane;
					num5 = (forcedUnderwater ? (-1f) : surfacePlane.GetDistanceToPoint(startingHandPosition));
					num6 = (forcedUnderwater ? (-1f) : surfacePlane.GetDistanceToPoint(endingHandPosition));
				}
				if (num5 <= 0f && num6 <= 0f)
				{
					num4 = 1f;
				}
				else if (num5 > 0f && num6 <= 0f)
				{
					num4 = (0f - num6) / (num5 - num6);
				}
				else if (num5 <= 0f && num6 > 0f)
				{
					num4 = (0f - num5) / (num6 - num5);
				}
				if (num4 > Mathf.Epsilon)
				{
					float resistance = liquidPropertiesList[(int)((!forcedUnderwater) ? contactingWaterVolume.LiquidType : LiquidType.Water)].resistance;
					swimmingVelocityChange = -palmForwardDirection * num3 * 2f * resistance * num4;
					Vector3 forward = mainCamera.transform.forward;
					if (Vector3.Dot(forward, GTPlayerTransform.PhysicsDown) > 0f)
					{
						Vector3 vector5 = Vector3.ProjectOnPlane(forward, GTPlayerTransform.PhysicsUp);
						float magnitude = vector5.magnitude;
						vector5 /= magnitude;
						float num7 = Vector3.Dot(swimmingVelocityChange, vector5);
						if (num7 > 0f)
						{
							Vector3 vector6 = vector5 * num7;
							float num8 = Vector3.Dot(forward, GTPlayerTransform.PhysicsUp);
							swimmingVelocityChange = swimmingVelocityChange - vector6 + vector6 * magnitude + GTPlayerTransform.PhysicsUp * num8 * num7;
						}
					}
					return true;
				}
			}
		}
		swimmingVelocityChange = Vector3.zero;
		return false;
	}

	private bool CheckWaterSurfaceJump(Vector3 startingHandPosition, Vector3 endingHandPosition, Vector3 palmForwardDirection, Vector3 handAvgVelocity, PlayerSwimmingParameters parameters, WaterVolume contactingWaterVolume, WaterVolume.SurfaceQuery waterSurface, out Vector3 jumpVelocity)
	{
		if (contactingWaterVolume != null)
		{
			Plane surfacePlane = waterSurface.surfacePlane;
			bool flag = handAvgVelocity.sqrMagnitude > parameters.waterSurfaceJumpHandSpeedThreshold * parameters.waterSurfaceJumpHandSpeedThreshold;
			if (surfacePlane.GetSide(startingHandPosition) && !surfacePlane.GetSide(endingHandPosition) && flag)
			{
				float value = Vector3.Dot(palmForwardDirection, -waterSurface.surfaceNormal);
				float value2 = Vector3.Dot(handAvgVelocity.normalized, -waterSurface.surfaceNormal);
				float num = parameters.waterSurfaceJumpPalmFacingCurve.Evaluate(Mathf.Clamp(value, 0.01f, 0.99f));
				float num2 = parameters.waterSurfaceJumpHandVelocityFacingCurve.Evaluate(Mathf.Clamp(value2, 0.01f, 0.99f));
				jumpVelocity = -handAvgVelocity * parameters.waterSurfaceJumpAmount * num * num2;
				return true;
			}
		}
		jumpVelocity = Vector3.zero;
		return false;
	}

	private bool TryNormalize(Vector3 input, out Vector3 normalized, out float magnitude, float eps = 0.0001f)
	{
		magnitude = input.magnitude;
		if (magnitude > eps)
		{
			normalized = input / magnitude;
			return true;
		}
		normalized = Vector3.zero;
		return false;
	}

	private bool TryNormalizeDown(Vector3 input, out Vector3 normalized, out float magnitude, float eps = 0.0001f)
	{
		magnitude = input.magnitude;
		if (magnitude > 1f)
		{
			normalized = input / magnitude;
			return true;
		}
		if (magnitude >= eps)
		{
			normalized = input;
			return true;
		}
		normalized = Vector3.zero;
		return false;
	}

	private float FreezeTagSlidePercentage()
	{
		if (materialData[currentMaterialIndex].overrideSlidePercent && materialData[currentMaterialIndex].slidePercent > freezeTagHandSlidePercent)
		{
			return materialData[currentMaterialIndex].slidePercent;
		}
		return freezeTagHandSlidePercent;
	}

	private void OnCollisionStay(UnityEngine.Collision collision)
	{
		bodyCollisionContactsCount = collision.GetContacts(bodyCollisionContacts);
		float num = -1f;
		for (int i = 0; i < bodyCollisionContactsCount; i++)
		{
			float num2 = Vector3.Dot(bodyCollisionContacts[i].normal, Vector3.up);
			if (num2 > num)
			{
				bodyGroundContact = bodyCollisionContacts[i];
				num = num2;
			}
		}
		_ = -1f;
		float num3 = 0.5f;
		if (num > num3)
		{
			bodyGroundContactTime = Time.time;
			Collider otherCollider = bodyGroundContact.otherCollider;
			bodyGroundIsSlippery = otherCollider != null && otherCollider.sharedMaterial != null && otherCollider.sharedMaterial.staticFriction <= 0.0001f && otherCollider.sharedMaterial.dynamicFriction <= 0.0001f;
		}
	}

	public async void DoLaunch(Vector3 velocity)
	{
		if (isClimbing)
		{
			EndClimbing(CurrentClimber, startingNewClimb: false);
		}
		playerRigidBody.linearVelocity = velocity;
		disableMovement = true;
		await Task.Delay(1);
		disableMovement = false;
	}

	private void OnEnable()
	{
		RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
	}

	private void OnJoinedRoom()
	{
		if (activeSizeChangerSettings != null && activeSizeChangerSettings.ExpireOnRoomJoin)
		{
			SetNativeScale(null);
		}
	}

	private void OnDisable()
	{
		RoomSystem.JoinedRoomEvent -= new Action(OnJoinedRoom);
	}

	public void ForceRigidBodySync()
	{
		forceRBSync = true;
	}

	internal void ClearHandHolds()
	{
		leftHand.isHolding = false;
		rightHand.isHolding = false;
		wasHoldingHandhold = false;
		activeHandHold = default(HandHoldState);
		secondaryHandHold = default(HandHoldState);
		OnChangeActiveHandhold();
	}

	internal void AddHandHold(Transform objectHeld, Vector3 localPositionHeld, GorillaGrabber grabber, bool forLeftHand, bool rotatePlayerWhenHeld, out Vector3 grabbedVelocity)
	{
		if (!leftHand.isHolding && !rightHand.isHolding)
		{
			grabbedVelocity = -bodyCollider.attachedRigidbody.linearVelocity;
			playerRigidBody.AddForce(grabbedVelocity, ForceMode.VelocityChange);
		}
		else
		{
			grabbedVelocity = Vector3.zero;
		}
		secondaryHandHold = activeHandHold;
		_ = grabber.transform.position;
		activeHandHold = new HandHoldState
		{
			grabber = grabber,
			objectHeld = objectHeld,
			localPositionHeld = localPositionHeld,
			localRotationalOffset = grabber.transform.rotation.eulerAngles.y - objectHeld.rotation.eulerAngles.y,
			applyRotation = rotatePlayerWhenHeld
		};
		if (forLeftHand)
		{
			leftHand.isHolding = true;
		}
		else
		{
			rightHand.isHolding = true;
		}
		OnChangeActiveHandhold();
	}

	internal void RemoveHandHold(GorillaGrabber grabber, bool forLeftHand)
	{
		_ = activeHandHold.objectHeld == grabber;
		if (activeHandHold.grabber == grabber)
		{
			activeHandHold = secondaryHandHold;
		}
		secondaryHandHold = default(HandHoldState);
		if (forLeftHand)
		{
			leftHand.isHolding = false;
		}
		else
		{
			rightHand.isHolding = false;
		}
		OnChangeActiveHandhold();
	}

	private void OnChangeActiveHandhold()
	{
		if (activeHandHold.objectHeld != null)
		{
			if (activeHandHold.objectHeld.TryGetComponent<PhotonView>(out var component))
			{
				VRRig.AttachLocalPlayerToPhotonView(component, activeHandHold.grabber.XrNode, activeHandHold.localPositionHeld, averagedVelocity);
				return;
			}
			if (activeHandHold.objectHeld.TryGetComponent<PhotonViewXSceneRef>(out var component2))
			{
				PhotonView photonView = component2.photonView;
				if ((object)photonView != null)
				{
					VRRig.AttachLocalPlayerToPhotonView(photonView, activeHandHold.grabber.XrNode, activeHandHold.localPositionHeld, averagedVelocity);
					return;
				}
			}
			if (activeHandHold.objectHeld.TryGetComponent<BuilderPieceHandHold>(out var component3) && component3.IsHandHoldMoving())
			{
				isHandHoldMoving = true;
				lastHandHoldRotation = component3.transform.rotation;
				movingHandHoldReleaseVelocity = playerRigidBody.linearVelocity;
			}
			else
			{
				isHandHoldMoving = false;
				lastHandHoldRotation = Quaternion.identity;
				movingHandHoldReleaseVelocity = Vector3.zero;
			}
		}
		VRRig.DetachLocalPlayerFromPhotonView();
	}

	private void FixedUpdate_HandHolds(float timeDelta)
	{
		if (activeHandHold.objectHeld == null)
		{
			if (wasHoldingHandhold)
			{
				playerRigidBody.linearVelocity = Vector3.ClampMagnitude(secondLastPreHandholdVelocity, 5.5f * scale);
			}
			wasHoldingHandhold = false;
			return;
		}
		Vector3 vector = activeHandHold.objectHeld.TransformPoint(activeHandHold.localPositionHeld);
		Vector3 position = activeHandHold.grabber.transform.position;
		secondLastPreHandholdVelocity = lastPreHandholdVelocity;
		lastPreHandholdVelocity = playerRigidBody.linearVelocity;
		wasHoldingHandhold = true;
		if (isHandHoldMoving)
		{
			lastPreHandholdVelocity = movingHandHoldReleaseVelocity;
			playerRigidBody.linearVelocity = Vector3.zero;
			Vector3 vector2 = vector - position;
			playerRigidBody.transform.position += vector2;
			movingHandHoldReleaseVelocity = vector2 / timeDelta;
			Quaternion rotationDelta = activeHandHold.objectHeld.rotation * Quaternion.Inverse(lastHandHoldRotation);
			RotateWithSurface(rotationDelta, vector);
			lastHandHoldRotation = activeHandHold.objectHeld.rotation;
		}
		else
		{
			playerRigidBody.linearVelocity = (vector - position) / timeDelta;
			if (activeHandHold.applyRotation)
			{
				turnParent.transform.RotateAround(vector, base.transform.up, activeHandHold.localRotationalOffset - (activeHandHold.grabber.transform.rotation.eulerAngles.y - activeHandHold.objectHeld.rotation.eulerAngles.y));
			}
		}
	}
}
