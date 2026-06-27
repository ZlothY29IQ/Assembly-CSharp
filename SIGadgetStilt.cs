using System;
using GorillaLocomotion;
using UnityEngine;

public class SIGadgetStilt : SIGadget
{
	[SerializeField]
	private GameButtonActivatable buttonActivatable;

	public GameObject tip;

	[SerializeField]
	private Vector3 offsetDir = Vector3.forward;

	private Vector3 tipDefaultOffset;

	public GameObject midpoint;

	public Transform stiltEnd;

	private bool hasEndB;

	public Transform stiltEndB;

	private bool hasEndC;

	public Transform stiltEndC;

	public Transform motorTransform;

	[SerializeField]
	private AudioSource motorAudio;

	[SerializeField]
	private SIUpgradeType[] restrictedUpgrades;

	[SerializeField]
	private float maxLengthNormal;

	[SerializeField]
	private float maxLengthUpgraded;

	[SerializeField]
	private float retractedLength;

	[SerializeField]
	private float lengthChangeSpeed;

	[SerializeField]
	private float maxArmLength;

	[SerializeField]
	private float extendSpeedNormal;

	[SerializeField]
	private float extendSpeedUpgraded;

	[SerializeField]
	private float retractSpeedNormal;

	[SerializeField]
	private float retractSpeedUpgraded;

	[SerializeField]
	private float rotateSpeedFactor;

	[SerializeField]
	private SoundBankPlayer retractSoundBank;

	[SerializeField]
	private SoundBankPlayer extendSoundBank;

	[SerializeField]
	private Material defaultMat;

	[SerializeField]
	private Material tagActivatedMat;

	[SerializeField]
	private GameObject[] tagActivatedObjects;

	[SerializeField]
	private MeshRenderer matDest;

	[SerializeField]
	private SkinnedMeshRenderer skinnedMatDest;

	private float currentExtendedLength;

	private float targetLength;

	private float currentLength;

	private float maxLength;

	private float extendSpeed;

	private float retractSpeed;

	private float currentMotorAngle;

	private float adjustmentSendRate = 0.25f;

	private float lastSentLength;

	private float nextAdjustmentSendTime = -1f;

	private bool IsSpinning;

	private StiltID currentStiltID = StiltID.None;

	private StiltID currentStiltIDB = StiltID.None;

	private StiltID currentStiltIDC = StiltID.None;

	private SnapJointType wasSnappedByLocalJoint;

	private const long IsSpinningBit = 1L;

	private int attachedPlayerActorNr = int.MinValue;

	private NetPlayer attachedNetPlayer;

	private VRRig attachedVRRig;

	private bool isTagged;

	public bool TriggerToExtend { get; private set; }

	public bool hasMotor { get; private set; }

	public bool StickToAdjustLength { get; private set; }

	public bool CanTag { get; private set; }

	public bool CanStun { get; private set; }

