using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GorillaNetworking;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.Networking;

namespace GorillaTagScripts.Builder;

public class SharedBlocksManager : MonoBehaviour
{
	[Serializable]
	public class SharedBlocksMap
	{
		public string MapID { get; set; }

		public string CreatorID { get; set; }

		public string CreatorNickName { get; set; }

		public DateTime CreateTime { get; set; }

		public DateTime UpdateTime { get; set; }

		public string MapData { get; set; }
	}

	[Serializable]
	public struct LocalPublishInfo
	{
		public string mapID;

		public long publishTime;
	}

	[Serializable]
	private class SharedBlocksRequestBase
	{
		public string mothershipId;

		public string mothershipToken;

		public string mothershipEnvId;
	}

	[Serializable]
	private class VoteRequest : SharedBlocksRequestBase
	{
		public string mapId;

		public int vote;
	}

	[Serializable]
	private class PublishMapRequestData : SharedBlocksRequestBase
	{
		public string userdataMetadataKey;

		public string playerNickname;
	}

	public enum MapSortMethod
	{
		Top,
		NewlyCreated,
		RecentlyUpdated
	}

	public struct StartingMapConfig
	{
		public int pageNumber;

		public int pageSize;

		public string sortMethod;

		public bool useMapID;

		public string mapID;
	}

	[Serializable]
	private class GetMapsRequest : SharedBlocksRequestBase
	{
		public int page;

		public int pageSize;

		public string sort;

		public bool ShowInactive;
	}

	[Serializable]
	private class GetMapDataFromIDRequest : SharedBlocksRequestBase
	{
		public string mapId;
	}

	[Serializable]
	private class GetMapIDFromPlayerRequest : SharedBlocksRequestBase
	{
		public string requestId;

		public string requestUserDataMetaKey;
	}

	[Serializable]
	private class GetMapIDFromPlayerResponse
	{
		public SharedBlocksMapMetaData result;

		public int statusCode;

		public string error;
	}

	[Serializable]
	private class SharedBlocksMapMetaData
	{
		public string mapId;

		public string mothershipId;

		public string userDataMetadataKey;

		public string nickname;

		public string createdTime;

		public string updatedTime;

		public int voteCount;

		public bool isActive;
	}

	[Serializable]
	private struct GetMapDataFromPlayerRequestData
	{
		public string CreatorID;

		public string MapScan;

		public BlocksMapRequestCallback Callback;
	}

	[Serializable]
	private class UpdateMapActiveRequest : SharedBlocksRequestBase
	{
		public string userdataMetadataKey;

		public bool setActive;
	}

	public delegate void PublishMapRequestCallback(bool success, string key, string mapID, long responseCode);

	public delegate void BlocksMapRequestCallback(SharedBlocksMap response);

	public static SharedBlocksManager instance;

	[SerializeField]
	private BuilderTableSerializationConfig serializationConfig;

	private int maxRetriesOnFail = 3;

	public const int MAP_ID_LENGTH = 8;

	private const string MAP_ID_PATTERN = "^[CFGHKMNPRTWXZ256789]+$";

	public const float MINIMUM_REFRESH_DELAY = 60f;

	public const int VOTE_HISTORY_LENGTH = 10;

	private const int NUM_CACHED_MAP_RESULTS = 5;

	private StartingMapConfig startingMapConfig = new StartingMapConfig
	{
		pageNumber = 0,
		pageSize = 50,
		sortMethod = MapSortMethod.Top.ToString(),
		useMapID = false,
		mapID = null
	};

	private bool hasQueriedSaveTime;

	private static List<string> saveDateKeys = new List<string>(BuilderScanKiosk.NUM_SAVE_SLOTS);

	private bool fetchedTableConfig;

	private int fetchTableConfigRetryCount;

	private string tableConfigResponse;

	private bool fetchTitleDataBuildInProgress;

	private bool fetchTitleDataBuildComplete;

	private int fetchTitleDataRetryCount;

	private string titleDataBuildCache = string.Empty;

	private bool[] hasPulledPrivateScanPlayfab = new bool[BuilderScanKiosk.NUM_SAVE_SLOTS];

	private int fetchPlayfabBuildsRetryCount;

	private readonly int publicSlotIndex = BuilderScanKiosk.NUM_SAVE_SLOTS;

	private string[] privateScanDataCache = new string[BuilderScanKiosk.NUM_SAVE_SLOTS];

	private bool[] hasPulledPrivateScanMothership = new bool[BuilderScanKiosk.NUM_SAVE_SLOTS];

	private bool hasPulledDevScan;

	private string devScanDataCache;

	private bool saveScanInProgress;

	private int currentSaveScanIndex = -1;

	private string currentSaveScanData = string.Empty;

	private bool getScanInProgress;

	private int currentGetScanIndex = -1;

	private int voteRetryCount;

	private bool voteInProgress;

	private bool publishRequestInProgress;

	private int postPublishMapRetryCount;

	private bool getMapDataFromIDInProgress;

	private int getMapDataFromIDRetryCount;

	private bool getTopMapsInProgress;

	private int getTopMapsRetryCount;

	private bool hasCachedTopMaps;

	private double lastGetTopMapsTime = double.MinValue;

	private bool updateMapActiveInProgress;

	private int updateMapActiveRetryCount;

	private List<SharedBlocksMap> latestPopularMaps = new List<SharedBlocksMap>();

	private static LinkedList<string> recentUpVotes = new LinkedList<string>();

	private static Dictionary<int, LocalPublishInfo> localPublishData = new Dictionary<int, LocalPublishInfo>(BuilderScanKiosk.NUM_SAVE_SLOTS);

	private static List<string> localMapIds = new List<string>(BuilderScanKiosk.NUM_SAVE_SLOTS);

	private List<SharedBlocksMap> mapResponseCache = new List<SharedBlocksMap>(5);

