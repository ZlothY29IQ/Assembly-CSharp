using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(5555)]
public class ZoneGraph : MonoBehaviour
{
	[SerializeField]
	private ZoneDef[] _zoneDefs = new ZoneDef[0];

	[SerializeField]
	private BoxCollider[] _colliders = new BoxCollider[0];

	[SerializeField]
	private ZoneNode[] _nodes = new ZoneNode[0];

	[NonSerialized]
	[Space]
	private Dictionary<BoxCollider, ZoneDef> _colliderToZoneDef = new Dictionary<BoxCollider, ZoneDef>(64);

	[NonSerialized]
	[Space]
	private Dictionary<BoxCollider, ZoneNode> _colliderToNode = new Dictionary<BoxCollider, ZoneNode>(64);

	[NonSerialized]
	[Space]
	private List<ZoneEntity> _entityList = new List<ZoneEntity>(16);

	private static ZoneGraph gGraph;

	private bool _compiledGraph;

	public static ZoneGraph Instance => gGraph;

	public static ZoneDef ColliderToZoneDef(BoxCollider collider)
	{
		if (!(collider == null))
		{
			return gGraph._colliderToZoneDef[collider];
		}
		return null;
	}

	public static ZoneNode ColliderToNode(BoxCollider collider)
	{
		if (!(collider == null))
		{
			return gGraph._colliderToNode[collider];
		}
		return ZoneNode.Null;
	}

	private void Awake()
	{
		if (gGraph != null && gGraph != this)
		{
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			gGraph = this;
		}
		CompileColliderMaps(_zoneDefs);
	}

	public void CheckCompiledMaps()
	{
		if (!_compiledGraph)
		{
			CompileColliderMaps(_zoneDefs);
		}
	}

	private void CompileColliderMaps(ZoneDef[] zones)
	{
		foreach (ZoneDef zoneDef in zones)
		{
			for (int j = 0; j < zoneDef.colliders.Length; j++)
			{
				BoxCollider boxCollider = zoneDef.colliders[j];
				if (!(boxCollider == null))
				{
					_colliderToZoneDef[boxCollider] = zoneDef;
				}
			}
		}
		for (int k = 0; k < _colliders.Length; k++)
		{
			BoxCollider boxCollider2 = _colliders[k];
			if (!(boxCollider2 == null))
			{
				_colliderToNode[boxCollider2] = _nodes[k];
			}
		}
		_compiledGraph = true;
	}

	public static int Compare(ZoneDef x, ZoneDef y)
	{
		if (x == null && y == null)
		{
			return 0;
		}
		if (x == null)
		{
			return 1;
		}
		if (y == null)
		{
			return -1;
		}
		int zoneId = (int)x.zoneId;
		int num = zoneId.CompareTo((int)y.zoneId);
		if (num == 0)
		{
			zoneId = (int)x.subZoneId;
			num = zoneId.CompareTo((int)y.subZoneId);
		}
		return num;
	}

	public static void Register(ZoneEntity entity)
	{
		if (gGraph == null)
		{
			gGraph = UnityEngine.Object.FindFirstObjectByType<ZoneGraph>();
		}
		if (!gGraph._entityList.Contains(entity))
		{
			gGraph._entityList.Add(entity);
		}
	}

	public static void Unregister(ZoneEntity entity)
	{
		gGraph._entityList.Remove(entity);
	}
}
