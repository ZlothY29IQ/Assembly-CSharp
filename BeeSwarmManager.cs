using System.Collections.Generic;
using UnityEngine;

public class BeeSwarmManager : MonoBehaviour
{
	[SerializeField]
	private XSceneRef[] flowerSections;

	[SerializeField]
	private int loopSizePerBee;

	[SerializeField]
	private int numBees;

	[SerializeField]
	private MeshRenderer beePrefab;

	[SerializeField]
	private AudioSource nearbyBeeBuzz;

	[SerializeField]
	private AudioSource generalBeeBuzz;

	private GameObject[] flowerSectionsResolved;

	private List<AnimatedBee> bees;

	private Transform playerCamera;

	private List<BeePerchPoint> allPerchPoints = new List<BeePerchPoint>();

	public static readonly List<GameObject> avoidPoints = new List<GameObject>();

	[field: SerializeField]
	public BeePerchPoint BeeHive { get; private set; }

	[field: SerializeField]
	public float BeeSpeed { get; private set; }

	[field: SerializeField]
	public float BeeMaxTravelTime { get; private set; }

	[field: SerializeField]
	public float BeeAcceleration { get; private set; }

	[field: SerializeField]
	public float BeeJitterStrength { get; private set; }

	[field: SerializeField]
	[field: Tooltip("Should be 0-1; closer to 1 = less damping")]
	public float BeeJitterDamping { get; private set; }

	[field: SerializeField]
	[field: Tooltip("Limits how far the bee can get off course")]
	public float BeeMaxJitterRadius { get; private set; }

	[field: SerializeField]
	[field: Tooltip("Bees stop jittering when close to their destination")]
	public float BeeNearDestinationRadius { get; private set; }

	[field: SerializeField]
	public float AvoidPointRadius { get; private set; }

	[field: SerializeField]
	public float BeeMinFlowerDuration { get; private set; }

	[field: SerializeField]
	public float BeeMaxFlowerDuration { get; private set; }

	[field: SerializeField]
	public float GeneralBuzzRange { get; private set; }

	private void Awake()
	{
		bees = new List<AnimatedBee>(numBees);
		for (int i = 0; i < numBees; i++)
		{
			AnimatedBee item = default(AnimatedBee);
			item.InitVisual(beePrefab, this);
			bees.Add(item);
		}
		playerCamera = Camera.main.transform;
	}

	private void Start()
	{
		XSceneRef[] array = flowerSections;
		foreach (XSceneRef xSceneRef in array)
		{
			if (xSceneRef.TryResolve(out GameObject result))
			{
				BeePerchPoint[] componentsInChildren = result.GetComponentsInChildren<BeePerchPoint>();
				foreach (BeePerchPoint item in componentsInChildren)
				{
					allPerchPoints.Add(item);
				}
			}
		}
		OnSeedChange();
		RandomTimedSeedManager.instance.AddCallbackOnSeedChanged(OnSeedChange);
	}

	private void OnDestroy()
	{
		RandomTimedSeedManager.instance.RemoveCallbackOnSeedChanged(OnSeedChange);
	}

	private void Update()
	{
		Vector3 position = playerCamera.transform.position;
		Vector3 position2 = Vector3.zero;
		Vector3 zero = Vector3.zero;
		_ = 1f / (float)bees.Count;
		float num = float.PositiveInfinity;
		float num2 = GeneralBuzzRange * GeneralBuzzRange;
		int num3 = 0;
		for (int i = 0; i < bees.Count; i++)
		{
			AnimatedBee value = bees[i];
			value.UpdateVisual(RandomTimedSeedManager.instance.currentSyncTime, this);
			Vector3 position3 = value.visual.transform.position;
			float sqrMagnitude = (position3 - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				position2 = position3;
				num = sqrMagnitude;
			}
			if (sqrMagnitude < num2)
			{
				zero += position3;
				num3++;
			}
			bees[i] = value;
		}
		nearbyBeeBuzz.transform.position = position2;
		if (num3 > 0)
		{
			generalBeeBuzz.transform.position = zero / num3;
			generalBeeBuzz.enabled = true;
		}
		else
		{
			generalBeeBuzz.enabled = false;
		}
	}

	private void OnSeedChange()
	{
		SRand rand = new SRand(RandomTimedSeedManager.instance.seed);
		List<BeePerchPoint> pickBuffer = new List<BeePerchPoint>(allPerchPoints.Count);
		List<BeePerchPoint> list = new List<BeePerchPoint>(loopSizePerBee);
		List<float> list2 = new List<float>(loopSizePerBee);
		for (int i = 0; i < bees.Count; i++)
		{
			AnimatedBee value = bees[i];
			list = new List<BeePerchPoint>(loopSizePerBee);
			list2 = new List<float>(loopSizePerBee);
			PickPoints(loopSizePerBee, pickBuffer, allPerchPoints, ref rand, list);
			for (int j = 0; j < list.Count; j++)
			{
				list2.Add(rand.NextFloat(BeeMinFlowerDuration, BeeMaxFlowerDuration));
			}
			value.InitRoute(list, list2, this);
			value.InitRouteTimestamps();
			bees[i] = value;
		}
	}

	private void PickPoints(int n, List<BeePerchPoint> pickBuffer, List<BeePerchPoint> allPerchPoints, ref SRand rand, List<BeePerchPoint> resultBuffer)
	{
		resultBuffer.Add(BeeHive);
		n--;
		int num = 100;
		while (pickBuffer.Count < n && num-- > 0)
		{
			n -= pickBuffer.Count;
			resultBuffer.AddRange(pickBuffer);
			pickBuffer.Clear();
			pickBuffer.AddRange(allPerchPoints);
			rand.Shuffle(pickBuffer);
		}
		resultBuffer.AddRange(pickBuffer.GetRange(pickBuffer.Count - n, n));
		pickBuffer.RemoveRange(pickBuffer.Count - n, n);
	}

	public static void RegisterAvoidPoint(GameObject obj)
	{
		avoidPoints.Add(obj);
	}

	public static void UnregisterAvoidPoint(GameObject obj)
	{
		avoidPoints.Remove(obj);
	}
}
