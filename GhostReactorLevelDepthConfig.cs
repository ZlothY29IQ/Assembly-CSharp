using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GhostReactorLevelDepthConfig", menuName = "ScriptableObjects/GhostReactorLevelDepthConfig")]
public class GhostReactorLevelDepthConfig : ScriptableObject
{
	[Serializable]
	public class LevelOption
	{
		public int weight = 100;

		public GhostReactorLevelGenConfig levelConfig;
	}

	public string displayName;

	public List<GhostReactorLevelGenConfig> configGenOptions = new List<GhostReactorLevelGenConfig>();

	public List<LevelOption> options = new List<LevelOption>();
}
