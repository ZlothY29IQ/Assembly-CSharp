using System;
using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

public class ScienceExperimentSceneElements : MonoBehaviour
{
	[Serializable]
	public struct DisableByLiquidData
	{
		public Transform target;

		public float heightOffset;
	}

	public List<DisableByLiquidData> disableByLiquidList = new List<DisableByLiquidData>();

	public ParticleSystem sodaFizzParticles;

	public ParticleSystem sodaEruptionParticles;

	private void Awake()
	{
		ScienceExperimentManager.instance.InitElements(this);
	}

	private void OnDestroy()
	{
		ScienceExperimentManager.instance.DeInitElements();
	}
}
