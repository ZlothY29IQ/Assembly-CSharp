using BoingKit;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace GorillaTagScripts;

[BurstCompile]
public struct BuilderFindPotentialSnaps : IJobParallelFor
{
	[ReadOnly]
	public float gridSize;

	[ReadOnly]
	public BuilderTable.SnapParams currSnapParams;

	[ReadOnly]
	public NativeList<BuilderGridPlaneData> gridPlanes;

	[ReadOnly]
	public NativeList<BuilderGridPlaneData> checkGridPlanes;

	[ReadOnly]
	public Vector3 worldToLocalPos;

	[ReadOnly]
	public Quaternion worldToLocalRot;

	[ReadOnly]
	public Vector3 localToWorldPos;

	[ReadOnly]
	public Quaternion localToWorldRot;

	public NativeQueue<BuilderPotentialPlacementData>.ParallelWriter potentialPlacements;

	public void Execute(int index)
	{
		BuilderGridPlaneData gridPlane = gridPlanes[index];
		for (int i = 0; i < checkGridPlanes.Length; i++)
		{
			BuilderGridPlaneData checkGridPlane = checkGridPlanes[i];
			BuilderPotentialPlacementData potentialPlacement = default(BuilderPotentialPlacementData);
			if (TryPlaceGridPlaneOnGridPlane(ref gridPlane, ref checkGridPlane, ref potentialPlacement))
			{
				potentialPlacements.Enqueue(potentialPlacement);
			}
		}
	}

