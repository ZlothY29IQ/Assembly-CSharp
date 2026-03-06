using System;
using System.Collections;
using System.Collections.Generic;
using GorillaNetworking;
using GorillaTagScripts;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

internal class PlayerCosmeticsSystem : MonoBehaviour, ITickSystemPre
{
	[Serializable]
	public class PlayFabSubscriptionData
	{
		public string Sku;

		public bool IsActive;
	}

	public float playerLookUpCooldown = 3f;

	public float getSharedGroupDataCooldown = 0.1f;

	private float startSearchingTime = float.MinValue;

	private bool isLookingUp;

	private bool isLookingUpNew;

	private string tempCosmetics;

	private NetPlayer playerTemp;

	private RigContainer tempRC;

	private List<string> inventory;

	private const string inventoryKey = "InventoryDict";

	private static readonly string subscriptionKey = "subscriptions.fan_club";

	private static PlayerCosmeticsSystem instance;

	private static Queue<NetPlayer> playersToLookUp = new Queue<NetPlayer>(20);

	private static Dictionary<int, IUserCosmeticsCallback> userCosmeticCallback = new Dictionary<int, IUserCosmeticsCallback>(20);

	private static Dictionary<int, string> userCosmeticsWaiting = new Dictionary<int, string>(5);

	private static List<string> playerIDsList = new List<string>(20);

	private static List<int> playerActorNumberList = new List<int>(20);

	private static List<int> playersWaiting = new List<int>();

	private static TimeSince sinceLastTryOnEvent = 0f;

	private static readonly Dictionary<string, int> k_tempUnlockedCosmetics = new Dictionary<string, int>(20);

	bool ITickSystemPre.PreTickRunning { get; set; }

	private static bool nullInstance
	{
		get
		{
			if ((object)instance != null)
			{
				return !instance;
			}
			return true;
		}
	}

	public static bool TempUnlocksEnabled { get; private set; } = false;

