using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GorillaGameModes;
using GorillaNetworking;
using JetBrains.Annotations;
using KID.Model;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.EventsModels;
using UnityEngine;

public static class GorillaTelemetry
{
	public static class k
	{
		public const string User = "User";

		public const string ZoneId = "ZoneId";

		public const string SubZoneId = "SubZoneId";

		public const string EventType = "EventType";

		public const string IsPrivateRoom = "IsPrivateRoom";

		public const string Items = "Items";

		public const string VoiceChatEnabled = "VoiceChatEnabled";

		public const string JoinGroups = "JoinGroups";

		public const string CustomUsernameEnabled = "CustomUsernameEnabled";

		public const string AgeCategory = "AgeCategory";

		public const string telemetry_zone_event = "telemetry_zone_event";

		public const string telemetry_shop_event = "telemetry_shop_event";

		public const string telemetry_kid_event = "telemetry_kid_event";

		public const string telemetry_ggwp_event = "telemetry_ggwp_event";

		public const string NOTHING = "NOTHING";

		public const string telemetry_wam_gameStartEvent = "telemetry_wam_gameStartEvent";

		public const string telemetry_wam_levelEndEvent = "telemetry_wam_levelEndEvent";

		public const string WamMachineId = "WamMachineId";

		public const string WamGameId = "WamGameId";

		public const string WamMLevelNumber = "WamMLevelNumber";

		public const string WamGoodMolesShown = "WamGoodMolesShown";

		public const string WamHazardMolesShown = "WamHazardMolesShown";

		public const string WamLevelMinScore = "WamLevelMinScore";

		public const string WamLevelScore = "WamLevelScore";

		public const string WamHazardMolesHit = "WamHazardMolesHit";

		public const string WamGameState = "WamGameState";

		public const string CustomMapName = "CustomMapName";

		public const string LowestFPS = "LowestFPS";

		public const string LowestFPSDrawCalls = "LowestFPSDrawCalls";

		public const string LowestFPSPlayerCount = "LowestFPSPlayerCount";

		public const string AverageFPS = "AverageFPS";

		public const string AverageDrawCalls = "AverageDrawCalls";

		public const string AveragePlayerCount = "AveragePlayerCount";

		public const string HighestFPS = "HighestFPS";

		public const string HighestFPSDrawCalls = "HighestFPSDrawCalls";

		public const string HighestFPSPlayerCount = "HighestFPSPlayerCount";

		public const string CustomMapCreator = "CustomMapCreator";

		public const string CustomMapModId = "CustomMapModId";

		public const string MinPlayerCount = "MinPlayerCount";

		public const string MaxPlayerCount = "MaxPlayerCount";

		public const string PlaytimeOnMap = "PlaytimeOnMap";

		public const string PlaytimeInSeconds = "PlaytimeInSeconds";

		public const string PrivateRoom = "PrivateRoom";

		public const string ghost_game_start = "ghost_game_start";

		public const string ghost_game_end = "ghost_game_end";

		public const string ghost_floor_start = "ghost_floor_start";

		public const string ghost_floor_end = "ghost_floor_end";

		public const string ghost_tool_purchased = "ghost_tool_purchased";

		public const string ghost_game_rank_up = "ghost_game_rank_up";

		public const string ghost_game_tool_unlock = "ghost_game_tool_unlock";

		public const string ghost_pod_upgrade_purchased = "ghost_pod_upgrade_purchased";

		public const string ghost_game_tool_upgrade = "ghost_game_tool_upgrade";

		public const string ghost_chaos_seed_start = "ghost_chaos_seed_start";

		public const string ghost_chaos_juice_collected = "ghost_chaos_juice_collected";

		public const string ghost_overdrive_purchased = "ghost_overdrive_purchased";

		public const string ghost_credits_refill_purchased = "ghost_credits_refill_purchased";

		public const string ghost_game_id = "ghost_game_id";

		public const string event_timestamp = "event_timestamp";

		public const string initial_cores_balance = "initial_cores_balance";

		public const string final_cores_balance = "final_cores_balance";

		public const string cores_spent_waiting_in_breakroom = "cores_spent_waiting_in_breakroom";

		public const string cores_collected = "cores_collected";

		public const string cores_collected_from_ghosts = "cores_collected_from_ghosts";

		public const string cores_collected_from_gathering = "cores_collected_from_gathering";

		public const string cores_spent_on_items = "cores_spent_on_items";

		public const string cores_spent_on_gates = "cores_spent_on_gates";

		public const string cores_spent_on_levels = "cores_spent_on_levels";

		public const string cores_given_to_others = "cores_given_to_others";

		public const string cores_received_from_others = "cores_received_from_others";

		public const string gates_unlocked = "gates_unlocked";

		public const string died = "died";

		public const string caught_in_anamole = "caught_in_anamole";

		public const string items_purchased = "items_purchased";

		public const string levels_unlocked = "levels_unlocked";

		public const string shift_cut = "shift_cut";

		public const string number_of_players = "number_of_players";

		public const string start_at_beginning = "start_at_beginning";

		public const string seconds_into_shift_at_join = "seconds_into_shift_at_join";

		public const string reason = "reason";

		public const string play_duration = "play_duration";

		public const string started_late = "started_late";

		public const string time_started = "time_started";

		public const string max_number_in_game = "max_number_in_game";

		public const string end_number_in_game = "end_number_in_game";

		public const string items_picked_up = "items_picked_up";

		public const string super_infection_room_disconnect = "super_infection_room_disconnect";

		public const string super_infection_interval = "super_infection_interval";

		public const string super_infection_purchase = "super_infection_purchase";

		public const string si_purchase_type = "si_purchase_type";

		public const string si_shiny_rock_cost = "si_shiny_rock_cost";

		public const string si_tech_points_purchased = "si_tech_points_purchased";

		public const string total_play_time = "total_play_time";

		public const string room_play_time = "room_play_time";

		public const string session_play_time = "session_play_time";

		public const string interval_play_time = "interval_play_time";

		public const string terminal_total_time = "terminal_total_time";

		public const string terminal_interval_time = "terminal_interval_time";

		public const string time_holding_gadget_type_total = "time_holding_gadget_type_total";

		public const string time_holding_gadget_type_interval = "time_holding_gadget_type_interval";

		public const string time_holding_own_gadgets_total = "time_holding_own_gadgets_total";

		public const string time_holding_own_gadgets_interval = "time_holding_own_gadgets_interval";

		public const string time_holding_others_gadgets_total = "time_holding_others_gadgets_total";

		public const string time_holding_others_gadgets_interval = "time_holding_others_gadgets_interval";

		public const string tags_holding_gadget_type_total = "tags_holding_gadget_type_total";

		public const string tags_holding_gadget_type_interval = "tags_holding_gadget_type_interval";

		public const string tags_holding_own_gadgets_total = "tags_holding_own_gadgets_total";

		public const string tags_holding_own_gadgets_interval = "tags_holding_own_gadgets_interval";

		public const string tags_holding_others_gadgets_total = "tags_holding_others_gadgets_total";

		public const string tags_holding_others_gadgets_interval = "tags_holding_others_gadgets_interval";

		public const string resource_collected_total = "resource_collected_total";

		public const string resource_collected_interval = "resource_collected_interval";

		public const string rounds_played_total = "rounds_played_total";

		public const string rounds_played_interval = "rounds_played_interval";

		public const string unlocked_nodes = "unlocked_nodes";

		public const string floor_joined = "floor_joined";

		public const string player_rank = "player_rank";

		public const string total_cores_collected_by_player = "total_cores_collected_by_player";

		public const string total_cores_collected_by_group = "total_cores_collected_by_group";

		public const string total_cores_spent_by_player = "total_cores_spent_by_player";

		public const string total_cores_spent_by_group = "total_cores_spent_by_group";

		public const string floor = "floor";

		public const string preset = "preset";

		public const string modifier = "modifier";

		public const string section = "section";

		public const string xp_gained = "xp_gained";

		public const string chaos_seeds_collected = "chaos_seeds_collected";

		public const string objectives_completed = "objectives_completed";

		public const string revives = "revives";

		public const string tool = "tool";

		public const string tool_level = "tool_level";

		public const string cores_spent = "cores_spent";

		public const string shiny_rocks_spent = "shiny_rocks_spent";

		public const string new_rank = "new_rank";

		public const string upgrade = "upgrade";

		public const string grift_price = "grift_price";

		public const string type = "type";

		public const string new_level = "new_level";

		public const string juice_spent = "juice_spent";

		public const string grift_spent = "grift_spent";

		public const string chaos_seeds_in_queue = "chaos_seeds_in_queue";

		public const string unlock_time = "unlock_time";

		public const string shiny_rocks_used = "shiny_rocks_used";

		public const string juice_collected = "juice_collected";

		public const string cores_processed_by_overdrive = "cores_processed_by_overdrive";

