using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag.Cosmetics;

[Serializable]
public class ContinuousPropertyArray
{
	private class PropertyComparer : IComparer<ContinuousProperty>
	{
		public int Compare(ContinuousProperty x, ContinuousProperty y)
		{
			if (!x.IsShaderProperty_Cached || !y.IsShaderProperty_Cached)
			{
				return y.IsShaderProperty_Cached.CompareTo(x.IsShaderProperty_Cached);
			}
			int num = x.GetTargetInstanceID() ^ x.IntValue;
			int value = y.GetTargetInstanceID() ^ y.IntValue;
			return num.CompareTo(value);
		}
	}

	[Tooltip("Divides the input value by this number before being fed into the property array. Unless you know what you're doing, you should probably leave this at 1. You can accomplish the same thing by changing the maximum X value for all the curves/gradients, this is just a shorthand.")]
	[SerializeField]
	private float maxExpectedValue = 1f;

	private float inverseMaximum;

	[Tooltip("Determines how quickly the internal value lerps towards the input value. A low number will take a long time to match but will be more resistant to fluctuations, visa versa for a high value. A good starting point is 5 to 10.")]
	[SerializeField]
	private float responsiveness = 5f;

	[Tooltip("If true (default behavior), the input value will be used directly. Disable this if you need better control over how smoothly the properties get applied.")]
	[SerializeField]
	private bool instant = true;

	[SerializeField]
	private ContinuousProperty[] list;

	private List<int> uniqueShaderPropertyIndices;

	private MaterialPropertyBlock mpb;

	private bool initialized;

	private float value;

	private float lastApplyTime;

	[NonSerialized]
	public bool cachedRigIsLocal;

	public int Count => list.Length;

	private void InitIfNeeded()
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		inverseMaximum = 1f / maxExpectedValue;
		value = 0f;
		lastApplyTime = Time.time - Time.deltaTime;
		for (int i = 0; i < list.Length; i++)
		{
			list[i].Init();
		}
		if (Application.isPlaying)
		{
			for (int j = 0; j < list.Length; j++)
			{
				list[j].InitThreshold();
			}
		}
		uniqueShaderPropertyIndices = new List<int>();
		mpb = new MaterialPropertyBlock();
		PropertyComparer propertyComparer = new PropertyComparer();
		Array.Sort(list, propertyComparer);
		if (!list[0].IsShaderProperty_Cached)
		{
			return;
		}
		for (int k = 0; k < list.Length; k++)
		{
			if (list[k].IsShaderProperty_Cached)
			{
				if (k == list.Length - 1 || (k > 0 && propertyComparer.Compare(list[k - 1], list[k]) != 0))
				{
					uniqueShaderPropertyIndices.Add(k);
				}
				continue;
			}
			uniqueShaderPropertyIndices.Add(k);
			break;
		}
	}

	public void ApplyAll(bool leftHand, float f)
	{
		ApplyAll(f);
	}

	public void ApplyAll(float f)
	{
		if (list.Length == 0)
		{
			return;
		}
		InitIfNeeded();
		float num = Time.time - lastApplyTime;
		value = (instant ? (f * inverseMaximum) : Mathf.Lerp(value, f * inverseMaximum, 1f - Mathf.Exp((0f - responsiveness) * num)));
		lastApplyTime = Time.time;
		int num2 = int.MaxValue;
		if (uniqueShaderPropertyIndices.Count > 0)
		{
			num2 = 0;
			((Renderer)list[0].Target).GetPropertyBlock(mpb, list[0].IntValue);
		}
		bool rigIsLocal = cachedRigIsLocal;
		for (int i = 0; i < list.Length; i++)
		{
			list[i].SetRigIsLocal(rigIsLocal);
			list[i].Apply(value, num, mpb);
			if (num2 < uniqueShaderPropertyIndices.Count && i >= uniqueShaderPropertyIndices[num2] - 1)
			{
				((Renderer)list[i].Target).SetPropertyBlock(mpb, list[0].IntValue);
				if (++num2 < uniqueShaderPropertyIndices.Count)
				{
					((Renderer)list[i + 1].Target).GetPropertyBlock(mpb, list[i + 1].IntValue);
				}
			}
		}
	}
}
