using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaLocomotion;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using Valve.VR;

public class PrivateUIRoom : MonoBehaviourTick
{
	[Flags]
	public enum OverlaySource
	{
		KID = 1,
		ModIO = 2,
		CustomMap = 4,
		AlarmClock = 8
	}

	[SerializeField]
	private TextMeshPro _text;

	[SerializeField]
	private float _textDistance = 4f;

	[SerializeField]
	private GameObject occluder;

	[SerializeField]
	private LayerMask visibleLayers;

	[SerializeField]
	private GameObject leftHandObject;

	[SerializeField]
	private GameObject rightHandObject;

	[SerializeField]
	private MeshRenderer backgroundRenderer;

	[SerializeField]
	private string backgroundDirectionPropertyName = "_SpotDirection";

	private int backgroundDirectionPropertyID;

	private int savedCullingLayers;

	private Transform _uiRoot;

	private Transform focusTransform;

	private List<Transform> ui;

	private Dictionary<Transform, Transform> uiParents;

	private float _initialAudioVolume;

	private bool inOverlay;

	private OverlaySource overlayForcedSources;

	private static PrivateUIRoom instance;

	private Vector3 lastStablePosition;

	private Quaternion lastStableRotation;

	[SerializeField]
	private float verticalPlay = 0.1f;

	[SerializeField]
	private float lateralPlay = 0.5f;

	[SerializeField]
	private float rotationalPlay = 45f;

	private int? savedCullingLayersShoudlerCam;

	private static Camera _shoulderCameraReference;

	private static CinemachineVirtualCamera _virtualCameraReference;

	private bool overlayForcedActive => overlayForcedSources != (OverlaySource)0;

	private GTPlayer localPlayer => GTPlayer.Instance;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			occluder.SetActive(value: false);
			leftHandObject.SetActive(value: false);
			rightHandObject.SetActive(value: false);
			ui = new List<Transform>();
			uiParents = new Dictionary<Transform, Transform>();
			backgroundDirectionPropertyID = Shader.PropertyToID(backgroundDirectionPropertyName);
			_uiRoot = new GameObject("UIRoot").transform;
			_uiRoot.parent = base.transform;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private new void OnEnable()
	{
		base.OnEnable();
		SteamVR_Events.System(EVREventType.VREvent_InputFocusChanged).Listen(ToggleHands);
	}

	private new void OnDisable()
	{
		base.OnDisable();
		SteamVR_Events.System(EVREventType.VREvent_InputFocusChanged).Remove(ToggleHands);
	}

	private static bool FindShoulderCamera()
	{
		if (_shoulderCameraReference.IsNotNull())
		{
			return true;
		}
		if (GorillaTagger.Instance.IsNull())
		{
			return false;
		}
		_shoulderCameraReference = GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<Camera>(includeInactive: true);
		if (_shoulderCameraReference == null)
		{
			Debug.LogError("[PRIVATE_UI_ROOMS] Could not find Shoulder Camera");
			return false;
		}
		_virtualCameraReference = _shoulderCameraReference.GetComponentInChildren<CinemachineVirtualCamera>();
		return true;
	}

	private void ToggleHands(VREvent_t ev)
	{
		Debug.Log($"[PrivateUIRoom::ToggleHands] Toggling hands visibility. Event: {ev.eventType} ({(EVREventType)ev.eventType})");
		Debug.Log($"[PrivateUIRoom::ToggleHands] _handsShowing: {instance.rightHandObject.activeSelf}");
		if (instance.rightHandObject.activeSelf)
		{
			HideHands();
		}
		else
		{
			ShowHands();
		}
	}

	private void HideHands()
	{
		Debug.Log("[PrivateUIRoom::OnSteamMenuShown] Steam menu shown, disabling hands.");
		instance.leftHandObject.SetActive(value: false);
		instance.rightHandObject.SetActive(value: false);
	}

	private void ShowHands()
	{
		Debug.Log("[PrivateUIRoom::OnSteamMenuShown] Steam menu hidden, re-enabling hands.");
		instance.leftHandObject.SetActive(value: true);
		instance.rightHandObject.SetActive(value: true);
	}