		public const string final_credits = "final_credits";

		public const string is_private_room = "is_private_room";

		public const string num_shifts_played = "num_shifts_played";

		public const string game_mode_played_event = "game_mode_played_event";

		public const string game_mode = "game_mode";
	}

	private class BatchRunner : MonoBehaviour
	{
		private IEnumerator Start()
		{
			while (true)
			{
				float start = Time.realtimeSinceStartup;
				while (Time.realtimeSinceStartup < start + TELEMETRY_FLUSH_SEC)
				{
					yield return null;
				}
				FlushPlayFabTelemetry();
				FlushMothershipTelemetry();
			}
		}
	}

	private static readonly float TELEMETRY_FLUSH_SEC;

	private static readonly ConcurrentQueue<EventContents> telemetryEventsQueuePlayFab;

	private static readonly ConcurrentQueue<MothershipAnalyticsEvent> telemetryEventsQueueMothership;

	private static readonly Dictionary<int, List<EventContents>> gListPoolPlayFab;

	private static readonly Dictionary<int, List<MothershipAnalyticsEvent>> gListPoolMothership;

	private static readonly string namespacePrefix;

	private static readonly string EVENT_NAMESPACE;

	private static PlayFabAuthenticator gPlayFabAuth;

	private static readonly Dictionary<string, object> gZoneEventArgs;

	private static readonly Dictionary<string, object> gNotifEventArgs;

	public static float nextStayTimestamp;

	private static readonly Dictionary<string, object> gGameModeStartEventArgs;

	private static readonly Dictionary<string, object> gShopEventArgs;

	private static CosmeticsController.CosmeticItem[] gSingleItemParam;

	private static BuilderSetManager.BuilderSetStoreItem[] gSingleItemBuilderParam;

	private static Dictionary<string, object> gKidEventArgs;

	private static readonly Dictionary<string, object> gWamGameStartArgs;

	private static readonly Dictionary<string, object> gWamLevelEndArgs;

	private static Dictionary<string, object> gCustomMapPerfArgs;

	private static Dictionary<string, object> gCustomMapTrackingMetrics;

	private static Dictionary<string, object> gCustomMapDownloadMetrics;

	private static readonly Dictionary<string, object> gGhostReactorShiftStartArgs;

	private static readonly Dictionary<string, object> gGhostReactorShiftEndArgs;

	private static readonly Dictionary<string, object> gGhostReactorFloorStartArgs;

	private static readonly Dictionary<string, object> gGhostReactorFloorEndArgs;

	private static readonly Dictionary<string, object> gGhostReactorToolPurchasedArgs;

	private static readonly Dictionary<string, object> gGhostReactorRankUpArgs;

	private static readonly Dictionary<string, object> gGhostReactorToolUnlockArgs;

	private static readonly Dictionary<string, object> gGhostReactorPodUpgradePurchasedArgs;

	private static readonly Dictionary<string, object> gGhostReactorToolUpgradeArgs;

	private static readonly Dictionary<string, object> gGhostReactorChaosSeedStartArgs;

	private static readonly Dictionary<string, object> gGhostReactorChaosJuiceCollectedArgs;

	private static readonly Dictionary<string, object> gGhostReactorOverdrivePurchasedArgs;

	private static readonly Dictionary<string, object> gGhostReactorCreditsRefillPurchasedArgs;

	private static readonly Dictionary<string, object> gSuperInfectionArgs;

