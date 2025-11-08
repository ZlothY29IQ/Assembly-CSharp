using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 44)]
[NetworkStructWeaved(11)]
public struct GhostLabData : INetworkStruct
{
	[FieldOffset(4)]
	[FixedBufferProperty(typeof(NetworkArray<NetworkBool>), typeof(UnityArraySurrogate_0040ElementReaderWriterNetworkBool), 10, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004010 _OpenDoors;

	[field: FieldOffset(0)]
	public int DoorState { get; set; }

	[Networked]
	[Capacity(10)]
	[NetworkedWeavedArray(10, 1, typeof(Fusion.ElementReaderWriterNetworkBool))]
	[NetworkedWeaved(1, 10)]
	public unsafe NetworkArray<NetworkBool> OpenDoors => new NetworkArray<NetworkBool>(Native.ReferenceToPointer(ref _OpenDoors), 10, Fusion.ElementReaderWriterNetworkBool.GetInstance());

	public GhostLabData(int state, bool[] openDoors)
	{
		DoorState = state;
		for (int i = 0; i < openDoors.Length; i++)
		{
			bool flag = openDoors[i];
			OpenDoors.Set(i, flag);
		}
	}
}
