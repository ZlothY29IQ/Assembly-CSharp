using System;
using GorillaLocomotion;
using Liv.Lck;
using Liv.Lck.Cosmetics;
using Liv.Lck.GorillaTag;
using UnityEngine;
using UnityEngine.XR;

public class LckBodyCameraSpawner : MonoBehaviourTick
{
	public enum CameraState
	{
		CameraDisabled,
		CameraOnNeck,
		CameraSpawned
	}

	public enum CameraPosition
	{
		CameraDefault,
		CameraSlingshot,
		NotVisible
	}

	public delegate void CameraStateDelegate(CameraState state);

	[SerializeField]
	private GameObject _cameraSpawnPrefab;

	[SerializeField]
	private Transform _cameraSpawnParentTransform;

	[SerializeField]
	private Transform _cameraModelOriginTransform;

	[SerializeField]
	private Transform _cameraModelTransform;

	[SerializeField]
	private LckDirectGrabbable _cameraModelGrabbable;

	[SerializeField]
	private Transform _cameraPositionDefault;

	[SerializeField]
	private Transform _cameraPositionSlingshot;

	private Vector3 _chestSpawnRotationOffset = new Vector3(90f, 0f, 0f);

	private Vector3 _rightHandSpawnOffsetAndroid = new Vector3(-0.265f, 0.02f, -0.065f);

	private Vector3 _leftHandSpawnOffsetAndroid = new Vector3(0.245f, 0.022f, -0.12f);

	private Vector3 _rotationOffsetAndroid = new Vector3(-90f, 60f, 125f);

	private Vector3 _rotationOffsetWindows = new Vector3(-70f, -180f, 0f);

	private Vector3 _rightHandSpawnOffsetWindows = new Vector3(-0.23f, -0.035f, -0.225f);

	private Vector3 _leftHandSpawnOffsetWindows = new Vector3(0.23f, -0.035f, -0.225f);

	[SerializeField]
	private float _activateDistance = 0.25f;

	[SerializeField]
	private float _snapToNeckDistance = 6f;

	[SerializeField]
	private LineRenderer _cameraStrapRenderer;

	[SerializeField]
	private Transform[] _cameraStrapPoints;

	[SerializeField]
	private Color _normalColor = Color.red;

	[SerializeField]
	private Color _ghostColor = Color.gray;

	[Header("Cosmetics References")]
	[SerializeField]
	private GtDummyTablet _dummyTablet;

	[SerializeField]
	private LckGameObjectSwapCosmetic _swapTablet;

	[SerializeField]
	private LckGameObjectSwapCosmetic _swapEmobi;

	private Transform _followTransform;

	private Vector3[] _cameraStrapPositions;

	private TabletSpawnInstance _tabletSpawnInstance;

	private VRRig _localRig;

	private bool _shouldMoveCameraToNeck;

	private CameraMode? _returnToCameraMode;

	private CameraState _cameraState;

	private CameraPosition _cameraPosition;

	public TabletSpawnInstance tabletSpawnInstance => _tabletSpawnInstance;

	public CameraState cameraState
	{
		get
		{
			return _cameraState;
		}
		set
		{
			switch (value)
			{
			case CameraState.CameraDisabled:
				cameraPosition = CameraPosition.NotVisible;
				_tabletSpawnInstance.uiVisible = false;
				_tabletSpawnInstance.cameraActive = false;
				ResetCameraModel();
				cameraVisible = false;
				_shouldMoveCameraToNeck = false;
				break;
			case CameraState.CameraOnNeck:
				cameraPosition = CameraPosition.CameraDefault;
				if ((bool)_tabletSpawnInstance.Controller.GtColliderTriggerProcessorsGroup.GetCurrentTriggerProcessor())
				{
					_tabletSpawnInstance.Controller.GtColliderTriggerProcessorsGroup.GetCurrentTriggerProcessor().ResetToDefaultAndTriggerButton();
					_tabletSpawnInstance.Controller.GtColliderTriggerProcessorsGroup.ClearAllTriggers();
				}
				_tabletSpawnInstance.uiVisible = false;
				_tabletSpawnInstance.cameraActive = true;
				ResetCameraModel();
				if (Application.platform == RuntimePlatform.Android)
				{
					SetPreviewActive(isActive: false);
				}
				cameraVisible = true;
				_shouldMoveCameraToNeck = false;
				_dummyTablet.SetDummyTabletBodyState(isActive: true);
				break;
			case CameraState.CameraSpawned:
				cameraPosition = CameraPosition.CameraDefault;
				_tabletSpawnInstance.uiVisible = true;
				_tabletSpawnInstance.cameraActive = true;
				if (Application.platform == RuntimePlatform.Android)
				{
					SetPreviewActive(isActive: true);
				}
				ResetCameraModel();
				cameraVisible = true;
				_shouldMoveCameraToNeck = false;
				_dummyTablet.SetDummyTabletBodyState(isActive: false);
				break;
			}
			_cameraState = value;
			LckBodyCameraSpawner.OnCameraStateChange?.Invoke(_cameraState);
		}
	}

