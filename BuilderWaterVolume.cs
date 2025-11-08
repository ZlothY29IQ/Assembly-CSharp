using UnityEngine;
using UnityEngine.Serialization;

public class BuilderWaterVolume : MonoBehaviour, IBuilderPieceComponent
{
	[SerializeField]
	private BuilderPiece piece;

	[SerializeField]
	private GameObject waterVolume;

	[SerializeField]
	private GameObject waterMesh;

	[FormerlySerializedAs("lillyPads")]
	[SerializeField]
	private Transform floatingObjects;

	[SerializeField]
	private Transform floating;

	[SerializeField]
	private Transform sunk;

	public void OnPieceCreate(int pieceType, int pieceId)
	{
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
		bool flag = (double)Vector3.Dot(Vector3.up, base.transform.up) > 0.5 && !piece.IsPieceMoving();
		waterVolume.SetActive(flag);
		waterMesh.SetActive(flag);
		if (floatingObjects != null)
		{
			floatingObjects.localPosition = (flag ? floating.localPosition : sunk.localPosition);
		}
	}

	public void OnPieceActivate()
	{
		bool flag = (double)Vector3.Dot(Vector3.up, base.transform.up) > 0.5 && !piece.IsPieceMoving();
		waterVolume.SetActive(flag);
		waterMesh.SetActive(flag);
		if (floatingObjects != null)
		{
			floatingObjects.localPosition = (flag ? floating.localPosition : sunk.localPosition);
		}
	}

	public void OnPieceDeactivate()
	{
		waterVolume.SetActive(value: false);
		waterMesh.SetActive(value: true);
		if (floatingObjects != null)
		{
			floatingObjects.localPosition = floating.localPosition;
		}
	}
}
