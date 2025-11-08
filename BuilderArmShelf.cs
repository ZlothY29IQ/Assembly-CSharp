using GorillaTagScripts;
using UnityEngine;

public class BuilderArmShelf : MonoBehaviour
{
	[HideInInspector]
	public BuilderPiece piece;

	public Transform pieceAnchor;

	private VRRig ownerRig;

	private void Start()
	{
		ownerRig = GetComponentInParent<VRRig>();
	}

	public bool IsOwnedLocally()
	{
		if (ownerRig != null)
		{
			return ownerRig.isLocal;
		}
		return false;
	}

	public bool CanAttachToArmPiece()
	{
		if (ownerRig != null)
		{
			return ownerRig.scaleFactor >= 1f;
		}
		return false;
	}

	public void DropAttachedPieces()
	{
		if (!(ownerRig != null) || !(piece != null))
		{
			return;
		}
		Vector3 zero = Vector3.zero;
		if (!(piece.firstChildPiece == null))
		{
			BuilderTable table = piece.GetTable();
			Vector3 vector = table.roomCenter.position - piece.transform.position;
			vector.Normalize();
			Vector3 vector2 = Quaternion.Euler(0f, 180f, 0f) * vector;
			zero = BuilderTable.DROP_ZONE_REPEL * vector2;
			BuilderPiece builderPiece = piece.firstChildPiece;
			while (builderPiece != null)
			{
				table.RequestDropPiece(builderPiece, builderPiece.transform.position + vector2 * 0.1f, builderPiece.transform.rotation, zero, Vector3.zero);
				builderPiece = builderPiece.nextSiblingPiece;
			}
		}
	}
}
