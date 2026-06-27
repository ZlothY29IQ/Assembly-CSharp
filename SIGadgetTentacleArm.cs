using System;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using UnityEngine;

public class SIGadgetTentacleArm : SIGadget, ICallBack, IEnergyGadget
{
	private class HeldPlayerCallback : ICallBack
	{
		private SIGadgetTentacleArm parent;

		private VRRig heldRig;

		private TakeMyHand_HandLink heldHandLink;

		public HeldPlayerCallback(SIGadgetTentacleArm parent)
		{
			this.parent = parent;
		}

		public void Register(VRRig heldPlayer, TakeMyHand_HandLink heldHandLink)
		{
			Unregister();
			heldRig = heldPlayer;
			this.heldHandLink = heldHandLink;
			heldPlayer.AddLateUpdateCallback(this);
		}

		public void Unregister()
		{
			if (heldRig != null)
			{
				heldRig.RemoveLateUpdateCallback(this);
			}
			heldRig = null;
		}

		public void CallBack()
		{
			parent.UpdateTentacleHoldingHandPos(heldHandLink);
		}
	}

	private const string preLog = "[SIGadgetWristJet]  ";

	private const string preErr = "[SIGadgetWristJet]  ERROR!!!  ";

	private const string preErrBeta = "[SIGadgetWristJet]  ERROR!!!  (beta only log)  ";

	[SerializeField]
	private GameObject claw;

	[SerializeField]
	private GameObject clawHoldingVisual;

	[SerializeField]
	private GameObject clawReleasedVisual;

	[SerializeField]
	private LayerMask worldCollisionLayers;

	[SerializeField]
	private Transform marker;

	[SerializeField]
	private float maxTentacleLength;

	[SerializeField]
	private float tentacleForwardAdjustment;

	[SerializeField]
	private MeshRenderer tentacleRenderer;

	[SerializeField]
	private Transform tentacleAnchor;

	[SerializeField]
	private MeshRenderer tentacleRenderer2;

	[SerializeField]
	private Transform tentacleAnchor2;

	[SerializeField]
	private SoundBankPlayer attachSound;

	[SerializeField]
	private SoundBankPlayer detachSound;

	[SerializeField]
	private SoundBankPlayer attachFailSound;

	[SerializeField]
	private SoundBankPlayer detachFailSound;

	[SerializeField]
	private SoundBankPlayer lowFuelSound;

	[SerializeField]
	private float hapticStrengthOnGrab = 0.5f;

	[SerializeField]
	private float hapticDurationOnGrab = 0.2f;

	[SerializeField]
	private float hapticStrengthOnRelease = 0.5f;

	[SerializeField]
	private float hapticDurationOnRelease = 0.2f;

	[SerializeField]
	private GameButtonActivatable buttonActivatable;

	[SerializeField]
	private float ClawMaxBlendSpeed = 10f;

	[SerializeField]
	private float ClawMaxRotBlendSpeed = 1000f;

	private MaterialPropertyBlock _gaugeMatPropBlock;

	[SerializeField]
	private GTRendererMatSlot[] m_gaugeMatSlots;

	private const float kFUEL_CAPACITY = 10f;

	private float fuelSize = 10f;

	private float currentFuel;

	public float FuelPerSecond_Holding = 1f;

	public float FuelCost_Wall_Multiplier = 2f;

	public float FuelCost_Slippery_Multiplier = 2f;

	public float FuelPerSecond_Recharging = 1f;

	public float FuelCost_Grab = 1f;

	public float FuelCost_JumpSpeed = 1f;

	public float MaxTentacleJumpSpeed = 8f;

	public float LengthFactor = 1.5f;

	public float MaxGrabAngle = 60f;

	public float WallAngle = 60f;

	public bool canHoldSlipperyWalls;

	private bool hasTentacle2;

	private Material tentacleMat;

	private Material tentacleMat2;

	private ShaderHashId tentacleStartDir_HASH = "_TentacleStartDir";

	private ShaderHashId tentacleEnd_HASH = "_TentacleEndPos";

	private ShaderHashId tentacleEndDir_HASH = "_TentacleEndDir";

	private ShaderHashId tentacleRingOrigin_HASH = "_TentacleRingOrigin";

	private bool isLeftHanded;

	private Vector3 knownSafePosition;

	private Vector3 clawHoldAdjustment;

