using System;
using System.Runtime.InteropServices;
using Fusion;

[Serializable]
[StructLayout(LayoutKind.Explicit, Size = 4)]
[NetworkStructWeaved(1)]
public struct HitTargetStruct(int v) : INetworkStruct
{
	[FieldOffset(0)]
	public int Score = v;
}
