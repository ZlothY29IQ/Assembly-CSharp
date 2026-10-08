using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

[StructLayout(LayoutKind.Explicit, Size = 44)]
[NetworkStructWeaved(11)]
public struct SkeletonNetData : INetworkStruct
{
	[FieldOffset(4)]
	public Vector3 Position;

	[FieldOffset(16)]
	public Quaternion Rotation;

	[field: FieldOffset(0)]
	public int CurrentState { get; set; }

	[field: FieldOffset(32)]
	public int CurrentNode { get; set; }

	[field: FieldOffset(36)]
	public int NextNode { get; set; }

	[field: FieldOffset(40)]
	public int AngerPoint { get; set; }

	public SkeletonNetData(int state, Vector3 pos, Quaternion rot, int cNode, int nNode, int angerPoint)
	{
		CurrentState = state;
		Position = pos;
		Rotation = rot;
		CurrentNode = cNode;
		NextNode = nNode;
		AngerPoint = angerPoint;
	}
}
