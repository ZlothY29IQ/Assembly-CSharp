using System;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Pool;

public class PlayerColoredCosmetic : MonoBehaviour
{
	[Serializable]
	private struct ColoringRule
	{
		[SerializeField]
		private string shaderColorProperty;

		private int _hashId;

		[SerializeField]
		private Renderer meshRenderer;

		[SerializeField]
		private int materialIndex;

		private Material _instancedMaterial;

		private Material _defaultMaterial;

		private bool _isValid;

		public void Init(bool dontCreateMaterialInstance, Component owner)
		{
			_isValid = false;
			_hashId = Shader.PropertyToID(shaderColorProperty);
			if (meshRenderer == null)
			{
				Debug.LogError("ERROR!!!  ColoringRule.Init: Default meshRenderer cannot be null! Path=" + owner.transform.GetPathQ(), owner);
				return;
			}
			List<Material> value;
			using (CollectionPool<List<Material>, Material>.Get(out value))
			{
				meshRenderer.GetSharedMaterials(value);
				if (materialIndex < 0 || materialIndex >= value.Count)
				{
					Debug.LogError("ERROR!!!  " + $"ColoringRule.Init: Material index {materialIndex} is out of range! Path=" + meshRenderer.transform.GetPathQ(), meshRenderer);
					return;
				}
				_defaultMaterial = value[materialIndex];
				if (_defaultMaterial == null)
				{
					Debug.LogError("ERROR!!!  ColoringRule.Init: Default material cannot be null! Path=" + meshRenderer.transform.GetPathQ(), meshRenderer);
					return;
				}
				if (dontCreateMaterialInstance)
				{
					_instancedMaterial = value[materialIndex];
				}
				else
				{
					_instancedMaterial = new Material(value[materialIndex]);
					value[materialIndex] = _instancedMaterial;
					meshRenderer.SetSharedMaterials(value);
				}
				_isValid = true;
			}
		}

		public void Apply(Color color, bool dontCreateMaterialInstance)
		{
			if (!_isValid)
			{
				return;
			}
			List<Material> value;
			if (dontCreateMaterialInstance)
			{
				using (CollectionPool<List<Material>, Material>.Get(out value))
				{
					meshRenderer.GetSharedMaterials(value);
					if (materialIndex >= 0 && materialIndex < value.Count && value[materialIndex] != null)
					{
						value[materialIndex].SetColor(_hashId, color);
					}
					return;
				}
			}
			_instancedMaterial.SetColor(_hashId, color);
		}
	}

	private const string preErr = "ERROR!!!  ";

	private bool _didInit;

	private VRRig _rig;

	[SerializeField]
	private Color lerpToColor = Color.white;

	[SerializeField]
	[Range(0f, 1f)]
	private float lerpStrength;

	[SerializeField]
	private bool dontCreateMaterialInstance;

	[SerializeField]
	private ColoringRule[] coloringRules;

	[SerializeField]
	private ParticleSystem[] particleSystems;

	private ParticleSystem.MainModule[] _particleMains;

	public void Awake()
	{
		for (int i = 0; i < coloringRules.Length; i++)
		{
			coloringRules[i].Init(dontCreateMaterialInstance, this);
		}
	}

	private void InitIfNeeded()
	{
		if (!_didInit)
		{
			_didInit = true;
			_rig = GetComponentInParent<VRRig>();
			if (_rig == null && GorillaTagger.Instance != null)
			{
				_rig = GorillaTagger.Instance.offlineVRRig;
			}
			_particleMains = new ParticleSystem.MainModule[particleSystems.Length];
			for (int i = 0; i < particleSystems.Length; i++)
			{
				_particleMains[i] = particleSystems[i].main;
			}
		}
	}

	private void OnEnable()
	{
		InitIfNeeded();
		if (_rig != null)
		{
			_rig.OnColorChanged += UpdateColor;
			UpdateColor(_rig.playerColor);
		}
	}

	private void OnDisable()
	{
		if (_rig != null)
		{
			_rig.OnColorChanged -= UpdateColor;
		}
	}

	public void UpdateColor(Color color)
	{
		InitIfNeeded();
		Color color2 = Color.Lerp(color, lerpToColor, lerpStrength);
		for (int i = 0; i < coloringRules.Length; i++)
		{
			coloringRules[i].Apply(color2, dontCreateMaterialInstance);
		}
		for (int j = 0; j < particleSystems.Length; j++)
		{
			_particleMains[j].startColor = color2;
		}
	}
}
