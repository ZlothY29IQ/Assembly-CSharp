using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 124)]
[NetworkStructWeaved(31)]
public struct PaintbrawlData : INetworkStruct
{
	[FieldOffset(0)]
	public GorillaPaintbrawlManager.PaintbrawlState currentPaintbrawlState;

	[FieldOffset(4)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 10, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004010 _playerLivesArray;

	[FieldOffset(44)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 10, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004010 _playerActorNumberArray;

	[FieldOffset(84)]
	[FixedBufferProperty(typeof(NetworkArray<GorillaPaintbrawlManager.PaintbrawlStatus>), typeof(UnityArraySurrogate_0040ReaderWriter_0040GorillaPaintbrawlManager__PaintbrawlStatus), 10, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004010 _playerStatusArray;

	[Networked]
	[Capacity(10)]
	[NetworkedWeavedArray(10, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(1, 10)]
	public unsafe NetworkArray<int> playerLivesArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _playerLivesArray), 10, Fusion.ElementReaderWriterInt32.GetInstance());

	[Networked]
	[Capacity(10)]
	[NetworkedWeavedArray(10, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(11, 10)]
	public unsafe NetworkArray<int> playerActorNumberArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _playerActorNumberArray), 10, Fusion.ElementReaderWriterInt32.GetInstance());

	[Networked]
	[Capacity(10)]
	[NetworkedWeavedArray(10, 1, typeof(ReaderWriter_0040GorillaPaintbrawlManager__PaintbrawlStatus))]
	[NetworkedWeaved(21, 10)]
	public unsafe NetworkArray<GorillaPaintbrawlManager.PaintbrawlStatus> playerStatusArray => new NetworkArray<GorillaPaintbrawlManager.PaintbrawlStatus>(Native.ReferenceToPointer(ref _playerStatusArray), 10, ReaderWriter_0040GorillaPaintbrawlManager__PaintbrawlStatus.GetInstance());
}
