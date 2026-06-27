using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

public class LightArray : MonoBehaviour
{
	[SerializeField]
	private LightArrayPresets presets;

	[SerializeField]
	private GameLight[] lights;

	[SerializeField]
	private LightArray[] subArrays;

	[SerializeField]
	private int cascadeTime;

	[SerializeField]
	private float setLightHue = -1f;

	[NonSerialized]
	private float preLightHue = -1f;

	[SerializeField]
	private float setLightSat = -1f;

	[NonSerialized]
	private float preLightSat = -1f;

	[SerializeField]
	private float setLightVal = -1f;

	[NonSerialized]
	private float preLightVal = -1f;

	[SerializeField]
	private float setLightIntensity = -1f;

	[NonSerialized]
	private float preLightIntensity = -1f;

	private void ToggleDynamicLighting()
	{
		GameLightingManager.instance.ToggleCustomDynamicLightingEnabled();
	}

	public void SetCascadeTime(int ct)
	{
		cascadeTime = ct;
	}

	public void SetSubArraysCascadeTime(int ct)
	{
		for (int i = 0; i < subArrays.Length; i++)
		{
			subArrays[i].cascadeTime = ct;
		}
	}

	public void SetPreset(int i)
	{
		if (!(presets == null))
		{
			LightArrayPresets.LightArrayPreset preset = presets.GetPreset(i);
			if (preset != null)
			{
				SetColorAndIntensity(preset.color, preset.intensity);
			}
		}
	}

	public void SetPreset(string n)
	{
		if (!(presets == null))
		{
			LightArrayPresets.LightArrayPreset preset = presets.GetPreset(n);
			if (preset != null)
			{
				SetColorAndIntensity(preset.color, preset.intensity);
			}
		}
	}

	public void SetColorAndIntensity(string RRGGBBF)
	{
		SetColorAndIntensity(GetColor(RRGGBBF), float.Parse(RRGGBBF.Substring(6).ToString()));
	}

	private async void SetColorAndIntensity(Color c, float intensity)
	{
		if (cascadeTime < 0)
		{
			for (int i = subArrays.Length - 1; i >= 0; i--)
			{
				subArrays[i].SetColorAndIntensity(c, intensity);
				await Task.Delay(Mathf.Abs(cascadeTime));
			}
		}
		else
		{
			for (int i = 0; i < subArrays.Length; i++)
			{
				subArrays[i].SetColorAndIntensity(c, intensity);
				await Task.Delay(Mathf.Abs(cascadeTime));
			}
		}
		if (cascadeTime < 0)
		{
			for (int i = lights.Length - 1; i >= 0; i--)
			{
				await SetLightColorAndIntensity(c, intensity, i);
			}
		}
		else
		{
			for (int i = 0; i < lights.Length; i++)
			{
				await SetLightColorAndIntensity(c, intensity, i);
			}
		}
	}

	public void SetColor(string RRGGBB)
	{
		SetColor(GetColor(RRGGBB));
	}

	private async void SetColor(Color c)
	{
		if (cascadeTime < 0)
		{
			for (int i = subArrays.Length - 1; i >= 0; i--)
			{
				subArrays[i].SetColor(c);
				await Task.Delay(Mathf.Abs(cascadeTime));
			}
		}
		else
		{
			for (int i = 0; i < subArrays.Length; i++)
			{
				subArrays[i].SetColor(c);
				await Task.Delay(Mathf.Abs(cascadeTime));
			}
		}
		if (cascadeTime < 0)
		{
			for (int i = lights.Length - 1; i >= 0; i--)
			{
				await SetLightColor(c, i);
			}
		}
		else
		{
			for (int i = 0; i < lights.Length; i++)
			{
				await SetLightColor(c, i);
			}
		}
	}

	public async void SetIntensity(float intensity)
	{
		if (cascadeTime < 0)
		{
			for (int i = subArrays.Length - 1; i >= 0; i--)
			{
				subArrays[i].SetIntensity(intensity);
				await Task.Delay(Mathf.Abs(cascadeTime));
			}
		}
		else
		{
			for (int i = 0; i < subArrays.Length; i++)
			{
				subArrays[i].SetIntensity(intensity);
				await Task.Delay(Mathf.Abs(cascadeTime));
			}
		}
		if (cascadeTime < 0)
		{
			for (int i = lights.Length - 1; i >= 0; i--)
			{
				await SetLightIntensity(intensity, i);
			}
		}
		else
		{
			for (int i = 0; i < lights.Length; i++)
			{
				await SetLightIntensity(intensity, i);
			}
		}
	}

	private async Task SetLightColorAndIntensity(Color c, float intensity, int i)
	{
		lights[i].light.color = c;
		lights[i].light.intensity = intensity;
		lights[i].UpdateCachedLightColorAndIntensity();
		if (cascadeTime != 0)
		{
			await Task.Delay(Mathf.Abs(cascadeTime));
		}
	}

	private async Task SetLightColor(Color c, int i)
	{
		lights[i].light.color = c;
		lights[i].UpdateCachedLightColorAndIntensity();
		if (cascadeTime != 0)
		{
			await Task.Delay(Mathf.Abs(cascadeTime));
		}
	}

	private async Task SetLightIntensity(float intensity, int i)
	{
		lights[i].light.intensity = intensity;
		lights[i].UpdateCachedLightColorAndIntensity();
		if (cascadeTime != 0)
		{
			await Task.Delay(Mathf.Abs(cascadeTime));
		}
	}

	private Color GetColor(string RRGGBB)
	{
		return new Color((float)int.Parse(RRGGBB.Substring(0, 2), NumberStyles.HexNumber) / 255f, (float)int.Parse(RRGGBB.Substring(2, 2), NumberStyles.HexNumber) / 255f, (float)int.Parse(RRGGBB.Substring(4, 2), NumberStyles.HexNumber) / 255f);
	}

	private void LateUpdate()
	{
		bool flag = false;
		bool flag2 = false;
		if (preLightHue != setLightHue)
		{
			flag = true;
			preLightHue = setLightHue;
		}
		if (preLightSat != setLightSat)
		{
			flag = true;
			preLightSat = setLightSat;
		}
		if (preLightVal != setLightVal)
		{
			flag = true;
			preLightVal = setLightVal;
		}
		if (preLightIntensity != setLightIntensity)
		{
			flag2 = true;
			preLightIntensity = setLightIntensity;
		}
		if (flag && flag2)
		{
			SetColorAndIntensity(Color.HSVToRGB(setLightHue, setLightSat, setLightVal), setLightIntensity);
		}
		else if (flag)
		{
			SetColor(Color.HSVToRGB(setLightHue, setLightSat, setLightVal));
		}
		else if (flag2)
		{
			SetIntensity(setLightIntensity);
		}
	}
}
