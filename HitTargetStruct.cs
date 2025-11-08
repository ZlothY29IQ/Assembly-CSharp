using System;
using System.Runtime.InteropServices;
using Fusion;

[Serializable]
[StructLayout(LayoutKind.Explicit, Size = 4)]
[NetworkStructWeaved(1)]
public struct HitTargetStruct : INetworkStruct
{
	[FieldOffset(0)]
	public int Score;

	public HitTargetStruct(int v)
	{
		Score = v;
	}
}