	private SharedBlocksMap defaultMap;

	private bool hasDefaultMap;

	private double defaultMapCacheTime = double.MinValue;

	private bool getDefaultMapInProgress;

	public List<SharedBlocksMap> LatestPopularMaps => latestPopularMaps;

	public string[] BuildData => privateScanDataCache;

	public event Action<string> OnGetTableConfiguration;

	public event Action<string> OnGetTitleDataBuildComplete;

	public event Action<int> OnSavePrivateScanSuccess;

	public event Action<int, string> OnSavePrivateScanFailed;

	public event Action<int, bool> OnFetchPrivateScanComplete;

	public event Action<bool, SharedBlocksMap> OnFoundDefaultSharedBlocksMap;

	public event Action<bool> OnGetPopularMapsComplete;

	public static event Action OnRecentMapIdsUpdated;

	public static event Action OnSaveTimeUpdated;

	public bool IsWaitingOnRequest()
	{
		if (!saveScanInProgress)
		{
			return getScanInProgress;
		}
		return true;
	}

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			for (int i = 0; i < BuilderScanKiosk.NUM_SAVE_SLOTS; i++)
			{
				privateScanDataCache[i] = string.Empty;
				hasPulledPrivateScanMothership[i] = false;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	public async void Start()
	{
		saveDateKeys.Clear();
		for (int i = 0; i < BuilderScanKiosk.NUM_SAVE_SLOTS; i++)
		{
			saveDateKeys.Add(GetPlayfabSlotTimeKey(i));
		}
		await WaitForPlayfabSessionToken();
		FetchConfigurationFromTitleData();
		LoadPlayerPrefs();
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnJoinedRoom);
		}
		if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom)
		{
			RefreshPopularMapsForRandom();
		}
	}

	private void OnDestroy()
	{
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnMultiplayerStarted -= new Action(OnJoinedRoom);
		}
	}

	private void OnJoinedRoom()
	{
		Debug.Log("OnJoinedRoom inside SharedBlocksManager");
		RefreshPopularMapsForRandom();
	}

	private bool TryGetCachedSharedBlocksMapByMapID(string mapID, out SharedBlocksMap result)
	{
		foreach (SharedBlocksMap item in mapResponseCache)
		{
			if (item.MapID.Equals(mapID))
			{
				result = item;
				return true;
			}
		}
		result = null;
		return false;
	}

	private void AddMapToResponseCache(SharedBlocksMap map)
	{
		if (map == null)
		{
			return;
		}
		try
		{
			int num = mapResponseCache.FindIndex((SharedBlocksMap x) => x.MapID.Equals(map.MapID));
			if (num < 0)
			{
				mapResponseCache.Add(map);
			}
			else
			{
				mapResponseCache[num] = map;
			}
		}
		catch (Exception ex)
		{
			GTDev.LogError("SharedBlocksManager AddMapToResponseCache Exception " + ex.ToString());
		}
		if (mapResponseCache.Count >= 5)
		{
			mapResponseCache.RemoveAt(0);
		}
	}

	public static bool IsMapIDValid(string mapID)
	{
		if (mapID.IsNullOrEmpty())
		{
			return false;
		}
		if (mapID.Length != 8)
		{
			return false;
		}
		if (!Regex.IsMatch(mapID, "^[CFGHKMNPRTWXZ256789]+$"))
		{
			GTDev.LogError("Invalid Characters in SharedBlocksManager IsMapIDValid map " + mapID);
			return false;
		}
		return true;
	}

	public static LinkedList<string> GetRecentUpVotes()
	{
		return recentUpVotes;
	}

	public static List<string> GetLocalMapIDs()
	{
		return localMapIds;
	}

	private static void SetPublishTimeForSlot(int slotID, DateTime time)
	{
		if (localPublishData.TryGetValue(slotID, out var value))
		{
			value.publishTime = time.ToBinary();
			localPublishData[slotID] = value;
			return;
		}
		LocalPublishInfo value2 = new LocalPublishInfo
		{
			mapID = null,
			publishTime = time.ToBinary()
		};
		localPublishData.Add(slotID, value2);
	}

	private static void SetMapIDAndPublishTimeForSlot(int slotID, string mapID, DateTime time)
	{
		LocalPublishInfo value = new LocalPublishInfo
		{
			mapID = mapID,
			publishTime = time.ToBinary()
		};
		localPublishData.AddOrUpdate(slotID, value);
	}

	public static LocalPublishInfo GetPublishInfoForSlot(int slot)
	{
		if (localPublishData.TryGetValue(slot, out var value))
		{
			return value;
		}
		return new LocalPublishInfo
		{
			mapID = null,
			publishTime = DateTime.MinValue.ToBinary()
		};
	}

	private void LoadPlayerPrefs()
	{
		string recentVotesPrefsKey = serializationConfig.recentVotesPrefsKey;
		string localMapsPrefsKey = serializationConfig.localMapsPrefsKey;
		string text = PlayerPrefs.GetString(recentVotesPrefsKey, null);
		string text2 = PlayerPrefs.GetString(localMapsPrefsKey, null);
		if (!text.IsNullOrEmpty())
		{
			try
			{
				recentUpVotes = JsonConvert.DeserializeObject<LinkedList<string>>(text);
				while (recentUpVotes.Count > 10)
				{
					recentUpVotes.RemoveLast();
				}
			}
			catch (Exception ex)
			{
				GTDev.LogWarning("SharedBlocksManager failed to deserialize Recent Up Votes " + ex.Message);
				recentUpVotes.Clear();
			}
		}
		else
		{
			recentUpVotes.Clear();
		}
		if (!text2.IsNullOrEmpty())
		{
			localPublishData.Clear();
			localMapIds.Clear();
			try
			{
				localPublishData = JsonConvert.DeserializeObject<Dictionary<int, LocalPublishInfo>>(text2);
			}
			catch (Exception ex2)
			{
				GTDev.LogWarning("SharedBlocksManager failed to deserialize localMapIDs " + ex2.Message);
				GetPlayfabLastSaveTime();
			}
			foreach (KeyValuePair<int, LocalPublishInfo> localPublishDatum in localPublishData)
			{
				if (!localPublishDatum.Value.mapID.IsNullOrEmpty() && IsMapIDValid(localPublishDatum.Value.mapID))
				{
					localMapIds.Add(localPublishDatum.Value.mapID);
				}
			}
			SharedBlocksManager.OnSaveTimeUpdated?.Invoke();
		}
		else
		{
			localMapIds.Clear();
			GetPlayfabLastSaveTime();
		}
		SharedBlocksManager.OnRecentMapIdsUpdated?.Invoke();
	}

	private void SaveRecentVotesToPlayerPrefs()
	{
		PlayerPrefs.SetString(serializationConfig.recentVotesPrefsKey, JsonConvert.SerializeObject(recentUpVotes));
		PlayerPrefs.Save();
	}

	private void SaveLocalMapIdsToPlayerPrefs()
	{
		PlayerPrefs.SetString(serializationConfig.localMapsPrefsKey, JsonConvert.SerializeObject(localPublishData));
		PlayerPrefs.Save();
	}

	public void RequestVote(string mapID, bool up, Action<bool, string> callback)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("SharedBlocksManager RequestVote Client Not Logged into Mothership");
			callback?.Invoke(arg1: false, 1.ToString());
			return;
		}
		if (voteInProgress)
		{
			GTDev.LogWarning("SharedBlocksManager RequestVote already in progress");
			return;
		}
		voteInProgress = true;
		StartCoroutine(PostVote(new VoteRequest
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			mapId = mapID,
			vote = (up ? 1 : (-1))
		}, callback));
	}

	private IEnumerator PostVote(VoteRequest data, Action<bool, string> callback)
	{
		UnityWebRequest request = new UnityWebRequest(serializationConfig.sharedBlocksApiBaseURL + "/api/MapVote", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			string mapId = data.mapId;
			if (data.vote == -1)
			{
				if (recentUpVotes.Remove(mapId))
				{
					SaveRecentVotesToPlayerPrefs();
					SharedBlocksManager.OnRecentMapIdsUpdated?.Invoke();
				}
			}
			else if (!recentUpVotes.Contains(mapId))
			{
				if (recentUpVotes.Count >= 10)
				{
					recentUpVotes.RemoveLast();
				}
				recentUpVotes.AddFirst(mapId);
				SaveRecentVotesToPlayerPrefs();
				SharedBlocksManager.OnRecentMapIdsUpdated?.Invoke();
			}
			voteInProgress = false;
			callback?.Invoke(arg1: true, "");
		}
		else
		{
			GTDev.LogError($"PostVote Error: {request.responseCode} -- raw response: " + request.downloadHandler.text);
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0202;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0202;
				}
				flag = false;
				goto IL_020a;
			}
			retry = true;
		}
		goto IL_0235;
		IL_0202:
		flag = true;
		goto IL_020a;
		IL_020a:
		if (flag)
		{
			retry = true;
		}
		else
		{
			voteInProgress = false;
			callback?.Invoke(arg1: false, "REQUEST ERROR");
		}
		goto IL_0235;
		IL_0235:
		if (retry)
		{
			if (voteRetryCount < maxRetriesOnFail)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, voteRetryCount + 1));
				voteRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				voteInProgress = false;
				RequestVote(data.mapId, data.vote == 1, callback);
			}
			else
			{
				voteRetryCount = 0;
				voteInProgress = false;
				callback?.Invoke(arg1: false, "CONNECTION ERROR");
			}
		}
	}

	public void RefreshPopularMapsForRandom()
	{
		if (ZoneManagement.instance.IsZoneActive(GTZone.monkeBlocksShared) && MothershipClientContext.IsClientLoggedIn() && !getTopMapsInProgress)
		{
			if (hasCachedTopMaps && Time.realtimeSinceStartupAsDouble <= lastGetTopMapsTime + 60.0 && latestPopularMaps != null && latestPopularMaps.Count > 0)
			{
				this.OnGetPopularMapsComplete?.Invoke(obj: true);
			}
			else
			{
				RequestGetConfiguredTopMaps();
			}
		}
	}

	public bool RequestGetConfiguredTopMaps()
	{
		return RequestGetTopMaps(startingMapConfig.pageNumber, startingMapConfig.pageSize, startingMapConfig.sortMethod);
	}

	private void RequestPublishMap(string userMetadataKey)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("SharedBlocksManager RequestPublishMap Client Not Logged into Mothership");
			PublishMapComplete(success: false, userMetadataKey, string.Empty, 0L);
			return;
		}
		if (publishRequestInProgress)
		{
			GTDev.LogWarning("SharedBlocksManager RequestPublishMap Publish Request in progress");
			return;
		}
		publishRequestInProgress = true;
		StartCoroutine(PostPublishMapRequest(new PublishMapRequestData
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			userdataMetadataKey = userMetadataKey,
			playerNickname = GorillaTagger.Instance.offlineVRRig.playerNameVisible
		}, PublishMapComplete));
	}

	private void PublishMapComplete(bool success, string key, [CanBeNull] string mapID, long response)
	{
		publishRequestInProgress = false;
		if (success)
		{
			int num = serializationConfig.scanSlotMothershipKeys.IndexOf(key);
			if (num >= 0)
			{
				if (localPublishData.TryGetValue(num, out var value))
				{
					localMapIds.Remove(value.mapID);
				}
				SetMapIDAndPublishTimeForSlot(num, mapID, DateTime.Now);
				SaveLocalMapIdsToPlayerPrefs();
			}
			if (!localMapIds.Contains(mapID))
			{
				localMapIds.Add(mapID);
				SharedBlocksManager.OnRecentMapIdsUpdated?.Invoke();
			}
			SharedBlocksMap map = new SharedBlocksMap
			{
				MapID = mapID,
				MapData = privateScanDataCache[num],
				CreatorNickName = GorillaTagger.Instance.offlineVRRig.playerNameVisible,
				UpdateTime = DateTime.Now
			};
			AddMapToResponseCache(map);
			this.OnSavePrivateScanSuccess?.Invoke(currentSaveScanIndex);
		}
		else
		{
			this.OnSavePrivateScanFailed?.Invoke(currentSaveScanIndex, "ERROR PUBLISHING: " + response);
		}
		currentSaveScanIndex = -1;
		currentSaveScanData = string.Empty;
	}

	private IEnumerator PostPublishMapRequest(PublishMapRequestData data, PublishMapRequestCallback callback)
	{
		UnityWebRequest request = new UnityWebRequest(serializationConfig.sharedBlocksApiBaseURL + "/api/Publish", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			GTDev.Log("PostPublishMapRequest Success: raw response: " + request.downloadHandler.text);
			try
			{
				string text = request.downloadHandler.text;
				bool success = !text.IsNullOrEmpty() && IsMapIDValid(text);
				callback?.Invoke(success, data.userdataMetadataKey, text, request.responseCode);
			}
			catch (Exception ex)
			{
				GTDev.LogError("SharedBlocksManager PostPublishMapRequest " + ex.Message);
				callback?.Invoke(success: false, data.userdataMetadataKey, null, request.responseCode);
			}
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_01db;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_01db;
				}
				flag = false;
				goto IL_01e3;
			}
			retry = true;
		}
		goto IL_021d;
		IL_021d:
		if (retry)
		{
			if (postPublishMapRetryCount < maxRetriesOnFail)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, postPublishMapRetryCount + 1));
				postPublishMapRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				publishRequestInProgress = false;
				RequestPublishMap(data.userdataMetadataKey);
			}
			else
			{
				postPublishMapRetryCount = 0;
				callback?.Invoke(success: false, data.userdataMetadataKey, string.Empty, request.responseCode);
			}
		}
		yield break;
		IL_01db:
		flag = true;
		goto IL_01e3;
		IL_01e3:
		if (flag)
		{
			retry = true;
		}
		else
		{
			callback?.Invoke(success: false, data.userdataMetadataKey, string.Empty, request.responseCode);
		}
		goto IL_021d;
	}

	public void RequestMapDataFromID(string mapID, BlocksMapRequestCallback callback)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("SharedBlocksManager RequestMapDataFromID Client Not Logged into Mothership");
			callback?.Invoke(null);
			return;
		}
		if (TryGetCachedSharedBlocksMapByMapID(mapID, out var result))
		{
			callback?.Invoke(result);
			return;
		}
		if (getMapDataFromIDInProgress)
		{
			GTDev.LogWarning("SharedBlocksManager RequestMapDataFromID Fetch already in progress");
			return;
		}
		getMapDataFromIDInProgress = true;
		StartCoroutine(GetMapDataFromID(new GetMapDataFromIDRequest
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			mapId = mapID
		}, callback));
	}

	private IEnumerator GetMapDataFromID(GetMapDataFromIDRequest data, BlocksMapRequestCallback callback)
	{
		UnityWebRequest request = new UnityWebRequest(serializationConfig.sharedBlocksApiBaseURL + "/api/GetMapData", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			string text = request.downloadHandler.text;
			GetMapDataFromIDComplete(data.mapId, text, callback);
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0149;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0149;
				}
				flag = false;
				goto IL_0151;
			}
			retry = true;
		}
		goto IL_0176;
		IL_0149:
		flag = true;
		goto IL_0151;
		IL_0176:
		if (retry)
		{
			if (getMapDataFromIDRetryCount < maxRetriesOnFail)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, getMapDataFromIDRetryCount + 1));
				getMapDataFromIDRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				getMapDataFromIDInProgress = false;
				RequestMapDataFromID(data.mapId, callback);
			}
			else
			{
				getMapDataFromIDRetryCount = 0;
				GetMapDataFromIDComplete(data.mapId, null, callback);
			}
		}
		yield break;
		IL_0151:
		if (flag)
		{
			retry = true;
		}
		else
		{
			GetMapDataFromIDComplete(data.mapId, null, callback);
		}
		goto IL_0176;
	}

	private void GetMapDataFromIDComplete(string mapID, [CanBeNull] string response, BlocksMapRequestCallback callback)
	{
		getMapDataFromIDInProgress = false;
		if (response == null)
		{
			callback?.Invoke(null);
			return;
		}
		SharedBlocksMap sharedBlocksMap = new SharedBlocksMap
		{
			MapID = mapID,
			MapData = response
		};
		AddMapToResponseCache(sharedBlocksMap);
		callback?.Invoke(sharedBlocksMap);
	}

	public bool RequestGetTopMaps(int pageNum, int pageSize, string sort)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("SharedBlocksManager RequestFetchPopularBlocksMaps Client Not Logged into Mothership");
			return false;
		}
		if (getTopMapsInProgress)
		{
			GTDev.LogWarning("SharedBlocksManager RequestFetchPopularBlocksMaps already in progress");
			return false;
		}
		getTopMapsInProgress = true;
		lastGetTopMapsTime = Time.realtimeSinceStartupAsDouble;
		StartCoroutine(GetTopMaps(new GetMapsRequest
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			page = pageNum,
			pageSize = pageSize,
			sort = sort,
			ShowInactive = false
		}, GetTopMapsComplete));
		return true;
	}

	private IEnumerator GetTopMaps(GetMapsRequest data, Action<List<SharedBlocksMapMetaData>> callback)
	{
		UnityWebRequest request = new UnityWebRequest(serializationConfig.sharedBlocksApiBaseURL + "/api/GetMaps", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			try
			{
				List<SharedBlocksMapMetaData> obj = JsonConvert.DeserializeObject<List<SharedBlocksMapMetaData>>(request.downloadHandler.text);
				callback?.Invoke(obj);
			}
			catch (Exception)
			{
				callback?.Invoke(null);
			}
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0160;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0160;
				}
				flag = false;
				goto IL_0168;
			}
			retry = true;
		}
		goto IL_0187;
		IL_0187:
		if (retry)
		{
			if (getTopMapsRetryCount < maxRetriesOnFail)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, getTopMapsRetryCount + 1));
				getTopMapsRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				getTopMapsInProgress = false;
				RequestGetTopMaps(data.page, data.pageSize, data.sort);
			}
			else
			{
				getTopMapsRetryCount = 0;
				callback?.Invoke(null);
			}
		}
		yield break;
		IL_0160:
		flag = true;
		goto IL_0168;
		IL_0168:
		if (flag)
		{
			retry = true;
		}
		else
		{
			callback?.Invoke(null);
		}
		goto IL_0187;
	}

	private void GetTopMapsComplete([CanBeNull] List<SharedBlocksMapMetaData> maps)
	{
		getTopMapsInProgress = false;
		if (maps != null)
		{
			latestPopularMaps.Clear();
			foreach (SharedBlocksMapMetaData map in maps)
			{
				if (map != null && IsMapIDValid(map.mapId))
				{
					DateTime createTime = DateTime.MinValue;
					DateTime updateTime = DateTime.MinValue;
					try
					{
						createTime = DateTime.Parse(map.createdTime);
						updateTime = DateTime.Parse(map.updatedTime);
					}
					catch (Exception ex)
					{
						GTDev.LogWarning("SharedBlocksManager GetTopMaps bad update or create time" + ex.Message);
					}
					SharedBlocksMap item = new SharedBlocksMap
					{
						MapID = map.mapId,
						CreatorID = null,
						CreatorNickName = map.nickname,
						CreateTime = createTime,
						UpdateTime = updateTime,
						MapData = null
					};
					latestPopularMaps.Add(item);
				}
			}
			hasCachedTopMaps = true;
			this.OnGetPopularMapsComplete?.Invoke(obj: true);
		}
		else
		{
			this.OnGetPopularMapsComplete?.Invoke(obj: false);
		}
	}

	private void RequestUpdateMapActive(string userMetadataKey, bool active)
	{
		if (!MothershipClientContext.IsClientLoggedIn())
		{
			GTDev.LogWarning("SharedBlocksManager RequestUpdateMapActive Client Not Logged into Mothership");
			return;
		}
		if (updateMapActiveInProgress)
		{
			GTDev.LogWarning("SharedBlocksManager RequestUpdateMapActive already in progress");
			return;
		}
		updateMapActiveInProgress = true;
		StartCoroutine(PostUpdateMapActive(new UpdateMapActiveRequest
		{
			mothershipId = MothershipClientContext.MothershipId,
			mothershipToken = MothershipClientContext.Token,
			mothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			userdataMetadataKey = userMetadataKey,
			setActive = active
		}, OnUpdatedMapActiveComplete));
	}

	private IEnumerator PostUpdateMapActive(UpdateMapActiveRequest data, Action<bool> callback)
	{
		UnityWebRequest request = new UnityWebRequest(serializationConfig.sharedBlocksApiBaseURL + "/api/UpdateMapActive", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		bool flag;
		if (request.result == UnityWebRequest.Result.Success)
		{
			callback?.Invoke(obj: true);
		}
		else
		{
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_012d;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_012d;
				}
				flag = false;
				goto IL_0135;
			}
			retry = true;
		}
		goto IL_0154;
		IL_012d:
		flag = true;
		goto IL_0135;
		IL_0154:
		if (retry)
		{
			if (updateMapActiveRetryCount < maxRetriesOnFail)
			{
				float time = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, updateMapActiveRetryCount + 1));
				updateMapActiveRetryCount++;
				yield return new WaitForSecondsRealtime(time);
				updateMapActiveInProgress = false;
				RequestUpdateMapActive(data.userdataMetadataKey, data.setActive);
			}
			else
			{
				updateMapActiveRetryCount = 0;
				callback?.Invoke(obj: false);
			}
		}
		yield break;
		IL_0135:
		if (flag)
		{
			retry = true;
		}
		else
		{
			callback?.Invoke(obj: false);
		}
		goto IL_0154;
	}

	private void OnUpdatedMapActiveComplete(bool success)
	{
		updateMapActiveInProgress = false;
	}

	private async Task WaitForPlayfabSessionToken()
	{
		while (!PlayFabAuthenticator.instance || PlayFabAuthenticator.instance.GetPlayFabPlayerId().IsNullOrEmpty() || PlayFabAuthenticator.instance.GetPlayFabSessionTicket().IsNullOrEmpty() || PlayFabAuthenticator.instance.userID.IsNullOrEmpty())
		{
			await Task.Yield();
			await Task.Delay(1000);
		}
	}

	public void RequestTableConfiguration()
	{
		if (fetchedTableConfig)
		{
			this.OnGetTableConfiguration?.Invoke(tableConfigResponse);
		}
	}

	private void FetchConfigurationFromTitleData()
	{
		PlayFabTitleDataCache.Instance.GetTitleData(serializationConfig.tableConfigurationKey, OnGetConfigurationSuccess, OnGetConfigurationFail);
	}

	private void OnGetConfigurationSuccess(string dataRecord)
	{
		GTDev.Log("SharedBlocksManager OnGetConfigurationSuccess");
		tableConfigResponse = dataRecord;
		fetchedTableConfig = true;
		this.OnGetTableConfiguration?.Invoke(tableConfigResponse);
	}

	private void OnGetConfigurationFail(PlayFabError error)
	{
		GTDev.LogWarning("SharedBlocksManager OnGetConfigurationFail " + error);
		if (fetchTableConfigRetryCount < maxRetriesOnFail)
		{
			float waitTime = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, fetchTableConfigRetryCount + 1));
			fetchTableConfigRetryCount++;
			StartCoroutine(RetryAfterWaitTime(waitTime, FetchConfigurationFromTitleData));
		}
		else
		{
			tableConfigResponse = string.Empty;
			fetchedTableConfig = true;
			this.OnGetTableConfiguration?.Invoke(tableConfigResponse);
		}
	}

	private IEnumerator RetryAfterWaitTime(float waitTime, Action function)
	{
		yield return new WaitForSecondsRealtime(waitTime);
		function?.Invoke();
	}

	public void FetchTitleDataBuild()
	{
		if (fetchTitleDataBuildComplete)
		{
			this.OnGetTitleDataBuildComplete?.Invoke(titleDataBuildCache);
		}
		else if (!fetchTitleDataBuildInProgress)
		{
			fetchTitleDataBuildInProgress = true;
			PlayFabTitleDataCache.Instance.GetTitleData(serializationConfig.titleDataKey, OnGetTitleDataBuildSuccess, OnGetTitleDataBuildFail);
		}
	}

	private void OnGetTitleDataBuildSuccess(string dataRecord)
	{
		fetchTitleDataBuildInProgress = false;
		GTDev.Log("SharedBlocksManager OnGetTitleDataBuildSuccess");
		if (!dataRecord.IsNullOrEmpty())
		{
			titleDataBuildCache = dataRecord;
			fetchTitleDataBuildComplete = true;
			this.OnGetTitleDataBuildComplete?.Invoke(titleDataBuildCache);
		}
		else
		{
			titleDataBuildCache = string.Empty;
			fetchTitleDataBuildComplete = true;
			this.OnGetTitleDataBuildComplete?.Invoke(titleDataBuildCache);
		}
	}

	private void OnGetTitleDataBuildFail(PlayFabError error)
	{
		fetchTitleDataBuildInProgress = false;
		GTDev.LogWarning("SharedBlocksManager FetchTitleDataBuildFail " + error);
		if (fetchTitleDataRetryCount < maxRetriesOnFail)
		{
			float waitTime = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, fetchTitleDataRetryCount + 1));
			fetchTitleDataRetryCount++;
			StartCoroutine(RetryAfterWaitTime(waitTime, FetchTitleDataBuild));
		}
		else
		{
			titleDataBuildCache = string.Empty;
			fetchTitleDataBuildComplete = true;
			this.OnGetTitleDataBuildComplete?.Invoke(titleDataBuildCache);
		}
	}

	private string GetPlayfabKeyForSlot(int slot)
	{
		return serializationConfig.playfabScanKey + slot.ToString("D2");
	}

	private string GetPlayfabSlotTimeKey(int slot)
	{
		return serializationConfig.playfabScanKey + slot.ToString("D2") + serializationConfig.timeAppend;
	}

	private void GetPlayfabLastSaveTime()
	{
		if (hasQueriedSaveTime)
		{
			SharedBlocksManager.OnSaveTimeUpdated?.Invoke();
			return;
		}
		PlayFab.ClientModels.GetUserDataRequest request = new PlayFab.ClientModels.GetUserDataRequest
		{
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			Keys = saveDateKeys
		};
		try
		{
			PlayFabClientAPI.GetUserData(request, OnGetLastSaveTimeSuccess, OnGetLastSaveTimeFailure);
		}
		catch (PlayFabException ex)
		{
			OnGetLastSaveTimeFailure(new PlayFabError
			{
				Error = PlayFabErrorCode.Unknown,
				ErrorMessage = ex.Message
			});
		}
		hasQueriedSaveTime = true;
	}

	private void OnGetLastSaveTimeSuccess(GetUserDataResult result)
	{
		bool flag = false;
		for (int i = 0; i < BuilderScanKiosk.NUM_SAVE_SLOTS; i++)
		{
			if (result.Data.TryGetValue(GetPlayfabSlotTimeKey(i), out var value))
			{
				flag = true;
				DateTime lastUpdated = value.LastUpdated;
				SetPublishTimeForSlot(i, lastUpdated + DateTimeOffset.Now.Offset);
			}
		}
		if (flag)
		{
			SaveLocalMapIdsToPlayerPrefs();
		}
		SharedBlocksManager.OnSaveTimeUpdated?.Invoke();
	}

	private void OnGetLastSaveTimeFailure(PlayFabError error)
	{
		string text = error?.ErrorMessage ?? "Null";
		GTDev.LogError("SharedBlocksManager GetLastSaveTimeFailure " + text);
	}

	private void FetchBuildFromPlayfab()
	{
		if (hasPulledPrivateScanPlayfab[currentGetScanIndex])
		{
			this.OnFetchPrivateScanComplete?.Invoke(currentGetScanIndex, arg2: true);
			currentGetScanIndex = -1;
			getScanInProgress = false;
		}
		else
		{
			PlayFab.ClientModels.GetUserDataRequest request = new PlayFab.ClientModels.GetUserDataRequest
			{
				PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
				Keys = new List<string> { GetPlayfabKeyForSlot(currentGetScanIndex) }
			};
			StartCoroutine(SendPlayfabUserDataRequest(request, OnFetchBuildFromPlayfabSuccess, OnFetchBuildFromPlayfabFail));
		}
	}

	private IEnumerator SendPlayfabUserDataRequest(PlayFab.ClientModels.GetUserDataRequest request, Action<GetUserDataResult> resultCallback, Action<PlayFabError> errorCallback)
	{
		while (!PlayFabSettings.staticPlayer.IsClientLoggedIn())
		{
			yield return new WaitForSecondsRealtime(5f);
		}
		try
		{
			PlayFabClientAPI.GetUserData(request, resultCallback, errorCallback);
		}
		catch (PlayFabException ex)
		{
			errorCallback?.Invoke(new PlayFabError
			{
				Error = PlayFabErrorCode.Unknown,
				ErrorMessage = ex.Message
			});
		}
	}

	private void OnFetchBuildFromPlayfabSuccess(GetUserDataResult result)
	{
		getScanInProgress = false;
		GTDev.Log("SharedBlocksManager OnFetchBuildsFromPlayfabSuccess");
		if (result != null && result.Data != null && result.Data.TryGetValue(GetPlayfabKeyForSlot(currentGetScanIndex), out var value))
		{
			privateScanDataCache[currentGetScanIndex] = value.Value;
			hasPulledPrivateScanPlayfab[currentGetScanIndex] = true;
			if (!value.Value.IsNullOrEmpty())
			{
				RequestSavePrivateScan(currentGetScanIndex, value.Value);
			}
		}
		else
		{
			privateScanDataCache[currentGetScanIndex] = string.Empty;
			hasPulledPrivateScanPlayfab[currentGetScanIndex] = true;
		}
		this.OnFetchPrivateScanComplete?.Invoke(currentGetScanIndex, arg2: true);
		currentGetScanIndex = -1;
	}

	private void OnFetchBuildFromPlayfabFail(PlayFabError error)
	{
		GTDev.LogWarning("SharedBlocksManager OnFetchBuildsFromPlayfabFail " + (error?.ErrorMessage ?? "Null"));
		if (error != null && error.Error == PlayFabErrorCode.ConnectionError && fetchPlayfabBuildsRetryCount < maxRetriesOnFail)
		{
			float waitTime = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, fetchPlayfabBuildsRetryCount + 1));
			fetchPlayfabBuildsRetryCount++;
			StartCoroutine(RetryAfterWaitTime(waitTime, FetchBuildFromPlayfab));
		}
		else
		{
			privateScanDataCache[currentGetScanIndex] = string.Empty;
			hasPulledPrivateScanPlayfab[currentGetScanIndex] = true;
			getScanInProgress = false;
			this.OnFetchPrivateScanComplete?.Invoke(currentGetScanIndex, arg2: false);
			currentGetScanIndex = -1;
		}
	}

	public bool TryGetRandomPopularMap(out SharedBlocksMap map)
	{
		map = null;
		if (latestPopularMaps == null || latestPopularMaps.Count == 0)
		{
			return false;
		}
		GTDev.Log($"[SharedBlocksManager] Selecting from {latestPopularMaps.Count} popular maps");
		for (int i = 0; i < Mathf.Min(10, latestPopularMaps.Count); i++)
		{
			int num = UnityEngine.Random.Range(0, latestPopularMaps.Count);
			GTDev.Log($"[SharedBlocksManager] Random pick index: {num}");
			SharedBlocksMap sharedBlocksMap = latestPopularMaps[num];
			if (sharedBlocksMap != null && IsMapIDValid(sharedBlocksMap.MapID))
			{
				map = sharedBlocksMap;
				return true;
			}
		}
		for (int j = 0; j < latestPopularMaps.Count; j++)
		{
			SharedBlocksMap sharedBlocksMap2 = latestPopularMaps[j];
			if (sharedBlocksMap2 != null && IsMapIDValid(sharedBlocksMap2.MapID))
			{
				map = sharedBlocksMap2;
				return true;
			}
		}
		return false;
	}

	private async Task WaitForMothership()
	{
		while (!MothershipClientContext.IsClientLoggedIn())
		{
			await Task.Yield();
			await Task.Delay(1000);
		}
	}

	public void RequestSavePrivateScan(int scanIndex, string scanData)
	{
		if (scanIndex < 0 || scanIndex >= serializationConfig.scanSlotMothershipKeys.Count)
		{
			GTDev.LogError($"SharedBlocksManager RequestSaveScanToMothership: scan index {scanIndex} out of bounds");
			return;
		}
		currentSaveScanIndex = scanIndex;
		currentSaveScanData = scanData;
		if (!hasPulledPrivateScanMothership[scanIndex])
		{
			PullMothershipPrivateScanThenPush(scanIndex);
			return;
		}
		privateScanDataCache[scanIndex] = scanData;
		RequestSetMothershipUserData(serializationConfig.scanSlotMothershipKeys[scanIndex], scanData);
	}

	private void PullMothershipPrivateScanThenPush(int scanIndex)
	{
		if (getScanInProgress && currentGetScanIndex != scanIndex)
		{
			GTDev.LogWarning("SharedBLocksManager PullMothershipPrivateScanThenPush GetScan in progress");
			this.OnSavePrivateScanFailed?.Invoke(scanIndex, "ERROR SAVING: BUSY");
			currentSaveScanIndex = -1;
			currentSaveScanData = string.Empty;
		}
		else
		{
			OnFetchPrivateScanComplete += PushMothershipPrivateScan;
			RequestFetchPrivateScan(scanIndex);
		}
	}

	private void PushMothershipPrivateScan(int scan, bool success)
	{
		if (scan == currentSaveScanIndex)
		{
			OnFetchPrivateScanComplete -= PushMothershipPrivateScan;
			privateScanDataCache[currentSaveScanIndex] = currentSaveScanData;
			RequestSetMothershipUserData(serializationConfig.scanSlotMothershipKeys[currentSaveScanIndex], currentSaveScanData);
		}
	}

	private void RequestSetMothershipUserData(string keyName, string value)
	{
		if (saveScanInProgress)
		{
			Debug.LogError("SharedBlocksManager RequestSetMothershipUserData: request already in progress");
			return;
		}
		saveScanInProgress = true;
		try
		{
			if (!MothershipClientApiUnity.SetUserDataValue(keyName, value, OnSetMothershipUserDataSuccess, OnSetMothershipUserDataFail))
			{
				Debug.LogError("SharedBlocksManager RequestSetMothershipUserData: SetUserDataValue Fail");
				OnSetMothershipDataComplete(success: false);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("SharedBlocksManager RequestSetMothershipUserData: exception " + ex.Message);
			OnSetMothershipDataComplete(success: false);
		}
	}

	private void OnSetMothershipUserDataSuccess(SetUserDataResponse response)
	{
		GTDev.Log("SharedBlocksManager OnSetMothershipUserDataSuccess");
		OnSetMothershipDataComplete(success: true);
		response.Dispose();
	}

	private void OnSetMothershipUserDataFail(MothershipError error, int status)
	{
		string text = ((error == null) ? status.ToString() : error.Message);
		GTDev.LogError("SharedBlocksManager OnSetMothershipUserDataFail: " + text);
		OnSetMothershipDataComplete(success: false);
		error?.Dispose();
	}

	private void OnSetMothershipDataComplete(bool success)
	{
		saveScanInProgress = false;
		if (BuilderScanKiosk.IsSaveSlotValid(currentSaveScanIndex))
		{
			if (success)
			{
				RequestPublishMap(serializationConfig.scanSlotMothershipKeys[currentSaveScanIndex]);
				return;
			}
			this.OnSavePrivateScanFailed?.Invoke(currentSaveScanIndex, "ERROR SAVING");
			currentSaveScanIndex = -1;
			currentSaveScanData = string.Empty;
		}
		else
		{
			currentSaveScanIndex = -1;
			currentSaveScanData = string.Empty;
		}
	}

	public bool TryGetPrivateScanResponse(int scanSlot, out string scanData)
	{
		if (scanSlot < 0 || scanSlot >= privateScanDataCache.Length || !hasPulledPrivateScanMothership[scanSlot])
		{
			scanData = string.Empty;
			return false;
		}
		scanData = privateScanDataCache[scanSlot];
		return true;
	}

	public void RequestFetchPrivateScan(int slot)
	{
		if (!BuilderScanKiosk.IsSaveSlotValid(slot))
		{
			GTDev.LogError($"SharedBlocksManager RequestSaveScan: slot {slot} OOB");
			slot = Mathf.Clamp(slot, 0, BuilderScanKiosk.NUM_SAVE_SLOTS - 1);
		}
		if (hasPulledPrivateScanMothership[slot])
		{
			bool arg = privateScanDataCache[slot].Length > 0;
			this.OnFetchPrivateScanComplete?.Invoke(slot, arg);
			return;
		}
		if (getScanInProgress)
		{
			Debug.LogError("SharedBlocksManager RequestFetchPrivateScan: request already in progress");
			if (slot != currentGetScanIndex)
			{
				this.OnFetchPrivateScanComplete?.Invoke(slot, arg2: false);
			}
			return;
		}
		currentGetScanIndex = slot;
		getScanInProgress = true;
		try
		{
			if (!MothershipClientApiUnity.GetUserDataValue(serializationConfig.scanSlotMothershipKeys[slot], OnGetMothershipPrivateScanSuccess, OnGetMothershipPrivateScanFail))
			{
				Debug.LogError("SharedBlocksManager RequestFetchPrivateScan failed ");
				currentGetScanIndex = -1;
				getScanInProgress = false;
				this.OnFetchPrivateScanComplete?.Invoke(slot, arg2: false);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("SharedBlocksManager RequestFetchPrivateScan exception " + ex.Message);
			currentGetScanIndex = -1;
			getScanInProgress = false;
			this.OnFetchPrivateScanComplete?.Invoke(slot, arg2: false);
		}
	}

	private void OnGetMothershipPrivateScanSuccess(MothershipUserData response)
	{
		GTDev.Log("SharedBlocksManager OnGetMothershipPrivateScanSuccess");
		bool flag = response != null && response.value != null && response.value.Length > 0;
		int arg = currentGetScanIndex;
		if (response != null)
		{
			privateScanDataCache[currentGetScanIndex] = response.value;
			hasPulledPrivateScanMothership[currentGetScanIndex] = true;
			if (flag)
			{
				LocalPublishInfo publishInfoForSlot = GetPublishInfoForSlot(currentGetScanIndex);
				if (publishInfoForSlot.mapID != null)
				{
					SharedBlocksMap map = new SharedBlocksMap
					{
						MapID = publishInfoForSlot.mapID,
						MapData = privateScanDataCache[currentGetScanIndex],
						CreatorNickName = GorillaTagger.Instance.offlineVRRig.playerNameVisible,
						UpdateTime = DateTime.Now
					};
					AddMapToResponseCache(map);
				}
				currentGetScanIndex = -1;
				getScanInProgress = false;
				this.OnFetchPrivateScanComplete?.Invoke(arg, arg2: true);
			}
			else
			{
				FetchBuildFromPlayfab();
			}
		}
		else
		{
			currentGetScanIndex = -1;
			getScanInProgress = false;
			this.OnFetchPrivateScanComplete?.Invoke(arg, arg2: false);
		}
		response?.Dispose();
	}

	private void OnGetMothershipPrivateScanFail(MothershipError error, int status)
	{
		string text = ((error == null) ? status.ToString() : error.Message);
		GTDev.LogError("SharedBlocksManager OnGetMothershipPrivateScanFail: " + text);
		int arg = currentGetScanIndex;
		if (BuilderScanKiosk.IsSaveSlotValid(currentGetScanIndex))
		{
			privateScanDataCache[currentGetScanIndex] = string.Empty;
			hasPulledPrivateScanMothership[currentGetScanIndex] = true;
		}
		getScanInProgress = false;
		currentGetScanIndex = -1;
		this.OnFetchPrivateScanComplete?.Invoke(arg, arg2: false);
		error?.Dispose();
	}
}
