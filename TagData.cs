using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 88)]
[NetworkStructWeaved(22)]
public struct TagData : INetworkStruct
{
	[FieldOffset(4)]
	public NetworkBool isCurrentlyTag;

	[FieldOffset(8)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 20, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004020 _infectedPlayerList;

	[Networked]
	[Capacity(20)]
	[NetworkedWeavedArray(20, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(2, 20)]
	public unsafe NetworkArray<int> infectedPlayerList => new NetworkArray<int>(Native.ReferenceToPointer(ref _infectedPlayerList), 20, Fusion.ElementReaderWriterInt32.GetInstance());

	[field: FieldOffset(0)]
	public int currentItID { get; set; }
}
