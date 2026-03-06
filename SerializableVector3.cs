using System;
using UnityEngine;

[Serializable]
public struct SerializableVector3(float x, float y, float z)
{
	public float x = x;

	public float y = y;

	public float z = z;

	public static implicit operator SerializableVector3(Vector3 v)
	{
		return new SerializableVector3(v.x, v.y, v.z);
	}

	public static implicit operator Vector3(SerializableVector3 v)
	{
		return new Vector3(v.x, v.y, v.z);
	}
}
