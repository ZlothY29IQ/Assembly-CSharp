using System;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
public struct GlobalObjectRef
{
	[FieldOffset(0)]
	public ulong targetObjectId;

	[FieldOffset(8)]
	public ulong targetPrefabId;

	[FieldOffset(16)]
	public Guid assetGUID;

	[FieldOffset(32)]
	[HideInInspector]
	public int identifierType;

	[FieldOffset(32)]
	[NonSerialized]
	private GlobalObjectRefType refType;

	public static GlobalObjectRef ObjectToRefSlow(UnityEngine.Object target)
	{
		return default(GlobalObjectRef);
	}

	public static UnityEngine.Object RefToObjectSlow(GlobalObjectRef @ref)
	{
		return null;
	}
}