	public CameraPosition cameraPosition
	{
		get
		{
			return _cameraPosition;
		}
		set
		{
			if (_cameraModelTransform != null && _cameraPosition != value)
			{
				switch (value)
				{
				case CameraPosition.CameraDefault:
					ChangeCameraModelParent(_cameraPositionDefault);
					_cameraPosition = CameraPosition.CameraDefault;
					break;
				case CameraPosition.CameraSlingshot:
					ChangeCameraModelParent(_cameraPositionSlingshot);
					_cameraPosition = CameraPosition.CameraSlingshot;
					break;
				case CameraPosition.NotVisible:
					break;
				}
			}
		}
	}

	private bool cameraVisible
	{
		get
		{
			return _cameraModelTransform.gameObject.activeSelf;
		}
		set
		{
			_cameraModelTransform.gameObject.SetActive(value);
			_cameraStrapRenderer.enabled = value;
		}
	}

	public static event CameraStateDelegate OnCameraStateChange;

	public void SetFollowTransform(Transform transform)
	{
		_followTransform = transform;
	}

	private void SetPreviewActive(bool isActive)
	{
		LckResult<LckService> service = LckService.GetService();
		if (!service.Success)
		{
			Debug.LogError("LCK Could not get Service" + service.Error.ToString());
		}
		else
		{
			service.Result?.SetPreviewActive(isActive);
		}
	}

	private void Awake()
	{
		_tabletSpawnInstance = new TabletSpawnInstance(_cameraSpawnPrefab, _cameraSpawnParentTransform);
	}

	private new void OnEnable()
	{
		base.OnEnable();
		InitCameraStrap();
		cameraState = CameraState.CameraDisabled;
		cameraPosition = CameraPosition.CameraDefault;
		ZoneManagement.OnZoneChange += OnZoneChanged;
		if (_swapTablet != null && _swapEmobi != null && _dummyTablet != null)
		{
			LckGameObjectSwapCosmetic swapTablet = _swapTablet;
			swapTablet.OnCosmeticSpawned = (Action<GameObject>)Delegate.Combine(swapTablet.OnCosmeticSpawned, new Action<GameObject>(_dummyTablet.OnTabletCosmeticSpawned));
			LckGameObjectSwapCosmetic swapEmobi = _swapEmobi;
			swapEmobi.OnCosmeticSpawned = (Action<GameObject>)Delegate.Combine(swapEmobi.OnCosmeticSpawned, new Action<GameObject>(_dummyTablet.OnEmobiCosmeticSpawned));
		}
	}

	private void Update()
	{
		_tabletSpawnInstance.Update();
	}

	private new void OnDisable()
	{
		base.OnDisable();
		ZoneManagement.OnZoneChange -= OnZoneChanged;
		if (_swapTablet != null && _swapEmobi != null && _dummyTablet != null)
		{
			LckGameObjectSwapCosmetic swapTablet = _swapTablet;
			swapTablet.OnCosmeticSpawned = (Action<GameObject>)Delegate.Remove(swapTablet.OnCosmeticSpawned, new Action<GameObject>(_dummyTablet.OnTabletCosmeticSpawned));
			LckGameObjectSwapCosmetic swapEmobi = _swapEmobi;
			swapEmobi.OnCosmeticSpawned = (Action<GameObject>)Delegate.Remove(swapEmobi.OnCosmeticSpawned, new Action<GameObject>(_dummyTablet.OnEmobiCosmeticSpawned));
		}
	}

