using System;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.Events;

public class RadioButtonGroupWearable : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private bool AllowSelectNone = true;

	[SerializeField]
	private GorillaPressableButton[] buttons;

	[SerializeField]
	private UnityEvent<int> OnSelectionChanged;

	[Tooltip("This is to determine what bit to change in VRRig.WearablesPackedStates.")]
	[SerializeField]
	private VRRig.WearablePackedStateSlots assignedSlot = VRRig.WearablePackedStateSlots.Pants1;

	private int lastReportedState;

	private VRRig ownerRig;

	private GTBitOps.BitWriteInfo stateBitsWriteInfo;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	private void Start()
	{
		stateBitsWriteInfo = VRRig.WearablePackedStatesBitWriteInfos[(int)assignedSlot];
		if (ownerRig.isLocal)
		{
			return;
		}
		GorillaPressableButton[] array = buttons;
		for (int i = 0; i < array.Length; i++)
		{
			Collider component = array[i].GetComponent<Collider>();
			if (component != null)
			{
				component.enabled = false;
			}
		}
	}

	private void OnEnable()
	{
		SharedRefreshState();
	}

	private int GetCurrentState()
	{
		return GTBitOps.ReadBits(ownerRig.WearablePackedStates, stateBitsWriteInfo.index, stateBitsWriteInfo.valueMask);
	}

	private void Update()
	{
		if (!ownerRig.isLocal && lastReportedState != GetCurrentState())
		{
			SharedRefreshState();
		}
	}

	public void SharedRefreshState()
	{
		int currentState = GetCurrentState();
		int num = (AllowSelectNone ? (currentState - 1) : currentState);
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].isOn = num == i;
			buttons[i].UpdateColor();
		}
		if (lastReportedState != currentState)
		{
			lastReportedState = currentState;
			OnSelectionChanged.Invoke(currentState);
		}
	}

	public void OnPress(GorillaPressableButton button)
	{
		int currentState = GetCurrentState();
		int num = Array.IndexOf(buttons, button);
		if (AllowSelectNone)
		{
			num++;
		}
		int value = num;
		if (AllowSelectNone && num == currentState)
		{
			value = 0;
		}
		ownerRig.WearablePackedStates = GTBitOps.WriteBits(ownerRig.WearablePackedStates, stateBitsWriteInfo, value);
		SharedRefreshState();
	}

	public void OnSpawn(VRRig rig)
	{
		ownerRig = rig;
	}

	public void OnDespawn()
	{
	}
}
