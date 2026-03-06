using System;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;

public class GorillaIOBT : MonoBehaviour
{
	private OVRSkeleton upperBodySkeleton;

	public AudioSource trackingChangedAudioSource;

	public AudioClip trackingGainedClip;

	public AudioClip trackingLostClip;

	protected bool _skipUpdate;

	protected readonly string trackingSpaceName = "TurnParent";

	protected readonly string centerEyeAnchorName = "Main Camera";

	protected readonly string leftHandAnchorName = "LeftHand Controller";

	protected readonly string rightHandAnchorName = "RightHand Controller";

	protected readonly string leftControllerAnchorName = "LeftControllerAnchor";

	protected readonly string rightControllerAnchorName = "RightControllerAnchor";

	protected Matrix4x4 _previousTrackingSpaceTransform;

	public OVRInput.Controller leftActiveController { get; private set; }

	public OVRInput.Controller rightActiveController { get; private set; }

	public bool IsHandTracking
	{
		get
		{
			if (leftActiveController != OVRInput.Controller.LHand)
			{
				return rightActiveController == OVRInput.Controller.RHand;
			}
			return true;
		}
	}

	public HandTrackingFingerCurl leftHandCurl { get; private set; }

	public HandTrackingFingerCurl rightHandCurl { get; private set; }

	public Transform trackingSpace { get; private set; }

	public Transform centerEyeAnchor { get; private set; }

	public Transform leftHandAnchor { get; private set; }

	public Transform rightHandAnchor { get; private set; }

	public Transform leftControllerAnchor { get; private set; }

	public Transform rightControllerAnchor { get; private set; }

	public event Action<GorillaIOBT> UpdatedAnchors;

	public event Action<Transform> TrackingSpaceChanged;

	protected virtual void Awake()
	{
		_skipUpdate = true;
		EnsureGameObjectIntegrity();
		upperBodySkeleton = GetComponent<OVRSkeleton>();
	}

	protected virtual void Start()
	{
		UpdateAnchors();
		Application.onBeforeRender += OnBeforeRenderCallback;
	}

	protected virtual void Update()
	{
		_skipUpdate = false;
		UpdateAnchors();
	}

	protected virtual void OnDestroy()
	{
		Application.onBeforeRender -= OnBeforeRenderCallback;
	}

