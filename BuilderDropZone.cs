using System.Collections;
using GorillaTagScripts;
using Photon.Pun;
using UnityEngine;

public class BuilderDropZone : MonoBehaviour
{
	public enum DropType
	{
		Invalid = -1,
		Repel,
		ReturnToShelf,
		BreakApart,
		Recycle
	}

	[SerializeField]
	private DropType dropType;

	[SerializeField]
	private bool onEnter = true;

	[SerializeField]
	private GameObject vfxRoot;

	[SerializeField]
	private GameObject sfxPrefab;

	public float effectDuration = 1f;

	private bool playingEffect;

	public bool overrideDirection;

	[SerializeField]
	private Vector3 repelDirectionLocal = Vector3.up;

	private Vector3 repelDirectionWorld = Vector3.up;

	[HideInInspector]
	public int dropZoneID = -1;

	internal BuilderTable table;

	private void Awake()
	{
		repelDirectionWorld = base.transform.TransformDirection(repelDirectionLocal.normalized);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!onEnter || !PhotonNetwork.IsMasterClient)
		{
			return;
		}
		BuilderPieceCollider component = other.GetComponent<BuilderPieceCollider>();
		if (!(component != null))
		{
			return;
		}
		BuilderPiece piece = component.piece;
		if (!(table != null) || !(table.builderNetworking != null) || piece == null)
		{
			return;
		}
		if (dropType == DropType.Recycle)
		{
			bool flag = piece.state != BuilderPiece.State.Displayed && piece.state != BuilderPiece.State.OnShelf && piece.state != BuilderPiece.State.AttachedAndPlaced;
			if (!piece.isBuiltIntoTable && flag)
			{
				table.builderNetworking.RequestRecyclePiece(piece.pieceId, piece.transform.position, piece.transform.rotation, playFX: true, -1);
			}
		}
		else
		{
			table.builderNetworking.PieceEnteredDropZone(piece, dropType, dropZoneID);
		}
	}

	public Vector3 GetRepelDirectionWorld()
	{
		return repelDirectionWorld;
	}

	public void PlayEffect()
	{
		if (vfxRoot != null && !playingEffect)
		{
			vfxRoot.SetActive(value: true);
			playingEffect = true;
			if (sfxPrefab != null)
			{
				ObjectPools.instance.Instantiate(sfxPrefab, base.transform.position, base.transform.rotation);
			}
			StartCoroutine(DelayedStopEffect());
		}
	}

	private IEnumerator DelayedStopEffect()
	{
		yield return new WaitForSeconds(effectDuration);
		vfxRoot.SetActive(value: false);
		playingEffect = false;
	}

	private void OnTriggerExit(Collider other)
	{
		if (onEnter || !PhotonNetwork.IsMasterClient)
		{
			return;
		}
		BuilderPieceCollider component = other.GetComponent<BuilderPieceCollider>();
		if (!(component != null))
		{
			return;
		}
		BuilderPiece piece = component.piece;
		if (!(table != null) || !(table.builderNetworking != null) || piece == null)
		{
			return;
		}
		if (dropType == DropType.Recycle)
		{
			bool flag = piece.state != BuilderPiece.State.Displayed && piece.state != BuilderPiece.State.OnShelf && piece.state != BuilderPiece.State.AttachedAndPlaced;
			if (!piece.isBuiltIntoTable && flag)
			{
				table.builderNetworking.RequestRecyclePiece(piece.pieceId, piece.transform.position, piece.transform.rotation, playFX: true, -1);
			}
		}
		else
		{
			table.builderNetworking.PieceEnteredDropZone(piece, dropType, dropZoneID);
		}
	}
}
