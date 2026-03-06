using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 244)]
[NetworkStructWeaved(61)]
public struct PaintbrawlData : INetworkStruct
{
	[FieldOffset(0)]
	public GorillaPaintbrawlManager.PaintbrawlState currentPaintbrawlState;

	[FieldOffset(4)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 20, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004020 _playerLivesArray;

	[FieldOffset(84)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 20, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004020 _playerActorNumberArray;

	[FieldOffset(164)]
	[FixedBufferProperty(typeof(NetworkArray<GorillaPaintbrawlManager.PaintbrawlStatus>), typeof(UnityArraySurrogate_0040ReaderWriter_0040GorillaPaintbrawlManager__PaintbrawlStatus), 20, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_004020 _playerStatusArray;

	[Networked]
	[Capacity(20)]
	[NetworkedWeavedArray(20, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(1, 20)]
	public unsafe NetworkArray<int> playerLivesArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _playerLivesArray), 20, Fusion.ElementReaderWriterInt32.GetInstance());

	[Networked]
	[Capacity(20)]
	[NetworkedWeavedArray(20, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(21, 20)]
	public unsafe NetworkArray<int> playerActorNumberArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _playerActorNumberArray), 20, Fusion.ElementReaderWriterInt32.GetInstance());

	[Networked]
	[Capacity(20)]
	[NetworkedWeavedArray(20, 1, typeof(ReaderWriter_0040GorillaPaintbrawlManager__PaintbrawlStatus))]
	[NetworkedWeaved(41, 20)]
	public unsafe NetworkArray<GorillaPaintbrawlManager.PaintbrawlStatus> playerStatusArray => new NetworkArray<GorillaPaintbrawlManager.PaintbrawlStatus>(Native.ReferenceToPointer(ref _playerStatusArray), 20, ReaderWriter_0040GorillaPaintbrawlManager__PaintbrawlStatus.GetInstance());
}