	public static string[] TempUnlockCosmeticString { get; private set; } = Array.Empty<string>();

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			base.transform.SetParent(null, worldPositionStays: true);
			UnityEngine.Object.DontDestroyOnLoad(this);
			inventory = new List<string>();
			inventory.Add("InventoryDict");
			inventory.Add(subscriptionKey);
			NetworkSystem.Instance.OnRaiseEvent += OnNetEvent;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Start()
	{
		playerLookUpCooldown = Mathf.Max(playerLookUpCooldown, 3f);
		PlayFabTitleDataCache.Instance.GetTitleData("EnableTempCosmeticUnlocks", delegate(string data)
		{
			if (bool.TryParse(data, out var result))
			{
				TempUnlocksEnabled = result;
			}
			else
			{
				Debug.LogError("PlayerCosmeticsSystem: error parsing EnableTempCosmeticUnlocks data");
			}
		}, delegate
		{
		});
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	private void LookUpPlayerCosmetics(bool wait = false)
	{
		if (!isLookingUp)
		{
			TickSystem<object>.AddPreTickCallback(this);
			startSearchingTime = (wait ? Time.realtimeSinceStartup : float.MinValue);
			isLookingUp = true;
		}
	}

	public void PreTick()
	{
		if (playersToLookUp.Count < 1)
		{
			TickSystem<object>.RemovePreTickCallback(this);
			startSearchingTime = float.MinValue;
			isLookingUp = false;
		}
		else if (!(startSearchingTime + playerLookUpCooldown > Time.realtimeSinceStartup))
		{
			NewCosmeticsPath();
		}
	}

	private void NewCosmeticsPath()
	{
		if (!isLookingUpNew)
		{
			StartCoroutine(NewCosmeticsPathCoroutine());
		}
	}

	private IEnumerator NewCosmeticsPathCoroutine()
	{
		isLookingUpNew = true;
		NetPlayer player = null;
		playerIDsList.Clear();
		playerActorNumberList.Clear();
		while (playersToLookUp.Count > 0)
		{
			player = playersToLookUp.Dequeue();
			string item = player.ActorNumber.ToString();
			if (player.InRoom() && !playerIDsList.Contains(item))
			{
				playerIDsList.Add(player.UserId);
				playerActorNumberList.Add(player.ActorNumber);
			}
		}
		for (int i = 0; i < playerIDsList.Count; i++)
		{
			int j = i;
			PlayFabClientAPI.GetSharedGroupData(new PlayFab.ClientModels.GetSharedGroupDataRequest
			{
				Keys = inventory,
				SharedGroupId = playerIDsList[j] + "Inventory"
			}, delegate(GetSharedGroupDataResult result)
			{
				if (!NetworkSystem.Instance.InRoom)
				{
					playersWaiting.Clear();
				}
				else
				{
					bool flag = false;
					foreach (KeyValuePair<string, PlayFab.ClientModels.SharedGroupDataRecord> datum in result.Data)
					{
						if (datum.Key == "InventoryDict")
						{
							if (Utils.PlayerInRoom(playerActorNumberList[j]))
							{
								tempCosmetics = datum.Value.Value;
								if (!userCosmeticCallback.TryGetValue(playerActorNumberList[j], out var value))
								{
									userCosmeticsWaiting[playerActorNumberList[j]] = tempCosmetics;
								}
								else
								{
									value.PendingUpdate = false;
									if (!value.OnGetUserCosmetics(tempCosmetics))
									{
										playersToLookUp.Enqueue(player);
										value.PendingUpdate = true;
									}
								}
							}
						}
						else if (datum.Key == subscriptionKey)
						{
							flag = true;
							NetPlayer netPlayer = null;
							NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
							foreach (NetPlayer netPlayer2 in allNetPlayers)
							{
								if (netPlayer2.ActorNumber == playerActorNumberList[j])
								{
									netPlayer = netPlayer2;
									break;
								}
							}
							if (netPlayer != null)
							{
								bool isSubscribed = false;
								if (!string.IsNullOrEmpty(datum.Value.Value))
								{
									try
									{
										isSubscribed = JsonConvert.DeserializeObject<PlayFabSubscriptionData>(datum.Value.Value).IsActive;
									}
									catch (Exception ex)
									{
										Debug.LogError("Failed to deserialize subscription data for " + netPlayer.NickName + ": " + ex.Message);
									}
								}
								SubscriptionManager.UpdatePlayerSubscriptionData(netPlayer, isSubscribed);
							}
						}
					}
					if (!flag)
					{
						NetPlayer netPlayer3 = null;
						NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
						foreach (NetPlayer netPlayer4 in allNetPlayers)
						{
							if (netPlayer4.ActorNumber == playerActorNumberList[j])
							{
								netPlayer3 = netPlayer4;
								break;
							}
						}
						if (netPlayer3 != null)
						{
							SubscriptionManager.UpdatePlayerSubscriptionData(netPlayer3, isSubscribed: false);
						}
					}
				}
			}, delegate(PlayFabError error)
			{
				if (error.Error == PlayFabErrorCode.NotAuthenticated)
				{
					PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
				}
				else if (error.Error == PlayFabErrorCode.AccountBanned)
				{
					GorillaGameManager.ForceStopGame_DisconnectAndDestroy();
				}
			});
			yield return new WaitForSecondsRealtime(getSharedGroupDataCooldown);
		}
		isLookingUpNew = false;
	}

	private void OnNetEvent(byte code, object data, int source)
	{
		if (code == 199 && source >= 0)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(source);
			MonkeAgent.IncrementRPCCall(new PhotonMessageInfoWrapped(source, NetworkSystem.Instance.ServerTimestamp), "UpdatePlayerCosmetics");
			UpdatePlayerCosmetics(player);
		}
	}

	public static void RegisterCosmeticCallback(int playerID, IUserCosmeticsCallback callback)
	{
		userCosmeticCallback[playerID] = callback;
		if (userCosmeticsWaiting.TryGetValue(playerID, out var value))
		{
			callback.PendingUpdate = false;
			callback.OnGetUserCosmetics(value);
			userCosmeticsWaiting.Remove(playerID);
		}
	}

