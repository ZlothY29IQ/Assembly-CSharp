using System;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
[WeaverGenerated]
[NetworkStructWeaved(10)]
internal struct FixedStorage_004010 : INetworkStruct
{
	[FieldOffset(0)]
	[WeaverGenerated]
	public unsafe fixed int Data[10];

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

	[FieldOffset(24)]
	[NonSerialized]
	[WeaverGenerated]
	private int _6;

	[FieldOffset(28)]
	[NonSerialized]
	[WeaverGenerated]
	private int _7;

	[FieldOffset(32)]
	[NonSerialized]
	[WeaverGenerated]
	private int _8;

	[FieldOffset(36)]
	[NonSerialized]
	[WeaverGenerated]
	private int _9;
}
