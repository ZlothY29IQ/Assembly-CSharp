using System;
using System.Collections.Generic;
using GorillaNetworking;
using Modio;
using Modio.Mods;
using PlayFab;
using UnityEngine.Events;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public static class CuratedDestinationsManager
{
	public enum CuratedDoorway
	{
		Left,
		Right
	}

	private const string CURATED_MAPS_PLAYFAB_KEY = "DestinationsCurated";

	[OnEnterPlay_SetNew]
	public static UnityEvent OnCuratedMapsUpdated = new UnityEvent();

	[OnEnterPlay_Set(false)]
	private static bool loadingCuratedMaps;

	[OnEnterPlay_Set(false)]
	private static bool curatedMapsRetrieved;

	[OnEnterPlay_Clear]
	private static readonly List<long> curatedModIds = new List<long>();

	[OnEnterPlay_Clear]
	private static readonly List<Mod> curatedMods = new List<Mod>();

	public static bool IsLoading => loadingCuratedMaps;

	public static bool HasRetrievedCuratedMaps => curatedMapsRetrieved;

	public static void RetrieveCuratedMaps(bool forceRefresh = false)
	{
		if (loadingCuratedMaps || (curatedMapsRetrieved && !forceRefresh))
		{
			return;
		}
		loadingCuratedMaps = true;
		PlayFabTitleDataCache.RegisterOnLoad(delegate(PlayFabTitleDataCache cache)
		{
			cache.GetTitleData("DestinationsCurated", OnGetCuratedMapsTitleData, delegate(PlayFabError error)
			{
				GTDev.LogError("[CuratedDestinationsManager::RetrieveCuratedMaps] Failed to retrieve curated maps from TitleData: " + error.ErrorMessage);
				FinishRetrieval(succeeded: false);
			});
		});
	}

	private static async void OnGetCuratedMapsTitleData(string data)
	{
		bool succeeded = true;
		try
		{
			curatedModIds.Clear();
			curatedMods.Clear();
			if (data.IsNullOrEmpty())
			{
				return;
			}
			if (data.Length >= 2 && data[0] == '"' && data[data.Length - 1] == '"')
			{
				data = data.Substring(1, data.Length - 2);
			}
			string[] array = data.Split(',');
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (string.IsNullOrWhiteSpace(text) || !long.TryParse(text.Trim(), out var curatedModId))
				{
					GTDev.LogError("[CuratedDestinationsManager::OnGetCuratedMapsTitleData] Failed to parse curated map id as a long: " + text);
					AddEmptySlot();
					continue;
				}
				(Error, Mod) tuple = await ModIOManager.GetMod(new ModId(curatedModId));
				if ((bool)tuple.Item1)
				{
					GTDev.LogError("[CuratedDestinationsManager::OnGetCuratedMapsTitleData] Failed to get mod profile " + $"for curated map {curatedModId}: {tuple.Item1.GetMessage()}");
					AddEmptySlot();
				}
				else if (tuple.Item2.Creator == null)
				{
					AddEmptySlot();
				}
				else if (UGCPermissionManager.FeaturedMapsOnly && !ModIOManager.IsFeaturedMap(tuple.Item2))
				{
					AddEmptySlot();
				}
				else
				{
					curatedModIds.Add(curatedModId);
					curatedMods.Add(tuple.Item2);
				}
			}
		}
		catch (Exception ex)
		{
			GTDev.LogError("[CuratedDestinationsManager::OnGetCuratedMapsTitleData] Failed to resolve curated maps: " + ex.Message);
			succeeded = false;
		}
		finally
		{
			FinishRetrieval(succeeded);
		}
	}

	private static void AddEmptySlot()
	{
		curatedModIds.Add(-1L);
		curatedMods.Add(null);
	}

	private static void FinishRetrieval(bool succeeded)
	{
		curatedMapsRetrieved = succeeded;
		loadingCuratedMaps = false;
		GTDev.Log($"[CuratedDestinationsManager::FinishRetrieval] succeeded {succeeded} curatedSlotCount {curatedModIds.Count}");
		OnCuratedMapsUpdated?.Invoke();
	}

	public static bool TryGetCuratedModId(CuratedDoorway doorway, out ModId modId)
	{
		return TryGetCuratedModId((int)doorway, out modId);
	}

	private static bool TryGetCuratedModId(int doorwayIndex, out ModId modId)
	{
		modId = ModId.Null;
		if (doorwayIndex < 0 || doorwayIndex >= curatedModIds.Count || curatedModIds[doorwayIndex] <= 0)
		{
			return false;
		}
		modId = new ModId(curatedModIds[doorwayIndex]);
		return true;
	}

	public static void TryGetCuratedMod(CuratedDoorway doorway, out Mod mod)
	{
		TryGetCuratedMod((int)doorway, out mod);
	}

	private static bool TryGetCuratedMod(int doorwayIndex, out Mod mod)
	{
		mod = null;
		if (doorwayIndex < 0 || doorwayIndex >= curatedMods.Count || curatedMods[doorwayIndex] == null)
		{
			return false;
		}
		mod = curatedMods[doorwayIndex];
		return true;
	}
}