	private Vector3 clawAnchorPosition;

	private Vector3 lastRequestedPlayerPosition;

	private Quaternion clawAnchorRotation;

	private bool isGripBroken;

	private bool hasGravityOverride;

	private bool isLowFuel;

	private bool hasFailedToGrab;

	private float _fps_holding_base;

	private float _fps_recharging_base;

	private float _grabCost_base;

	private float _jumpCost_base;

	private float _jumpSpeed_base;

	private float _grabAngle_base;

	private float _min_grab_dot;

	private float _wall_angle_dot;

	private float _current_grab_fps;

	private float _lowFuelThreshold;

	private HeldPlayerCallback heldPlayerCallback;

	private bool hasRigCallback;

	private VRRig rigForCallback;

	private Vector3 clawVisualPos;

	private Quaternion clawVisualRot;

	private bool wasGrabPressed;

	private const long HoldingLeftHand_Bit = 2305843009213693952L;

	private const long Anchored_Bit = 4611686018427387904L;

	private const long HoldingHand_Bit = long.MinValue;

	private int lastCallbackFrame;

	private int lastHeldCallbackFrame;

	public bool isAnchored { get; private set; }

	public bool isHoldingHand { get; private set; }

	public bool UsesEnergy => true;

	public bool IsFull => currentFuel >= fuelSize;

