using System;
using System.Collections.Generic;
using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using Modio.Mods;
using PlayFab;
using UnityEngine;

public class CustomMapsGalleryView : MonoBehaviour
{
	private class RequestSynchronizer
	{
		public int LatestRequest { get; private set; } = -1;

		public void SendRequest(IDictionary<Mod, Action<string>> modsAndCallbacks, Action<PlayFabError> errorCallback = null)
		{
			int latestRequest = LatestRequest + 1;
			LatestRequest = latestRequest;
			new SynchronizedRequest(this, LatestRequest, modsAndCallbacks, errorCallback).Send();
		}
	}

	private class SynchronizedRequest
	{
		private readonly RequestSynchronizer _parent;

		private readonly int _id;

		private readonly IDictionary<Mod, Action<string>> _modsAndCallbacks;

		private readonly Action<PlayFabError> _errorCallback;

		public SynchronizedRequest(RequestSynchronizer parent, int id, IDictionary<Mod, Action<string>> modsAndCallbacks, Action<PlayFabError> errorCallback)
		{
			_parent = parent;
			_id = id;
			_modsAndCallbacks = modsAndCallbacks;
			_errorCallback = errorCallback;
		}

		public void Send()
		{
			Dictionary<Mod, Action<string>> dictionary = new Dictionary<Mod, Action<string>>();
			foreach (Mod key in _modsAndCallbacks.Keys)
			{
				dictionary[key] = WrapCallback(_modsAndCallbacks[key]);
			}
			PlayerCountHelper.GetPlayerCountBatched(dictionary, _errorCallback);
		}

		private Action<string> WrapCallback(Action<string> source)
		{
			return delegate(string result)
			{
				if (_id == _parent.LatestRequest)
				{
					source(result);
				}
			};
		}
	}

	[SerializeField]
	private List<CustomMapsModTile> modTiles = new List<CustomMapsModTile>();

	private readonly RequestSynchronizer _synchronizer = new RequestSynchronizer();

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
		Dictionary<Mod, Action<string>> dictionary = new Dictionary<Mod, Action<string>>();
		for (int i = 0; i < mods.Count; i++)
		{
			modTiles[i].SetMod(mods[i], useMapName);
			int idx = i;
			dictionary[mods[idx]] = delegate(string count)
			{
				modTiles[idx].PlayerCountText = count;
			};
		}
		_synchronizer.SendRequest(dictionary);
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
