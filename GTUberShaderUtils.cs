using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public static class GTUberShaderUtils
{
	private static Shader kUberShader;

	private static readonly ShaderHashId _StencilComparison = "_StencilComparison";

	private static readonly ShaderHashId _StencilPassFront = "_StencilPassFront";

	private static readonly ShaderHashId _StencilReference = "_StencilReference";

	private static readonly ShaderHashId _ColorMask_ = "_ColorMask_";

	private static readonly ShaderHashId _ManualZWrite = "_ManualZWrite";

	private static readonly ShaderHashId _ZWrite = "_ZWrite";

	private static readonly int[] kRenderQueueInts = new int[6] { 1000, 2000, 2450, 2500, 3000, 4000 };

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetStencilComparison(this Material m, GTShaderStencilCompare cmp)
	{
		m.SetFloat(_StencilComparison, (float)cmp);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetStencilPassFrontOp(this Material m, GTShaderStencilOp op)
	{
		m.SetFloat(_StencilPassFront, (float)op);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetStencilReferenceValue(this Material m, int value)
	{
		m.SetFloat(_StencilReference, value);
	}

	public static void SetVisibleToXRay(this Material m, bool visible, bool saveToDisk = false)
	{
		GTShaderStencilCompare cmp = (visible ? GTShaderStencilCompare.Equal : GTShaderStencilCompare.NotEqual);
		GTShaderStencilOp op = (visible ? GTShaderStencilOp.Replace : GTShaderStencilOp.Keep);
		m.SetStencilComparison(cmp);
		m.SetStencilPassFrontOp(op);
		m.SetStencilReferenceValue(7);
	}

	public static void SetRevealsXRay(this Material m, bool reveals, bool changeQueue = true, bool saveToDisk = false)
	{
		m.SetFloat(_ZWrite, (!reveals) ? 1 : 0);
		m.SetFloat(_ColorMask_, (!reveals) ? 14 : 0);
		m.SetStencilComparison(GTShaderStencilCompare.Disabled);
		m.SetStencilPassFrontOp(reveals ? GTShaderStencilOp.Replace : GTShaderStencilOp.Keep);
		m.SetStencilReferenceValue(reveals ? 7 : 0);
		if (changeQueue)
		{
			int renderQueue = m.renderQueue;
			m.renderQueue = renderQueue + ((!reveals) ? 1 : (-1));
		}
	}

	public static int GetNearestRenderQueue(this Material m, out RenderQueue queue)
	{
		int renderQueue = m.renderQueue;
		int num = -1;
		int num2 = int.MaxValue;
		for (int i = 0; i < kRenderQueueInts.Length; i++)
		{
			int num3 = kRenderQueueInts[i];
			int num4 = Math.Abs(num3 - renderQueue);
			if (num2 > num4)
			{
				num = num3;
				num2 = num4;
			}
		}
		queue = (RenderQueue)num;
		return num;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitOnLoad()
	{
		kUberShader = Shader.Find("GorillaTag/UberShader");
	}
}
