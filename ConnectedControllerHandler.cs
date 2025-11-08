using System.Collections;
using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

internal class ConnectedControllerHandler : MonoBehaviour
{
	[SerializeField]
	private HandTransformFollowOffest rightHandFollower;

	[SerializeField]
	private HandTransformFollowOffest leftHandFollower;

	[SerializeField]
	private XRController rightXRController;

	[SerializeField]
	private XRController leftXRController;

	[SerializeField]
	private GorillaSnapTurn snapTurnController;

	private List<XRController> rightControllerList;

	private List<XRController> leftcontrollerList;

	private const InputDeviceCharacteristics rightCharecteristics = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right;

	private const InputDeviceCharacteristics leftCharecteristics = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left;

	private bool rightControllerValid = true;

	private bool leftControllerValid = true;

	[SerializeField]
	private bool rightValid = true;

	[SerializeField]
	private bool leftValid = true;

	[SerializeField]
	private Vector3 lastRightPos;

	[SerializeField]
	private Vector3 lastLeftPos;

	private Vector3 tempRightPos;

	private Vector3 tempLeftPos;

	private bool updateControllers;

	private GTPlayer playerHandler;

	[Tooltip("The rate at which controllers are checked to be moving, if they not moving, overrides and enables one hand mode")]
	[SerializeField]
	private float overridePollRate = 15f;

	[SerializeField]
	private bool overrideEnabled;

	[SerializeField]
	private OverrideControllers overrideController;

	[field: OnEnterPlay_SetNull]
	public static ConnectedControllerHandler Instance { get; private set; }

	public bool RightValid => rightValid;

	public bool LeftValid => leftValid;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Object.Destroy(this);
			return;
		}
		Instance = this;
		if (leftHandFollower == null || rightHandFollower == null || rightXRController == null || leftXRController == null || snapTurnController == null)
		{
			base.enabled = false;
			return;
		}
		rightControllerList = new List<XRController>();
		leftcontrollerList = new List<XRController>();
		rightControllerList.Add(rightXRController);
		leftcontrollerList.Add(leftXRController);
		InputDevice deviceAtXRNode = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		InputDevice deviceAtXRNode2 = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
		Debug.Log($"right controller? {(deviceAtXRNode.characteristics & (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right)) == (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right)}");
		rightControllerValid = deviceAtXRNode.isValid;
		leftControllerValid = deviceAtXRNode2.isValid;
		InputDevices.deviceConnected += DeviceConnected;
		InputDevices.deviceDisconnected += DeviceDisconnected;
		UpdateControllerStates();
	}

	private void Start()
	{
		if (leftHandFollower != null && rightHandFollower != null && !(leftXRController == null) && !(rightXRController == null) && !(snapTurnController == null))
		{
			playerHandler = GTPlayer.Instance;
			rightHandFollower.followTransform = GorillaTagger.Instance.offlineVRRig.transform;
			leftHandFollower.followTransform = GorillaTagger.Instance.offlineVRRig.transform;
		}
	}

	private void OnEnable()
	{
		StartCoroutine(ControllerValidator());
	}

	private void OnDisable()
	{
		StopCoroutine(ControllerValidator());
	}

	private void OnDestroy()
	{
		if (Instance != null && Instance == this)
		{
			Instance = null;
		}
		InputDevices.deviceConnected -= DeviceConnected;
		InputDevices.deviceDisconnected -= DeviceDisconnected;
	}

	private void LateUpdate()
	{
		if (!rightValid)
		{
			rightHandFollower.UpdatePositionRotation();
		}
		if (!leftValid)
		{
			leftHandFollower.UpdatePositionRotation();
		}
	}

	private IEnumerator ControllerValidator()
	{
		yield return null;
		lastRightPos = ControllerInputPoller.DevicePosition(XRNode.RightHand);
		lastLeftPos = ControllerInputPoller.DevicePosition(XRNode.LeftHand);
		while (true)
		{
			yield return new WaitForSeconds(overridePollRate);
			updateControllers = false;
			if (playerHandler.inOverlay)
			{
				continue;
			}
			if (rightControllerValid)
			{
				tempRightPos = ControllerInputPoller.DevicePosition(XRNode.RightHand);
				if (tempRightPos == lastRightPos)
				{
					if ((overrideController & OverrideControllers.RightController) != OverrideControllers.RightController)
					{
						overrideController |= OverrideControllers.RightController;
						updateControllers = true;
					}
				}
				else if ((overrideController & OverrideControllers.RightController) == OverrideControllers.RightController)
				{
					overrideController &= ~OverrideControllers.RightController;
					updateControllers = true;
				}
				lastRightPos = tempRightPos;
			}
			if (leftControllerValid)
			{
				tempLeftPos = ControllerInputPoller.DevicePosition(XRNode.LeftHand);
				if (tempLeftPos == lastLeftPos)
				{
					if ((overrideController & OverrideControllers.LeftController) != OverrideControllers.LeftController)
					{
						overrideController |= OverrideControllers.LeftController;
						updateControllers = true;
					}
				}
				else if ((overrideController & OverrideControllers.LeftController) == OverrideControllers.LeftController)
				{
					overrideController &= ~OverrideControllers.LeftController;
					updateControllers = true;
				}
				lastLeftPos = tempLeftPos;
			}
			if (updateControllers)
			{
				overrideEnabled = overrideController != OverrideControllers.None;
				UpdateControllerStates();
			}
		}
	}

	private void DeviceDisconnected(InputDevice device)
	{
		if ((device.characteristics & (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right)) == (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right))
		{
			rightControllerValid = false;
		}
		if ((device.characteristics & (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left)) == (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left))
		{
			leftControllerValid = false;
		}
		UpdateControllerStates();
	}

	private void DeviceConnected(InputDevice device)
	{
		if ((device.characteristics & (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right)) == (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right))
		{
			rightControllerValid = true;
		}
		if ((device.characteristics & (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left)) == (InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left))
		{
			leftControllerValid = true;
		}
		UpdateControllerStates();
	}

	private void UpdateControllerStates()
	{
		if (overrideEnabled && overrideController != 0)
		{
			rightValid = rightControllerValid && (overrideController & OverrideControllers.RightController) != OverrideControllers.RightController;
			leftValid = leftControllerValid && (overrideController & OverrideControllers.LeftController) != OverrideControllers.LeftController;
		}
		else
		{
			rightValid = rightControllerValid;
			leftValid = leftControllerValid;
		}
		rightXRController.enabled = rightValid;
		leftXRController.enabled = leftValid;
		AssignSnapturnController();
	}

	private void AssignSnapturnController()
	{
		if (!leftValid && rightValid)
		{
			snapTurnController.controllers = rightControllerList;
		}
		else if (!rightValid && leftValid)
		{
			snapTurnController.controllers = leftcontrollerList;
		}
		else
		{
			snapTurnController.controllers = rightControllerList;
		}
	}

	public bool GetValidForXRNode(XRNode controllerNode)
	{
		return controllerNode switch
		{
			XRNode.LeftHand => leftValid, 
			XRNode.RightHand => rightValid, 
			_ => true, 
		};
	}
}
