using UnityEngine;

namespace GorillaTagScripts;

public struct BuilderPotentialPlacement
{
	public BuilderPiece attachPiece;

	public BuilderPiece parentPiece;

	public int attachIndex;

	public int parentAttachIndex;

	public Vector3 localPosition;

	public Quaternion localRotation;

	public Vector3 attachPlaneNormal;

	public float attachDistance;

	public float score;

	public SnapBounds attachBounds;

	public SnapBounds parentAttachBounds;

	public byte twist;

	public sbyte bumpOffsetX;

	public sbyte bumpOffsetZ;

	public void Reset()
	{
		attachPiece = null;
		parentPiece = null;
		attachIndex = -1;
		parentAttachIndex = -1;
		localPosition = Vector3.zero;
		localRotation = Quaternion.identity;
		attachDistance = float.MaxValue;
		attachPlaneNormal = Vector3.zero;
		score = float.MinValue;
		twist = 0;
		bumpOffsetX = 0;
		bumpOffsetZ = 0;
	}
}
