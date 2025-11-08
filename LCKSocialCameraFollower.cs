using System;
using System.Collections.Generic;
using GorillaExtensions;
using Liv.Lck.GorillaTag;
using UnityEngine;

public class LCKSocialCameraFollower : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private Transform _scaleTransform;

	[SerializeField]
	private CoconutCamera _coconutCamera;

	[SerializeField]
	private List<GameObject> _visualObjects;

	[SerializeField]
	private RigContainer m_rigContainer;

	private Transform m_transformToFollow;

	private LckSocialCamera m_networkController;

	public Transform ScaleTransform => _scaleTransform;

	public CoconutCamera CoconutCamera => _coconutCamera;

	public List<GameObject> VisualObjects => _visualObjects;

	bool ITickSystemTick.TickRunning { get; set; }

	private void Awake()
	{
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
		base.transform.parent = null;
	}

	private void PostRigEnable(RigContainer _)
	{
		base.gameObject.SetActive(value: true);
		_coconutCamera.SetVisualsActive(active: false);
		_coconutCamera.SetRecordingState(isRecording: false);
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
			TickSystem<object>.RemoveCallbackTarget(this);
		}
	}

	void ITickSystemTick.Tick()
	{
		base.transform.position = m_transformToFollow.position;
		base.transform.root.rotation = m_transformToFollow.rotation;
	}
}
