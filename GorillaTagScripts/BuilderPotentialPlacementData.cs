using UnityEngine;

namespace GorillaTagScripts;

public struct BuilderPotentialPlacementData
{
	public int pieceId;

	public int parentPieceId;

	public float score;

	public Vector3 localPosition;

	public Quaternion localRotation;

	public int attachIndex;

	public int parentAttachIndex;

	public float attachDistance;

	public Vector3 attachPlaneNormal;

	public SnapBounds attachBounds;

	public SnapBounds parentAttachBounds;

	public byte twist;

	public sbyte bumpOffsetX;

	public sbyte bumpOffsetZ;

	public BuilderPotentialPlacement ToPotentialPlacement(BuilderTable table)
	{
		BuilderPotentialPlacement result = new BuilderPotentialPlacement
		{
			attachPiece = table.GetPiece(pieceId),
			parentPiece = table.GetPiece(parentPieceId),
			score = score,
			localPosition = localPosition,
			localRotation = localRotation,
			attachIndex = attachIndex,
			parentAttachIndex = parentAttachIndex,
			attachDistance = attachDistance,
			attachPlaneNormal = attachPlaneNormal,
			attachBounds = attachBounds,
			parentAttachBounds = parentAttachBounds,
			twist = twist,
			bumpOffsetX = bumpOffsetX,
			bumpOffsetZ = bumpOffsetZ
		};
		if (result.parentPiece != null)
		{
			BuilderAttachGridPlane builderAttachGridPlane = result.parentPiece.gridPlanes[result.parentAttachIndex];
			result.localPosition = builderAttachGridPlane.transform.InverseTransformPoint(result.localPosition);
			result.localRotation = Quaternion.Inverse(builderAttachGridPlane.transform.rotation) * result.localRotation;
		}
		return result;
	}
}
