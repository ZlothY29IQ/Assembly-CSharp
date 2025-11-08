using System;
using UnityEngine;

public class MagicCauldronLiquid : MonoBehaviour
{
	[Serializable]
	public struct WaveParams
	{
		public float amplitude;

		public float frequency;

		public float scale;

		public float rotation;
	}

	[SerializeField]
	private ApplyMaterialProperty _applyMaterial;

	[SerializeField]
	private Color _colorStart;

	[SerializeField]
	private Color _colorEnd;

	[SerializeField]
	private bool _animating;

	[SerializeField]
	private float _animProgress;

	[SerializeField]
	private AnimationCurve _animationCurve = AnimationCurves.EaseOutCubic;

	[SerializeField]
	private AnimationCurve _waveCurve = AnimationCurves.EaseInElastic;

	public float animLength = 1f;

	public WaveParams waveNormal;

	public WaveParams waveAnimating;

	private void Test()
	{
		_animProgress = 0f;
		_animating = true;
		base.enabled = true;
	}

	public void AnimateColorFromTo(Color a, Color b, float length = 1f)
	{
		_colorStart = a;
		_colorEnd = b;
		_animProgress = 0f;
		_animating = true;
		animLength = length;
		base.enabled = true;
	}

	private void ApplyColor(Color color)
	{
		if ((bool)_applyMaterial)
		{
			_applyMaterial.SetColor(ShaderProps._BaseColor, color);
			_applyMaterial.Apply();
		}
	}

	private void ApplyWaveParams(float amplitude, float frequency, float scale, float rotation)
	{
		if ((bool)_applyMaterial)
		{
			_applyMaterial.SetFloat(ShaderProps._WaveAmplitude, amplitude);
			_applyMaterial.SetFloat(ShaderProps._WaveFrequency, frequency);
			_applyMaterial.SetFloat(ShaderProps._WaveScale, scale);
			_applyMaterial.Apply();
		}
	}

	private void OnEnable()
	{
		if ((bool)_applyMaterial)
		{
			_applyMaterial.mode = ApplyMaterialProperty.ApplyMode.MaterialPropertyBlock;
		}
	}

	private void OnDisable()
	{
		_animating = false;
		_animProgress = 0f;
	}

	private void Update()
	{
		if (_animating)
		{
			float num = _animationCurve.Evaluate(_animProgress / animLength);
			float t = _waveCurve.Evaluate(_animProgress / animLength);
			if (num >= 1f)
			{
				ApplyColor(_colorEnd);
				_animating = false;
				base.enabled = false;
				return;
			}
			Color color = Color.Lerp(_colorStart, _colorEnd, num);
			Mathf.Lerp(waveNormal.frequency, waveAnimating.frequency, t);
			Mathf.Lerp(waveNormal.amplitude, waveAnimating.amplitude, t);
			Mathf.Lerp(waveNormal.scale, waveAnimating.scale, t);
			Mathf.Lerp(waveNormal.rotation, waveAnimating.rotation, t);
			ApplyColor(color);
			_animProgress += Time.deltaTime;
		}
	}
}
