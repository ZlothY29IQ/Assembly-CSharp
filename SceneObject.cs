using System;
using UnityEngine;

[Serializable]
public class SceneObject : IEquatable<SceneObject>
{
	public int classID;

	public ulong fileID;

	[SerializeField]
	public string typeString;

	public string json;

	public Type GetObjectType()
	{
		if (string.IsNullOrWhiteSpace(typeString))
		{
			return null;
		}
		if (typeString.Contains("ProxyType"))
		{
			return ProxyType.Parse(typeString);
		}
		return Type.GetType(typeString);
	}

	public SceneObject(int classID, ulong fileID)
	{
		this.classID = classID;
		this.fileID = fileID;
		typeString = UnityYaml.ClassIDToType[classID].AssemblyQualifiedName;
	}

	public bool Equals(SceneObject other)
	{
		if (fileID == other.fileID)
		{
			return classID == other.classID;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is SceneObject other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int i = classID;
		int i2 = StaticHash.Compute((long)fileID);
		return StaticHash.Compute(i, i2);
	}

	public static bool operator ==(SceneObject x, SceneObject y)
	{
		return x.Equals(y);
	}

	public static bool operator !=(SceneObject x, SceneObject y)
	{
		return !x.Equals(y);
	}
}
