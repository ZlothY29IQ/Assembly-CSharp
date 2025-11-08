using GorillaLocomotion.Climbing;
using Photon.Pun;
using UnityEngine;

namespace GorillaLocomotion.Swimming;

public struct WaterOverlappingCollider
{
	public bool playBigSplash;

	public bool playDripEffect;

	public bool overrideBoundingRadius;

	public float boundingRadiusOverride;

	public float scaleMultiplier;

	public Collider collider;

	public GorillaVelocityTracker velocityTracker;

	public WaterVolume.SurfaceQuery lastSurfaceQuery;

	public NetworkView photonViewForRPC;

	public bool surfaceDetected;

	public bool inWater;

	public bool inVolume;

	public float lastBoundingRadius;

	public Vector3 lastRipplePosition;

	public float lastRippleScale;

	public float lastRippleTime;

	public float lastInWaterTime;

	public float nextDripTime;

	public void PlayRippleEffect(GameObject rippleEffectPrefab, Vector3 surfacePoint, Vector3 surfaceNormal, float defaultRippleScale, float currentTime, WaterVolume volume)
	{
		lastRipplePosition = GetClosestPositionOnSurface(surfacePoint, surfaceNormal);
		lastBoundingRadius = GetBoundingRadiusOnSurface(surfaceNormal);
		lastRippleScale = defaultRippleScale * lastBoundingRadius * 2f * scaleMultiplier;
		lastRippleTime = currentTime;
		ObjectPools.instance.Instantiate(rippleEffectPrefab, lastRipplePosition, Quaternion.FromToRotation(Vector3.up, lastSurfaceQuery.surfaceNormal) * Quaternion.AngleAxis(-90f, Vector3.right), lastRippleScale).GetComponent<WaterRippleEffect>().PlayEffect(volume);
	}

	public void PlaySplashEffect(GameObject splashEffectPrefab, Vector3 splashPosition, float splashScale, bool bigSplash, bool enteringWater, WaterVolume volume)
	{
		Quaternion quaternion = Quaternion.FromToRotation(Vector3.up, lastSurfaceQuery.surfaceNormal) * Quaternion.AngleAxis(-90f, Vector3.right);
		ObjectPools.instance.Instantiate(splashEffectPrefab, splashPosition, quaternion, splashScale * scaleMultiplier).GetComponent<WaterSplashEffect>().PlayEffect(bigSplash, enteringWater, scaleMultiplier, volume);
		if (!(photonViewForRPC != null))
		{
			return;
		}
		float time = Time.time;
		int num = -1;
		float num2 = time + 10f;
		for (int i = 0; i < WaterVolume.splashRPCSendTimes.Length; i++)
		{
			if (WaterVolume.splashRPCSendTimes[i] < num2)
			{
				num2 = WaterVolume.splashRPCSendTimes[i];
				num = i;
			}
		}
		if (time - 0.5f > num2)
		{
			WaterVolume.splashRPCSendTimes[num] = time;
			photonViewForRPC.SendRPC("RPC_PlaySplashEffect", RpcTarget.Others, splashPosition, quaternion, splashScale * scaleMultiplier, lastBoundingRadius, bigSplash, enteringWater);
		}
	}

	public void PlayDripEffect(GameObject rippleEffectPrefab, Vector3 surfacePoint, Vector3 surfaceNormal, float dripScale)
	{
		Vector3 closestPositionOnSurface = GetClosestPositionOnSurface(surfacePoint, surfaceNormal);
		float num = (overrideBoundingRadius ? boundingRadiusOverride : lastBoundingRadius);
		Vector3 vector = Vector3.ProjectOnPlane(Random.onUnitSphere * num * 0.5f, surfaceNormal);
		ObjectPools.instance.Instantiate(rippleEffectPrefab, closestPositionOnSurface + vector, Quaternion.FromToRotation(Vector3.up, lastSurfaceQuery.surfaceNormal) * Quaternion.AngleAxis(-90f, Vector3.right), dripScale * scaleMultiplier);
	}

	public Vector3 GetClosestPositionOnSurface(Vector3 surfacePoint, Vector3 surfaceNormal)
	{
		return Vector3.ProjectOnPlane(collider.transform.position - surfacePoint, surfaceNormal) + surfacePoint;
	}

	private float GetBoundingRadiusOnSurface(Vector3 surfaceNormal)
	{
		if (overrideBoundingRadius)
		{
			lastBoundingRadius = boundingRadiusOverride;
			return boundingRadiusOverride;
		}
		Vector3 extents = collider.bounds.extents;
		Vector3 vector = Vector3.ProjectOnPlane(collider.transform.right * extents.x, surfaceNormal);
		Vector3 vector2 = Vector3.ProjectOnPlane(collider.transform.up * extents.y, surfaceNormal);
		Vector3 vector3 = Vector3.ProjectOnPlane(collider.transform.forward * extents.z, surfaceNormal);
		float sqrMagnitude = vector.sqrMagnitude;
		float sqrMagnitude2 = vector2.sqrMagnitude;
		float sqrMagnitude3 = vector3.sqrMagnitude;
		if (sqrMagnitude >= sqrMagnitude2 && sqrMagnitude >= sqrMagnitude3)
		{
			return vector.magnitude;
		}
		if (sqrMagnitude2 >= sqrMagnitude && sqrMagnitude2 >= sqrMagnitude3)
		{
			return vector2.magnitude;
		}
		return vector3.magnitude;
	}
}
