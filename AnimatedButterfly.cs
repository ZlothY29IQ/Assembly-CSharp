using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

public struct AnimatedButterfly
{
	private struct TimedDestination
	{
		public float syncTime;

		public float syncEndTime;

		public GameObject destination;
	}

	private List<TimedDestination> destinationCache;

	private TimedDestination destinationA;

	private TimedDestination destinationB;

	private float loopDuration;

	private Vector3 oldPosition;

	private Vector3 velocity;

	public MeshRenderer visual;

	private Material material;

	private float speed;

	private float maxTravelTime;

	private Quaternion travellingLocalRotation;

	private float baseFlapSpeed;

	private bool wasPerched;

	public void UpdateVisual(float syncTime, ButterflySwarmManager manager)
	{
		if (destinationCache == null)
		{
			return;
		}
		syncTime %= loopDuration;
		GetPositionAndDestinationAtTime(syncTime, out var idealPosition, out var destination);
		Vector3 target = (destination - oldPosition).normalized * speed;
		velocity = Vector3.MoveTowards(velocity * manager.BeeJitterDamping, target, manager.BeeAcceleration * Time.deltaTime);
		float sqrMagnitude = (oldPosition - destination).sqrMagnitude;
		if (sqrMagnitude < manager.BeeNearDestinationRadius * manager.BeeNearDestinationRadius)
		{
			visual.transform.position = Vector3.MoveTowards(visual.transform.position, destination, Time.deltaTime);
			visual.transform.rotation = destinationB.destination.transform.rotation;
			if (sqrMagnitude < 1E-07f && !wasPerched)
			{
				material.SetFloat(ShaderProps._VertexFlapSpeed, manager.PerchedFlapSpeed);
				material.SetFloat(ShaderProps._VertexFlapPhaseOffset, manager.PerchedFlapPhase);
				wasPerched = true;
			}
		}
		else
		{
			if (wasPerched)
			{
				material.SetFloat(ShaderProps._VertexFlapSpeed, baseFlapSpeed);
				material.SetFloat(ShaderProps._VertexFlapPhaseOffset, 0f);
				wasPerched = false;
			}
			velocity += Random.insideUnitSphere * manager.BeeJitterStrength * Time.deltaTime;
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
				visual.transform.rotation = Quaternion.LookRotation(destination - vector) * travellingLocalRotation;
			}
		}
		oldPosition = visual.transform.position;
	}

	public void GetPositionAndDestinationAtTime(float syncTime, out Vector3 idealPosition, out Vector3 destination)
	{
		if (syncTime > destinationB.syncEndTime || syncTime < destinationA.syncTime || (object)destinationA.destination == null || (object)destinationB.destination == null)
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
					idealPosition = destinationCache[num3].destination.transform.position;
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
		destination = destinationB.destination.transform.position;
		idealPosition = Vector3.Lerp(destinationA.destination.transform.position, destination, t);
	}

	public void InitVisual(MeshRenderer prefab, ButterflySwarmManager manager)
	{
		visual = Object.Instantiate(prefab, manager.transform);
		material = visual.material;
		material.SetFloat(ShaderProps._VertexFlapPhaseOffset, 0f);
	}

	public void SetColor(Color color)
	{
		material.SetColor(ShaderProps._BaseColor, color);
	}

	public void SetFlapSpeed(float flapSpeed)
	{
		material.SetFloat(ShaderProps._VertexFlapSpeed, flapSpeed);
		baseFlapSpeed = flapSpeed;
	}

	public void InitRoute(List<GameObject> route, List<float> holdTimes, ButterflySwarmManager manager)
	{
		speed = manager.BeeSpeed;
		maxTravelTime = manager.BeeMaxTravelTime;
		travellingLocalRotation = manager.TravellingLocalRotation;
		destinationCache = new List<TimedDestination>(route.Count + 1);
		destinationCache.Clear();
		destinationCache.Add(new TimedDestination
		{
			syncTime = 0f,
			syncEndTime = 0f,
			destination = route[0]
		});
		float num = 0f;
		for (int i = 1; i < route.Count; i++)
		{
			float a = (route[i].transform.position - route[i - 1].transform.position).magnitude / speed;
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
		num += Mathf.Min((route[0].transform.position - route[route.Count - 1].transform.position).magnitude / speed, maxTravelTime);
		float num3 = holdTimes[0];
		destinationCache.Add(new TimedDestination
		{
			syncTime = num,
			syncEndTime = num + num3,
			destination = route[0]
		});
		loopDuration = num + (route[0].transform.position - route[route.Count - 1].transform.position).magnitude * manager.BeeSpeed + holdTimes[0];
	}
}