	public override void Tick()
	{
		if (_followTransform != null && base.transform.parent != null)
		{
			Matrix4x4 localToWorldMatrix = base.transform.parent.localToWorldMatrix;
			Vector3 position = localToWorldMatrix.MultiplyPoint(_followTransform.localPosition + _followTransform.localRotation * new Vector3(0f, -0.05f, 0.1f));
			Quaternion rotation = Quaternion.LookRotation(localToWorldMatrix.MultiplyVector(_followTransform.localRotation * Vector3.forward), localToWorldMatrix.MultiplyVector(_followTransform.localRotation * Vector3.up));
			base.transform.SetPositionAndRotation(position, rotation);
		}
		switch (_cameraState)
		{
		case CameraState.CameraOnNeck:
			UpdateCameraStrap();
			if (_cameraModelGrabbable.isGrabbed)
			{
				GorillaGrabber grabber3 = _cameraModelGrabbable.grabber;
				Transform gorillaGrabberTransform2 = grabber3.transform;
				if (ShouldSpawnCamera(gorillaGrabberTransform2))
				{
					SpawnCamera(grabber3, gorillaGrabberTransform2);
					if (_returnToCameraMode.HasValue)
					{
						_tabletSpawnInstance?.Controller.SetCameraMode(_returnToCameraMode.Value);
						_returnToCameraMode = null;
					}
				}
			}
			else
			{
				ResetCameraModel();
			}
			break;
		case CameraState.CameraSpawned:
		{
			UpdateCameraStrap();
			if (_cameraModelGrabbable.isGrabbed)
			{
				GorillaGrabber grabber = _cameraModelGrabbable.grabber;
				Transform gorillaGrabberTransform = grabber.transform;
				if (ShouldSpawnCamera(gorillaGrabberTransform))
				{
					SpawnCamera(grabber, gorillaGrabberTransform);
				}
			}
			else
			{
				ResetCameraModel();
			}
			if (!_tabletSpawnInstance.isSpawned)
			{
				break;
			}
			Transform transform2;
			if (_tabletSpawnInstance.directGrabbable.isGrabbed)
			{
				GorillaGrabber grabber2 = _tabletSpawnInstance.directGrabbable.grabber;
				Transform transform = grabber2.transform;
				if (!ShouldSpawnCamera(transform))
				{
					cameraState = CameraState.CameraOnNeck;
					_cameraModelGrabbable.target.SetPositionAndRotation(transform.position, transform.rotation * Quaternion.Euler(_chestSpawnRotationOffset.x, _chestSpawnRotationOffset.y, _chestSpawnRotationOffset.z));
					_tabletSpawnInstance.directGrabbable.ForceRelease();
					_tabletSpawnInstance.SetParent(_cameraModelTransform);
					_tabletSpawnInstance.ResetLocalPose();
					_cameraModelGrabbable.ForceGrab(grabber2);
					_cameraModelGrabbable.onReleased += OnCameraModelReleased;
					if (_tabletSpawnInstance.Controller.CurrentCameraMode == CameraMode.Selfie)
					{
						_returnToCameraMode = CameraMode.Selfie;
						_tabletSpawnInstance.Controller.SetCameraMode(CameraMode.FirstPerson);
					}
				}
			}
			else if (_shouldMoveCameraToNeck && GtTag.TryGetTransform(GtTagType.HMD, out transform2) && Vector3.SqrMagnitude(base.transform.position - tabletSpawnInstance.position) >= _snapToNeckDistance * _snapToNeckDistance)
			{
				cameraState = CameraState.CameraOnNeck;
				_tabletSpawnInstance.SetParent(_cameraModelTransform);
				_tabletSpawnInstance.ResetLocalPose();
				_shouldMoveCameraToNeck = false;
			}
			break;
		}
		}
		if (!IsSlingshotActiveInHierarchy())
		{
			cameraPosition = CameraPosition.CameraDefault;
		}
		else
		{
			cameraPosition = CameraPosition.CameraSlingshot;
		}
	}

	private void OnZoneChanged(ZoneData[] zones)
	{
		if (_tabletSpawnInstance.isSpawned && !_tabletSpawnInstance.directGrabbable.isGrabbed)
		{
			_shouldMoveCameraToNeck = true;
		}
	}

	private void OnDestroy()
	{
		_tabletSpawnInstance.Dispose();
	}

	[ContextMenu("Put tablet on neck")]
	public void ManuallySetCameraOnNeck()
	{
		if (cameraState != CameraState.CameraOnNeck && cameraState != CameraState.CameraDisabled && _tabletSpawnInstance.isSpawned)
		{
			cameraState = CameraState.CameraOnNeck;
			_tabletSpawnInstance.SetParent(_cameraModelTransform);
			_tabletSpawnInstance.ResetLocalPose();
			_shouldMoveCameraToNeck = false;
			if (_tabletSpawnInstance.Controller.CurrentCameraMode == CameraMode.Selfie)
			{
				_returnToCameraMode = CameraMode.Selfie;
				_tabletSpawnInstance.Controller.SetCameraMode(CameraMode.FirstPerson);
			}
		}
	}

