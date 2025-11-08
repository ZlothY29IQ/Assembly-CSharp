using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PrimaryButtonWatcher : MonoBehaviour
{
	public PrimaryButtonEvent primaryButtonPress;

	private bool lastButtonState;

	private List<InputDevice> devicesWithPrimaryButton;

	private void Awake()
	{
		if (primaryButtonPress == null)
		{
			primaryButtonPress = new PrimaryButtonEvent();
		}
		devicesWithPrimaryButton = new List<InputDevice>();
	}

	private void OnEnable()
	{
		List<InputDevice> list = new List<InputDevice>();
		InputDevices.GetDevices(list);
		foreach (InputDevice item in list)
		{
			InputDevices_deviceConnected(item);
		}
		InputDevices.deviceConnected += InputDevices_deviceConnected;
		InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
	}

	private void OnDisable()
	{
		InputDevices.deviceConnected -= InputDevices_deviceConnected;
		InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
		devicesWithPrimaryButton.Clear();
	}

	private void InputDevices_deviceConnected(InputDevice device)
	{
		if (device.TryGetFeatureValue(CommonUsages.primaryButton, out var _))
		{
			devicesWithPrimaryButton.Add(device);
		}
	}

	private void InputDevices_deviceDisconnected(InputDevice device)
	{
		if (devicesWithPrimaryButton.Contains(device))
		{
			devicesWithPrimaryButton.Remove(device);
		}
	}

	private void Update()
	{
		bool flag = false;
		foreach (InputDevice item in devicesWithPrimaryButton)
		{
			bool value = false;
			flag = (item.TryGetFeatureValue(CommonUsages.primaryButton, out value) && value) || flag;
		}
		if (flag != lastButtonState)
		{
			primaryButtonPress.Invoke(flag);
			lastButtonState = flag;
		}
	}
}
