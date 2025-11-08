using System.Collections.Generic;
using UnityEngine;

public class MonkeCandle : RubberDuck
{
	private ParticleSystem.Particle[] fxParticleArray = new ParticleSystem.Particle[20];

	public AudioSource movingFxAudio;

	public AudioSource fxExplodeAudio;

	private List<uint> currentParticles = new List<uint>();

	private Dictionary<uint, Vector3> particleInfoDict = new Dictionary<uint, Vector3>();

	private Vector3 outPosition;

	protected override void Start()
	{
		base.Start();
		if (!IsMyItem())
		{
			movingFxAudio.volume *= 0.5f;
			fxExplodeAudio.volume *= 0.5f;
		}
	}

	public override void TriggeredLateUpdate()
	{
		base.TriggeredLateUpdate();
		if (!particleFX.isPlaying)
		{
			return;
		}
		int particles = particleFX.GetParticles(fxParticleArray);
		if (particles <= 0)
		{
			movingFxAudio.GTStop();
			if (currentParticles.Count == 0)
			{
				return;
			}
		}
		for (int i = 0; i < particles; i++)
		{
			if (currentParticles.Contains(fxParticleArray[i].randomSeed))
			{
				currentParticles.Remove(fxParticleArray[i].randomSeed);
			}
		}
		foreach (uint currentParticle in currentParticles)
		{
			if (particleInfoDict.TryGetValue(currentParticle, out outPosition))
			{
				fxExplodeAudio.transform.position = outPosition;
				fxExplodeAudio.GTPlayOneShot(fxExplodeAudio.clip);
				particleInfoDict.Remove(currentParticle);
			}
		}
		currentParticles.Clear();
		for (int j = 0; j < particles; j++)
		{
			if (j == 0)
			{
				movingFxAudio.transform.position = fxParticleArray[j].position;
			}
			if (particleInfoDict.TryGetValue(fxParticleArray[j].randomSeed, out outPosition))
			{
				particleInfoDict[fxParticleArray[j].randomSeed] = fxParticleArray[j].position;
			}
			else
			{
				particleInfoDict.Add(fxParticleArray[j].randomSeed, fxParticleArray[j].position);
				if (j == 0 && !movingFxAudio.isPlaying)
				{
					movingFxAudio.GTPlay();
				}
			}
			currentParticles.Add(fxParticleArray[j].randomSeed);
		}
	}
}
