using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 172)]
[NetworkStructWeaved(43)]
public struct HuntData : INetworkStruct
{
	[FieldOffset(0)]
	public NetworkBool huntStarted;

	[FieldOffset(4)]
	public NetworkBool waitingToStartNextHuntGame;

	[FieldOffset(8)]
	public int countDownTime;

	[FieldOffset(12)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 20, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004020 _currentHuntedArray;

	[FieldOffset(92)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 20, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004020 _currentTargetArray;

	[Networked]
	[Capacity(20)]
	[NetworkedWeavedArray(20, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(3, 20)]
	public unsafe NetworkArray<int> currentHuntedArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _currentHuntedArray), 20, Fusion.ElementReaderWriterInt32.GetInstance());

	[Networked]
	[Capacity(20)]
	[NetworkedWeavedArray(20, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(23, 20)]
	public unsafe NetworkArray<int> currentTargetArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _currentTargetArray), 20, Fusion.ElementReaderWriterInt32.GetInstance());
}
