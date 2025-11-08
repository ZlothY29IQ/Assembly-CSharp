using Liv.Lck;
using Liv.Lck.GorillaTag;
using Unity.Cinemachine;
using UnityEngine;

public class MonitorOutputController : MonoBehaviour
{
	[SerializeField]
	private GTLckController _gtLckController;

	private Camera _lckCamera;

	private CameraMode _lckActiveCameraMode;

	private Camera _shoulderCamera;

	private float _shoulderCameraFov;

	private void Awake()
	{
		_lckCamera = _gtLckController.GetActiveCamera();
	}

	private void OnEnable()
	{
		_gtLckController.OnCameraModeChanged += OnCameraModeChanged;
		LckBodyCameraSpawner.OnCameraStateChange += CameraStateChanged;
	}

	private void Update()
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			Object.Destroy(this);
		}
		if (_shoulderCamera == null)
		{
			FindShoulderCamera();
		}
		if (_lckCamera != null)
		{
			_shoulderCamera.transform.position = _lckCamera.transform.position;
			_shoulderCamera.transform.rotation = _lckCamera.transform.rotation;
			_shoulderCamera.fieldOfView = _lckCamera.fieldOfView;
		}
		else
		{
			_lckCamera = _gtLckController.GetActiveCamera();
		}
	}

	private void CameraStateChanged(LckBodyCameraSpawner.CameraState state)
	{
		switch (state)
		{
		case LckBodyCameraSpawner.CameraState.CameraDisabled:
			RestoreShoulderCamera();
			break;
		case LckBodyCameraSpawner.CameraState.CameraOnNeck:
			TakeOverShoulderCamera();
			break;
		case LckBodyCameraSpawner.CameraState.CameraSpawned:
			TakeOverShoulderCamera();
			break;
		}
	}

	private void OnDisable()
	{
		_gtLckController.OnCameraModeChanged -= OnCameraModeChanged;
		_shoulderCamera.gameObject.GetComponentInChildren<CinemachineBrain>().enabled = true;
		LckBodyCameraSpawner.OnCameraStateChange -= CameraStateChanged;
	}

	private void OnCameraModeChanged(CameraMode mode, ILckCamera lckCamera)
	{
		_lckCamera = lckCamera.GetCameraComponent();
		_lckActiveCameraMode = mode;
	}

	private void TakeOverShoulderCamera()
	{
		FindShoulderCamera();
		_shoulderCamera.gameObject.GetComponentInChildren<CinemachineBrain>().enabled = false;
		_shoulderCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("LCKHide"));
	}

	private void RestoreShoulderCamera()
	{
		FindShoulderCamera();
		_shoulderCamera.gameObject.GetComponentInChildren<CinemachineBrain>().enabled = true;
		_shoulderCamera.cullingMask |= 1 << LayerMask.NameToLayer("LCKHide");
		_shoulderCamera.fieldOfView = _shoulderCameraFov;
	}

	private void FindShoulderCamera()
	{
		if (!(_shoulderCamera != null) && GorillaTagger.hasInstance && base.isActiveAndEnabled)
		{
			_shoulderCamera = GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<Camera>();
			_shoulderCameraFov = _shoulderCamera.fieldOfView;
		}
	}
}
