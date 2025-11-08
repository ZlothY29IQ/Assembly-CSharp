using System;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRSenseLineOfSight
{
	public enum RaycastMode
	{
		Geometry,
		Navmesh,
		GeometryAndNavMesh,
		GeometryOrNavMesh
	}

	public float sightDist;

	public LayerMask visibilityMask;

	public RaycastMode rayCastMode;

	public static RaycastHit[] visibilityHits = new RaycastHit[16];

	public bool HasLineOfSight(Vector3 headPos, Vector3 targetPos)
	{
		return HasLineOfSight(headPos, targetPos, sightDist, visibilityMask.value, rayCastMode);
	}

	public static bool HasLineOfSight(Vector3 headPos, Vector3 targetPos, float sightDist, int layerMask, RaycastMode rayCastMode = RaycastMode.Geometry)
	{
		switch (rayCastMode)
		{
		case RaycastMode.Geometry:
			return HasGeoLineOfSight(headPos, targetPos, sightDist, layerMask);
		case RaycastMode.Navmesh:
			return HasNavmeshLineOfSight(headPos, targetPos, sightDist);
		case RaycastMode.GeometryAndNavMesh:
			if (HasGeoLineOfSight(headPos, targetPos, sightDist, layerMask))
			{
				return HasNavmeshLineOfSight(headPos, targetPos, sightDist);
			}
			return false;
		case RaycastMode.GeometryOrNavMesh:
			if (!HasNavmeshLineOfSight(headPos, targetPos, sightDist))
			{
				return HasGeoLineOfSight(headPos, targetPos, sightDist, layerMask);
			}
			return true;
		default:
			return false;
		}
	}

	public static bool HasGeoLineOfSight(Vector3 headPos, Vector3 targetPos, float sightDist, int layerMask)
	{
		float num = Vector3.Distance(targetPos, headPos);
		if (num > sightDist)
		{
			return false;
		}
		return Physics.RaycastNonAlloc(new Ray(headPos, targetPos - headPos), visibilityHits, Mathf.Min(num, sightDist), layerMask, QueryTriggerInteraction.Ignore) < 1;
	}

	public static bool HasNavmeshLineOfSight(Vector3 headPos, Vector3 targetPos, float sightDist)
	{
		if ((targetPos - headPos).sqrMagnitude > sightDist * sightDist)
		{
			return false;
		}
		NavMeshHit hit2;
		if (NavMesh.SamplePosition(headPos, out var hit, 1f, -1))
		{
			return !NavMesh.Raycast(hit.position, targetPos, out hit2, -1);
		}
		return false;
	}
}
