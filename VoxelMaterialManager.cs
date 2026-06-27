using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMaterialManager : MonoBehaviour
{
	[Serializable]
	public struct LightingProfile
	{
		public Color color;

		public Vector3 direction;
	}

	public Material[] voxelMats;

	public List<string> lightmapNames;

	public List<LightingProfile> lightingProfiles;

	[Range(0f, 1f)]
	[SerializeField]
	private float shadowBrightness = 0.3f;

	[Range(0f, 1f)]
	[SerializeField]
	private float backlightBrightness = 0.2f;

	[SerializeField]
	private int startingIndex = 2;

	private int _timeOfDayIndex = -1;

	private void OnEnable()
	{
		SetLightingProfile(startingIndex);
	}

	private void Update()
	{
		if (_timeOfDayIndex != BetterDayNightManager.instance.currentTimeIndex)
		{
			UpdateMaterial();
		}
	}

	private void UpdateMaterial()
	{
		string currentTimeOfDay = BetterDayNightManager.instance.currentTimeOfDay;
		if (!string.IsNullOrEmpty(currentTimeOfDay))
		{
			int num = lightmapNames.IndexOf(currentTimeOfDay);
			if (num >= 0 && num < lightingProfiles.Count)
			{
				SetLightingProfile(num);
				_timeOfDayIndex = BetterDayNightManager.instance.currentTimeIndex;
			}
		}
	}

	private void SetLightingProfile(int index)
	{
		index = Mathf.Clamp(index, 0, lightingProfiles.Count - 1);
		LightingProfile lightingProfile = lightingProfiles[index];
		Shader.SetGlobalVector("_Light_Direction", lightingProfile.direction);
		Shader.SetGlobalColor("_Light_Color", lightingProfile.color);
		Shader.SetGlobalColor("_Shadow_Color", lightingProfile.color * shadowBrightness);
		Shader.SetGlobalColor("_Backlight_Color", lightingProfile.color * backlightBrightness);
		_timeOfDayIndex = index;
	}
}
