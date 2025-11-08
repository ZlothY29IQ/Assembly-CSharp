using System;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
[WeaverGenerated]
[NetworkStructWeaved(6)]
internal struct FixedStorage_00406 : INetworkStruct
{
	[FieldOffset(0)]
	[WeaverGenerated]
	public unsafe fixed int Data[6];

	[FieldOffset(4)]
	[NonSerialized]
	[WeaverGenerated]
	private int _1;

	[FieldOffset(8)]
	[NonSerialized]
	[WeaverGenerated]
	private int _2;

	[FieldOffset(12)]
	[NonSerialized]
	[WeaverGenerated]
	private int _3;

	[FieldOffset(16)]
	[NonSerialized]
	[WeaverGenerated]
	private int _4;

	[FieldOffset(20)]
	[NonSerialized]
	[WeaverGenerated]
	private int _5;
}
