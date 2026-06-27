using System;
using UnityEngine;

[Serializable]
public class SerializableBSPTree
{
	[SerializeField]
	public SerializableBSPNode[] nodes;

	[SerializeField]
	public MatrixZonePair[] matrices;

	[SerializeField]
	public ZoneDef[] zones;

	[SerializeField]
	public int rootIndex = -1;

	public ZoneDef FindZone(Vector3 point)
	{
		if (nodes == null || rootIndex < 0 || rootIndex >= nodes.Length)
		{
			return null;
		}
		return FindZoneRecursive(point, rootIndex);
	}

	private ZoneDef FindZoneRecursive(Vector3 point, int nodeIndex)
	{
		if (nodeIndex < 0 || nodeIndex >= nodes.Length)
		{
			return null;
		}
		SerializableBSPNode serializableBSPNode = nodes[nodeIndex];
		if (serializableBSPNode.axis == SerializableBSPNode.Axis.Zone)
		{
			return zones[serializableBSPNode.zoneIndex];
		}
		if (serializableBSPNode.axis == SerializableBSPNode.Axis.MatrixChain || serializableBSPNode.axis == SerializableBSPNode.Axis.MatrixFinal)
		{
			if (serializableBSPNode.matrixIndex < 0)
			{
				if (serializableBSPNode.axis == SerializableBSPNode.Axis.MatrixFinal)
				{
					if (serializableBSPNode.outsideChildIndex >= 0 && serializableBSPNode.outsideChildIndex < zones.Length)
					{
						return zones[serializableBSPNode.outsideChildIndex];
					}
					return null;
				}
				return FindZoneRecursive(point, serializableBSPNode.outsideChildIndex);
			}
			MatrixZonePair matrixZonePair = matrices[serializableBSPNode.matrixIndex];
			Vector3 vector = matrixZonePair.matrix.MultiplyPoint3x4(point);
			if (Mathf.Abs(vector.x) <= 1f && Mathf.Abs(vector.y) <= 1f && Mathf.Abs(vector.z) <= 1f)
			{
				if (matrixZonePair.zoneIndex >= 0 && matrixZonePair.zoneIndex < zones.Length)
				{
					return zones[matrixZonePair.zoneIndex];
				}
				return null;
			}
			if (serializableBSPNode.axis == SerializableBSPNode.Axis.MatrixFinal)
			{
				if (serializableBSPNode.outsideChildIndex >= 0 && serializableBSPNode.outsideChildIndex < zones.Length)
				{
					return zones[serializableBSPNode.outsideChildIndex];
				}
				return null;
			}
			return FindZoneRecursive(point, serializableBSPNode.outsideChildIndex);
		}
		float axisValue = GetAxisValue(point, serializableBSPNode.axis);
		ZoneDef zoneDef = null;
		zoneDef = ((!(axisValue < serializableBSPNode.splitValue)) ? FindZoneRecursive(point, serializableBSPNode.rightChildIndex) : FindZoneRecursive(point, serializableBSPNode.leftChildIndex));
		if (zoneDef == null && Mathf.Abs(axisValue - serializableBSPNode.splitValue) < 2f)
		{
			zoneDef = ((!(axisValue < serializableBSPNode.splitValue)) ? FindZoneRecursive(point, serializableBSPNode.leftChildIndex) : FindZoneRecursive(point, serializableBSPNode.rightChildIndex));
		}
		return zoneDef;
	}

	public int FindZoneIdx(GTZone zoneId, GTSubZone subZoneId)
	{
		for (int i = 0; i < zones.Length; i++)
		{
			if (zones[i].zoneId == zoneId && zones[i].subZoneId == subZoneId)
			{
				return i;
			}
		}
		return -1;
	}

	private float GetAxisValue(Vector3 point, SerializableBSPNode.Axis axis)
	{
		return axis switch
		{
			SerializableBSPNode.Axis.X => point.x, 
			SerializableBSPNode.Axis.Y => point.y, 
			SerializableBSPNode.Axis.Z => point.z, 
			SerializableBSPNode.Axis.MatrixChain => 0f, 
			SerializableBSPNode.Axis.MatrixFinal => 0f, 
			SerializableBSPNode.Axis.Zone => 0f, 
			_ => 0f, 
		};
	}
}
