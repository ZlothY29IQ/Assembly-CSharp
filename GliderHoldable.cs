using System;
using System.Collections;
using System.Runtime.InteropServices;
using AA;
using Fusion;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaTagScripts;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
[NetworkBehaviourWeaved(11)]
public class GliderHoldable : NetworkHoldableObject, IRequestableOwnershipGuardCallbacks
{
	private enum GliderState
	{
		LocallyHeld,
		LocallyDropped,
		RemoteSyncing
	}

	private class HoldingHand
	{
		public bool active;

		public Transform transform;

		public Vector3 holdLocalPos;

		public Vector3 handleLocalPos;

		public Quaternion localHoldRotation;

		public void Activate(Transform handTransform, Transform gliderTransform, Vector3 worldGrabPoint)
		{
			active = true;
			transform = handTransform.transform;
			holdLocalPos = handTransform.InverseTransformPoint(worldGrabPoint);
			handleLocalPos = gliderTransform.InverseTransformVector(gliderTransform.position - worldGrabPoint);
			localHoldRotation = Quaternion.Inverse(handTransform.rotation) * gliderTransform.rotation;
		}

		public void Deactivate()
		{
			active = false;
			transform = null;
			holdLocalPos = Vector3.zero;
			handleLocalPos = Vector3.zero;
			localHoldRotation = Quaternion.identity;
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 44)]
	[NetworkStructWeaved(11)]
	internal struct SyncedState : INetworkStruct
	{
		[FieldOffset(0)]
		public int riderId;

		[FieldOffset(4)]
		public byte materialIndex;

		[FieldOffset(8)]
		public byte audioLevel;

		[FieldOffset(12)]
		public NetworkBool tagged;

		[FieldOffset(16)]
		public Vector3 position;

		[FieldOffset(28)]
		public Quaternion rotation;

		public void Init(Vector3 defaultPosition, Quaternion defaultRotation)
		{
			riderId = -1;
			materialIndex = 0;
			audioLevel = 0;
			position = defaultPosition;
			rotation = defaultRotation;
		}

		public SyncedState(int id = -1)
		{
			riderId = id;
			materialIndex = 0;
			audioLevel = 0;
			tagged = default(NetworkBool);
			position = default(Vector3);
			rotation = default(Quaternion);
		}
	}

	[Serializable]
	private struct CosmeticMaterialOverride
	{
		public string cosmeticName;

		public Material material;
	}

	[Header("Flight Settings")]
	[SerializeField]
	private Vector2 pitchMinMax = new Vector2(-80f, 80f);

	[SerializeField]
	private Vector2 rollMinMax = new Vector2(-70f, 70f);

	[SerializeField]
	private float pitchHalfLife = 0.2f;

	public Vector2 pitchVelocityTargetMinMax = new Vector2(-60f, 60f);

	public Vector2 pitchVelocityRampTimeMinMax = new Vector2(-1f, 1f);

	[SerializeField]
	private float pitchVelocityFollowRateAngle = 60f;

	[SerializeField]
	private float pitchVelocityFollowRateMagnitude = 5f;

	[SerializeField]
	private AnimationCurve liftVsAttack = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve dragVsAttack = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	[Range(0f, 1f)]
	public float attackDragFactor = 0.1f;

	[SerializeField]
	private AnimationCurve dragVsSpeed = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	public float dragVsSpeedMaxSpeed = 30f;

	[SerializeField]
	[Range(0f, 1f)]
	public float dragVsSpeedDragFactor = 0.2f;

	[SerializeField]
	private AnimationCurve liftIncreaseVsRoll = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float liftIncreaseVsRollMaxAngle = 20f;

	[SerializeField]
	[Range(0f, 1f)]
	private float gravityCompensation = 0.8f;

	[Range(0f, 1f)]
	public float pullUpLiftBonus = 0.1f;

	public float pullUpLiftActivationVelocity = 1f;

	public float pullUpLiftActivationAcceleration = 3f;

	[Header("Body Positioning Control")]
	[SerializeField]
	private float riderPosDirectPitchMax = 70f;

	[SerializeField]
	private Vector2 riderPosRange = new Vector2(2.2f, 0.75f);

	[SerializeField]
	private float riderPosRangeOffset = 0.15f;

	[SerializeField]
	private Vector2 riderPosRangeNormalizedDeadzone = new Vector2(0.15f, 0.05f);

	[Header("Direct Handle Control")]
	[SerializeField]
	private float oneHandHoldRotationRate = 2f;

	private Vector3 oneHandSimulatedHoldOffset = new Vector3(0.5f, -0.35f, 0.25f);

	private float oneHandPitchMultiplier = 0.8f;

	[SerializeField]
	private float twoHandHoldRotationRate = 4f;

	[SerializeField]
	private bool twoHandGliderInversionOnYawInsteadOfRoll;

	[Header("Player Settings")]
	[SerializeField]
	private bool setMaxHandSlipDuringFlight = true;

	[SerializeField]
	private float maxSlipOverrideSpeedThreshold = 5f;

	[Header("Player Camera Rotation")]
	[SerializeField]
	private float subtlePlayerPitchFactor = 0.2f;

	[SerializeField]
	private float subtlePlayerPitchRate = 2f;

	[SerializeField]
	private float subtlePlayerRollFactor = 0.2f;

	[SerializeField]
	private float subtlePlayerRollRate = 2f;

	[SerializeField]
	private Vector2 subtlePlayerRotationSpeedRampMinMax = new Vector2(2f, 8f);

	[SerializeField]
	private Vector2 subtlePlayerRollAccelMinMax = new Vector2(0f, 30f);

	[SerializeField]
	private Vector2 subtlePlayerPitchAccelMinMax = new Vector2(0f, 10f);

	[SerializeField]
	private float accelSmoothingFollowRate = 2f;

	[Header("Haptics")]
	[SerializeField]
	private Vector2 hapticAccelInputRange = new Vector2(5f, 20f);

	[SerializeField]
	private float hapticAccelOutputMax = 0.35f;

	[SerializeField]
	private Vector2 hapticMaxSpeedInputRange = new Vector2(5f, 10f);

	[SerializeField]
	private Vector2 hapticSpeedInputRange = new Vector2(3f, 30f);

	[SerializeField]
	private float hapticSpeedOutputMax = 0.15f;

	[SerializeField]
	private Vector2 whistlingAudioSpeedInputRange = new Vector2(15f, 30f);

	[Header("Audio")]
	[SerializeField]
	private float audioVolumeMultiplier = 0.25f;

	[SerializeField]
	private float infectedAudioVolumeMultiplier = 0.5f;

	[SerializeField]
	private Vector2 whooshSpeedThresholdInput = new Vector2(10f, 25f);

	[SerializeField]
	private Vector2 whooshVolumeOutput = new Vector2(0.2f, 0.75f);

	[SerializeField]
	private float whooshCheckDistance = 2f;

	[Header("Tag Adjustment")]
	[SerializeField]
	private bool extendTagRangeInFlight = true;

