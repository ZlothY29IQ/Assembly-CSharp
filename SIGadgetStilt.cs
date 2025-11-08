using System;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
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
	private float boostSpeedFactor;

	[SerializeField]
	private GorillaVelocityTracker tipVelocityTracker;

	[SerializeField]
	private SoundBankPlayer retractSoundBank;

	[SerializeField]
	private SoundBankPlayer extendSoundBank;

	[SerializeField]
	private Material defaultMat;

	[SerializeField]
	private Material tagActivatedMat;

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

	private float adjustmentSendRate = 0.25f;

	private float lastSentLength;

	private float nextAdjustmentSendTime = -1f;

	private StiltID currentStiltID = StiltID.None;

	private SnapJointType wasSnappedByLocalJoint;

	private int attachedPlayerActorNr = int.MinValue;

	private NetPlayer attachedNetPlayer;

	private VRRig attachedVRRig;

	private bool isTagged;

	public bool TriggerToExtend { get; private set; }

	public bool StickToAdjustLength { get; private set; }

	public bool CanTag { get; private set; }

	public bool CanStun { get; private set; }

	private void Awake()
	{
		tipVelocityTracker.enabled = false;
		tipDefaultOffset = tip.transform.localPosition;
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
			tipVelocityTracker.enabled = false;
		}
	}

	private void OnGrabbed()
	{
		DisableCurrentStilt();
		HandleStartInteraction();
		if (IsEquippedLocal())
		{
			activatedLocally = true;
			currentStiltID = ((gameEntity.heldByHandIndex != 0) ? StiltID.Held_Right : StiltID.Held_Left);
			if (boostSpeedFactor > 0f)
			{
				tipVelocityTracker.enabled = true;
				tipVelocityTracker.SetRelativeTo(VRRig.LocalRig.transform);
			}
			GTPlayer.Instance.EnableStilt(currentStiltID, stiltEnd.position, maxArmLength, CanTag, CanStun, boostSpeedFactor, tipVelocityTracker);
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
			gameEntity.RequestState(gameEntity.id, (long)(targetLength * 1000f));
		}
	}

	private void OnSnapped()
	{
		DisableCurrentStilt();
		HandleStartInteraction();
		if (IsEquippedLocal())
		{
			wasSnappedByLocalJoint = gameEntity.snappedJoint;
			if (wasSnappedByLocalJoint == SnapJointType.ArmL)
			{
				currentStiltID = StiltID.Snapped_Left;
				if (boostSpeedFactor > 0f)
				{
					tipVelocityTracker.enabled = true;
					tipVelocityTracker.SetRelativeTo(VRRig.LocalRig.transform);
				}
				GTPlayer.Instance.EnableStilt(currentStiltID, stiltEnd.position, maxArmLength, CanTag, CanStun, boostSpeedFactor, tipVelocityTracker);
			}
			else if (wasSnappedByLocalJoint == SnapJointType.ArmR)
			{
				currentStiltID = StiltID.Snapped_Right;
				if (boostSpeedFactor > 0f)
				{
					tipVelocityTracker.enabled = true;
					tipVelocityTracker.SetRelativeTo(VRRig.LocalRig.transform);
				}
				GTPlayer.Instance.EnableStilt(currentStiltID, stiltEnd.position, maxArmLength, CanTag, CanStun, boostSpeedFactor, tipVelocityTracker);
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
		if (wasSnappedByLocalJoint == SnapJointType.ArmL)
		{
			wasSnappedByLocalJoint = SnapJointType.None;
		}
		else if (wasSnappedByLocalJoint == SnapJointType.ArmR)
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
		if (currentStiltID != StiltID.None)
		{
			bool num = !TriggerToExtend || CheckInput();
			bool flag = false;
			float oldLength = targetLength;
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
					flag = true;
				}
			}
			else if (!Mathf.Approximately(targetLength, retractedLength))
			{
				targetLength = retractedLength;
				lastSentLength = targetLength;
				flag = true;
			}
			if (flag)
			{
				CheckPlaySounds(oldLength, targetLength);
				gameEntity.RequestState(gameEntity.id, (long)(targetLength * 1000f));
			}
		}
		UpdateLength();
	}

	protected override void OnUpdateRemote(float dt)
	{
		base.OnUpdateRemote(dt);
		UpdateLength();
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

	private void UpdateLength()
	{
		if (!Mathf.Approximately(currentLength, targetLength))
		{
			float num = ((targetLength > currentLength) ? extendSpeed : retractSpeed);
			currentLength = Mathf.MoveTowards(currentLength, targetLength, num * Time.deltaTime);
			ApplyCurrentLength();
			if (currentStiltID != StiltID.None)
			{
				GTPlayer.Instance.UpdateStiltOffset(currentStiltID, stiltEnd.position);
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
		float oldLength = targetLength;
		targetLength = Mathf.Clamp((float)newState * 0.001f, retractedLength, maxLength);
		if (!IsEquippedLocal())
		{
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
		attachedPlayerActorNr = GetAttachedPlayerActorNumber();
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
	}
}
