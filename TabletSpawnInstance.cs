using System;
using Liv.Lck.GorillaTag;
using UnityEngine;

public class TabletSpawnInstance : IDisposable
{
	private GameObject _cameraGameObjectInstance;

	private GameObject _cameraSpawnPrefab;

	private GameEvents _GtCamera;

	private Transform _cameraSpawnParentTransform;

	private Transform _cameraSpawnInstanceTransform;

	public GTLckController Controller;

	private LckSocialCameraManager _lckSocialCameraManager;

	private bool _cameraActive;

	private bool _uiVisible;

	public LckDirectGrabbable directGrabbable => _lckSocialCameraManager.lckDirectGrabbable;

	public bool cameraActive
	{
		get
		{
			return _cameraActive;
		}
		set
		{
			_cameraActive = value;
			if (!_cameraActive && Controller != null)
			{
				Controller.StopRecording();
			}
			if (_lckSocialCameraManager != null)
			{
				_lckSocialCameraManager.cameraActive = _cameraActive;
			}
		}
	}

	public bool uiVisible
	{
		get
		{
			return _uiVisible;
		}
		set
		{
			_uiVisible = value;
			if (_lckSocialCameraManager != null)
			{
				_lckSocialCameraManager.uiVisible = _uiVisible;
			}
		}
	}

	public bool isSpawned => _cameraGameObjectInstance != null;

	public Vector3 position
	{
		get
		{
			if (_cameraSpawnInstanceTransform == null)
			{
				return Vector3.zero;
			}
			return _cameraSpawnInstanceTransform.position;
		}
	}

	public Quaternion rotation
	{
		get
		{
			if (_cameraSpawnInstanceTransform == null)
			{
				return Quaternion.identity;
			}
			return _cameraSpawnInstanceTransform.rotation;
		}
	}

	public event Action onGrabbed;

	public event Action onReleased;

	public bool ResetLocalPose()
	{
		if (_cameraSpawnInstanceTransform == null)
		{
			return false;
		}
		_cameraSpawnInstanceTransform.localPosition = Vector3.zero;
		_cameraSpawnInstanceTransform.localRotation = Quaternion.identity;
		return true;
	}

	public bool ResetParent()
	{
		if (_cameraSpawnInstanceTransform == null)
		{
			return false;
		}
		_cameraSpawnInstanceTransform.SetParent(_cameraSpawnParentTransform);
		return true;
	}

	public bool SetParent(Transform transform)
	{
		if (_cameraSpawnInstanceTransform == null)
		{
			return false;
		}
		_cameraSpawnInstanceTransform.SetParent(transform);
		return true;
	}

	public TabletSpawnInstance(GameObject cameraSpawnPrefab, Transform cameraSpawnParentTransform)
	{
		_cameraSpawnPrefab = cameraSpawnPrefab;
		_cameraSpawnParentTransform = cameraSpawnParentTransform;
	}

	public void Update()
	{
		if (!(Controller == null))
		{
			Camera activeCamera = Controller.GetActiveCamera();
			Camera main = Camera.main;
			if (main != null)
			{
				activeCamera.nearClipPlane = main.nearClipPlane;
				activeCamera.farClipPlane = main.farClipPlane;
			}
		}
	}

	public void SpawnCamera()
	{
		if (!isSpawned)
		{
			_cameraGameObjectInstance = UnityEngine.Object.Instantiate(_cameraSpawnPrefab, _cameraSpawnParentTransform);
			_lckSocialCameraManager = _cameraGameObjectInstance.GetComponent<LckSocialCameraManager>();
			_lckSocialCameraManager.lckDirectGrabbable.onGrabbed += delegate
			{
				this.onGrabbed?.Invoke();
			};
			_lckSocialCameraManager.lckDirectGrabbable.onReleased += delegate
			{
				this.onReleased?.Invoke();
			};
			_cameraSpawnInstanceTransform = _cameraGameObjectInstance.transform;
			Controller = _cameraGameObjectInstance.GetComponent<GTLckController>();
		}
		uiVisible = uiVisible;
		cameraActive = cameraActive;
	}

	public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
	{
		if (!(_cameraSpawnInstanceTransform == null))
		{
			_cameraSpawnInstanceTransform.SetPositionAndRotation(position, rotation);
		}
	}

	public void SetLocalScale(Vector3 scale)
	{
		if (!(_cameraSpawnInstanceTransform == null))
		{
			_cameraSpawnInstanceTransform.localScale = scale;
		}
	}

	public void Dispose()
	{
		if (_cameraGameObjectInstance != null)
		{
			UnityEngine.Object.Destroy(_cameraGameObjectInstance);
			_cameraGameObjectInstance = null;
		}
	}
}
