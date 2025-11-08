using System;
using UnityEngine;

[Serializable]
public class XformNode
{
	public Vector4 localPosition;

	public Transform parent;

	public Vector4 worldPosition
	{
		get
		{
			if (!parent)
			{
				return localPosition;
			}
			Matrix4x4 m = parent.localToWorldMatrix;
			Vector4 point = localPosition;
			MatrixUtils.MultiplyXYZ3x4(ref m, ref point);
			return point;
		}
	}

	public float radius
	{
		get
		{
			return localPosition.w;
		}
		set
		{
			localPosition.w = value;
		}
	}

	public Matrix4x4 LocalTRS()
	{
		return Matrix4x4.TRS(localPosition, Quaternion.identity, Vector3.one);
	}
}
