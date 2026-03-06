using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

internal class ConnectedControllerHandler : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private HandTransformFollowOffset rightHandFollower;

	[SerializeField]
	private HandTransformFollowOffset leftHandFollower;

	[SerializeField]
	private XRController rightXRController;

	[SerializeField]
	private XRController leftXRController;

	[SerializeField]
	private GorillaSnapTurn snapTurnController;

	private List<XRController> rightControllerList;

	private List<XRController> leftcontrollerList;

	[SerializeField]
	private bool overrideEnabled;

	private bool overrideLeftEnable;

	private bool overrideRightEnable;

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
	private float stoppedDurationMinimum = 5f;

	[SerializeField]
	private OverrideControllers overriddenControllers;

	private float timeStoppedMovingLeft;

	private float timeStoppedMovingRight;

	public Vector3 oculusRightPosOffset = new Vector3(0f, -0.27f, 0.09f);

	public Quaternion oculusRightRotOffset = Quaternion.Euler(275f, 270f, -5f);

	public Vector3 oculusLeftPosOffset = new Vector3(-0f, -0.27f, 0.09f);

	public Quaternion oculusLeftRotOffset = Quaternion.Euler(275f, 90f, 5f);

	[field: OnEnterPlay_SetNull]
	public static ConnectedControllerHandler Instance { get; private set; }

	[SerializeField]
	private bool rightValid
	{
		get
		{
			if (!overrideRightEnable)
			{
				if (ControllerInputPoller.instance.RightHandValid)
				{
					return !overriddenControllers.HasFlag(OverrideControllers.RightController);
				}
				return false;
			}
			return true;
		}
	}

	[SerializeField]
	private bool leftValid
	{
		get
		{
			if (!overrideLeftEnable)
			{
				if (ControllerInputPoller.instance.LeftHandValid)
				{
					return !overriddenControllers.HasFlag(OverrideControllers.LeftController);
				}
				return false;
			}
			return true;
		}
	}

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

	public void SetRightHandOffsets(Vector3 positionOffset, Quaternion rotationOffset)
	{
		rightHandFollower.positionOffset = positionOffset;
		rightHandFollower.rotationOffset = rotationOffset;
	}

	public void SetLeftHandOffsets(Vector3 positionOffset, Quaternion rotationOffset)
	{
		leftHandFollower.positionOffset = positionOffset;
		leftHandFollower.rotationOffset = rotationOffset;
	}

	public void SetOculusOffsets(bool rightHand = true, bool leftHand = true)
	{
		if (rightHand)
		{
			SetRightHandOffsets(oculusRightPosOffset, oculusRightRotOffset);
		}
		if (leftHand)
		{
			SetLeftHandOffsets(oculusLeftPosOffset, oculusLeftRotOffset);
		}
	}

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this);
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this);
	}

	private void OnDestroy()
	{
		if (Instance != null && Instance == this)
		{
			Instance = null;
		}
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

	public void SliceUpdate()
	{
		if (playerHandler.inOverlay)
		{
			return;
		}
		updateControllers = false;
		if (ControllerInputPoller.instance.RightHandValid)
		{
			tempRightPos = ControllerInputPoller.DevicePosition(XRNode.RightHand);
			if (tempRightPos == lastRightPos)
			{
				if (Time.time > timeStoppedMovingRight + stoppedDurationMinimum && !overriddenControllers.HasFlag(OverrideControllers.RightController))
				{
					overriddenControllers |= OverrideControllers.RightController;
					updateControllers = true;
				}
			}
			else
			{
				timeStoppedMovingRight = Time.time;
				if (overriddenControllers.HasFlag(OverrideControllers.RightController))
				{
					overriddenControllers &= ~OverrideControllers.RightController;
					updateControllers = true;
				}
			}
			lastRightPos = tempRightPos;
		}
		if (ControllerInputPoller.instance.LeftHandValid)
		{
			tempLeftPos = ControllerInputPoller.DevicePosition(XRNode.LeftHand);
			if (tempLeftPos == lastLeftPos)
			{
				if (Time.time > timeStoppedMovingLeft + stoppedDurationMinimum && !overriddenControllers.HasFlag(OverrideControllers.LeftController))
				{
					overriddenControllers |= OverrideControllers.LeftController;
					updateControllers = true;
				}
			}
			else
			{
				timeStoppedMovingLeft = Time.time;
				if (overriddenControllers.HasFlag(OverrideControllers.LeftController))
				{
					overriddenControllers &= ~OverrideControllers.LeftController;
					updateControllers = true;
				}
			}
			lastLeftPos = tempLeftPos;
		}
		if ((!leftXRController.enabled && leftValid) || (!rightXRController.enabled && rightValid))
		{
			updateControllers = true;
		}
		if (updateControllers)
		{
			overrideEnabled = overriddenControllers != OverrideControllers.None;
			UpdateControllerStates();
		}
	}

	private void UpdateControllerStates()
	{
		leftXRController.enabled = leftValid;
		rightXRController.enabled = rightValid;
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
