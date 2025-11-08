using GorillaExtensions;
using GorillaLocomotion;
using GorillaTag.Rendering;
using GorillaTagScripts.VirtualStumpCustomMaps;
using GT_CustomMapSupportRuntime;
using UnityEngine;

public class CMSZoneShaderSettingsTrigger : MonoBehaviour
{
	public GameObject shaderSettingsObject;

	public bool activateCustomMapDefaults;

	public bool activateOnEnable;

	public void OnEnable()
	{
		if (activateOnEnable)
		{
			ActivateShaderSettings();
		}
	}

	public void CopySettings(ZoneShaderTriggerSettings triggerSettings)
	{
		base.gameObject.layer = UnityLayer.GorillaBoundary.ToLayerIndex();
		activateOnEnable = triggerSettings.activateOnEnable;
		if (triggerSettings.activationType == ZoneShaderTriggerSettings.ActivationType.ActivateCustomMapDefaults)
		{
			activateCustomMapDefaults = true;
			return;
		}
		GameObject zoneShaderSettingsObject = triggerSettings.zoneShaderSettingsObject;
		if (zoneShaderSettingsObject.IsNotNull())
		{
			shaderSettingsObject = zoneShaderSettingsObject;
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.bodyCollider)
		{
			ActivateShaderSettings();
		}
	}

	private void ActivateShaderSettings()
	{
		if (activateCustomMapDefaults)
		{
			CustomMapManager.ActivateDefaultZoneShaderSettings();
		}
		else if (shaderSettingsObject.IsNotNull())
		{
			ZoneShaderSettings component = shaderSettingsObject.GetComponent<ZoneShaderSettings>();
			if (component.IsNotNull())
			{
				component.BecomeActiveInstance();
			}
		}
	}
}
