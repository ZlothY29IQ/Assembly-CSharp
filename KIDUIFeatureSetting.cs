using System;
using KID.Model;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KIDUIFeatureSetting : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _featureNameTxt;

	[SerializeField]
	private TMP_Text _featureStatusTxt;

	[SerializeField]
	private KIDUIToggle _featureToggle;

	[SerializeField]
	private GameObject _tickIcon;

	[SerializeField]
	private GameObject _crossIcon;

	[SerializeField]
	private GameObject _guardianManagedLocked;

	[SerializeField]
	private GameObject _guardianManagedEnabled;

	private bool _hasToggle;

	private string _featureName;

	private string _permissionName;

	private string _enabledTextStr;

	private string _disabledTextStr;

	private EKIDFeatures _featureType;

	private Action<EKIDFeatures> _onChangeCallback;

	private KIDUI_MainScreen.FeatureToggleSetup _feature;

	public bool AlwaysCheckFeatureSetting { get; private set; }

	public void CreateNewFeatureSettingGuardianManaged(KIDUI_MainScreen.FeatureToggleSetup feature, bool isEnabled)
	{
		CreateNewFeatureSettingWithoutToggle(feature);
		_guardianManagedEnabled.SetActive(isEnabled);
		_guardianManagedLocked.SetActive(!isEnabled);
	}

	public KIDUIToggle CreateNewFeatureSettingWithToggle(KIDUI_MainScreen.FeatureToggleSetup feature, bool initialState = false, bool alwaysCheckFeatureSetting = false)
	{
		SetFeatureData(feature, alwaysCheckFeatureSetting, featureToggleEnabled: true);
		_featureToggle.SetValue(initialState);
		_featureToggle?.RegisterOnChangeEvent(SetFeatureName);
		return _featureToggle;
	}

	public void CreateNewFeatureSettingWithoutToggle(KIDUI_MainScreen.FeatureToggleSetup feature, bool alwaysCheckFeatureSetting = false)
	{
		SetFeatureData(feature, alwaysCheckFeatureSetting, featureToggleEnabled: false);
	}

	private void SetFeatureData(KIDUI_MainScreen.FeatureToggleSetup feature, bool alwaysCheckFeatureSetting, bool featureToggleEnabled)
	{
		if (!LocalisationManager.TryGetTranslationForCurrentLocaleWithLocString(feature.enabledText, out var result, "ON"))
		{
			Debug.LogError($"[LOCALIZATION::FEATURE_SETTING] Failed to get key for  k-ID Feature [{feature.featureName}]\n[{feature.enabledText}]", this);
		}
		_enabledTextStr = result;
		if (!LocalisationManager.TryGetTranslationForCurrentLocaleWithLocString(feature.disabledText, out result, "OFF"))
		{
			Debug.LogError($"[LOCALIZATION::FEATURE_SETTING] Failed to get key for  k-ID Feature [{feature.featureName}]\n[{feature.disabledText}]", this);
		}
		_disabledTextStr = result;
		_hasToggle = featureToggleEnabled;
		_featureType = feature.linkedFeature;
		if (!LocalisationManager.TryGetTranslationForCurrentLocaleWithLocString(feature.featureName, out result, feature.permissionName))
		{
			Debug.LogError($"[LOCALIZATION::FeatureSetting] Failed to get key for k-ID Feature [{feature.featureName}]\n[{feature.disabledText}]", this);
		}
		_featureName = result;
		SetFeatureName();
		GameObject obj = base.gameObject;
		obj.name = obj.name + "_" + feature.featureName;
		_permissionName = feature.permissionName;
		_featureToggle.gameObject.SetActive(featureToggleEnabled);
		AlwaysCheckFeatureSetting = alwaysCheckFeatureSetting;
		_feature = feature;
	}

	public void RefreshTextOnLanguageChanged()
	{
		if (!LocalisationManager.TryGetTranslationForCurrentLocaleWithLocString(_feature.enabledText, out var result, "ON"))
		{
			Debug.LogError($"[LOCALIZATION::FeatureSetting] Failed to get key for Game Mode [{_feature.enabledText}]");
		}
		_enabledTextStr = result;
		Debug.Log("[KIDUIFeatureSetting::Language] Refreshed enabled text: " + _enabledTextStr);
		if (!LocalisationManager.TryGetTranslationForCurrentLocaleWithLocString(_feature.disabledText, out result, "OFF"))
		{
			Debug.LogError($"[LOCALIZATION::FeatureSetting] Failed to get key for Game Mode [{_feature.disabledText}]");
		}
		_disabledTextStr = result;
		Debug.Log("[KIDUIFeatureSetting::Language] Refreshed disabled text: " + _disabledTextStr);
		if (!LocalisationManager.TryGetTranslationForCurrentLocaleWithLocString(_feature.featureName, out result, _feature.permissionName))
		{
			Debug.LogError($"[LOCALIZATION::FeatureSetting] Failed to get key for Game Mode [{_feature.disabledText}]");
		}
		_featureName = result;
		Debug.Log("[KIDUIFeatureSetting::Language] Refreshed feature name text: " + _featureName);
		SetFeatureName();
	}

	public void UnregisterOnToggleChangeEvent(Action action)
	{
		_featureToggle.UnregisterOnChangeEvent(action);
	}

	public void RegisterToggleOnEvent(Action action)
	{
		_featureToggle.RegisterToggleOnEvent(action);
	}

	public void UnregisterToggleOnEvent(Action action)
	{
		_featureToggle.UnregisterToggleOnEvent(action);
	}

	public void RegisterToggleOffEvent(Action action)
	{
		_featureToggle.RegisterToggleOffEvent(action);
	}

	public void UnregisterToggleOffEvent(Action action)
	{
		_featureToggle.UnregisterToggleOffEvent(action);
	}

	public bool GetFeatureToggleState()
	{
		if (_hasToggle)
		{
			return _featureToggle.IsOn;
		}
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(_featureType);
		if (permissionDataByFeature.ManagedBy != Permission.ManagedByEnum.GUARDIAN)
		{
			Debug.LogError("[KID::FeatureSetting] GetToggleState: feature has no toggle AND is not managed by Guardian");
		}
		return permissionDataByFeature.Enabled;
	}

	public bool GetHasToggle()
	{
		return _hasToggle;
	}

	public void SetFeatureSettingVisible(bool visible)
	{
		base.gameObject.SetActive(visible);
	}

	public void SetFeatureToggle(bool enableToggle)
	{
		_featureToggle.interactable = enableToggle;
	}

	public void SetGuardianManagedState(bool isEnabled)
	{
		_featureToggle.gameObject.SetActive(value: false);
		_guardianManagedEnabled.SetActive(isEnabled);
		_guardianManagedLocked.SetActive(!isEnabled);
		SetupGuardianManagedClickHandlers();
		SetFeatureName();
	}

	public void SetPlayerManagedState(bool isInteractable, bool isOptedIn)
	{
		_featureToggle.gameObject.SetActive(value: true);
		_guardianManagedEnabled.SetActive(value: false);
		_guardianManagedLocked.SetActive(value: false);
		_featureToggle.interactable = isInteractable;
		_featureToggle.SetValue(isOptedIn);
	}

	private void SetFeatureName()
	{
		string text = (GetFeatureToggleState() ? ("<b>(" + _enabledTextStr + ")</b>") : ("<b>(" + _disabledTextStr + ")</b>"));
		_featureNameTxt.text = "<b>" + _featureName + "</b>";
		_featureStatusTxt.text = text ?? "";
	}

	private void SetupGuardianManagedClickHandlers()
	{
		AddDeniedSoundHandler(_guardianManagedEnabled);
		AddDeniedSoundHandler(_guardianManagedLocked);
	}

	private void AddDeniedSoundHandler(GameObject obj)
	{
		if (!(obj == null))
		{
			EventTrigger component = obj.GetComponent<EventTrigger>();
			if (component != null)
			{
				UnityEngine.Object.DestroyImmediate(component);
			}
			EventTrigger eventTrigger = obj.AddComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerDown;
			entry.callback.AddListener(delegate
			{
				Debug.Log("[KIDUIFeatureSetting] Guardian-managed feature clicked - playing denied sound");
				KIDAudioManager.Instance?.PlaySound(KIDAudioManager.KIDSoundType.Denied);
			});
			eventTrigger.triggers.Add(entry);
			EnsureRaycastTarget(obj);
		}
	}

	private void EnsureRaycastTarget(GameObject obj)
	{
		Graphic component = obj.GetComponent<Graphic>();
		if (component != null)
		{
			component.raycastTarget = true;
			return;
		}
		Image image = obj.GetComponent<Image>();
		if (image == null)
		{
			image = obj.AddComponent<Image>();
		}
		image.color = new Color(0f, 0f, 0f, 0f);
		image.raycastTarget = true;
	}
}
