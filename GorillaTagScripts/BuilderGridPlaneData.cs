using UnityEngine;

namespace GorillaTagScripts;

public struct BuilderGridPlaneData
{
	public int width;

	public int length;

	public bool male;

	public int pieceId;

	public int pieceIndex;

	public float boundingRadius;

	public int attachIndex;

	public Vector3 position;

	public Quaternion rotation;

	public Vector3 localPosition;

	public Quaternion localRotation;

	public BuilderGridPlaneData(BuilderAttachGridPlane gridPlane, int pieceIndex)
	{
		gridPlane.center.transform.GetPositionAndRotation(out position, out rotation);
		localPosition = gridPlane.pieceToGridPosition;
		localRotation = gridPlane.pieceToGridRotation;
		width = gridPlane.width;
		length = gridPlane.length;
		male = gridPlane.male;
		pieceId = gridPlane.piece.pieceId;
		this.pieceIndex = pieceIndex;
		boundingRadius = gridPlane.boundingRadius;
		attachIndex = gridPlane.attachIndex;
	}
}
