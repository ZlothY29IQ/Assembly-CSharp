using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 1348)]
[NetworkStructWeaved(337)]
public struct FlockingData : INetworkStruct
{
	[FieldOffset(4)]
	[FixedBufferProperty(typeof(NetworkLinkedList<Vector3>), typeof(UnityLinkedListSurrogate_0040ElementReaderWriterVector3), 30, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_0040153 _Positions;

	[FieldOffset(616)]
	[FixedBufferProperty(typeof(NetworkLinkedList<Quaternion>), typeof(UnityLinkedListSurrogate_0040ReaderWriter_0040UnityEngine_Quaternion), 30, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_0040183 _Rotations;

	[field: FieldOffset(0)]
	public int count { get; set; }

	[Networked]
	[Capacity(30)]
	[NetworkedWeavedLinkedList(30, 3, typeof(Fusion.ElementReaderWriterVector3))]
	[NetworkedWeaved(1, 153)]
	public unsafe NetworkLinkedList<Vector3> Positions => new NetworkLinkedList<Vector3>(Native.ReferenceToPointer(ref _Positions), 30, Fusion.ElementReaderWriterVector3.GetInstance());

	[Networked]
	[Capacity(30)]
	[NetworkedWeavedLinkedList(30, 4, typeof(ReaderWriter_0040UnityEngine_Quaternion))]
	[NetworkedWeaved(154, 183)]
	public unsafe NetworkLinkedList<Quaternion> Rotations => new NetworkLinkedList<Quaternion>(Native.ReferenceToPointer(ref _Rotations), 30, ReaderWriter_0040UnityEngine_Quaternion.GetInstance());

	public FlockingData(List<Flocking> items)
	{
		count = items.Count;
		foreach (Flocking item in items)
		{
			Positions.Add(item.pos);
			Rotations.Add(item.rot);
		}
	}
}