	private void Awake()
	{
		tipDefaultOffset = tip.transform.localPosition;
		hasMotor = motorTransform != null;
		hasEndB = stiltEndB != null;
		hasEndC = stiltEndC != null;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(OnSnapped));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(OnReleased));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(OnUnsnapped));
		gameEntity.OnStateChanged += OnEntityStateChanged;
	}

	private void DisableCurrentStilt()
	{
		if (currentStiltID != StiltID.None)
		{
			GTPlayer.Instance.DisableStilt(currentStiltID);
			currentStiltID = StiltID.None;
		}
		if (currentStiltIDB != StiltID.None)
		{
			GTPlayer.Instance.DisableStilt(currentStiltIDB);
			currentStiltIDB = StiltID.None;
		}
		if (currentStiltIDC != StiltID.None)
		{
			GTPlayer.Instance.DisableStilt(currentStiltIDC);
			currentStiltIDC = StiltID.None;
		}
	}

	private void OnGrabbed()
	{
		DisableCurrentStilt();
		HandleStartInteraction();
		if (IsEquippedLocal())
		{
			activatedLocally = true;
			if (gameEntity.heldByHandIndex == 0)
			{
				currentStiltID = StiltID.Held_Left;
				GTPlayer.Instance.EnableStilt(currentStiltID, isLeftHand: true, stiltEnd.position, maxArmLength, CanTag, CanStun);
				if (hasEndB)
				{
					currentStiltIDB = StiltID.Held_Left2;
					GTPlayer.Instance.EnableStilt(currentStiltIDB, isLeftHand: true, stiltEndB.position, maxArmLength, CanTag, CanStun);
				}
				if (hasEndC)
				{
					currentStiltIDC = StiltID.Held_Left3;
					GTPlayer.Instance.EnableStilt(currentStiltIDC, isLeftHand: true, stiltEndC.position, maxArmLength, CanTag, CanStun);
				}
			}
			else
			{
				currentStiltID = StiltID.Held_Right;
				GTPlayer.Instance.EnableStilt(currentStiltID, isLeftHand: false, stiltEnd.position, maxArmLength, CanTag, CanStun);
				if (hasEndB)
				{
					currentStiltIDB = StiltID.Held_Right2;
					GTPlayer.Instance.EnableStilt(currentStiltIDB, isLeftHand: false, stiltEndB.position, maxArmLength, CanTag, CanStun);
				}
				if (hasEndC)
				{
					currentStiltIDC = StiltID.Held_Right3;
					GTPlayer.Instance.EnableStilt(currentStiltIDC, isLeftHand: false, stiltEndC.position, maxArmLength, CanTag, CanStun);
				}
			}
		}
		else
		{
			activatedLocally = false;
		}
		wasSnappedByLocalJoint = SnapJointType.None;
	}

	private void OnReleased()
	{
		DisableCurrentStilt();
		HandleStopInteraction();
		if (gameEntity.WasLastHeldByLocalPlayer() && TriggerToExtend && !Mathf.Approximately(targetLength, retractedLength))
		{
			targetLength = retractedLength;
			gameEntity.RequestState(gameEntity.id, PackStateForNetwork());
		}
	}

	private void OnSnapped()
	{
		DisableCurrentStilt();
		HandleStartInteraction();
		if (IsEquippedLocal())
		{
			wasSnappedByLocalJoint = gameEntity.snappedJoint;
			if (wasSnappedByLocalJoint == SnapJointType.HandL)
			{
				currentStiltID = StiltID.Snapped_Left;
				GTPlayer.Instance.EnableStilt(currentStiltID, isLeftHand: true, stiltEnd.position, maxArmLength, CanTag, CanStun);
				if (hasEndB)
				{
					currentStiltIDB = StiltID.Snapped_Left2;
					GTPlayer.Instance.EnableStilt(currentStiltIDB, isLeftHand: true, stiltEndB.position, maxArmLength, CanTag, CanStun);
				}
				if (hasEndC)
				{
					currentStiltIDC = StiltID.Snapped_Left3;
					GTPlayer.Instance.EnableStilt(currentStiltIDC, isLeftHand: true, stiltEndC.position, maxArmLength, CanTag, CanStun);
				}
			}
			else if (wasSnappedByLocalJoint == SnapJointType.HandR)
			{
				currentStiltID = StiltID.Snapped_Right;
				GTPlayer.Instance.EnableStilt(currentStiltID, isLeftHand: false, stiltEnd.position, maxArmLength, CanTag, CanStun);
				if (hasEndB)
				{
					currentStiltIDB = StiltID.Snapped_Right2;
					GTPlayer.Instance.EnableStilt(currentStiltIDB, isLeftHand: false, stiltEndB.position, maxArmLength, CanTag, CanStun);
				}
				if (hasEndC)
				{
					currentStiltIDC = StiltID.Snapped_Right3;
					GTPlayer.Instance.EnableStilt(currentStiltIDC, isLeftHand: false, stiltEndC.position, maxArmLength, CanTag, CanStun);
				}
			}
		}
		else
		{
			wasSnappedByLocalJoint = SnapJointType.None;
		}
	}

	private void OnUnsnapped()
	{
		DisableCurrentStilt();
		HandleStopInteraction();
		if (wasSnappedByLocalJoint == SnapJointType.HandL)
		{
			wasSnappedByLocalJoint = SnapJointType.None;
		}
		else if (wasSnappedByLocalJoint == SnapJointType.HandR)
		{
			wasSnappedByLocalJoint = SnapJointType.None;
		}
	}

	private void OnDestroy()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			DisableCurrentStilt();
			if (attachedVRRig != null)
			{
				VRRig vRRig = attachedVRRig;
				vRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(vRRig.OnMaterialIndexChanged, new Action<int, int>(HandleVRRigMaterialIndexChanged));
			}
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
		if (IsBlocked(SIExclusionType.AffectsLocalMovement))
		{
			DisableCurrentStilt();
			return;
		}
		bool isSpinning = IsSpinning;
		bool flag = false;
		if (currentStiltID != StiltID.None)
		{
			bool num = !TriggerToExtend || CheckInput();
			IsSpinning = hasMotor && CheckInput();
			bool flag2 = false;
			float oldLength = targetLength;
			if (IsSpinning)
			{
				SpinMotor(dt);
				flag = true;
			}
			if (num)
			{
				if (StickToAdjustLength)
				{
					Vector2 joystickInput = GetJoystickInput();
					if (Mathf.Abs(joystickInput.y) > 0.75f && Mathf.Abs(joystickInput.x) < 0.5f)
					{
						currentExtendedLength = Mathf.Clamp(currentExtendedLength + joystickInput.y * lengthChangeSpeed * Time.deltaTime, retractedLength, maxLength);
					}
				}
				if (!Mathf.Approximately(targetLength, currentExtendedLength))
				{
					targetLength = currentExtendedLength;
				}
				if (!Mathf.Approximately(targetLength, lastSentLength) && Time.time > nextAdjustmentSendTime)
				{
					nextAdjustmentSendTime = Time.time + adjustmentSendRate;
					lastSentLength = targetLength;
					flag2 = true;
				}
			}
			else if (!Mathf.Approximately(targetLength, retractedLength))
			{
				targetLength = retractedLength;
				lastSentLength = targetLength;
				flag2 = true;
			}
			if (flag2 || IsSpinning != isSpinning)
			{
				CheckPlaySounds(oldLength, targetLength);
				gameEntity.RequestState(gameEntity.id, PackStateForNetwork());
			}
		}
		if (hasMotor && !flag && motorAudio.isPlaying)
		{
			motorAudio.Stop();
		}
		isSpinning = IsSpinning;
		UpdateEndPoints(IsSpinning);
	}

	private long PackStateForNetwork()
	{
		long num = 0L;
		if (IsSpinning)
		{
			num |= 1;
		}
		else if (hasMotor)
		{
			long num2 = Mathf.RoundToInt(currentMotorAngle);
			num |= num2 << 1;
		}
		long num3 = Mathf.Clamp(Mathf.RoundToInt(targetLength * 1000f), 0, 3000);
		return num | (num3 << 10);
	}

	private void UnpackStateFromNetwork(long state)
	{
		IsSpinning = (state & 1) != 0;
		if (hasMotor && !IsSpinning)
		{
			currentMotorAngle = (state >> 1) & 0x1FF;
			motorTransform.localRotation = Quaternion.AngleAxis(currentMotorAngle, Vector3.right);
		}
		int num = (int)((state >> 10) & 0xFFF);
		targetLength = Mathf.Clamp((float)num * 0.001f, retractedLength, maxLength);
	}

	private void SpinMotor(float dt)
	{
		SuperInfectionManager activeSuperInfectionManager = SuperInfectionManager.activeSuperInfectionManager;
		float num = (((object)activeSuperInfectionManager != null && activeSuperInfectionManager.IsSupercharged) ? 1.5f : 1f);
		currentMotorAngle = (currentMotorAngle + rotateSpeedFactor * num * dt) % 360f;
		motorTransform.localRotation = Quaternion.AngleAxis(currentMotorAngle, Vector3.right);
		if (!motorAudio.isPlaying)
		{
			motorAudio.Play();
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
		base.OnUpdateRemote(dt);
		if (hasMotor)
		{
			if (IsSpinning && (gameEntity.heldByActorNumber >= 0 || gameEntity.snappedByActorNumber >= 0))
			{
				SpinMotor(dt);
			}
			else if (motorAudio.isPlaying)
			{
				motorAudio.Stop();
			}
		}
		UpdateEndPoints(force: false);
	}

	private bool CheckInput()
	{
		return buttonActivatable.CheckInput();
	}

	public override SIUpgradeSet FilterUpgradeNodes(SIUpgradeSet upgrades)
	{
		if (restrictedUpgrades.Length == 0)
		{
			return upgrades;
		}
		SIUpgradeSet result = default(SIUpgradeSet);
		SIUpgradeType[] array = restrictedUpgrades;
		foreach (SIUpgradeType upgrade in array)
		{
			if (upgrades.Contains(upgrade))
			{
				result.Add(upgrade);
			}
		}
		return result;
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		CanTag = withUpgrades.Contains(SIUpgradeType.Stilt_Tag_Tip);
		CanStun = withUpgrades.Contains(SIUpgradeType.Stilt_Stun_Tip);
		TriggerToExtend = buttonActivatable != null && withUpgrades.Contains(SIUpgradeType.Stilt_Retractable);
		StickToAdjustLength = TriggerToExtend && withUpgrades.Contains(SIUpgradeType.Stilt_Adjustable_Length);
		extendSpeed = (withUpgrades.Contains(SIUpgradeType.Stilt_Retract_Speed) ? extendSpeedUpgraded : extendSpeedNormal);
		retractSpeed = (withUpgrades.Contains(SIUpgradeType.Stilt_Retract_Speed) ? retractSpeedUpgraded : retractSpeedNormal);
		maxLength = ((TriggerToExtend && withUpgrades.Contains(SIUpgradeType.Stilt_Max_Length)) ? maxLengthUpgraded : maxLengthNormal);
		currentExtendedLength = maxLength;
		targetLength = (TriggerToExtend ? retractedLength : currentExtendedLength);
		currentLength = targetLength;
		ApplyCurrentLength();
	}

	private void UpdateEndPoints(bool force)
	{
		if (force || !Mathf.Approximately(currentLength, targetLength))
		{
			float num = ((targetLength > currentLength) ? extendSpeed : retractSpeed);
			currentLength = Mathf.MoveTowards(currentLength, targetLength, num * Time.deltaTime);
			ApplyCurrentLength();
			if (currentStiltID != StiltID.None)
			{
				GTPlayer.Instance.UpdateStiltOffset(currentStiltID, stiltEnd.position);
			}
			if (currentStiltIDB != StiltID.None)
			{
				GTPlayer.Instance.UpdateStiltOffset(currentStiltIDB, stiltEndB.position);
			}
			if (currentStiltIDC != StiltID.None)
			{
				GTPlayer.Instance.UpdateStiltOffset(currentStiltIDC, stiltEndC.position);
			}
		}
	}

	private void ApplyCurrentLength()
	{
		tip.transform.localPosition = offsetDir * currentLength + tipDefaultOffset;
		Vector3 localScale = midpoint.transform.localScale;
		localScale.z = currentLength;
		midpoint.transform.localScale = localScale;
	}

	private void OnEntityStateChanged(long oldState, long newState)
	{
		if (!IsEquippedLocal())
		{
			float oldLength = targetLength;
			UnpackStateFromNetwork(newState);
			CheckPlaySounds(oldLength, targetLength);
		}
	}

	private void CheckPlaySounds(float oldLength, float newLength)
	{
		if (!Mathf.Approximately(oldLength, newLength))
		{
			if (Mathf.Approximately(newLength, retractedLength))
			{
				retractSoundBank.Play();
			}
			else if (Mathf.Approximately(oldLength, retractedLength))
			{
				extendSoundBank.Play();
			}
		}
	}

	private void HandleStartInteraction()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		attachedPlayerActorNr = gameEntity.AttachedPlayerActorNr;
		attachedNetPlayer = NetworkSystem.Instance.GetPlayer(attachedPlayerActorNr);
		if (GamePlayer.TryGetGamePlayer(attachedPlayerActorNr, out var out_gamePlayer))
		{
			if (attachedVRRig != null)
			{
				VRRig vRRig = attachedVRRig;
				vRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(vRRig.OnMaterialIndexChanged, new Action<int, int>(HandleVRRigMaterialIndexChanged));
			}
			attachedVRRig = out_gamePlayer.rig;
			VRRig vRRig2 = attachedVRRig;
			vRRig2.OnMaterialIndexChanged = (Action<int, int>)Delegate.Combine(vRRig2.OnMaterialIndexChanged, new Action<int, int>(HandleVRRigMaterialIndexChanged));
			int num = (isTagged ? 2 : 0);
			if (num != attachedVRRig.setMatIndex)
			{
				HandleVRRigMaterialIndexChanged(num, attachedVRRig.setMatIndex);
			}
		}
	}

	private void HandleStopInteraction()
	{
		attachedPlayerActorNr = -1;
		attachedNetPlayer = null;
		if (attachedVRRig != null)
		{
			VRRig vRRig = attachedVRRig;
			vRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(vRRig.OnMaterialIndexChanged, new Action<int, int>(HandleVRRigMaterialIndexChanged));
		}
		attachedVRRig = null;
		if (isTagged)
		{
			HandleVRRigMaterialIndexChanged(2, 0);
		}
	}

	private void HandleVRRigMaterialIndexChanged(int oldMatIndex, int newMatIndex)
	{
		if (attachedPlayerActorNr != -1 && (newMatIndex == 2 || newMatIndex == 1) && CanTag && GorillaGameManager.instance is SuperInfectionGame superInfectionGame)
		{
			isTagged = attachedNetPlayer != null && superInfectionGame.IsInfected(attachedNetPlayer);
			if ((bool)matDest)
			{
				matDest.sharedMaterial = tagActivatedMat;
			}
			if ((bool)skinnedMatDest)
			{
				skinnedMatDest.sharedMaterial = tagActivatedMat;
			}
		}
		else
		{
			isTagged = false;
			if ((bool)matDest)
			{
				matDest.sharedMaterial = defaultMat;
			}
			if ((bool)skinnedMatDest)
			{
				skinnedMatDest.sharedMaterial = defaultMat;
			}
		}
		GameObject[] array = tagActivatedObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(isTagged);
		}
	}
}
