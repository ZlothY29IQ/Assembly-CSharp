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

	private LckSocialCamera _socialCameraInstance;

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

	public void SetLckSocialCamera(LckSocialCamera socialCamera)
	{
		_socialCameraInstance = socialCamera;
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
			service.Result.OnRecordingStopped += OnRecordingStopped;
		}
		_gtLckController.OnCameraModeChanged += OnCameraModeChanged;
	}

	private void Update()
	{
		if (_socialCameraInstance != null)
		{
			if (_lckCamera != null)
			{
				Transform transform = _lckCamera.transform;
				_socialCameraInstance.transform.position = transform.position;
				_socialCameraInstance.transform.rotation = transform.rotation;
				Camera main = Camera.main;
				if (main != null)
				{
					_lckCamera.nearClipPlane = main.nearClipPlane;
					_lckCamera.farClipPlane = main.farClipPlane;
				}
			}
			CameraMode lckActiveCameraMode = _lckActiveCameraMode;
			if (lckActiveCameraMode == CameraMode.Selfie || (uint)(lckActiveCameraMode - 2) <= 1u)
			{
				_socialCameraInstance.visible = !_forceHidden && cameraActive;
			}
			else
			{
				_socialCameraInstance.visible = false;
			}
			_socialCameraInstance.recording = _recording;
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
		}
		_gtLckController.OnCameraModeChanged -= OnCameraModeChanged;
	}

	private void OnRecordingStarted(LckResult result)
	{
		_recording = result.Success;
	}

	private void OnRecordingStopped(LckResult result)
	{
		_recording = false;
	}

	private void OnCameraModeChanged(CameraMode mode, ILckCamera lckCamera)
	{
		_lckCamera = lckCamera.GetCameraComponent();
		_lckActiveCameraMode = mode;
	}
}