	public bool TryPlaceGridPlaneOnGridPlane(ref BuilderGridPlaneData gridPlane, ref BuilderGridPlaneData checkGridPlane, ref BuilderPotentialPlacementData potentialPlacement)
	{
		if (checkGridPlane.male == gridPlane.male)
		{
			return false;
		}
		if (checkGridPlane.pieceId == gridPlane.pieceId)
		{
			return false;
		}
		Vector3 position = gridPlane.position;
		Quaternion rotation = gridPlane.rotation;
		Vector3 vector = worldToLocalRot * (position + worldToLocalPos);
		Quaternion quaternion = worldToLocalRot * rotation;
		position = localToWorldPos + localToWorldRot * vector;
		rotation = localToWorldRot * quaternion;
		Vector3 position2 = checkGridPlane.position;
		float sqrMagnitude = (position2 - position).sqrMagnitude;
		float num = checkGridPlane.boundingRadius + gridPlane.boundingRadius;
		if (sqrMagnitude > num * num)
		{
			return false;
		}
		Quaternion rotation2 = checkGridPlane.rotation;
		Quaternion quaternion2 = Quaternion.Inverse(rotation2);
		Quaternion quaternion3 = quaternion2 * rotation;
		float num2 = Vector3.Dot(Vector3.up, quaternion3 * Vector3.up);
		if (num2 < currSnapParams.maxUpDotProduct)
		{
			return false;
		}
		Vector3 vector2 = quaternion2 * (position - position2);
		float y = vector2.y;
		if (Mathf.Abs(y) > 1f)
		{
			return false;
		}
		if ((gridPlane.male && y > currSnapParams.minOffsetY) || (!gridPlane.male && y < 0f - currSnapParams.minOffsetY))
		{
			return false;
		}
		if (Mathf.Abs(y) > currSnapParams.maxOffsetY)
		{
			return false;
		}
		Quaternion twist = Quaternion.identity;
		if (new Vector3(quaternion3.x, quaternion3.y, quaternion3.z).sqrMagnitude > MathUtil.Epsilon)
		{
			QuaternionUtil.DecomposeSwingTwist(quaternion3, Vector3.up, out var _, out twist);
		}
		float maxTwistDotProduct = currSnapParams.maxTwistDotProduct;
		Vector3 lhs = twist * Vector3.forward;
		float num3 = Vector3.Dot(lhs, Vector3.forward);
		float num4 = Vector3.Dot(lhs, Vector3.right);
		bool flag = Mathf.Abs(num3) > maxTwistDotProduct;
		bool flag2 = Mathf.Abs(num4) > maxTwistDotProduct;
		if (!(flag || flag2))
		{
			return false;
		}
		uint num5 = 0u;
		float y2;
		if (flag)
		{
			y2 = ((num3 > 0f) ? 0f : 180f);
			num5 = ((!(num3 > 0f)) ? 2u : 0u);
		}
		else
		{
			y2 = ((num4 > 0f) ? 90f : 270f);
			num5 = ((num4 > 0f) ? 1u : 3u);
		}
		int num6 = (flag2 ? gridPlane.width : gridPlane.length);
		int num7 = (flag2 ? gridPlane.length : gridPlane.width);
		float num8 = ((num7 % 2 == 0) ? (gridSize / 2f) : 0f);
		float num9 = ((num6 % 2 == 0) ? (gridSize / 2f) : 0f);
		float num10 = ((checkGridPlane.width % 2 == 0) ? (gridSize / 2f) : 0f);
		float num11 = ((checkGridPlane.length % 2 == 0) ? (gridSize / 2f) : 0f);
		float num12 = num8 - num10;
		float num13 = num9 - num11;
		int num14 = Mathf.RoundToInt((vector2.x - num12) / gridSize);
		int num15 = Mathf.RoundToInt((vector2.z - num13) / gridSize);
		int num16 = num14 + Mathf.FloorToInt((float)num7 / 2f);
		int num17 = num15 + Mathf.FloorToInt((float)num6 / 2f);
		int num18 = num16 - (num7 - 1);
		int num19 = num17 - (num6 - 1);
		int num20 = Mathf.FloorToInt((float)checkGridPlane.width / 2f);
		int num21 = Mathf.FloorToInt((float)checkGridPlane.length / 2f);
		int num22 = num20 - (checkGridPlane.width - 1);
		int num23 = num21 - (checkGridPlane.length - 1);
		if (num18 > num20 || num16 < num22 || num19 > num21 || num17 < num23)
		{
			return false;
		}
		Quaternion quaternion4 = Quaternion.Euler(0f, y2, 0f);
		Quaternion quaternion5 = rotation2 * quaternion4;
		float x = (float)num14 * gridSize + num12;
		float z = (float)num15 * gridSize + num13;
		Vector3 vector3 = new Vector3(x, 0f, z);
		Vector3 vector4 = position2 + rotation2 * vector3;
		Quaternion quaternion6 = quaternion5 * Quaternion.Inverse(gridPlane.localRotation);
		Vector3 localPosition = vector4 - quaternion6 * gridPlane.localPosition;
		potentialPlacement.localPosition = localPosition;
		potentialPlacement.localRotation = quaternion6;
		float num24 = 0.025f;
		float score = 0f - Mathf.Abs(y) + num2 * num24;
		potentialPlacement.score = score;
		potentialPlacement.pieceId = gridPlane.pieceId;
		potentialPlacement.attachIndex = gridPlane.attachIndex;
		potentialPlacement.parentPieceId = checkGridPlane.pieceId;
		potentialPlacement.parentAttachIndex = checkGridPlane.attachIndex;
		potentialPlacement.attachDistance = Mathf.Abs(y);
		potentialPlacement.attachPlaneNormal = Vector3.up;
		if (!checkGridPlane.male)
		{
			potentialPlacement.attachPlaneNormal *= -1f;
		}
		potentialPlacement.parentAttachBounds.min.x = Mathf.Max(num22, num18);
		potentialPlacement.parentAttachBounds.min.y = Mathf.Max(num23, num19);
		potentialPlacement.parentAttachBounds.max.x = Mathf.Min(num20, num16);
		potentialPlacement.parentAttachBounds.max.y = Mathf.Min(num21, num17);
		potentialPlacement.twist = (byte)num5;
		potentialPlacement.bumpOffsetX = (sbyte)num14;
		potentialPlacement.bumpOffsetZ = (sbyte)num15;
		Vector2Int v = Vector2Int.zero;
		Vector2Int v2 = Vector2Int.zero;
		v.x = potentialPlacement.parentAttachBounds.min.x - num14;
		v2.x = potentialPlacement.parentAttachBounds.max.x - num14;
		v.y = potentialPlacement.parentAttachBounds.min.y - num15;
		v2.y = potentialPlacement.parentAttachBounds.max.y - num15;
		int offsetX = ((num7 % 2 == 0) ? 1 : 0);
		int offsetY = ((num6 % 2 == 0) ? 1 : 0);
		if (flag && num3 <= 0f)
		{
			v = Rotate180(v, offsetX, offsetY);
			v2 = Rotate180(v2, offsetX, offsetY);
		}
		else if (flag2 && num4 <= 0f)
		{
			v = Rotate270(v, offsetX, offsetY);
			v2 = Rotate270(v2, offsetX, offsetY);
		}
		else if (flag2 && num4 >= 0f)
		{
			v = Rotate90(v, offsetX, offsetY);
			v2 = Rotate90(v2, offsetX, offsetY);
		}
		potentialPlacement.attachBounds.min.x = Mathf.Min(v.x, v2.x);
		potentialPlacement.attachBounds.min.y = Mathf.Min(v.y, v2.y);
		potentialPlacement.attachBounds.max.x = Mathf.Max(v.x, v2.x);
		potentialPlacement.attachBounds.max.y = Mathf.Max(v.y, v2.y);
		return true;
	}

	private Vector2Int Rotate90(Vector2Int v, int offsetX, int offsetY)
	{
		return new Vector2Int(v.y * -1 + offsetY, v.x);
	}

	private Vector2Int Rotate270(Vector2Int v, int offsetX, int offsetY)
	{
		return new Vector2Int(v.y, v.x * -1 + offsetX);
	}

	private Vector2Int Rotate180(Vector2Int v, int offsetX, int offsetY)
	{
		return new Vector2Int(v.x * -1 + offsetX, v.y * -1 + offsetY);
	}
}
