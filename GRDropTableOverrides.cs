using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GhostReactorDropTableOverrides", menuName = "ScriptableObjects/GhostReactorDropTableOverride")]
public class GRDropTableOverrides : ScriptableObject
{
	[Serializable]
	public class DropTableOverride
	{
		public GRBreakableItemSpawnConfig table;

		public GRBreakableItemSpawnConfig overrideTable;
	}

	public List<DropTableOverride> overrides;

	public GRBreakableItemSpawnConfig GetOverride(GRBreakableItemSpawnConfig table)
	{
		for (int i = 0; i < overrides.Count; i++)
		{
			if (overrides[i].table == table)
			{
				return overrides[i].overrideTable;
			}
		}
		return null;
	}
}
