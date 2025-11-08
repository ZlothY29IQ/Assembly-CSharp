using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag.Rendering;

public class ZoneLiquidEffectableManager : MonoBehaviour
{
	private readonly List<ZoneLiquidEffectable> zoneLiquidEffectables = new List<ZoneLiquidEffectable>(32);

	public static ZoneLiquidEffectableManager instance { get; private set; }

	public static bool hasInstance { get; private set; }

	protected void Awake()
	{
		if (hasInstance && instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			SetInstance(this);
		}
	}

	protected void OnDestroy()
	{
		if (instance == this)
		{
			hasInstance = false;
			instance = null;
		}
	}

	protected void LateUpdate()
	{
		int layerMask = UnityLayer.Water.ToLayerMask();
		foreach (ZoneLiquidEffectable zoneLiquidEffectable in zoneLiquidEffectables)
		{
			Transform transform = zoneLiquidEffectable.transform;
			zoneLiquidEffectable.inLiquidVolume = Physics.CheckSphere(transform.position, zoneLiquidEffectable.radius * transform.lossyScale.x, layerMask);
			if (zoneLiquidEffectable.inLiquidVolume != zoneLiquidEffectable.wasInLiquidVolume)
			{
				for (int i = 0; i < zoneLiquidEffectable.childRenderers.Length; i++)
				{
					if (zoneLiquidEffectable.inLiquidVolume)
					{
						zoneLiquidEffectable.childRenderers[i].material.EnableKeyword("_WATER_EFFECT");
						zoneLiquidEffectable.childRenderers[i].material.EnableKeyword("_HEIGHT_BASED_WATER_EFFECT");
					}
					else
					{
						zoneLiquidEffectable.childRenderers[i].material.DisableKeyword("_WATER_EFFECT");
						zoneLiquidEffectable.childRenderers[i].material.DisableKeyword("_HEIGHT_BASED_WATER_EFFECT");
					}
				}
			}
			zoneLiquidEffectable.wasInLiquidVolume = zoneLiquidEffectable.inLiquidVolume;
		}
	}

	private static void CreateManager()
	{
		SetInstance(new GameObject("ZoneLiquidEffectableManager").AddComponent<ZoneLiquidEffectableManager>());
	}

	private static void SetInstance(ZoneLiquidEffectableManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void Register(ZoneLiquidEffectable effect)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (effect == null || instance.zoneLiquidEffectables.Contains(effect))
		{
			return;
		}
		instance.zoneLiquidEffectables.Add(effect);
		effect.inLiquidVolume = false;
		for (int i = 0; i < effect.childRenderers.Length; i++)
		{
			if (!(effect.childRenderers[i] == null))
			{
				Material sharedMaterial = effect.childRenderers[i].sharedMaterial;
				if (!(sharedMaterial == null) || sharedMaterial.shader.keywordSpace.FindKeyword("_WATER_EFFECT").isValid)
				{
					effect.inLiquidVolume = sharedMaterial.IsKeywordEnabled("_WATER_EFFECT") && sharedMaterial.IsKeywordEnabled("_HEIGHT_BASED_WATER_EFFECT");
					break;
				}
			}
		}
	}

	public static void Unregister(ZoneLiquidEffectable effect)
	{
		instance.zoneLiquidEffectables.Remove(effect);
	}
}
