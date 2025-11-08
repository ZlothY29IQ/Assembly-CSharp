using UnityEngine;

namespace GorillaNetworking.Store;

public class StandTypeData
{
	public enum EStandDataID
	{
		departmentID,
		displayID,
		standID,
		bustType,
		playFabID,
		Count
	}

	public string departmentID = "";

	public string displayID = "";

	public string standID = "";

	public string bustType = "";

	public string playFabID = "";

	public StandTypeData(string[] spawnData)
	{
		departmentID = spawnData[0];
		displayID = spawnData[1];
		standID = spawnData[2];
		bustType = spawnData[3];
		if (spawnData.Length == 5)
		{
			playFabID = spawnData[4];
		}
		Debug.Log("StoreStuff: StandTypeData: " + departmentID + "\n" + displayID + "\n" + standID + "\n" + bustType + "\n" + playFabID);
	}

	public StandTypeData(string departmentID, string displayID, string standID, HeadModel_CosmeticStand.BustType bustType, string playFabID)
	{
		this.departmentID = departmentID;
		this.displayID = displayID;
		this.standID = standID;
		this.bustType = bustType.ToString();
		this.playFabID = playFabID;
	}
}
