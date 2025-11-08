using GorillaTag.Rendering;
using UnityEngine;

public class GorillaTriggerBoxShaderSettings : GorillaTriggerBox
{
	[SerializeField]
	private XSceneRef settingsRef;

	[SerializeField]
	private ZoneShaderSettings sameSceneSettingsRef;

	private ZoneShaderSettings settings;

	private void Awake()
	{
		if (sameSceneSettingsRef != null)
		{
			settings = sameSceneSettingsRef;
		}
		else
		{
			settingsRef.TryResolve(out settings);
		}
	}

	public override void OnBoxTriggered()
	{
		if (settings == null)
		{
			if (sameSceneSettingsRef != null)
			{
				settings = sameSceneSettingsRef;
			}
			else
			{
				settingsRef.TryResolve(out settings);
			}
		}
		if (settings != null)
		{
			settings.BecomeActiveInstance();
		}
		else
		{
			ZoneShaderSettings.ActivateDefaultSettings();
		}
	}
}
