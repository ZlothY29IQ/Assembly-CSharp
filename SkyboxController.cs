using System;
using UnityEngine;
using UnityEngine.Rendering;

public class SkyboxController : MonoBehaviour
{
	public MeshRenderer skyFront;

	public MeshRenderer skyBack;

	public Material[] skyMaterials = new Material[0];

	[Range(0f, 1f)]
	public float lerpValue;

	[NonSerialized]
	private Material _currentSky;

	[NonSerialized]
	private Material _nextSky;

	private TimeSince lastUpdate = TimeSince.Now();

	[Space]
	private BetterDayNightManager _dayNightManager;

	private double _currentSeconds = -1.0;

	private double _totalSecondsInRange = -1.0;

	private float _currentTime = -1f;

	private void Start()
	{
		if (_dayNightManager.AsNull() == null)
		{
			_dayNightManager = BetterDayNightManager.instance;
		}
		if (!(_dayNightManager.AsNull() == null))
		{
			for (int i = 0; i < _dayNightManager.timeOfDayRange.Length; i++)
			{
				_totalSecondsInRange += _dayNightManager.timeOfDayRange[i] * 3600.0;
			}
			_totalSecondsInRange = Math.Floor(_totalSecondsInRange);
		}
	}

	private void Update()
	{
		if (lastUpdate.HasElapsed(1f, resetOnElapsed: true))
		{
			UpdateTime();
			UpdateSky();
		}
	}

	private void OnValidate()
	{
		UpdateSky();
	}

	private void UpdateTime()
	{
		_currentSeconds = ((ITimeOfDaySystem)_dayNightManager).currentTimeInSeconds;
		_currentSeconds = Math.Floor(_currentSeconds);
		_currentTime = (float)(_currentSeconds / _totalSecondsInRange);
	}

	private void UpdateSky()
	{
		if (skyMaterials != null && skyMaterials.Length != 0)
		{
			int num = skyMaterials.Length;
			float num2 = Mathf.Clamp(_currentTime, 0f, 1f);
			float num3 = 1f / (float)num;
			int num4 = (int)(num2 / num3);
			float num5 = (num2 - (float)num4 * num3) / num3;
			_currentSky = skyMaterials[num4];
			_nextSky = skyMaterials[(num4 + 1) % num];
			skyFront.sharedMaterial = _currentSky;
			skyBack.sharedMaterial = _nextSky;
			if (_currentSky.renderQueue != 3000)
			{
				SetFrontToTransparent();
			}
			if (_nextSky.renderQueue == 3000)
			{
				SetBackToOpaque();
			}
			_currentSky.SetFloat(ShaderProps._SkyAlpha, 1f - num5);
		}
	}

	private void SetFrontToTransparent()
	{
		bool flag = false;
		bool flag2 = false;
		string val = "Transparent";
		int renderQueue = 3000;
		BlendMode blendMode = BlendMode.SrcAlpha;
		BlendMode blendMode2 = BlendMode.OneMinusSrcAlpha;
		BlendMode blendMode3 = BlendMode.One;
		BlendMode blendMode4 = BlendMode.OneMinusSrcAlpha;
		Material sharedMaterial = skyFront.sharedMaterial;
		sharedMaterial.SetFloat(ShaderProps._ZWrite, flag ? 1f : 0f);
		sharedMaterial.SetShaderPassEnabled("DepthOnly", flag);
		sharedMaterial.SetFloat(ShaderProps._AlphaToMask, flag2 ? 1f : 0f);
		sharedMaterial.SetOverrideTag("RenderType", val);
		sharedMaterial.renderQueue = renderQueue;
		sharedMaterial.SetFloat(ShaderProps._SrcBlend, (float)blendMode);
		sharedMaterial.SetFloat(ShaderProps._DstBlend, (float)blendMode2);
		sharedMaterial.SetFloat(ShaderProps._SrcBlendAlpha, (float)blendMode3);
		sharedMaterial.SetFloat(ShaderProps._DstBlendAlpha, (float)blendMode4);
	}

	private void SetFrontToOpaque()
	{
		bool flag = false;
		bool flag2 = false;
		flag = true;
		string val = "Opaque";
		int renderQueue = 2000;
		BlendMode blendMode = BlendMode.One;
		BlendMode blendMode2 = BlendMode.Zero;
		BlendMode blendMode3 = BlendMode.One;
		BlendMode blendMode4 = BlendMode.Zero;
		Material sharedMaterial = skyFront.sharedMaterial;
		sharedMaterial.SetFloat(ShaderProps._ZWrite, flag ? 1f : 0f);
		sharedMaterial.SetShaderPassEnabled("DepthOnly", flag);
		sharedMaterial.SetFloat(ShaderProps._AlphaToMask, flag2 ? 1f : 0f);
		sharedMaterial.SetOverrideTag("RenderType", val);
		sharedMaterial.renderQueue = renderQueue;
		sharedMaterial.SetFloat(ShaderProps._SrcBlend, (float)blendMode);
		sharedMaterial.SetFloat(ShaderProps._DstBlend, (float)blendMode2);
		sharedMaterial.SetFloat(ShaderProps._SrcBlendAlpha, (float)blendMode3);
		sharedMaterial.SetFloat(ShaderProps._DstBlendAlpha, (float)blendMode4);
	}

	private void SetBackToOpaque()
	{
		bool flag = false;
		bool flag2 = false;
		flag = true;
		string val = "Opaque";
		int renderQueue = 2000;
		BlendMode blendMode = BlendMode.One;
		BlendMode blendMode2 = BlendMode.Zero;
		BlendMode blendMode3 = BlendMode.One;
		BlendMode blendMode4 = BlendMode.Zero;
		Material sharedMaterial = skyBack.sharedMaterial;
		sharedMaterial.SetFloat(ShaderProps._ZWrite, flag ? 1f : 0f);
		sharedMaterial.SetShaderPassEnabled("DepthOnly", flag);
		sharedMaterial.SetFloat(ShaderProps._AlphaToMask, flag2 ? 1f : 0f);
		sharedMaterial.SetOverrideTag("RenderType", val);
		sharedMaterial.renderQueue = renderQueue;
		sharedMaterial.SetFloat(ShaderProps._SrcBlend, (float)blendMode);
		sharedMaterial.SetFloat(ShaderProps._DstBlend, (float)blendMode2);
		sharedMaterial.SetFloat(ShaderProps._SrcBlendAlpha, (float)blendMode3);
		sharedMaterial.SetFloat(ShaderProps._DstBlendAlpha, (float)blendMode4);
	}
}
