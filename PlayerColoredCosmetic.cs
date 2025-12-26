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

		private int hashId;

		[SerializeField]
		private Renderer meshRenderer;

		[SerializeField]
		private int materialIndex;

		private Material instancedMaterial;

		private Material defaultMaterial;

		public void Init()
		{
			hashId = Shader.PropertyToID(shaderColorProperty);
			if (meshRenderer == null)
			{
				Debug.LogError("ERROR!!!  ColoringRule.Init: Default meshRenderer cannot be null! Path=" + meshRenderer.transform.GetPathQ());
			}
			List<Material> value;
			using (CollectionPool<List<Material>, Material>.Get(out value))
			{
				meshRenderer.GetSharedMaterials(value);
				if (materialIndex < 0 || materialIndex >= value.Count)
				{
					Debug.LogError("ERROR!!!  " + $"ColoringRule.Init: Material index {materialIndex} is out of range! Path=" + meshRenderer.transform.GetPathQ(), meshRenderer);
				}
				defaultMaterial = value[materialIndex];
				if (defaultMaterial == null)
				{
					Debug.LogError("ERROR!!!  ColoringRule.Init: Default material cannot be null! Path=" + meshRenderer.transform.GetPathQ(), meshRenderer);
				}
				instancedMaterial = new Material(value[materialIndex]);
				value[materialIndex] = instancedMaterial;
				meshRenderer.SetSharedMaterials(value);
			}
		}

		public void Apply(Color color)
		{
			instancedMaterial.SetColor(hashId, color);
		}
	}

	private const string preLog = "[GT/PlayerColoredCosmetic]  ";

	private const string preErr = "ERROR!!!  ";

	private bool didInit;

	private VRRig rig;

	[SerializeField]
	private Color lerpToColor = Color.white;

	[SerializeField]
	[Range(0f, 1f)]
	private float lerpStrength;

	[SerializeField]
	private ColoringRule[] coloringRules;

	[SerializeField]
	private ParticleSystem[] particleSystems;

	private ParticleSystem.MainModule[] particleMains;

	public void Awake()
	{
		for (int i = 0; i < coloringRules.Length; i++)
		{
			coloringRules[i].Init();
		}
	}

	private void InitIfNeeded()
	{
		if (!didInit)
		{
			didInit = true;
			rig = GetComponentInParent<VRRig>();
			if (rig == null && GorillaTagger.Instance != null)
			{
				rig = GorillaTagger.Instance.offlineVRRig;
			}
			particleMains = new ParticleSystem.MainModule[particleSystems.Length];
			for (int i = 0; i < particleSystems.Length; i++)
			{
				particleMains[i] = particleSystems[i].main;
			}
		}
	}

	private void OnEnable()
	{
		InitIfNeeded();
		if (rig != null)
		{
			rig.OnColorChanged += UpdateColor;
			UpdateColor(rig.playerColor);
		}
	}

	private void OnDisable()
	{
		if (rig != null)
		{
			rig.OnColorChanged -= UpdateColor;
		}
	}

	public void UpdateColor(Color color)
	{
		InitIfNeeded();
		Color color2 = Color.Lerp(color, lerpToColor, lerpStrength);
		for (int i = 0; i < coloringRules.Length; i++)
		{
			coloringRules[i].Apply(color2);
		}
		for (int j = 0; j < particleSystems.Length; j++)
		{
			particleMains[j].startColor = color2;
		}
	}
}
