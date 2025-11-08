using System.Collections.Generic;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaTag.Rendering;

public class WaterBubbleParticleVolumeCollector : MonoBehaviour
{
	public ParticleSystem[] particleSystems;

	private ParticleSystem.TriggerModule[] particleTriggerModules;

	private ParticleSystem.EmissionModule[] particleEmissionModules;

	private Collider[] bubbleableVolumeColliders;

	private bool emissionEnabled;

	protected void Awake()
	{
		List<WaterVolume> componentsInHierarchy = SceneManager.GetActiveScene().GetComponentsInHierarchy<WaterVolume>();
		List<Collider> list = new List<Collider>(componentsInHierarchy.Count * 4);
		foreach (WaterVolume item in componentsInHierarchy)
		{
			if (item.Parameters != null && !item.Parameters.allowBubblesInVolume)
			{
				continue;
			}
			foreach (Collider volumeCollider in item.volumeColliders)
			{
				if (!(volumeCollider == null))
				{
					list.Add(volumeCollider);
				}
			}
		}
		bubbleableVolumeColliders = list.ToArray();
		particleTriggerModules = new ParticleSystem.TriggerModule[particleSystems.Length];
		particleEmissionModules = new ParticleSystem.EmissionModule[particleSystems.Length];
		for (int i = 0; i < particleSystems.Length; i++)
		{
			particleTriggerModules[i] = particleSystems[i].trigger;
			particleEmissionModules[i] = particleSystems[i].emission;
		}
		for (int j = 0; j < particleSystems.Length; j++)
		{
			ParticleSystem.TriggerModule triggerModule = particleTriggerModules[j];
			for (int k = 0; k < list.Count; k++)
			{
				triggerModule.SetCollider(k, bubbleableVolumeColliders[k]);
			}
		}
		SetEmissionState(setEnabled: false);
	}

	protected void LateUpdate()
	{
		bool headInWater = GTPlayer.Instance.HeadInWater;
		if (headInWater && !emissionEnabled)
		{
			SetEmissionState(setEnabled: true);
		}
		else if (!headInWater && emissionEnabled)
		{
			SetEmissionState(setEnabled: false);
		}
	}

	private void SetEmissionState(bool setEnabled)
	{
		float rateOverTimeMultiplier = (setEnabled ? 1f : 0f);
		for (int i = 0; i < particleEmissionModules.Length; i++)
		{
			particleEmissionModules[i].rateOverTimeMultiplier = rateOverTimeMultiplier;
		}
		emissionEnabled = setEnabled;
	}
}
