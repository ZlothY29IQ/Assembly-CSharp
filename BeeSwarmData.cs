using System.Runtime.InteropServices;
using Fusion;

[StructLayout(LayoutKind.Explicit, Size = 12)]
[NetworkStructWeaved(3)]
public struct BeeSwarmData : INetworkStruct
{
	[field: FieldOffset(0)]
	public int TargetActorNumber { get; set; }

	[field: FieldOffset(4)]
	public int CurrentState { get; set; }

	[field: FieldOffset(8)]
	public float CurrentSpeed { get; set; }

	public BeeSwarmData(int actorNr, int state, float speed)
	{
		TargetActorNumber = actorNr;
		CurrentState = state;
		CurrentSpeed = speed;
	}
}
