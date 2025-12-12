using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace GorillaNetworking.Store;

public class StandImport
{
	public List<StandTypeData> standData = new List<StandTypeData>();

	public Dictionary<string, StandTypeData> standKeyToDataDict = new Dictionary<string, StandTypeData>();

	public void DecomposeFromTitleDataString(string data)
	{
		string[] array = data.Split("\\n");
		for (int i = 0; i < array.Length; i++)
		{
			DecomposeStandDataTitleData(array[i]);
		}
	}

	public void DecomposeStandDataTitleData(string dataString)
	{
		string[] array = dataString.Split("\\t");
		if (array.Length == 5)
		{
			standData.Add(new StandTypeData(array));
			return;
		}
		if (array.Length == 4)
		{
			standData.Add(new StandTypeData(array));
			return;
		}
		string text = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			text = text + text2 + "|";
		}
		Debug.LogError("Store Importer Data String is not valid : " + text);
	}

	public void DeserializeFromJSON(string JSONString)
	{
		standData = JsonConvert.DeserializeObject<List<StandTypeData>>(JSONString);
	}

	public void DecomposeStandData(string dataString)
	{
		string[] array = dataString.Split('\t');
		if (array.Length == 5)
		{
			standData.Add(new StandTypeData(array));
			return;
		}
		if (array.Length == 4)
		{
			standData.Add(new StandTypeData(array));
			return;
		}
		string text = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			text = text + text2 + "|";
		}
		Debug.LogError("Store Importer Data String is not valid : " + text);
	}
}