	[SerializeField]
	private Vector2 tagRangeSpeedInput = new Vector2(5f, 20f);

	[SerializeField]
	private Vector2 tagRangeOutput = new Vector2(0.03f, 3f);

	[SerializeField]
	private bool debugDrawTagRange = true;

	[Header("Infected State")]
	[SerializeField]
	private float infectedSpeedIncrease = 5f;

	[Header("Glider Materials")]
	[SerializeField]
	private MeshRenderer leafMesh;

	[SerializeField]
	private Material baseLeafMaterial;

	[SerializeField]
	private Material infectedLeafMaterial;

	[SerializeField]
	private Material frozenLeafMaterial;

	[SerializeField]
	private CosmeticMaterialOverride[] cosmeticMaterialOverrides;

	[Header("Network Syncing")]
	[SerializeField]
	private float networkSyncFollowRate = 2f;

	[Header("Life Cycle")]
	[SerializeField]
	private Transform maxDistanceRespawnOrigin;

	[SerializeField]
	private float maxDistanceBeforeRespawn = 180f;

	[SerializeField]
	private float maxDroppedTimeToRespawn = 120f;

	[Header("Rigidbody")]
	[SerializeField]
	private float windUprightTorqueMultiplier = 1f;

	[SerializeField]
	private float gravityUprightTorqueMultiplier = 0.5f;

	[SerializeField]
	private float fallingGravityReduction = 0.1f;

	[Header("References")]
	[SerializeField]
	private AudioSource calmAudio;

	[SerializeField]
	private AudioSource activeAudio;

	[SerializeField]
	private AudioSource whistlingAudio;

	[SerializeField]
	private AudioSource leftWhooshAudio;

	[SerializeField]
	private AudioSource rightWhooshAudio;

	[SerializeField]
	private InteractionPoint handle;

	[SerializeField]
	private RequestableOwnershipGuard ownershipGuard;

	private bool subtlePlayerPitchActive = true;

	private bool subtlePlayerRollActive = true;

	private float subtlePlayerPitch;

	private float subtlePlayerRoll;

	private float subtlePlayerPitchRateExp = 0.75f;

	private float subtlePlayerRollRateExp = 0.025f;

	private float defaultMaxDistanceBeforeRespawn = 180f;

	private HoldingHand leftHold = new HoldingHand();

	private HoldingHand rightHold = new HoldingHand();

	private SyncedState syncedState;

	private Vector3 twoHandRotationOffsetAxis = Vector3.forward;

	private float twoHandRotationOffsetAngle;

	private Rigidbody rb;

	private Vector2 riderPosition = Vector2.zero;

	private Vector3 previousVelocity;

	private Vector3 currentVelocity;

	private float pitch;

	private float yaw;

	private float roll;

	private float pitchVel;

	private float yawVel;

	private float rollVel;

	private float oneHandRotationRateExp;

	private float twoHandRotationRateExp;

	private Quaternion playerFacingRotationOffset = Quaternion.identity;

	private const float accelAveragingWindow = 0.1f;

	private AverageVector3 accelerationAverage = new AverageVector3();

	private float accelerationSmoothed;

	private float turnAccelerationSmoothed;

	private float accelSmoothingFollowRateExp = 1f;

	private float networkSyncFollowRateExp = 2f;

	private bool pendingOwnershipRequest;

	private Vector3 positionLocalToVRRig = Vector3.zero;

	private Quaternion rotationLocalToVRRig = Quaternion.identity;

	private Coroutine reenableOwnershipRequestCoroutine;

	private Vector3 spawnPosition;

	private Quaternion spawnRotation;

	private Vector3 skyJungleSpawnPostion;

	private Quaternion skyJungleSpawnRotation;

	private Transform skyJungleRespawnOrigin;

	private float lastHeldTime = -1f;

	private Vector3? leftHoldPositionLocal;

	private Vector3? rightHoldPositionLocal;

	private float whooshSoundDuration = 1f;

	private float whooshSoundRetriggerThreshold = 0.5f;

	private float leftWhooshStartTime = -1f;

	private Vector3 leftWhooshHitPoint = Vector3.zero;

	private Vector3 whooshAudioPositionOffset = new Vector3(0.5f, -0.25f, 0.5f);

	private float rightWhooshStartTime = -1f;

	private Vector3 rightWhooshHitPoint = Vector3.zero;

	private int ridersMaterialOverideIndex;

	private int windVolumeForceAppliedFrame = -1;

	private bool holdingTwoGliders;

	private GliderState gliderState;

	private float audioLevel;

	private int riderId = -1;

	[SerializeField]
	private VRRig cachedRig;

