using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 44)]
[NetworkStructWeaved(11)]
public struct SkeletonNetData : INetworkStruct
{
	[FieldOffset(4)]
	[FixedBufferProperty(typeof(Vector3), typeof(UnityValueSurrogate_0040ElementReaderWriterVector3), 0, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_00403 _Position;

	[FieldOffset(16)]
	[FixedBufferProperty(typeof(Quaternion), typeof(UnityValueSurrogate_0040ReaderWriter_0040UnityEngine_Quaternion), 0, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_00404 _Rotation;

	[field: FieldOffset(0)]
	public int CurrentState { get; set; }

	[Networked]
	[NetworkedWeaved(1, 3)]
	public unsafe Vector3 Position
	{
		readonly get
		{
			return *(Vector3*)Native.ReferenceToPointer(ref _Position);
		}
		set
		{
			*(Vector3*)Native.ReferenceToPointer(ref _Position) = value;
		}
	}

	[Networked]
	[NetworkedWeaved(4, 4)]
	public unsafe Quaternion Rotation
	{
		readonly get
		{
			return *(Quaternion*)Native.ReferenceToPointer(ref _Rotation);
		}
		set
		{
			*(Quaternion*)Native.ReferenceToPointer(ref _Rotation) = value;
		}
	}

	[field: FieldOffset(32)]
	public int CurrentNode { get; set; }

	[field: FieldOffset(36)]
	public int NextNode { get; set; }

	[field: FieldOffset(40)]
	public int AngerPoint { get; set; }

	public SkeletonNetData(int state, Vector3 pos, Quaternion rot, int cNode, int nNode, int angerPoint)
	{
		CurrentState = state;
		Position = pos;
		Rotation = rot;
		CurrentNode = cNode;
		NextNode = nNode;
		AngerPoint = angerPoint;
	}
}
