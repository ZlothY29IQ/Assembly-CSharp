using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuilderMaterialResourceColors", menuName = "Gorilla Tag/Builder/ResourceColors", order = 0)]
public class BuilderResourceColors : ScriptableObject
{
	public List<BuilderResourceColor> colors;

	public Color GetColorForType(BuilderResourceType type)
	{
		foreach (BuilderResourceColor color in colors)
		{
			if (color.type == type)
			{
				return color.color;
			}
		}
		return Color.black;
	}
}