	protected virtual void UpdateAnchors()
	{
		if (!OVRManager.OVRManagerinitialized)
		{
			return;
		}
		EnsureGameObjectIntegrity();
		if (!Application.isPlaying)
		{
			return;
		}
		if (_skipUpdate)
		{
			centerEyeAnchor.FromOVRPose(OVRPose.identity, isLocal: true);
			return;
		}
		_ = OVRManager.instance.monoscopic;
		OVRNodeStateProperties.IsHmdPresent();
		OVRManager.tracker.GetPose();
		Quaternion.Euler(0f - OVRManager.instance.headPoseRelativeOffsetRotation.x, 0f - OVRManager.instance.headPoseRelativeOffsetRotation.y, OVRManager.instance.headPoseRelativeOffsetRotation.z);
		OVRInput.Controller controller = leftActiveController;
		OVRInput.Controller controller2 = rightActiveController;
		leftActiveController = OVRInput.GetActiveControllerForHand(OVRInput.Handedness.LeftHanded);
		rightActiveController = OVRInput.GetActiveControllerForHand(OVRInput.Handedness.RightHanded);
		if (leftActiveController == OVRInput.Controller.None)
		{
			if (OVRInput.GetControllerPositionValid(OVRInput.Controller.LHand))
			{
				leftActiveController = OVRInput.Controller.LHand;
			}
			else if (OVRInput.GetControllerPositionValid(OVRInput.Controller.LTouch))
			{
				leftActiveController = OVRInput.Controller.LTouch;
			}
		}
		if (rightActiveController == OVRInput.Controller.None)
		{
			if (OVRInput.GetControllerPositionValid(OVRInput.Controller.RHand))
			{
				rightActiveController = OVRInput.Controller.RHand;
			}
			else if (OVRInput.GetControllerPositionValid(OVRInput.Controller.RTouch))
			{
				rightActiveController = OVRInput.Controller.RTouch;
			}
		}
		if (controller == OVRInput.Controller.None && leftActiveController != OVRInput.Controller.None)
		{
			trackingChangedAudioSource.PlayOneShot(trackingGainedClip);
		}
		else if (controller != OVRInput.Controller.None && leftActiveController == OVRInput.Controller.None)
		{
			trackingChangedAudioSource.PlayOneShot(trackingLostClip);
		}
		if (controller2 == OVRInput.Controller.None && rightActiveController != OVRInput.Controller.None)
		{
			trackingChangedAudioSource.PlayOneShot(trackingGainedClip);
		}
		else if (controller2 != OVRInput.Controller.None && rightActiveController == OVRInput.Controller.None)
		{
			trackingChangedAudioSource.PlayOneShot(trackingLostClip);
		}
		if (leftActiveController == OVRInput.Controller.LHand)
		{
			leftHandAnchor.localPosition = OVRInput.GetLocalControllerPosition(leftActiveController);
			leftHandAnchor.localRotation = OVRInput.GetLocalControllerRotation(leftActiveController);
			leftHandAnchor.localRotation *= Quaternion.Euler(0f, 90f, -90f);
		}
		if (rightActiveController == OVRInput.Controller.RHand)
		{
			rightHandAnchor.localPosition = OVRInput.GetLocalControllerPosition(rightActiveController);
			rightHandAnchor.localRotation = OVRInput.GetLocalControllerRotation(rightActiveController);
			rightHandAnchor.localRotation *= Quaternion.Euler(0f, -90f, 90f);
		}
		OVRPose oVRPose = OVRPose.identity;
		OVRPose oVRPose2 = OVRPose.identity;
		if (OVRManager.loadedXRDevice == OVRManager.XRDevice.OpenVR)
		{
			oVRPose = OVRManager.GetOpenVRControllerOffset(XRNode.LeftHand);
			oVRPose2 = OVRManager.GetOpenVRControllerOffset(XRNode.RightHand);
			OVRManager.SetOpenVRLocalPose(trackingSpace.InverseTransformPoint(leftControllerAnchor.position), trackingSpace.InverseTransformPoint(rightControllerAnchor.position), Quaternion.Inverse(trackingSpace.rotation) * leftControllerAnchor.rotation, Quaternion.Inverse(trackingSpace.rotation) * rightControllerAnchor.rotation);
		}
		rightControllerAnchor.localPosition = oVRPose2.position;
		rightControllerAnchor.localRotation = oVRPose2.orientation;
		leftControllerAnchor.localPosition = oVRPose.position;
		leftControllerAnchor.localRotation = oVRPose.orientation;
		GTPlayer.Instance.SetHandOffsets(isLeftHand: true, new Vector3(0.03f, -0.16f, 0f), Quaternion.Euler(89f, 6f, 11f));
		GTPlayer.Instance.SetHandOffsets(isLeftHand: false, new Vector3(-0.01f, -0.16f, 0f), Quaternion.Euler(89f, 6f, 11f));
		RaiseUpdatedAnchorsEvent();
		CheckForTrackingSpaceChangesAndRaiseEvent();
	}

	protected virtual void OnBeforeRenderCallback()
	{
		if (OVRManager.loadedXRDevice == OVRManager.XRDevice.Oculus && OVRManager.instance.LateControllerUpdate)
		{
			UpdateAnchors();
		}
	}

	protected virtual void CheckForTrackingSpaceChangesAndRaiseEvent()
	{
		if (!(trackingSpace == null))
		{
			Matrix4x4 localToWorldMatrix = trackingSpace.localToWorldMatrix;
			bool num = this.TrackingSpaceChanged != null && !_previousTrackingSpaceTransform.Equals(localToWorldMatrix);
			_previousTrackingSpaceTransform = localToWorldMatrix;
			if (num)
			{
				this.TrackingSpaceChanged(trackingSpace);
			}
		}
	}

