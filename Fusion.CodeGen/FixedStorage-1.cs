using System;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
[WeaverGenerated]
[NetworkStructWeaved(1)]
internal struct FixedStorage_00401 : INetworkStruct
{
	[FieldOffset(0)]
	[WeaverGenerated]
	public unsafe fixed int Data[1];
}