	private void ToggleLevelVisibility(bool levelShouldBeVisible)
	{
		Camera component = GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
		if (levelShouldBeVisible)
		{
			component.cullingMask = savedCullingLayers;
			if (savedCullingLayersShoudlerCam.HasValue)
			{
				_shoulderCameraReference.cullingMask = savedCullingLayersShoudlerCam.Value;
				savedCullingLayersShoudlerCam = null;
			}
			return;
		}
		savedCullingLayers = component.cullingMask;
		component.cullingMask = visibleLayers;
		if (FindShoulderCamera())
		{
			savedCullingLayersShoudlerCam = _shoulderCameraReference.cullingMask;
			_shoulderCameraReference.cullingMask = visibleLayers;
			_virtualCameraReference.enabled = false;
		}
	}

	private static void StopOverlay()
	{
		instance.localPlayer.inOverlay = false;
		instance.inOverlay = false;
		instance.localPlayer.disableMovement = false;
		instance.localPlayer.InReportMenu = false;
		instance.ToggleLevelVisibility(levelShouldBeVisible: true);
		instance.occluder.SetActive(value: false);
		instance.leftHandObject.SetActive(value: false);
		instance.rightHandObject.SetActive(value: false);
		_virtualCameraReference.enabled = true;
		KIDAudioManager.Instance.SetKIDUIAudioActive(active: false);
		Debug.Log("[PrivateUIRoom::StopOverlay] Re-enabling Game Audio");
	}

	private void GetIdealScreenPositionRotation(out Vector3 position, out Quaternion rotation, out Vector3 scale)
	{
		GameObject mainCamera = GorillaTagger.Instance.mainCamera;
		rotation = Quaternion.Euler(0f, mainCamera.transform.eulerAngles.y, 0f);
		scale = localPlayer.turnParent.transform.localScale;
		position = mainCamera.transform.position + rotation * Vector3.zero * scale.x;
	}

