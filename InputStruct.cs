using System;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Explicit, Size = 168)]
[NetworkStructWeaved(42)]
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
	public int rotation;

	[FieldOffset(52)]
	public int packedFields;

	[FieldOffset(56)]
	public short packedCompetitiveData;

	[FieldOffset(60)]
	public Vector3 velocity;

	[FieldOffset(72)]
	public int grabbedRopeIndex;

	[FieldOffset(76)]
	public int ropeBoneIndex;

	[FieldOffset(80)]
	public bool ropeGrabIsLeft;

	[FieldOffset(84)]
	public bool ropeGrabIsBody;

	[FieldOffset(88)]
	public Vector3 ropeGrabOffset;

	[FieldOffset(100)]
	public bool movingSurfaceIsMonkeBlock;

	[FieldOffset(104)]
	public long hoverboardPosRot;

	[FieldOffset(112)]
	public short hoverboardColor;

	[FieldOffset(116)]
	public long propHuntPosRot;

	[FieldOffset(124)]
	public double serverTimeStamp;

	[FieldOffset(132)]
	public short taggedById;

	[FieldOffset(136)]
	public bool isGroundedHand;

	[FieldOffset(140)]
	public bool isGroundedButt;

	[FieldOffset(144)]
	public int leftHandGrabbedActorNumber;

	[FieldOffset(148)]
	public bool leftGrabbedHandIsLeft;

	[FieldOffset(152)]
	public int rightHandGrabbedActorNumber;

	[FieldOffset(156)]
	public bool rightGrabbedHandIsLeft;

	[FieldOffset(160)]
	public float lastTouchedGroundAtTime;

	[FieldOffset(164)]
	public float lastHandTouchedGroundAtTime;
}
