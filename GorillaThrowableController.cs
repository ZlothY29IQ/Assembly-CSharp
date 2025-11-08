using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class GorillaThrowableController : MonoBehaviour
{
	public Transform leftHandController;

	public Transform rightHandController;

	public bool leftHandIsGrabbing;

	public bool rightHandIsGrabbing;

	public GorillaThrowable leftHandGrabbedObject;

	public GorillaThrowable rightHandGrabbedObject;

	public float hoverVibrationStrength = 0.25f;

	public float hoverVibrationDuration = 0.05f;

	public float handRadius = 0.05f;

	private InputDevice rightDevice;

	private InputDevice leftDevice;

	private InputDevice inputDevice;

	private float triggerValue;

	private bool boolVar;

	private Collider[] colliders = new Collider[10];

	private Collider minCollider;

	private Collider returnCollider;

	private float magnitude;

	public bool testCanGrab;

	private int gorillaThrowableLayerMask;

	protected void Awake()
	{
		gorillaThrowableLayerMask = LayerMask.GetMask("GorillaThrowable");
	}

	private void LateUpdate()
	{
		if (testCanGrab)
		{
			testCanGrab = false;
			CanGrabAnObject(rightHandController, out returnCollider);
			Debug.Log(returnCollider.gameObject, returnCollider.gameObject);
		}
		if (leftHandIsGrabbing)
		{
			if (CheckIfHandHasReleased(XRNode.LeftHand))
			{
				if (leftHandGrabbedObject != null)
				{
					leftHandGrabbedObject.ThrowThisThingo();
					leftHandGrabbedObject = null;
				}
				leftHandIsGrabbing = false;
			}
		}
		else if (CheckIfHandHasGrabbed(XRNode.LeftHand))
		{
			leftHandIsGrabbing = true;
			if (CanGrabAnObject(leftHandController, out returnCollider))
			{
				leftHandGrabbedObject = returnCollider.GetComponent<GorillaThrowable>();
				leftHandGrabbedObject.Grabbed(leftHandController);
			}
		}
		if (rightHandIsGrabbing)
		{
			if (CheckIfHandHasReleased(XRNode.RightHand))
			{
				if (rightHandGrabbedObject != null)
				{
					rightHandGrabbedObject.ThrowThisThingo();
					rightHandGrabbedObject = null;
				}
				rightHandIsGrabbing = false;
			}
		}
		else if (CheckIfHandHasGrabbed(XRNode.RightHand))
		{
			rightHandIsGrabbing = true;
			if (CanGrabAnObject(rightHandController, out returnCollider))
			{
				rightHandGrabbedObject = returnCollider.GetComponent<GorillaThrowable>();
				rightHandGrabbedObject.Grabbed(rightHandController);
			}
		}
	}

	private bool CheckIfHandHasReleased(XRNode node)
	{
		inputDevice = InputDevices.GetDeviceAtXRNode(node);
		triggerValue = ((node == XRNode.LeftHand) ? SteamVR_Actions.gorillaTag_LeftTriggerFloat.GetAxis(SteamVR_Input_Sources.LeftHand) : SteamVR_Actions.gorillaTag_RightTriggerFloat.GetAxis(SteamVR_Input_Sources.RightHand));
		if (triggerValue < 0.75f)
		{
			triggerValue = ((node == XRNode.LeftHand) ? SteamVR_Actions.gorillaTag_LeftGripFloat.GetAxis(SteamVR_Input_Sources.LeftHand) : SteamVR_Actions.gorillaTag_RightGripFloat.GetAxis(SteamVR_Input_Sources.RightHand));
			if (triggerValue < 0.75f)
			{
				return true;
			}
		}
		return false;
	}

	private bool CheckIfHandHasGrabbed(XRNode node)
	{
		inputDevice = InputDevices.GetDeviceAtXRNode(node);
		triggerValue = ((node == XRNode.LeftHand) ? SteamVR_Actions.gorillaTag_LeftTriggerFloat.GetAxis(SteamVR_Input_Sources.LeftHand) : SteamVR_Actions.gorillaTag_RightTriggerFloat.GetAxis(SteamVR_Input_Sources.RightHand));
		if (triggerValue > 0.75f)
		{
			return true;
		}
		triggerValue = ((node == XRNode.LeftHand) ? SteamVR_Actions.gorillaTag_LeftGripFloat.GetAxis(SteamVR_Input_Sources.LeftHand) : SteamVR_Actions.gorillaTag_RightGripFloat.GetAxis(SteamVR_Input_Sources.RightHand));
		if (triggerValue > 0.75f)
		{
			return true;
		}
		return false;
	}

	private bool CanGrabAnObject(Transform handTransform, out Collider returnCollider)
	{
		magnitude = 100f;
		returnCollider = null;
		Debug.Log("trying:");
		if (Physics.OverlapSphereNonAlloc(handTransform.position, handRadius, colliders, gorillaThrowableLayerMask) > 0)
		{
			Debug.Log("found something!");
			minCollider = colliders[0];
			Collider[] array = colliders;
			foreach (Collider collider in array)
			{
				if (collider != null)
				{
					Debug.Log("found this", collider);
					if ((collider.transform.position - handTransform.position).magnitude < magnitude)
					{
						minCollider = collider;
						magnitude = (collider.transform.position - handTransform.position).magnitude;
					}
				}
			}
			returnCollider = minCollider;
			return true;
		}
		return false;
	}

	public void GrabbableObjectHover(bool isLeft)
	{
		GorillaTagger.Instance.StartVibration(isLeft, hoverVibrationStrength, hoverVibrationDuration);
	}
}
