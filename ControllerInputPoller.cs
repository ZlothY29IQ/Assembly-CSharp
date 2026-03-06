using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GorillaTag;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class ControllerInputPoller : MonoBehaviour
{
	private enum _EPressCadence
	{
		Start,
		End,
		Held
	}

	private struct _InputCallback(EControllerInputPressFlags flags, Action<EHandednessFlags> callback)
	{
		public readonly EControllerInputPressFlags flags = flags;

		public readonly Action<EHandednessFlags> callback = callback;
	}

	private struct _InputCallbacksCadenceInfo(int initialCapacity)
	{
		public readonly List<_InputCallback> list = new List<_InputCallback>(initialCapacity);
	}

	public const int k_defaultExecutionOrder = -400;

	[OnEnterPlay_SetNull]
	public static volatile ControllerInputPoller instance;

	public float leftControllerIndexFloat;

	public float leftControllerGripFloat;

	public float rightControllerIndexFloat;

	public float rightControllerGripFloat;

	public float leftControllerIndexTouch;

	public float rightControllerIndexTouch;

	public float rightStickLRFloat;

	public Vector3 leftControllerPosition;

	public Vector3 rightControllerPosition;

	public Vector3 headPosition;

	public Quaternion leftControllerRotation;

	public Quaternion rightControllerRotation;

	public Quaternion headRotation;

	public InputDevice leftControllerDevice;

	public InputDevice rightControllerDevice;

	public InputDevice headDevice;

	public bool leftControllerIsValid;

	public bool rightControllerIsValid;

	public bool handTrackingActive;

	public bool leftControllerPrimaryButton;

	public bool leftControllerSecondaryButton;

	public bool rightControllerPrimaryButton;

	public bool rightControllerSecondaryButton;

	public bool leftControllerPrimaryButtonTouch;

	public bool leftControllerSecondaryButtonTouch;

	public bool rightControllerPrimaryButtonTouch;

	public bool rightControllerSecondaryButtonTouch;

	public bool leftControllerTriggerButton;

	public bool rightControllerTriggerButton;

	public bool leftGrab;

	public bool leftGrabRelease;

	public bool rightGrab;

	public bool rightGrabRelease;

	public bool leftGrabMomentary;

	public bool leftGrabReleaseMomentary;

	public bool rightGrabMomentary;

	public bool rightGrabReleaseMomentary;

	private bool _leftIndexPressed;

	private bool _leftIndexReleased;

	private bool _rightIndexPressed;

	private bool _rightIndexReleased;

	private bool _leftIndexPressedThisFrame;

	private bool _leftIndexReleasedThisFrame;

	private bool _rightIndexPressedThisFrame;

	private bool _rightIndexReleasedThisFrame;

	private Vector3 _leftVelocity;

	private Vector3 _rightVelocity;

	private Vector3 _leftAngularVelocity;

	private Vector3 _rightAngularVelocity;

	public Vector2 leftControllerPrimary2DAxis;

	public Vector2 rightControllerPrimary2DAxis;

	public AnimationCurve handTriggerCurve;

	public AnimationCurve handGripCurve;

	private List<Action> onUpdate = new List<Action>();

	private List<Action> onUpdateNext = new List<Action>();

	private bool didModifyOnUpdate;

	public Vector3 leftHandOffset = new Vector3(0.01f, -0.16f, 0f);

	public Quaternion leftHandRotation = Quaternion.Euler(89f, 6f, 11f);

	public Vector3 rightHandOffset = new Vector3(-0.01f, -0.16f, 0f);

	public Quaternion rightHandRotation = Quaternion.Euler(89f, 6f, 11f);

	private static _InputCallbacksCadenceInfo _g_callbacks_onPressStart = new _InputCallbacksCadenceInfo(32);

	private static _InputCallbacksCadenceInfo _g_callbacks_onPressEnd = new _InputCallbacksCadenceInfo(32);

	private static _InputCallbacksCadenceInfo _g_callbacks_onPressUpdate = new _InputCallbacksCadenceInfo(32);

	public bool LeftHandValid
	{
		get
		{
			if (!leftControllerIsValid)
			{
				return handTrackingActive;
			}
			return true;
		}
	}

	public bool RightHandValid
	{
		get
		{
			if (!rightControllerIsValid)
			{
				return handTrackingActive;
			}
			return true;
		}
	}

	[DebugReadout]
	public bool leftIndexPressed => _leftIndexPressed;

	[DebugReadout]
	public bool leftIndexReleased => _leftIndexReleased;

	[DebugReadout]
	public bool rightIndexPressed => _rightIndexPressed;

	[DebugReadout]
	public bool rightIndexReleased => _rightIndexReleased;

	[DebugReadout]
	public bool leftIndexPressedThisFrame => _leftIndexPressedThisFrame;

	[DebugReadout]
	public bool leftIndexReleasedThisFrame => _leftIndexReleasedThisFrame;

	[DebugReadout]
	public bool rightIndexPressedThisFrame => _rightIndexPressedThisFrame;

	[DebugReadout]
	public bool rightIndexReleasedThisFrame => _rightIndexReleasedThisFrame;

	[DebugReadout]
	public Vector3 leftVelocity => _leftVelocity;

	[DebugReadout]
	public Vector3 rightVelocity => _rightVelocity;

	[DebugReadout]
	public Vector3 leftAngularVelocity => _leftAngularVelocity;

	[DebugReadout]
	public Vector3 rightAngularVelocity => _rightAngularVelocity;

	public GorillaControllerType controllerType { get; private set; }

	public EControllerInputPressFlags leftPressFlags { get; private set; }

	public EControllerInputPressFlags rightPressFlags { get; private set; }

	public EControllerInputPressFlags leftPressFlagsLastFrame { get; private set; }

	public EControllerInputPressFlags rightPressFlagsLastFrame { get; private set; }

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public static void AddUpdateCallback(Action callback)
	{
		if (!instance.didModifyOnUpdate)
		{
			instance.onUpdateNext.Clear();
			instance.onUpdateNext.AddRange(instance.onUpdate);
			instance.didModifyOnUpdate = true;
		}
		instance.onUpdateNext.Add(callback);
	}

	public static void RemoveUpdateCallback(Action callback)
	{
		if (!instance.didModifyOnUpdate)
		{
			instance.onUpdateNext.Clear();
			instance.onUpdateNext.AddRange(instance.onUpdate);
			instance.didModifyOnUpdate = true;
		}
		instance.onUpdateNext.Remove(callback);
	}

	public void LateUpdate()
	{
		leftControllerIsValid = leftControllerDevice.isValid;
		if (!leftControllerIsValid)
		{
			leftControllerDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			leftControllerIsValid = leftControllerDevice.isValid;
			if (leftControllerIsValid)
			{
				controllerType = GorillaControllerType.OCULUS_DEFAULT;
				if (leftControllerDevice.name.ToLower().Contains("knuckles"))
				{
					controllerType = GorillaControllerType.INDEX;
				}
				Debug.Log($"Found left controller: {leftControllerDevice.name} ControllerType: {controllerType}");
			}
		}
		rightControllerIsValid = rightControllerDevice.isValid;
		if (!rightControllerIsValid)
		{
			rightControllerDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		}
		if (!headDevice.isValid)
		{
			headDevice = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
		}
		_ = leftControllerDevice;
		_ = rightControllerDevice;
		_ = headDevice;
		leftControllerDevice.TryGetFeatureValue(CommonUsages.primaryButton, out leftControllerPrimaryButton);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out leftControllerSecondaryButton);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.primaryTouch, out leftControllerPrimaryButtonTouch);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.secondaryTouch, out leftControllerSecondaryButtonTouch);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.grip, out leftControllerGripFloat);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.trigger, out leftControllerIndexFloat);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.devicePosition, out leftControllerPosition);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out leftControllerRotation);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftControllerPrimary2DAxis);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.triggerButton, out leftControllerTriggerButton);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.primaryButton, out rightControllerPrimaryButton);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out rightControllerSecondaryButton);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.primaryTouch, out rightControllerPrimaryButtonTouch);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.secondaryTouch, out rightControllerSecondaryButtonTouch);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.grip, out rightControllerGripFloat);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.trigger, out rightControllerIndexFloat);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.devicePosition, out rightControllerPosition);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out rightControllerRotation);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightControllerPrimary2DAxis);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.triggerButton, out rightControllerTriggerButton);
		leftControllerPrimaryButton = SteamVR_Actions.gorillaTag_LeftPrimaryClick.GetState(SteamVR_Input_Sources.LeftHand);
		leftControllerSecondaryButton = SteamVR_Actions.gorillaTag_LeftSecondaryClick.GetState(SteamVR_Input_Sources.LeftHand);
		leftControllerPrimaryButtonTouch = SteamVR_Actions.gorillaTag_LeftPrimaryTouch.GetState(SteamVR_Input_Sources.LeftHand);
		leftControllerSecondaryButtonTouch = SteamVR_Actions.gorillaTag_LeftSecondaryTouch.GetState(SteamVR_Input_Sources.LeftHand);
		leftControllerGripFloat = SteamVR_Actions.gorillaTag_LeftGripFloat.GetAxis(SteamVR_Input_Sources.LeftHand);
		leftControllerIndexFloat = SteamVR_Actions.gorillaTag_LeftTriggerFloat.GetAxis(SteamVR_Input_Sources.LeftHand);
		leftControllerTriggerButton = SteamVR_Actions.gorillaTag_LeftTriggerClick.GetState(SteamVR_Input_Sources.LeftHand);
		leftControllerPrimary2DAxis = SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.GetAxis(SteamVR_Input_Sources.LeftHand);
		rightControllerPrimaryButton = SteamVR_Actions.gorillaTag_RightPrimaryClick.GetState(SteamVR_Input_Sources.RightHand);
		rightControllerSecondaryButton = SteamVR_Actions.gorillaTag_RightSecondaryClick.GetState(SteamVR_Input_Sources.RightHand);
		rightControllerPrimaryButtonTouch = SteamVR_Actions.gorillaTag_RightPrimaryTouch.GetState(SteamVR_Input_Sources.RightHand);
		rightControllerSecondaryButtonTouch = SteamVR_Actions.gorillaTag_RightSecondaryTouch.GetState(SteamVR_Input_Sources.RightHand);
		rightControllerGripFloat = SteamVR_Actions.gorillaTag_RightGripFloat.GetAxis(SteamVR_Input_Sources.RightHand);
		rightControllerIndexFloat = SteamVR_Actions.gorillaTag_RightTriggerFloat.GetAxis(SteamVR_Input_Sources.RightHand);
		rightControllerTriggerButton = SteamVR_Actions.gorillaTag_RightTriggerClick.GetState(SteamVR_Input_Sources.RightHand);
		rightControllerPrimary2DAxis = SteamVR_Actions.gorillaTag_RightJoystick2DAxis.GetAxis(SteamVR_Input_Sources.RightHand);
		headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out headPosition);
		headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out headRotation);
		CalculateGrabState(leftControllerIndexFloat, ref _leftIndexPressed, ref _leftIndexReleased, out _leftIndexPressedThisFrame, out _leftIndexReleasedThisFrame, 0.75f, 0.65f);
		CalculateGrabState(rightControllerIndexFloat, ref _rightIndexPressed, ref _rightIndexReleased, out _rightIndexPressedThisFrame, out _rightIndexReleasedThisFrame, 0.75f, 0.65f);
		if (controllerType == GorillaControllerType.OCULUS_DEFAULT)
		{
			CalculateGrabState(leftControllerGripFloat, ref leftGrab, ref leftGrabRelease, out leftGrabMomentary, out leftGrabReleaseMomentary, 0.75f, 0.65f);
			CalculateGrabState(rightControllerGripFloat, ref rightGrab, ref rightGrabRelease, out rightGrabMomentary, out rightGrabReleaseMomentary, 0.75f, 0.65f);
		}
		else if (controllerType == GorillaControllerType.INDEX)
		{
			CalculateGrabState(leftControllerGripFloat, ref leftGrab, ref leftGrabRelease, out leftGrabMomentary, out leftGrabReleaseMomentary, 0.1f, 0.01f);
			CalculateGrabState(rightControllerGripFloat, ref rightGrab, ref rightGrabRelease, out rightGrabMomentary, out rightGrabReleaseMomentary, 0.1f, 0.01f);
		}
		handTrackingActive = false;
		leftControllerDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out _leftVelocity);
		leftControllerDevice.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out _leftAngularVelocity);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out _rightVelocity);
		rightControllerDevice.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out _rightAngularVelocity);
		_UpdatePressFlags();
		if (didModifyOnUpdate)
		{
			List<Action> list = onUpdateNext;
			List<Action> list2 = onUpdate;
			onUpdate = list;
			onUpdateNext = list2;
			didModifyOnUpdate = false;
		}
		foreach (Action item in onUpdate)
		{
			item();
		}
	}

	private void CalculateGrabState(float grabValue, ref bool grab, ref bool grabRelease, out bool grabMomentary, out bool grabReleaseMomentary, float grabThreshold, float grabReleaseThreshold)
	{
		bool flag = grabValue >= grabThreshold;
		bool flag2 = grabValue <= grabReleaseThreshold;
		grabMomentary = flag && !grab;
		grabReleaseMomentary = flag2 && !grabRelease;
		grab = flag;
		grabRelease = flag2;
	}

	public void RecalculateGrabState()
	{
		CalculateGrabState(leftControllerIndexFloat, ref _leftIndexPressed, ref _leftIndexReleased, out _leftIndexPressedThisFrame, out _leftIndexReleasedThisFrame, 0.75f, 0.65f);
		CalculateGrabState(rightControllerIndexFloat, ref _rightIndexPressed, ref _rightIndexReleased, out _rightIndexPressedThisFrame, out _rightIndexReleasedThisFrame, 0.75f, 0.65f);
		if (controllerType == GorillaControllerType.OCULUS_DEFAULT)
		{
			CalculateGrabState(leftControllerGripFloat, ref leftGrab, ref leftGrabRelease, out leftGrabMomentary, out leftGrabReleaseMomentary, 0.75f, 0.65f);
			CalculateGrabState(rightControllerGripFloat, ref rightGrab, ref rightGrabRelease, out rightGrabMomentary, out rightGrabReleaseMomentary, 0.75f, 0.65f);
		}
		else if (controllerType == GorillaControllerType.INDEX)
		{
			CalculateGrabState(leftControllerGripFloat, ref leftGrab, ref leftGrabRelease, out leftGrabMomentary, out leftGrabReleaseMomentary, 0.1f, 0.01f);
			CalculateGrabState(rightControllerGripFloat, ref rightGrab, ref rightGrabRelease, out rightGrabMomentary, out rightGrabReleaseMomentary, 0.1f, 0.01f);
		}
	}

	public static bool GetIndexPressed(XRNode node)
	{
		return node switch
		{
			XRNode.RightHand => instance.rightIndexPressed, 
			XRNode.LeftHand => instance.leftIndexPressed, 
			_ => false, 
		};
	}

	public static bool GetIndexReleased(XRNode node)
	{
		return node switch
		{
			XRNode.RightHand => instance.rightIndexReleased, 
			XRNode.LeftHand => instance.leftIndexReleased, 
			_ => false, 
		};
	}

	public static bool GetIndexPressedThisFrame(XRNode node)
	{
		return node switch
		{
			XRNode.RightHand => instance.leftIndexPressedThisFrame, 
			XRNode.LeftHand => instance.leftIndexPressedThisFrame, 
			_ => false, 
		};
	}

	public static bool GetIndexReleasedThisFrame(XRNode node)
	{
		return node switch
		{
			XRNode.RightHand => instance.leftIndexReleasedThisFrame, 
			XRNode.LeftHand => instance.leftIndexReleasedThisFrame, 
			_ => false, 
		};
	}

	public static bool GetGrab(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftGrab, 
			XRNode.RightHand => instance.rightGrab, 
			_ => false, 
		};
	}

	public static bool GetGrabRelease(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftGrabRelease, 
			XRNode.RightHand => instance.rightGrabRelease, 
			_ => false, 
		};
	}

	public static bool GetGrabMomentary(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftGrabMomentary, 
			XRNode.RightHand => instance.rightGrabMomentary, 
			_ => false, 
		};
	}

	public static bool GetGrabReleaseMomentary(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftGrabReleaseMomentary, 
			XRNode.RightHand => instance.rightGrabReleaseMomentary, 
			_ => false, 
		};
	}

	public static Vector2 Primary2DAxis(XRNode node)
	{
		if (node == XRNode.LeftHand)
		{
			return instance.leftControllerPrimary2DAxis;
		}
		return instance.rightControllerPrimary2DAxis;
	}

	public static bool PrimaryButtonPress(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftControllerPrimaryButton, 
			XRNode.RightHand => instance.rightControllerPrimaryButton, 
			_ => false, 
		};
	}

	public static bool SecondaryButtonPress(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftControllerSecondaryButton, 
			XRNode.RightHand => instance.rightControllerSecondaryButton, 
			_ => false, 
		};
	}

	public static bool PrimaryButtonTouch(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftControllerPrimaryButtonTouch, 
			XRNode.RightHand => instance.rightControllerPrimaryButtonTouch, 
			_ => false, 
		};
	}

	public static bool SecondaryButtonTouch(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftControllerSecondaryButtonTouch, 
			XRNode.RightHand => instance.rightControllerSecondaryButtonTouch, 
			_ => false, 
		};
	}

	public static float GripFloat(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftControllerGripFloat, 
			XRNode.RightHand => instance.rightControllerGripFloat, 
			_ => 0f, 
		};
	}

	public static float TriggerFloat(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftControllerIndexFloat, 
			XRNode.RightHand => instance.rightControllerIndexFloat, 
			_ => 0f, 
		};
	}

	public static float TriggerTouch(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftControllerIndexTouch, 
			XRNode.RightHand => instance.rightControllerIndexTouch, 
			_ => 0f, 
		};
	}

	public static Vector3 DevicePosition(XRNode node)
	{
		return node switch
		{
			XRNode.Head => instance.headPosition, 
			XRNode.LeftHand => instance.leftControllerPosition, 
			XRNode.RightHand => instance.rightControllerPosition, 
			_ => Vector3.zero, 
		};
	}

	public static Quaternion DeviceRotation(XRNode node)
	{
		return node switch
		{
			XRNode.Head => instance.headRotation, 
			XRNode.LeftHand => instance.leftControllerRotation, 
			XRNode.RightHand => instance.rightControllerRotation, 
			_ => Quaternion.identity, 
		};
	}

	public static Vector3 DeviceVelocity(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftVelocity, 
			XRNode.RightHand => instance.rightVelocity, 
			_ => Vector3.zero, 
		};
	}

	public static Vector3 DeviceAngularVelocity(XRNode node)
	{
		return node switch
		{
			XRNode.LeftHand => instance.leftAngularVelocity, 
			XRNode.RightHand => instance.rightAngularVelocity, 
			_ => Vector3.zero, 
		};
	}

	public static bool PositionValid(XRNode node)
	{
		return node switch
		{
			XRNode.Head => instance.headDevice.isValid, 
			XRNode.LeftHand => instance.leftControllerDevice.isValid, 
			XRNode.RightHand => instance.rightControllerDevice.isValid, 
			_ => false, 
		};
	}

	public static bool HasPressFlags(XRNode node, EControllerInputPressFlags inputStateFlags)
	{
		EControllerInputPressFlags inputStateFlags2 = GetInputStateFlags(node);
		if (inputStateFlags != EControllerInputPressFlags.None)
		{
			return (inputStateFlags2 & inputStateFlags) == inputStateFlags;
		}
		return false;
	}

	public static EControllerInputPressFlags GetInputStateFlags(XRNode node)
	{
		return node switch
		{
			XRNode.RightHand => instance.rightPressFlags, 
			XRNode.LeftHand => instance.leftPressFlags, 
			_ => EControllerInputPressFlags.None, 
		};
	}

	public static void AddCallbackOnPressStart(EControllerInputPressFlags flags, Action<EHandednessFlags> callback)
	{
		_AddInputStateCallback(ref _g_callbacks_onPressStart, flags, callback);
	}

	public static void AddCallbackOnPressEnd(EControllerInputPressFlags flags, Action<EHandednessFlags> callback)
	{
		_AddInputStateCallback(ref _g_callbacks_onPressEnd, flags, callback);
	}

	public static void AddCallbackOnPressUpdate(EControllerInputPressFlags flags, Action<EHandednessFlags> callback)
	{
		_AddInputStateCallback(ref _g_callbacks_onPressUpdate, flags, callback);
	}

	private static void _AddInputStateCallback(ref _InputCallbacksCadenceInfo ref_callbacksInfo, EControllerInputPressFlags flags, Action<EHandednessFlags> callback)
	{
		if (callback != null && flags != EControllerInputPressFlags.None)
		{
			if (ref_callbacksInfo.list.Capacity <= ref_callbacksInfo.list.Count)
			{
				ref_callbacksInfo.list.Capacity = ref_callbacksInfo.list.Count * 2;
			}
			ref_callbacksInfo.list.Add(new _InputCallback(flags, callback));
		}
	}

	public static void RemoveCallbackOnPressStart(Action<EHandednessFlags> callback)
	{
		_RemoveInputStateCallback(ref _g_callbacks_onPressStart, callback);
	}

	public static void RemoveCallbackOnPressEnd(Action<EHandednessFlags> callback)
	{
		_RemoveInputStateCallback(ref _g_callbacks_onPressEnd, callback);
	}

	public static void RemoveCallbackOnPressUpdate(Action<EHandednessFlags> callback)
	{
		_RemoveInputStateCallback(ref _g_callbacks_onPressUpdate, callback);
	}

	private static void _RemoveInputStateCallback(ref _InputCallbacksCadenceInfo ref_callbacksInfo, Action<EHandednessFlags> callback)
	{
		if (callback != null)
		{
			ref_callbacksInfo.list.RemoveAll((_InputCallback sub) => sub.callback == callback);
		}
	}

	private void _UpdatePressFlags()
	{
		leftPressFlagsLastFrame = leftPressFlags;
		leftPressFlags = (EControllerInputPressFlags)((leftIndexPressed ? 1 : 0) | (leftGrab ? 2 : 0) | (leftControllerPrimaryButton ? 4 : 0) | (leftControllerSecondaryButton ? 8 : 0));
		rightPressFlagsLastFrame = rightPressFlags;
		rightPressFlags = (EControllerInputPressFlags)((rightIndexPressed ? 1 : 0) | (rightGrab ? 2 : 0) | (rightControllerPrimaryButton ? 4 : 0) | (rightControllerSecondaryButton ? 8 : 0));
		_UpdatePressFlags_Callbacks(ref _g_callbacks_onPressStart, _EPressCadence.Start, leftPressFlags, leftPressFlagsLastFrame, rightPressFlags, rightPressFlagsLastFrame);
		_UpdatePressFlags_Callbacks(ref _g_callbacks_onPressEnd, _EPressCadence.End, leftPressFlags, leftPressFlagsLastFrame, rightPressFlags, rightPressFlagsLastFrame);
		_UpdatePressFlags_Callbacks(ref _g_callbacks_onPressUpdate, _EPressCadence.Held, leftPressFlags, leftPressFlagsLastFrame, rightPressFlags, rightPressFlagsLastFrame);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void _UpdatePressFlags_Callbacks(ref _InputCallbacksCadenceInfo callbacksInfo, _EPressCadence cadence, EControllerInputPressFlags lFlags_now, EControllerInputPressFlags lFlags_old, EControllerInputPressFlags rFlags_now, EControllerInputPressFlags rFlags_old)
	{
		for (int i = 0; i < callbacksInfo.list.Count; i++)
		{
			EControllerInputPressFlags flags = callbacksInfo.list[i].flags;
			Action<EHandednessFlags> callback = callbacksInfo.list[i].callback;
			EHandednessFlags eHandednessFlags = _IsHandContributingToPressCadence(EHandednessFlags.Left, cadence, flags, lFlags_now, lFlags_old) | _IsHandContributingToPressCadence(EHandednessFlags.Right, cadence, flags, rFlags_now, rFlags_old);
			if (eHandednessFlags != EHandednessFlags.None && callback != null)
			{
				try
				{
					callbacksInfo.list[i].callback(eHandednessFlags);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static EHandednessFlags _IsHandContributingToPressCadence(EHandednessFlags hand, _EPressCadence pressCadence, EControllerInputPressFlags cbFlags, EControllerInputPressFlags flags_now, EControllerInputPressFlags flags_old)
	{
		if ((pressCadence != _EPressCadence.Held || (cbFlags & flags_now) != cbFlags) && (pressCadence != _EPressCadence.Start || (cbFlags & flags_now) != cbFlags || (cbFlags & flags_old) == cbFlags) && (pressCadence != _EPressCadence.End || (cbFlags & flags_now) == cbFlags || (cbFlags & flags_old) != cbFlags))
		{
			return EHandednessFlags.None;
		}
		return hand;
	}
}
