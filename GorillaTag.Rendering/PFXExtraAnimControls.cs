using UnityEngine;

namespace GorillaTag.Rendering;

public class PFXExtraAnimControls : MonoBehaviour
{
	public float emitRateMult = 1f;

	public float emitBurstProbabilityMult = 1f;

	[SerializeField]
	private ParticleSystem[] particleSystems;

	private ParticleSystem.EmissionModule[] emissionModules;

	private ParticleSystem.Burst[][] cachedEmitBursts;

	private ParticleSystem.Burst[][] adjustedEmitBursts;

	protected void Awake()
	{
		emissionModules = new ParticleSystem.EmissionModule[particleSystems.Length];
		cachedEmitBursts = new ParticleSystem.Burst[particleSystems.Length][];
		adjustedEmitBursts = new ParticleSystem.Burst[particleSystems.Length][];
		for (int i = 0; i < particleSystems.Length; i++)
		{
			ParticleSystem.EmissionModule emission = particleSystems[i].emission;
			cachedEmitBursts[i] = new ParticleSystem.Burst[emission.burstCount];
			adjustedEmitBursts[i] = new ParticleSystem.Burst[emission.burstCount];
			for (int j = 0; j < emission.burstCount; j++)
			{
				cachedEmitBursts[i][j] = emission.GetBurst(j);
				adjustedEmitBursts[i][j] = emission.GetBurst(j);
			}
			emissionModules[i] = emission;
		}
	}

	protected void LateUpdate()
	{
		for (int i = 0; i < emissionModules.Length; i++)
		{
			emissionModules[i].rateOverTimeMultiplier = emitRateMult;
			Mathf.Min(emissionModules[i].burstCount, cachedEmitBursts[i].Length);
			for (int j = 0; j < cachedEmitBursts[i].Length; j++)
			{
				adjustedEmitBursts[i][j].probability = cachedEmitBursts[i][j].probability * emitBurstProbabilityMult;
			}
			emissionModules[i].SetBursts(adjustedEmitBursts[i]);
		}
	}
}
