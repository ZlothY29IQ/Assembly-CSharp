using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BSPTreeBuilder
{
	public class BoxMetadata
	{
		public BoxCollider box;

		public ZoneDef zone;

		public int matrixIndex;

		public int priority;

		public readonly BoundsInt bounds;

		public BoxMetadata(BoxCollider boxCollider, ZoneDef zoneData, int matrixIdx, int priority)
		{
			box = boxCollider;
			zone = zoneData;
			matrixIndex = matrixIdx;
			bounds = BoundsInt.FromBounds(boxCollider.bounds);
			this.priority = priority;
		}

		public bool ContainsPoint(Vector3 worldPoint)
		{
			Vector3 vector = box.transform.InverseTransformPoint(worldPoint);
			Vector3 size = box.size;
			Vector3 center = box.center;
			Vector3 vector2 = center - size * 0.5f;
			Vector3 vector3 = center + size * 0.5f;
			if (vector.x >= vector2.x && vector.x <= vector3.x && vector.y >= vector2.y && vector.y <= vector3.y && vector.z >= vector2.z)
			{
				return vector.z <= vector3.z;
			}
			return false;
		}

		public BoundsInt GetWorldBounds()
		{
			return bounds;
		}
	}

	private const int MAX_ZONES_PER_LEAF = 10;

	private const int MAX_DEPTH = 15;

	private const int MAX_NODES = 650;

	private static Vector3 testPoint = new Vector3(60f, 49f, -98f);

	public static SerializableBSPTree BuildTree(ZoneDef[] zones)
	{
		List<BoxMetadata> list = new List<BoxMetadata>();
		List<MatrixZonePair> list2 = new List<MatrixZonePair>();
		new List<BoxCollider>();
		for (int i = 0; i < zones.Length; i++)
		{
			ZoneDef zoneDef = zones[i];
			List<BoxCollider> list3 = new List<BoxCollider>();
			zoneDef.GetComponents(list3);
			list3.AddRange(zoneDef.transform.GetComponentsInChildren<BoxCollider>());
			Debug.Log($"SerializableBSPTree zone {zoneDef.zoneId}/{zoneDef.subZoneId} box count {list3.Count}");
			foreach (BoxCollider item3 in list3)
			{
				int count = list2.Count;
				int zoneIndex = Array.IndexOf(zones.ToArray(), zoneDef);
				list2.Add(new MatrixZonePair
				{
					matrix = BoxColliderUtils.GetWorldToNormalizedBoxMatrix(item3),
					zoneIndex = zoneIndex
				});
				list.Add(new BoxMetadata(item3, zoneDef, count, zones.Length - i));
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		List<SerializableBSPNode> list4 = new List<SerializableBSPNode>();
		List<MatrixBSPNode> list5 = new List<MatrixBSPNode>();
		Dictionary<(int, int), int> matrixNodeCache = new Dictionary<(int, int), int>();
		int matrixNodeCacheHits = 0;
		list5.Add(new MatrixBSPNode
		{
			matrixIndex = -1,
			outsideChildIndex = 0
		});
		BoundsInt bounds = CalculateWorldBounds(list);
		int num = BuildTreeRecursive(zones.ToArray(), list, bounds, 0, SerializableBSPNode.Axis.X, list4, list5, matrixNodeCache, ref matrixNodeCacheHits);
		CleanupUnreferencedMatrices(list5, list2);
		List<SerializableBSPNode> list6 = new List<SerializableBSPNode>(list4);
		int count2 = list6.Count;
		for (int j = 0; j < list5.Count; j++)
		{
			MatrixBSPNode matrixBSPNode = list5[j];
			if (matrixBSPNode.matrixIndex < 0)
			{
				SerializableBSPNode item = new SerializableBSPNode
				{
					axis = SerializableBSPNode.Axis.Zone,
					splitValue = 0f,
					leftChildIndex = (short)matrixBSPNode.outsideChildIndex,
					rightChildIndex = 0
				};
				list6.Add(item);
			}
			else
			{
				bool flag = matrixBSPNode.outsideChildIndex >= 0;
				SerializableBSPNode item2 = new SerializableBSPNode
				{
					axis = (flag ? SerializableBSPNode.Axis.MatrixFinal : SerializableBSPNode.Axis.MatrixChain),
					splitValue = 0f,
					leftChildIndex = (short)matrixBSPNode.matrixIndex,
					rightChildIndex = (short)(flag ? matrixBSPNode.outsideChildIndex : (count2 - matrixBSPNode.outsideChildIndex))
				};
				list6.Add(item2);
			}
		}
		for (int k = 0; k < list6.Count; k++)
		{
			SerializableBSPNode value = list6[k];
			bool flag2 = false;
			SerializableBSPNode.Axis axis = value.axis;
			if ((uint)axis <= 2u)
			{
				if (value.leftChildIndex < 0)
				{
					value.leftChildIndex = (short)(count2 - value.leftChildIndex);
					flag2 = true;
				}
				if (value.rightChildIndex < 0)
				{
					value.rightChildIndex = (short)(count2 - value.rightChildIndex);
					flag2 = true;
				}
			}
			if (flag2)
			{
				list6[k] = value;
			}
		}
		if (num < 0)
		{
			num = count2 - num;
		}
		SerializableBSPTree serializableBSPTree = new SerializableBSPTree();
		serializableBSPTree.nodes = list6.ToArray();
		serializableBSPTree.matrices = list2.ToArray();
		serializableBSPTree.zones = zones.ToArray();
		serializableBSPTree.rootIndex = num;
		int num2 = serializableBSPTree.nodes.Length * 12;
		int num3 = serializableBSPTree.matrices.Length * 68;
		int num4 = num2 + num3;
		Debug.Log($"Unified BSP Tree generated: {serializableBSPTree.nodes.Length} total nodes ({list4.Count} spatial + {list5.Count} matrix) ({num2} bytes), {serializableBSPTree.matrices.Length} matrix-zone pairs ({num3} bytes). Matrix nodes deduplicated: {matrixNodeCacheHits}. Total: {num4} bytes");
		return serializableBSPTree;
	}

	private static int BuildTreeRecursive(ZoneDef[] zones, List<BoxMetadata> boxes, BoundsInt bounds, int depth, SerializableBSPNode.Axis axis, List<SerializableBSPNode> nodeList, List<MatrixBSPNode> matrixNodeList, Dictionary<(int matrixIndex, int outsideIndex), int> matrixNodeCache, ref int matrixNodeCacheHits)
	{
		Debug.Log($"Building node at depth {depth} with {boxes.Count} boxes, total nodes so far: {nodeList.Count}");
		int count = nodeList.Count;
		if (bounds.Contains(testPoint))
		{
			_ = 1;
		}
		List<BoxMetadata> list = new List<BoxMetadata>();
		int num = -1;
		foreach (BoxMetadata box in boxes)
		{
			if (box.bounds.GetIntersection(bounds) == bounds && BoxColliderUtils.DoesBoxContainRegion(box.box, bounds))
			{
				list.Add(box);
				num = Mathf.Max(box.priority);
			}
		}
		if (list.Count > 1)
		{
			foreach (BoxMetadata item in list)
			{
				if (item.priority < num)
				{
					boxes.Remove(item);
				}
				else if (item.priority == num)
				{
					num++;
				}
			}
		}
		bool flag = true;
		for (int i = 1; i < boxes.Count; i++)
		{
			BoxMetadata boxMetadata = boxes[i];
			if (boxMetadata.zone != boxes[0].zone)
			{
				flag = false;
				break;
			}
			if (boxMetadata.bounds.GetIntersection(bounds) == bounds)
			{
				list.Add(boxMetadata);
			}
		}
		if (flag || boxes.Count == 1)
		{
			return CreateMatrixNodeTree(zones, boxes, matrixNodeList, bounds, matrixNodeCache, ref matrixNodeCacheHits);
		}
		if (depth >= 15)
		{
			Debug.LogWarning($"Maximum depth {15} reached with {boxes.Count} boxes, creating matrix node tree");
			return CreateMatrixNodeTree(zones, boxes, matrixNodeList, bounds, matrixNodeCache, ref matrixNodeCacheHits);
		}
		if (nodeList.Count >= 650)
		{
			Debug.LogWarning($"Maximum nodes {650} reached, creating matrix node tree");
			return CreateMatrixNodeTree(zones, boxes, matrixNodeList, bounds, matrixNodeCache, ref matrixNodeCacheHits);
		}
		if (boxes.Count <= 10)
		{
			Debug.Log($"Creating matrix node tree with {boxes.Count} boxes at depth {depth}");
			return CreateMatrixNodeTree(zones, boxes, matrixNodeList, bounds, matrixNodeCache, ref matrixNodeCacheHits);
		}
		SerializableBSPNode serializableBSPNode = new SerializableBSPNode
		{
			axis = axis,
			leftChildIndex = -1,
			rightChildIndex = -1
		};
		nodeList.Add(serializableBSPNode);
		int bestSplitValue;
		SerializableBSPNode.Axis axis2 = (serializableBSPNode.axis = FindBestAxis(boxes, bounds, axis, out bestSplitValue));
		serializableBSPNode.splitValue = (float)bestSplitValue / 1000f;
		Debug.Log($"Best axis: {axis2}, split value: {bestSplitValue}");
		BoundsInt boundsInt = bounds;
		BoundsInt boundsInt2 = bounds;
		switch (axis2)
		{
		case SerializableBSPNode.Axis.X:
			boundsInt.SetMinMax(bounds.min, new Vector3Int(bestSplitValue, bounds.max.y, bounds.max.z));
			boundsInt2.SetMinMax(new Vector3Int(bestSplitValue, bounds.min.y, bounds.min.z), bounds.max);
			break;
		case SerializableBSPNode.Axis.Y:
			boundsInt.SetMinMax(bounds.min, new Vector3Int(bounds.max.x, bestSplitValue, bounds.max.z));
			boundsInt2.SetMinMax(new Vector3Int(bounds.min.x, bestSplitValue, bounds.min.z), bounds.max);
			break;
		case SerializableBSPNode.Axis.Z:
			boundsInt.SetMinMax(bounds.min, new Vector3Int(bounds.max.x, bounds.max.y, bestSplitValue));
			boundsInt2.SetMinMax(new Vector3Int(bounds.min.x, bounds.min.y, bestSplitValue), bounds.max);
			break;
		}
		List<BoxMetadata> effectiveBoxes = GetEffectiveBoxes(boxes, boundsInt);
		List<BoxMetadata> effectiveBoxes2 = GetEffectiveBoxes(boxes, boundsInt2);
		List<BoxMetadata> list2 = new List<BoxMetadata>(effectiveBoxes);
		List<BoxMetadata> list3 = new List<BoxMetadata>(effectiveBoxes2);
		Debug.Log($"Split result: leftBoxes={list2.Count}, rightBoxes={list3.Count}");
		if (list2.Count == 0 || list3.Count == 0)
		{
			Debug.Log($"No valid split found, creating matrix node tree with {boxes.Count} boxes");
			return CreateMatrixNodeTree(zones, boxes, matrixNodeList, bounds, matrixNodeCache, ref matrixNodeCacheHits);
		}
		SerializableBSPNode.Axis nextAxis = GetNextAxis(axis);
		serializableBSPNode.leftChildIndex = (short)BuildTreeRecursive(zones, list2, boundsInt, depth + 1, nextAxis, nodeList, matrixNodeList, matrixNodeCache, ref matrixNodeCacheHits);
		serializableBSPNode.rightChildIndex = (short)BuildTreeRecursive(zones, list3, boundsInt2, depth + 1, nextAxis, nodeList, matrixNodeList, matrixNodeCache, ref matrixNodeCacheHits);
		nodeList[count] = serializableBSPNode;
		return count;
	}

	private static SerializableBSPNode.Axis FindBestAxis(List<BoxMetadata> boxes, BoundsInt bounds, SerializableBSPNode.Axis preferredAxis, out int bestSplitValue)
	{
		SerializableBSPNode.Axis[] obj = new SerializableBSPNode.Axis[3]
		{
			preferredAxis,
			GetNextAxis(preferredAxis),
			GetNextAxis(GetNextAxis(preferredAxis))
		};
		SerializableBSPNode.Axis axis = preferredAxis;
		int num = int.MaxValue;
		bestSplitValue = GetFallbackSplit(bounds, preferredAxis);
		SerializableBSPNode.Axis[] array = obj;
		foreach (SerializableBSPNode.Axis axis2 in array)
		{
			int bestScore;
			int num2 = FindOptimalSplit(boxes, bounds, axis2, out bestScore);
			Debug.Log($"Axis {axis2}: split={num2:F3}, score={bestScore}");
			if (bestScore < num)
			{
				num = bestScore;
				axis = axis2;
				bestSplitValue = num2;
			}
		}
		Debug.Log($"Selected axis {axis} with score {num}");
		return axis;
	}

	private static int EvaluateBestSplit(List<BoxMetadata> boxes, BoundsInt bounds, SerializableBSPNode.Axis axis, int splitValue)
	{
		BoundsInt boundsInt = bounds;
		BoundsInt boundsInt2 = bounds;
		switch (axis)
		{
		case SerializableBSPNode.Axis.X:
			boundsInt.SetMinMax(bounds.min, new Vector3Int(splitValue, bounds.max.y, bounds.max.z));
			boundsInt2.SetMinMax(new Vector3Int(splitValue, bounds.min.y, bounds.min.z), bounds.max);
			break;
		case SerializableBSPNode.Axis.Y:
			boundsInt.SetMinMax(bounds.min, new Vector3Int(bounds.max.x, splitValue, bounds.max.z));
			boundsInt2.SetMinMax(new Vector3Int(bounds.min.x, splitValue, bounds.min.z), bounds.max);
			break;
		case SerializableBSPNode.Axis.Z:
			boundsInt.SetMinMax(bounds.min, new Vector3Int(bounds.max.x, bounds.max.y, splitValue));
			boundsInt2.SetMinMax(new Vector3Int(bounds.min.x, bounds.min.y, splitValue), bounds.max);
			break;
		}
		return EvaluateSplit(boxes, splitValue, axis, bounds);
	}

	private static int FindOptimalSplit(List<BoxMetadata> boxes, BoundsInt bounds, SerializableBSPNode.Axis axis, out int bestScore)
	{
		List<int> list = new List<int>();
		foreach (BoxMetadata box in boxes)
		{
			BoundsInt bounds2 = box.bounds;
			switch (axis)
			{
			case SerializableBSPNode.Axis.X:
				list.Add(bounds2.min.x);
				list.Add(bounds2.max.x);
				break;
			case SerializableBSPNode.Axis.Y:
				list.Add(bounds2.min.y);
				list.Add(bounds2.max.y);
				break;
			case SerializableBSPNode.Axis.Z:
				list.Add(bounds2.min.z);
				list.Add(bounds2.max.z);
				break;
			}
		}
		list = (from x in list.Distinct()
			orderby x
			select x).ToList();
		int num = GetFallbackSplit(bounds, axis);
		bestScore = int.MaxValue;
		Debug.Log($"Evaluating {list.Count} split candidates for {axis} axis");
		int axisValue = GetAxisValue(bounds.min, axis);
		int axisValue2 = GetAxisValue(bounds.max, axis);
		foreach (int item in list)
		{
			if (item > axisValue && item < axisValue2)
			{
				int num2 = EvaluateSplit(boxes, item, axis, bounds);
				if (num2 < bestScore)
				{
					bestScore = num2;
					num = item;
				}
			}
		}
		Debug.Log($"Best split: {num} with score {bestScore}");
		return num;
	}

	private static int GetFallbackSplit(BoundsInt bounds, SerializableBSPNode.Axis axis)
	{
		return axis switch
		{
			SerializableBSPNode.Axis.X => bounds.center.x, 
			SerializableBSPNode.Axis.Y => bounds.center.y, 
			SerializableBSPNode.Axis.Z => bounds.center.z, 
			_ => 0, 
		};
	}

	private static int EvaluateSplit(List<BoxMetadata> boxes, int splitValue, SerializableBSPNode.Axis axis, BoundsInt bounds)
	{
		BoundsInt region = bounds;
		BoundsInt region2 = bounds;
		switch (axis)
		{
		case SerializableBSPNode.Axis.X:
			region.SetMinMax(bounds.min, new Vector3Int(splitValue - 1, bounds.max.y, bounds.max.z));
			region2.SetMinMax(new Vector3Int(splitValue + 1, bounds.min.y, bounds.min.z), bounds.max);
			break;
		case SerializableBSPNode.Axis.Y:
			region.SetMinMax(bounds.min, new Vector3Int(bounds.max.x, splitValue - 1, bounds.max.z));
			region2.SetMinMax(new Vector3Int(bounds.min.x, splitValue + 1, bounds.min.z), bounds.max);
			break;
		case SerializableBSPNode.Axis.Z:
			region.SetMinMax(bounds.min, new Vector3Int(bounds.max.x, bounds.max.y, splitValue - 1));
			region2.SetMinMax(new Vector3Int(bounds.min.x, bounds.min.y, splitValue + 1), bounds.max);
			break;
		}
		List<BoxMetadata> effectiveBoxes = GetEffectiveBoxes(boxes, region);
		List<BoxMetadata> effectiveBoxes2 = GetEffectiveBoxes(boxes, region2);
		int count = effectiveBoxes.Count;
		int count2 = effectiveBoxes2.Count;
		int count3 = boxes.Count;
		int num = count3 - count;
		int num2 = count3 - count2;
		Debug.Log($"  Split evaluation: {count3} total -> L:{count} (eliminated:{num}), R:{count2} (eliminated:{num2})");
		if (count == 0 || count2 == 0)
		{
			return 1000;
		}
		return -((num + 1) * (num2 + 1));
	}

	private static List<BoxMetadata> GetEffectiveBoxes(List<BoxMetadata> boxes, BoundsInt region)
	{
		List<BoxMetadata> list = new List<BoxMetadata>();
		List<BoxMetadata> list2 = new List<BoxMetadata>();
		foreach (BoxMetadata box in boxes)
		{
			if (box.bounds.Intersects(region))
			{
				list2.Add(box);
			}
		}
		foreach (BoxMetadata item in list2)
		{
			bool flag = false;
			BoundsInt intersection = item.bounds.GetIntersection(region);
			foreach (BoxMetadata item2 in list2)
			{
				if (item2 != item && item2.priority > item.priority && item2.bounds.GetIntersection(region).Contains(intersection) && BoxColliderUtils.DoesBoxContainBox(item2.box, item.box))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(item);
			}
		}
		return list;
	}

	private static List<BoxMetadata> GetEffectiveSpanningBoxes(List<BoxMetadata> boxes, BoundsInt leftBounds, BoundsInt rightBounds)
	{
		List<BoxMetadata> list = new List<BoxMetadata>();
		Dictionary<BoxMetadata, BoundsInt> dictionary = new Dictionary<BoxMetadata, BoundsInt>();
		foreach (BoxMetadata box in boxes)
		{
			dictionary[box] = box.bounds;
		}
		foreach (BoxMetadata box2 in boxes)
		{
			BoundsInt boundsInt = dictionary[box2];
			if (boundsInt.Intersects(leftBounds) && boundsInt.Intersects(rightBounds))
			{
				list.Add(box2);
			}
		}
		return list;
	}

	private static SerializableBSPNode.Axis GetNextAxis(SerializableBSPNode.Axis currentAxis)
	{
		return currentAxis switch
		{
			SerializableBSPNode.Axis.X => SerializableBSPNode.Axis.Y, 
			SerializableBSPNode.Axis.Y => SerializableBSPNode.Axis.Z, 
			SerializableBSPNode.Axis.Z => SerializableBSPNode.Axis.X, 
			_ => SerializableBSPNode.Axis.X, 
		};
	}

	private static int GetAxisValue(Vector3Int point, SerializableBSPNode.Axis axis)
	{
		return axis switch
		{
			SerializableBSPNode.Axis.X => point.x, 
			SerializableBSPNode.Axis.Y => point.y, 
			SerializableBSPNode.Axis.Z => point.z, 
			_ => 0, 
		};
	}

	private static BoundsInt CalculateWorldBounds(List<BoxMetadata> boxes)
	{
		if (boxes.Count == 0)
		{
			return default(BoundsInt);
		}
		BoundsInt bounds = boxes[0].bounds;
		for (int i = 1; i < boxes.Count; i++)
		{
			bounds.Encapsulate(boxes[i].bounds);
		}
		return bounds;
	}

	private static float CalculateIntersectionVolume(BoundsInt box, BoundsInt region)
	{
		if (!box.Intersects(region))
		{
			return 0f;
		}
		return box.GetIntersection(region).VolumeFloat();
	}

	private static int CreateMatrixNodeTree(ZoneDef[] zones, List<BoxMetadata> boxes, List<MatrixBSPNode> matrixNodeList, BoundsInt bounds, Dictionary<(int matrixIndex, int outsideIndex), int> matrixNodeCache, ref int matrixNodeCacheHits)
	{
		if (boxes.Count == 0)
		{
			Debug.LogWarning("Cannot create matrix node tree with no boxes - returning zone 0");
			return 0;
		}
		List<BoxMetadata> list = new List<BoxMetadata>();
		List<BoxMetadata> list2 = new List<BoxMetadata>();
		foreach (BoxMetadata box in boxes)
		{
			if (box.bounds.GetIntersection(bounds) == bounds && BoxColliderUtils.DoesBoxContainRegion(box.box, bounds))
			{
				list.Add(box);
			}
			else
			{
				list2.Add(box);
			}
		}
		if (list.Count > 0)
		{
			ZoneDef zone = list[0].zone;
			int priority = list[0].priority;
			foreach (BoxMetadata item in list)
			{
				if (item.priority > priority)
				{
					priority = item.priority;
					zone = item.zone;
				}
			}
			for (int num = list2.Count - 1; num >= 0; num--)
			{
				if (list2[num].priority < priority)
				{
					list2.RemoveAt(num);
				}
			}
			if (list2.Count == 0)
			{
				MatrixBSPNode matrixNode = new MatrixBSPNode
				{
					matrixIndex = -1
				};
				int outsideChildIndex = Array.IndexOf(zones, zone);
				matrixNode.outsideChildIndex = outsideChildIndex;
				return AddMatrixNodeWithCache(matrixNode, matrixNodeList, matrixNodeCache, ref matrixNodeCacheHits);
			}
			List<BoxMetadata> list3 = SortBoxesByPriority(list2);
			foreach (BoxMetadata item2 in list)
			{
				if (item2.zone == zone)
				{
					list3.Add(item2);
					break;
				}
			}
			return CreateSequentialMatrixNodes(zones, list3, matrixNodeList, 0, zones, matrixNodeCache, ref matrixNodeCacheHits);
		}
		List<BoxMetadata> boxes2 = SortBoxesByPriority(boxes);
		return CreateSequentialMatrixNodes(zones, boxes2, matrixNodeList, 0, zones, matrixNodeCache, ref matrixNodeCacheHits);
	}

	private static int CreateSequentialMatrixNodes(ZoneDef[] zones, List<BoxMetadata> boxes, List<MatrixBSPNode> matrixNodeList, int boxIndex, ZoneDef[] allZones, Dictionary<(int matrixIndex, int outsideIndex), int> matrixNodeCache, ref int matrixNodeCacheHits)
	{
		if (boxIndex == 0 && boxes.Count > 1)
		{
			while (boxes.Count > 1 && boxes[boxes.Count - 1].zone == boxes[boxes.Count - 2].zone)
			{
				boxes.RemoveAt(boxes.Count - 1);
			}
		}
		if (boxIndex >= boxes.Count)
		{
			return 0;
		}
		BoxMetadata boxMetadata = boxes[boxIndex];
		if (boxIndex == boxes.Count - 1)
		{
			MatrixBSPNode matrixNode = new MatrixBSPNode
			{
				matrixIndex = -1
			};
			int outsideChildIndex = Array.IndexOf(allZones, boxMetadata.zone);
			matrixNode.outsideChildIndex = outsideChildIndex;
			return AddMatrixNodeWithCache(matrixNode, matrixNodeList, matrixNodeCache, ref matrixNodeCacheHits);
		}
		MatrixBSPNode matrixNode2 = new MatrixBSPNode
		{
			matrixIndex = boxMetadata.matrixIndex
		};
		int outsideChildIndex2 = CreateSequentialMatrixNodes(zones, boxes, matrixNodeList, boxIndex + 1, allZones, matrixNodeCache, ref matrixNodeCacheHits);
		matrixNode2.outsideChildIndex = outsideChildIndex2;
		return AddMatrixNodeWithCache(matrixNode2, matrixNodeList, matrixNodeCache, ref matrixNodeCacheHits);
	}

	private static int AddMatrixNodeWithCache(MatrixBSPNode matrixNode, List<MatrixBSPNode> matrixNodeList, Dictionary<(int matrixIndex, int outsideIndex), int> matrixNodeCache, ref int matrixNodeCacheHits)
	{
		(int, int) key = (matrixNode.matrixIndex, matrixNode.outsideChildIndex);
		if (matrixNodeCache.TryGetValue(key, out var value))
		{
			matrixNodeCacheHits++;
			return -value;
		}
		int count = matrixNodeList.Count;
		matrixNodeList.Add(matrixNode);
		matrixNodeCache[key] = count;
		return -count;
	}

	private static List<BoxMetadata> SortBoxesByPriority(List<BoxMetadata> boxes)
	{
		List<BoxMetadata> list = new List<BoxMetadata>(boxes);
		list.Sort((BoxMetadata a, BoxMetadata b) => b.priority.CompareTo(a.priority));
		return list;
	}

	private static void CleanupUnreferencedMatrices(List<MatrixBSPNode> matrixNodeList, List<MatrixZonePair> matricesList)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (MatrixBSPNode matrixNode in matrixNodeList)
		{
			if (matrixNode.matrixIndex >= 0)
			{
				hashSet.Add(matrixNode.matrixIndex);
			}
		}
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		List<MatrixZonePair> list = new List<MatrixZonePair>();
		for (int i = 0; i < matricesList.Count; i++)
		{
			if (hashSet.Contains(i))
			{
				dictionary[i] = list.Count;
				list.Add(matricesList[i]);
			}
		}
		for (int j = 0; j < matrixNodeList.Count; j++)
		{
			MatrixBSPNode value = matrixNodeList[j];
			if (dictionary.TryGetValue(value.matrixIndex, out var value2))
			{
				value.matrixIndex = value2;
				matrixNodeList[j] = value;
			}
		}
		int count = matricesList.Count;
		matricesList.Clear();
		matricesList.AddRange(list);
		int num = count - list.Count;
		if (num > 0)
		{
			Debug.Log($"Cleaned up {num} unreferenced matrices. Matrices reduced from {count} to {list.Count}");
		}
	}
}
