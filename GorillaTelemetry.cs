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
				FlushMothershipTelemetry();
			}
		}
	}

	private static readonly float TELEMETRY_FLUSH_SEC;

	private static readonly ConcurrentQueue<MothershipAnalyticsEvent> telemetryEventsQueueMothership;

	private static readonly Dictionary<int, List<MothershipAnalyticsEvent>> gListPoolMothership;

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

	private static readonly GhostReactorTelemetryData gGhostReactorShiftStartArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorShiftEndArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorFloorStartArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorFloorEndArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorToolPurchasedArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorRankUpArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorToolUnlockArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorPodUpgradePurchasedArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorToolUpgradeArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorChaosSeedStartArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorChaosJuiceCollectedArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorOverdrivePurchasedArgs;

	private static readonly GhostReactorTelemetryData gGhostReactorCreditsRefillPurchasedArgs;

	private static readonly SuperInfectionTelemetryData gSuperInfectionArgs;

	private static readonly SuperInfectionTelemetryData gSuperInfectionPurchaseArgs;

	static GorillaTelemetry()
	{
		TELEMETRY_FLUSH_SEC = 10f;
		telemetryEventsQueueMothership = new ConcurrentQueue<MothershipAnalyticsEvent>();
		gListPoolMothership = new Dictionary<int, List<MothershipAnalyticsEvent>>();
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
		gGhostReactorShiftStartArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_game_start",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "initial_cores_balance", null },
				{ "number_of_players", null },
				{ "start_at_beginning", null },
				{ "seconds_into_shift_at_join", null },
				{ "floor_joined", null },
				{ "player_rank", null },
				{ "is_private_room", null }
			}
		};
		gGhostReactorShiftEndArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_game_end",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "final_cores_balance", null },
				{ "total_cores_collected_by_player", null },
				{ "total_cores_collected_by_group", null },
				{ "total_cores_spent_by_player", null },
				{ "total_cores_spent_by_group", null },
				{ "gates_unlocked", null },
				{ "died", null },
				{ "items_purchased", null },
				{ "shift_cut_data", null },
				{ "play_duration", null },
				{ "started_late", null },
				{ "time_started", null },
				{ "reason", null },
				{ "max_number_in_game", null },
				{ "end_number_in_game", null },
				{ "items_picked_up", null },
				{ "revives", null },
				{ "num_shifts_played", null }
			}
		};
		gGhostReactorFloorStartArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_floor_start",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "initial_cores_balance", null },
				{ "number_of_players", null },
				{ "start_at_beginning", null },
				{ "seconds_into_shift_at_join", null },
				{ "player_rank", null },
				{ "floor", null },
				{ "preset", null },
				{ "modifier", null },
				{ "is_private_room", null }
			}
		};
		gGhostReactorFloorEndArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_floor_end",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "final_cores_balance", null },
				{ "total_cores_collected_by_player", null },
				{ "total_cores_collected_by_group", null },
				{ "total_cores_spent_by_player", null },
				{ "total_cores_spent_by_group", null },
				{ "gates_unlocked", null },
				{ "died", null },
				{ "items_purchased", null },
				{ "shift_cut_data", null },
				{ "play_duration", null },
				{ "started_late", null },
				{ "time_started", null },
				{ "reason", null },
				{ "max_number_in_game", null },
				{ "end_number_in_game", null },
				{ "items_picked_up", null },
				{ "revives", null },
				{ "floor", null },
				{ "preset", null },
				{ "modifier", null },
				{ "chaos_seeds_collected", null },
				{ "objectives_completed", null },
				{ "section", null },
				{ "xp_gained", null }
			}
		};
		gGhostReactorToolPurchasedArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_tool_purchased",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "tool", null },
				{ "tool_level", null },
				{ "cores_spent", null },
				{ "shiny_rocks_spent", null },
				{ "floor", null },
				{ "preset", null }
			}
		};
		gGhostReactorRankUpArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_game_rank_up",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "new_rank", null },
				{ "floor", null },
				{ "preset", null }
			}
		};
		gGhostReactorToolUnlockArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_game_tool_unlock",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "tool", null }
			}
		};
		gGhostReactorPodUpgradePurchasedArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_pod_upgrade_purchased",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "tool", null },
				{ "new_level", null },
				{ "shiny_rocks_spent", null },
				{ "juice_spent", null }
			}
		};
		gGhostReactorToolUpgradeArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_game_tool_upgrade",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "type", null },
				{ "tool", null },
				{ "new_level", null },
				{ "juice_spent", null },
				{ "grift_spent", null },
				{ "cores_spent", null },
				{ "floor", null },
				{ "preset", null }
			}
		};
		gGhostReactorChaosSeedStartArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_chaos_seed_start",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "unlock_time", null },
				{ "chaos_seeds_in_queue", null },
				{ "floor", null },
				{ "preset", null }
			}
		};
		gGhostReactorChaosJuiceCollectedArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_chaos_juice_collected",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "juice_collected", null },
				{ "cores_processed_by_overdrive", null }
			}
		};
		gGhostReactorOverdrivePurchasedArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_overdrive_purchased",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "shiny_rocks_used", null },
				{ "chaos_seeds_in_queue", null },
				{ "floor", null },
				{ "preset", null }
			}
		};
		gGhostReactorCreditsRefillPurchasedArgs = new GhostReactorTelemetryData
		{
			EventName = "ghost_credits_refill_purchased",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "ghost_game_id", null },
				{ "event_timestamp", null },
				{ "shiny_rocks_spent", null },
				{ "final_credits", null },
				{ "floor", null },
				{ "preset", null }
			}
		};
		gSuperInfectionArgs = new SuperInfectionTelemetryData
		{
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "event_timestamp", null },
				{ "total_play_time", null },
				{ "room_play_time", null },
				{ "session_play_time", null },
				{ "interval_play_time", null },
				{ "terminal_total_time", null },
				{ "terminal_interval_time", null },
				{ "time_holding_gadget_type_total", null },
				{ "time_holding_gadget_type_interval", null },
				{ "time_holding_own_gadgets_total", null },
				{ "time_holding_own_gadgets_interval", null },
				{ "time_holding_others_gadgets_total", null },
				{ "time_holding_others_gadgets_interval", null },
				{ "tags_holding_gadget_type_total", null },
				{ "tags_holding_gadget_type_interval", null },
				{ "tags_holding_own_gadgets_total", null },
				{ "tags_holding_own_gadgets_interval", null },
				{ "tags_holding_others_gadgets_total", null },
				{ "tags_holding_others_gadgets_interval", null },
				{ "resource_type_collected_total", null },
				{ "resource_type_collected_interval", null },
				{ "rounds_played_total", null },
				{ "rounds_played_interval", null },
				{ "unlocked_nodes", null },
				{ "player_count", null }
			}
		};
		gSuperInfectionPurchaseArgs = new SuperInfectionTelemetryData
		{
			EventName = "super_infection_purchase",
			CustomTags = new string[2]
			{
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment
			},
			BodyData = new Dictionary<string, object>
			{
				{ "event_timestamp", null },
				{ "total_play_time", null },
				{ "room_play_time", null },
				{ "session_play_time", null },
				{ "si_purchase_type", null },
				{ "si_shiny_rock_cost", null },
				{ "si_tech_points_purchased", null }
			}
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
			EnqueueTelemetryEvent("telemetry_shop_event", dictionary);
		}
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
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorShiftStartArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["initial_cores_balance"] = initialCores.ToString();
			ghostReactorTelemetryData.BodyData["number_of_players"] = numPlayers.ToString();
			ghostReactorTelemetryData.BodyData["start_at_beginning"] = wasPlayerInAtStart.ToString();
			ghostReactorTelemetryData.BodyData["seconds_into_shift_at_join"] = timeIntoShift.ToString();
			ghostReactorTelemetryData.BodyData["floor_joined"] = floorJoined.ToString();
			ghostReactorTelemetryData.BodyData["player_rank"] = playerRank;
			ghostReactorTelemetryData.BodyData["is_private_room"] = NetworkSystem.Instance.SessionIsPrivate.ToString();
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorGameEnd(string gameId, int finalCores, int totalCoresCollectedByPlayer, int totalCoresCollectedByGroup, int totalCoresSpentByPlayer, int totalCoresSpentByGroup, int gatesUnlocked, int deaths, List<string> itemsPurchased, int shiftCut, bool isShiftActuallyEnding, float timeIntoShiftAtJoin, float playDuration, bool wasPlayerInAtStart, ZoneClearReason zoneClearReason, int maxNumberOfPlayersInShift, int endNumberOfPlayers, Dictionary<string, int> itemTypesHeldThisShift, int revives, int numShiftsPlayed)
	{
		if (IsConnectedToPlayfab())
		{
			string value = "shift_ended";
			if (!isShiftActuallyEnding)
			{
				value = ((zoneClearReason != ZoneClearReason.LeaveZone) ? "disconnect" : "left_zone");
			}
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorShiftEndArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["final_cores_balance"] = finalCores.ToString();
			ghostReactorTelemetryData.BodyData["total_cores_collected_by_player"] = totalCoresCollectedByPlayer.ToString();
			ghostReactorTelemetryData.BodyData["total_cores_collected_by_group"] = totalCoresCollectedByGroup.ToString();
			ghostReactorTelemetryData.BodyData["total_cores_spent_by_player"] = totalCoresSpentByPlayer.ToString();
			ghostReactorTelemetryData.BodyData["total_cores_spent_by_group"] = totalCoresSpentByGroup.ToString();
			ghostReactorTelemetryData.BodyData["gates_unlocked"] = gatesUnlocked.ToString();
			ghostReactorTelemetryData.BodyData["died"] = deaths.ToString();
			ghostReactorTelemetryData.BodyData["items_purchased"] = itemsPurchased.ToJson();
			ghostReactorTelemetryData.BodyData["shift_cut_data"] = shiftCut.ToJson();
			ghostReactorTelemetryData.BodyData["play_duration"] = playDuration.ToString();
			ghostReactorTelemetryData.BodyData["started_late"] = (!wasPlayerInAtStart).ToString();
			ghostReactorTelemetryData.BodyData["time_started"] = timeIntoShiftAtJoin.ToString();
			ghostReactorTelemetryData.BodyData["reason"] = value;
			ghostReactorTelemetryData.BodyData["max_number_in_game"] = maxNumberOfPlayersInShift.ToString();
			ghostReactorTelemetryData.BodyData["end_number_in_game"] = endNumberOfPlayers.ToString();
			ghostReactorTelemetryData.BodyData["items_picked_up"] = itemTypesHeldThisShift.ToJson();
			ghostReactorTelemetryData.BodyData["revives"] = revives.ToString();
			ghostReactorTelemetryData.BodyData["num_shifts_played"] = numShiftsPlayed.ToString();
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorFloorStart(string gameId, int initialCores, float timeIntoShift, bool wasPlayerInAtStart, int numPlayers, string playerRank, int floor, string preset, string modifier)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorFloorStartArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["initial_cores_balance"] = initialCores.ToString();
			ghostReactorTelemetryData.BodyData["number_of_players"] = numPlayers.ToString();
			ghostReactorTelemetryData.BodyData["start_at_beginning"] = wasPlayerInAtStart.ToString();
			ghostReactorTelemetryData.BodyData["seconds_into_shift_at_join"] = timeIntoShift.ToString();
			ghostReactorTelemetryData.BodyData["player_rank"] = playerRank;
			ghostReactorTelemetryData.BodyData["floor"] = floor.ToString();
			ghostReactorTelemetryData.BodyData["preset"] = preset;
			ghostReactorTelemetryData.BodyData["modifier"] = modifier;
			ghostReactorTelemetryData.BodyData["is_private_room"] = NetworkSystem.Instance.SessionIsPrivate.ToString();
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorFloorComplete(string gameId, int finalCores, int totalCoresCollectedByPlayer, int totalCoresCollectedByGroup, int totalCoresSpentByPlayer, int totalCoresSpentByGroup, int gatesUnlocked, int deaths, List<string> itemsPurchased, int shiftCut, bool isShiftActuallyEnding, float timeIntoShiftAtJoin, float playDuration, bool wasPlayerInAtStart, ZoneClearReason zoneClearReason, int maxNumberOfPlayersInShift, int endNumberOfPlayers, Dictionary<string, int> itemTypesHeldThisShift, int revives, int floor, string preset, string modifier, int chaosSeedsCollected, bool objectivesCompleted, string section, int xpGained)
	{
		if (IsConnectedToPlayfab())
		{
			string value = "shift_ended";
			if (!isShiftActuallyEnding)
			{
				value = ((zoneClearReason != ZoneClearReason.LeaveZone) ? "disconnect" : "left_zone");
			}
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorFloorEndArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["final_cores_balance"] = finalCores.ToString();
			ghostReactorTelemetryData.BodyData["total_cores_collected_by_player"] = totalCoresCollectedByPlayer.ToString();
			ghostReactorTelemetryData.BodyData["total_cores_collected_by_group"] = totalCoresCollectedByGroup.ToString();
			ghostReactorTelemetryData.BodyData["total_cores_spent_by_player"] = totalCoresSpentByPlayer.ToString();
			ghostReactorTelemetryData.BodyData["total_cores_spent_by_group"] = totalCoresSpentByGroup.ToString();
			ghostReactorTelemetryData.BodyData["gates_unlocked"] = gatesUnlocked.ToString();
			ghostReactorTelemetryData.BodyData["died"] = deaths.ToString();
			ghostReactorTelemetryData.BodyData["items_purchased"] = itemsPurchased.ToJson();
			ghostReactorTelemetryData.BodyData["shift_cut_data"] = shiftCut.ToJson();
			ghostReactorTelemetryData.BodyData["play_duration"] = playDuration.ToString();
			ghostReactorTelemetryData.BodyData["started_late"] = (!wasPlayerInAtStart).ToString();
			ghostReactorTelemetryData.BodyData["time_started"] = timeIntoShiftAtJoin.ToString();
			ghostReactorTelemetryData.BodyData["reason"] = value;
			ghostReactorTelemetryData.BodyData["max_number_in_game"] = maxNumberOfPlayersInShift.ToString();
			ghostReactorTelemetryData.BodyData["end_number_in_game"] = endNumberOfPlayers.ToString();
			ghostReactorTelemetryData.BodyData["items_picked_up"] = itemTypesHeldThisShift.ToJson();
			ghostReactorTelemetryData.BodyData["revives"] = revives.ToString();
			ghostReactorTelemetryData.BodyData["floor"] = floor.ToString();
			ghostReactorTelemetryData.BodyData["preset"] = preset;
			ghostReactorTelemetryData.BodyData["modifier"] = modifier;
			ghostReactorTelemetryData.BodyData["chaos_seeds_collected"] = chaosSeedsCollected.ToString();
			ghostReactorTelemetryData.BodyData["objectives_completed"] = objectivesCompleted.ToString();
			ghostReactorTelemetryData.BodyData["section"] = section;
			ghostReactorTelemetryData.BodyData["xp_gained"] = xpGained.ToString();
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorToolPurchased(string gameId, string toolName, int toolLevel, int coresSpent, int shinyRocksSpent, int floor, string preset)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorToolPurchasedArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["tool"] = toolName;
			ghostReactorTelemetryData.BodyData["tool_level"] = toolLevel.ToString();
			ghostReactorTelemetryData.BodyData["cores_spent"] = coresSpent.ToString();
			ghostReactorTelemetryData.BodyData["shiny_rocks_spent"] = shinyRocksSpent.ToString();
			ghostReactorTelemetryData.BodyData["floor"] = floor.ToString();
			ghostReactorTelemetryData.BodyData["preset"] = preset;
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorRankUp(string gameId, string newRank, int floor, string preset)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorRankUpArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["new_rank"] = newRank;
			ghostReactorTelemetryData.BodyData["floor"] = floor.ToString();
			ghostReactorTelemetryData.BodyData["preset"] = preset;
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorToolUnlock(string gameId, string toolName)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorToolUnlockArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["tool"] = toolName;
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorPodUpgradePurchased(string gameId, string toolName, int level, int shinyRocksSpent, int juiceSpent)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorPodUpgradePurchasedArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["tool"] = toolName;
			ghostReactorTelemetryData.BodyData["new_level"] = level.ToString();
			ghostReactorTelemetryData.BodyData["shiny_rocks_spent"] = shinyRocksSpent.ToString();
			ghostReactorTelemetryData.BodyData["juice_spent"] = juiceSpent.ToString();
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorToolUpgrade(string gameId, string upgradeType, string toolName, int newLevel, int juiceSpent, int griftSpent, int coresSpent, int floor, string preset)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorToolUpgradeArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["type"] = upgradeType;
			ghostReactorTelemetryData.BodyData["tool"] = toolName;
			ghostReactorTelemetryData.BodyData["new_level"] = newLevel.ToString();
			ghostReactorTelemetryData.BodyData["juice_spent"] = juiceSpent.ToString();
			ghostReactorTelemetryData.BodyData["grift_spent"] = griftSpent.ToString();
			ghostReactorTelemetryData.BodyData["cores_spent"] = coresSpent.ToString();
			ghostReactorTelemetryData.BodyData["floor"] = floor.ToString();
			ghostReactorTelemetryData.BodyData["preset"] = preset;
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorChaosSeedStart(string gameId, string unlockTime, int chaosSeedsInQueue, int floor, string preset)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorChaosSeedStartArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["unlock_time"] = unlockTime;
			ghostReactorTelemetryData.BodyData["chaos_seeds_in_queue"] = chaosSeedsInQueue.ToString();
			ghostReactorTelemetryData.BodyData["floor"] = floor.ToString();
			ghostReactorTelemetryData.BodyData["preset"] = preset;
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorChaosJuiceCollected(string gameId, int juiceCollected, int coresProcessedByOverdrive)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorChaosJuiceCollectedArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["juice_collected"] = juiceCollected.ToString();
			ghostReactorTelemetryData.BodyData["cores_processed_by_overdrive"] = coresProcessedByOverdrive.ToString();
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorOverdrivePurchased(string gameId, int shinyRocksUsed, int chaosSeedsInQueue, int floor, string preset)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorOverdrivePurchasedArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["shiny_rocks_used"] = shinyRocksUsed.ToString();
			ghostReactorTelemetryData.BodyData["chaos_seeds_in_queue"] = chaosSeedsInQueue.ToString();
			ghostReactorTelemetryData.BodyData["floor"] = floor.ToString();
			ghostReactorTelemetryData.BodyData["preset"] = preset;
			EnqueueTelemetryEvent(ghostReactorTelemetryData.EventName, ghostReactorTelemetryData.BodyData, ghostReactorTelemetryData.CustomTags);
		}
	}

	public static void GhostReactorCreditsRefillPurchased(string gameId, int shinyRocksSpent, int finalCredits, int floor, string preset)
	{
		if (IsConnected())
		{
			GhostReactorTelemetryData ghostReactorTelemetryData = gGhostReactorCreditsRefillPurchasedArgs;
			ghostReactorTelemetryData.BodyData["ghost_game_id"] = gameId;
			ghostReactorTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			ghostReactorTelemetryData.BodyData["shiny_rocks_spent"] = shinyRocksSpent.ToString();
			ghostReactorTelemetryData.BodyData["final_credits"] = finalCredits.ToString();
			ghostReactorTelemetryData.BodyData["floor"] = floor.ToString();
			ghostReactorTelemetryData.BodyData["preset"] = preset;
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
		Span<char> span = stackalloc char[num];
		num = 0;
		for (int j = 0; j < unlockedNodes.Length; j++)
		{
			for (int l = 0; l < unlockedNodes[j].Length; l++)
			{
				span[num] = (unlockedNodes[j][l] ? '1' : '0');
				num++;
			}
		}
		SuperInfectionTelemetryData superInfectionTelemetryData = gSuperInfectionArgs;
		superInfectionTelemetryData.EventName = (roomDisconnect ? "super_infection_room_left" : "super_infection_interval");
		Dictionary<string, object> bodyData = superInfectionTelemetryData.BodyData;
		if (bodyData["tags_holding_gadget_type_total"] == null)
		{
			object obj = (bodyData["tags_holding_gadget_type_total"] = new Dictionary<string, object>());
		}
		bodyData = superInfectionTelemetryData.BodyData;
		if (bodyData["tags_holding_gadget_type_interval"] == null)
		{
			object obj = (bodyData["tags_holding_gadget_type_interval"] = new Dictionary<string, object>());
		}
		Dictionary<string, object> dictionary3 = (Dictionary<string, object>)superInfectionTelemetryData.BodyData["tags_holding_gadget_type_total"];
		Dictionary<string, object> dictionary4 = (Dictionary<string, object>)superInfectionTelemetryData.BodyData["tags_holding_gadget_type_interval"];
		for (int m = 0; m < 11; m++)
		{
			SITechTreePageId key = (SITechTreePageId)m;
			tagsUsingGadgetsTotal.TryGetValue(key, out var value);
			tagsUsingGadgetsInterval.TryGetValue(key, out var value2);
			string key2 = key.ToString();
			dictionary3[key2] = value.ToString();
			dictionary4[key2] = value2.ToString();
		}
		bodyData = superInfectionTelemetryData.BodyData;
		if (bodyData["resource_type_collected_total"] == null)
		{
			object obj = (bodyData["resource_type_collected_total"] = new Dictionary<string, object>());
		}
		bodyData = superInfectionTelemetryData.BodyData;
		if (bodyData["resource_type_collected_interval"] == null)
		{
			object obj = (bodyData["resource_type_collected_interval"] = new Dictionary<string, object>());
		}
		Dictionary<string, object> dictionary7 = (Dictionary<string, object>)superInfectionTelemetryData.BodyData["resource_type_collected_total"];
		Dictionary<string, object> dictionary8 = (Dictionary<string, object>)superInfectionTelemetryData.BodyData["resource_type_collected_interval"];
		for (int n = 0; n < 6; n++)
		{
			SIResource.ResourceType key3 = (SIResource.ResourceType)n;
			resourcesGatheredTotal.TryGetValue(key3, out var value3);
			resourcesGatheredInterval.TryGetValue(key3, out var value4);
			string key4 = key3.ToString();
			dictionary7[key4] = value3.ToString();
			dictionary8[key4] = value4.ToString();
		}
		superInfectionTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
		superInfectionTelemetryData.BodyData["total_play_time"] = totalPlayTime.ToString();
		superInfectionTelemetryData.BodyData["room_play_time"] = roomPlayTime.ToString();
		superInfectionTelemetryData.BodyData["session_play_time"] = sessionPlayTime.ToString();
		superInfectionTelemetryData.BodyData["interval_play_time"] = intervalPlayTime.ToString();
		superInfectionTelemetryData.BodyData["terminal_total_time"] = terminalTotalTime.ToString();
		superInfectionTelemetryData.BodyData["terminal_interval_time"] = terminalIntervalTime.ToString();
		superInfectionTelemetryData.BodyData["time_holding_gadget_type_total"] = timeUsingGadgetsTotal;
		superInfectionTelemetryData.BodyData["time_holding_gadget_type_interval"] = timeUsingGadgetsInterval;
		superInfectionTelemetryData.BodyData["time_holding_own_gadgets_total"] = timeUsingOwnGadgetsTotal.ToString();
		superInfectionTelemetryData.BodyData["time_holding_own_gadgets_interval"] = timeUsingOwnGadgetsInterval.ToString();
		superInfectionTelemetryData.BodyData["time_holding_others_gadgets_total"] = timeUsingOthersGadgetsTotal.ToString();
		superInfectionTelemetryData.BodyData["time_holding_others_gadgets_interval"] = timeUsingOthersGadgetsInterval.ToString();
		superInfectionTelemetryData.BodyData["tags_holding_gadget_type_total"] = dictionary3;
		superInfectionTelemetryData.BodyData["tags_holding_gadget_type_interval"] = dictionary4;
		superInfectionTelemetryData.BodyData["tags_holding_own_gadgets_total"] = tagsHoldingOwnGadgetsTotal.ToString();
		superInfectionTelemetryData.BodyData["tags_holding_own_gadgets_interval"] = tagsHoldingOwnGadgetsInterval.ToString();
		superInfectionTelemetryData.BodyData["tags_holding_others_gadgets_total"] = tagsHoldingOthersGadgetsTotal.ToString();
		superInfectionTelemetryData.BodyData["tags_holding_others_gadgets_interval"] = tagsHoldingOthersGadgetsInterval.ToString();
		superInfectionTelemetryData.BodyData["resource_type_collected_total"] = dictionary7;
		superInfectionTelemetryData.BodyData["resource_type_collected_interval"] = dictionary8;
		superInfectionTelemetryData.BodyData["rounds_played_total"] = roundsPlayedTotal.ToString();
		superInfectionTelemetryData.BodyData["rounds_played_interval"] = roundsPlayedInterval.ToString();
		superInfectionTelemetryData.BodyData["unlocked_nodes"] = new string(span);
		superInfectionTelemetryData.BodyData["player_count"] = numberOfPlayers.ToString();
		EnqueueTelemetryEvent(superInfectionTelemetryData.EventName, superInfectionTelemetryData.BodyData, superInfectionTelemetryData.CustomTags);
	}

	public static void SuperInfectionEvent(string purchaseType, int shinyRockCost, int techPointsPurchased, float totalPlayTime, float roomPlayTime, float sessionPlayTime)
	{
		if (IsConnectedIgnoreRoom())
		{
			SuperInfectionTelemetryData superInfectionTelemetryData = gSuperInfectionPurchaseArgs;
			superInfectionTelemetryData.BodyData["event_timestamp"] = DateTime.Now.ToString();
			superInfectionTelemetryData.BodyData["total_play_time"] = totalPlayTime.ToString();
			superInfectionTelemetryData.BodyData["room_play_time"] = roomPlayTime.ToString();
			superInfectionTelemetryData.BodyData["session_play_time"] = sessionPlayTime.ToString();
			superInfectionTelemetryData.BodyData["si_purchase_type"] = purchaseType;
			superInfectionTelemetryData.BodyData["si_shiny_rock_cost"] = shinyRockCost.ToString();
			superInfectionTelemetryData.BodyData["si_tech_points_purchased"] = techPointsPurchased.ToString();
			EnqueueTelemetryEvent(superInfectionTelemetryData.EventName, superInfectionTelemetryData.BodyData, superInfectionTelemetryData.CustomTags);
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
			EnqueueTelemetryEvent("telemetry_ggwp_event", dictionary);
		}
	}
}
