using System.Collections.Generic;
using Modio.Mods;
using UnityEngine;

public class CustomMapsGalleryView : MonoBehaviour
{
	[SerializeField]
	private List<CustomMapsModTile> modTiles = new List<CustomMapsModTile>();

	public void ResetGallery()
	{
		for (int i = 0; i < modTiles.Count; i++)
		{
			modTiles[i].DeactivateTile();
		}
	}

	public bool DisplayGallery(List<Mod> mods, bool useMapName, out string error)
	{
		if (mods.Count > modTiles.Count)
		{
			GTDev.LogError("Displayed Mod list is longer than the number of mod tiles in the gallery");
			error = "Displayed Mod list is longer than the number of mod tiles in the gallery";
			return false;
		}
		for (int i = 0; i < mods.Count; i++)
		{
			modTiles[i].SetMod(mods[i], useMapName);
		}
		error = string.Empty;
		return true;
	}

	public void ShowTileText(bool show, bool useMapName)
	{
		for (int i = 0; i < modTiles.Count; i++)
		{
			modTiles[i].ShowTileText(show, useMapName);
		}
	}

	public void ShowDetailsForEntry(int entryIndex)
	{
		if (modTiles.Count > entryIndex)
		{
			modTiles[entryIndex].ShowDetails();
		}
	}

	public void HighlightTileAtIndex(int tileIndex)
	{
		if (tileIndex <= modTiles.Count)
		{
			modTiles[tileIndex].HighlightTile();
		}
	}
}
