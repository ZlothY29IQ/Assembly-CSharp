using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace GorillaTag.Cosmetics;

public class RCRemoteHoldable : TransferrableObject, ISnapTurnOverride
{
	public struct RCInput
	{
		public Vector2 joystick;

		public float trigger;

		public byte buttons;
	}

	[SerializeField]
	private Transform joystickTransform;

	[SerializeField]
	private Transform triggerTransform;

	[SerializeField]
	private Transform buttonTransform;

	private RCVehicle targetVehicle;

	private float joystickLeanDegrees = 30f;

	private float triggerPullDegrees = 40f;

	private float buttonPressDepth = 0.005f;

	private Quaternion initialJoystickRotation;

	private Quaternion initialTriggerRotation;

	private Quaternion initialButtonRotation;

	private Vector3 initialButtonPosition;

	private bool currentlyHeld;

	private XRNode xrNode;

	private RCInput currentInput;

	[HideInInspector]
	public RCCosmeticNetworkSync networkSync;

	private string networkSyncPrefabName = "RCCosmeticNetworkSync";

	private RubberDuckEvents _events;

	private object[] emptyArgs = new object[0];

	public XRNode XRNode => xrNode;

	public RCVehicle Vehicle => targetVehicle;

	public bool TurnOverrideActive()
	{
		if (base.gameObject.activeSelf && currentlyHeld)
		{
			return xrNode == XRNode.RightHand;
		}
		return false;
	}

	protected override void Awake()
	{
		base.Awake();
		initialJoystickRotation = joystickTransform.localRotation;
		initialTriggerRotation = triggerTransform.localRotation;
		if (buttonTransform != null)
		{
			initialButtonRotation = buttonTransform.localRotation;
			initialButtonPosition = buttonTransform.localPosition;
		}
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		if (!_TryFindRemoteVehicle())
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (_events.IsNotNull() || base.gameObject.TryGetComponent<RubberDuckEvents>(out _events))
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((base.myOnlineRig != null) ? base.myOnlineRig.creator : ((!(base.myRig != null)) ? null : ((base.myRig.creator != null) ? base.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
			else
			{
				Debug.LogError("Failed to get a reference to the Photon Player needed to hook up the cosmetic event");
			}
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnStartConnectionEvent);
		}
		WakeUpRemoteVehicle();
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		GorillaSnapTurn gorillaSnapTurn = ((GorillaTagger.Instance != null) ? GorillaTagger.Instance.GetComponent<GorillaSnapTurn>() : null);
		if (gorillaSnapTurn != null)
		{
			gorillaSnapTurn.UnsetTurningOverride(this);
		}
		if (networkSync != null && networkSync.photonView.IsMine)
		{
			PhotonNetwork.Destroy(networkSync.gameObject);
			networkSync = null;
		}
		if (_events.IsNotNull())
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnStartConnectionEvent);
			_events.Dispose();
			_events = null;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		GorillaSnapTurn gorillaSnapTurn = ((GorillaTagger.Instance != null) ? GorillaTagger.Instance.GetComponent<GorillaSnapTurn>() : null);
		if (gorillaSnapTurn != null)
		{
			gorillaSnapTurn.UnsetTurningOverride(this);
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		base.OnGrab(pointGrabbed, grabbingHand);
		if (PhotonNetwork.InRoom && networkSync != null && networkSync.photonView.Owner == null)
		{
			PhotonNetwork.Destroy(networkSync.gameObject);
			networkSync = null;
		}
		if (networkSync == null && PhotonNetwork.InRoom)
		{
			object[] data = new object[1] { myIndex };
			GameObject gameObject = PhotonNetwork.Instantiate(networkSyncPrefabName, Vector3.zero, Quaternion.identity, 0, data);
			networkSync = ((gameObject != null) ? gameObject.GetComponent<RCCosmeticNetworkSync>() : null);
		}
		currentlyHeld = true;
		bool flag = grabbingHand == EquipmentInteractor.instance.rightHand;
		xrNode = (flag ? XRNode.RightHand : XRNode.LeftHand);
		GorillaSnapTurn component = GorillaTagger.Instance.GetComponent<GorillaSnapTurn>();
		if (flag)
		{
			component.SetTurningOverride(this);
		}
		else
		{
			component.UnsetTurningOverride(this);
		}
		if (targetVehicle != null)
		{
			targetVehicle.StartConnection(this, networkSync);
		}
		if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
		{
			_events.Activate.RaiseOthers(emptyArgs);
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		currentlyHeld = false;
		currentInput = default(RCInput);
		if (targetVehicle != null)
		{
			targetVehicle.EndConnection();
		}
		joystickTransform.localRotation = initialJoystickRotation;
		triggerTransform.localRotation = initialTriggerRotation;
		GorillaTagger.Instance.GetComponent<GorillaSnapTurn>().UnsetTurningOverride(this);
		return true;
	}

	private void Update()
	{
		if (currentlyHeld)
		{
			currentInput.joystick = ControllerInputPoller.Primary2DAxis(xrNode);
			currentInput.trigger = ControllerInputPoller.TriggerFloat(xrNode);
			currentInput.buttons = (byte)(ControllerInputPoller.PrimaryButtonPress(xrNode) ? 1u : 0u);
			if (targetVehicle != null)
			{
				targetVehicle.ApplyRemoteControlInput(currentInput);
			}
			joystickTransform.localRotation = initialJoystickRotation * Quaternion.Euler(joystickLeanDegrees * currentInput.joystick.y, 0f, (0f - joystickLeanDegrees) * currentInput.joystick.x);
			triggerTransform.localRotation = initialTriggerRotation * Quaternion.Euler(triggerPullDegrees * currentInput.trigger, 0f, 0f);
			if (buttonTransform != null)
			{
				buttonTransform.localPosition = initialButtonPosition + initialButtonRotation * new Vector3(0f, 0f, (0f - buttonPressDepth) * (float)((currentInput.buttons > 0) ? 1 : 0));
			}
		}
	}

	public void OnStartConnectionEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender == target && info.senderID == ownerRig.creator.ActorNumber)
		{
			WakeUpRemoteVehicle();
		}
	}