	private void OnCameraModelReleased()
	{
		_cameraModelGrabbable.onReleased -= OnCameraModelReleased;
		ResetCameraModel();
	}

	public void SpawnCamera(GorillaGrabber overrideGorillaGrabber, Transform transform)
	{
		if (!_tabletSpawnInstance.isSpawned)
		{
			_tabletSpawnInstance.SpawnCamera();
		}
		cameraState = CameraState.CameraSpawned;
		_cameraModelGrabbable.ForceRelease();
		_tabletSpawnInstance.ResetParent();
		Vector3 vector = Vector3.zero;
		Vector3 zero = Vector3.zero;
		zero = _rotationOffsetWindows;
		switch (overrideGorillaGrabber.XrNode)
		{
		case XRNode.LeftHand:
			vector = _leftHandSpawnOffsetWindows;
			zero.z = 12f;
			break;
		case XRNode.RightHand:
			vector = _rightHandSpawnOffsetWindows;
			zero.z = -12f;
			break;
		}
		if (!GTPlayer.Instance.IsDefaultScale)
		{
			vector *= 0.06f;
		}
		vector = transform.rotation * vector;
		_tabletSpawnInstance.SetPositionAndRotation(transform.position + vector, transform.rotation * Quaternion.Euler(zero));
		_tabletSpawnInstance.directGrabbable.ForceGrab(overrideGorillaGrabber);
		_tabletSpawnInstance.SetLocalScale(Vector3.one);
	}

	private bool ShouldSpawnCamera(Transform gorillaGrabberTransform)
	{
		Matrix4x4 worldToLocalMatrix = base.transform.worldToLocalMatrix;
		Vector3 vector = worldToLocalMatrix.MultiplyPoint(_cameraModelOriginTransform.position);
		Vector3 vector2 = worldToLocalMatrix.MultiplyPoint(gorillaGrabberTransform.position);
		return Vector3.SqrMagnitude(vector - vector2) >= _activateDistance * _activateDistance;
	}

	private void ChangeCameraModelParent(Transform transform)
	{
		if (_cameraModelTransform != null)
		{
			_cameraModelGrabbable.SetOriginalTargetParent(transform);
			if (!_cameraModelGrabbable.isGrabbed)
			{
				_cameraModelTransform.transform.parent = transform;
				_cameraModelTransform.transform.localPosition = Vector3.zero;
			}
		}
	}

	private void InitCameraStrap()
	{
		_cameraStrapRenderer.positionCount = _cameraStrapPoints.Length;
		_cameraStrapPositions = new Vector3[_cameraStrapPoints.Length];
	}

	private void UpdateCameraStrap()
	{
		for (int i = 0; i < _cameraStrapPoints.Length; i++)
		{
			_cameraStrapPositions[i] = _cameraStrapPoints[i].position;
		}
		_cameraStrapRenderer.SetPositions(_cameraStrapPositions);
		Vector3 lossyScale = base.transform.lossyScale;
		float num = (lossyScale.x + lossyScale.y + lossyScale.z) * 0.3333333f;
		_cameraStrapRenderer.widthMultiplier = num * 0.02f;
		Color color = ((cameraState == CameraState.CameraSpawned) ? _ghostColor : _normalColor);
		_cameraStrapRenderer.startColor = color;
		_cameraStrapRenderer.endColor = color;
	}

	private void ResetCameraModel()
	{
		_cameraModelTransform.localPosition = Vector3.zero;
		_cameraModelTransform.localRotation = Quaternion.identity;
	}

	private VRRig GetLocalRig()
	{
		if (_localRig == null)
		{
			_localRig = VRRigCache.Instance.localRig.Rig;
		}
		return _localRig;
	}

	private bool IsSlingshotHeldInHand(out bool leftHand, out bool rightHand)
	{
		VRRig localRig = GetLocalRig();
		if (localRig == null)
		{
			leftHand = false;
			rightHand = false;
			return false;
		}
		leftHand = localRig.projectileWeapon.InLeftHand();
		rightHand = localRig.projectileWeapon.InRightHand();
		return localRig.projectileWeapon.InHand();
	}

	private bool IsSlingshotActiveInHierarchy()
	{
		VRRig localRig = GetLocalRig();
		if (localRig == null || localRig.projectileWeapon == null)
		{
			return false;
		}
		return localRig.projectileWeapon.gameObject.activeInHierarchy;
	}
}