	protected virtual void RaiseUpdatedAnchorsEvent()
	{
		if (this.UpdatedAnchors != null)
		{
			this.UpdatedAnchors(this);
		}
	}

	public virtual void EnsureGameObjectIntegrity()
	{
		if (OVRManager.instance != null)
		{
			_ = OVRManager.instance.monoscopic;
		}
		if (trackingSpace == null)
		{
			trackingSpace = ConfigureAnchor(null, trackingSpaceName);
			_previousTrackingSpaceTransform = trackingSpace.localToWorldMatrix;
		}
		if (centerEyeAnchor == null)
		{
			centerEyeAnchor = ConfigureAnchor(trackingSpace, centerEyeAnchorName);
		}
		if (leftHandAnchor == null)
		{
			leftHandAnchor = ConfigureAnchor(trackingSpace, leftHandAnchorName);
		}
		if (rightHandAnchor == null)
		{
			rightHandAnchor = ConfigureAnchor(trackingSpace, rightHandAnchorName);
		}
		if (leftControllerAnchor == null)
		{
			leftControllerAnchor = ConfigureAnchor(leftHandAnchor, leftControllerAnchorName);
		}
		if (rightControllerAnchor == null)
		{
			rightControllerAnchor = ConfigureAnchor(rightHandAnchor, rightControllerAnchorName);
		}
		if (leftHandCurl == null)
		{
			leftHandCurl = leftHandAnchor?.GetComponent<HandTrackingFingerCurl>();
		}
		if (rightHandCurl == null)
		{
			rightHandCurl = rightHandAnchor?.GetComponent<HandTrackingFingerCurl>();
		}
	}

	protected Transform ConfigureAnchor(Transform root, string name)
	{
		Transform transform = ((root != null) ? root.Find(name) : null);
		if (transform == null)
		{
			transform = base.transform.Find(name);
		}
		if (transform == null)
		{
			transform = new GameObject(name).transform;
		}
		transform.name = name;
		transform.parent = ((root != null) ? root : base.transform);
		transform.localScale = Vector3.one;
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
		return transform;
	}

	public virtual Matrix4x4 ComputeTrackReferenceMatrix()
	{
		if (centerEyeAnchor == null)
		{
			Debug.LogError("centerEyeAnchor is required");
			return Matrix4x4.identity;
		}
		OVRPose identity = OVRPose.identity;
		if (OVRNodeStateProperties.GetNodeStatePropertyVector3(XRNode.Head, NodeStatePropertyType.Position, OVRPlugin.Node.Head, OVRPlugin.Step.Render, out var retVec))
		{
			identity.position = retVec;
		}
		if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(XRNode.Head, NodeStatePropertyType.Orientation, OVRPlugin.Node.Head, OVRPlugin.Step.Render, out var retQuat))
		{
			identity.orientation = retQuat;
		}
		OVRPose oVRPose = identity.Inverse();
		Matrix4x4 matrix4x = Matrix4x4.TRS(oVRPose.position, oVRPose.orientation, Vector3.one);
		return centerEyeAnchor.localToWorldMatrix * matrix4x;
	}

	protected void CheckForAnchorsInParent()
	{
		Transform parent = base.transform.parent;
		while ((bool)parent)
		{
			Check<OVRSpatialAnchor>(parent);
			Check<OVRSceneAnchor>(parent);
			parent = parent.parent;
		}
		void Check<T>(Transform node) where T : MonoBehaviour
		{
			T component = node.GetComponent<T>();
			if ((bool)component && component.enabled)
			{
				component.enabled = false;
				Debug.LogError("The " + typeof(T).Name + " '" + component.name + "' is a parent of the GorillaIOBT '" + base.name + "', which is not allowed. An " + typeof(T).Name + " may not be the parent of an GorillaIOBT because the GorillaIOBT defines the tracking space for the anchor, and its transform is relative to the GorillaIOBT.");
			}
		}
	}
}
