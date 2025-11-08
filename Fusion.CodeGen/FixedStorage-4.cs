using System;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
[WeaverGenerated]
[NetworkStructWeaved(4)]
internal struct FixedStorage_00404 : INetworkStruct
{
	[FieldOffset(0)]
	[WeaverGenerated]
	public unsafe fixed int Data[4];

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
}
