using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FXP;
using PlayFab;
using UnityEngine;

namespace GorillaNetworking.Store;

public class StoreUpdater : MonoBehaviour
{
	public static volatile StoreUpdater instance;

	private DateTime StoreItemsChangeTimeUTC;

	private Dictionary<string, CosmeticItemPrefab> cosmeticItemPrefabsDictionary = new Dictionary<string, CosmeticItemPrefab>();

	private Dictionary<string, List<StoreUpdateEvent>> pedestalUpdateEvents = new Dictionary<string, List<StoreUpdateEvent>>();

	private Dictionary<string, Coroutine> pedestalUpdateCoroutines = new Dictionary<string, Coroutine>();

	private Dictionary<string, Coroutine> pedestalClearCartCoroutines = new Dictionary<string, Coroutine>();

	private string tempJson;

	private bool bLoadFromJSON = true;

	private bool bUsePlaceHolderJSON;

	public DateTime DateTimeNowServerAdjusted => GorillaComputer.instance.GetServerTime();

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			HandleHMDMounted();
		}
		else
		{
			HandleHMDUnmounted();
		}
	}

	public void Initialize()
	{
		FindAllCosmeticItemPrefabs();
		OVRManager.HMDMounted += HandleHMDMounted;
		OVRManager.HMDUnmounted += HandleHMDUnmounted;
		OVRManager.HMDLost += HandleHMDUnmounted;
		OVRManager.HMDAcquired += HandleHMDMounted;
		if (bLoadFromJSON)
		{
			GetEventsFromTitleData();
		}
	}

	public void OnDestroy()
	{
		OVRManager.HMDMounted -= HandleHMDMounted;
		OVRManager.HMDUnmounted -= HandleHMDUnmounted;
		OVRManager.HMDLost -= HandleHMDUnmounted;
		OVRManager.HMDAcquired -= HandleHMDMounted;
	}

	private void HandleHMDUnmounted()
	{
		foreach (string key in pedestalUpdateCoroutines.Keys)
		{
			if (pedestalUpdateCoroutines[key] != null)
			{
				StopCoroutine(pedestalUpdateCoroutines[key]);
			}
		}
		foreach (string key2 in cosmeticItemPrefabsDictionary.Keys)
		{
			if (cosmeticItemPrefabsDictionary[key2] != null)
			{
				cosmeticItemPrefabsDictionary[key2].StopCountdownCoroutine();
			}
		}
	}

	private void HandleHMDMounted()
	{
		foreach (string key in cosmeticItemPrefabsDictionary.Keys)
		{
			if (cosmeticItemPrefabsDictionary[key] != null && pedestalUpdateEvents.ContainsKey(key) && cosmeticItemPrefabsDictionary[key].gameObject.activeInHierarchy)
			{
				CheckEventsOnResume(pedestalUpdateEvents[key]);
				StartNextEvent(key, playFX: false);
			}
		}
	}

	private void FindAllCosmeticItemPrefabs()
	{
		CosmeticItemPrefab[] array = UnityEngine.Object.FindObjectsByType<CosmeticItemPrefab>(FindObjectsSortMode.None);
		foreach (CosmeticItemPrefab cosmeticItemPrefab in array)
		{
			if (cosmeticItemPrefabsDictionary.ContainsKey(cosmeticItemPrefab.PedestalID))
			{
				Debug.LogWarning("StoreUpdater - Duplicate Pedestal ID " + cosmeticItemPrefab.PedestalID);
			}
			else
			{
				cosmeticItemPrefabsDictionary.Add(cosmeticItemPrefab.PedestalID, cosmeticItemPrefab);
			}
		}
	}

	private IEnumerator HandlePedestalUpdate(StoreUpdateEvent updateEvent, bool playFX)
	{
		cosmeticItemPrefabsDictionary[updateEvent.PedestalID].SetStoreUpdateEvent(updateEvent, playFX);
		yield return new WaitForSeconds((float)(updateEvent.EndTimeUTC.ToUniversalTime() - DateTimeNowServerAdjusted).TotalSeconds);
		if (pedestalClearCartCoroutines.ContainsKey(updateEvent.PedestalID))
		{
			if (pedestalClearCartCoroutines[updateEvent.PedestalID] != null)
			{
				StopCoroutine(pedestalClearCartCoroutines[updateEvent.PedestalID]);
			}
			pedestalClearCartCoroutines[updateEvent.PedestalID] = StartCoroutine(HandleClearCart(updateEvent));
		}
		else
		{
			pedestalClearCartCoroutines.Add(updateEvent.PedestalID, StartCoroutine(HandleClearCart(updateEvent)));
		}
		if (cosmeticItemPrefabsDictionary[updateEvent.PedestalID].gameObject.activeInHierarchy)
		{
			pedestalUpdateEvents[updateEvent.PedestalID].RemoveAt(0);
			StartNextEvent(updateEvent.PedestalID, playFX: true);
		}
	}

	private IEnumerator HandleClearCart(StoreUpdateEvent updateEvent)
	{
		float seconds = Math.Clamp((float)(updateEvent.EndTimeUTC.ToUniversalTime() - DateTimeNowServerAdjusted).TotalSeconds + 60f, 0f, 60f);
		yield return new WaitForSeconds(seconds);
		if (CosmeticsController.instance.RemoveItemFromCart(CosmeticsController.instance.GetItemFromDict(updateEvent.ItemName)))
		{
			CosmeticsController.instance.ClearCheckout(sendEvent: true);
			CosmeticsController.instance.UpdateShoppingCart();
			CosmeticsController.instance.UpdateWornCosmetics(sync: true);
		}
	}

	private void StartNextEvent(string pedestalID, bool playFX)
	{
		if (pedestalUpdateEvents[pedestalID].Count > 0)
		{
			Coroutine value = StartCoroutine(HandlePedestalUpdate(pedestalUpdateEvents[pedestalID].First(), playFX));
			if (pedestalUpdateCoroutines.ContainsKey(pedestalID))
			{
				if (pedestalUpdateCoroutines[pedestalID] != null && pedestalUpdateCoroutines[pedestalID] != null)
				{
					StopCoroutine(pedestalUpdateCoroutines[pedestalID]);
				}
				pedestalUpdateCoroutines[pedestalID] = value;
			}
			else
			{
				pedestalUpdateCoroutines.Add(pedestalID, value);
			}
			if (pedestalUpdateEvents[pedestalID].Count == 0 && !bLoadFromJSON)
			{
				GetStoreUpdateEventsPlaceHolder(pedestalID);
			}
		}
		else if (!bLoadFromJSON)
		{
			GetStoreUpdateEventsPlaceHolder(pedestalID);
			StartNextEvent(pedestalID, playFX: true);
		}
	}

	private void GetStoreUpdateEventsPlaceHolder(string PedestalID)
	{
		List<StoreUpdateEvent> list = new List<StoreUpdateEvent>();
		list = CreateTempEvents(PedestalID, 1, 15);
		CheckEvents(list);
		if (pedestalUpdateEvents.ContainsKey(PedestalID))
		{
			pedestalUpdateEvents[PedestalID].AddRange(list);
		}
		else
		{
			pedestalUpdateEvents.Add(PedestalID, list);
		}
	}

	private void CheckEvents(List<StoreUpdateEvent> updateEvents)
	{
		for (int i = 0; i < updateEvents.Count; i++)
		{
			if (updateEvents[i].EndTimeUTC.ToUniversalTime() < DateTimeNowServerAdjusted)
			{
				updateEvents.RemoveAt(i);
				i--;
			}
		}
	}

	private void CheckEventsOnResume(List<StoreUpdateEvent> updateEvents)
	{
		bool flag = false;
		for (int i = 0; i < updateEvents.Count; i++)
		{
			if (!(updateEvents[i].EndTimeUTC.ToUniversalTime() < DateTimeNowServerAdjusted))
			{
				continue;
			}
			if (Math.Clamp((float)(updateEvents[i].EndTimeUTC.ToUniversalTime() - DateTimeNowServerAdjusted).TotalSeconds + 60f, 0f, 60f) <= 0f)
			{
				flag ^= CosmeticsController.instance.RemoveItemFromCart(CosmeticsController.instance.GetItemFromDict(updateEvents[i].ItemName));
			}
			else if (pedestalClearCartCoroutines.ContainsKey(updateEvents[i].PedestalID))
			{
				if (pedestalClearCartCoroutines[updateEvents[i].PedestalID] != null)
				{
					StopCoroutine(pedestalClearCartCoroutines[updateEvents[i].PedestalID]);
				}
				pedestalClearCartCoroutines[updateEvents[i].PedestalID] = StartCoroutine(HandleClearCart(updateEvents[i]));
			}
			else
			{
				pedestalClearCartCoroutines.Add(updateEvents[i].PedestalID, StartCoroutine(HandleClearCart(updateEvents[i])));
			}
			updateEvents.RemoveAt(i);
			i--;
		}
		if (flag)
		{
			CosmeticsController.instance.ClearCheckout(sendEvent: true);
			CosmeticsController.instance.UpdateShoppingCart();
			CosmeticsController.instance.UpdateWornCosmetics(sync: true);
		}
	}

	private void GetEventsFromTitleData()
	{
		if (bUsePlaceHolderJSON)
		{
			DateTime startTime = new DateTime(2024, 2, 13, 16, 0, 0, DateTimeKind.Utc);
			List<StoreUpdateEvent> updateEvents = StoreUpdateEvent.DeserializeFromJSonList(StoreUpdateEvent.SerializeArrayAsJSon(CreateTempEvents("Pedestal1", 2, 120, startTime).ToArray()));
			HandleRecievingEventsFromTitleData(updateEvents);
			return;
		}
		PlayFabTitleDataCache.Instance.GetTitleData("TOTD", delegate(string result)
		{
			List<StoreUpdateEvent> updateEvents2 = StoreUpdateEvent.DeserializeFromJSonList(result);
			HandleRecievingEventsFromTitleData(updateEvents2);
		}, delegate(PlayFabError error)
		{
			Debug.Log("StoreUpdater - Error Title Data : " + error.ErrorMessage);
		});
	}

	private void HandleRecievingEventsFromTitleData(List<StoreUpdateEvent> updateEvents)
	{
		CheckEvents(updateEvents);
		if (CosmeticsController.instance.GetItemFromDict("LBAEY.").isNullItem)
		{
			Debug.LogWarning("StoreUpdater - CosmeticsController is not initialized.  Reinitializing TitleData");
			GetEventsFromTitleData();
			return;
		}
		foreach (StoreUpdateEvent updateEvent in updateEvents)
		{
			if (pedestalUpdateEvents.ContainsKey(updateEvent.PedestalID))
			{
				pedestalUpdateEvents[updateEvent.PedestalID].Add(updateEvent);
				continue;
			}
			pedestalUpdateEvents.Add(updateEvent.PedestalID, new List<StoreUpdateEvent>());
			pedestalUpdateEvents[updateEvent.PedestalID].Add(updateEvent);
		}
		foreach (string key in pedestalUpdateEvents.Keys)
		{
			if (cosmeticItemPrefabsDictionary.ContainsKey(key))
			{
				StartNextEvent(key, playFX: false);
			}
		}
		foreach (string key2 in cosmeticItemPrefabsDictionary.Keys)
		{
			if (!pedestalUpdateEvents.ContainsKey(key2))
			{
				GetStoreUpdateEventsPlaceHolder(key2);
				StartNextEvent(key2, playFX: false);
			}
		}
	}

	private void PrintJSONEvents()
	{
		string json = StoreUpdateEvent.SerializeArrayAsJSon(CreateTempEvents("Pedestal1", 5, 28).ToArray());
		foreach (StoreUpdateEvent item in StoreUpdateEvent.DeserializeFromJSonList(json))
		{
			_ = item;
		}
		tempJson = json;
	}

	private List<StoreUpdateEvent> CreateTempEvents(string PedestalID, int minuteDelay, int totalEvents)
	{
		string[] array = new string[14]
		{
			"LBAEY.", "LBAEZ.", "LBAFA.", "LBAFB.", "LBAFC.", "LBAFD.", "LBAFE.", "LBAFF.", "LBAFG.", "LBAFH.",
			"LBAFO.", "LBAFP.", "LBAFQ.", "LBAFR."
		};
		List<StoreUpdateEvent> list = new List<StoreUpdateEvent>();
		for (int i = 0; i < totalEvents; i++)
		{
			StoreUpdateEvent item = new StoreUpdateEvent(PedestalID, array[i % 14], DateTime.UtcNow + TimeSpan.FromMinutes(minuteDelay * i), DateTime.UtcNow + TimeSpan.FromMinutes(minuteDelay * (i + 1)));
			list.Add(item);
		}
		return list;
	}

	private List<StoreUpdateEvent> CreateTempEvents(string PedestalID, int minuteDelay, int totalEvents, DateTime startTime)
	{
		string[] array = new string[14]
		{
			"LBAEY.", "LBAEZ.", "LBAFA.", "LBAFB.", "LBAFC.", "LBAFD.", "LBAFE.", "LBAFF.", "LBAFG.", "LBAFH.",
			"LBAFO.", "LBAFP.", "LBAFQ.", "LBAFR."
		};
		List<StoreUpdateEvent> list = new List<StoreUpdateEvent>();
		for (int i = 0; i < totalEvents; i++)
		{
			StoreUpdateEvent item = new StoreUpdateEvent(PedestalID, array[i % 14], startTime + TimeSpan.FromMinutes(minuteDelay * i), startTime + TimeSpan.FromMinutes(minuteDelay * (i + 1)));
			list.Add(item);
		}
		return list;
	}

	public void PedestalAsleep(CosmeticItemPrefab pedestal)
	{
		if (pedestalUpdateCoroutines.ContainsKey(pedestal.PedestalID) && pedestalUpdateCoroutines[pedestal.PedestalID] != null)
		{
			StopCoroutine(pedestalUpdateCoroutines[pedestal.PedestalID]);
		}
	}

	public void PedestalAwakened(CosmeticItemPrefab pedestal)
	{
		if (!cosmeticItemPrefabsDictionary.ContainsKey(pedestal.PedestalID))
		{
			cosmeticItemPrefabsDictionary.Add(pedestal.PedestalID, pedestal);
		}
		if (pedestalUpdateEvents.ContainsKey(pedestal.PedestalID))
		{
			CheckEventsOnResume(pedestalUpdateEvents[pedestal.PedestalID]);
			StartNextEvent(pedestal.PedestalID, playFX: false);
		}
	}
}
