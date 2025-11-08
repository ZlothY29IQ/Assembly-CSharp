using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace GorillaTag.Cosmetics;

public class ControllerButtonEvent : MonoBehaviour, ISpawnable
{
	private enum ButtonType
	{
		trigger,
		primary,
		secondary,
		grip
	}

	[SerializeField]
	private float gripValue = 0.75f;

	[SerializeField]
	private float gripReleaseValue = 0.01f;

	[SerializeField]
	private float triggerValue = 0.75f;

	[SerializeField]
	private float triggerReleaseValue = 0.01f;

	[SerializeField]
	private ButtonType buttonType;

	[Tooltip("How many frames should pass to trigger a press stayed button")]
	[SerializeField]
	private int frameInterval = 20;

	public UnityEvent<bool, float> onButtonPressed;

	public UnityEvent<bool, float> onButtonReleased;

	public UnityEvent<bool, float> onButtonPressStayed;

	private float triggerLastValue;

	private float gripLastValue;

	private bool primaryLastValue;

	private bool secondaryLastValue;

	private int frameCounter;

	private bool inLeftHand;

	private VRRig myRig;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void OnSpawn(VRRig rig)
	{
		myRig = rig;
	}

	public void OnDespawn()
	{
	}

	private bool IsMyItem()
	{
		if (myRig != null)
		{
			return myRig.isOfflineVRRig;
		}
		return false;
	}

	private void Awake()
	{
		triggerLastValue = 0f;
		gripLastValue = 0f;
		primaryLastValue = false;
		secondaryLastValue = false;
		frameCounter = 0;
	}

	public void LateUpdate()
	{
		if (!IsMyItem())
		{
			return;
		}
		XRNode node = (inLeftHand ? XRNode.LeftHand : XRNode.RightHand);
		switch (buttonType)
		{
		case ButtonType.trigger:
		{
			float num2 = ControllerInputPoller.TriggerFloat(node);
			if (num2 > triggerValue)
			{
				frameCounter++;
			}
			if (num2 > triggerValue && triggerLastValue < triggerValue)
			{
				onButtonPressed?.Invoke(inLeftHand, num2);
			}
			else if (num2 <= triggerReleaseValue && triggerLastValue > triggerReleaseValue)
			{
				onButtonReleased?.Invoke(inLeftHand, num2);
				frameCounter = 0;
			}
			else if (num2 > triggerValue && triggerLastValue >= triggerValue && frameCounter % frameInterval == 0)
			{
				onButtonPressStayed?.Invoke(inLeftHand, num2);
				frameCounter = 0;
			}
			triggerLastValue = num2;
			break;
		}
		case ButtonType.primary:
		{
			bool flag2 = ControllerInputPoller.PrimaryButtonPress(node);
			if (flag2)
			{
				frameCounter++;
			}
			if (flag2 && !primaryLastValue)
			{
				onButtonPressed?.Invoke(inLeftHand, 1f);
			}
			else if (!flag2 && primaryLastValue)
			{
				onButtonReleased?.Invoke(inLeftHand, 0f);
				frameCounter = 0;
			}
			else if (flag2 && primaryLastValue && frameCounter % frameInterval == 0)
			{
				onButtonPressStayed?.Invoke(inLeftHand, 1f);
				frameCounter = 0;
			}
			primaryLastValue = flag2;
			break;
		}
		case ButtonType.secondary:
		{
			bool flag = ControllerInputPoller.SecondaryButtonPress(node);
			if (flag)
			{
				frameCounter++;
			}
			if (flag && !secondaryLastValue)
			{
				onButtonPressed?.Invoke(inLeftHand, 1f);
			}
			else if (!flag && secondaryLastValue)
			{
				onButtonReleased?.Invoke(inLeftHand, 0f);
				frameCounter = 0;
			}
			else if (flag && secondaryLastValue && frameCounter % frameInterval == 0)
			{
				onButtonPressStayed?.Invoke(inLeftHand, 1f);
				frameCounter = 0;
			}
			secondaryLastValue = flag;
			break;
		}
		case ButtonType.grip:
		{
			float num = ControllerInputPoller.GripFloat(node);
			if (num > gripValue)
			{
				frameCounter++;
			}
			if (num > gripValue && gripLastValue < gripValue)
			{
				onButtonPressed?.Invoke(inLeftHand, num);
			}
			else if (num <= gripReleaseValue && gripLastValue > gripReleaseValue)
			{
				onButtonReleased?.Invoke(inLeftHand, num);
				frameCounter = 0;
			}
			else if (num > gripValue && gripLastValue >= gripValue && frameCounter % frameInterval == 0)
			{
				onButtonPressStayed?.Invoke(inLeftHand, num);
				frameCounter = 0;
			}
			gripLastValue = num;
			break;
		}
		}
	}
}
