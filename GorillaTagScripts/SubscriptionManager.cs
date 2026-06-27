using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Oculus.Platform;
using Oculus.Platform.Models;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

namespace GorillaTagScripts;

public class SubscriptionManager : MonoBehaviour
{
	public enum SubscriptionStatus
	{
		Active,
		Inactive,
		Unknown
	}

	public enum SubscriptionTerm
	{
		MONTHLY,
		QUARTERLY,
		SEMIANNUAL,
		ANNUAL
	}

	public enum SubscriptionFeatures
	{
		GoldenName,
		IOBT,
		HandTracking,
		SubscriptionFeatureCount
	}

	public struct SubscriptionDetails
	{
		public bool active;

		public int daysAccrued;

		public bool[] subscriptionFeatureSettings;

		public int tier;

		public DateTime subscriptionActiveUntilDate;

		public bool autoRenew;

		public int autoRenewMonths;
	}

	[Serializable]
	private class MothershipSubscription
	{
		public string SubscriptionId;

		public DateTimeOffset EarliestStartDate;

		public DateTimeOffset CurrentStartDate;

		public DateTimeOffset MostRecentBillingCycleStartDate;

		public DateTimeOffset MostRecentBillingCycleEndDate;

		public int TotalLifetimeSeconds;

		public bool IsActive;

		public bool IsCancelling;

		public string Sku;

		public string PlayerId;

		public string TrialType;

		public string ExternalServiceName;

		public string ExternalSubscriptionId;

		public string SubscriptionCatalogItemId;
	}

	[Serializable]
	private class GrantedSubscriptionBenefit
	{
		public string BenefitId;

		public DateTimeOffset GrantedTime;

		public string PlayFabItemId;
	}

	[Serializable]
	private class GetMySubscriptionsAndTheirBenefitsRequest
	{
		public bool Refresh;

		public bool? SkipBenefitsCheck;

		public bool? SkipSharedGroupDataUpdate;

		public string MothershipId;

		public string MothershipToken;

		public string MothershipEnvId;

		public string MothershipDeploymentId;
	}

	[Serializable]
	private class GetMySubscriptionsAndTheirBenefitsResponse
	{
		public List<MothershipSubscription> Subscriptions;

		public Dictionary<string, List<GrantedSubscriptionBenefit>> PreviouslyGrantedBenefitsBySubscriptionSku;

		public Dictionary<string, List<GrantedSubscriptionBenefit>> NewlyGrantedBenefitsBySubscriptionSku;

		public bool? SharedGroupDataUpdateSucceeded;
	}

	public const string FAN_CLUB_BASE_SKU = "fan_club";

	public const string FAN_CLUB_STEAM_SKU = "40494";

	public const string SUBSCRIBER_NAME_COLOR_HEX = "#ffc600";

	public static Color SUBSCRIBER_NAME_COLOR = Color.gold;

	public const int PERF_SEND_RATE = 20;

	public static int DEFAULT_SEND_RATE = 30;

	public static int PERF_CHANGE_ROOMSIZE = 10;

	private static SubscriptionManager Instance;

	public static Action OnSubscriptionData;

	public static Action OnLocalSubscriptionData;

	private Dictionary<NetPlayer, SubscriptionDetails> subData = new Dictionary<NetPlayer, SubscriptionDetails>();

	private Dictionary<VRRig, NetPlayer> rigs = new Dictionary<VRRig, NetPlayer>();

	private static SubscriptionDetails localSubscriptionDetails;

	private static bool _localSubscriptionDataInitialized;

	public const string SUB_PREFIX = "SMKEYPREFIX";

	public static string[] SUBS_KEYS;

	private static int maxRetries = 3;

	private int attempts;

	private static Dictionary<string, int> subSettings = new Dictionary<string, int>();

	public static bool LocalSubscriptionDataInitialized => _localSubscriptionDataInitialized;