	private bool infectedState;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 11)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private SyncedState _Data;

	private bool OutOfBounds
	{
		get
		{
			if (maxDistanceRespawnOrigin != null)
			{
				return (maxDistanceRespawnOrigin.position - base.transform.position).sqrMagnitude > maxDistanceBeforeRespawn * maxDistanceBeforeRespawn;
			}
			return false;
		}
	}

	public override bool TwoHanded => true;

	[Networked]
	[NetworkedWeaved(0, 11)]
	internal unsafe SyncedState Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing GliderHoldable.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(SyncedState*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing GliderHoldable.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(SyncedState*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		base.transform.parent = null;
		defaultMaxDistanceBeforeRespawn = maxDistanceBeforeRespawn;
		spawnPosition = (skyJungleSpawnPostion = base.transform.position);
		spawnRotation = (skyJungleSpawnRotation = base.transform.rotation);
		skyJungleRespawnOrigin = maxDistanceRespawnOrigin;
		syncedState.Init(spawnPosition, spawnRotation);
		rb = GetComponent<Rigidbody>();
		yaw = base.transform.rotation.eulerAngles.y;
		oneHandRotationRateExp = Mathf.Exp(oneHandHoldRotationRate);
		twoHandRotationRateExp = Mathf.Exp(twoHandHoldRotationRate);
		subtlePlayerPitchRateExp = Mathf.Exp(subtlePlayerPitchRate);
		subtlePlayerRollRateExp = Mathf.Exp(subtlePlayerRollRate);
		accelSmoothingFollowRateExp = Mathf.Exp(accelSmoothingFollowRate);
		networkSyncFollowRateExp = Mathf.Exp(networkSyncFollowRate);
		ownershipGuard.AddCallbackTarget(this);
		calmAudio.volume = 0f;
		activeAudio.volume = 0f;
		whistlingAudio.volume = 0f;
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		if (ownershipGuard != null)
		{
			ownershipGuard.RemoveCallbackTarget(this);
		}
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		Respawn();
		base.OnDisable();
	}

	public void Respawn()
	{
		if ((!base.IsValid || !base.IsMine) && NetworkSystem.Instance.InRoom)
		{
			return;
		}
		if (EquipmentInteractor.instance != null)
		{
			if (EquipmentInteractor.instance.leftHandHeldEquipment == this)
			{
				OnRelease(null, EquipmentInteractor.instance.leftHand);
			}
			if (EquipmentInteractor.instance.rightHandHeldEquipment == this)
			{
				OnRelease(null, EquipmentInteractor.instance.rightHand);
			}
		}
		rb.isKinematic = true;
		base.transform.position = spawnPosition;
		base.transform.rotation = spawnRotation;
		lastHeldTime = -1f;
		syncedState.Init(spawnPosition, spawnRotation);
	}

	public void CustomMapLoad(Transform placeholderTransform, float respawnDistance)
	{
		maxDistanceRespawnOrigin = placeholderTransform;
		spawnPosition = placeholderTransform.position;
		spawnRotation = placeholderTransform.rotation;
		maxDistanceBeforeRespawn = respawnDistance;
		Respawn();
	}

	public void CustomMapUnload()
	{
		maxDistanceRespawnOrigin = skyJungleRespawnOrigin;
		spawnPosition = skyJungleSpawnPostion;
		spawnRotation = skyJungleSpawnRotation;
		maxDistanceBeforeRespawn = defaultMaxDistanceBeforeRespawn;
		Respawn();
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
		if (!base.IsMine && NetworkSystem.Instance.InRoom && !pendingOwnershipRequest && syncedState.riderId == -1)
		{
			ownershipGuard.RequestOwnershipImmediately(delegate
			{
				pendingOwnershipRequest = false;
			});
			pendingOwnershipRequest = true;
			if (reenableOwnershipRequestCoroutine != null)
			{
				StopCoroutine(reenableOwnershipRequestCoroutine);
			}
			reenableOwnershipRequestCoroutine = StartCoroutine(ReenableOwnershipRequest());
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (base.IsMine || !NetworkSystem.Instance.InRoom || pendingOwnershipRequest)
		{
			OnGrabAuthority(pointGrabbed, grabbingHand);
		}
		else if (NetworkSystem.Instance.InRoom && !base.IsMine && !pendingOwnershipRequest && syncedState.riderId == -1)
		{
			ownershipGuard.RequestOwnershipImmediately(delegate
			{
				pendingOwnershipRequest = false;
			});
			pendingOwnershipRequest = true;
			if (reenableOwnershipRequestCoroutine != null)
			{
				StopCoroutine(reenableOwnershipRequestCoroutine);
			}
			reenableOwnershipRequestCoroutine = StartCoroutine(ReenableOwnershipRequest());
			OnGrabAuthority(pointGrabbed, grabbingHand);
		}
	}

	public void OnGrabAuthority(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!base.IsMine && NetworkSystem.Instance.InRoom && !pendingOwnershipRequest)
		{
			return;
		}
		bool flag = grabbingHand == EquipmentInteractor.instance.leftHand;
		if ((flag && !EquipmentInteractor.instance.isLeftGrabbing) || (!flag && !EquipmentInteractor.instance.isRightGrabbing))
		{
			return;
		}
		if (riderId != NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			riderId = NetworkSystem.Instance.LocalPlayer.ActorNumber;
			cachedRig = getNewHolderRig(riderId);
		}
		EquipmentInteractor.instance.UpdateHandEquipment(this, flag);
		GorillaTagger.Instance.StartVibration(flag, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
		Vector3 worldGrabPoint = ClosestPointInHandle(grabbingHand.transform.position, pointGrabbed);
		if (flag)
		{
			leftHold.Activate(grabbingHand.transform, base.transform, worldGrabPoint);
		}
		else
		{
			rightHold.Activate(grabbingHand.transform, base.transform, worldGrabPoint);
		}
		if (leftHold.active && rightHold.active)
		{
			Vector3 handsVector = GetHandsVector(leftHold.transform.position, rightHold.transform.position, GTPlayer.Instance.headCollider.transform.position, flipBasedOnFacingDir: true);
			twoHandRotationOffsetAxis = Vector3.Cross(handsVector, base.transform.right).normalized;
			if ((double)twoHandRotationOffsetAxis.sqrMagnitude < 0.001)
			{
				twoHandRotationOffsetAxis = base.transform.right;
				twoHandRotationOffsetAngle = 0f;
			}
			else
			{
				twoHandRotationOffsetAngle = Vector3.SignedAngle(handsVector, base.transform.right, twoHandRotationOffsetAxis);
			}
		}
		rb.isKinematic = true;
		rb.useGravity = false;
		ridersMaterialOverideIndex = 0;
		if (cosmeticMaterialOverrides.Length != 0)
		{
			VRRig offlineVRRig = cachedRig;
			if (offlineVRRig == null)
			{
				offlineVRRig = GorillaTagger.Instance.offlineVRRig;
			}
			if (offlineVRRig != null)
			{
				for (int i = 0; i < cosmeticMaterialOverrides.Length; i++)
				{
					if (cosmeticMaterialOverrides[i].cosmeticName != null && offlineVRRig.cosmeticSet != null && offlineVRRig.cosmeticSet.HasItem(cosmeticMaterialOverrides[i].cosmeticName))
					{
						ridersMaterialOverideIndex = i + 1;
						break;
					}
				}
			}
		}
		infectedState = false;
		if (GorillaGameManager.instance as GorillaTagManager != null)
		{
			infectedState = syncedState.tagged;
		}
		if (infectedState)
		{
			leafMesh.material = GetInfectedMaterial();
		}
		else
		{
			leafMesh.material = GetMaterialFromIndex((byte)ridersMaterialOverideIndex);
		}
		if (EquipmentInteractor.instance.rightHandHeldEquipment != null && EquipmentInteractor.instance.rightHandHeldEquipment.GetType() == typeof(GliderHoldable) && EquipmentInteractor.instance.leftHandHeldEquipment != null && EquipmentInteractor.instance.leftHandHeldEquipment.GetType() == typeof(GliderHoldable) && EquipmentInteractor.instance.leftHandHeldEquipment != EquipmentInteractor.instance.rightHandHeldEquipment)
		{
			holdingTwoGliders = true;
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		holdingTwoGliders = false;
		bool flag = releasingHand == EquipmentInteractor.instance.leftHand;
		if (leftHold.active && rightHold.active)
		{
			if (flag)
			{
				rightHold.Activate(rightHold.transform, base.transform, ClosestPointInHandle(rightHold.transform.position, handle));
			}
			else
			{
				leftHold.Activate(leftHold.transform, base.transform, ClosestPointInHandle(leftHold.transform.position, handle));
			}
		}
		Vector3 averageVelocity = GTPlayer.Instance.GetHandVelocityTracker(flag).GetAverageVelocity(worldSpace: true);
		(flag ? leftHold : rightHold).Deactivate();
		EquipmentInteractor.instance.UpdateHandEquipment(null, flag);
		if (!leftHold.active && !rightHold.active)
		{
			gliderState = GliderState.LocallyDropped;
			audioLevel = 0f;
			riderId = -1;
			cachedRig = null;
			subtlePlayerPitch = 0f;
			subtlePlayerRoll = 0f;
			leftHoldPositionLocal = null;
			rightHoldPositionLocal = null;
			ridersMaterialOverideIndex = 0;
			if (base.IsMine || !NetworkSystem.Instance.InRoom)
			{
				rb.isKinematic = false;
				rb.useGravity = true;
				rb.linearVelocity = averageVelocity;
				syncedState.riderId = -1;
				syncedState.tagged = false;
				syncedState.materialIndex = 0;
				syncedState.position = base.transform.position;
				syncedState.rotation = base.transform.rotation;
				syncedState.audioLevel = 0;
			}
			leafMesh.material = baseLeafMaterial;
		}
		return true;
	}

	public override void DropItemCleanup()
	{
	}

	public void FixedUpdate()
	{
		if (!base.IsMine && NetworkSystem.Instance.InRoom && !pendingOwnershipRequest)
		{
			return;
		}
		GTPlayer instance = GTPlayer.Instance;
		if (holdingTwoGliders)
		{
			instance.AddForce(Physics.gravity, ForceMode.Acceleration);
		}
		else if (leftHold.active || rightHold.active)
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			previousVelocity = currentVelocity;
			currentVelocity = instance.RigidbodyVelocity;
			float magnitude = currentVelocity.magnitude;
			accelerationAverage.AddSample((currentVelocity - previousVelocity) / Time.fixedDeltaTime, Time.fixedTime);
			float rollAngle180Wrapping = GetRollAngle180Wrapping();
			float angle = liftIncreaseVsRoll.Evaluate(Mathf.Clamp01(Mathf.Abs(rollAngle180Wrapping / 180f))) * liftIncreaseVsRollMaxAngle;
			Vector3 vector = Vector3.RotateTowards(currentVelocity, Quaternion.AngleAxis(angle, -base.transform.right) * base.transform.forward * magnitude, pitchVelocityFollowRateAngle * (MathF.PI / 180f) * fixedDeltaTime, pitchVelocityFollowRateMagnitude * fixedDeltaTime);
			Vector3 vector2 = vector - currentVelocity;
			float num = NormalizeAngle180(Vector3.SignedAngle(Vector3.ProjectOnPlane(currentVelocity, base.transform.right), base.transform.forward, base.transform.right));
			if (num > 90f)
			{
				num = Mathf.Lerp(0f, 90f, Mathf.InverseLerp(180f, 90f, num));
			}
			else if (num < -90f)
			{
				num = Mathf.Lerp(0f, -90f, Mathf.InverseLerp(-180f, -90f, num));
			}
			float time = Mathf.Lerp(-1f, 1f, Mathf.InverseLerp(-90f, 90f, num));
			Mathf.Lerp(-1f, 1f, Mathf.InverseLerp(-90f, 90f, pitch));
			float num2 = liftVsAttack.Evaluate(time);
			instance.AddForce(vector2 * num2, ForceMode.VelocityChange);
			float num3 = dragVsAttack.Evaluate(time);
			float num4 = ((syncedState.riderId != -1 && syncedState.materialIndex == 1) ? (dragVsSpeedMaxSpeed + infectedSpeedIncrease) : dragVsSpeedMaxSpeed);
			float num5 = dragVsSpeed.Evaluate(Mathf.Clamp01(magnitude / num4));
			float num6 = Mathf.Clamp01(num3 * attackDragFactor + num5 * dragVsSpeedDragFactor);
			instance.AddForce(-currentVelocity * num6, ForceMode.Acceleration);
			if (pitch > 0f && currentVelocity.y > 0f && (currentVelocity - previousVelocity).y > 0f)
			{
				float a = Mathf.InverseLerp(0f, pullUpLiftActivationVelocity, currentVelocity.y);
				float b = Mathf.InverseLerp(0f, pullUpLiftActivationAcceleration, (currentVelocity - previousVelocity).y / fixedDeltaTime);
				float num7 = Mathf.Min(a, b);
				instance.AddForce(-Physics.gravity * pullUpLiftBonus * num7, ForceMode.Acceleration);
			}
			if (Vector3.Dot(vector, Physics.gravity) > 0f)
			{
				instance.AddForce(-Physics.gravity * gravityCompensation, ForceMode.Acceleration);
			}
		}
		else
		{
			Vector3 vector3 = WindResistanceForceOffset(base.transform.up, Vector3.down);
			Vector3 position = base.transform.position - vector3 * gravityUprightTorqueMultiplier;
			rb.AddForceAtPosition((0f - fallingGravityReduction) * Physics.gravity * rb.mass, position, ForceMode.Force);
		}
	}

	public void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		if (base.IsMine || !NetworkSystem.Instance.InRoom || pendingOwnershipRequest)
		{
			AuthorityUpdate(deltaTime);
		}
		else
		{
			RemoteSyncUpdate(deltaTime);
		}
	}

	private void AuthorityUpdate(float dt)
	{
		if (!leftHold.active && !rightHold.active)
		{
			AuthorityUpdateUnheld(dt);
		}
		else if (leftHold.active || rightHold.active)
		{
			AuthorityUpdateHeld(dt);
		}
		syncedState.audioLevel = (byte)Mathf.FloorToInt(255f * audioLevel);
	}

	private void AuthorityUpdateHeld(float dt)
	{
		if (gliderState != GliderState.LocallyHeld)
		{
			gliderState = GliderState.LocallyHeld;
		}
		rb.isKinematic = true;
		lastHeldTime = Time.time;
		if (leftHold.active)
		{
			leftHold.holdLocalPos = Vector3.Lerp(Vector3.zero, leftHold.holdLocalPos, Mathf.Exp(-5f * dt));
		}
		if (rightHold.active)
		{
			rightHold.holdLocalPos = Vector3.Lerp(Vector3.zero, rightHold.holdLocalPos, Mathf.Exp(-5f * dt));
		}
		Vector3 vector = Vector3.zero;
		if (leftHold.active && rightHold.active)
		{
			vector = (leftHold.transform.TransformPoint(leftHold.holdLocalPos) + rightHold.transform.TransformPoint(rightHold.holdLocalPos)) * 0.5f;
		}
		else if (leftHold.active)
		{
			vector = leftHold.transform.TransformPoint(leftHold.holdLocalPos);
		}
		else if (rightHold.active)
		{
			vector = rightHold.transform.TransformPoint(rightHold.holdLocalPos);
		}
		UpdateGliderPosition();
		float magnitude = currentVelocity.magnitude;
		if (setMaxHandSlipDuringFlight && magnitude > maxSlipOverrideSpeedThreshold)
		{
			if (leftHold.active)
			{
				GTPlayer.Instance.SetLeftMaximumSlipThisFrame();
			}
			if (rightHold.active)
			{
				GTPlayer.Instance.SetRightMaximumSlipThisFrame();
			}
		}
		bool flag = false;
		GorillaTagManager gorillaTagManager = GorillaGameManager.instance as GorillaTagManager;
		if (gorillaTagManager != null)
		{
			flag = gorillaTagManager.IsInfected(NetworkSystem.Instance.LocalPlayer);
		}
		bool num = flag != infectedState;
		infectedState = flag;
		if (num)
		{
			if (infectedState)
			{
				leafMesh.material = GetInfectedMaterial();
			}
			else
			{
				leafMesh.material = GetMaterialFromIndex(syncedState.materialIndex);
			}
		}
		Vector3 average = accelerationAverage.GetAverage();
		accelerationSmoothed = Mathf.Lerp(average.magnitude, accelerationSmoothed, Mathf.Exp((0f - accelSmoothingFollowRateExp) * dt));
		float num2 = Mathf.InverseLerp(hapticMaxSpeedInputRange.x, hapticMaxSpeedInputRange.y, magnitude);
		float num3 = Mathf.InverseLerp(hapticAccelInputRange.x, hapticAccelInputRange.y, accelerationSmoothed);
		float num4 = Mathf.InverseLerp(hapticSpeedInputRange.x, hapticSpeedInputRange.y, magnitude);
		UpdateAudioSource(calmAudio, num2 * audioVolumeMultiplier);
		UpdateAudioSource(activeAudio, num3 * num2 * audioVolumeMultiplier);
		if (infectedState)
		{
			UpdateAudioSource(whistlingAudio, Mathf.InverseLerp(whistlingAudioSpeedInputRange.x, whistlingAudioSpeedInputRange.y, magnitude) * num3 * num2 * audioVolumeMultiplier);
		}
		else
		{
			UpdateAudioSource(whistlingAudio, 0f);
		}
		float amplitude = Mathf.Max(num3 * hapticAccelOutputMax * num2, num4 * hapticSpeedOutputMax);
		if (rightHold.active)
		{
			GorillaTagger.Instance.DoVibration(XRNode.RightHand, amplitude, dt);
		}
		if (leftHold.active)
		{
			GorillaTagger.Instance.DoVibration(XRNode.LeftHand, amplitude, dt);
		}
		Vector3 origin = handle.transform.position + handle.transform.rotation * new Vector3(0f, 0f, 1f);
		if (Time.frameCount % 2 == 0)
		{
			Vector3 direction = handle.transform.rotation * new Vector3(-0.707f, 0f, 0.707f);
			if (leftWhooshStartTime < Time.time - whooshSoundRetriggerThreshold && magnitude > whooshSpeedThresholdInput.x && Physics.Raycast(new Ray(origin, direction), out var hitInfo, whooshCheckDistance, GTPlayer.Instance.locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
			{
				leftWhooshStartTime = Time.time;
				leftWhooshHitPoint = hitInfo.point;
				leftWhooshAudio.GTStop();
				leftWhooshAudio.volume = Mathf.Lerp(whooshVolumeOutput.x, whooshVolumeOutput.y, Mathf.InverseLerp(whooshSpeedThresholdInput.x, whooshSpeedThresholdInput.y, magnitude));
				leftWhooshAudio.GTPlay();
			}
		}
		else
		{
			Vector3 direction2 = handle.transform.rotation * new Vector3(0.707f, 0f, 0.707f);
			if (rightWhooshStartTime < Time.time - whooshSoundRetriggerThreshold && magnitude > whooshSpeedThresholdInput.x && Physics.Raycast(new Ray(origin, direction2), out var hitInfo2, whooshCheckDistance, GTPlayer.Instance.locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
			{
				rightWhooshStartTime = Time.time;
				rightWhooshHitPoint = hitInfo2.point;
				rightWhooshAudio.GTStop();
				rightWhooshAudio.volume = Mathf.Lerp(whooshVolumeOutput.x, whooshVolumeOutput.y, Mathf.InverseLerp(whooshSpeedThresholdInput.x, whooshSpeedThresholdInput.y, magnitude));
				rightWhooshAudio.GTPlay();
			}
		}
		Vector3 headCenterPosition = GTPlayer.Instance.HeadCenterPosition;
		if (leftWhooshStartTime > Time.time - whooshSoundDuration)
		{
			leftWhooshAudio.transform.position = leftWhooshHitPoint;
		}
		else
		{
			leftWhooshAudio.transform.localPosition = new Vector3(0f - whooshAudioPositionOffset.x, whooshAudioPositionOffset.y, whooshAudioPositionOffset.z);
		}
		if (rightWhooshStartTime > Time.time - whooshSoundDuration)
		{
			rightWhooshAudio.transform.position = rightWhooshHitPoint;
		}
		else
		{
			rightWhooshAudio.transform.localPosition = new Vector3(whooshAudioPositionOffset.x, whooshAudioPositionOffset.y, whooshAudioPositionOffset.z);
		}
		if (extendTagRangeInFlight)
		{
			float tagRadiusOverrideThisFrame = Mathf.Lerp(tagRangeOutput.x, tagRangeOutput.y, Mathf.InverseLerp(tagRangeSpeedInput.x, tagRangeSpeedInput.y, magnitude));
			GorillaTagger.Instance.SetTagRadiusOverrideThisFrame(tagRadiusOverrideThisFrame);
			if (debugDrawTagRange)
			{
				GorillaTagger.Instance.DebugDrawTagCasts(Color.yellow);
			}
		}
		Vector3 normalized = Vector3.ProjectOnPlane(base.transform.right, Vector3.up).normalized;
		Vector3 normalized2 = Vector3.ProjectOnPlane(base.transform.forward, Vector3.up).normalized;
		float num5 = 0f - Vector3.Dot(vector - handle.transform.position, normalized2);
		Vector3 vector2 = handle.transform.position - normalized2 * (riderPosRange.y * 0.5f + riderPosRangeOffset + num5);
		float num6 = Vector3.Dot(headCenterPosition - vector2, normalized);
		float num7 = Vector3.Dot(headCenterPosition - vector2, normalized2);
		num6 /= riderPosRange.x * 0.5f;
		num7 /= riderPosRange.y * 0.5f;
		riderPosition.x = Mathf.Sign(num6) * Mathf.Lerp(0f, 1f, Mathf.InverseLerp(riderPosRangeNormalizedDeadzone.x, 1f, Mathf.Abs(num6)));
		riderPosition.y = Mathf.Sign(num7) * Mathf.Lerp(0f, 1f, Mathf.InverseLerp(riderPosRangeNormalizedDeadzone.y, 1f, Mathf.Abs(num7)));
		Vector3 vector3;
		Vector3 vector4;
		if (leftHold.active && rightHold.active)
		{
			vector3 = leftHold.transform.position;
			leftHoldPositionLocal = GTPlayer.Instance.transform.InverseTransformPoint(vector3);
			vector4 = rightHold.transform.position;
			rightHoldPositionLocal = GTPlayer.Instance.transform.InverseTransformPoint(vector4);
		}
		else if (leftHold.active)
		{
			vector3 = leftHold.transform.position;
			leftHoldPositionLocal = GTPlayer.Instance.transform.InverseTransformPoint(vector3);
			Vector3 vector5 = vector3 + leftHold.transform.forward * oneHandSimulatedHoldOffset.x;
			if (rightHoldPositionLocal.HasValue)
			{
				rightHoldPositionLocal = Vector3.Lerp(GTPlayer.Instance.transform.InverseTransformPoint(vector5), rightHoldPositionLocal.Value, Mathf.Exp(-5f * dt));
				vector4 = GTPlayer.Instance.transform.TransformPoint(rightHoldPositionLocal.Value);
			}
			else
			{
				vector4 = vector5;
				rightHoldPositionLocal = GTPlayer.Instance.transform.InverseTransformPoint(vector4);
			}
		}
		else
		{
			vector4 = rightHold.transform.position;
			rightHoldPositionLocal = GTPlayer.Instance.transform.InverseTransformPoint(vector4);
			Vector3 vector6 = vector4 + rightHold.transform.forward * oneHandSimulatedHoldOffset.x;
			if (leftHoldPositionLocal.HasValue)
			{
				leftHoldPositionLocal = Vector3.Lerp(GTPlayer.Instance.transform.InverseTransformPoint(vector6), leftHoldPositionLocal.Value, Mathf.Exp(-5f * dt));
				vector3 = GTPlayer.Instance.transform.TransformPoint(leftHoldPositionLocal.Value);
			}
			else
			{
				vector3 = vector6;
				leftHoldPositionLocal = GTPlayer.Instance.transform.InverseTransformPoint(vector3);
			}
		}
		GetHandsOrientationVectors(vector3, vector4, GTPlayer.Instance.headCollider.transform, flipBasedOnFacingDir: false, out var handsVector, out var handsUpVector);
		float num8 = riderPosition.y * riderPosDirectPitchMax;
		if (!leftHold.active || !rightHold.active)
		{
			num8 *= oneHandPitchMultiplier;
		}
		Spring.CriticalSpringDamperExact(ref pitch, ref pitchVel, num8, 0f, pitchHalfLife, dt);
		pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
		Quaternion quaternion = Quaternion.AngleAxis(pitch, Vector3.right);
		twoHandRotationOffsetAngle = Mathf.Lerp(0f, twoHandRotationOffsetAngle, Mathf.Exp(-8f * dt));
		Vector3 upwards = (twoHandGliderInversionOnYawInsteadOfRoll ? handsUpVector : Vector3.up);
		Quaternion quaternion2 = Quaternion.AngleAxis(twoHandRotationOffsetAngle, twoHandRotationOffsetAxis) * Quaternion.LookRotation(handsVector, upwards) * Quaternion.AngleAxis(-90f, Vector3.up);
		float num9 = ((leftHold.active && rightHold.active) ? twoHandRotationRateExp : oneHandRotationRateExp);
		base.transform.rotation = Quaternion.Slerp(quaternion2 * quaternion, base.transform.rotation, Mathf.Exp((0f - num9) * dt));
		if (subtlePlayerPitchActive || subtlePlayerRollActive)
		{
			float a = Mathf.InverseLerp(subtlePlayerRotationSpeedRampMinMax.x, subtlePlayerRotationSpeedRampMinMax.y, currentVelocity.magnitude);
			Quaternion quaternion3 = Quaternion.identity;
			if (subtlePlayerRollActive)
			{
				float num10 = GetRollAngle180Wrapping();
				if (num10 > 90f)
				{
					num10 = Mathf.Lerp(0f, 90f, Mathf.InverseLerp(180f, 90f, num10));
				}
				else if (num10 < -90f)
				{
					num10 = Mathf.Lerp(0f, -90f, Mathf.InverseLerp(-180f, -90f, num10));
				}
				Vector3 normalized3 = new Vector3(currentVelocity.x, 0f, currentVelocity.z).normalized;
				Vector3 vector7 = new Vector3(average.x, 0f, average.z);
				float num11 = Vector3.Dot(vector7 - Vector3.Dot(vector7, normalized3) * normalized3, Vector3.Cross(normalized3, Vector3.up));
				turnAccelerationSmoothed = Mathf.Lerp(num11, turnAccelerationSmoothed, Mathf.Exp((0f - accelSmoothingFollowRateExp) * dt));
				float b = 0f;
				if (num11 * num10 > 0f)
				{
					b = Mathf.InverseLerp(subtlePlayerRollAccelMinMax.x, subtlePlayerRollAccelMinMax.y, Mathf.Abs(turnAccelerationSmoothed));
				}
				float a2 = num10 * subtlePlayerRollFactor * Mathf.Min(a, b);
				subtlePlayerRoll = Mathf.Lerp(a2, subtlePlayerRoll, Mathf.Exp((0f - subtlePlayerRollRateExp) * dt));
				quaternion3 = Quaternion.AngleAxis(subtlePlayerRoll, base.transform.forward);
			}
			Quaternion quaternion4 = Quaternion.identity;
			if (subtlePlayerPitchActive)
			{
				float a3 = pitch * subtlePlayerPitchFactor * Mathf.Min(a, 1f);
				subtlePlayerPitch = Mathf.Lerp(a3, subtlePlayerPitch, Mathf.Exp((0f - subtlePlayerPitchRateExp) * dt));
				quaternion4 = Quaternion.AngleAxis(subtlePlayerPitch, -base.transform.right);
			}
			GTPlayerTransform.ApplyRotationOverride(quaternion4 * quaternion3, Time.frameCount);
		}
		UpdateGliderPosition();
		if (syncedState.riderId != NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			riderId = (syncedState.riderId = NetworkSystem.Instance.LocalPlayer.ActorNumber);
			cachedRig = getNewHolderRig(riderId);
		}
		syncedState.tagged = infectedState;
		syncedState.materialIndex = (byte)ridersMaterialOverideIndex;
		if (cachedRig != null)
		{
			syncedState.position = cachedRig.transform.InverseTransformPoint(base.transform.position);
			syncedState.rotation = Quaternion.Inverse(cachedRig.transform.rotation) * base.transform.rotation;
		}
		else
		{
			Debug.LogError("Glider failed to get a reference to the local player's VRRig while the player was flying", this);
		}
		audioLevel = num3 * num2;
		if (OutOfBounds)
		{
			Respawn();
		}
		if (leftHold.active && EquipmentInteractor.instance.leftHandHeldEquipment != this)
		{
			OnRelease(null, EquipmentInteractor.instance.leftHand);
		}
		if (rightHold.active && EquipmentInteractor.instance.rightHandHeldEquipment != this)
		{
			OnRelease(null, EquipmentInteractor.instance.rightHand);
		}
	}

	private void AuthorityUpdateUnheld(float dt)
	{
		syncedState.position = base.transform.position;
		syncedState.rotation = base.transform.rotation;
		if (gliderState != GliderState.LocallyDropped)
		{
			gliderState = GliderState.LocallyDropped;
			syncedState.riderId = -1;
			syncedState.materialIndex = 0;
			syncedState.tagged = false;
			leafMesh.material = baseLeafMaterial;
		}
		if (audioLevel * audioVolumeMultiplier > 0.001f)
		{
			audioLevel = Mathf.Lerp(0f, audioLevel, Mathf.Exp(-2f * dt));
			UpdateAudioSource(calmAudio, audioLevel * audioVolumeMultiplier);
			UpdateAudioSource(activeAudio, audioLevel * audioVolumeMultiplier);
			UpdateAudioSource(whistlingAudio, audioLevel * audioVolumeMultiplier);
		}
		if (OutOfBounds || (lastHeldTime > 0f && lastHeldTime < Time.time - maxDroppedTimeToRespawn))
		{
			Respawn();
		}
	}

	private void RemoteSyncUpdate(float dt)
	{
		rb.isKinematic = true;
		int num = syncedState.riderId;
		bool flag = riderId != num;
		if (flag)
		{
			riderId = num;
			cachedRig = getNewHolderRig(riderId);
		}
		if (riderId == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			cachedRig = null;
			syncedState.riderId = -1;
			syncedState.materialIndex = 0;
			syncedState.audioLevel = 0;
		}
		if (syncedState.riderId == -1)
		{
			base.transform.position = Vector3.Lerp(syncedState.position, base.transform.position, Mathf.Exp((0f - networkSyncFollowRateExp) * dt));
			base.transform.rotation = Quaternion.Slerp(syncedState.rotation, base.transform.rotation, Mathf.Exp((0f - networkSyncFollowRateExp) * dt));
		}
		else if (cachedRig != null)
		{
			positionLocalToVRRig = Vector3.Lerp(syncedState.position, positionLocalToVRRig, Mathf.Exp((0f - networkSyncFollowRateExp) * dt));
			rotationLocalToVRRig = Quaternion.Slerp(syncedState.rotation, rotationLocalToVRRig, Mathf.Exp((0f - networkSyncFollowRateExp) * dt));
			base.transform.position = cachedRig.transform.TransformPoint(positionLocalToVRRig);
			base.transform.rotation = cachedRig.transform.rotation * rotationLocalToVRRig;
		}
		bool flag2 = false;
		if (GorillaGameManager.instance as GorillaTagManager != null)
		{
			flag2 = syncedState.tagged;
		}
		bool num2 = flag2 != infectedState;
		infectedState = flag2;
		if (num2 || flag)
		{
			if (infectedState)
			{
				leafMesh.material = GetInfectedMaterial();
			}
			else
			{
				leafMesh.material = GetMaterialFromIndex(syncedState.materialIndex);
			}
		}
		float num3 = Mathf.Clamp01((float)(int)syncedState.audioLevel / 255f);
		if (audioLevel != num3)
		{
			audioLevel = num3;
			if (syncedState.riderId != -1 && (bool)syncedState.tagged)
			{
				UpdateAudioSource(calmAudio, audioLevel * infectedAudioVolumeMultiplier);
				UpdateAudioSource(activeAudio, audioLevel * infectedAudioVolumeMultiplier);
				UpdateAudioSource(whistlingAudio, audioLevel * infectedAudioVolumeMultiplier);
			}
			else
			{
				UpdateAudioSource(calmAudio, audioLevel * audioVolumeMultiplier);
				UpdateAudioSource(activeAudio, audioLevel * audioVolumeMultiplier);
				UpdateAudioSource(whistlingAudio, 0f);
			}
		}
	}

	private VRRig getNewHolderRig(int riderId)
	{
		if (riderId >= 0)
		{
			NetPlayer netPlayer = null;
			netPlayer = ((riderId != NetworkSystem.Instance.LocalPlayer.ActorNumber) ? NetworkSystem.Instance.GetPlayer(riderId) : NetworkSystem.Instance.LocalPlayer);
			if (netPlayer != null && VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig))
			{
				return playerRig.Rig;
			}
		}
		return null;
	}

	private Vector3 ClosestPointInHandle(Vector3 startingPoint, InteractionPoint interactionPoint)
	{
		CapsuleCollider component = interactionPoint.GetComponent<CapsuleCollider>();
		Vector3 vector = startingPoint;
		if (component != null)
		{
			Vector3 vector2 = ((component.direction == 0) ? Vector3.right : ((component.direction == 1) ? Vector3.up : Vector3.forward));
			Vector3 vector3 = component.transform.rotation * vector2;
			Vector3 vector4 = component.transform.position + component.transform.rotation * component.center;
			float num = Mathf.Clamp(Vector3.Dot(vector - vector4, vector3), (0f - component.height) * 0.5f, component.height * 0.5f);
			vector = vector4 + vector3 * num;
		}
		return vector;
	}

	private void UpdateGliderPosition()
	{
		if (leftHold.active && rightHold.active)
		{
			Vector3 vector = leftHold.transform.TransformPoint(leftHold.holdLocalPos) + base.transform.TransformVector(leftHold.handleLocalPos);
			Vector3 vector2 = rightHold.transform.TransformPoint(rightHold.holdLocalPos) + base.transform.TransformVector(rightHold.handleLocalPos);
			base.transform.position = (vector + vector2) * 0.5f;
		}
		else if (leftHold.active)
		{
			base.transform.position = leftHold.transform.TransformPoint(leftHold.holdLocalPos) + base.transform.TransformVector(leftHold.handleLocalPos);
		}
		else if (rightHold.active)
		{
			base.transform.position = rightHold.transform.TransformPoint(rightHold.holdLocalPos) + base.transform.TransformVector(rightHold.handleLocalPos);
		}
	}

	private Vector3 GetHandsVector(Vector3 leftHandPos, Vector3 rightHandPos, Vector3 headPos, bool flipBasedOnFacingDir)
	{
		Vector3 vector = rightHandPos - leftHandPos;
		Vector3 rhs = (rightHandPos + leftHandPos) * 0.5f - headPos;
		Vector3 normalized = Vector3.Cross(Vector3.up, rhs).normalized;
		if (flipBasedOnFacingDir && Vector3.Dot(vector, normalized) < 0f)
		{
			vector = -vector;
		}
		return vector;
	}

	private void GetHandsOrientationVectors(Vector3 leftHandPos, Vector3 rightHandPos, Transform head, bool flipBasedOnFacingDir, out Vector3 handsVector, out Vector3 handsUpVector)
	{
		handsVector = rightHandPos - leftHandPos;
		float magnitude = handsVector.magnitude;
		handsVector /= Mathf.Max(magnitude, 0.001f);
		Vector3 position = head.position;
		float num = 1f;
		Vector3 planeNormal = ((Vector3.Dot(head.right, handsVector) < 0f) ? handsVector : (-handsVector));
		Vector3 normalized = Vector3.ProjectOnPlane(-head.forward, planeNormal).normalized;
		Vector3 vector = normalized * num + position;
		Vector3 vector2 = (leftHandPos + rightHandPos) * 0.5f;
		Vector3 vector3 = Vector3.ProjectOnPlane(vector2 - head.position, Vector3.up);
		float magnitude2 = vector3.magnitude;
		vector3 /= Mathf.Max(magnitude2, 0.001f);
		Vector3 normalized2 = Vector3.ProjectOnPlane(-base.transform.forward, Vector3.up).normalized;
		Vector3 vector4 = -vector3 * num + position;
		float num2 = Vector3.Dot(normalized2, -vector3);
		float num3 = Vector3.Dot(normalized2, normalized);
		if (Vector3.Dot(base.transform.up, Vector3.up) < 0f)
		{
			num2 = Mathf.Abs(num2);
			num3 = Mathf.Abs(num3);
		}
		num2 = Mathf.Max(num2, 0f);
		num3 = Mathf.Max(num3, 0f);
		Vector3 vector5 = (vector4 * num2 + vector * num3) / Mathf.Max(num2 + num3, 0.001f);
		Vector3 vector6 = vector2 - vector5;
		Vector3 normalized3 = Vector3.Cross(Vector3.up, vector6).normalized;
		if (flipBasedOnFacingDir && Vector3.Dot(handsVector, normalized3) < 0f)
		{
			handsVector = -handsVector;
		}
		handsUpVector = Vector3.Cross(Vector3.ProjectOnPlane(vector6, Vector3.up), handsVector).normalized;
	}

	private Material GetMaterialFromIndex(byte materialIndex)
	{
		if (materialIndex < 1 || materialIndex > cosmeticMaterialOverrides.Length)
		{
			return baseLeafMaterial;
		}
		return cosmeticMaterialOverrides[materialIndex - 1].material;
	}

	private float GetRollAngle180Wrapping()
	{
		Vector3 normalized = Vector3.ProjectOnPlane(base.transform.forward, Vector3.up).normalized;
		float angle = Vector3.SignedAngle(Vector3.Cross(Vector3.up, normalized).normalized, base.transform.right, base.transform.forward);
		return NormalizeAngle180(angle);
	}

	private float SignedAngleInPlane(Vector3 from, Vector3 to, Vector3 normal)
	{
		from = Vector3.ProjectOnPlane(from, normal);
		to = Vector3.ProjectOnPlane(to, normal);
		return Vector3.SignedAngle(from, to, normal);
	}

	private float NormalizeAngle180(float angle)
	{
		angle = (angle + 180f) % 360f;
		if (angle < 0f)
		{
			angle += 360f;
		}
		return angle - 180f;
	}

	private void UpdateAudioSource(AudioSource source, float level)
	{
		source.volume = level;
		if (!source.isPlaying && level > 0.01f)
		{
			source.GTPlay();
		}
		else if (source.isPlaying && level < 0.01f && syncedState.riderId == -1)
		{
			source.GTStop();
		}
	}

	private Material GetInfectedMaterial()
	{
		if (GorillaGameManager.instance is GorillaFreezeTagManager)
		{
			return frozenLeafMaterial;
		}
		return infectedLeafMaterial;
	}

	public void OnTriggerStay(Collider other)
	{
		GliderWindVolume component = other.GetComponent<GliderWindVolume>();
		if (!(component == null) && (base.IsMine || !NetworkSystem.Instance.InRoom || pendingOwnershipRequest) && Time.frameCount != windVolumeForceAppliedFrame)
		{
			if (leftHold.active || rightHold.active)
			{
				Vector3 accelFromVelocity = component.GetAccelFromVelocity(GTPlayer.Instance.RigidbodyVelocity);
				GTPlayer.Instance.AddForce(accelFromVelocity, ForceMode.Acceleration);
				windVolumeForceAppliedFrame = Time.frameCount;
			}
			else
			{
				Vector3 accelFromVelocity2 = component.GetAccelFromVelocity(rb.linearVelocity);
				Vector3 vector = WindResistanceForceOffset(base.transform.up, component.WindDirection);
				Vector3 position = base.transform.position + vector * windUprightTorqueMultiplier;
				rb.AddForceAtPosition(accelFromVelocity2 * rb.mass, position, ForceMode.Force);
				windVolumeForceAppliedFrame = Time.frameCount;
			}
		}
	}

	private Vector3 WindResistanceForceOffset(Vector3 upDir, Vector3 windDir)
	{
		if (Vector3.Dot(upDir, windDir) < 0f)
		{
			upDir *= -1f;
		}
		return Vector3.ProjectOnPlane(upDir - windDir, upDir);
	}

	public override void ReadDataFusion()
	{
		int num = syncedState.riderId;
		syncedState = Data;
		if (num != syncedState.riderId)
		{
			positionLocalToVRRig = syncedState.position;
			rotationLocalToVRRig = syncedState.rotation;
		}
	}

	public override void WriteDataFusion()
	{
		Data = syncedState;
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == ((PunNetPlayer)ownershipGuard.actualOwner)?.PlayerRef)
		{
			int num = syncedState.riderId;
			syncedState.riderId = (int)stream.ReceiveNext();
			syncedState.tagged = (bool)stream.ReceiveNext();
			syncedState.materialIndex = (byte)stream.ReceiveNext();
			syncedState.audioLevel = (byte)stream.ReceiveNext();
			syncedState.position.SetValueSafe((Vector3)stream.ReceiveNext());
			syncedState.rotation.SetValueSafe((Quaternion)stream.ReceiveNext());
			if (num != syncedState.riderId)
			{
				positionLocalToVRRig = syncedState.position;
				rotationLocalToVRRig = syncedState.rotation;
			}
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.Equals(ownershipGuard.actualOwner?.GetPlayerRef()))
		{
			stream.SendNext(syncedState.riderId);
			stream.SendNext((bool)syncedState.tagged);
			stream.SendNext(syncedState.materialIndex);
			stream.SendNext(syncedState.audioLevel);
			stream.SendNext(syncedState.position);
			stream.SendNext(syncedState.rotation);
		}
	}

	private IEnumerator ReenableOwnershipRequest()
	{
		yield return new WaitForSeconds(3f);
		pendingOwnershipRequest = false;
	}

	public void OnOwnershipTransferred(NetPlayer toPlayer, NetPlayer fromPlayer)
	{
		if (toPlayer == NetworkSystem.Instance.LocalPlayer)
		{
			pendingOwnershipRequest = false;
			if (!leftHold.active && !rightHold.active && (spawnPosition - base.transform.position).sqrMagnitude > 1f)
			{
				rb.isKinematic = false;
				rb.WakeUp();
				lastHeldTime = Time.time;
			}
		}
	}

	public bool OnOwnershipRequest(NetPlayer fromPlayer)
	{
		if (base.IsMine && NetworkSystem.Instance.InRoom && (leftHold.active || rightHold.active))
		{
			return false;
		}
		return true;
	}

	public void OnMyOwnerLeft()
	{
	}

	public bool OnMasterClientAssistedTakeoverRequest(NetPlayer fromPlayer, NetPlayer toPlayer)
	{
		return false;
	}

	public void OnMyCreatorLeft()
	{
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
}
