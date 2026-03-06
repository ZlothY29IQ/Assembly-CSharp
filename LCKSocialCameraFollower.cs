using System;
using System.Collections.Generic;
using GorillaExtensions;
using Liv.Lck.GorillaTag;
using UnityEngine;
using UnityEngine.Serialization;

public class LCKSocialCameraFollower : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private Transform _scaleTransform;

	[FormerlySerializedAs("_coconutCamera")]
	[SerializeField]
	private GameObject _cameraVisualsRoot;

	[SerializeField]
	private List<GameObject> _visualObjects;

	[SerializeField]
	private RigContainer m_rigContainer;

	private Transform m_transformToFollow;

	private LckSocialCamera m_networkController;

	private IGtCameraVisuals m_gtCameraVisuals;

	private bool isParentedToRig;

	public Transform ScaleTransform => _scaleTransform;

	public GameObject CameraVisualsRoot => _cameraVisualsRoot;

	public List<GameObject> VisualObjects => _visualObjects;

	bool ITickSystemTick.TickRunning { get; set; }

	private void Awake()
	{
		m_gtCameraVisuals = _cameraVisualsRoot.GetComponent<IGtCameraVisuals>();
		if (m_rigContainer.Rig.isOfflineVRRig)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		m_rigContainer.RigEvents.disableEvent.Add(new Action<RigContainer>(PreRigDisable));
		m_rigContainer.RigEvents.enableEvent.Add(new Action<RigContainer>(PostRigEnable));
	}

	private void Start()
	{
		if (!isParentedToRig)
		{
			base.transform.parent = null;
		}
	}

	public void SetParentToRig()
	{
		isParentedToRig = true;
		base.transform.parent = m_rigContainer.transform;
		base.transform.localPosition = new Vector3(0f, -0.2f, 0.132f);
		base.transform.localRotation = Quaternion.identity;
	}

	public void SetParentNull()
	{
		isParentedToRig = false;
		base.transform.parent = null;
	}

	private void PostRigEnable(RigContainer _)
	{
		base.gameObject.SetActive(value: true);
		m_gtCameraVisuals.SetNetworkedVisualsActive(active: false);
		m_gtCameraVisuals.SetRecordingState(isRecording: false);
	}

	private void PreRigDisable(RigContainer _)
	{
		base.gameObject.SetActive(value: false);
	}

	public void SetNetworkController(LckSocialCamera networkController)
	{
		if (m_networkController.IsNotNull() && m_networkController != networkController)
		{
			m_networkController.TurnOff();
		}
		m_networkController = networkController;
		m_transformToFollow = m_networkController.transform;
		TickSystem<object>.AddTickCallback(this);
	}

	public void RemoveNetworkController(LckSocialCamera networkController)
	{
		if (!(m_networkController != networkController))
		{
			m_transformToFollow = null;
			m_networkController = null;
			TickSystem<object>.RemoveTickCallback(this);
		}
	}

	void ITickSystemTick.Tick()
	{
		if (!isParentedToRig && !(m_transformToFollow == null))
		{
			base.transform.position = m_transformToFollow.position;
			base.transform.root.rotation = m_transformToFollow.rotation;
		}
	}
}
