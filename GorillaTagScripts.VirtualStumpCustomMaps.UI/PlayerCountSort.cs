using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.Mods;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps.UI;

public static class PlayerCountSort
{
	public class Ranking
	{
		public readonly List<Mod> Mods;

		public readonly IReadOnlyDictionary<string, ulong> PlayerCounts;

		public readonly float Time;

		public bool IsStale => UnityEngine.Time.realtimeSinceStartup - Time >= 60f;

		public Ranking(List<Mod> mods, IReadOnlyDictionary<string, ulong> playerCounts, float time)
		{
			Mods = mods;
			PlayerCounts = playerCounts;
			Time = time;
		}

		public Ranking Copy()
		{
			return new Ranking(new List<Mod>(Mods), PlayerCounts, Time);
		}
	}

	private const int CANDIDATE_POOL_SIZE = 200;

	private const int MODIO_PAGE_SIZE = 100;

	private const int PLAYER_COUNT_BATCH_SIZE = 25;

	private const float RANKING_LIFETIME_SECONDS = 60f;

	private static readonly Ranking EmptyRanking = new Ranking(new List<Mod>(), new Dictionary<string, ulong>(), 0f);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, Ranking> rankingCache = new Dictionary<string, Ranking>();

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, Task<(bool success, Ranking ranking)>> pendingRankings = new Dictionary<string, Task<(bool, Ranking)>>();

	public static async Task<(bool success, Ranking ranking)> GetModsByPlayerCount(string requiredTag = null, bool forceRefresh = false)
	{
		string cacheKey = $"{requiredTag}|{UGCPermissionManager.FeaturedMapsOnly}";
		if (!forceRefresh && rankingCache.TryGetValue(cacheKey, out var value) && !value.IsStale)
		{
			return (success: true, ranking: value.Copy());
		}
		if (!pendingRankings.TryGetValue(cacheKey, out var pending))
		{
			pending = BuildRanking(requiredTag);
			pendingRankings[cacheKey] = pending;
		}
		(bool, Ranking) tuple;
		try
		{
			tuple = await pending;
		}
		catch (Exception ex)
		{
			GTDev.LogError("[PlayerCountSort::GetModsByPlayerCount] Failed to rank maps: " + ex.Message);
			tuple = (false, EmptyRanking);
		}
		finally
		{
			if (pendingRankings.TryGetValue(cacheKey, out Task<(bool, Ranking)> value2) && value2 == pending)
			{
				pendingRankings.Remove(cacheKey);
			}
		}
		if (tuple.Item1)
		{
			rankingCache[cacheKey] = tuple.Item2;
		}
		return (success: tuple.Item1, ranking: tuple.Item2.Copy());
	}

	private static async Task<(bool success, Ranking ranking)> BuildRanking(string requiredTag)
	{
		List<Mod> candidates = await GetCandidates(requiredTag);
		if (candidates == null)
		{
			return (success: false, ranking: EmptyRanking);
		}
		if (candidates.Count == 0)
		{
			return (success: true, ranking: new Ranking(candidates, new Dictionary<string, ulong>(), Time.realtimeSinceStartup));
		}
		List<string> list = candidates.Select((Mod mod) => mod.Id.ToString()).ToList();
		List<Task<(bool, Dictionary<string, ulong>)>> list2 = new List<Task<(bool, Dictionary<string, ulong>)>>();
		for (int num = 0; num < list.Count; num += 25)
		{
			int count = Mathf.Min(25, list.Count - num);
			list2.Add(PlayerCountHelper.GetPlayerCountsAsync(list.GetRange(num, count)));
		}
		(bool, Dictionary<string, ulong>)[] array = await Task.WhenAll(list2);
		if (array.Any(((bool success, Dictionary<string, ulong> counts) batch) => !batch.success))
		{
			GTDev.LogError("[PlayerCountSort::BuildRanking] Failed to retrieve player counts for the candidate maps.");
			return (success: false, ranking: EmptyRanking);
		}
		Dictionary<string, ulong> playerCounts = new Dictionary<string, ulong>();
		(bool, Dictionary<string, ulong>)[] array2 = array;
		for (int num2 = 0; num2 < array2.Length; num2++)
		{
			foreach (KeyValuePair<string, ulong> item in array2[num2].Item2)
			{
				playerCounts[item.Key] = item.Value;
			}
		}
		List<Mod> list3 = candidates.OrderByDescending((Mod mod) => (!playerCounts.TryGetValue(mod.Id.ToString(), out var value)) ? 0 : value).ToList();
		GTDev.Log($"[PlayerCountSort::BuildRanking] Ranked {list3.Count} maps by player count, " + $"{playerCounts.Count((KeyValuePair<string, ulong> kvp) => kvp.Value != 0)} with players.");
		return (success: true, ranking: new Ranking(list3, playerCounts, Time.realtimeSinceStartup));
	}

	private static async Task<List<Mod>> GetCandidates(string requiredTag)
	{
		ModIOManager.TryGetNewMapsModId(out var newMapsModId);
		List<Mod> candidates = new List<Mod>();
		HashSet<ModId> seen = new HashSet<ModId>();
		int pageIndex = 0;
		while (candidates.Count < 200)
		{
			ModSearchFilter modSearchFilter = new ModSearchFilter(pageIndex);
			modSearchFilter.SortBy = SortModsBy.Popular;
			modSearchFilter.IsSortAscending = false;
			if (!string.IsNullOrEmpty(requiredTag))
			{
				modSearchFilter.AddTag(requiredTag);
			}
			if (UGCPermissionManager.FeaturedMapsOnly)
			{
				modSearchFilter.AddTag("Featured");
			}
			var (error, modioPage) = await ModIOManager.GetMods(modSearchFilter.GetModsFilter());
			if ((bool)error || modioPage == null)
			{
				GTDev.LogError("[PlayerCountSort::GetCandidates] Failed to retrieve maps from mod.io. Error: " + error.GetMessage());
				return null;
			}
			if (modioPage.Data == null || modioPage.Data.Length == 0)
			{
				break;
			}
			Mod[] data = modioPage.Data;
			foreach (Mod mod in data)
			{
				if (mod != null && !(mod.Id == newMapsModId) && seen.Add(mod.Id))
				{
					candidates.Add(mod);
					if (candidates.Count >= 200)
					{
						break;
					}
				}
			}
			if ((pageIndex + 1) * 100 >= modioPage.TotalSearchResults)
			{
				break;
			}
			pageIndex++;
		}
		return candidates;
	}
}
