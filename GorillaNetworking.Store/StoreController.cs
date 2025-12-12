using System.Collections.Generic;
using System.Linq;
using GorillaExtensions;
using GorillaTag.CosmeticSystem;
using PlayFab;
using UnityEngine;

namespace GorillaNetworking.Store;

public class StoreController : MonoBehaviour
{
	[OnEnterPlay_Clear]
	public static volatile StoreController instance;

	public List<StoreDepartment> Departments;

	private Dictionary<string, DynamicCosmeticStand> CosmeticStandsDict;

	public Dictionary<string, List<DynamicCosmeticStand>> StandsByPlayfabID;

	public AllCosmeticsArraySO AllCosmeticsArraySO;

	public bool cosmeticsInitialized;

	public bool LoadFromTitleData;

	private string exportHeader = "Department ID\tDisplay ID\tStand ID\tStand Type\tPlayFab ID";

	private StandImport standImport;

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		CosmeticStandsDict = new Dictionary<string, DynamicCosmeticStand>();
		StandsByPlayfabID = new Dictionary<string, List<DynamicCosmeticStand>>();
	}

	public void RefreshCosmeticStandsDictionaryFromDepartments()
	{
		foreach (StoreDepartment department in Departments)
		{
			if (department == null || department.departmentName.IsNullOrEmpty())
			{
				continue;
			}
			StoreDisplay[] displays = department.Displays;
			foreach (StoreDisplay storeDisplay in displays)
			{
				if (storeDisplay.displayName.IsNullOrEmpty())
				{
					continue;
				}
				DynamicCosmeticStand[] stands = storeDisplay.Stands;
				foreach (DynamicCosmeticStand dynamicCosmeticStand in stands)
				{
					if (!dynamicCosmeticStand.StandName.IsNullOrEmpty())
					{
						string text = department.departmentName + "|" + storeDisplay.displayName + "|" + dynamicCosmeticStand.StandName;
						if (CosmeticStandsDict.ContainsKey(text))
						{
							Debug.LogError("StoreStuff: Duplicate Stand Name: " + text + " Please Fix Gameobject : " + dynamicCosmeticStand.gameObject.GetPath() + dynamicCosmeticStand.gameObject.name, base.gameObject);
						}
						else
						{
							CosmeticStandsDict.Add(text, dynamicCosmeticStand);
						}
					}
				}
			}
		}
	}

	public void AddStandToCosmeticStandsDictionary(DynamicCosmeticStand stand)
	{
		if (!(stand.parentDepartment == null) && !stand.parentDepartment.departmentName.IsNullOrEmpty() && !(stand.parentDisplay == null) && !stand.parentDisplay.displayName.IsNullOrEmpty() && !stand.StandName.IsNullOrEmpty() && CosmeticStandsDict != null)
		{
			string text = stand.parentDepartment.departmentName + "|" + stand.parentDisplay.displayName + "|" + stand.StandName;
			if (CosmeticStandsDict.ContainsKey(text))
			{
				Debug.LogError("StoreStuff: Duplicate Stand Name: " + text + " Please Fix Gameobject : " + stand.gameObject.GetPath() + stand.gameObject.name, base.gameObject);
			}
			else
			{
				CosmeticStandsDict.Add(text, stand);
			}
		}
	}

	public void RemoveStandFromDynamicCosmeticStandsDictionary(DynamicCosmeticStand stand)
	{
		if (!(stand.parentDepartment == null) && !stand.parentDepartment.departmentName.IsNullOrEmpty() && !(stand.parentDisplay == null) && !stand.parentDisplay.displayName.IsNullOrEmpty() && !stand.StandName.IsNullOrEmpty() && CosmeticStandsDict != null)
		{
			string text = stand.parentDepartment.departmentName + "|" + stand.parentDisplay.displayName + "|" + stand.StandName;
			if (!CosmeticStandsDict.ContainsKey(text))
			{
				Debug.LogError("StoreStuff: StoreController doesn't have stand in its dict. that's weird!: " + text + " Please Fix Gameobject : " + stand.gameObject.GetPath() + stand.gameObject.name, base.gameObject);
			}
			else
			{
				CosmeticStandsDict.Remove(text);
			}
		}
	}

	private void Create_StandsByPlayfabIDDictionary()
	{
		foreach (DynamicCosmeticStand value in CosmeticStandsDict.Values)
		{
			AddStandToPlayfabIDDictionary(value);
		}
	}

	public void AddStandToPlayfabIDDictionary(DynamicCosmeticStand dynamicCosmeticStand)
	{
		if (!dynamicCosmeticStand.StandName.IsNullOrEmpty() && !dynamicCosmeticStand.thisCosmeticName.IsNullOrEmpty())
		{
			if (StandsByPlayfabID.ContainsKey(dynamicCosmeticStand.thisCosmeticName))
			{
				StandsByPlayfabID[dynamicCosmeticStand.thisCosmeticName].Add(dynamicCosmeticStand);
				return;
			}
			StandsByPlayfabID.Add(dynamicCosmeticStand.thisCosmeticName, new List<DynamicCosmeticStand> { dynamicCosmeticStand });
		}
	}

	public void RemoveStandFromPlayFabIDDictionary(DynamicCosmeticStand dynamicCosmeticStand)
	{
		if (StandsByPlayfabID.TryGetValue(dynamicCosmeticStand.thisCosmeticName, out var value))
		{
			value.Remove(dynamicCosmeticStand);
		}
	}

	public void ExportCosmeticStandLayoutWithItems()
	{
	}

	public void ExportCosmeticStandLayoutWITHOUTItems()
	{
	}

	public void ImportCosmeticStandLayout()
	{
	}

	private void InitializeFromTitleData()
	{
		PlayFabTitleDataCache.Instance.GetTitleData("StoreLayoutData", delegate(string data)
		{
			ImportCosmeticStandLayoutFromTitleData(data);
		}, delegate(PlayFabError e)
		{
			Debug.LogError($"Error getting StoreLayoutData data: {e}");
		});
	}

	private void ImportCosmeticStandLayoutFromTitleData(string TSVData)
	{
		standImport = new StandImport();
		standImport.DecomposeFromTitleDataString(TSVData);
		foreach (StandTypeData standDatum in standImport.standData)
		{
			string key = standDatum.departmentID + "|" + standDatum.displayID + "|" + standDatum.standID;
			standImport.standKeyToDataDict.Add(key, standDatum);
			if (CosmeticStandsDict.ContainsKey(key))
			{
				CosmeticStandsDict[key].SetStandTypeString(standDatum.bustType);
				CosmeticStandsDict[key].SpawnItemOntoStand(standDatum.playFabID);
				CosmeticStandsDict[key].InitializeCosmetic();
			}
		}
	}

	public void InitializeStandFromTitleData(DynamicCosmeticStand stand)
	{
		if (stand.parentDepartment == null || stand.parentDepartment.departmentName.IsNullOrEmpty() || stand.parentDisplay == null || stand.parentDisplay.displayName.IsNullOrEmpty() || stand.StandName.IsNullOrEmpty() || CosmeticStandsDict == null)
		{
			Debug.LogError("Stand " + stand.name + " is missing important setup data somehow, please fix!", stand.gameObject);
			return;
		}
		string key = stand.parentDepartment.departmentName + "|" + stand.parentDisplay.displayName + "|" + stand.StandName;
		if (CosmeticStandsDict.ContainsKey(key) && standImport.standKeyToDataDict.ContainsKey(key))
		{
			StandTypeData standTypeData = standImport.standKeyToDataDict[key];
			CosmeticStandsDict[key].SetStandTypeString(standTypeData.bustType);
			CosmeticStandsDict[key].SpawnItemOntoStand(standTypeData.playFabID);
			CosmeticStandsDict[key].InitializeCosmetic();
		}
	}

	public void InitalizeCosmeticStands()
	{
		cosmeticsInitialized = true;
		RefreshCosmeticStandsDictionaryFromDepartments();
		if (LoadFromTitleData)
		{
			InitializeFromTitleData();
		}
	}

	public void LoadCosmeticOntoStand(string standID, string playFabId)
	{
		if (CosmeticStandsDict.ContainsKey(standID))
		{
			CosmeticStandsDict[standID].SpawnItemOntoStand(playFabId);
			Debug.Log("StoreStuff: Cosmetic Loaded Onto Stand: " + standID + " | " + playFabId);
		}
	}

	public void ClearCosmetics()
	{
		foreach (StoreDepartment department in Departments)
		{
			StoreDisplay[] displays = department.Displays;
			for (int i = 0; i < displays.Length; i++)
			{
				DynamicCosmeticStand[] stands = displays[i].Stands;
				for (int j = 0; j < stands.Length; j++)
				{
					stands[j].ClearCosmetics();
				}
			}
		}
	}

	public static CosmeticSO FindCosmeticInAllCosmeticsArraySO(string playfabId)
	{
		if (instance == null)
		{
			instance = Object.FindAnyObjectByType<StoreController>();
		}
		return instance.AllCosmeticsArraySO.SearchForCosmeticSO(playfabId);
	}

	public DynamicCosmeticStand FindCosmeticStandByCosmeticName(string PlayFabID)
	{
		foreach (DynamicCosmeticStand value in CosmeticStandsDict.Values)
		{
			if (value.thisCosmeticName == PlayFabID)
			{
				return value;
			}
		}
		return null;
	}

	public void FindAllDepartments()
	{
		Departments = Object.FindObjectsByType<StoreDepartment>(FindObjectsSortMode.None).ToList();
	}

	public void SaveAllCosmeticsPositions()
	{
		foreach (StoreDepartment department in Departments)
		{
			StoreDisplay[] displays = department.Displays;
			foreach (StoreDisplay storeDisplay in displays)
			{
				DynamicCosmeticStand[] stands = storeDisplay.Stands;
				foreach (DynamicCosmeticStand dynamicCosmeticStand in stands)
				{
					Debug.Log("StoreStuff: Saving Items mount transform: " + department.departmentName + "|" + storeDisplay.displayName + "|" + dynamicCosmeticStand.StandName + "|" + dynamicCosmeticStand.DisplayHeadModel.bustType.ToString() + "|" + dynamicCosmeticStand.thisCosmeticName);
					dynamicCosmeticStand.UpdateCosmeticsMountPositions();
				}
			}
		}
	}

	public static void SetForGame()
	{
		if (instance == null)
		{
			instance = Object.FindAnyObjectByType<StoreController>();
		}
		instance.RefreshCosmeticStandsDictionaryFromDepartments();
		foreach (DynamicCosmeticStand value in instance.CosmeticStandsDict.Values)
		{
			value.SetStandType(value.DisplayHeadModel.bustType);
			value.SpawnItemOntoStand(value.thisCosmeticName);
		}
	}
}
