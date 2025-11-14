using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaNetworking;
using GorillaNetworking.Store;
using GT_CustomMapSupportRuntime;
using PlayFab;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

[Serializable]
[CreateAssetMenu(menuName = "ScriptableObjects/CustomMapCosmeticDataSO", order = 0)]
public class CustomMapCosmeticsData : ScriptableObject
{
	[SerializeField]
	private List<CustomMapCosmeticItem> fallbackItems;

	[SerializeField]
	private List<CustomMapCosmeticItem> customMapCosmeticItemList;

	public string titleDataKey = "CustomMapCosmeticData";

	private bool initializedFromTitleData;

	public void OnEnable()
	{
		initializedFromTitleData = false;
	}

	public void OnDestroy()
	{
		if (PlayFabTitleDataCache.Instance.IsNotNull())
		{
			PlayFabTitleDataCache.Instance.OnTitleDataUpdate.RemoveListener(OnTitleDataUpdated);
		}
	}

	public bool TryGetItem(GTObjectPlaceholder.ECustomMapCosmeticItem customMapItemSlot, out CustomMapCosmeticItem foundItem)
	{
		if (!initializedFromTitleData)
		{
			UpdateFromTitleData();
		}
		foundItem = new CustomMapCosmeticItem
		{
			bustType = HeadModel_CosmeticStand.BustType.Disabled,
			playFabID = "INVALID"
		};
		for (int i = 0; i < customMapCosmeticItemList.Count; i++)
		{
			if (customMapCosmeticItemList[i].customMapItemSlot == customMapItemSlot)
			{
				foundItem = customMapCosmeticItemList[i];
				return true;
			}
		}
		for (int j = 0; j < fallbackItems.Count; j++)
		{
			if (fallbackItems[j].customMapItemSlot == customMapItemSlot)
			{
				foundItem = fallbackItems[j];
				return true;
			}
		}
		return false;
	}

	private void UpdateFromTitleData()
	{
		if (!initializedFromTitleData && !PlayFabTitleDataCache.Instance.IsNull())
		{
			PlayFabTitleDataCache.Instance.OnTitleDataUpdate.RemoveListener(OnTitleDataUpdated);
			PlayFabTitleDataCache.Instance.OnTitleDataUpdate.AddListener(OnTitleDataUpdated);
			if (PlayFabTitleDataCache.Instance == null)
			{
				Debug.LogError("[CustomMapCosmeticsData::UpdateFromTitleData] TitleData not available, using fallback item data.");
				initializedFromTitleData = true;
			}
			else
			{
				PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, OnGetCosmeticsDataFromTitleData, OnPlayFabError);
				initializedFromTitleData = true;
			}
		}
	}

	private void OnTitleDataUpdated(string updatedKey)
	{
		if (updatedKey == titleDataKey)
		{
			initializedFromTitleData = false;
			UpdateFromTitleData();
		}
	}

	private void OnGetCosmeticsDataFromTitleData(string cosmeticsData)
	{
		string[] array = cosmeticsData.Split("|");
		foreach (string obj in array)
		{
			string s = obj;
			s = s.RemoveAll('\\');
			s = s.Trim('"');
			CustomMapCosmeticItem itemFromJson = JsonUtility.FromJson<CustomMapCosmeticItem>(s);
			customMapCosmeticItemList.RemoveAll((CustomMapCosmeticItem item) => (item.customMapItemSlot == itemFromJson.customMapItemSlot) ? true : false);
			customMapCosmeticItemList.Add(itemFromJson);
		}
	}

	private void OnPlayFabError(PlayFabError error)
	{
		Debug.LogError("[CustomMapCosmeticsData::OnPlayFabError] failed to retrieve CosmeticsData from PlayFab: " + error.ErrorMessage);
	}
}
