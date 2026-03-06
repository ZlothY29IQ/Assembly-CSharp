using System;
using MathGeoLib;
using UnityEngine;

[Serializable]
public struct BoundsInfo
{
	public Vector3 center;

	public Vector3 size;

	public Quaternion rotation;

	public Vector3 scale;

	public float inflate;

	[Space]
	public Vector3 centerAA;

	public Vector3 sizeAA;

	public Vector3 scaleAA;

	public float inflateAA;

	public Vector3 sizeComputed => Vector3.Scale(size, scale) * inflate;

	public Vector3 sizeComputedAA => Vector3.Scale(sizeAA, scaleAA) * inflateAA;

	public static BoundsInfo ComputeBounds(Vector3[] vertices)
	{
		if (vertices.Length == 0)
		{
			return default(BoundsInfo);
		}
		OrientedBoundingBox orientedBoundingBox = OrientedBoundingBox.BruteEnclosing(vertices);
		Vector4 column = orientedBoundingBox.Axis1;
		Vector4 column2 = orientedBoundingBox.Axis2;
		Vector4 column3 = orientedBoundingBox.Axis3;
		Vector4 column4 = new Vector4(0f, 0f, 0f, 1f);
		BoundsInfo result = new BoundsInfo
		{
			center = orientedBoundingBox.Center,
			size = orientedBoundingBox.Extent * 2f,
			rotation = new Matrix4x4(column, column2, column3, column4).rotation,
			scale = Vector3.one,
			inflate = 1f
		};
		Bounds bounds = GeometryUtility.CalculateBounds(vertices, Matrix4x4.identity);
		result.centerAA = bounds.center;
		result.sizeAA = bounds.size;
		result.scaleAA = Vector3.one;
		result.inflateAA = 1f;
		return result;
	}

	public static BoxCollider CreateBoxCollider(BoundsInfo bounds)
	{
		int hashCode = bounds.center.QuantizedId128().GetHashCode();
		int hashCode2 = bounds.size.QuantizedId128().GetHashCode();
		int hashCode3 = bounds.rotation.QuantizedId128().GetHashCode();
		int num = StaticHash.Compute(hashCode, hashCode2, hashCode3);
		Transform transform = new GameObject($"BoxCollider_{num:X8}").transform;
		transform.position = bounds.center;
		transform.rotation = bounds.rotation;
		BoxCollider boxCollider = transform.gameObject.AddComponent<BoxCollider>();
		boxCollider.size = bounds.sizeComputed;
		return boxCollider;
	}

	public static BoxCollider CreateBoxColliderAA(BoundsInfo bounds)
	{
		int hashCode = bounds.center.QuantizedId128().GetHashCode();
		int hashCode2 = bounds.size.QuantizedId128().GetHashCode();
		int num = StaticHash.Compute(hashCode, hashCode2);
		Transform transform = new GameObject($"BoxCollider_{num:X8}").transform;
		transform.position = bounds.centerAA;
		BoxCollider boxCollider = transform.gameObject.AddComponent<BoxCollider>();
		boxCollider.size = bounds.sizeComputedAA;
		return boxCollider;
	}
}
