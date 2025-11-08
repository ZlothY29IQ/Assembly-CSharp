using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

public struct AnimatedBee
{
	private struct TimedDestination
	{
		public float syncTime;

		public float syncEndTime;

		public BeePerchPoint destination;
	}

	private List<TimedDestination> destinationCache;

	private TimedDestination destinationA;

	private TimedDestination destinationB;

	private float loopDuration;

	private Vector3 oldPosition;

	private Vector3 velocity;

	public MeshRenderer visual;

	private float oldSyncTime;

	private List<BeePerchPoint> route;

	private List<float> holdTimes;

	private float speed;

	private float maxTravelTime;

	public void UpdateVisual(float syncTime, BeeSwarmManager manager)
	{
		if (destinationCache == null)
		{
			return;
		}
		syncTime %= loopDuration;
		if (syncTime < oldSyncTime)
		{
			InitRouteTimestamps();
		}
		GetPositionAndDestinationAtTime(syncTime, out var idealPosition, out var destination);
		Vector3 target = (destination - oldPosition).normalized * speed;
		velocity = Vector3.MoveTowards(velocity * manager.BeeJitterDamping, target, manager.BeeAcceleration * Time.deltaTime);
		if ((oldPosition - destination).IsLongerThan(manager.BeeNearDestinationRadius))
		{
			velocity += Random.insideUnitSphere * manager.BeeJitterStrength * Time.deltaTime;
		}
		Vector3 vector = oldPosition + velocity * Time.deltaTime;
		if ((vector - idealPosition).IsLongerThan(manager.BeeMaxJitterRadius))
		{
			vector = idealPosition + (vector - idealPosition).normalized * manager.BeeMaxJitterRadius;
			velocity = (vector - oldPosition) / Time.deltaTime;
		}
		foreach (GameObject avoidPoint in BeeSwarmManager.avoidPoints)
		{
			Vector3 position = avoidPoint.transform.position;
			if ((vector - position).IsShorterThan(manager.AvoidPointRadius))
			{
				Vector3 normalized = Vector3.Cross(position - vector, destination - vector).normalized;
				_ = (destination - position).normalized;
				float num = Vector3.Dot(vector - position, normalized);
				Vector3 vector2 = (manager.AvoidPointRadius - num) * normalized;
				vector += vector2;
				velocity += vector2;
			}
		}
		visual.transform.position = vector;
		if ((destination - vector).IsLongerThan(0.01f))
		{
			visual.transform.rotation = Quaternion.LookRotation(Vector3.up, vector - destination);
		}
		oldPosition = vector;
		oldSyncTime = syncTime;
	}

	public void GetPositionAndDestinationAtTime(float syncTime, out Vector3 idealPosition, out Vector3 destination)
	{
		if (syncTime > destinationB.syncEndTime || syncTime < destinationA.syncTime)
		{
			int num = 0;
			int num2 = destinationCache.Count - 1;
			while (num + 1 < num2)
			{
				int num3 = (num + num2) / 2;
				float syncTime2 = destinationCache[num3].syncTime;
				float syncEndTime = destinationCache[num3].syncEndTime;
				if (syncTime2 <= syncTime && syncEndTime >= syncTime)
				{
					idealPosition = destinationCache[num3].destination.GetPoint();
					destination = idealPosition;
				}
				if (syncEndTime < syncTime)
				{
					num = num3;
				}
				else
				{
					num2 = num3;
				}
			}
			destinationA = destinationCache[num];
			destinationB = destinationCache[num2];
		}
		float t = Mathf.InverseLerp(destinationA.syncEndTime, destinationB.syncTime, syncTime);
		destination = destinationB.destination.GetPoint();
		idealPosition = Vector3.Lerp(destinationA.destination.GetPoint(), destination, t);
	}

	public void InitVisual(MeshRenderer prefab, BeeSwarmManager manager)
	{
		visual = Object.Instantiate(prefab, manager.transform);
	}

	public void InitRouteTimestamps()
	{
		destinationB.syncEndTime = -1f;
		destinationCache.Clear();
		destinationCache.Add(new TimedDestination
		{
			syncTime = 0f,
			destination = route[0]
		});
		float num = 0f;
		for (int i = 1; i < route.Count; i++)
		{
			if (route[i].enabled)
			{
				float a = (route[i].transform.position - route[i - 1].transform.position).magnitude * speed;
				a = Mathf.Min(a, maxTravelTime);
				num += a;
				float num2 = holdTimes[i];
				destinationCache.Add(new TimedDestination
				{
					syncTime = num,
					syncEndTime = num + num2,
					destination = route[i]
				});
				num += num2;
			}
		}
		num += Mathf.Min((route[0].transform.position - route[route.Count - 1].transform.position).magnitude * speed, maxTravelTime);
		float num3 = holdTimes[0];
		destinationCache.Add(new TimedDestination
		{
			syncTime = num,
			syncEndTime = num + num3,
			destination = route[0]
		});
	}

	public void InitRoute(List<BeePerchPoint> route, List<float> holdTimes, BeeSwarmManager manager)
	{
		this.route = route;
		this.holdTimes = holdTimes;
		speed = manager.BeeSpeed;
		maxTravelTime = manager.BeeMaxTravelTime;
		destinationCache = new List<TimedDestination>(route.Count + 1);
		float num = 0f;
		for (int i = 1; i < route.Count; i++)
		{
			num += (route[i].transform.position - route[i - 1].transform.position).magnitude * manager.BeeSpeed + holdTimes[i];
		}
		loopDuration = num + (route[0].transform.position - route[route.Count - 1].transform.position).magnitude * manager.BeeSpeed + holdTimes[0];
	}
}
