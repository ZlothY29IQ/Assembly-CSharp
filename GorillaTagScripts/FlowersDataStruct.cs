using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

namespace GorillaTagScripts;

[StructLayout(LayoutKind.Explicit, Size = 52)]
[NetworkStructWeaved(13)]
public struct FlowersDataStruct : INetworkStruct
{
	[FieldOffset(4)]
	[FixedBufferProperty(typeof(NetworkLinkedList<byte>), typeof(UnityLinkedListSurrogate_0040ElementReaderWriterByte), 1, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_00406 _FlowerWateredData;

	[FieldOffset(28)]
	[FixedBufferProperty(typeof(NetworkLinkedList<int>), typeof(UnityLinkedListSurrogate_0040ElementReaderWriterInt32), 1, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_00406 _FlowerStateData;

	[field: FieldOffset(0)]
	public int FlowerCount { get; set; }

	[Networked]
	[NetworkedWeavedLinkedList(1, 1, typeof(Fusion.ElementReaderWriterByte))]
	[NetworkedWeaved(1, 6)]
	public unsafe NetworkLinkedList<byte> FlowerWateredData => new NetworkLinkedList<byte>(Native.ReferenceToPointer(ref _FlowerWateredData), 1, Fusion.ElementReaderWriterByte.GetInstance());

	[Networked]
	[NetworkedWeavedLinkedList(1, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(7, 6)]
	public unsafe NetworkLinkedList<int> FlowerStateData => new NetworkLinkedList<int>(Native.ReferenceToPointer(ref _FlowerStateData), 1, Fusion.ElementReaderWriterInt32.GetInstance());

	public FlowersDataStruct(List<Flower> allFlowers)
	{
		FlowerCount = allFlowers.Count;
		foreach (Flower allFlower in allFlowers)
		{
			FlowerWateredData.Add((byte)(allFlower.IsWatered ? 1u : 0u));
			FlowerStateData.Add((int)allFlower.GetCurrentState());
		}
	}
}
