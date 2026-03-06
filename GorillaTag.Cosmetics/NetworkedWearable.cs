using GorillaNetworking;
using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class NetworkedWearable : MonoBehaviour, ISpawnable, ITickSystemTick
{
	[Tooltip("Whether the wearable state is toggled on by default.")]
	[SerializeField]
	private bool startTrue;

	[Tooltip("This is to determine what bit to change in VRRig.WearablesPackedStates.")]
	[SerializeField]
	private CosmeticsController.CosmeticCategory assignedSlot;

	[FormerlySerializedAs("IsTwoHanded")]
	[SerializeField]
	private bool isTwoHanded;

	private const string listenInfo = "listenForChangesLocal should be false in most cases";

	private const string listenDetails = "listenForChangesLocal should be false in most cases\nIf you have a first person part and a local rig part that both need to react to a state change\ncall the Toggle/Set functions to change the state from one prefab and check \nlistenForChangesLocal on the other prefab ";

	[SerializeField]
	private bool listenForChangesLocal;

	private VRRig.WearablePackedStateSlots wearableSlot;

	private VRRig.WearablePackedStateSlots leftSlot = VRRig.WearablePackedStateSlots.LeftHand;

	private VRRig.WearablePackedStateSlots rightSlot = VRRig.WearablePackedStateSlots.RightHand;

	private VRRig myRig;

	private bool isLocal;

	private bool value;

	private bool leftHandValue;

	private bool rightHandValue;

	[SerializeField]
	protected UnityEvent OnWearableStateTrue;

	[SerializeField]
	protected UnityEvent OnWearableStateFalse;

	[SerializeField]
	protected UnityEvent OnLeftWearableStateTrue;

	[SerializeField]
	protected UnityEvent OnLeftWearableStateFalse;

	[SerializeField]
	protected UnityEvent OnRightWearableStateTrue;

	[SerializeField]
	protected UnityEvent OnRightWearableStateFalse;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public bool TickRunning { get; set; }

	private void Awake()
	{
		if (assignedSlot != CosmeticsController.CosmeticCategory.Paw)
		{
			isTwoHanded = false;
		}
		wearableSlot = CosmeticCategoryToWearableSlot(assignedSlot, isLeft: true);
		leftSlot = CosmeticCategoryToWearableSlot(assignedSlot, isLeft: true);
		rightSlot = CosmeticCategoryToWearableSlot(assignedSlot, isLeft: false);
	}

	private void OnEnable()
	{
		if (IsSpawned)
		{
			if (isLocal && !listenForChangesLocal)
			{
				SetWearableStateBool(startTrue);
			}
			else if (!TickRunning)
			{
				TickSystem<object>.AddTickCallback(this);
			}
		}
	}

	public void ToggleWearableStateBool()
	{
		if (isLocal && IsSpawned && IsCategoryValid(assignedSlot) && !(myRig == null))
		{
			if (listenForChangesLocal)
			{
				GTDev.LogError("NetworkedWearable with listenForChangesLocal calling ToggleWearableStateBool on object " + base.gameObject.name + ".You should not change state from a listener");
			}
			else if (assignedSlot == CosmeticsController.CosmeticCategory.Paw && isTwoHanded)
			{
				GTDev.LogWarning("NetworkedWearable calling ToggleWearableStateBool on two handed object " + base.gameObject.name + ". please use ToggleLeftWearableStateBool or ToggleRightWearableStateBool instead");
				ToggleLeftWearableStateBool();
			}
			else
			{
				value = !value;
				myRig.WearablePackedStates = GTBitOps.WriteBit(myRig.WearablePackedStates, (int)wearableSlot, value);
				OnWearableStateChanged();
			}
		}
	}

	public void SetWearableStateBool(bool newState)
	{
		if (isLocal && IsSpawned && IsCategoryValid(assignedSlot) && !(myRig == null))
		{
			if (listenForChangesLocal)
			{
				GTDev.LogError("NetworkedWearable with listenForChangesLocal calling SetWearableStateBool on object " + base.gameObject.name + ".You should not change state from a listener");
			}
			else if (assignedSlot == CosmeticsController.CosmeticCategory.Paw && isTwoHanded)
			{
				GTDev.LogWarning("NetworkedWearable calling SetWearableStateBool on two handed object " + base.gameObject.name + ". please use SetLeftWearableStateBool or SetRightWearableStateBool instead");
				SetLeftWearableStateBool(newState);
			}
			else if (value != newState)
			{
				value = newState;
				myRig.WearablePackedStates = GTBitOps.WriteBit(myRig.WearablePackedStates, (int)wearableSlot, value);
				OnWearableStateChanged();
			}
		}
	}

	public void ToggleLeftWearableStateBool()
	{
		if (isLocal && IsSpawned && IsCategoryValid(assignedSlot) && !(myRig == null))
		{
			if (listenForChangesLocal)
			{
				GTDev.LogError("NetworkedWearable with listenForChangesLocal calling ToggleLeftWearableStateBool on object " + base.gameObject.name + ".You should not change state from a listener");
			}
			else if (assignedSlot != CosmeticsController.CosmeticCategory.Paw || !isTwoHanded)
			{
				GTDev.LogWarning("NetworkedWearable calling ToggleLeftWearableStateBool on one handed object " + base.gameObject.name + ". Please use ToggleWearableStateBool instead");
				ToggleWearableStateBool();
			}
			else
			{
				leftHandValue = !leftHandValue;
				myRig.WearablePackedStates = GTBitOps.WriteBit(myRig.WearablePackedStates, (int)leftSlot, leftHandValue);
				OnLeftStateChanged();
			}
		}
	}

	public void ToggleRightWearableStateBool()
	{
		if (isLocal && IsSpawned && IsCategoryValid(assignedSlot) && !(myRig == null))
		{
			if (listenForChangesLocal)
			{
				GTDev.LogError("NetworkedWearable with listenForChangesLocal calling ToggleRightWearableStateBool on object " + base.gameObject.name + ".You should not change state from a listener");
			}
			else if (assignedSlot != CosmeticsController.CosmeticCategory.Paw || !isTwoHanded)
			{
				GTDev.LogWarning("NetworkedWearable calling ToggleRightWearableStateBool on one handed object " + base.gameObject.name + ". Please use ToggleWearableStateBool instead");
				ToggleWearableStateBool();
			}
			else
			{
				rightHandValue = !rightHandValue;
				myRig.WearablePackedStates = GTBitOps.WriteBit(myRig.WearablePackedStates, (int)rightSlot, rightHandValue);
				OnRightStateChanged();
			}
		}
	}

	public void SetLeftWearableStateBool(bool newState)
	{
		if (isLocal && IsSpawned && IsCategoryValid(assignedSlot) && !(myRig == null))
		{
			if (listenForChangesLocal)
			{
				GTDev.LogError("NetworkedWearable with listenForChangesLocal calling SetLeftWearableStateBool on object " + base.gameObject.name + ".You should not change state from a listener");
			}
			else if (assignedSlot != CosmeticsController.CosmeticCategory.Paw || !isTwoHanded)
			{
				GTDev.LogWarning("NetworkedWearable calling SetLeftWearableStateBool on one handed object " + base.gameObject.name + ". Please use SetWearableStateBool instead");
				SetWearableStateBool(newState);
			}
			else if (leftHandValue != newState)
			{
				leftHandValue = newState;
				myRig.WearablePackedStates = GTBitOps.WriteBit(myRig.WearablePackedStates, (int)leftSlot, leftHandValue);
				OnLeftStateChanged();
			}
		}
	}

	public void SetRightWearableStateBool(bool newState)
	{
		if (isLocal && IsSpawned && IsCategoryValid(assignedSlot) && !(myRig == null))
		{
			if (listenForChangesLocal)
			{
				GTDev.LogError("NetworkedWearable with listenForChangesLocal calling SetRightWearableStateBool on object " + base.gameObject.name + ".You should not change state from a listener");
			}
			else if (assignedSlot != CosmeticsController.CosmeticCategory.Paw || !isTwoHanded)
			{
				GTDev.LogWarning("NetworkedWearable calling SetRightWearableStateBool on one handed object " + base.gameObject.name + ". Please use SetWearableStateBool instead");
				SetWearableStateBool(newState);
			}
			else if (rightHandValue != newState)
			{
				rightHandValue = newState;
				myRig.WearablePackedStates = GTBitOps.WriteBit(myRig.WearablePackedStates, (int)rightSlot, rightHandValue);
				OnRightStateChanged();
			}
		}
	}

	public void OnDisable()
	{
		if (isLocal && !listenForChangesLocal)
		{
			SetWearableStateBool(newState: false);
		}
		else if (TickRunning)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
	}

	private void OnWearableStateChanged()
	{
		if (value)
		{
			OnWearableStateTrue?.Invoke();
		}
		else
		{
			OnWearableStateFalse?.Invoke();
		}
	}

	private void OnLeftStateChanged()
	{
		if (leftHandValue)
		{
			OnLeftWearableStateTrue?.Invoke();
		}
		else
		{
			OnLeftWearableStateFalse?.Invoke();
		}
	}

	private void OnRightStateChanged()
	{
		if (rightHandValue)
		{
			OnRightWearableStateTrue?.Invoke();
		}
		else
		{
			OnRightWearableStateFalse?.Invoke();
		}
	}

	public void OnSpawn(VRRig rig)
	{
		if (assignedSlot == CosmeticsController.CosmeticCategory.Paw && CosmeticSelectedSide == ECosmeticSelectSide.Both)
		{
			GTDev.LogWarning($"NetworkedWearable: Cosmetic {base.gameObject.name} with category {assignedSlot} has select side Both, assuming left side!");
		}
		if (!IsCategoryValid(assignedSlot))
		{
			GTDev.LogError($"NetworkedWearable: Cosmetic {base.gameObject.name} spawned with invalid category {assignedSlot}!");
		}
		myRig = rig;
		isLocal = rig.isLocal;
		wearableSlot = CosmeticCategoryToWearableSlot(assignedSlot, CosmeticSelectedSide != ECosmeticSelectSide.Right);
	}

	public void OnDespawn()
	{
	}

	public void Tick()
	{
		if ((isLocal && !listenForChangesLocal) || !IsSpawned)
		{
			return;
		}
		if (assignedSlot == CosmeticsController.CosmeticCategory.Paw && isTwoHanded)
		{
			bool flag = GTBitOps.ReadBit(myRig.WearablePackedStates, (int)leftSlot);
			if (leftHandValue != flag)
			{
				leftHandValue = flag;
				OnLeftStateChanged();
			}
			flag = GTBitOps.ReadBit(myRig.WearablePackedStates, (int)rightSlot);
			if (rightHandValue != flag)
			{
				rightHandValue = flag;
				OnRightStateChanged();
			}
		}
		else
		{
			bool flag2 = GTBitOps.ReadBit(myRig.WearablePackedStates, (int)wearableSlot);
			if (value != flag2)
			{
				value = flag2;
				OnWearableStateChanged();
			}
		}
	}

	public static bool IsCategoryValid(CosmeticsController.CosmeticCategory category)
	{
		switch (category)
		{
		case CosmeticsController.CosmeticCategory.Hat:
		case CosmeticsController.CosmeticCategory.Badge:
		case CosmeticsController.CosmeticCategory.Face:
		case CosmeticsController.CosmeticCategory.Paw:
		case CosmeticsController.CosmeticCategory.Fur:
		case CosmeticsController.CosmeticCategory.Shirt:
		case CosmeticsController.CosmeticCategory.Pants:
			return true;
		default:
			return false;
		}
	}

	private VRRig.WearablePackedStateSlots CosmeticCategoryToWearableSlot(CosmeticsController.CosmeticCategory category, bool isLeft)
	{
		switch (category)
		{
		case CosmeticsController.CosmeticCategory.Hat:
			return VRRig.WearablePackedStateSlots.Hat;
		case CosmeticsController.CosmeticCategory.Badge:
			return VRRig.WearablePackedStateSlots.Badge;
		case CosmeticsController.CosmeticCategory.Face:
			return VRRig.WearablePackedStateSlots.Face;
		case CosmeticsController.CosmeticCategory.Paw:
			if (!isLeft)
			{
				return VRRig.WearablePackedStateSlots.RightHand;
			}
			return VRRig.WearablePackedStateSlots.LeftHand;
		case CosmeticsController.CosmeticCategory.Fur:
			return VRRig.WearablePackedStateSlots.Fur;
		case CosmeticsController.CosmeticCategory.Shirt:
			return VRRig.WearablePackedStateSlots.Shirt;
		case CosmeticsController.CosmeticCategory.Pants:
			return VRRig.WearablePackedStateSlots.Pants1;
		default:
			GTDev.LogWarning($"NetworkedWearable: {category} item cannot set wearable state");
			return VRRig.WearablePackedStateSlots.Hat;
		}
	}
}
