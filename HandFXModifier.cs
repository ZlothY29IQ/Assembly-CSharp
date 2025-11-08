using UnityEngine;

public class HandFXModifier : FXModifier
{
	private Vector3 originalScale;

	[SerializeField]
	private float minScale;

	[SerializeField]
	private float maxScale;

	[SerializeField]
	private ParticleSystem dustBurst;

	[SerializeField]
	private ParticleSystem dustLinger;

	private void Awake()
	{
		originalScale = base.transform.localScale;
	}

	private void OnDisable()
	{
		base.transform.localScale = originalScale;
	}

	public override void UpdateScale(float scale, Color color)
	{
		scale = Mathf.Clamp(scale, minScale, maxScale);
		base.transform.localScale = originalScale * scale;
	}
}
