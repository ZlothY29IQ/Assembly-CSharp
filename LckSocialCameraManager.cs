using System;
using Liv.Lck;
using Liv.Lck.GorillaTag;
using UnityEngine;

public class LckSocialCameraManager : MonoBehaviour
{
	[SerializeField]
	private GameObject _localUi;

	[SerializeField]
	private GameObject _localCameras;

	[SerializeField]
	private GTLckController _gtLckController;

	[SerializeField]
	private LckDirectGrabbable _lckDirectGrabbable;

	[SerializeField]
	public CoconutCamera CoconutCamera;

	private LckSocialCamera _socialCameraCococamInstance;

	private LckSocialCamera _socialCameraTabletInstance;

	private Camera _lckCamera;

	private CameraMode _lckActiveCameraMode;

	[OnEnterPlay_SetNull]
	private static LckSocialCameraManager _instance;

	public static Action<LckSocialCameraManager> OnManagerSpawned;

	private bool _recording;

	private bool _forceHidden;

	public LckDirectGrabbable lckDirectGrabbable => _lckDirectGrabbable;

	public static LckSocialCameraManager Instance => _instance;

	public bool cameraActive
	{
		get
		{
			return _localCameras.activeSelf;
		}
		set
		{
			_localCameras.SetActive(value);
			if (!value)
			{
				_gtLckController.StopRecording();
			}
		}
	}

	public bool uiVisible
	{
		get
		{
			return _localUi.activeSelf;
		}
		set
		{
			_localUi.SetActive(value);
		}
	}

	public void SetForceHidden(bool hidden)
	{
		_forceHidden = hidden;
	}

	private void Awake()
	{
		SetManagerInstance();
		_lckCamera = _gtLckController.GetActiveCamera();
	}

	public void SetLckSocialCococamCamera(LckSocialCamera socialCamera)
	{
		_socialCameraCococamInstance = socialCamera;
	}

	public void SetLckSocialTabletCamera(LckSocialCamera socialCameraTablet)
	{
		_socialCameraTabletInstance = socialCameraTablet;
	}

	private void SetManagerInstance()
	{
		_instance = this;
		OnManagerSpawned?.Invoke(this);
	}

	private void OnEnable()
	{
		LckResult<LckService> service = LckService.GetService();
		if (service.Result != null)
		{
			service.Result.OnRecordingStarted += OnRecordingStarted;
			service.Result.OnStreamingStarted += OnRecordingStarted;
			service.Result.OnRecordingStopped += OnRecordingStopped;
			service.Result.OnStreamingStopped += OnRecordingStopped;
		}
		LckBodyCameraSpawner.OnCameraStateChange += OnBodyCameraStateChanged;
		_gtLckController.OnCameraModeChanged += OnCameraModeChanged;
	}

	private void OnBodyCameraStateChanged(LckBodyCameraSpawner.CameraState state)
	{
		if (_socialCameraTabletInstance == null)
		{
			return;
		}
		if (_forceHidden)
		{
			_socialCameraTabletInstance.visible = false;
			_socialCameraCococamInstance.visible = false;
			return;
		}
		switch (state)
		{
		case LckBodyCameraSpawner.CameraState.CameraDisabled:
			_socialCameraTabletInstance.visible = false;
			_socialCameraCococamInstance.visible = false;
			_socialCameraTabletInstance.IsOnNeck = false;
			break;
		case LckBodyCameraSpawner.CameraState.CameraOnNeck:
			_socialCameraTabletInstance.visible = true;
			_socialCameraTabletInstance.IsOnNeck = true;
			break;
		case LckBodyCameraSpawner.CameraState.CameraSpawned:
			_socialCameraTabletInstance.visible = true;
			_socialCameraTabletInstance.IsOnNeck = false;
			if (_lckActiveCameraMode == CameraMode.ThirdPerson)
			{
				_socialCameraCococamInstance.visible = true;
			}
			break;
		}
	}

	private void Update()
	{
		if (_socialCameraCococamInstance != null && _socialCameraTabletInstance != null && _lckCamera != null)
		{
			Transform transform = _lckCamera.transform;
			_socialCameraCococamInstance.transform.position = transform.position;
			_socialCameraCococamInstance.transform.rotation = transform.rotation;
			_socialCameraTabletInstance.transform.position = base.transform.position;
			_socialCameraTabletInstance.transform.rotation = base.transform.rotation;
			Camera main = Camera.main;
			if (main != null)
			{
				_lckCamera.nearClipPlane = main.nearClipPlane;
				_lckCamera.farClipPlane = main.farClipPlane;
			}
		}
		if (CoconutCamera.gameObject.activeSelf)
		{
			switch (_lckActiveCameraMode)
			{
			case CameraMode.ThirdPerson:
			case CameraMode.Drone:
				CoconutCamera.SetVisualsActive(cameraActive);
				break;
			case CameraMode.Selfie:
				CoconutCamera.SetVisualsActive(active: false);
				break;
			default:
				CoconutCamera.SetVisualsActive(active: false);
				break;
			}
			CoconutCamera.SetRecordingState(_recording);
		}
	}

	private void OnDisable()
	{
		LckResult<LckService> service = LckService.GetService();
		if (service.Result != null)
		{
			service.Result.OnRecordingStarted -= OnRecordingStarted;
			service.Result.OnRecordingStopped -= OnRecordingStopped;
			service.Result.OnStreamingStopped -= OnRecordingStopped;
			service.Result.OnStreamingStopped -= OnRecordingStopped;
		}
		LckBodyCameraSpawner.OnCameraStateChange -= OnBodyCameraStateChanged;
		_gtLckController.OnCameraModeChanged -= OnCameraModeChanged;
	}

	private void OnRecordingStarted(LckResult result)
	{
		_recording = result.Success;
		if (_socialCameraCococamInstance != null && _socialCameraTabletInstance != null)
		{
			_socialCameraCococamInstance.recording = result.Success;
			_socialCameraTabletInstance.recording = result.Success;
		}
	}

	private void OnRecordingStopped(LckResult result)
	{
		_recording = false;
		if (_socialCameraCococamInstance != null && _socialCameraTabletInstance != null)
		{
			_socialCameraCococamInstance.recording = false;
			_socialCameraTabletInstance.recording = false;
		}
	}

	private void OnCameraModeChanged(CameraMode mode, ILckCamera lckCamera)
	{
		_lckCamera = lckCamera.GetCameraComponent();
		_lckActiveCameraMode = mode;
		if (_socialCameraCococamInstance == null || _socialCameraTabletInstance == null)
		{
			return;
		}
		if (_forceHidden)
		{
			_socialCameraTabletInstance.visible = false;
			_socialCameraCococamInstance.visible = false;
			return;
		}
		switch (_lckActiveCameraMode)
		{
		case CameraMode.FirstPerson:
			if (_socialCameraCococamInstance.visible)
			{
				_socialCameraCococamInstance.visible = false;
			}
			break;
		case CameraMode.ThirdPerson:
			if (!_socialCameraCococamInstance.visible)
			{
				_socialCameraCococamInstance.visible = true;
			}
			break;
		case CameraMode.Selfie:
			if (_socialCameraCococamInstance.visible)
			{
				_socialCameraCococamInstance.visible = false;
			}
			break;
		case CameraMode.Drone:
			_socialCameraCococamInstance.visible = !_forceHidden && cameraActive;
			_socialCameraTabletInstance.visible = cameraActive;
			break;
		default:
			_socialCameraCococamInstance.visible = cameraActive;
			_socialCameraTabletInstance.visible = cameraActive;
			break;
		}
	}
}
