using System;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Explicit, Size = 164)]
[NetworkStructWeaved(41)]
public struct InputStruct : INetworkStruct
{
	[FieldOffset(0)]
	public int headRotation;

	[FieldOffset(4)]
	public bool usingNewIK;

	[FieldOffset(8)]
	public int bodyRotation;

	[FieldOffset(12)]
	public short leftUpperArmRotation;

	[FieldOffset(16)]
	public short rightUpperArmRotation;

	[FieldOffset(20)]
	public long rightHandLong;

	[FieldOffset(28)]
	public long leftHandLong;

	[FieldOffset(36)]
	public long position;

	[FieldOffset(44)]
	public int handPosition;

	[FieldOffset(48)]
	public int packedFields;

	[FieldOffset(52)]
	public short packedCompetitiveData;

	[FieldOffset(56)]
	public Vector3 velocity;

	[FieldOffset(68)]
	public int grabbedRopeIndex;

	[FieldOffset(72)]
	public int ropeBoneIndex;

	[FieldOffset(76)]
	public bool ropeGrabIsLeft;

	[FieldOffset(80)]
	public bool ropeGrabIsBody;

	[FieldOffset(84)]
	public Vector3 ropeGrabOffset;

	[FieldOffset(96)]
	public bool movingSurfaceIsMonkeBlock;

	[FieldOffset(100)]
	public long hoverboardPosRot;

	[FieldOffset(108)]
	public short hoverboardColor;

	[FieldOffset(112)]
	public long propHuntPosRot;

	[FieldOffset(120)]
	public double serverTimeStamp;

	[FieldOffset(128)]
	public short taggedById;

	[FieldOffset(132)]
	public bool isGroundedHand;

	[FieldOffset(136)]
	public bool isGroundedButt;

	[FieldOffset(140)]
	public int leftHandGrabbedActorNumber;

	[FieldOffset(144)]
	public bool leftGrabbedHandIsLeft;

	[FieldOffset(148)]
	public int rightHandGrabbedActorNumber;

	[FieldOffset(152)]
	public bool rightGrabbedHandIsLeft;

	[FieldOffset(156)]
	public float lastTouchedGroundAtTime;

	[FieldOffset(160)]
	public float lastHandTouchedGroundAtTime;
}
