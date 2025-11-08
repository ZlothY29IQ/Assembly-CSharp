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
		BuilderPotentialPlacement builderPotentialPlacement = default(BuilderPotentialPlacement);
		builderPotentialPlacement.attachPiece = table.GetPiece(pieceId);
		builderPotentialPlacement.parentPiece = table.GetPiece(parentPieceId);
		builderPotentialPlacement.score = score;
		builderPotentialPlacement.localPosition = localPosition;
		builderPotentialPlacement.localRotation = localRotation;
		builderPotentialPlacement.attachIndex = attachIndex;
		builderPotentialPlacement.parentAttachIndex = parentAttachIndex;
		builderPotentialPlacement.attachDistance = attachDistance;
		builderPotentialPlacement.attachPlaneNormal = attachPlaneNormal;
		builderPotentialPlacement.attachBounds = attachBounds;
		builderPotentialPlacement.parentAttachBounds = parentAttachBounds;
		builderPotentialPlacement.twist = twist;
		builderPotentialPlacement.bumpOffsetX = bumpOffsetX;
		builderPotentialPlacement.bumpOffsetZ = bumpOffsetZ;
		BuilderPotentialPlacement result = builderPotentialPlacement;
		if (result.parentPiece != null)
		{
			BuilderAttachGridPlane builderAttachGridPlane = result.parentPiece.gridPlanes[result.parentAttachIndex];
			result.localPosition = builderAttachGridPlane.transform.InverseTransformPoint(result.localPosition);
			result.localRotation = Quaternion.Inverse(builderAttachGridPlane.transform.rotation) * result.localRotation;
		}
		return result;
	}
}
