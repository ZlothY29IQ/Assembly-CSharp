using System;
using UnityEngine;

[Serializable]
public struct SerializableVector2(float x, float y)
{
	public float x = x;

	public float y = y;

	public static implicit operator SerializableVector2(Vector2 v)
	{
		return new SerializableVector2(v.x, v.y);
	}

	public static implicit operator Vector2(SerializableVector2 v)
	{
		return new Vector2(v.x, v.y);
	}
}
