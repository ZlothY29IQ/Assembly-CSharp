using System;
using GorillaLocomotion;
using Liv.Lck.GorillaTag;
using UnityEngine;

public class LckTabletSizeManager : MonoBehaviour
{
	[SerializeField]
	private GTLckController _controller;

	[SerializeField]
	private LckDirectGrabbable _lckDirectGrabbable;

	[SerializeField]
	private GtTabletFollower _tabletFollower;

	[SerializeField]
	private Camera _firstPersonCamera;

	[SerializeField]
	private Camera _selfieCamera;

	private Vector3 _firstPersonCamShrinkPosition = new Vector3(0f, 0f, -0.78f);

	private Vector3 _firstPersonCamDefaultPosition = Vector3.zero;

	private float _shrinkSize = 0.06f;

	private Vector3 _shrinkVector = new Vector3(0.06f, 0.06f, 0.06f);

	private float _customNearClip = 0.0006f;

	private bool _isDefaultScale = true;

	private void Start()
	{
		GTLckController controller = _controller;
		controller.OnFOVUpdated = (Action<CameraMode>)Delegate.Combine(controller.OnFOVUpdated, new Action<CameraMode>(UpdateCustomNearClip));
		_controller.OnHorizontalModeChanged += OnHorizontalModeChanged;
	}

	private void OnDestroy()
	{
		_controller.OnHorizontalModeChanged -= OnHorizontalModeChanged;
		GTLckController controller = _controller;
		controller.OnFOVUpdated = (Action<CameraMode>)Delegate.Remove(controller.OnFOVUpdated, new Action<CameraMode>(UpdateCustomNearClip));
	}

	private void OnHorizontalModeChanged(bool mode)
	{
		UpdateCustomNearClip(CameraMode.Selfie);
		UpdateCustomNearClip(CameraMode.FirstPerson);
	}

	private void UpdateCustomNearClip(CameraMode mode)
	{
		if (!GTPlayer.Instance.IsDefaultScale)
		{
			switch (mode)
			{
			case CameraMode.Selfie:
				SetCustomNearClip(_selfieCamera);
				break;
			case CameraMode.FirstPerson:
				SetCustomNearClip(_firstPersonCamera);
				break;
			case CameraMode.ThirdPerson:
			case CameraMode.Headset:
			case CameraMode.Drone:
				break;
			}
		}
	}

	private void SetCustomNearClip(Camera cam)
	{
		if (!GTPlayer.Instance.IsDefaultScale)
		{
			Matrix4x4 projectionMatrix = ((!_controller.HorizontalMode) ? Matrix4x4.Perspective(cam.fieldOfView, 0.5625f, _customNearClip, cam.farClipPlane) : Matrix4x4.Perspective(cam.fieldOfView, 1.777778f, _customNearClip, cam.farClipPlane));
			cam.projectionMatrix = projectionMatrix;
		}
	}

	private void ClearCustomNearClip()
	{
		_selfieCamera.ResetProjectionMatrix();
		_firstPersonCamera.ResetProjectionMatrix();
	}

	private void PlayerBecameSmall()
	{
		_firstPersonCamera.transform.localPosition = _firstPersonCamShrinkPosition;
		_tabletFollower.SetPlayerSizeModifier(isDefaultScale: false, _shrinkSize);
		if (!_lckDirectGrabbable.isGrabbed)
		{
			SetCameraOnNeck();
		}
		SetCustomNearClip(_selfieCamera);
		SetCustomNearClip(_firstPersonCamera);
	}

	private void PlayerBecameDefaultSize()
	{
		_firstPersonCamera.transform.localPosition = _firstPersonCamDefaultPosition;
		_tabletFollower.SetPlayerSizeModifier(isDefaultScale: true, 1f);
		if (!_lckDirectGrabbable.isGrabbed)
		{
			SetCameraOnNeck();
			base.transform.localScale = Vector3.one;
		}
		ClearCustomNearClip();
	}

	private void SetCameraOnNeck()
	{
		GTPlayer instance = GTPlayer.Instance;
		if (instance == null)
		{
			Debug.LogError("Unable to find playerInstance!");
			return;
		}
		LckBodyCameraSpawner componentInChildren = instance.GetComponentInChildren<LckBodyCameraSpawner>(includeInactive: true);
		if (componentInChildren == null)
		{
			Debug.LogError("Unable to find bodyCameraSpawner!");
		}
		else
		{
			componentInChildren.ManuallySetCameraOnNeck();
		}
	}

	private void Update()
	{
		if (!GTPlayer.Instance.IsDefaultScale && _isDefaultScale != GTPlayer.Instance.IsDefaultScale)
		{
			_isDefaultScale = false;
			PlayerBecameSmall();
		}
		else if (GTPlayer.Instance.IsDefaultScale && _isDefaultScale != GTPlayer.Instance.IsDefaultScale)
		{
			_isDefaultScale = true;
			PlayerBecameDefaultSize();
		}
	}
}
