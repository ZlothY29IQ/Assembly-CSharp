using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LightArrayPresets", menuName = "Scriptable Objects/LightArrayPresets")]
public class LightArrayPresets : ScriptableObject
{
	[Serializable]
	public class LightArrayPreset
	{
		public string name = "Color";

		public Color color = Color.white;

		public float intensity = 1f;
	}

	private Dictionary<string, LightArrayPreset> lookup;

	[SerializeField]
	private LightArrayPreset[] presets;

	private void initLookup()
	{
		lookup = new Dictionary<string, LightArrayPreset>();
		for (int i = 0; i < presets.Length; i++)
		{
			lookup.Add(presets[i].name, presets[i]);
		}
	}

	public LightArrayPreset GetPreset(int i)
	{
		return presets[i];
	}

	public LightArrayPreset GetPreset(string n)
	{
		if (lookup == null)
		{
			initLookup();
		}
		return lookup[n];
	}
}
