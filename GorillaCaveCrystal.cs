using System;
using UnityEngine;

public class GorillaCaveCrystal : Tappable
{
	public bool overrideSoundAndMaterial;

	public CrystalOctave octave;

	public CrystalNote note;

	[SerializeField]
	private MeshRenderer _crystalRenderer;

	public TapInnerGlow tapScript;

	[HideInInspector]
	public GorillaCaveCrystalVisuals visuals;

	[HideInInspector]
	[SerializeField]
	private AnimationCurve _lerpInCurve = AnimationCurve.Constant(0f, 1f, 1f);

	[HideInInspector]
	[SerializeField]
	private AnimationCurve _lerpOutCurve = AnimationCurve.Constant(0f, 1f, 1f);

	[HideInInspector]
	[SerializeField]
	private bool _animating;

	[HideInInspector]
	[SerializeField]
	[Range(0f, 1f)]
	private float _tapStrength = 1f;

	[NonSerialized]
	private TimeSince _timeSinceLastTap;

	private void Awake()
	{
		if (tapScript == null)
		{
			tapScript = GetComponent<TapInnerGlow>();
		}
	}

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped info)
	{
		_tapStrength = tapStrength;
		AnimateCrystal();
	}

	private void AnimateCrystal()
	{
		if ((bool)tapScript)
		{
			tapScript.Tap();
		}
	}
}
