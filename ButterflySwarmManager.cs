using System.Collections.Generic;
using UnityEngine;

public class ButterflySwarmManager : MonoBehaviour
{
	[SerializeField]
	private XSceneRef[] perchSections;

	[SerializeField]
	private int loopSizePerBee;

	[SerializeField]
	private int numBees;

	[SerializeField]
	private MeshRenderer beePrefab;

	[SerializeField]
	private float maxFlapSpeed;

	[SerializeField]
	private float minFlapSpeed;

	private List<AnimatedButterfly> butterflies;

	private List<List<GameObject>> allPerchZones = new List<List<GameObject>>();

	[field: SerializeField]
	public float PerchedFlapSpeed { get; private set; }

	[field: SerializeField]
	public float PerchedFlapPhase { get; private set; }

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
	[field: Tooltip(">0 to get butterflies to align to their destination rotation as they land")]
	public float DestRotationAlignmentSpeed { get; private set; }

	[field: SerializeField]
	[field: Tooltip("Model orientation relative to the direction vector while flying")]
	public Vector3 TravellingLocalRotationEuler { get; private set; }

	public Quaternion TravellingLocalRotation { get; private set; }

	[field: SerializeField]
	public float AvoidPointRadius { get; private set; }

	[field: SerializeField]
	public float BeeMinFlowerDuration { get; private set; }

	[field: SerializeField]
	public float BeeMaxFlowerDuration { get; private set; }

	[field: SerializeField]
	public Color[] BeeColors { get; private set; }

	private void Awake()
	{
		TravellingLocalRotation = Quaternion.Euler(TravellingLocalRotationEuler);
		butterflies = new List<AnimatedButterfly>(numBees);
		for (int i = 0; i < numBees; i++)
		{
			AnimatedButterfly item = default(AnimatedButterfly);
			item.InitVisual(beePrefab, this);
			if (BeeColors.Length != 0)
			{
				item.SetColor(BeeColors[i % BeeColors.Length]);
			}
			butterflies.Add(item);
		}
	}

	private void Start()
	{
		XSceneRef[] array = perchSections;
		foreach (XSceneRef xSceneRef in array)
		{
			if (!xSceneRef.TryResolve(out GameObject result))
			{
				continue;
			}
			List<GameObject> list = new List<GameObject>();
			allPerchZones.Add(list);
			foreach (Transform item in result.transform)
			{
				list.Add(item.gameObject);
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
		for (int i = 0; i < butterflies.Count; i++)
		{
			AnimatedButterfly value = butterflies[i];
			value.UpdateVisual(RandomTimedSeedManager.instance.currentSyncTime, this);
			butterflies[i] = value;
		}
	}

	private void OnSeedChange()
	{
		SRand rand = new SRand(RandomTimedSeedManager.instance.seed);
		List<List<GameObject>> list = new List<List<GameObject>>(allPerchZones.Count);
		for (int i = 0; i < allPerchZones.Count; i++)
		{
			List<GameObject> list2 = new List<GameObject>();
			list2.AddRange(allPerchZones[i]);
			list.Add(list2);
		}
		List<GameObject> list3 = new List<GameObject>(loopSizePerBee);
		List<float> list4 = new List<float>(loopSizePerBee);
		for (int j = 0; j < butterflies.Count; j++)
		{
			AnimatedButterfly value = butterflies[j];
			value.SetFlapSpeed(rand.NextFloat(minFlapSpeed, maxFlapSpeed));
			list3.Clear();
			list4.Clear();
			PickPoints(loopSizePerBee, list, ref rand, list3);
			for (int k = 0; k < list3.Count; k++)
			{
				list4.Add(rand.NextFloat(BeeMinFlowerDuration, BeeMaxFlowerDuration));
			}
			if (list3.Count == 0)
			{
				butterflies.Clear();
				break;
			}
			value.InitRoute(list3, list4, this);
			butterflies[j] = value;
		}
	}

	private void PickPoints(int n, List<List<GameObject>> pickBuffer, ref SRand rand, List<GameObject> resultBuffer)
	{
		int exclude = rand.NextInt(0, pickBuffer.Count);
		int num = -1;
		int num2 = n - 2;
		while (resultBuffer.Count < n)
		{
			int num3 = ((resultBuffer.Count >= num2) ? rand.NextIntWithExclusion2(0, pickBuffer.Count, num, exclude) : rand.NextIntWithExclusion(0, pickBuffer.Count, num));
			int num4 = 10;
			while (num3 == num || pickBuffer[num3].Count == 0)
			{
				num3 = (num3 + 1) % pickBuffer.Count;
				num4--;
				if (num4 <= 0)
				{
					return;
				}
			}
			num = num3;
			List<GameObject> list = pickBuffer[num];
			while (list.Count == 0)
			{
				num = (num + 1) % pickBuffer.Count;
				list = pickBuffer[num];
			}
			resultBuffer.Add(list[list.Count - 1]);
			list.RemoveAt(list.Count - 1);
		}
	}
}
