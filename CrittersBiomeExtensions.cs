using System;
using System.Collections.Generic;

public static class CrittersBiomeExtensions
{
	private static List<CrittersBiome> _allScannableBiomes;

	private static Dictionary<CrittersBiome, string> _habitatLookup;

	private static List<CrittersBiome> _habitatBiomes;

	static CrittersBiomeExtensions()
	{
		_habitatLookup = new Dictionary<CrittersBiome, string>();
		_allScannableBiomes = new List<CrittersBiome>();
		foreach (CrittersBiome value in Enum.GetValues(typeof(CrittersBiome)))
		{
			if (value != CrittersBiome.Any && value != CrittersBiome.IntroArea)
			{
				_allScannableBiomes.Add(value);
			}
		}
	}

	public static string GetHabitatDescription(this CrittersBiome biome)
	{
		if (!_habitatLookup.TryGetValue(biome, out var value))
		{
			if (biome == CrittersBiome.Any)
			{
				value = "Any";
			}
			else
			{
				if (_habitatBiomes == null)
				{
					_habitatBiomes = new List<CrittersBiome>();
				}
				_habitatBiomes.Clear();
				for (int i = 0; i < _allScannableBiomes.Count; i++)
				{
					if (biome.HasFlag(_allScannableBiomes[i]))
					{
						_habitatBiomes.Add(_allScannableBiomes[i]);
					}
				}
			}
			value = ((_habitatBiomes.Count > 3) ? "Various" : string.Join(", ", _habitatBiomes));
			_habitatLookup[biome] = value;
		}
		return value;
	}
}
