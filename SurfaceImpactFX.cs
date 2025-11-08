using UnityEngine;

public class SurfaceImpactFX : MonoBehaviour
{
	public ParticleSystem particleFX;

	public float startingGravityModifier;

	public Vector3 startingScale = Vector3.one;

	private ParticleSystem.MainModule fxMainModule;

	public void Awake()
	{
		if (particleFX == null)
		{
			particleFX = GetComponent<ParticleSystem>();
		}
		if (particleFX == null)
		{
			Debug.LogError("SurfaceImpactFX: No ParticleSystem found! Disabling component.", this);
			base.enabled = false;
		}
		else
		{
			fxMainModule = particleFX.main;
		}
	}

	public void SetScale(float scale)
	{
		fxMainModule.gravityModifierMultiplier = startingGravityModifier * scale;
		base.transform.localScale = startingScale * scale;
	}
}