	private static void AssignShoulderCameraToCanvases(Transform focus)
	{
		Debug.Log("[KID::PrivateUIRoom::CanvasCameraAssigner] setting up canvases with shoulder camera.");
		if (FindShoulderCamera())
		{
			Canvas componentInChildren = focus.GetComponentInChildren<Canvas>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.worldCamera = _shoulderCameraReference;
				Debug.Log("[KID::PrivateUIRoom::CanvasCameraAssigner] Assigned shoulder camera to Canvas: " + componentInChildren.name);
			}
			else
			{
				Debug.LogError("[KID::PrivateUIRoom::CanvasCameraAssigner] No Canvas component found on this GameObject.");
			}
		}
	}

	public static void AddUI(Transform focus)
	{
		if (instance.ui.Contains(focus))
		{
			return;
		}
		instance._text.text = "";
		AssignShoulderCameraToCanvases(focus);
		instance.uiParents.Add(focus, focus.parent);
		focus.gameObject.SetActive(value: false);
		focus.parent = instance._uiRoot;
		focus.localPosition = Vector3.zero;
		focus.localRotation = Quaternion.identity;
		instance.ui.Add(focus);
		if (instance.ui.Count == 1 && instance.focusTransform == null)
		{
			instance.focusTransform = instance.ui[0];
			instance.focusTransform.gameObject.SetActive(value: true);
			if (!instance.inOverlay)
			{
				StartOverlay();
			}
		}
		instance.UpdateUIPositionAndRotation();
	}

	public static void RemoveUI(Transform focus)
	{
		if (instance.ui.Contains(focus))
		{
			focus.gameObject.SetActive(value: false);
			instance.ui.Remove(focus);
			if (instance.focusTransform == focus)
			{
				instance.focusTransform = null;
			}
			if (instance.uiParents[focus] != null)
			{
				focus.parent = instance.uiParents[focus];
				instance.uiParents.Remove(focus);
			}
			else
			{
				UnityEngine.Object.Destroy(focus.gameObject);
			}
			if (instance.ui.Count > 0)
			{
				instance.focusTransform = instance.ui[0];
				instance.focusTransform.gameObject.SetActive(value: true);
			}
			else if (!instance.overlayForcedActive)
			{
				StopOverlay();
			}
		}
	}

	public static void ForceStartOverlay(OverlaySource source, string text = "")
	{
		if (!(instance == null))
		{
			instance.overlayForcedSources |= source;
			if (!instance.inOverlay)
			{
				instance._text.text = text;
				StartOverlay();
			}
		}
	}

	public static void StopForcedOverlay(OverlaySource source)
	{
		if (!(instance == null))
		{
			instance.overlayForcedSources &= ~source;
			if (!instance.overlayForcedActive && instance.ui.Count == 0 && instance.inOverlay)
			{
				StopOverlay();
			}
		}
	}

	private static void StartOverlay()
	{
		instance.GetIdealScreenPositionRotation(out var _, out var _, out var scale);
		instance.leftHandObject.transform.localScale = scale;
		instance.rightHandObject.transform.localScale = scale;
		instance.occluder.transform.localScale = scale;
		instance.localPlayer.InReportMenu = true;
		instance.localPlayer.disableMovement = true;
		instance.occluder.SetActive(value: true);
		instance.rightHandObject.SetActive(value: true);
		instance.leftHandObject.SetActive(value: true);
		instance.ToggleLevelVisibility(levelShouldBeVisible: false);
		instance.localPlayer.inOverlay = true;
		instance.inOverlay = true;
		KIDAudioManager.Instance.SetKIDUIAudioActive(active: true);
		Debug.Log("[PrivateUIRoom::StartOverlay] Muting Game Audio");
	}

	public override void Tick()
	{
		if (localPlayer.InReportMenu)
		{
			occluder.transform.position = GorillaTagger.Instance.mainCamera.transform.position;
			Transform controllerTransform = localPlayer.GetControllerTransform(isLeftHand: true);
			Transform controllerTransform2 = localPlayer.GetControllerTransform(isLeftHand: false);
			rightHandObject.transform.SetPositionAndRotation(controllerTransform2.position, controllerTransform2.rotation);
			leftHandObject.transform.SetPositionAndRotation(controllerTransform.position, controllerTransform.rotation);
			if (ShouldUpdateRotation())
			{
				UpdateUIPositionAndRotation();
			}
			else if (ShouldUpdatePosition())
			{
				UpdateUIPosition();
			}
		}
	}

	private bool ShouldUpdateRotation()
	{
		float magnitude = (GorillaTagger.Instance.mainCamera.transform.position - lastStablePosition).X_Z().magnitude;
		float num = Quaternion.Angle(b: Quaternion.Euler(0f, GorillaTagger.Instance.mainCamera.transform.rotation.eulerAngles.y, 0f), a: lastStableRotation);
		if (!(magnitude > lateralPlay))
		{
			return num >= rotationalPlay;
		}
		return true;
	}

	private bool ShouldUpdatePosition()
	{
		return Mathf.Abs(GorillaTagger.Instance.mainCamera.transform.position.y - lastStablePosition.y) > verticalPlay;
	}

	private void UpdateUIPositionAndRotation()
	{
		Transform transform = GorillaTagger.Instance.mainCamera.transform;
		lastStablePosition = transform.position;
		lastStableRotation = transform.rotation;
		Vector3 normalized = transform.forward.X_Z().normalized;
		_uiRoot.SetPositionAndRotation(lastStablePosition + normalized * 0.02f, Quaternion.LookRotation(normalized));
		_shoulderCameraReference.transform.position = _uiRoot.position;
		_shoulderCameraReference.transform.rotation = _uiRoot.rotation;
		backgroundRenderer.material.SetVector(backgroundDirectionPropertyID, backgroundRenderer.transform.InverseTransformDirection(normalized));
		SetTextPositionAndRotation(transform);
	}

	private void SetTextPositionAndRotation(Transform pov)
	{
		if (_text.enabled && !string.IsNullOrEmpty(_text.text))
		{
			_text.transform.position = pov.position + _textDistance * (pov.rotation * Vector3.forward);
			_text.transform.rotation = Quaternion.LookRotation(pov.rotation * Vector3.forward, Vector3.up);
		}
	}

	private void UpdateUIPosition()
	{
		Transform transform = GorillaTagger.Instance.mainCamera.transform;
		lastStablePosition = transform.position;
		_uiRoot.position = lastStablePosition + lastStableRotation * new Vector3(0f, 0f, 0.02f);
		_shoulderCameraReference.transform.position = _uiRoot.position;
		SetTextPositionAndRotation(transform);
	}

	public static bool GetInOverlay()
	{
		if (instance == null)
		{
			return false;
		}
		return instance.inOverlay;
	}
}
