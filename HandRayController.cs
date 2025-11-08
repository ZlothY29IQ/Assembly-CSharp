using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandRayController : MonoBehaviour
{
	private enum HandSide
	{
		Left,
		Right
	}

	[OnEnterPlay_SetNull]
	private static HandRayController instance;

	[SerializeField]
	private XRRayInteractor _leftHandRay;

	[SerializeField]
	private XRRayInteractor _rightHandRay;

	private bool _hasInitialised;

	private HandSide ActiveHand = HandSide.Right;

	private XRRayInteractor _activeHandRay;

	private int _activationCounter;

	public static HandRayController Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Object.FindAnyObjectByType<HandRayController>();
				if (instance == null)
				{
					Debug.LogErrorFormat("[KID::UI::HAND_RAY_CONTROLLER] Not found in scene");
				}
			}
			return instance;
		}
	}

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Debug.LogErrorFormat(base.gameObject, "[KID::UI::HAND_RAY_CONTROLLER] Duplicate instance of HandRayController");
			Object.DestroyImmediate(this);
		}
		else
		{
			instance = this;
		}
	}

	private void Start()
	{
		XRRayInteractor leftHandRay = _leftHandRay;
		Transform attachTransform = (_leftHandRay.rayOriginTransform = KIDHandReference.LeftHand.transform);
		leftHandRay.attachTransform = attachTransform;
		XRRayInteractor rightHandRay = _rightHandRay;
		attachTransform = (_rightHandRay.rayOriginTransform = KIDHandReference.RightHand.transform);
		rightHandRay.attachTransform = attachTransform;
		DisableHandRays();
		_activationCounter = 0;
	}

	private void OnDisable()
	{
		DisableHandRays();
	}

	public void EnableHandRays()
	{
		if (_activationCounter == 0)
		{
			if ((bool)ControllerBehaviour.Instance)
			{
				ControllerBehaviour.Instance.OnAction += PostUpdate;
			}
			ToggleHands();
		}
		_activationCounter++;
	}

	public void DisableHandRays()
	{
		_activationCounter--;
		if (_activationCounter == 0)
		{
			if ((bool)ControllerBehaviour.Instance)
			{
				ControllerBehaviour.Instance.OnAction -= PostUpdate;
			}
			HideHands();
		}
	}

	public void PulseActiveHandray(float vibrationStrength, float vibrationDuration)
	{
		if (!(_activeHandRay == null))
		{
			_activeHandRay.SendHapticImpulse(vibrationStrength, vibrationDuration);
		}
	}

	private void PostUpdate()
	{
		if (!_hasInitialised)
		{
			return;
		}
		if (ActiveHand == HandSide.Left)
		{
			if (ControllerBehaviour.Instance.RightButtonDown)
			{
				ToggleHands();
			}
		}
		else if (ControllerBehaviour.Instance.LeftButtonDown)
		{
			ToggleHands();
		}
	}

	private void ToggleRightHandRay(bool enabled)
	{
		Debug.LogFormat($"[KID::UI::HAND_RAY_CONTROLLER] RIGHT Hand is: {_rightHandRay.gameObject.activeInHierarchy}. Setting to: {enabled}");
		_rightHandRay.gameObject.SetActive(enabled);
		if (enabled)
		{
			_activeHandRay = _rightHandRay;
		}
	}

	private void ToggleLeftHandRay(bool enabled)
	{
		Debug.LogFormat($"[KID::UI::HAND_RAY_CONTROLLER] LEFT Hand is: {_rightHandRay.gameObject.activeInHierarchy}. Setting to: {enabled}");
		_leftHandRay.gameObject.SetActive(enabled);
		if (enabled)
		{
			_activeHandRay = _leftHandRay;
		}
	}

	private void InitialiseHands()
	{
		Debug.Log("[KID::UI::HAND_RAY_CONTROLLER] Initialising Hands");
		ToggleRightHandRay(ActiveHand == HandSide.Right);
		ToggleLeftHandRay(ActiveHand == HandSide.Left);
		_hasInitialised = true;
	}

	private void ToggleHands()
	{
		if (!_hasInitialised)
		{
			InitialiseHands();
			return;
		}
		HandSide handSide = ((ActiveHand == HandSide.Left) ? HandSide.Right : HandSide.Left);
		Debug.LogFormat("[KID::UI::HAND_RAY_CONTROLLER] Setting ActiveHand FROM: [" + ActiveHand.ToString() + "] TO: [" + handSide.ToString() + "]");
		ActiveHand = handSide;
		ToggleRightHandRay(handSide == HandSide.Right);
		ToggleLeftHandRay(handSide == HandSide.Left);
	}

	private void HideHands()
	{
		ToggleRightHandRay(enabled: false);
		ToggleLeftHandRay(enabled: false);
		_hasInitialised = false;
		_activeHandRay = null;
	}
}
