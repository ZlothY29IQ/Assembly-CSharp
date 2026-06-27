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

	private LckSocialCamera _networkedCococam;

	private LckSocialCamera _networkedTablet;

	private Camera _lckCamera;

	private CameraMode _cameraMode;

	private LckBodyCameraSpawner.CameraState _cameraState;

	[OnEnterPlay_SetNull]
	private static LckSocialCameraManager _instance;

	public static Action<LckSocialCameraManager> OnManagerSpawned;

	private bool _isRecording;

	private bool _isForceHidden;

	private bool _needsUpdate = true;

	private Vector3 _tabletPositionOffset = new Vector3(0f, 0.11f, -0.08f);

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
			if (_localCameras.activeSelf != value)
			{
				_localCameras.SetActive(value);
				_needsUpdate = true;
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

	private void Awake()
	{
		SetManagerInstance();
		_lckCamera = _gtLckController.GetActiveCamera();
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
		_cameraMode = _gtLckController.CurrentCameraMode;
	}

	private void Update()
	{
		if (_lckCamera != null)
		{
			Transform transform = _lckCamera.transform;
			if (_networkedCococam != null)
			{
				_networkedCococam.transform.position = transform.position;
				_networkedCococam.transform.rotation = transform.rotation;
			}
			if (_networkedTablet != null)
			{
				if (_networkedTablet.IsOnNeck)
				{
					_networkedTablet.transform.position = base.transform.position;
				}
				else
				{
					_networkedTablet.transform.position = base.transform.position + _tabletPositionOffset * _networkedTablet.VrRig.scaleFactor;
				}
				_networkedTablet.transform.rotation = base.transform.rotation;
			}
		}
		if (_needsUpdate)
		{
			UpdateCococamVisibility(_cameraState, _cameraMode, _isForceHidden, cameraActive);
			UpdateTabletVisibility(_cameraState, _isForceHidden, cameraActive);
			UpdateCococamRecording(_isRecording);
			UpdateTabletRecording(_isRecording);
			_needsUpdate = false;
		}
	}

	private void OnDisable()
	{
		LckResult<LckService> service = LckService.GetService();
		if (service.Result != null)
		{
			service.Result.OnRecordingStarted -= OnRecordingStarted;
			service.Result.OnStreamingStarted -= OnRecordingStarted;
			service.Result.OnRecordingStopped -= OnRecordingStopped;
			service.Result.OnStreamingStopped -= OnRecordingStopped;
		}
		LckBodyCameraSpawner.OnCameraStateChange -= OnBodyCameraStateChanged;
		_gtLckController.OnCameraModeChanged -= OnCameraModeChanged;
	}

	public void SetForceHidden(bool hidden)
	{
		if (_isForceHidden != hidden)
		{
			_isForceHidden = hidden;
			_needsUpdate = true;
		}
	}

	public void SetLckSocialCococamCamera(LckSocialCamera socialCamera)
	{
		if (!(_networkedCococam == socialCamera))
		{
			_networkedCococam = socialCamera;
			_needsUpdate = true;
		}
	}

	public void SetLckSocialTabletCamera(LckSocialCamera socialCameraTablet)
	{
		if (!(_networkedTablet == socialCameraTablet))
		{
			_networkedTablet = socialCameraTablet;
			_needsUpdate = true;
		}
	}

	private void SetManagerInstance()
	{
		_instance = this;
		OnManagerSpawned?.Invoke(this);
	}

	private void OnBodyCameraStateChanged(LckBodyCameraSpawner.CameraState state)
	{
		if (_cameraState != state)
		{
			_cameraState = state;
			_needsUpdate = true;
		}
	}

	private void OnCameraModeChanged(CameraMode mode, ILckCamera lckCamera)
	{
		_lckCamera = lckCamera.GetCameraComponent();
		if (_cameraMode != mode)
		{
			_cameraMode = mode;
			_needsUpdate = true;
		}
	}

	private void OnRecordingStarted(LckResult result)
	{
		if (_isRecording != result.Success)
		{
			_isRecording = result.Success;
			_needsUpdate = true;
		}
	}

	private void OnRecordingStopped(LckResult result)
	{
		if (_isRecording)
		{
			_isRecording = false;
			_needsUpdate = true;
		}
	}

	private void UpdateCococamRecording(bool recording)
	{
		CoconutCamera.SetRecordingState(recording);
		if (!(_networkedCococam == null))
		{
			_networkedCococam.recording = recording;
		}
	}

	private void UpdateCococamVisibility(LckBodyCameraSpawner.CameraState cameraState, CameraMode cameraMode, bool forceHidden, bool cameraActive)
	{
		if (cameraMode == CameraMode.ThirdPerson || cameraMode == CameraMode.Drone)
		{
			CoconutCamera.SetVisualsActive(cameraActive);
		}
		else
		{
			CoconutCamera.SetVisualsActive(active: false);
		}
		if (!(_networkedCococam == null))
		{
			if (cameraState == LckBodyCameraSpawner.CameraState.CameraDisabled || forceHidden || !cameraActive)
			{
				_networkedCococam.visible = false;
			}
			else
			{
				_networkedCococam.visible = cameraMode == CameraMode.ThirdPerson || cameraMode == CameraMode.Drone;
			}
		}
	}

	private void UpdateTabletRecording(bool recording)
	{
		if (!(_networkedTablet == null))
		{
			_networkedTablet.recording = recording;
		}
	}

	private void UpdateTabletVisibility(LckBodyCameraSpawner.CameraState cameraState, bool forceHidden, bool cameraActive)
	{
		if (!(_networkedTablet == null))
		{
			if (cameraState == LckBodyCameraSpawner.CameraState.CameraDisabled || forceHidden)
			{
				_networkedTablet.visible = false;
				_networkedTablet.IsOnNeck = false;
			}
			else
			{
				_networkedTablet.visible = cameraActive;
				_networkedTablet.IsOnNeck = cameraState == LckBodyCameraSpawner.CameraState.CameraOnNeck;
			}
		}
	}
}
