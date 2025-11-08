using System;
using UnityEngine;

namespace GorillaTag.Rendering;

public sealed class ZoneLiquidEffectable : MonoBehaviour
{
	public float radius = 1f;

	[NonSerialized]
	public bool inLiquidVolume;

	[NonSerialized]
	public bool wasInLiquidVolume;

	[NonSerialized]
	public Renderer[] childRenderers;

	private void Awake()
	{
		childRenderers = GetComponentsInChildren<Renderer>(includeInactive: false);
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
	}
}
