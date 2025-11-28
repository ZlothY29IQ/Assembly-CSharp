using System;
using System.Collections;
using GorillaLocomotion;
using Liv.Lck.Cosmetics;
using Liv.Lck.GorillaTag;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class LckWallCameraSpawner : MonoBehaviour
{
	public enum WallSpawnerState
	{
		CameraOnHook,
		CameraDragging,
		CameraOffHook
	}

	[SerializeField]
	private GameObject _lckBodySpawnerPrefab;

	[SerializeField]
	private LckDirectGrabbable _cameraHandleGrabbable;

	[SerializeField]
	private Transform _cameraModelOriginTransform;

	[SerializeField]
	private Transform _cameraModelTransform;

	[SerializeField]
	private LineRenderer _cameraStrapRenderer;

	[SerializeField]
	private float _activateDistance = 0.25f;

	[SerializeField]
	private Transform[] _cameraStrapPoints;

	private Vector3[] _cameraStrapPositions;

	private float _spawnRotationOffsetAndroid = -80f;

	private float _spawnRotationOffsetWindows = -55f;

	[SerializeField]
	private Color _normalColor = Color.red;

	[Header("Cosmetics References")]
	[SerializeField]
	private GtDummyTablet _dummyTablet;

	[SerializeField]
	private LckGameObjectSwapCosmetic _swapTablet;

	[SerializeField]
	private LckGameObjectSwapCosmetic _swapEmobi;

	private static LckBodyCameraSpawner _bodySpawner;

	private static Camera _prewarmCamera;

	private WallSpawnerState _wallSpawnerState;

	public WallSpawnerState wallSpawnerState
	{
		get
		{
			return _wallSpawnerState;
		}
		set
		{
			switch (value)
			{
			case WallSpawnerState.CameraOnHook:
				ResetCameraModel();
				UpdateCameraStrap();
				cameraVisible = true;
				break;
			case WallSpawnerState.CameraOffHook:
				ResetCameraModel();
				UpdateCameraStrap();
				cameraVisible = true;
				break;
			}
			_wallSpawnerState = value;
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
			_cameraStrapRenderer.gameObject.SetActive(value);
		}
	}

	private LckBodyCameraSpawner GetOrCreateBodyCameraSpawner()
	{
		if (_bodySpawner != null)
		{
			return _bodySpawner;
		}
		GTPlayer instance = GTPlayer.Instance;
		if (instance == null)
		{
			Debug.LogError("Unable to find Player!");
			return null;
		}
		AddGTag(Camera.main.gameObject, GtTagType.HMD);
		AddGTag(instance.gameObject, GtTagType.Player);
		Transform transform = instance.bodyCollider.transform;
		GameObject obj = UnityEngine.Object.Instantiate(_lckBodySpawnerPrefab, transform.parent);
		Transform obj2 = obj.transform;
		obj2.localPosition = Vector3.zero;
		obj2.localRotation = Quaternion.identity;
		obj2.localScale = Vector3.one;
		_bodySpawner = obj.GetComponent<LckBodyCameraSpawner>();
		_bodySpawner.SetFollowTransform(transform);
		GorillaTagger instance2 = GorillaTagger.Instance;
		if (instance2 != null)
		{
			AddGTag(instance2.leftHandTriggerCollider, GtTagType.LeftHand);
			AddGTag(instance2.rightHandTriggerCollider, GtTagType.RightHand);
		}
		else
		{
			Debug.LogError("Unable to find GorillaTagger!");
		}
		return _bodySpawner;
	}

	private static void AddGTag(GameObject go, GtTagType gtTagType)
	{
		if (!go.GetComponent<GtTag>())
		{
			GtTag gtTag = go.AddComponent<GtTag>();
			gtTag.gtTagType = gtTagType;
			gtTag.enabled = true;
		}
	}

	private void Awake()
	{
		InitCameraStrap();
	}

	private void OnEnable()
	{
		if (_swapTablet != null && _swapEmobi != null && _dummyTablet != null)
		{
			LckGameObjectSwapCosmetic swapTablet = _swapTablet;
			swapTablet.OnCosmeticSpawned = (Action<GameObject>)Delegate.Combine(swapTablet.OnCosmeticSpawned, new Action<GameObject>(_dummyTablet.OnTabletCosmeticSpawned));
			LckGameObjectSwapCosmetic swapEmobi = _swapEmobi;
			swapEmobi.OnCosmeticSpawned = (Action<GameObject>)Delegate.Combine(swapEmobi.OnCosmeticSpawned, new Action<GameObject>(_dummyTablet.OnEmobiCosmeticSpawned));
		}
		_cameraHandleGrabbable.onGrabbed += OnGrabbed;
		_cameraHandleGrabbable.onReleased += OnReleased;
		wallSpawnerState = WallSpawnerState.CameraOnHook;
	}

	private void Start()
	{
		CreatePrewarmCamera();
	}

	private void Update()
	{
		switch (_wallSpawnerState)
		{
		case WallSpawnerState.CameraOnHook:
			if (GetOrCreateBodyCameraSpawner() == null)
			{
				Debug.LogError("Lck, Unable to find LckBodyCameraSpawner");
				base.gameObject.SetActive(value: false);
			}
			else if (_bodySpawner.cameraState == LckBodyCameraSpawner.CameraState.CameraSpawned && _bodySpawner.tabletSpawnInstance.isSpawned && _bodySpawner.tabletSpawnInstance.directGrabbable.isGrabbed)
			{
				LckDirectGrabbable directGrabbable = _bodySpawner.tabletSpawnInstance.directGrabbable;
				GorillaGrabber grabber = directGrabbable.grabber;
				if (!ShouldSpawnCamera(grabber.transform))
				{
					directGrabbable.ForceRelease();
					_bodySpawner.cameraState = LckBodyCameraSpawner.CameraState.CameraDisabled;
					_cameraHandleGrabbable.target.SetPositionAndRotation(grabber.transform.position, grabber.transform.rotation * Quaternion.Euler(_spawnRotationOffsetWindows, 180f, 0f));
					_cameraHandleGrabbable.ForceGrab(grabber);
				}
			}
			break;
		case WallSpawnerState.CameraDragging:
			UpdateCameraStrap();
			if (ShouldSpawnCamera(_cameraHandleGrabbable.grabber.transform))
			{
				SpawnCamera(_cameraHandleGrabbable.grabber);
			}
			break;
		}
	}

	private void OnDisable()
	{
		if (_swapTablet != null && _swapEmobi != null && _dummyTablet != null)
		{
			LckGameObjectSwapCosmetic swapTablet = _swapTablet;
			swapTablet.OnCosmeticSpawned = (Action<GameObject>)Delegate.Remove(swapTablet.OnCosmeticSpawned, new Action<GameObject>(_dummyTablet.OnTabletCosmeticSpawned));
			LckGameObjectSwapCosmetic swapEmobi = _swapEmobi;
			swapEmobi.OnCosmeticSpawned = (Action<GameObject>)Delegate.Remove(swapEmobi.OnCosmeticSpawned, new Action<GameObject>(_dummyTablet.OnEmobiCosmeticSpawned));
		}
		_cameraHandleGrabbable.onGrabbed -= OnGrabbed;
		_cameraHandleGrabbable.onReleased -= OnReleased;
	}

	private void SpawnCamera(GorillaGrabber lastGorillaGrabber)
	{
		if (_bodySpawner == null)
		{
			Debug.LogError("Lck, unable to spawn camera, body spawner is null!");
			return;
		}
		if (_bodySpawner.tabletSpawnInstance != null && _bodySpawner.tabletSpawnInstance.Controller != null && _bodySpawner.tabletSpawnInstance.Controller.GtColliderTriggerProcessorsGroup != null && (bool)_bodySpawner.tabletSpawnInstance.Controller.GtColliderTriggerProcessorsGroup.GetCurrentTriggerProcessor())
		{
			_bodySpawner.tabletSpawnInstance.Controller.GtColliderTriggerProcessorsGroup.GetCurrentTriggerProcessor().ResetToDefaultAndTriggerButton();
			_bodySpawner.tabletSpawnInstance.Controller.GtColliderTriggerProcessorsGroup.ClearAllTriggers();
		}
		cameraVisible = false;
		_cameraHandleGrabbable.ForceRelease();
		_bodySpawner.SpawnCamera(lastGorillaGrabber, lastGorillaGrabber.transform);
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
		LineRenderer cameraStrapRenderer = _cameraStrapRenderer;
		Color startColor = (_cameraStrapRenderer.endColor = _normalColor);
		cameraStrapRenderer.startColor = startColor;
	}

	private void ResetCameraModel()
	{
		_cameraModelTransform.localPosition = Vector3.zero;
		_cameraModelTransform.localRotation = Quaternion.identity;
	}

	private bool ShouldSpawnCamera(Transform gorillaGrabberTransform)
	{
		Matrix4x4 worldToLocalMatrix = base.transform.worldToLocalMatrix;
		Vector3 vector = worldToLocalMatrix.MultiplyPoint(_cameraModelOriginTransform.position);
		Vector3 vector2 = worldToLocalMatrix.MultiplyPoint(gorillaGrabberTransform.position);
		return Vector3.SqrMagnitude(vector - vector2) >= _activateDistance * _activateDistance;
	}

	private void OnGrabbed()
	{
		wallSpawnerState = WallSpawnerState.CameraDragging;
	}

	private void OnReleased()
	{
		wallSpawnerState = WallSpawnerState.CameraOnHook;
	}

	private void CreatePrewarmCamera()
	{
		if (!(_prewarmCamera != null))
		{
			GameObject obj = new GameObject("prewarm camera");
			obj.transform.SetParent(base.transform);
			_prewarmCamera = obj.AddComponent<Camera>();
			Camera main = Camera.main;
			_prewarmCamera.clearFlags = main.clearFlags;
			_prewarmCamera.fieldOfView = main.fieldOfView;
			_prewarmCamera.nearClipPlane = main.nearClipPlane;
			_prewarmCamera.farClipPlane = main.farClipPlane;
			_prewarmCamera.cullingMask = main.cullingMask;
			_prewarmCamera.tag = "Untagged";
			_prewarmCamera.stereoTargetEye = StereoTargetEyeMask.None;
			_prewarmCamera.targetTexture = new RenderTexture(32, 32, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D32_SFloat_S8_UInt);
			_prewarmCamera.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
			StartCoroutine(DestroyPrewarmCameraDelayed());
		}
	}

	private IEnumerator DestroyPrewarmCameraDelayed()
	{
		yield return new WaitForSeconds(1f);
		DestroyPrewarmCamera();
	}

	private void DestroyPrewarmCamera()
	{
		if (!(_prewarmCamera == null))
		{
			RenderTexture targetTexture = _prewarmCamera.targetTexture;
			_prewarmCamera.targetTexture = null;
			targetTexture.Release();
			UnityEngine.Object.Destroy(_prewarmCamera.gameObject);
			_prewarmCamera = null;
		}
	}
}
