using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 48)]
[NetworkStructWeaved(12)]
public struct TagData : INetworkStruct
{
	[FieldOffset(4)]
	public NetworkBool isCurrentlyTag;

	[FieldOffset(8)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 10, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004010 _infectedPlayerList;

	[Networked]
	[Capacity(10)]
	[NetworkedWeavedArray(10, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(2, 10)]
	public unsafe NetworkArray<int> infectedPlayerList => new NetworkArray<int>(Native.ReferenceToPointer(ref _infectedPlayerList), 10, Fusion.ElementReaderWriterInt32.GetInstance());

	[field: FieldOffset(0)]
	public int currentItID { get; set; }
}
