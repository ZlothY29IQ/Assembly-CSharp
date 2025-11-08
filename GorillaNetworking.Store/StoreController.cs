using System.Collections.Generic;
using System.Linq;
using GorillaExtensions;
using GorillaTag.CosmeticSystem;
using PlayFab;
using UnityEngine;

namespace GorillaNetworking.Store;

public class StoreController : MonoBehaviour
{
	public static volatile StoreController instance;

	public List<StoreDepartment> Departments;

	private Dictionary<string, DynamicCosmeticStand> CosmeticStandsDict;

	public Dictionary<string, List<DynamicCosmeticStand>> StandsByPlayfabID;

	public AllCosmeticsArraySO AllCosmeticsArraySO;

	public bool LoadFromTitleData;

	private string exportHeader = "Department ID\tDisplay ID\tStand ID\tStand Type\tPlayFab ID";

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Object.Destroy(base.gameObject);
		}
	}

	public void Start()
	{
	}

	public void CreateDynamicCosmeticStandsDictionatary()
	{
		CosmeticStandsDict = new Dictionary<string, DynamicCosmeticStand>();
		foreach (StoreDepartment department in Departments)
		{
			if (department.departmentName.IsNullOrEmpty())
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
						if (!CosmeticStandsDict.ContainsKey(department.departmentName + "|" + storeDisplay.displayName + "|" + dynamicCosmeticStand.StandName))
						{
							CosmeticStandsDict.Add(department.departmentName + "|" + storeDisplay.displayName + "|" + dynamicCosmeticStand.StandName, dynamicCosmeticStand);
						}
						else
						{
							Debug.LogError("StoreStuff: Duplicate Stand Name: " + department.departmentName + "|" + storeDisplay.displayName + "|" + dynamicCosmeticStand.StandName + " Please Fix Gameobject : " + dynamicCosmeticStand.gameObject.GetPath() + dynamicCosmeticStand.gameObject.name);
						}
					}
				}
			}
		}
	}

	private void Create_StandsByPlayfabIDDictionary()
	{
		StandsByPlayfabID = new Dictionary<string, List<DynamicCosmeticStand>>();
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
		StandImport standImport = new StandImport();
		standImport.DecomposeFromTitleDataString(TSVData);
		foreach (StandTypeData standDatum in standImport.standData)
		{
			string text = standDatum.departmentID + "|" + standDatum.displayID + "|" + standDatum.standID;
			if (CosmeticStandsDict.ContainsKey(text))
			{
				Debug.Log("StoreStuff: Stand Updated: " + standDatum.departmentID + "|" + standDatum.displayID + "|" + standDatum.standID + "|" + standDatum.bustType + "|" + standDatum.playFabID + "|");
				CosmeticStandsDict[text].SetStandTypeString(standDatum.bustType);
				Debug.Log("Manually Initializing Stand: " + text + " |||| " + standDatum.playFabID);
				CosmeticStandsDict[text].SpawnItemOntoStand(standDatum.playFabID);
				CosmeticStandsDict[text].InitializeCosmetic();
			}
		}
	}

	public void InitalizeCosmeticStands()
	{
		CreateDynamicCosmeticStandsDictionatary();
		foreach (DynamicCosmeticStand value in CosmeticStandsDict.Values)
		{
			value.InitializeCosmetic();
		}
		Create_StandsByPlayfabIDDictionary();
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
		instance.CreateDynamicCosmeticStandsDictionatary();
		foreach (DynamicCosmeticStand value in instance.CosmeticStandsDict.Values)
		{
			value.SetStandType(value.DisplayHeadModel.bustType);
			value.SpawnItemOntoStand(value.thisCosmeticName);
		}
	}
}
