using System;
using System.Collections.Generic;
using UnityEngine;

namespace Critters.Scripts;

public class CrittersSpawningData : MonoBehaviour
{
	[Serializable]
	public class CreatureSpawnParameters
	{
		public CritterTemplate Template;

		public int ChancesToSpawn;

		[NonSerialized]
		[HideInInspector]
		public int StartingIndex;
	}

	public List<CreatureSpawnParameters> SpawnParametersList;

	private List<int> templateCollection = new List<int>();

	public void InitializeSpawnCollection()
	{
		for (int i = 0; i < SpawnParametersList.Count; i++)
		{
			for (int j = 0; j < SpawnParametersList[i].ChancesToSpawn; j++)
			{
				templateCollection.Add(i);
			}
		}
	}

	public int GetRandomTemplate()
	{
		int index = UnityEngine.Random.Range(0, templateCollection.Count - 1);
		return templateCollection[index];
	}
}
