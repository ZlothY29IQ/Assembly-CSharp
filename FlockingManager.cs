using System;
using System.Collections.Generic;
using Fusion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

[NetworkBehaviourWeaved(337)]
public class FlockingManager : NetworkComponent
{
	public class FishArea
	{
		public string id;

		public List<Flocking> fishList = new List<Flocking>();

		public Vector3 colliderCenter;

		public BoxCollider[] colliders;

		public Vector3 nextWaypoint = Vector3.zero;

		public ZoneBasedObject zoneBasedObject;
	}

	public class FishFood
	{
		public BoxCollider collider;

		public bool isRealFood;

		public SlingshotProjectile slingshotProjectile;
	}

	public List<GameObject> fishAreaContainer;

	public string foodProjectileTag = "WaterBalloonProjectile";

	private Dictionary<string, Vector3> areaToWaypointDict = new Dictionary<string, Vector3>();

	private List<FishArea> fishAreaList = new List<FishArea>();

	private List<Flocking> allFish = new List<Flocking>();

	public UnityAction<FishFood> onFoodDetected;

	public UnityAction<BoxCollider> onFoodDestroyed;

	private bool hasBeenSerialized;

	public static readonly List<GameObject> avoidPoints = new List<GameObject>();

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 337)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private FlockingData _Data;

	[Networked]
	[NetworkedWeaved(0, 337)]
	public unsafe FlockingData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing FlockingManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(FlockingData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing FlockingManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(FlockingData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		foreach (GameObject item in fishAreaContainer)
		{
			Flocking[] componentsInChildren = item.GetComponentsInChildren<Flocking>(includeInactive: false);
			FishArea fishArea = new FishArea();
			fishArea.id = item.name;
			fishArea.colliders = item.GetComponentsInChildren<BoxCollider>();
			fishArea.colliderCenter = fishArea.colliders[0].bounds.center;
			fishArea.fishList.AddRange(componentsInChildren);
			fishArea.zoneBasedObject = item.GetComponent<ZoneBasedObject>();
			areaToWaypointDict[fishArea.id] = Vector3.zero;
			Flocking[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].FishArea = fishArea;
			}
			fishAreaList.Add(fishArea);
			allFish.AddRange(fishArea.fishList);
			SlingshotProjectileHitNotifier component = item.GetComponent<SlingshotProjectileHitNotifier>();
			if (component != null)
			{
				component.OnProjectileTriggerEnter += ProjectileHitReceiver;
				component.OnProjectileTriggerExit += ProjectileHitExit;
			}
			else
			{
				Debug.LogError("Needs SlingshotProjectileHitNotifier added to each fish area");
			}
		}
	}

	private new void Start()
	{
		NetworkSystem.Instance.RegisterSceneNetworkItem(base.gameObject);
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		fishAreaList.Clear();
		areaToWaypointDict.Clear();
		allFish.Clear();
		foreach (GameObject item in fishAreaContainer)
		{
			SlingshotProjectileHitNotifier component = item.GetComponent<SlingshotProjectileHitNotifier>();
			if (component != null)
			{
				component.OnProjectileTriggerExit -= ProjectileHitExit;
				component.OnProjectileTriggerEnter -= ProjectileHitReceiver;
			}
		}
	}

	private void Update()
	{
		if (UnityEngine.Random.Range(0, 10000) >= 50)
		{
			return;
		}
		foreach (FishArea fishArea in fishAreaList)
		{
			if (fishArea.zoneBasedObject != null)
			{
				fishArea.zoneBasedObject.gameObject.SetActive(fishArea.zoneBasedObject.IsLocalPlayerInZone());
			}
			fishArea.nextWaypoint = GetRandomPointInsideCollider(fishArea);
			areaToWaypointDict[fishArea.id] = fishArea.nextWaypoint;
			Debug.DrawLine(fishArea.nextWaypoint, Vector3.forward * 5f, Color.magenta);
		}
	}

	public Vector3 GetRandomPointInsideCollider(FishArea fishArea)
	{
		int num = UnityEngine.Random.Range(0, fishArea.colliders.Length);
		BoxCollider obj = fishArea.colliders[num];
		Vector3 vector = obj.size / 2f;
		Vector3 position = new Vector3(UnityEngine.Random.Range(0f - vector.x, vector.x), UnityEngine.Random.Range(0f - vector.y, vector.y), UnityEngine.Random.Range(0f - vector.z, vector.z));
		return obj.transform.TransformPoint(position);
	}

	public bool IsInside(Vector3 point, FishArea fish)
	{
		BoxCollider[] colliders = fish.colliders;
		foreach (BoxCollider obj in colliders)
		{
			Vector3 center = obj.center;
			Vector3 vector = obj.transform.InverseTransformPoint(point);
			vector -= center;
			Vector3 size = obj.size;
			if (Mathf.Abs(vector.x) < size.x / 2f && Mathf.Abs(vector.y) < size.y / 2f && Mathf.Abs(vector.z) < size.z / 2f)
			{
				return true;
			}
		}
		return false;
	}

	public Vector3 RestrictPointToArea(Vector3 point, FishArea fish)
	{
		Vector3 result = default(Vector3);
		float num = float.MaxValue;
		BoxCollider[] colliders = fish.colliders;
		foreach (BoxCollider boxCollider in colliders)
		{
			Vector3 center = boxCollider.center;
			Vector3 vector = boxCollider.transform.InverseTransformPoint(point);
			Vector3 vector2 = vector - center;
			Vector3 size = boxCollider.size;
			float num2 = size.x / 2f;
			float num3 = size.y / 2f;
			float num4 = size.z / 2f;
			if (Mathf.Abs(vector2.x) < num2 && Mathf.Abs(vector2.y) < num3 && Mathf.Abs(vector2.z) < num4)
			{
				return point;
			}
			Vector3 vector3 = new Vector3(center.x - num2, center.y - num3, center.z - num4);
			Vector3 vector4 = new Vector3(center.x + num2, center.y + num3, center.z + num4);
			Vector3 vector5 = new Vector3(Mathf.Clamp(vector.x, vector3.x, vector4.x), Mathf.Clamp(vector.y, vector3.y, vector4.y), Mathf.Clamp(vector.z, vector3.z, vector4.z));
			float num5 = Vector3.Distance(vector, vector5);
			if (num5 < num)
			{
				num = num5;
				if (num5 > 1f)
				{
					Vector3 vector6 = Vector3.Normalize(vector - vector5);
					result = boxCollider.transform.TransformPoint(vector5 + vector6 * 1f);
				}
				else
				{
					result = point;
				}
			}
		}
		return result;
	}

	private void ProjectileHitReceiver(SlingshotProjectile projectile, Collider collider1)
	{
		bool isRealFood = projectile.CompareTag(foodProjectileTag);
		FishFood arg = new FishFood
		{
			collider = (collider1 as BoxCollider),
			isRealFood = isRealFood,
			slingshotProjectile = projectile
		};
		onFoodDetected?.Invoke(arg);
	}

	private void ProjectileHitExit(SlingshotProjectile projectile, Collider collider2)
	{
		onFoodDestroyed?.Invoke(collider2 as BoxCollider);
	}

	public override void WriteDataFusion()
	{
		Data = new FlockingData(allFish);
	}

	public override void ReadDataFusion()
	{
		for (int i = 0; i < Data.count; i++)
		{
			Vector3 syncPos = Data.Positions[i];
			Quaternion syncRot = Data.Rotations[i];
			allFish[i].SetSyncPosRot(syncPos, syncRot);
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public static void RegisterAvoidPoint(GameObject obj)
	{
		avoidPoints.Add(obj);
	}

	public static void UnregisterAvoidPoint(GameObject obj)
	{
		avoidPoints.Remove(obj);
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}
}