	public static void RemoveCosmeticCallback(int playerID)
	{
		if (userCosmeticCallback.ContainsKey(playerID))
		{
			userCosmeticCallback.Remove(playerID);
		}
	}

	public static void UpdatePlayerCosmetics(NetPlayer player)
	{
		if (player != null && !player.IsLocal)
		{
			playersToLookUp.Enqueue(player);
			if (userCosmeticCallback.TryGetValue(player.ActorNumber, out var value))
			{
				value.PendingUpdate = true;
			}
			if (!nullInstance)
			{
				instance.LookUpPlayerCosmetics(wait: true);
			}
		}
	}

	public static void UpdatePlayerCosmetics(List<NetPlayer> players)
	{
		foreach (NetPlayer player in players)
		{
			if (player != null && !player.IsLocal)
			{
				playersToLookUp.Enqueue(player);
				if (userCosmeticCallback.TryGetValue(player.ActorNumber, out var value))
				{
					value.PendingUpdate = true;
				}
			}
		}
		if (!nullInstance)
		{
			instance.LookUpPlayerCosmetics();
		}
	}

	public static void SetRigTryOn(bool inTryon, RigContainer rigRefg)
	{
		VRRig rig = rigRefg.Rig;
		rig.inTryOnRoom = inTryon;
		if (inTryon)
		{
			if (sinceLastTryOnEvent.HasElapsed(0.5f, resetOnElapsed: true))
			{
				GorillaTelemetry.PostShopEvent(rig, GTShopEventType.item_try_on, rig.tryOnSet.items);
			}
		}
		else if (rig.isOfflineVRRig)
		{
			rig.tryOnSet.ClearSet(CosmeticsController.instance.nullItem);
			CosmeticsController.instance.ClearCheckout(sendEvent: false);
			CosmeticsController.instance.UpdateShoppingCart();
			CosmeticsController.instance.UpdateWornCosmetics(sync: true);
			rig.myBodyDockPositions.RefreshTransferrableItems();
			return;
		}
		rig.LocalUpdateCosmeticsWithTryon(rig.cosmeticSet, rig.tryOnSet, playfx: false);
		rig.myBodyDockPositions.RefreshTransferrableItems();
	}

	public static void SetRigTemporarySpace(bool enteringSpace, RigContainer rigRef, IReadOnlyList<string> cosmeticIds)
	{
		rigRef.Rig.inTempCosmSpace = enteringSpace;
		if (enteringSpace)
		{
			CosmeticsController.CosmeticSet currentWornSet = CosmeticsController.instance.currentWornSet;
			CosmeticsController.instance.tempUnlockedSet.CopyItemsIntoEmpty(currentWornSet);
			UnlockTemporaryCosmeticsForPlayer(rigRef, cosmeticIds);
		}
		else
		{
			LockTemporaryCosmeticsForPlayer(rigRef, cosmeticIds);
		}
	}

	public static void UnlockTemporaryCosmeticsForPlayer(RigContainer rigRef)
	{
		UnlockTemporaryCosmeticsForPlayer(rigRef, TempUnlockCosmeticString);
	}

	public static void UnlockTemporaryCosmeticsForPlayer(RigContainer rigRef, IReadOnlyList<string> cosmeticIds)
	{
		if (cosmeticIds == null)
		{
			Debug.LogError("PlayerCosmeticsSystem failed to unlock temporary cosmetics, cosmetic IDs are null");
			return;
		}
		VRRig rig = rigRef.Rig;
		foreach (string cosmeticId in cosmeticIds)
		{
			if (rig.TemporaryCosmetics.Add(cosmeticId) && rig.isOfflineVRRig && !rig.HasCosmetic(cosmeticId))
			{
				CosmeticsController.instance.AddTempUnlockToWardrobe(cosmeticId);
			}
		}
		CosmeticsController.instance.OnCosmeticsUpdated?.Invoke();
		if (rig.isOfflineVRRig)
		{
			CosmeticsController.instance.UpdateWornCosmetics(sync: true);
		}
		else
		{
			rig.RefreshCosmetics();
		}
	}

