using System;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTag;

[ExecuteAlways]
public class TextureTransitioner : MonoBehaviour, IResettableItem
{
	public enum DirectionRetentionMode
	{
		None,
		IncreaseOnly,
		DecreaseOnly
	}

	public bool editorPreview;

	[Tooltip("The component that will drive the texture transitions.")]
	public MonoBehaviour dynamicFloatComponent;

	[Tooltip("Set these values so that after remap 0 is the first texture in the textures list and 1 is the last.")]
	public GorillaMath.RemapFloatInfo remapInfo;

	public DirectionRetentionMode directionRetentionMode;

	public string texTransitionShaderParamName = "_TexTransition";

	public string tex1ShaderParamName = "_MainTex";

	public string tex2ShaderParamName = "_Tex2";

	public Texture[] textures;

	public Renderer[] renderers;

	[NonSerialized]
	public IDynamicFloat iDynamicFloat;

	[NonSerialized]
	public int texTransitionShaderParam;

	[NonSerialized]
	public int tex1ShaderParam;

	[NonSerialized]
	public int tex2ShaderParam;

	[NonSerialized]
	[DebugReadout]
	public float normalizedValue;

	[NonSerialized]
	[DebugReadout]
	public int transitionPercent;

	[NonSerialized]
	[DebugReadout]
	public int tex1Index;

	[NonSerialized]
	[DebugReadout]
	public int tex2Index;

	protected void Awake()
	{
		if (Application.isPlaying || editorPreview)
		{
			TextureTransitionerManager.EnsureInstanceIsAvailable();
		}
		RefreshShaderParams();
		iDynamicFloat = (IDynamicFloat)dynamicFloatComponent;
		ResetToDefaultState();
	}

	protected void OnEnable()
	{
		TextureTransitionerManager.Register(this);
		if (Application.isPlaying && !remapInfo.IsValid())
		{
			Debug.LogError("Bad min/max values for remapRanges: " + this.GetComponentPath(), this);
			base.enabled = false;
		}
		if (Application.isPlaying && textures.Length == 0)
		{
			Debug.LogError("Textures array is empty: " + this.GetComponentPath(), this);
			base.enabled = false;
		}
		if (Application.isPlaying && iDynamicFloat == null)
		{
			if (dynamicFloatComponent == null)
			{
				Debug.LogError("dynamicFloatComponent cannot be null: " + this.GetComponentPath(), this);
			}
			iDynamicFloat = (IDynamicFloat)dynamicFloatComponent;
			if (iDynamicFloat == null)
			{
				Debug.LogError("Component assigned to dynamicFloatComponent does not implement IDynamicFloat: " + this.GetComponentPath(), this);
				base.enabled = false;
			}
		}
	}

	protected void OnDisable()
	{
		TextureTransitionerManager.Unregister(this);
	}

	private void RefreshShaderParams()
	{
		texTransitionShaderParam = Shader.PropertyToID(texTransitionShaderParamName);
		tex1ShaderParam = Shader.PropertyToID(tex1ShaderParamName);
		tex2ShaderParam = Shader.PropertyToID(tex2ShaderParamName);
	}

	public void ResetToDefaultState()
	{
		normalizedValue = 0f;
		transitionPercent = 0;
		tex1Index = 0;
		tex2Index = 0;
	}
}
