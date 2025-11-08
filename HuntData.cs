using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 92)]
[NetworkStructWeaved(23)]
public struct HuntData : INetworkStruct
{
	[FieldOffset(0)]
	public NetworkBool huntStarted;

	[FieldOffset(4)]
	public NetworkBool waitingToStartNextHuntGame;

	[FieldOffset(8)]
	public int countDownTime;

	[FieldOffset(12)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 10, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004010 _currentHuntedArray;

	[FieldOffset(52)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 10, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004010 _currentTargetArray;

	[Networked]
	[Capacity(10)]
	[NetworkedWeavedArray(10, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(3, 10)]
	public unsafe NetworkArray<int> currentHuntedArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _currentHuntedArray), 10, Fusion.ElementReaderWriterInt32.GetInstance());

	[Networked]
	[Capacity(10)]
	[NetworkedWeavedArray(10, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(13, 10)]
	public unsafe NetworkArray<int> currentTargetArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _currentTargetArray), 10, Fusion.ElementReaderWriterInt32.GetInstance());
}