	public static void LockTemporaryCosmeticsForPlayer(RigContainer rigRef)
	{
		LockTemporaryCosmeticsForPlayer(rigRef, TempUnlockCosmeticString);
	}

	public static void LockTemporaryCosmeticsForPlayer(RigContainer rigRef, IReadOnlyList<string> cosmeticIds)
	{
		if (cosmeticIds == null)
		{
			Debug.LogError("PlayerCosmeticsSystem failed to unlock temporary cosmetics, cosmetic IDs are null");
			return;
		}
		VRRig rig = rigRef.Rig;
		foreach (string cosmeticId in cosmeticIds)
		{
			if (rig.TemporaryCosmetics.Remove(cosmeticId) && rig.isOfflineVRRig && !rig.HasCosmetic(cosmeticId))
			{
				CosmeticsController.instance.RemoveTempUnlockFromWardrobe(cosmeticId);
			}
		}
		CosmeticsController.instance.OnCosmeticsUpdated?.Invoke();
		if (rig.isOfflineVRRig)
		{
			CosmeticsController.instance.UpdateWornCosmetics(sync: true);
		}
		else
		{
			rig.RefreshCosmetics();
		}
	}

	internal static void UnlockTemporaryCosmeticsGlobal(IReadOnlyList<string> cosmeticIds)
	{
		int count = cosmeticIds.Count;
		for (int i = 0; i < count; i++)
		{
			UnlockTemporaryCosmeticGlobal(cosmeticIds[i]);
		}
	}

	internal static void UnlockTemporaryCosmeticGlobal(string cosmeticId)
	{
		int num = 0;
		if (k_tempUnlockedCosmetics.ContainsKey(cosmeticId))
		{
			num = k_tempUnlockedCosmetics[cosmeticId];
		}
		num++;
		k_tempUnlockedCosmetics[cosmeticId] = num;
	}

	internal static void LockTemporaryCosmeticsGlobal(IReadOnlyList<string> cosmeticIds)
	{
		int count = cosmeticIds.Count;
		for (int i = 0; i < count; i++)
		{
			LockTemporaryCosmeticGlobal(cosmeticIds[i]);
		}
	}

	internal static void LockTemporaryCosmeticGlobal(string cosmeticId)
	{
		if (!k_tempUnlockedCosmetics.ContainsKey(cosmeticId))
		{
			Debug.LogError("PlayerCosmeticsSystem: Unable to lock cosmetic, ID:-" + cosmeticId + " not found!");
			return;
		}
		int num = k_tempUnlockedCosmetics[cosmeticId];
		num--;
		k_tempUnlockedCosmetics[cosmeticId] = num;
	}

	public static bool IsTemporaryCosmeticAllowed(VRRig rigRef, string cosmeticId)
	{
		if (!rigRef.TemporaryCosmetics.Contains(cosmeticId))
		{
			if (k_tempUnlockedCosmetics.TryGetValue(cosmeticId, out var value))
			{
				return value > 0;
			}
			return false;
		}
		return true;
	}

	public static bool LocalIsTemporaryCosmetic(string cosmeticId)
	{
		VRRig rig = VRRigCache.Instance.localRig.Rig;
		if (!rig.HasCosmetic(cosmeticId))
		{
			return IsTemporaryCosmeticAllowed(rig, cosmeticId);
		}
		return false;
	}

	public static bool LocalPlayerInTemporaryCosmeticSpace()
	{
		return VRRigCache.Instance.localRig.Rig.inTempCosmSpace;
	}

	public static void StaticReset()
	{
		playersToLookUp.Clear();
		userCosmeticCallback.Clear();
		userCosmeticsWaiting.Clear();
		playerIDsList.Clear();
		playersWaiting.Clear();
	}
}
