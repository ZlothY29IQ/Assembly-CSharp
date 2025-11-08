using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag;

[ExecuteAlways]
public class TextureTransitionerManager : MonoBehaviour
{
	public static readonly List<TextureTransitioner> components = new List<TextureTransitioner>(256);

	private MaterialPropertyBlock matPropBlock;

	public static TextureTransitionerManager instance { get; private set; }

	protected void Awake()
	{
		if (instance != null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		instance = this;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(base.gameObject);
		}
		matPropBlock = new MaterialPropertyBlock();
	}

	protected void LateUpdate()
	{
		foreach (TextureTransitioner component in components)
		{
			int num = component.textures.Length;
			float num2 = Mathf.Clamp01(component.remapInfo.Remap(component.iDynamicFloat.floatValue));
			switch (component.directionRetentionMode)
			{
			case TextureTransitioner.DirectionRetentionMode.IncreaseOnly:
				num2 = Mathf.Max(num2, component.normalizedValue);
				break;
			case TextureTransitioner.DirectionRetentionMode.DecreaseOnly:
				num2 = Mathf.Min(num2, component.normalizedValue);
				break;
			}
			float num3 = num2 * (float)(num - 1);
			float num4 = num3 % 1f;
			int num5 = (int)(num4 * 1000f);
			int num6 = (int)num3;
			int num7 = Mathf.Min(num - 1, num6 + 1);
			if (num5 != component.transitionPercent || num6 != component.tex1Index || num7 != component.tex2Index)
			{
				matPropBlock.SetFloat(component.texTransitionShaderParam, num4);
				matPropBlock.SetTexture(component.tex1ShaderParam, component.textures[num6]);
				matPropBlock.SetTexture(component.tex2ShaderParam, component.textures[num7]);
				Renderer[] renderers = component.renderers;
				for (int i = 0; i < renderers.Length; i++)
				{
					renderers[i].SetPropertyBlock(matPropBlock);
				}
				component.normalizedValue = num2;
				component.transitionPercent = num5;
				component.tex1Index = num6;
				component.tex2Index = num7;
			}
		}
	}

	public static void EnsureInstanceIsAvailable()
	{
		if (!(instance != null))
		{
			GameObject obj = new GameObject();
			instance = obj.AddComponent<TextureTransitionerManager>();
			obj.name = "TextureTransitionerManager (Singleton)";
		}
	}

	public static void Register(TextureTransitioner component)
	{
		components.Add(component);
	}

	public static void Unregister(TextureTransitioner component)
	{
		components.Remove(component);
	}
}