	public static bool SubsOnlyMatchmaking
	{
		get
		{
			return PlayerPrefs.GetInt("subsOnlyMatchmaking") == 1;
		}
		set
		{
			PlayerPrefs.SetInt("subsOnlyMatchmaking", value ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	public static string GetSubsFeatureKey(SubscriptionFeatures feature)
	{
		return SUBS_KEYS[(int)feature];
	}

	private async void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			SUBS_KEYS = new string[3];
			for (int i = 0; i < SUBS_KEYS.Length; i++)
			{
				SubscriptionFeatures subscriptionFeatures = (SubscriptionFeatures)i;
				_ = string.Empty;
				string text = subscriptionFeatures switch
				{
					SubscriptionFeatures.GoldenName => "GOLDEN_NAME_KEY", 
					SubscriptionFeatures.IOBT => "IOBT_ENABLE_KEY", 
					_ => subscriptionFeatures.ToString().ToUpper() + "_ENABLE_KEY", 
				};
				SUBS_KEYS[i] = "SMKEYPREFIX" + text;
			}
			while (NetworkSystem.Instance == null && NetworkSystem.Instance.AllNetPlayers.Length == 0)
			{
				await Awaitable.WaitForSecondsAsync(0.1f);
			}
			DEFAULT_SEND_RATE = PhotonNetwork.SendRate;
			UpdatePlayerSubsDetails(NetworkSystem.Instance.LocalPlayer);
			ForceRecheck();
		}
		else
		{
			Debug.LogError("Failed attempt to instantiate a second SubscriptionManager. Don't do that!");
			UnityEngine.Object.DestroyImmediate(base.gameObject);
		}
	}

	protected void OnEnable()
	{
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerJoinedRoom);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeft);
		InitializePersonalSubscriptionData();
	}

	public static async void InitializePersonalSubscriptionData()
	{
		while (!KIDManager.InitialisationComplete)
		{
			await Awaitable.WaitForSecondsAsync(0.5f);
		}
		while (!MothershipClientApiUnity.IsClientLoggedIn())
		{
			await Awaitable.WaitForSecondsAsync(0.5f);
		}
		GetMySubscriptionsAndTheirBenefitsRequest requestBody = new GetMySubscriptionsAndTheirBenefitsRequest
		{
			Refresh = true,
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId
		};
		int retryCount = 0;
		while (true)
		{
			using UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.IapApiBaseUrl + "/api/GetMySubscriptionsAndTheirBenefits", "POST");
			string s = JsonConvert.SerializeObject(requestBody);
			request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(s));
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");
			request.timeout = 15;
			await request.SendWebRequest();
			if (request.result == UnityWebRequest.Result.Success)
			{
				GetMySubscriptionsAndTheirBenefitsResponse getMySubscriptionsAndTheirBenefitsResponse;
				try
				{
					getMySubscriptionsAndTheirBenefitsResponse = JsonConvert.DeserializeObject<GetMySubscriptionsAndTheirBenefitsResponse>(request.downloadHandler.text);
				}
				catch (Exception)
				{
					Debug.LogError("[SubscriptionResults] Error deserializing subscription data: " + request.downloadHandler.text);
					break;
				}
				if (getMySubscriptionsAndTheirBenefitsResponse != null && getMySubscriptionsAndTheirBenefitsResponse.Subscriptions != null)
				{
					for (int i = 0; i < getMySubscriptionsAndTheirBenefitsResponse.Subscriptions.Count; i++)
					{
						if (!(getMySubscriptionsAndTheirBenefitsResponse.Subscriptions[i].Sku != "fan_club"))
						{
							MothershipSubscription mothershipSubscription = getMySubscriptionsAndTheirBenefitsResponse.Subscriptions[i];
							int daysAccrued = mothershipSubscription.TotalLifetimeSeconds / 86400;
							DateTime localDateTime = mothershipSubscription.MostRecentBillingCycleStartDate.LocalDateTime;
							DateTime localDateTime2 = mothershipSubscription.MostRecentBillingCycleEndDate.LocalDateTime;
							int autoRenewMonths = Mathf.RoundToInt((float)(localDateTime2 - localDateTime).Days / 30f);
							localSubscriptionDetails = new SubscriptionDetails
							{
								active = mothershipSubscription.IsActive,
								daysAccrued = daysAccrued,
								tier = 1,
								autoRenew = !mothershipSubscription.IsCancelling,
								autoRenewMonths = autoRenewMonths,
								subscriptionActiveUntilDate = localDateTime2
							};
							Instance.subData[NetworkSystem.Instance.LocalPlayer] = localSubscriptionDetails;
							_localSubscriptionDataInitialized = true;
							OnLocalSubscriptionData?.Invoke();
							return;
						}
					}
				}
				localSubscriptionDetails = default(SubscriptionDetails);
				Instance.subData[NetworkSystem.Instance.LocalPlayer] = localSubscriptionDetails;
				_localSubscriptionDataInitialized = true;
				OnLocalSubscriptionData?.Invoke();
				break;
			}
			Debug.LogError($"[SubscriptionResults] Error fetching subscription data: {request.downloadHandler.text} (Status: {request.responseCode})");
			bool flag = request.result != UnityWebRequest.Result.ProtocolError;
			bool flag2;
			if (!flag)
			{
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_047e;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_047e;
				}
				flag2 = false;
				goto IL_0486;
			}
			goto IL_048a;
			IL_047e:
			flag2 = true;
			goto IL_0486;
			IL_048a:
			if (flag)
			{
				if (retryCount < maxRetries)
				{
					int num = retryCount + 1;
					retryCount = num;
					await Awaitable.WaitForSecondsAsync(UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, num)));
					continue;
				}
				Debug.LogError("[SubscriptionResults] Maximum retries attempted");
				break;
			}
			break;
			IL_0486:
			flag = flag2;
			goto IL_048a;
		}
	}

	protected void OnDisable()
	{
		RoomSystem.PlayerJoinedEvent -= new Action<NetPlayer>(OnPlayerJoinedRoom);
		RoomSystem.PlayerLeftEvent -= new Action<NetPlayer>(OnPlayerLeft);
	}

	public static SubscriptionDetails GetSubscriptionDetails(VRRig rig)
	{
		if (Instance == null || !Instance.rigs.ContainsKey(rig))
		{
			return default(SubscriptionDetails);
		}
		return GetSubscriptionDetails(Instance.rigs[rig]);
	}

	public static SubscriptionDetails GetSubscriptionDetails(NetPlayer np)
	{
		if (Instance == null || !Instance.subData.TryGetValue(np, out var value))
		{
			return default(SubscriptionDetails);
		}
		return value;
	}

	public static bool IsPlayerSubscribed(VRRig rig)
	{
		return GetSubscriptionDetails(rig).active;
	}

	public static bool IsPlayerSubscribed(NetPlayer np)
	{
		return GetSubscriptionDetails(np).active;
	}

	public static SubscriptionDetails GetSubscriptionDetails()
	{
		if (Instance == null || !Instance.subData.TryGetValue(VRRig.LocalRig.creator, out var value))
		{
			return default(SubscriptionDetails);
		}
		return value;
	}

	public static SubscriptionStatus LocalSubscriptionStatus()
	{
		if (Instance == null || !Instance.subData.TryGetValue(VRRig.LocalRig.creator, out var value))
		{
			return SubscriptionStatus.Unknown;
		}
		if (!value.active)
		{
			return SubscriptionStatus.Inactive;
		}
		return SubscriptionStatus.Active;
	}

	public static SubscriptionDetails LocalSubscriptionDetails()
	{
		return localSubscriptionDetails;
	}

	public static bool IsLocalSubscribed()
	{
		if (Instance == null || VRRig.LocalRig == null || VRRig.LocalRig.creator == null || !Instance.subData.TryGetValue(VRRig.LocalRig.creator, out var value))
		{
			return false;
		}
		return value.active;
	}

	public static void ForceRecheck()
	{
		Instance.OnPlayerJoinedRoom(null);
	}

	private void OnPlayerJoinedRoom(NetPlayer npl)
	{
		if (OnSubscriptionData != null)
		{
			OnSubscriptionData();
		}
		if (NetworkSystem.Instance.AllNetPlayers.Length > PERF_CHANGE_ROOMSIZE)
		{
			GorillaTagger.Instance.ToggleForcedPerformanceRefresh();
			PhotonNetwork.SendRate = 20;
		}
	}

	private void UpdatePlayerSubsDetails(NetPlayer player, bool? isSubscribed = null, int? daysAccrued = null)
	{
		if (player != null)
		{
			if (VRRigCache.Instance.TryGetVrrig(player.ActorNumber, out var playerRig))
			{
				rigs[playerRig.Rig] = player;
			}
			if (player == NetworkSystem.Instance.LocalPlayer)
			{
				_ = 1;
			}
			else
				_ = player == VRRig.LocalRig.creator;
			bool flag = false;
			int daysAccrued2 = 0;
			int tier = 0;
			if (isSubscribed.HasValue)
			{
				flag = isSubscribed.Value;
				tier = (flag ? 1 : 0);
				daysAccrued2 = daysAccrued.GetValueOrDefault();
			}
			SubscriptionDetails value = new SubscriptionDetails
			{
				active = flag,
				tier = tier,
				daysAccrued = daysAccrued2
			};
			subData[player] = value;
		}
	}

	private void OnPlayerLeft(NetPlayer pl)
	{
		if (subData.ContainsKey(pl))
		{
			subData.Remove(pl);
		}
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		if (allNetPlayers.Length <= PERF_CHANGE_ROOMSIZE)
		{
			GorillaTagger.Instance.ToggleDefaultPerformanceRefresh();
			PhotonNetwork.SendRate = DEFAULT_SEND_RATE;
		}
		NetPlayer lowestNetPlayer = GetLowestNetPlayer(allNetPlayers);
		if (lowestNetPlayer != null && lowestNetPlayer == NetworkSystem.Instance.LocalPlayer)
		{
			byte currentRoomExpectedSize = RoomSystem.GetCurrentRoomExpectedSize();
			PhotonNetwork.CurrentRoom.MaxPlayers = currentRoomExpectedSize;
		}
	}

	private NetPlayer GetLowestNetPlayer(NetPlayer[] players)
	{
		NetPlayer result = null;
		int num = int.MaxValue;
		for (int i = 0; i < players.Length; i++)
		{
			if (players[i].ActorNumber < num)
			{
				num = players[i].ActorNumber;
				result = players[i];
			}
		}
		return result;
	}

	private void OnGetViewerPurchasesStartup(Message msg)
	{
		if (msg.IsError)
		{
			if (attempts < 3)
			{
				attempts++;
				IAP.GetViewerPurchases().OnComplete(OnGetViewerPurchasesStartup);
			}
		}
		else
		{
			if (msg.GetPurchaseList() == null || _localSubscriptionDataInitialized)
			{
				return;
			}
			bool flag = false;
			foreach (Purchase purchase in msg.GetPurchaseList())
			{
				if (purchase.Type == ProductType.SUBSCRIPTION && purchase.Sku.Contains("fan_club"))
				{
					flag = true;
					localSubscriptionDetails = new SubscriptionDetails
					{
						active = (DateTime.Now < purchase.ExpirationTime),
						subscriptionActiveUntilDate = purchase.ExpirationTime
					};
				}
			}
			if (!flag)
			{
				localSubscriptionDetails = new SubscriptionDetails
				{
					active = false
				};
			}
		}
	}

	public static void SetSubscriptionSettingValue(SubscriptionFeatures feature, int settingValue)
	{
		string subsFeatureKey = GetSubsFeatureKey(feature);
		PlayerPrefs.SetInt(subsFeatureKey, settingValue);
		subSettings[subsFeatureKey] = settingValue;
		PlayerPrefs.Save();
	}

	public static int GetSubscriptionSettingValue(SubscriptionFeatures feature)
	{
		string subsFeatureKey = GetSubsFeatureKey(feature);
		if (subSettings.TryGetValue(subsFeatureKey, out var value))
		{
			return value;
		}
		subSettings[subsFeatureKey] = PlayerPrefs.GetInt(subsFeatureKey, 1);
		return subSettings[subsFeatureKey];
	}

	public static bool GetSubscriptionSettingBool(SubscriptionFeatures feature)
	{
		return GetSubscriptionSettingValue(feature) >= 1;
	}

	public static bool IsSubscriptionFeatureAvailable(SubscriptionFeatures feature)
	{
		switch (feature)
		{
		case SubscriptionFeatures.IOBT:
		{
			if (UnityEngine.Application.platform != RuntimePlatform.Android)
			{
				return false;
			}
			OVRPlugin.SystemHeadset systemHeadsetType = OVRPlugin.GetSystemHeadsetType();
			if (systemHeadsetType != OVRPlugin.SystemHeadset.Meta_Quest_3 && systemHeadsetType != OVRPlugin.SystemHeadset.Meta_Quest_3S && systemHeadsetType != OVRPlugin.SystemHeadset.Meta_Link_Quest_3)
			{
				return systemHeadsetType == OVRPlugin.SystemHeadset.Meta_Link_Quest_3S;
			}
			return true;
		}
		case SubscriptionFeatures.HandTracking:
			if (UnityEngine.Application.platform != RuntimePlatform.Android)
			{
				return false;
			}
			return true;
		default:
			return true;
		}
	}

	public static bool CheckSubscriptionFeaturePermission(SubscriptionFeatures feature)
	{
		return feature switch
		{
			SubscriptionFeatures.IOBT => OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.BodyTracking), 
			SubscriptionFeatures.HandTracking => OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.BodyTracking), 
			_ => true, 
		};
	}

	[RuntimeInitializeOnLoadMethod]
	private static void OnLoad()
	{
	}

	public static void UpdatePlayerSubscriptionData(NetPlayer player, bool isSubscribed, int daysAccrued = 0)
	{
		if (Instance == null)
		{
			Debug.LogWarning("SubscriptionManager: Instance is null, cannot update player subscription data");
			return;
		}
		if (player == null)
		{
			Debug.LogWarning("SubscriptionManager: NetPlayer is null, cannot update subscription data");
			return;
		}
		Instance.UpdatePlayerSubsDetails(player, isSubscribed, daysAccrued);
		if (OnSubscriptionData != null)
		{
			OnSubscriptionData();
		}
	}
}
