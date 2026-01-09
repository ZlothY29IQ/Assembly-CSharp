using System.Collections.Generic;
using UnityEngine;

public class SpawnRegion<TItem, TRegion> : MonoBehaviour where TItem : Object where TRegion : SpawnRegion<TItem, TRegion>
{
	private static List<TRegion> _regions = new List<TRegion>();

	private static Dictionary<int, TRegion> _regionLookup = new Dictionary<int, TRegion>();

	private static Dictionary<TItem, int> _itemRegionLookup = new Dictionary<TItem, int>();

	[SerializeField]
	private float _scale = 10f;

	[SerializeField]
	[Tooltip("If set, spawn points will be created via raycasts from one of these points.")]
	private Transform[] spawnOrigins;

	[SerializeField]
	[Tooltip("If set, all spawn points will be tested against this transform to see if they're inside geo.  Ignored if spawn origins are configured.")]
	private Transform geoTestPoint;

	private List<TItem> _items = new List<TItem>();

	private bool _useSpawnOrigins;

	private bool _testAgainstGeo;

	private RaycastHit[] _hitTestBuffer;

	public static List<TRegion> Regions => _regions;

	public int MaxItems { get; private set; } = 10;

	private bool HasSpawnOrigins
	{
		get
		{
			Transform[] array = spawnOrigins;
			if (array == null)
			{
				return false;
			}
			return array.Length != 0;
		}
	}

	public List<TItem> Items => _items;

	public int ItemCount => _items.Count;

	public int ID { get; private set; }

	private void OnEnable()
	{
		Transform[] array = spawnOrigins;
		_useSpawnOrigins = array != null && array.Length != 0;
		_testAgainstGeo = !_useSpawnOrigins && (bool)geoTestPoint;
		if (_testAgainstGeo && _hitTestBuffer == null)
		{
			_hitTestBuffer = new RaycastHit[20];
		}
		RegisterRegion((TRegion)this);
	}

	private void OnDisable()
	{
		UnregisterRegion((TRegion)this);
		foreach (TItem item in _items)
		{
			if ((bool)item)
			{
				_itemRegionLookup.Remove(item);
			}
		}
		_items.Clear();
	}

	private static void RegisterRegion(TRegion region)
	{
		_regionLookup[region.ID] = region;
		_regions.Add(region);
	}

	private static void UnregisterRegion(TRegion region)
	{
		_regionLookup.Remove(region.ID);
		_regions.Remove(region);
	}

	public static void AddItemToRegion(TItem item, int regionId)
	{
		if (_regionLookup.TryGetValue(regionId, out var value))
		{
			value.AddItem(item);
		}
	}

	public static void RemoveItemFromRegion(TItem item)
	{
		if (_itemRegionLookup.TryGetValue(item, out var value) && _regionLookup.TryGetValue(value, out var value2))
		{
			value2.RemoveItem(item);
		}
	}

	public void AddItem(TItem item)
	{
		_items.Add(item);
		_itemRegionLookup[item] = ID;
	}

	public void RemoveItem(TItem item)
	{
		_items.Remove(item);
		_itemRegionLookup.Remove(item);
	}

	public (bool isOnGround, Vector3 position, Vector3 normal) GetSpawnPointWithNormal(int maxTries = 5)
	{
		for (int i = 0; i < maxTries; i++)
		{
			if (TryGetSpawnPoint(out var spawnPoint))
			{
				return (isOnGround: true, position: spawnPoint.point, normal: spawnPoint.normal);
			}
		}
		float num = _scale / 2f;
		Vector3 item = base.transform.TransformPoint(new Vector3(Random.Range(0f - num, num), num, Random.Range(0f - num, num)));
		return (isOnGround: false, position: item, normal: Vector3.up);
	}

	private bool TryGetSpawnPoint(out RaycastHit spawnPoint)
	{
		float num = base.transform.lossyScale.y * _scale;
		Vector3 position;
		if (_useSpawnOrigins)
		{
			position = spawnOrigins[Random.Range(0, spawnOrigins.Length)].position;
			if (TryGetSpawnPoint(position, Random.onUnitSphere, Mathf.Max(num, 100f), out spawnPoint))
			{
				if (!(spawnPoint.normal.y > 0f))
				{
					return TryGetSpawnPoint(spawnPoint.point, Vector3.down, num, out spawnPoint);
				}
				return true;
			}
			spawnPoint = default(RaycastHit);
			return false;
		}
		float num2 = _scale / 2f;
		position = base.transform.TransformPoint(new Vector3(Random.Range(0f - num2, num2), num2, Random.Range(0f - num2, num2)));
		if (_testAgainstGeo && IsInsideGeo(position))
		{
			spawnPoint = default(RaycastHit);
			return false;
		}
		return TryGetSpawnPoint(position, Vector3.down, num, out spawnPoint);
	}

	private bool TryGetSpawnPoint(Vector3 origin, Vector3 direction, float distance, out RaycastHit spawnPoint)
	{
		if (Physics.Raycast(origin, direction, out var hitInfo, distance, -1, QueryTriggerInteraction.Ignore))
		{
			Debug.DrawLine(origin, hitInfo.point, Color.green, 5f);
			spawnPoint = hitInfo;
			return true;
		}
		Debug.DrawLine(origin, origin + direction * distance, Color.red, 5f);
		spawnPoint = default(RaycastHit);
		return false;
	}

	private bool IsInsideGeo(Vector3 point)
	{
		Vector3 position = geoTestPoint.position;
		Vector3 vector = position - point;
		int num;
		int num2;
		while (true)
		{
			num = Physics.RaycastNonAlloc(point, vector, _hitTestBuffer, vector.magnitude, -1, QueryTriggerInteraction.Ignore);
			num2 = Physics.RaycastNonAlloc(position, -vector, _hitTestBuffer, vector.magnitude, -1, QueryTriggerInteraction.Ignore);
			if (num < _hitTestBuffer.Length && num2 < _hitTestBuffer.Length)
			{
				break;
			}
			_hitTestBuffer = new RaycastHit[_hitTestBuffer.Length * 2];
		}
		bool flag = (num + num2) % 2 != 0;
		Debug.DrawLine(point, position, flag ? Color.red : Color.green, 5f);
		return flag;
	}
}
