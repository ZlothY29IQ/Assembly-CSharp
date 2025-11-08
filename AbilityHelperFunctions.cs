using UnityEngine;
using UnityEngine.AI;

public static class AbilityHelperFunctions
{
	private static int navMeshWalkableArea = -1;

	public static float EaseOutPower(float t, float power)
	{
		return 1f - Mathf.Pow(1f - t, power);
	}

	public static int RandomRangeUnique(int minInclusive, int maxExclusive, int lastValue)
	{
		int num = maxExclusive - minInclusive;
		if (num <= 1)
		{
			return minInclusive;
		}
		int num2 = Random.Range(minInclusive, maxExclusive);
		if (num2 != lastValue)
		{
			return num2;
		}
		return (num2 + 1) % num;
	}

	public static int GetNavMeshWalkableArea()
	{
		if (navMeshWalkableArea == -1)
		{
			navMeshWalkableArea = NavMesh.GetAreaFromName("walkable");
		}
		return navMeshWalkableArea;
	}

	public static Vector3? GetLocationToInvestigate(Vector3 listenerLocation, float hearingRadius, Vector3? currentInvestigationLocation)
	{
		if (GRNoiseEventManager.instance.GetMostRecentNoiseEventInRadius(listenerLocation, hearingRadius, out var outEvent) && NavMesh.SamplePosition(outEvent.position, out var hit, 1f, GetNavMeshWalkableArea()))
		{
			return hit.position;
		}
		return currentInvestigationLocation ?? ((Vector3?)null);
	}
}
