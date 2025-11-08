using System;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
[WeaverGenerated]
[NetworkStructWeaved(3)]
internal struct FixedStorage_00403 : INetworkStruct
{
	[FieldOffset(0)]
	[WeaverGenerated]
	public unsafe fixed int Data[3];

	[FieldOffset(4)]
	[NonSerialized]
	[WeaverGenerated]
	private int _1;

	[FieldOffset(8)]
	[NonSerialized]
	[WeaverGenerated]
	private int _2;
}
