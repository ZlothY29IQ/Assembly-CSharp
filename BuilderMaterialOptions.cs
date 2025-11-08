using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuilderMaterialOptions01a", menuName = "Gorilla Tag/Builder/Options", order = 0)]
public class BuilderMaterialOptions : ScriptableObject
{
	[Serializable]
	public class Options
	{
		public string materialId;

		public Material material;

		[GorillaSoundLookup]
		public int soundIndex;

		[NonSerialized]
		public int materialType;
	}

	public List<Options> options;

	public void GetMaterialFromType(int materialType, out Material material, out int soundIndex)
	{
		if (options == null)
		{
			material = null;
			soundIndex = -1;
			return;
		}
		foreach (Options option in options)
		{
			if (option.materialId.GetHashCode() == materialType)
			{
				material = option.material;
				soundIndex = option.soundIndex;
				return;
			}
		}
		material = null;
		soundIndex = -1;
	}

	public void GetDefaultMaterial(out int materialType, out Material material, out int soundIndex)
	{
		if (options.Count > 0)
		{
			materialType = options[0].materialId.GetHashCode();
			material = options[0].material;
			soundIndex = options[0].soundIndex;
		}
		else
		{
			materialType = -1;
			material = null;
			soundIndex = -1;
		}
	}
}