	static GorillaTelemetry()
	{
		TELEMETRY_FLUSH_SEC = 10f;
		telemetryEventsQueuePlayFab = new ConcurrentQueue<EventContents>();
		telemetryEventsQueueMothership = new ConcurrentQueue<MothershipAnalyticsEvent>();
		gListPoolPlayFab = new Dictionary<int, List<EventContents>>();
		gListPoolMothership = new Dictionary<int, List<MothershipAnalyticsEvent>>();
		namespacePrefix = "custom";
		EVENT_NAMESPACE = namespacePrefix + "." + PlayFabAuthenticatorSettings.TitleId;
		gZoneEventArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["EventType"] = null,
			["ZoneId"] = null,
			["SubZoneId"] = null
		};
		gNotifEventArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["EventType"] = null
		};
		nextStayTimestamp = 0f;
		gGameModeStartEventArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["EventType"] = null,
			["game_mode"] = null
		};
		gShopEventArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["EventType"] = null,
			["Items"] = null
		};
		gSingleItemParam = new CosmeticsController.CosmeticItem[1];
		gSingleItemBuilderParam = new BuilderSetManager.BuilderSetStoreItem[1];
		gKidEventArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["EventType"] = null,
			["AgeCategory"] = null,
			["VoiceChatEnabled"] = null,
			["CustomUsernameEnabled"] = null,
			["JoinGroups"] = null
		};
		gWamGameStartArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["WamGameId"] = null,
			["WamMachineId"] = null
		};
		gWamLevelEndArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["WamGameId"] = null,
			["WamMachineId"] = null,
			["WamMLevelNumber"] = null,
			["WamGoodMolesShown"] = null,
			["WamHazardMolesShown"] = null,
			["WamLevelMinScore"] = null,
			["WamLevelScore"] = null,
			["WamHazardMolesHit"] = null,
			["WamGameState"] = null
		};
		gCustomMapPerfArgs = new Dictionary<string, object>
		{
			["CustomMapName"] = null,
			["CustomMapModId"] = null,
			["LowestFPS"] = null,
			["LowestFPSDrawCalls"] = null,
			["LowestFPSPlayerCount"] = null,
			["AverageFPS"] = null,
			["AverageDrawCalls"] = null,
			["AveragePlayerCount"] = null,
			["HighestFPS"] = null,
			["HighestFPSDrawCalls"] = null,
			["HighestFPSPlayerCount"] = null,
			["PlaytimeInSeconds"] = null
		};
		gCustomMapTrackingMetrics = new Dictionary<string, object>
		{
			["User"] = null,
			["CustomMapName"] = null,
			["CustomMapModId"] = null,
			["CustomMapCreator"] = null,
			["MinPlayerCount"] = null,
			["MaxPlayerCount"] = null,
			["PlaytimeOnMap"] = null,
			["PrivateRoom"] = null
		};
		gCustomMapDownloadMetrics = new Dictionary<string, object>
		{
			["User"] = null,
			["CustomMapName"] = null,
			["CustomMapModId"] = null,
			["CustomMapCreator"] = null
		};
		gGhostReactorShiftStartArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["initial_cores_balance"] = null,
			["number_of_players"] = null,
			["start_at_beginning"] = null,
			["seconds_into_shift_at_join"] = null,
			["floor_joined"] = null,
			["player_rank"] = null,
			["is_private_room"] = null
		};
		gGhostReactorShiftEndArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["final_cores_balance"] = null,
			["total_cores_collected_by_player"] = null,
			["total_cores_collected_by_group"] = null,
			["total_cores_spent_by_player"] = null,
			["total_cores_spent_by_group"] = null,
			["gates_unlocked"] = null,
			["died"] = null,
			["items_purchased"] = null,
			["shift_cut"] = null,
			["play_duration"] = null,
			["started_late"] = null,
			["time_started"] = null,
			["reason"] = null,
			["max_number_in_game"] = null,
			["end_number_in_game"] = null,
			["items_picked_up"] = null,
			["revives"] = null,
			["num_shifts_played"] = null
		};
		gGhostReactorFloorStartArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["initial_cores_balance"] = null,
			["number_of_players"] = null,
			["start_at_beginning"] = null,
			["seconds_into_shift_at_join"] = null,
			["player_rank"] = null,
			["floor"] = null,
			["preset"] = null,
			["modifier"] = null,
			["is_private_room"] = null
		};
		gGhostReactorFloorEndArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["final_cores_balance"] = null,
			["total_cores_collected_by_player"] = null,
			["total_cores_collected_by_group"] = null,
			["total_cores_spent_by_player"] = null,
			["total_cores_spent_by_group"] = null,
			["gates_unlocked"] = null,
			["died"] = null,
			["items_purchased"] = null,
			["shift_cut"] = null,
			["play_duration"] = null,
			["started_late"] = null,
			["time_started"] = null,
			["max_number_in_game"] = null,
			["end_number_in_game"] = null,
			["items_picked_up"] = null,
			["revives"] = null,
			["floor"] = null,
			["preset"] = null,
			["modifier"] = null,
			["chaos_seeds_collected"] = null,
			["objectives_completed"] = null,
			["section"] = null,
			["xp_gained"] = null
		};
		gGhostReactorToolPurchasedArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["tool"] = null,
			["tool_level"] = null,
			["cores_spent"] = null,
			["shiny_rocks_spent"] = null,
			["floor"] = null,
			["preset"] = null
		};
		gGhostReactorRankUpArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["new_rank"] = null,
			["floor"] = null,
			["preset"] = null
		};
		gGhostReactorToolUnlockArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["tool"] = null
		};
		gGhostReactorPodUpgradePurchasedArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["tool"] = null,
			["new_level"] = null,
			["shiny_rocks_spent"] = null,
			["juice_spent"] = null
		};
		gGhostReactorToolUpgradeArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["type"] = null,
			["tool"] = null,
			["new_level"] = null,
			["juice_spent"] = null,
			["grift_spent"] = null,
			["cores_spent"] = null,
			["floor"] = null,
			["preset"] = null
		};
		gGhostReactorChaosSeedStartArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["unlock_time"] = null,
			["chaos_seeds_in_queue"] = null,
			["floor"] = null,
			["preset"] = null
		};
		gGhostReactorChaosJuiceCollectedArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["juice_collected"] = null,
			["cores_processed_by_overdrive"] = null
		};
		gGhostReactorOverdrivePurchasedArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["shiny_rocks_used"] = null,
			["chaos_seeds_in_queue"] = null,
			["floor"] = null,
			["preset"] = null
		};
		gGhostReactorCreditsRefillPurchasedArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["ghost_game_id"] = null,
			["event_timestamp"] = null,
			["shiny_rocks_spent"] = null,
			["final_credits"] = null,
			["floor"] = null,
			["preset"] = null
		};
		gSuperInfectionArgs = new Dictionary<string, object>
		{
			["User"] = null,
			["total_play_time"] = null,
			["room_play_time"] = null,
			["session_play_time"] = null,
			["interval_play_time"] = null,
			["terminal_total_time"] = null,
			["terminal_interval_time"] = null,
			["time_holding_gadget_type_total"] = null,
			["time_holding_gadget_type_interval"] = null,
			["tags_holding_gadget_type_total"] = null,
			["tags_holding_gadget_type_interval"] = null,
			["tags_holding_own_gadgets_total"] = null,
			["tags_holding_own_gadgets_interval"] = null,
			["tags_holding_others_gadgets_total"] = null,
			["tags_holding_others_gadgets_interval"] = null,
			["resource_collected_total"] = null,
			["resource_collected_interval"] = null,
			["rounds_played_total"] = null,
			["rounds_played_interval"] = null,
			["unlocked_nodes"] = null,
			["number_of_players"] = null,
			["si_purchase_type"] = null,
			["si_shiny_rock_cost"] = null,
			["si_tech_points_purchased"] = null
		};
		GameObject gameObject = new GameObject("GorillaTelemetryBatcher");
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		gameObject.AddComponent<BatchRunner>();
	}

	public static void EnqueueTelemetryEvent(string eventName, object content, [CanBeNull] string[] customTags = null)
	{
		if (content != null && !string.IsNullOrWhiteSpace(eventName) && GorillaServer.Instance.CheckIsMothershipTelemetryEnabled())
		{
			if (telemetryEventsQueueMothership.Count > 100)
			{
				Debug.LogError("[Telemetry] Too many telemetry events!  Not enqueueing " + eventName + ": " + content.ToJson());
				return;
			}
			telemetryEventsQueueMothership.Enqueue(new MothershipAnalyticsEvent
			{
				event_name = eventName,
				event_timestamp = DateTime.UtcNow.ToString("O"),
				body = JsonConvert.SerializeObject(content),
				custom_tags = ((customTags != null && customTags.Length != 0) ? SerializeCustomTags(customTags) : string.Empty)
			});
		}
	}

	[Obsolete("EnqueueTelemetryEventPlayFab is deprecated. Use EnqueueTelemetryEvent instead.")]
	private static void EnqueueTelemetryEventPlayFab(EventContents eventContent)
	{
		if (GorillaServer.Instance.CheckIsPlayFabTelemetryEnabled())
		{
			telemetryEventsQueuePlayFab.Enqueue(eventContent);
		}
	}

	private static void FlushPlayFabTelemetry()
	{
		int count = telemetryEventsQueuePlayFab.Count;
		if (count == 0)
		{
			return;
		}
		EventContents[] array = ArrayPool<EventContents>.Shared.Rent(count);
		try
		{
			int i;
			for (i = 0; i < count; i++)
			{
				array[i] = (telemetryEventsQueuePlayFab.TryDequeue(out var result) ? result : null);
			}
			if (i == 0)
			{
				ArrayPool<EventContents>.Shared.Return(array);
				return;
			}
			PlayFabEventsAPI.WriteTelemetryEvents(new WriteEventsRequest
			{
				Events = GetEventListForArrayPlayFab(array, i)
			}, delegate
			{
			}, delegate
			{
			});
		}
		finally
		{
			ArrayPool<EventContents>.Shared.Return(array);
		}
	}

	private static void FlushMothershipTelemetry()
	{
		int count = telemetryEventsQueueMothership.Count;
		if (count == 0)
		{
			return;
		}
		MothershipAnalyticsEvent[] array = ArrayPool<MothershipAnalyticsEvent>.Shared.Rent(count);
		try
		{
			int i;
			for (i = 0; i < count; i++)
			{
				array[i] = (telemetryEventsQueueMothership.TryDequeue(out var result) ? result : null);
			}
			if (i == 0)
			{
				ArrayPool<MothershipAnalyticsEvent>.Shared.Return(array);
				return;
			}
			MothershipWriteEventsRequest req = new MothershipWriteEventsRequest
			{
				title_id = MothershipClientApiUnity.TitleId,
				deployment_id = MothershipClientApiUnity.DeploymentId,
				env_id = MothershipClientApiUnity.EnvironmentId,
				events = new AnalyticsRequestVector(GetEventListForArrayMothership(array, i))
			};
			MothershipClientApiUnity.WriteEvents(MothershipClientContext.MothershipId, req, delegate
			{
			}, delegate
			{
			});
		}
		finally
		{
			ArrayPool<MothershipAnalyticsEvent>.Shared.Return(array);
		}
	}

	private static List<EventContents> GetEventListForArrayPlayFab(EventContents[] array, int count)
	{
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			if (array[i] != null)
			{
				num++;
			}
		}
		if (!gListPoolPlayFab.TryGetValue(num, out var value))
		{
			value = new List<EventContents>(num);
			gListPoolPlayFab.TryAdd(num, value);
		}
		else
		{
			value.Clear();
		}
		for (int j = 0; j < count; j++)
		{
			if (array[j] != null)
			{
				value.Add(array[j]);
			}
		}
		return value;
	}

	private static List<MothershipAnalyticsEvent> GetEventListForArrayMothership(MothershipAnalyticsEvent[] array, int count)
	{
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			if (array[i] != null)
			{
				num++;
			}
		}
		if (!gListPoolMothership.TryGetValue(num, out var value))
		{
			value = new List<MothershipAnalyticsEvent>(num);
			gListPoolMothership.TryAdd(num, value);
		}
		else
		{
			value.Clear();
		}
		_ = LocalisationManager.CurrentLanguage.Identifier.Code;
		for (int j = 0; j < count; j++)
		{
			if (array[j] != null)
			{
				value.Add(array[j]);
			}
		}
		return value;
	}

	private static bool IsConnected()
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			return false;
		}
		if ((object)gPlayFabAuth == null)
		{
			gPlayFabAuth = PlayFabAuthenticator.instance;
		}
		if (gPlayFabAuth == null)
		{
			return false;
		}
		return true;
	}

	private static bool IsConnectedToPlayfab()
	{
		if ((object)gPlayFabAuth == null)
		{
			gPlayFabAuth = PlayFabAuthenticator.instance;
		}
		if (gPlayFabAuth == null)
		{
			return false;
		}
		return true;
	}

	private static bool IsConnectedIgnoreRoom()
	{
		if ((object)gPlayFabAuth == null)
		{
			gPlayFabAuth = PlayFabAuthenticator.instance;
		}
		if (gPlayFabAuth == null)
		{
			return false;
		}
		return true;
	}

	private static string PlayFabUserId()
	{
		return gPlayFabAuth.GetPlayFabPlayerId();
	}

	private static string SerializeCustomTags(string[] customTags)
	{
		string result = string.Empty;
		if (customTags != null && customTags.Length != 0)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			for (int i = 0; i < customTags.Length; i++)
			{
				dictionary.Add($"tag{i + 1}", customTags[i]);
			}
			result = JsonConvert.SerializeObject(dictionary);
		}
		return result;
	}

	public static void EnqueueZoneEvent(ZoneDef zone, GTZoneEventType zoneEventType)
	{
		if (zoneEventType != GTZoneEventType.zone_stay || !(Time.realtimeSinceStartup < nextStayTimestamp))
		{
			nextStayTimestamp = Time.realtimeSinceStartup + (float)zone.trackStayIntervalSec;
			if (IsConnected() && GorillaServer.Instance.CheckIsTZE_Enabled())
			{
				string value = PlayFabUserId();
				string name = zoneEventType.GetName();
				string name2 = zone.zoneId.GetName();
				string name3 = zone.subZoneId.GetName();
				bool sessionIsPrivate = NetworkSystem.Instance.SessionIsPrivate;
				Dictionary<string, object> dictionary = gZoneEventArgs;
				dictionary["User"] = value;
				dictionary["EventType"] = name;
				dictionary["ZoneId"] = name2;
				dictionary["SubZoneId"] = name3;
				dictionary["IsPrivateRoom"] = sessionIsPrivate;
				EnqueueTelemetryEventPlayFab(new EventContents
				{
					Name = "telemetry_zone_event",
					EventNamespace = EVENT_NAMESPACE,
					Payload = dictionary
				});
				EnqueueTelemetryEvent("telemetry_zone_event", dictionary);
			}
		}
	}

	public static void PostGameModeEvent(GTGameModeEventType gameModeEvent, GameModeType gameMode)
	{
		if (IsConnected())
		{
			string value = PlayFabUserId();
			string name = gameModeEvent.GetName();
			string name2 = gameMode.GetName();
			Dictionary<string, object> dictionary = gGameModeStartEventArgs;
			dictionary["User"] = value;
			dictionary["EventType"] = name;
			dictionary["game_mode"] = name2;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "game_mode_played_event",
				EventNamespace = EVENT_NAMESPACE,
				Payload = dictionary
			});
			EnqueueTelemetryEvent("game_mode_played_event", dictionary);
		}
	}

	public static void PostShopEvent(VRRig playerRig, GTShopEventType shopEvent, CosmeticsController.CosmeticItem item)
	{
		gSingleItemParam[0] = item;
		PostShopEvent(playerRig, shopEvent, gSingleItemParam);
		gSingleItemParam[0] = default(CosmeticsController.CosmeticItem);
	}

	private static string[] FetchItemArgs(IList<CosmeticsController.CosmeticItem> items)
	{
		int count = items.Count;
		if (count == 0)
		{
			return Array.Empty<string>();
		}
		HashSet<string> hashSet = new HashSet<string>(count);
		int num = 0;
		for (int i = 0; i < items.Count; i++)
		{
			CosmeticsController.CosmeticItem cosmeticItem = items[i];
			if (!cosmeticItem.isNullItem)
			{
				string itemName = cosmeticItem.itemName;
				if (!string.IsNullOrWhiteSpace(itemName) && !itemName.Contains("NOTHING", StringComparison.InvariantCultureIgnoreCase) && hashSet.Add(itemName))
				{
					num++;
				}
			}
		}
		string[] array = new string[num];
		hashSet.CopyTo(array);
		return array;
	}

	public static void PostShopEvent(VRRig playerRig, GTShopEventType shopEvent, IList<CosmeticsController.CosmeticItem> items)
	{
		if (IsConnected() && playerRig.isLocal)
		{
			string value = PlayFabUserId();
			string name = shopEvent.GetName();
			string[] value2 = FetchItemArgs(items);
			Dictionary<string, object> dictionary = gShopEventArgs;
			dictionary["User"] = value;
			dictionary["EventType"] = name;
			dictionary["Items"] = value2;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "telemetry_shop_event",
				EventNamespace = EVENT_NAMESPACE,
				Payload = dictionary
			});
			EnqueueTelemetryEvent("telemetry_shop_event", dictionary);
		}
	}

	private static void PostShopEvent_OnResult(WriteEventResponse result)
	{
	}

	private static void PostShopEvent_OnError(PlayFabError error)
	{
	}

	public static void PostBuilderKioskEvent(VRRig playerRig, GTShopEventType shopEvent, BuilderSetManager.BuilderSetStoreItem item)
	{
		gSingleItemBuilderParam[0] = item;
		PostBuilderKioskEvent(playerRig, shopEvent, gSingleItemBuilderParam);
		gSingleItemBuilderParam[0] = default(BuilderSetManager.BuilderSetStoreItem);
	}

	private static string[] BuilderItemsToStrings(IList<BuilderSetManager.BuilderSetStoreItem> items)
	{
		int count = items.Count;
		if (count == 0)
		{
			return Array.Empty<string>();
		}
		HashSet<string> hashSet = new HashSet<string>(count);
		int num = 0;
		for (int i = 0; i < items.Count; i++)
		{
			BuilderSetManager.BuilderSetStoreItem builderSetStoreItem = items[i];
			if (!builderSetStoreItem.isNullItem)
			{
				string playfabID = builderSetStoreItem.playfabID;
				if (!string.IsNullOrWhiteSpace(playfabID) && !playfabID.Contains("NOTHING", StringComparison.InvariantCultureIgnoreCase) && hashSet.Add(playfabID))
				{
					num++;
				}
			}
		}
		string[] array = new string[num];
		hashSet.CopyTo(array);
		return array;
	}

	public static void PostBuilderKioskEvent(VRRig playerRig, GTShopEventType shopEvent, IList<BuilderSetManager.BuilderSetStoreItem> items)
	{
		if (IsConnected() && playerRig.isLocal)
		{
			string value = PlayFabUserId();
			string name = shopEvent.GetName();
			string[] value2 = BuilderItemsToStrings(items);
			Dictionary<string, object> dictionary = gShopEventArgs;
			dictionary["User"] = value;
			dictionary["EventType"] = name;
			dictionary["Items"] = value2;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "telemetry_shop_event",
				EventNamespace = EVENT_NAMESPACE,
				Payload = dictionary
			});
			EnqueueTelemetryEvent("telemetry_shop_event", dictionary);
		}
	}

	public static void PostKidEvent(bool joinGroupsEnabled, bool voiceChatEnabled, bool customUsernamesEnabled, AgeStatusType ageCategory, GTKidEventType kidEvent)
	{
		if (!((double)UnityEngine.Random.value < 0.1) && IsConnected())
		{
			string value = PlayFabUserId();
			string name = kidEvent.GetName();
			string value2 = ((ageCategory == AgeStatusType.LEGALADULT) ? "Not_Managed_Account" : "Managed_Account");
			string value3 = joinGroupsEnabled.ToString().ToUpper();
			string value4 = voiceChatEnabled.ToString().ToUpper();
			string value5 = customUsernamesEnabled.ToString().ToUpper();
			Dictionary<string, object> dictionary = gKidEventArgs;
			dictionary["User"] = value;
			dictionary["EventType"] = name;
			dictionary["AgeCategory"] = value2;
			dictionary["VoiceChatEnabled"] = value4;
			dictionary["CustomUsernameEnabled"] = value5;
			dictionary["JoinGroups"] = value3;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "telemetry_kid_event",
				EventNamespace = EVENT_NAMESPACE,
				Payload = dictionary
			});
			EnqueueTelemetryEvent("telemetry_kid_event", dictionary);
		}
	}

	public static void WamGameStart(string playerId, string gameId, string machineId)
	{
		if (IsConnected())
		{
			gWamGameStartArgs["User"] = playerId;
			gWamGameStartArgs["WamGameId"] = gameId;
			gWamGameStartArgs["WamMachineId"] = machineId;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "telemetry_wam_gameStartEvent",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gWamGameStartArgs
			});
			EnqueueTelemetryEvent("telemetry_wam_gameStartEvent", gWamGameStartArgs);
		}
	}

	public static void WamLevelEnd(string playerId, int gameId, string machineId, int currentLevelNumber, int levelGoodMolesShown, int levelHazardMolesShown, int levelMinScore, int currentScore, int levelHazardMolesHit, string currentGameResult)
	{
		if (IsConnected())
		{
			gWamLevelEndArgs["User"] = playerId;
			gWamLevelEndArgs["WamGameId"] = gameId.ToString();
			gWamLevelEndArgs["WamMachineId"] = machineId;
			gWamLevelEndArgs["WamMLevelNumber"] = currentLevelNumber.ToString();
			gWamLevelEndArgs["WamGoodMolesShown"] = levelGoodMolesShown.ToString();
			gWamLevelEndArgs["WamHazardMolesShown"] = levelHazardMolesShown.ToString();
			gWamLevelEndArgs["WamLevelMinScore"] = levelMinScore.ToString();
			gWamLevelEndArgs["WamLevelScore"] = currentScore.ToString();
			gWamLevelEndArgs["WamHazardMolesHit"] = levelHazardMolesHit.ToString();
			gWamLevelEndArgs["WamGameState"] = currentGameResult;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "telemetry_wam_levelEndEvent",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gWamLevelEndArgs
			});
			EnqueueTelemetryEvent("telemetry_wam_levelEndEvent", gWamLevelEndArgs);
		}
	}

	public static void PostCustomMapPerformance(string mapName, long mapModId, int lowestFPS, int lowestDC, int lowestPC, int avgFPS, int avgDC, int avgPC, int highestFPS, int highestDC, int highestPC, int playtime)
	{
		if (IsConnected())
		{
			Dictionary<string, object> dictionary = gCustomMapPerfArgs;
			dictionary["CustomMapName"] = mapName;
			dictionary["CustomMapModId"] = mapModId.ToString();
			dictionary["LowestFPS"] = lowestFPS.ToString();
			dictionary["LowestFPSDrawCalls"] = lowestDC.ToString();
			dictionary["LowestFPSPlayerCount"] = lowestPC.ToString();
			dictionary["AverageFPS"] = avgFPS.ToString();
			dictionary["AverageDrawCalls"] = avgDC.ToString();
			dictionary["AveragePlayerCount"] = avgPC.ToString();
			dictionary["HighestFPS"] = highestFPS.ToString();
			dictionary["HighestFPSDrawCalls"] = highestDC.ToString();
			dictionary["HighestFPSPlayerCount"] = highestPC.ToString();
			dictionary["PlaytimeInSeconds"] = playtime.ToString();
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "CustomMapPerformance",
				EventNamespace = EVENT_NAMESPACE,
				Payload = dictionary
			});
			EnqueueTelemetryEvent("CustomMapPerformance", dictionary);
		}
	}

	public static void PostCustomMapTracking(string mapName, long mapModId, string mapCreatorUsername, int minPlayers, int maxPlayers, int playtime, bool privateRoom)
	{
		if (IsConnected())
		{
			int num = playtime % 60;
			int num2 = (playtime - num) / 60;
			int num3 = num2 % 60;
			int num4 = (num2 - num3) / 60;
			string value = $"{num4}.{num3}.{num}";
			Dictionary<string, object> dictionary = gCustomMapTrackingMetrics;
			dictionary["User"] = PlayFabUserId();
			dictionary["CustomMapName"] = mapName;
			dictionary["CustomMapModId"] = mapModId.ToString();
			dictionary["CustomMapCreator"] = mapCreatorUsername;
			dictionary["MinPlayerCount"] = minPlayers.ToString();
			dictionary["MaxPlayerCount"] = maxPlayers.ToString();
			dictionary["PlaytimeInSeconds"] = playtime.ToString();
			dictionary["PrivateRoom"] = privateRoom.ToString();
			dictionary["PlaytimeOnMap"] = value;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "CustomMapTracking",
				EventNamespace = EVENT_NAMESPACE,
				Payload = dictionary
			});
			EnqueueTelemetryEvent("CustomMapTracking", dictionary);
		}
	}

	public static void PostCustomMapDownloadEvent(string mapName, long mapModId, string mapCreatorUsername)
	{
	}

	public static void GhostReactorShiftStart(string gameId, int initialCores, float timeIntoShift, bool wasPlayerInAtStart, int numPlayers, int floorJoined, string playerRank)
	{
		if (IsConnected())
		{
			gGhostReactorShiftStartArgs["User"] = PlayFabUserId();
			gGhostReactorShiftStartArgs["ghost_game_id"] = gameId;
			gGhostReactorShiftStartArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorShiftStartArgs["initial_cores_balance"] = initialCores.ToString();
			gGhostReactorShiftStartArgs["number_of_players"] = numPlayers.ToString();
			gGhostReactorShiftStartArgs["start_at_beginning"] = wasPlayerInAtStart.ToString();
			gGhostReactorShiftStartArgs["seconds_into_shift_at_join"] = timeIntoShift.ToString();
			gGhostReactorShiftStartArgs["floor_joined"] = floorJoined.ToString();
			gGhostReactorShiftStartArgs["player_rank"] = playerRank;
			gGhostReactorShiftStartArgs["is_private_room"] = NetworkSystem.Instance.SessionIsPrivate.ToString();
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_game_start",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorShiftStartArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_game_start",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{
						"initial_cores_balance",
						initialCores.ToString()
					},
					{
						"number_of_players",
						numPlayers.ToString()
					},
					{
						"start_at_beginning",
						wasPlayerInAtStart.ToString()
					},
					{
						"seconds_into_shift_at_join",
						timeIntoShift.ToString()
					},
					{
						"floor_joined",
						floorJoined.ToString()
					},
					{ "player_rank", playerRank },
					{
						"is_private_room",
						NetworkSystem.Instance.SessionIsPrivate.ToString()
					}
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorGameEnd(string gameId, int finalCores, int totalCoresCollectedByPlayer, int totalCoresCollectedByGroup, int totalCoresSpentByPlayer, int totalCoresSpentByGroup, int gatesUnlocked, int deaths, List<string> itemsPurchased, int shiftCut, bool isShiftActuallyEnding, float timeIntoShiftAtJoin, float playDuration, bool wasPlayerInAtStart, ZoneClearReason zoneClearReason, int maxNumberOfPlayersInShift, int endNumberOfPlayers, Dictionary<string, int> itemTypesHeldThisShift, int revives, int numShiftsPlayed)
	{
		if (IsConnectedToPlayfab())
		{
			gGhostReactorShiftEndArgs["User"] = PlayFabUserId();
			gGhostReactorShiftEndArgs["ghost_game_id"] = gameId;
			gGhostReactorShiftEndArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorShiftEndArgs["final_cores_balance"] = finalCores.ToString();
			gGhostReactorShiftEndArgs["total_cores_collected_by_player"] = totalCoresCollectedByPlayer.ToString();
			gGhostReactorShiftEndArgs["total_cores_collected_by_group"] = totalCoresCollectedByGroup.ToString();
			gGhostReactorShiftEndArgs["total_cores_spent_by_player"] = totalCoresSpentByPlayer.ToString();
			gGhostReactorShiftEndArgs["total_cores_spent_by_group"] = totalCoresSpentByGroup.ToString();
			gGhostReactorShiftEndArgs["gates_unlocked"] = gatesUnlocked.ToString();
			gGhostReactorShiftEndArgs["died"] = deaths.ToString();
			gGhostReactorShiftEndArgs["items_purchased"] = itemsPurchased.ToJson();
			gGhostReactorShiftEndArgs["shift_cut"] = shiftCut.ToJson();
			gGhostReactorShiftEndArgs["play_duration"] = playDuration.ToString();
			gGhostReactorShiftEndArgs["started_late"] = (!wasPlayerInAtStart).ToString();
			gGhostReactorShiftEndArgs["time_started"] = timeIntoShiftAtJoin.ToString();
			gGhostReactorShiftEndArgs["revives"] = revives.ToString();
			string value = "shift_ended";
			if (!isShiftActuallyEnding)
			{
				value = ((zoneClearReason != ZoneClearReason.LeaveZone) ? "disconnect" : "left_zone");
			}
			gGhostReactorShiftEndArgs["reason"] = value;
			gGhostReactorShiftEndArgs["max_number_in_game"] = maxNumberOfPlayersInShift.ToString();
			gGhostReactorShiftEndArgs["end_number_in_game"] = endNumberOfPlayers.ToString();
			gGhostReactorShiftEndArgs["items_picked_up"] = itemTypesHeldThisShift.ToJson();
			gGhostReactorShiftEndArgs["num_shifts_played"] = numShiftsPlayed.ToString();
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_game_end",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorShiftEndArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_game_end",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{
						"final_cores_balance",
						finalCores.ToString()
					},
					{
						"total_cores_collected_by_player",
						totalCoresCollectedByPlayer.ToString()
					},
					{
						"total_cores_collected_by_group",
						totalCoresCollectedByGroup.ToString()
					},
					{
						"total_cores_spent_by_player",
						totalCoresSpentByPlayer.ToString()
					},
					{
						"total_cores_spent_by_group",
						totalCoresSpentByGroup.ToString()
					},
					{
						"gates_unlocked",
						gatesUnlocked.ToString()
					},
					{
						"died",
						deaths.ToString()
					},
					{
						"items_purchased",
						itemsPurchased.ToJson()
					},
					{
						"shift_cut_data",
						shiftCut.ToJson()
					},
					{
						"play_duration",
						playDuration.ToString()
					},
					{
						"started_late",
						(!wasPlayerInAtStart).ToString()
					},
					{
						"time_started",
						timeIntoShiftAtJoin.ToString()
					},
					{ "reason", value },
					{
						"max_number_in_game",
						maxNumberOfPlayersInShift.ToString()
					},
					{
						"end_number_in_game",
						endNumberOfPlayers.ToString()
					},
					{
						"items_picked_up",
						itemTypesHeldThisShift.ToJson()
					},
					{
						"revives",
						revives.ToString()
					},
					{
						"num_shifts_played",
						numShiftsPlayed.ToString()
					}
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorFloorStart(string gameId, int initialCores, float timeIntoShift, bool wasPlayerInAtStart, int numPlayers, string playerRank, int floor, string preset, string modifier)
	{
		if (IsConnected())
		{
			gGhostReactorFloorStartArgs["User"] = PlayFabUserId();
			gGhostReactorFloorStartArgs["ghost_game_id"] = gameId;
			gGhostReactorFloorStartArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorFloorStartArgs["initial_cores_balance"] = initialCores.ToString();
			gGhostReactorFloorStartArgs["number_of_players"] = numPlayers.ToString();
			gGhostReactorFloorStartArgs["start_at_beginning"] = wasPlayerInAtStart.ToString();
			gGhostReactorFloorStartArgs["seconds_into_shift_at_join"] = timeIntoShift.ToString();
			gGhostReactorFloorStartArgs["player_rank"] = playerRank;
			gGhostReactorFloorStartArgs["floor"] = floor.ToString();
			gGhostReactorFloorStartArgs["preset"] = preset.ToString();
			gGhostReactorFloorStartArgs["modifier"] = modifier.ToString();
			gGhostReactorFloorStartArgs["is_private_room"] = NetworkSystem.Instance.SessionIsPrivate.ToString();
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_floor_start",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorFloorStartArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_floor_start",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{
						"initial_cores_balance",
						initialCores.ToString()
					},
					{
						"number_of_players",
						numPlayers.ToString()
					},
					{
						"start_at_beginning",
						wasPlayerInAtStart.ToString()
					},
					{
						"seconds_into_shift_at_join",
						timeIntoShift.ToString()
					},
					{ "player_rank", playerRank },
					{
						"floor",
						floor.ToString()
					},
					{
						"preset",
						preset.ToString()
					},
					{
						"modifier",
						modifier.ToString()
					},
					{
						"is_private_room",
						NetworkSystem.Instance.SessionIsPrivate.ToString()
					}
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorFloorComplete(string gameId, int finalCores, int totalCoresCollectedByPlayer, int totalCoresCollectedByGroup, int totalCoresSpentByPlayer, int totalCoresSpentByGroup, int gatesUnlocked, int deaths, List<string> itemsPurchased, int shiftCut, bool isShiftActuallyEnding, float timeIntoShiftAtJoin, float playDuration, bool wasPlayerInAtStart, ZoneClearReason zoneClearReason, int maxNumberOfPlayersInShift, int endNumberOfPlayers, Dictionary<string, int> itemTypesHeldThisShift, int revives, int floor, string preset, string modifier, int chaosSeedsCollected, bool objectivesCompleted, string section, int xpGained)
	{
		if (IsConnectedToPlayfab())
		{
			gGhostReactorFloorEndArgs["User"] = PlayFabUserId();
			gGhostReactorFloorEndArgs["ghost_game_id"] = gameId;
			gGhostReactorFloorEndArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorFloorEndArgs["final_cores_balance"] = finalCores.ToString();
			gGhostReactorFloorEndArgs["total_cores_collected_by_player"] = totalCoresCollectedByPlayer.ToString();
			gGhostReactorFloorEndArgs["total_cores_collected_by_group"] = totalCoresCollectedByGroup.ToString();
			gGhostReactorFloorEndArgs["total_cores_spent_by_player"] = totalCoresSpentByPlayer.ToString();
			gGhostReactorFloorEndArgs["total_cores_spent_by_group"] = totalCoresSpentByGroup.ToString();
			gGhostReactorFloorEndArgs["gates_unlocked"] = gatesUnlocked.ToString();
			gGhostReactorFloorEndArgs["died"] = deaths.ToString();
			gGhostReactorFloorEndArgs["items_purchased"] = itemsPurchased.ToJson();
			gGhostReactorFloorEndArgs["shift_cut"] = shiftCut.ToJson();
			gGhostReactorFloorEndArgs["play_duration"] = playDuration.ToString();
			gGhostReactorFloorEndArgs["started_late"] = (!wasPlayerInAtStart).ToString();
			gGhostReactorFloorEndArgs["time_started"] = timeIntoShiftAtJoin.ToString();
			gGhostReactorFloorEndArgs["revives"] = revives.ToString();
			string value = "shift_ended";
			if (!isShiftActuallyEnding)
			{
				value = ((zoneClearReason != ZoneClearReason.LeaveZone) ? "disconnect" : "left_zone");
			}
			gGhostReactorFloorEndArgs["reason"] = value;
			gGhostReactorFloorEndArgs["max_number_in_game"] = maxNumberOfPlayersInShift.ToString();
			gGhostReactorFloorEndArgs["end_number_in_game"] = endNumberOfPlayers.ToString();
			gGhostReactorFloorEndArgs["items_picked_up"] = itemTypesHeldThisShift.ToJson();
			gGhostReactorFloorEndArgs["floor"] = floor.ToString();
			gGhostReactorFloorEndArgs["preset"] = preset;
			gGhostReactorFloorEndArgs["modifier"] = modifier;
			gGhostReactorFloorEndArgs["section"] = section;
			gGhostReactorFloorEndArgs["xp_gained"] = xpGained.ToString();
			gGhostReactorFloorEndArgs["chaos_seeds_collected"] = chaosSeedsCollected.ToString();
			gGhostReactorFloorEndArgs["objectives_completed"] = objectivesCompleted.ToString();
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_floor_end",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorFloorEndArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_floor_end",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{
						"final_cores_balance",
						finalCores.ToString()
					},
					{
						"total_cores_collected_by_player",
						totalCoresCollectedByPlayer.ToString()
					},
					{
						"total_cores_collected_by_group",
						totalCoresCollectedByGroup.ToString()
					},
					{
						"total_cores_spent_by_player",
						totalCoresSpentByPlayer.ToString()
					},
					{
						"total_cores_spent_by_group",
						totalCoresSpentByGroup.ToString()
					},
					{
						"gates_unlocked",
						gatesUnlocked.ToString()
					},
					{
						"died",
						deaths.ToString()
					},
					{
						"items_purchased",
						itemsPurchased.ToJson()
					},
					{
						"shift_cut_data",
						shiftCut.ToJson()
					},
					{
						"play_duration",
						playDuration.ToString()
					},
					{
						"started_late",
						(!wasPlayerInAtStart).ToString()
					},
					{
						"time_started",
						timeIntoShiftAtJoin.ToString()
					},
					{ "reason", value },
					{
						"max_number_in_game",
						maxNumberOfPlayersInShift.ToString()
					},
					{
						"end_number_in_game",
						endNumberOfPlayers.ToString()
					},
					{
						"items_picked_up",
						itemTypesHeldThisShift.ToJson()
					},
					{
						"revives",
						revives.ToString()
					},
					{
						"floor",
						floor.ToString()
					},
					{ "preset", preset },
					{ "modifier", modifier },
					{
						"chaos_seeds_collected",
						chaosSeedsCollected.ToString()
					},
					{
						"objectives_completed",
						objectivesCompleted.ToString()
					},
					{ "section", section },
					{
						"xp_gained",
						xpGained.ToString()
					}
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorToolPurchased(string gameId, string toolName, int toolLevel, int coresSpent, int shinyRocksSpent, int floor, string preset)
	{
		if (IsConnected())
		{
			gGhostReactorToolPurchasedArgs["User"] = PlayFabUserId();
			gGhostReactorToolPurchasedArgs["ghost_game_id"] = gameId;
			gGhostReactorToolPurchasedArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorToolPurchasedArgs["tool"] = toolName;
			gGhostReactorToolPurchasedArgs["tool_level"] = toolLevel.ToString();
			gGhostReactorToolPurchasedArgs["cores_spent"] = coresSpent.ToString();
			gGhostReactorToolPurchasedArgs["shiny_rocks_spent"] = shinyRocksSpent.ToString();
			gGhostReactorToolPurchasedArgs["floor"] = floor.ToString();
			gGhostReactorToolPurchasedArgs["preset"] = preset;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_tool_purchased",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorToolPurchasedArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_tool_purchased",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{ "tool", toolName },
					{
						"tool_level",
						toolLevel.ToString()
					},
					{
						"cores_spent",
						coresSpent.ToString()
					},
					{
						"shiny_rocks_spent",
						shinyRocksSpent.ToString()
					},
					{
						"floor",
						floor.ToString()
					},
					{ "preset", preset }
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorRankUp(string gameId, string newRank, int floor, string preset)
	{
		if (IsConnected())
		{
			gGhostReactorRankUpArgs["User"] = PlayFabUserId();
			gGhostReactorRankUpArgs["ghost_game_id"] = gameId;
			gGhostReactorRankUpArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorRankUpArgs["new_rank"] = newRank;
			gGhostReactorRankUpArgs["floor"] = floor.ToString();
			gGhostReactorRankUpArgs["preset"] = preset;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_game_rank_up",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorRankUpArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_game_rank_up",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{ "new_rank", newRank },
					{
						"floor",
						floor.ToString()
					},
					{ "preset", preset }
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorToolUnlock(string gameId, string toolName)
	{
		if (IsConnected())
		{
			gGhostReactorToolUnlockArgs["User"] = PlayFabUserId();
			gGhostReactorToolUnlockArgs["ghost_game_id"] = gameId;
			gGhostReactorToolUnlockArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorToolUnlockArgs["tool"] = toolName;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_game_tool_unlock",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorToolUnlockArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_game_tool_unlock",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{ "tool", toolName }
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorPodUpgradePurchased(string gameId, string toolName, int level, int shinyRocksSpent, int juiceSpent)
	{
		if (IsConnected())
		{
			gGhostReactorPodUpgradePurchasedArgs["User"] = PlayFabUserId();
			gGhostReactorPodUpgradePurchasedArgs["ghost_game_id"] = gameId;
			gGhostReactorPodUpgradePurchasedArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorPodUpgradePurchasedArgs["tool"] = toolName;
			gGhostReactorPodUpgradePurchasedArgs["new_level"] = level.ToString();
			gGhostReactorPodUpgradePurchasedArgs["shiny_rocks_spent"] = shinyRocksSpent.ToString();
			gGhostReactorPodUpgradePurchasedArgs["juice_spent"] = juiceSpent.ToString();
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_pod_upgrade_purchased",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorPodUpgradePurchasedArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_pod_upgrade_purchased",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{ "tool", toolName },
					{
						"new_level",
						level.ToString()
					},
					{
						"shiny_rocks_spent",
						shinyRocksSpent.ToString()
					},
					{
						"juice_spent",
						juiceSpent.ToString()
					}
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorToolUpgrade(string gameId, string upgradeType, string toolName, int newLevel, int juiceSpent, int griftSpent, int coresSpent, int floor, string preset)
	{
		if (IsConnected())
		{
			gGhostReactorToolUpgradeArgs["User"] = PlayFabUserId();
			gGhostReactorToolUpgradeArgs["ghost_game_id"] = gameId;
			gGhostReactorToolUpgradeArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorToolUpgradeArgs["type"] = upgradeType;
			gGhostReactorToolUpgradeArgs["tool"] = toolName;
			gGhostReactorToolUpgradeArgs["new_level"] = newLevel.ToString();
			gGhostReactorToolUpgradeArgs["juice_spent"] = juiceSpent.ToString();
			gGhostReactorToolUpgradeArgs["grift_spent"] = griftSpent.ToString();
			gGhostReactorToolUpgradeArgs["cores_spent"] = coresSpent.ToString();
			gGhostReactorToolUpgradeArgs["floor"] = floor.ToString();
			gGhostReactorToolUpgradeArgs["preset"] = preset;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_game_tool_upgrade",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorToolUpgradeArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_game_tool_upgrade",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{ "type", upgradeType },
					{ "tool", toolName },
					{
						"new_level",
						newLevel.ToString()
					},
					{
						"juice_spent",
						juiceSpent.ToString()
					},
					{
						"grift_spent",
						griftSpent.ToString()
					},
					{
						"cores_spent",
						coresSpent.ToString()
					},
					{
						"floor",
						floor.ToString()
					},
					{ "preset", preset }
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorChaosSeedStart(string gameId, string unlockTime, int chaosSeedsInQueue, int floor, string preset)
	{
		if (IsConnected())
		{
			gGhostReactorChaosSeedStartArgs["User"] = PlayFabUserId();
			gGhostReactorChaosSeedStartArgs["ghost_game_id"] = gameId;
			gGhostReactorChaosSeedStartArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorChaosSeedStartArgs["unlock_time"] = unlockTime;
			gGhostReactorChaosSeedStartArgs["chaos_seeds_in_queue"] = chaosSeedsInQueue.ToString();
			gGhostReactorChaosSeedStartArgs["floor"] = floor.ToString();
			gGhostReactorChaosSeedStartArgs["preset"] = preset;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_chaos_seed_start",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorChaosSeedStartArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_chaos_seed_start",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{ "unlock_time", unlockTime },
					{
						"chaos_seeds_in_queue",
						chaosSeedsInQueue.ToString()
					},
					{
						"floor",
						floor.ToString()
					},
					{ "preset", preset }
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorChaosJuiceCollected(string gameId, int juiceCollected, int coresProcessedByOverdrive)
	{
		if (IsConnected())
		{
			gGhostReactorChaosJuiceCollectedArgs["User"] = PlayFabUserId();
			gGhostReactorChaosJuiceCollectedArgs["ghost_game_id"] = gameId;
			gGhostReactorChaosJuiceCollectedArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorChaosJuiceCollectedArgs["juice_collected"] = juiceCollected.ToString();
			gGhostReactorChaosJuiceCollectedArgs["cores_processed_by_overdrive"] = coresProcessedByOverdrive.ToString();
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_chaos_juice_collected",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorChaosJuiceCollectedArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_chaos_juice_collected",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{
						"juice_collected",
						juiceCollected.ToString()
					},
					{
						"cores_processed_by_overdrive",
						coresProcessedByOverdrive.ToString()
					}
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorOverdrivePurchased(string gameId, int shinyRocksUsed, int chaosSeedsInQueue, int floor, string preset)
	{
		if (IsConnected())
		{
			gGhostReactorOverdrivePurchasedArgs["User"] = PlayFabUserId();
			gGhostReactorOverdrivePurchasedArgs["ghost_game_id"] = gameId;
			gGhostReactorOverdrivePurchasedArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorOverdrivePurchasedArgs["shiny_rocks_used"] = shinyRocksUsed.ToString();
			gGhostReactorOverdrivePurchasedArgs["chaos_seeds_in_queue"] = chaosSeedsInQueue.ToString();
			gGhostReactorOverdrivePurchasedArgs["floor"] = floor.ToString();
			gGhostReactorOverdrivePurchasedArgs["preset"] = preset;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_overdrive_purchased",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorOverdrivePurchasedArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_overdrive_purchased",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{
						"shiny_rocks_used",
						shinyRocksUsed.ToString()
					},
					{
						"chaos_seeds_in_queue",
						chaosSeedsInQueue.ToString()
					},
					{
						"floor",
						floor.ToString()
					},
					{ "preset", preset }
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorCreditsRefillPurchased(string gameId, int shinyRocksSpent, int finalCredits, int floor, string preset)
	{
		if (IsConnected())
		{
			gGhostReactorCreditsRefillPurchasedArgs["User"] = PlayFabUserId();
			gGhostReactorCreditsRefillPurchasedArgs["ghost_game_id"] = gameId;
			gGhostReactorCreditsRefillPurchasedArgs["event_timestamp"] = DateTime.Now.ToString();
			gGhostReactorCreditsRefillPurchasedArgs["shiny_rocks_spent"] = shinyRocksSpent.ToString();
			gGhostReactorCreditsRefillPurchasedArgs["final_credits"] = finalCredits.ToString();
			gGhostReactorCreditsRefillPurchasedArgs["floor"] = floor.ToString();
			gGhostReactorCreditsRefillPurchasedArgs["preset"] = preset;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "ghost_credits_refill_purchased",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gGhostReactorCreditsRefillPurchasedArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "ghost_credits_refill_purchased",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{ "ghost_game_id", gameId },
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{
						"shiny_rocks_spent",
						shinyRocksSpent.ToString()
					},
					{
						"final_credits",
						finalCredits.ToString()
					},
					{
						"floor",
						floor.ToString()
					},
					{ "preset", preset }
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void SuperInfectionEvent(bool roomDisconnect, float totalPlayTime, float roomPlayTime, float sessionPlayTime, float intervalPlayTime, float terminalTotalTime, float terminalIntervalTime, Dictionary<SITechTreePageId, float> timeUsingGadgetsTotal, Dictionary<SITechTreePageId, float> timeUsingGadgetsInterval, float timeUsingOwnGadgetsTotal, float timeUsingOwnGadgetsInterval, float timeUsingOthersGadgetsTotal, float timeUsingOthersGadgetsInterval, Dictionary<SITechTreePageId, int> tagsUsingGadgetsTotal, Dictionary<SITechTreePageId, int> tagsUsingGadgetsInterval, int tagsHoldingOwnGadgetsTotal, int tagsHoldingOwnGadgetsInterval, int tagsHoldingOthersGadgetsTotal, int tagsHoldingOthersGadgetsInterval, Dictionary<SIResource.ResourceType, int> resourcesGatheredTotal, Dictionary<SIResource.ResourceType, int> resourcesGatheredInterval, int roundsPlayedTotal, int roundsPlayedInterval, bool[][] unlockedNodes, int numberOfPlayers)
	{
		if (!IsConnectedIgnoreRoom())
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < unlockedNodes.Length; i++)
		{
			num += unlockedNodes[i].Length;
		}
		char[] array = new char[num];
		num = 0;
		for (int j = 0; j < unlockedNodes.Length; j++)
		{
			for (int l = 0; l < unlockedNodes[j].Length; l++)
			{
				array[num] = (unlockedNodes[j][l] ? '1' : '0');
				num++;
			}
		}
		gSuperInfectionArgs["User"] = PlayFabUserId();
		gSuperInfectionArgs["event_timestamp"] = DateTime.Now.ToString();
		gSuperInfectionArgs["total_play_time"] = totalPlayTime.ToString();
		gSuperInfectionArgs["room_play_time"] = roomPlayTime.ToString();
		gSuperInfectionArgs["session_play_time"] = sessionPlayTime.ToString();
		gSuperInfectionArgs["interval_play_time"] = intervalPlayTime.ToString();
		gSuperInfectionArgs["terminal_total_time"] = terminalTotalTime.ToString();
		gSuperInfectionArgs["terminal_interval_time"] = terminalIntervalTime.ToString();
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
		Dictionary<string, object> dictionary3 = new Dictionary<string, object>();
		Dictionary<string, object> dictionary4 = new Dictionary<string, object>();
		for (int m = 0; m < 11; m++)
		{
			SITechTreePageId key = (SITechTreePageId)m;
			timeUsingGadgetsTotal.TryGetValue(key, out var value);
			timeUsingGadgetsInterval.TryGetValue(key, out var value2);
			tagsUsingGadgetsTotal.TryGetValue(key, out var value3);
			tagsUsingGadgetsInterval.TryGetValue(key, out var value4);
			string key2 = key.ToString();
			dictionary[key2] = value.ToString();
			dictionary2[key2] = value2.ToString();
			dictionary3[key2] = value3.ToString();
			dictionary4[key2] = value4.ToString();
		}
		Dictionary<string, object> dictionary5 = new Dictionary<string, object>();
		Dictionary<string, object> dictionary6 = new Dictionary<string, object>();
		for (int n = 0; n < 6; n++)
		{
			SIResource.ResourceType key3 = (SIResource.ResourceType)n;
			resourcesGatheredTotal.TryGetValue(key3, out var value5);
			resourcesGatheredInterval.TryGetValue(key3, out var value6);
			string key4 = key3.ToString();
			dictionary5[key4] = value5.ToString();
			dictionary6[key4] = value6.ToString();
		}
		gSuperInfectionArgs["time_holding_gadget_type_total"] = dictionary;
		gSuperInfectionArgs["time_holding_gadget_type_interval"] = dictionary2;
		gSuperInfectionArgs["time_holding_own_gadgets_total"] = timeUsingOwnGadgetsTotal.ToString();
		gSuperInfectionArgs["time_holding_own_gadgets_interval"] = timeUsingOwnGadgetsInterval.ToString();
		gSuperInfectionArgs["time_holding_others_gadgets_total"] = timeUsingOthersGadgetsTotal.ToString();
		gSuperInfectionArgs["time_holding_others_gadgets_interval"] = timeUsingOthersGadgetsInterval.ToString();
		gSuperInfectionArgs["tags_holding_gadget_type_total"] = dictionary3;
		gSuperInfectionArgs["tags_holding_gadget_type_interval"] = dictionary4;
		gSuperInfectionArgs["tags_holding_own_gadgets_total"] = tagsHoldingOwnGadgetsTotal.ToString();
		gSuperInfectionArgs["tags_holding_own_gadgets_interval"] = tagsHoldingOwnGadgetsInterval.ToString();
		gSuperInfectionArgs["tags_holding_others_gadgets_total"] = tagsHoldingOthersGadgetsTotal.ToString();
		gSuperInfectionArgs["tags_holding_others_gadgets_interval"] = tagsHoldingOthersGadgetsInterval.ToString();
		gSuperInfectionArgs["resource_collected_total"] = dictionary5;
		gSuperInfectionArgs["resource_collected_interval"] = dictionary6;
		gSuperInfectionArgs["rounds_played_total"] = roundsPlayedTotal.ToString();
		gSuperInfectionArgs["rounds_played_interval"] = roundsPlayedInterval.ToString();
		gSuperInfectionArgs["unlocked_nodes"] = new string(array);
		gSuperInfectionArgs["number_of_players"] = numberOfPlayers.ToString();
		EnqueueTelemetryEventPlayFab(new EventContents
		{
			Name = (roomDisconnect ? "super_infection_room_disconnect" : "super_infection_interval"),
			EventNamespace = EVENT_NAMESPACE,
			Payload = gSuperInfectionArgs
		});
		GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
		{
			EventName = (roomDisconnect ? "super_infection_room_left" : "super_infection_interval"),
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{
					"event_timestamp",
					DateTime.Now.ToString()
				},
				{
					"total_play_time",
					totalPlayTime.ToString()
				},
				{
					"room_play_time",
					roomPlayTime.ToString()
				},
				{
					"session_play_time",
					sessionPlayTime.ToString()
				},
				{
					"interval_play_time",
					intervalPlayTime.ToString()
				},
				{
					"terminal_total_time",
					terminalTotalTime.ToString()
				},
				{
					"terminal_interval_time",
					terminalIntervalTime.ToString()
				},
				{ "time_holding_gadget_type_total", timeUsingGadgetsTotal },
				{ "time_holding_gadget_type_interval", timeUsingGadgetsInterval },
				{
					"time_holding_own_gadgets_total",
					timeUsingOwnGadgetsTotal.ToString()
				},
				{
					"time_holding_own_gadgets_interval",
					timeUsingOwnGadgetsInterval.ToString()
				},
				{
					"time_holding_others_gadgets_total",
					timeUsingOthersGadgetsTotal.ToString()
				},
				{
					"time_holding_others_gadgets_interval",
					timeUsingOthersGadgetsInterval.ToString()
				},
				{ "tags_holding_gadget_type_total", dictionary3 },
				{ "tags_holding_gadget_type_interval", dictionary4 },
				{
					"tags_holding_own_gadgets_total",
					tagsHoldingOwnGadgetsTotal.ToString()
				},
				{
					"tags_holding_own_gadgets_interval",
					tagsHoldingOwnGadgetsInterval.ToString()
				},
				{
					"tags_holding_others_gadgets_total",
					tagsHoldingOthersGadgetsTotal.ToString()
				},
				{
					"tags_holding_others_gadgets_interval",
					tagsHoldingOthersGadgetsInterval.ToString()
				},
				{ "resource_type_collected_total", dictionary5 },
				{ "resource_type_collected_interval", dictionary6 },
				{
					"rounds_played_total",
					roundsPlayedTotal.ToString()
				},
				{
					"rounds_played_interval",
					roundsPlayedInterval.ToString()
				},
				{
					"unlocked_nodes",
					new string(array)
				},
				{
					"player_count",
					numberOfPlayers.ToString()
				}
			}
		};
		EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
	}

	public static void SuperInfectionEvent(string purchaseType, int shinyRockCost, int techPointsPurchased, float totalPlayTime, float roomPlayTime, float sessionPlayTime)
	{
		if (IsConnectedIgnoreRoom())
		{
			gSuperInfectionArgs["User"] = PlayFabUserId();
			gSuperInfectionArgs["event_timestamp"] = DateTime.Now.ToString();
			gSuperInfectionArgs["total_play_time"] = totalPlayTime.ToString();
			gSuperInfectionArgs["room_play_time"] = roomPlayTime.ToString();
			gSuperInfectionArgs["session_play_time"] = sessionPlayTime.ToString();
			gSuperInfectionArgs["si_purchase_type"] = purchaseType;
			gSuperInfectionArgs["si_shiny_rock_cost"] = shinyRockCost;
			gSuperInfectionArgs["si_tech_points_purchased"] = techPointsPurchased;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "super_infection_purchase",
				EventNamespace = EVENT_NAMESPACE,
				Payload = gSuperInfectionArgs
			});
			GhostReactorTelemetryData ghostReactorTelemetryData = new GhostReactorTelemetryData
			{
				EventName = "super_infection_purchase",
				CustomTags = new string[2]
				{
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, object>
				{
					{
						"event_timestamp",
						DateTime.Now.ToString()
					},
					{
						"total_play_time",
						totalPlayTime.ToString()
					},
					{
						"room_play_time",
						roomPlayTime.ToString()
					},
					{
						"session_play_time",
						sessionPlayTime.ToString()
					},
					{
						"si_purchase_type",
						purchaseType.ToString()
					},
					{
						"si_shiny_rock_cost",
						shinyRockCost.ToString()
					},
					{
						"si_tech_points_purchased",
						techPointsPurchased.ToString()
					}
				}
			};
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void PostNotificationEvent(string notificationType)
	{
		if (IsConnected())
		{
			string value = PlayFabUserId();
			Dictionary<string, object> dictionary = gNotifEventArgs;
			dictionary["User"] = value;
			dictionary["EventType"] = notificationType;
			EnqueueTelemetryEventPlayFab(new EventContents
			{
				Name = "telemetry_ggwp_event",
				EventNamespace = EVENT_NAMESPACE,
				Payload = dictionary
			});
			EnqueueTelemetryEvent("telemetry_ggwp_event", dictionary);
		}
	}
}