	private void Awake()
	{
		_fps_holding_base = FuelPerSecond_Holding;
		_fps_recharging_base = FuelPerSecond_Recharging;
		_grabCost_base = FuelCost_Grab;
		_jumpCost_base = FuelCost_JumpSpeed;
		_jumpSpeed_base = MaxTentacleJumpSpeed;
		_grabAngle_base = MaxGrabAngle;
		_wall_angle_dot = Mathf.Cos(MathF.PI / 180f * WallAngle);
		tentacleMat = new Material(tentacleRenderer.sharedMaterial);
		tentacleRenderer.sharedMaterial = tentacleMat;
		if (tentacleRenderer2 != null)
		{
			hasTentacle2 = true;
			tentacleMat2 = new Material(tentacleRenderer2.sharedMaterial);
			tentacleRenderer2.sharedMaterial = tentacleMat2;
		}
		_gaugeMatPropBlock = new MaterialPropertyBlock();
		if (m_gaugeMatSlots == null)
		{
			m_gaugeMatSlots = Array.Empty<GTRendererMatSlot>();
		}
		int num = 0;
		for (int i = 0; i < m_gaugeMatSlots.Length; i++)
		{
			if (m_gaugeMatSlots[i].TryInitialize())
			{
				m_gaugeMatSlots[num] = m_gaugeMatSlots[i];
				num++;
			}
		}
		if (num != m_gaugeMatSlots.Length)
		{
			Array.Resize(ref m_gaugeMatSlots, num);
		}
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(OnSnapped));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(OnReleased));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(OnUnsnapped));
		gameEntity.OnStateChanged += OnEntityStateChanged;
		heldPlayerCallback = new HeldPlayerCallback(this);
	}

	private void Start()
	{
		clawVisualPos = claw.transform.position;
		clawVisualRot = claw.transform.rotation;
		clawReleasedVisual.SetActive(value: false);
		CallBack();
	}

	private void OnDestroy()
	{
		if (hasRigCallback)
		{
			hasRigCallback = false;
			rigForCallback.RemoveLateUpdateCallback(this);
		}
		if (hasGravityOverride)
		{
			GTPlayer.Instance.UnsetGravityOverride(this);
			hasGravityOverride = false;
		}
		heldPlayerCallback.Unregister();
	}

	private void OnGrabbed()
	{
		isLeftHanded = gameEntity.heldByHandIndex == 0;
		if (GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			hasRigCallback = true;
			rigForCallback = out_gamePlayer.rig;
			rigForCallback.AddLateUpdateCallback(this);
		}
	}

	private void OnSnapped()
	{
		isLeftHanded = gameEntity.snappedJoint == SnapJointType.HandL;
		if (GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer))
		{
			hasRigCallback = true;
			rigForCallback = out_gamePlayer.rig;
			rigForCallback.AddLateUpdateCallback(this);
		}
	}

	private void OnReleased()
	{
		ClearClawAnchor();
		if (hasRigCallback)
		{
			hasRigCallback = false;
			rigForCallback.RemoveLateUpdateCallback(this);
		}
	}

	private void OnUnsnapped()
	{
		if (hasRigCallback)
		{
			hasRigCallback = false;
			rigForCallback.RemoveLateUpdateCallback(this);
		}
	}

	private bool CheckInput()
	{
		return buttonActivatable.CheckInput();
	}

	private Vector3 GetIdealClawPosition(VRRig rig)
	{
		Vector3 position = rig.bodyTransform.position;
		position.y += 0.05f;
		Vector3 position2 = base.transform.position;
		Vector3 vector = position2 - position;
		return position2 + vector * LengthFactor + base.transform.forward * tentacleForwardAdjustment;
	}

	protected override void OnUpdateAuthority(float dt)
	{
		bool flag = CheckInput();
		if (isGripBroken)
		{
			if (flag)
			{
				flag = false;
			}
			else
			{
				isGripBroken = false;
			}
		}
		Vector3 position = base.transform.position;
		Vector3 idealClawPosition = GetIdealClawPosition(VRRig.LocalRig);
		Quaternion rotation = base.transform.rotation;
		float num = 0.15f;
		bool flag2 = isLowFuel;
		if ((knownSafePosition - idealClawPosition).IsLongerThan(1f))
		{
			Vector3 position2 = GTPlayer.Instance.headCollider.transform.position;
			Ray ray = new Ray(position2, idealClawPosition - position2);
			if (Physics.SphereCast(ray, num, out var hitInfo, (idealClawPosition - position2).magnitude, worldCollisionLayers))
			{
				knownSafePosition = ray.origin + ray.direction * (hitInfo.distance - num * 2.01f);
			}
			else
			{
				knownSafePosition = position;
			}
		}
		if ((isAnchored || isHoldingHand) && !flag)
		{
			GorillaTagger.Instance.StartVibration(isLeftHanded, hapticStrengthOnRelease, hapticDurationOnRelease);
			ClearClawAnchor();
		}
		else
		{
			if (isAnchored)
			{
				currentFuel = Mathf.Max(0f, currentFuel - dt * _current_grab_fps);
				isLowFuel = currentFuel < _lowFuelThreshold;
				if (isLowFuel && !flag2)
				{
					lowFuelSound.Play();
				}
				UpdateFuelGauge();
				if (currentFuel == 0f)
				{
					isGripBroken = true;
					flag = false;
					ClearClawAnchor();
					detachFailSound.Play();
				}
				else
				{
					Vector3 position3 = GTPlayer.Instance.transform.position;
					clawHoldAdjustment -= position3 - lastRequestedPlayerPosition;
					Vector3 v = clawAnchorPosition - (idealClawPosition + clawHoldAdjustment);
					v.ClampThisMagnitudeSafe(MaxTentacleJumpSpeed * dt);
					GTPlayer.Instance.RequestTentacleMove(isLeftHanded, v);
					GTPlayer.Instance.TentacleActiveAtFrame = Time.frameCount + 1;
					lastRequestedPlayerPosition = position3 + v;
					if ((clawAnchorPosition - base.transform.position).IsLongerThan(maxTentacleLength))
					{
						isGripBroken = true;
						ClearClawAnchor();
						detachFailSound.Play();
					}
					else
					{
						clawVisualPos = clawAnchorPosition;
						clawVisualRot = clawAnchorRotation;
					}
				}
				wasGrabPressed = flag;
				return;
			}
			if (isHoldingHand)
			{
				TakeMyHand_HandLink takeMyHand_HandLink = (isLeftHanded ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
				if (takeMyHand_HandLink.IsLinkActive())
				{
					Vector3 position4 = (isLeftHanded ? VRRig.LocalRig.leftHand : VRRig.LocalRig.rightHand).overrideTarget.position;
					takeMyHand_HandLink.TentacleOffset = idealClawPosition - position4;
				}
				else
				{
					isGripBroken = true;
					ClearClawAnchor();
					detachFailSound.Play();
				}
				wasGrabPressed = flag;
				return;
			}
		}
		RaycastHit hitInfo2;
		bool num2 = Physics.SphereCast(new Ray(knownSafePosition, idealClawPosition - knownSafePosition), num, out hitInfo2, (idealClawPosition - knownSafePosition).magnitude, worldCollisionLayers);
		Vector3 vector = idealClawPosition;
		Quaternion clawRotation = rotation;
		bool flag3 = false;
		bool flag4 = currentFuel < FuelCost_Grab + FuelPerSecond_Holding;
		if (flag4 && flag)
		{
			isGripBroken = true;
			flag = false;
			attachFailSound.Play();
		}
		if (num2)
		{
			if (!flag4)
			{
				float num3 = Vector3.Dot(hitInfo2.normal, Vector3.up);
				if (num3 >= _min_grab_dot)
				{
					_current_grab_fps = ((num3 >= _wall_angle_dot) ? FuelPerSecond_Holding : (FuelPerSecond_Holding * FuelCost_Wall_Multiplier));
					flag3 = true;
					if (GTPlayer.Instance.GetSlidePercentage(hitInfo2) > 0.5f)
					{
						if (!canHoldSlipperyWalls)
						{
							flag3 = false;
							if (flag && !hasFailedToGrab)
							{
								attachFailSound.Play();
								hasFailedToGrab = true;
							}
						}
						else
						{
							_current_grab_fps *= FuelCost_Slippery_Multiplier;
						}
					}
				}
				else if (flag && !hasFailedToGrab)
				{
					attachFailSound.Play();
					hasFailedToGrab = true;
				}
			}
			knownSafePosition += (idealClawPosition - knownSafePosition).normalized * (hitInfo2.distance - num * 2.01f);
			vector = hitInfo2.point + hitInfo2.normal * 0.1f;
		}
		else
		{
			knownSafePosition = idealClawPosition;
		}
		if (flag && flag3)
		{
			vector = hitInfo2.point + hitInfo2.normal * 0.01f;
			clawRotation = Quaternion.LookRotation(-hitInfo2.normal, rotation * Vector3.up);
			SetClawAnchor(vector, clawRotation, vector - idealClawPosition);
			GorillaTagger.Instance.StartVibration(isLeftHanded, hapticStrengthOnGrab, hapticDurationOnGrab);
			currentFuel -= FuelCost_Grab;
		}
		else
		{
			if (flag && !wasGrabPressed && (!GorillaComputer.instance.IsPlayerInVirtualStump() || !CustomMapManager.WantsHoldingHandsDisabled()))
			{
				TakeMyHand_HandLink takeMyHand_HandLink2 = (isLeftHanded ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
				Vector3 position5 = (isLeftHanded ? VRRig.LocalRig.leftHand : VRRig.LocalRig.rightHand).overrideTarget.position;
				foreach (VRRig activeRig in VRRigCache.ActiveRigs)
				{
					if (activeRig.isLocal)
					{
						continue;
					}
					if (activeRig.leftHandLink.interactionPoint.OverlapCheck(vector) && activeRig.leftHandLink.CanBeGrabbed())
					{
						if (takeMyHand_HandLink2.TentacleTryCreateLink(activeRig.leftHandLink))
						{
							isHoldingHand = true;
							clawHoldingVisual.SetActive(value: true);
							clawReleasedVisual.SetActive(value: false);
							takeMyHand_HandLink2.TentacleOffset = idealClawPosition - position5;
							heldPlayerCallback.Register(activeRig, activeRig.leftHandLink);
							gameEntity.RequestState(gameEntity.id, GetStateLong());
							break;
						}
					}
					else if (activeRig.rightHandLink.interactionPoint.OverlapCheck(vector) && activeRig.rightHandLink.CanBeGrabbed() && takeMyHand_HandLink2.TentacleTryCreateLink(activeRig.rightHandLink))
					{
						isHoldingHand = true;
						clawHoldingVisual.SetActive(value: true);
						clawReleasedVisual.SetActive(value: false);
						takeMyHand_HandLink2.TentacleOffset = idealClawPosition - position5;
						heldPlayerCallback.Register(activeRig, activeRig.rightHandLink);
						gameEntity.RequestState(gameEntity.id, GetStateLong());
						break;
					}
				}
			}
			Vector3 axis = Quaternion.AngleAxis(Time.time * 180f, Vector3.forward) * Vector3.up;
			if (flag3)
			{
				clawRotation = Quaternion.Lerp(rotation, Quaternion.LookRotation(-hitInfo2.normal, rotation * Vector3.up), 0.75f);
				clawRotation *= Quaternion.AngleAxis(5f, axis);
			}
			else
			{
				clawRotation *= Quaternion.AngleAxis(20f, axis);
				vector.y += 0.05f * Mathf.Cos(Time.time * 2f);
			}
		}
		clawVisualPos = vector;
		clawVisualRot = clawRotation;
		if (!isAnchored)
		{
			isLowFuel = currentFuel < FuelCost_Grab;
		}
		wasGrabPressed = flag;
		UpdateFuelGauge();
	}

	private void UpdateFuelGauge()
	{
		float value = currentFuel / fuelSize;
		for (int i = 0; i < m_gaugeMatSlots.Length; i++)
		{
			_gaugeMatPropBlock.SetFloat(ShaderProps._EmissionDissolveProgress, value);
			m_gaugeMatSlots[i].renderer.SetPropertyBlock(_gaugeMatPropBlock, m_gaugeMatSlots[i].slot);
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
		if (isAnchored)
		{
			return;
		}
		VRRig attachedPlayerRig = GetAttachedPlayerRig();
		if (attachedPlayerRig == null)
		{
			return;
		}
		Vector3 idealClawPosition = GetIdealClawPosition(attachedPlayerRig);
		Quaternion rotation = base.transform.rotation;
		Vector3 position = base.transform.position;
		if ((knownSafePosition - idealClawPosition).IsLongerThan(1f))
		{
			knownSafePosition = position;
		}
		if (isHoldingHand)
		{
			TakeMyHand_HandLink obj = (isLeftHanded ? attachedPlayerRig.leftHandLink : attachedPlayerRig.rightHandLink);
			Vector3 position2 = (isLeftHanded ? attachedPlayerRig.leftHand : attachedPlayerRig.rightHand).rigTarget.position;
			obj.TentacleOffset = idealClawPosition - position2;
			return;
		}
		float num = 0.15f;
		RaycastHit hitInfo;
		bool num2 = Physics.SphereCast(new Ray(knownSafePosition, idealClawPosition - knownSafePosition), num, out hitInfo, (idealClawPosition - knownSafePosition).magnitude, worldCollisionLayers);
		Vector3 axis = Quaternion.AngleAxis(Time.time * 180f, Vector3.forward) * Vector3.up;
		Vector3 vector = idealClawPosition;
		Quaternion quaternion = rotation;
		if (num2)
		{
			knownSafePosition += (idealClawPosition - knownSafePosition).normalized * (hitInfo.distance - num * 2.01f);
			vector = hitInfo.point + hitInfo.normal * 0.1f;
			quaternion *= Quaternion.AngleAxis(5f, axis);
		}
		else
		{
			knownSafePosition = idealClawPosition;
			quaternion *= Quaternion.AngleAxis(20f, axis);
			vector.y += 0.05f * Mathf.Cos(Time.time * 2f);
		}
		clawVisualPos = vector;
		clawVisualRot = quaternion;
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		FuelPerSecond_Holding = _fps_holding_base * (withUpgrades.Contains(SIUpgradeType.Tentacle_Efficiency) ? 0.8f : 1f);
		FuelPerSecond_Recharging = _fps_recharging_base * (withUpgrades.Contains(SIUpgradeType.Tentacle_Charge_Rate) ? 1.2f : 1f);
		FuelCost_Grab = _grabCost_base * (withUpgrades.Contains(SIUpgradeType.Tentacle_Efficiency) ? 0.8f : 1f);
		FuelCost_JumpSpeed = _jumpCost_base * (withUpgrades.Contains(SIUpgradeType.Tentacle_Efficiency) ? 0.8f : 1f);
		MaxGrabAngle = (withUpgrades.Contains(SIUpgradeType.Tentacle_Power_Claw) ? 180f : _grabAngle_base);
		MaxTentacleJumpSpeed = _jumpSpeed_base;
		_min_grab_dot = Mathf.Cos(MathF.PI / 180f * MaxGrabAngle);
		_lowFuelThreshold = FuelCost_Grab + FuelPerSecond_Holding;
	}

	private long GetStateLong()
	{
		if (isAnchored)
		{
			return 0x4000000000000000L | BitPackUtils.PackAnchoredPosRotForNetwork(clawAnchorPosition, clawAnchorRotation);
		}
		if (isHoldingHand)
		{
			TakeMyHand_HandLink takeMyHand_HandLink = (isLeftHanded ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
			int num = takeMyHand_HandLink.grabbedPlayer?.ActorNumber ?? 0;
			return long.MinValue | (takeMyHand_HandLink.grabbedHandIsLeft ? 2305843009213693952L : 0) | num;
		}
		return 0L;
	}

	private void SetClawAnchor(Vector3 clawPosition, Quaternion clawRotation, Vector3 adjustment)
	{
		if (!isAnchored)
		{
			attachSound.Play();
		}
		hasFailedToGrab = false;
		isAnchored = true;
		clawHoldAdjustment = adjustment;
		clawAnchorPosition = clawPosition;
		clawAnchorRotation = clawRotation;
		clawHoldingVisual.SetActive(value: true);
		clawReleasedVisual.SetActive(value: false);
		if (IsEquippedLocal())
		{
			lastRequestedPlayerPosition = GTPlayer.Instance.transform.position;
			GTPlayer.Instance.SetGravityOverride(this, GravityOverrideFunction);
			hasGravityOverride = true;
			SIPlayer.LocalPlayer.OnKnockback += OnKnockback;
			gameEntity.RequestState(gameEntity.id, GetStateLong());
		}
	}

	private void ClearClawAnchor()
	{
		if (isAnchored || isHoldingHand)
		{
			detachSound.Play();
		}
		hasFailedToGrab = false;
		isAnchored = false;
		clawHoldingVisual.SetActive(value: false);
		clawReleasedVisual.SetActive(value: true);
		if (isHoldingHand && IsEquippedLocal())
		{
			(isLeftHanded ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink).BreakLink();
		}
		isHoldingHand = false;
		if (hasGravityOverride)
		{
			GTPlayer.Instance.UnsetGravityOverride(this);
			hasGravityOverride = false;
		}
		if (IsEquippedLocal() && !IsBlocked(SIExclusionType.AffectsLocalMovement))
		{
			Vector3 averagedVelocity = GTPlayer.Instance.AveragedVelocity;
			float a = averagedVelocity.magnitude;
			if (FuelCost_JumpSpeed > 0f)
			{
				a = Mathf.Min(a, currentFuel / FuelCost_JumpSpeed * MaxTentacleJumpSpeed);
			}
			a = Mathf.Min(a, MaxTentacleJumpSpeed);
			currentFuel -= a / MaxTentacleJumpSpeed * FuelCost_JumpSpeed;
			if (averagedVelocity.IsLongerThan(a))
			{
				GTPlayer.Instance.SetVelocity(averagedVelocity.normalized * a);
			}
			else
			{
				GTPlayer.Instance.SetVelocity(averagedVelocity);
			}
			SIPlayer.LocalPlayer.OnKnockback -= OnKnockback;
			gameEntity.RequestState(gameEntity.id, GetStateLong());
		}
	}

	private void OnKnockback(Vector3 knockbackVector)
	{
		if (isAnchored)
		{
			isGripBroken = true;
			ClearClawAnchor();
		}
	}

	private void GravityOverrideFunction(GTPlayer player)
	{
	}

	private void OnEntityStateChanged(long oldState, long newState)
	{
		if (IsEquippedLocal() || activatedLocally)
		{
			return;
		}
		if ((newState & long.MinValue) != 0L)
		{
			isHoldingHand = true;
			clawHoldingVisual.SetActive(value: true);
			clawReleasedVisual.SetActive(value: false);
			if (GamePlayer.TryGetGamePlayer((int)newState, out var out_gamePlayer))
			{
				heldPlayerCallback.Register(out_gamePlayer.rig, ((newState & 0x2000000000000000L) != 0L) ? out_gamePlayer.rig.leftHandLink : out_gamePlayer.rig.rightHandLink);
			}
		}
		else if (newState != 0L)
		{
			int attachedPlayerActorNr = gameEntity.AttachedPlayerActorNr;
			if (attachedPlayerActorNr >= 1 && GamePlayer.TryGetGamePlayer(attachedPlayerActorNr, out var out_gamePlayer2))
			{
				BitPackUtils.UnpackAnchoredPosRotForNetwork(newState, out_gamePlayer2.rig.transform.position, out var pos, out var rot);
				SetClawAnchor(pos, rot, Vector3.zero);
				clawVisualPos = clawAnchorPosition;
				clawVisualRot = clawAnchorRotation;
			}
		}
		else
		{
			ClearClawAnchor();
		}
	}

	public override void OnEntityInit()
	{
		currentFuel = 10f;
	}

	public static Vector3 GetPlaneIntersection(Vector3 p1Pos, Vector3 p1Norm, Vector3 p2Pos, Vector3 p2Norm, Vector3 refPoint)
	{
		Vector3 normalized = Vector3.Cross(p1Norm, p2Norm).normalized;
		float num = Vector3.Dot(p1Pos, p1Norm);
		float num2 = Vector3.Dot(p2Pos, p2Norm);
		float num3 = Vector3.Dot(p1Norm, p2Norm);
		float num4 = 1f - num3 * num3;
		if (Mathf.Abs(num4) < 0.001f)
		{
			return refPoint;
		}
		float num5 = (num - num2 * num3) / num4;
		float num6 = (num2 - num * num3) / num4;
		Vector3 vector = num5 * p1Norm + num6 * p2Norm;
		return vector + Vector3.Project(refPoint - vector, normalized);
	}

	public static Vector3 SplineSample(float theta, Vector3 startDir, Vector3 endPos, Vector3 endDir)
	{
		float num = 1f - theta;
		float t = Mathf.Lerp(theta * theta, 1f - num * num, theta);
		Vector3 a = startDir * theta;
		Vector3 b = endPos + endDir * num;
		return Vector3.Lerp(a, b, t);
	}

	private void UpdateTentacle(Material material, Transform tentacle, Transform anchor)
	{
		Vector3 vector = Vector3.forward * LengthFactor;
		material.SetVector(tentacleStartDir_HASH, vector);
		Vector3 vector2 = tentacle.InverseTransformPoint(anchor.position);
		material.SetVector(tentacleEnd_HASH, vector2);
		Vector3 vector3 = -tentacle.InverseTransformDirection(anchor.forward) * LengthFactor;
		material.SetVector(tentacleEndDir_HASH, vector3);
		Vector3 vector4 = SplineSample(0.25f, vector, vector2, vector3);
		Vector3 vector5 = SplineSample(0.26f, vector, vector2, vector3);
		Vector3 vector6 = SplineSample(0.75f, vector, vector2, vector3);
		Vector3 vector7 = SplineSample(0.76f, vector, vector2, vector3);
		Vector3 planeIntersection = GetPlaneIntersection(vector4, (vector5 - vector4).normalized, vector6, (vector7 - vector6).normalized, Quaternion.AngleAxis(90f, Vector3.forward) * vector2.WithZ(0f).normalized);
		material.SetVector(tentacleRingOrigin_HASH, planeIntersection);
	}

	public void CallBack()
	{
		lastCallbackFrame = Time.frameCount;
		if (!isHoldingHand || lastHeldCallbackFrame == lastCallbackFrame)
		{
			claw.transform.localPosition = Vector3.MoveTowards(claw.transform.localPosition, claw.transform.parent.InverseTransformPoint(clawVisualPos), ClawMaxBlendSpeed * Time.deltaTime);
			claw.transform.localRotation = Quaternion.RotateTowards(claw.transform.localRotation, claw.transform.parent.InverseTransformRotation(clawVisualRot), ClawMaxRotBlendSpeed * Time.deltaTime);
			UpdateTentacle(tentacleMat, tentacleRenderer.transform, tentacleAnchor);
			if (hasTentacle2)
			{
				UpdateTentacle(tentacleMat2, tentacleRenderer2.transform, tentacleAnchor2);
			}
		}
	}

	private void UpdateTentacleHoldingHandPos(TakeMyHand_HandLink heldHandLink)
	{
		if (!isHoldingHand)
		{
			heldPlayerCallback.Unregister();
			return;
		}
		lastHeldCallbackFrame = Time.frameCount;
		clawVisualPos = heldHandLink.LinkPosition;
		clawVisualRot = heldHandLink.transform.rotation * Quaternion.AngleAxis(90f, Vector3.right);
		if (lastHeldCallbackFrame == lastCallbackFrame)
		{
			CallBack();
		}
	}

	public void UpdateRecharge(float dt)
	{
		if (!isAnchored)
		{
			currentFuel = Mathf.Clamp(currentFuel + dt * FuelPerSecond_Recharging, 0f, fuelSize);
		}
	}
}