	public void WakeUpRemoteVehicle()
	{
		if (networkSync != null && targetVehicle.IsNotNull() && !targetVehicle.HasLocalAuthority)
		{
			targetVehicle.WakeUpRemote(networkSync);
		}
	}

	private bool _TryFindRemoteVehicle()
	{
		if (targetVehicle != null)
		{
			return true;
		}
		VRRig componentInParent = GetComponentInParent<VRRig>(includeInactive: true);
		if (componentInParent.IsNull())
		{
			Debug.LogError("RCRemoteHoldable: unable to find parent vrrig");
			return false;
		}
		CosmeticItemInstance cosmeticItemInstance = componentInParent.cosmeticsObjectRegistry.Cosmetic(base.name);
		if (cosmeticItemInstance == null)
		{
			return false;
		}
		int instanceID = base.gameObject.GetInstanceID();
		if (_TryFindRemoteVehicle_InCosmeticInstanceArray(instanceID, cosmeticItemInstance.objects))
		{
			return true;
		}
		if (_TryFindRemoteVehicle_InCosmeticInstanceArray(instanceID, cosmeticItemInstance.leftObjects))
		{
			return true;
		}
		if (_TryFindRemoteVehicle_InCosmeticInstanceArray(instanceID, cosmeticItemInstance.rightObjects))
		{
			return true;
		}
		return false;
	}

	private bool _TryFindRemoteVehicle_InCosmeticInstanceArray(int thisGobjInstId, List<GameObject> gameObjects)
	{
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.GetInstanceID() != thisGobjInstId)
			{
				targetVehicle = gameObject.GetComponentInChildren<RCVehicle>(includeInactive: true);
				if ((object)targetVehicle != null)
				{
					return true;
				}
			}
		}
		return false;
	}
}
