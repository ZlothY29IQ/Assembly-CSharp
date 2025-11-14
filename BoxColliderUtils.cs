using UnityEngine;

public static class BoxColliderUtils
{
	public static Matrix4x4 GetWorldToNormalizedBoxMatrix(BoxCollider boxCollider)
	{
		Transform transform = boxCollider.transform;
		Vector3 center = boxCollider.center;
		Vector3 size = boxCollider.size;
		Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;
		Matrix4x4 matrix4x = Matrix4x4.Translate(-center);
		return Matrix4x4.Scale(new Vector3((size.x != 0f) ? (2f / size.x) : 1f, (size.y != 0f) ? (2f / size.y) : 1f, (size.z != 0f) ? (2f / size.z) : 1f)) * matrix4x * worldToLocalMatrix;
	}

	public static bool DoesBoxContainPoint(BoxCollider boxCollider, Vector3 worldPoint)
	{
		Vector3 vector = GetWorldToNormalizedBoxMatrix(boxCollider).MultiplyPoint3x4(worldPoint);
		if (Mathf.Abs(vector.x) <= 1f && Mathf.Abs(vector.y) <= 1f)
		{
			return Mathf.Abs(vector.z) <= 1f;
		}
		return false;
	}

	public static bool DoesBoxContainBox(BoxCollider containerBox, BoxCollider containedBox)
	{
		Transform transform = containedBox.transform;
		Vector3 vector = transform.TransformPoint(containedBox.center);
		Vector3 vector2 = containedBox.size * 0.5f;
		Vector3 vector3 = transform.TransformVector(new Vector3(vector2.x, 0f, 0f));
		Vector3 vector4 = transform.TransformVector(new Vector3(0f, vector2.y, 0f));
		Vector3 vector5 = transform.TransformVector(new Vector3(0f, 0f, vector2.z));
		if (!DoesBoxContainPoint(containerBox, vector - vector3 - vector4 - vector5) || !DoesBoxContainPoint(containerBox, vector + vector3 - vector4 - vector5) || !DoesBoxContainPoint(containerBox, vector - vector3 + vector4 - vector5) || !DoesBoxContainPoint(containerBox, vector + vector3 + vector4 - vector5) || !DoesBoxContainPoint(containerBox, vector - vector3 - vector4 + vector5) || !DoesBoxContainPoint(containerBox, vector + vector3 - vector4 + vector5) || !DoesBoxContainPoint(containerBox, vector - vector3 + vector4 + vector5) || !DoesBoxContainPoint(containerBox, vector + vector3 + vector4 + vector5))
		{
			return false;
		}
		return true;
	}

	public static bool DoesBoxContainRegion(BoxCollider box, BoundsInt regionBounds)
	{
		Matrix4x4 worldToNormalizedBoxMatrix = GetWorldToNormalizedBoxMatrix(box);
		Vector3 vector = BoundsInt.IntToFloat(regionBounds.min);
		Vector3 vector2 = BoundsInt.IntToFloat(regionBounds.max);
		Vector3[] array = new Vector3[8]
		{
			new Vector3(vector.x, vector.y, vector.z),
			new Vector3(vector2.x, vector.y, vector.z),
			new Vector3(vector.x, vector2.y, vector.z),
			new Vector3(vector2.x, vector2.y, vector.z),
			new Vector3(vector.x, vector.y, vector2.z),
			new Vector3(vector2.x, vector.y, vector2.z),
			new Vector3(vector.x, vector2.y, vector2.z),
			new Vector3(vector2.x, vector2.y, vector2.z)
		};
		foreach (Vector3 point in array)
		{
			Vector3 vector3 = worldToNormalizedBoxMatrix.MultiplyPoint3x4(point);
			if (Mathf.Abs(vector3.x) > 1f || Mathf.Abs(vector3.y) > 1f || Mathf.Abs(vector3.z) > 1f)
			{
				return false;
			}
		}
		return true;
	}
}
