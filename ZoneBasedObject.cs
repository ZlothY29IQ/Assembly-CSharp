using UnityEngine;

public class ZoneBasedObject : MonoBehaviour
{
	public GTZone[] zones;

	public bool IsLocalPlayerInZone()
	{
		GTZone[] array = zones;
		for (int i = 0; i < array.Length; i++)
		{
			if (ZoneManagement.IsInZone(array[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static ZoneBasedObject SelectRandomEligible(ZoneBasedObject[] objects, string overrideChoice = "")
	{
		ZoneBasedObject[] array;
		if (overrideChoice != "")
		{
			array = objects;
			foreach (ZoneBasedObject zoneBasedObject in array)
			{
				if (zoneBasedObject.gameObject.name == overrideChoice)
				{
					return zoneBasedObject;
				}
			}
		}
		ZoneBasedObject result = null;
		int num = 0;
		array = objects;
		foreach (ZoneBasedObject zoneBasedObject2 in array)
		{
			if (!zoneBasedObject2.gameObject.activeInHierarchy)
			{
				continue;
			}
			GTZone[] array2 = zoneBasedObject2.zones;
			for (int j = 0; j < array2.Length; j++)
			{
				if (ZoneManagement.IsInZone(array2[j]))
				{
					if (Random.Range(0, num) == 0)
					{
						result = zoneBasedObject2;
					}
					num++;
					break;
				}
			}
		}
		return result;
	}
}
