using System;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Explicit, Size = 84)]
[NetworkStructWeaved(21)]
public struct ReliableStateData : INetworkStruct
{
	[FieldOffset(44)]
	[FixedBufferProperty(typeof(NetworkArray<long>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt64), 5, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004010 _TransferrableStates;

	[field: FieldOffset(0)]
	public long Header { get; set; }

	[Networked]
	[Capacity(5)]
	[NetworkedWeavedArray(5, 2, typeof(Fusion.ElementReaderWriterInt64))]
	[NetworkedWeaved(11, 10)]
	public unsafe NetworkArray<long> TransferrableStates => new NetworkArray<long>(Native.ReferenceToPointer(ref _TransferrableStates), 5, Fusion.ElementReaderWriterInt64.GetInstance());

	[field: FieldOffset(8)]
	public int WearablesPackedState { get; set; }

	[field: FieldOffset(12)]
	public int LThrowableProjectileIndex { get; set; }

	[field: FieldOffset(16)]
	public int RThrowableProjectileIndex { get; set; }

	[field: FieldOffset(20)]
	public int SizeLayerMask { get; set; }

	[field: FieldOffset(24)]
	public int RandomThrowableIndex { get; set; }

	[field: FieldOffset(28)]
	public long PackedBeads { get; set; }

	[field: FieldOffset(36)]
	public long PackedBeadsMoreThan6 { get; set; }
}
