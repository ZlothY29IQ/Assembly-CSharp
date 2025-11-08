using System.Collections;
using GorillaTagScripts;
using Photon.Pun;
using UnityEngine;

public class BuilderDispenser : MonoBehaviour
{
	public Transform displayTransform;

	public Transform spawnTransform;

	public Animation animateParent;

	public AnimationClip dispenseDefaultAnimation;

	public GameObject dispenserFX;

	private AnimationClip currentAnimation;

	[HideInInspector]
	public BuilderTable table;

	[HideInInspector]
	public int shelfID;

	private BuilderPieceSet.PieceInfo pieceToSpawn;

	private BuilderPiece spawnedPieceInstance;

	private int materialType = -1;

	private BuilderPieceSet.PieceInfo nullPiece;

	private int spawnCount;

	private double nextSpawnTime;

	private bool hasPiece;

	private float OnGrabSpawnDelay = 0.5f;

	private float spawnRetryDelay = 2f;

	private bool playFX;

	private void Awake()
	{
		nullPiece = new BuilderPieceSet.PieceInfo
		{
			piecePrefab = null,
			overrideSetMaterial = false
		};
	}

	public void UpdateDispenser()
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		if (!hasPiece && Time.timeAsDouble > nextSpawnTime && pieceToSpawn.piecePrefab != null)
		{
			TrySpawnPiece();
			nextSpawnTime = Time.timeAsDouble + (double)spawnRetryDelay;
		}
		else if (hasPiece && (spawnedPieceInstance == null || (spawnedPieceInstance.state != BuilderPiece.State.OnShelf && spawnedPieceInstance.state != BuilderPiece.State.Displayed)))
		{
			StopAllCoroutines();
			if (spawnedPieceInstance != null)
			{
				spawnedPieceInstance.shelfOwner = -1;
			}
			nextSpawnTime = Time.timeAsDouble + (double)OnGrabSpawnDelay;
			spawnedPieceInstance = null;
			hasPiece = false;
		}
	}

	public bool DoesPieceMatchSpawnInfo(BuilderPiece piece)
	{
		if (piece == null || pieceToSpawn.piecePrefab == null)
		{
			return false;
		}
		if (piece.pieceType != pieceToSpawn.piecePrefab.name.GetStaticHash())
		{
			return false;
		}
		if (!(piece.materialOptions != null))
		{
			return true;
		}
		int num = piece.materialType;
		piece.materialOptions.GetDefaultMaterial(out var num2, out var _, out var _);
		if (pieceToSpawn.overrideSetMaterial)
		{
			for (int i = 0; i < pieceToSpawn.pieceMaterialTypes.Length; i++)
			{
				string text = pieceToSpawn.pieceMaterialTypes[i];
				if (!string.IsNullOrEmpty(text))
				{
					int hashCode = text.GetHashCode();
					if (hashCode == num)
					{
						return true;
					}
					if (hashCode == num2 && num == -1)
					{
						return true;
					}
				}
				else if (num == -1 || num == num2)
				{
					return true;
				}
			}
		}
		else if (num == materialType || (materialType == num2 && num == -1) || (num == num2 && materialType == -1))
		{
			return true;
		}
		return false;
	}

	public void ShelfPieceCreated(BuilderPiece piece, bool playAnimation)
	{
		if (!DoesPieceMatchSpawnInfo(piece))
		{
			return;
		}
		if (hasPiece && spawnedPieceInstance != null)
		{
			spawnedPieceInstance.shelfOwner = -1;
		}
		spawnedPieceInstance = piece;
		hasPiece = true;
		spawnCount++;
		spawnCount = Mathf.Max(0, spawnCount);
		if (table.GetTableState() == BuilderTable.TableState.Ready && playAnimation)
		{
			StartCoroutine(PlayAnimation());
			if (playFX)
			{
				ObjectPools.instance.Instantiate(dispenserFX, spawnTransform.position, spawnTransform.rotation);
			}
			else
			{
				playFX = true;
			}
		}
		else
		{
			Vector3 desiredShelfOffset = pieceToSpawn.piecePrefab.desiredShelfOffset;
			Vector3 position = displayTransform.position + displayTransform.rotation * desiredShelfOffset;
			Quaternion rotation = displayTransform.rotation * Quaternion.Euler(pieceToSpawn.piecePrefab.desiredShelfRotationOffset);
			spawnedPieceInstance.transform.SetPositionAndRotation(position, rotation);
			spawnedPieceInstance.SetState(BuilderPiece.State.OnShelf);
			playFX = true;
		}
	}

	private IEnumerator PlayAnimation()
	{
		spawnedPieceInstance.SetState(BuilderPiece.State.Displayed);
		animateParent.Rewind();
		spawnedPieceInstance.transform.SetParent(animateParent.transform);
		spawnedPieceInstance.transform.SetLocalPositionAndRotation(pieceToSpawn.piecePrefab.desiredShelfOffset, Quaternion.Euler(pieceToSpawn.piecePrefab.desiredShelfRotationOffset));
		animateParent.Play();
		yield return new WaitForSeconds(animateParent.clip.length);
		if (spawnedPieceInstance != null && spawnedPieceInstance.state == BuilderPiece.State.Displayed)
		{
			spawnedPieceInstance.transform.SetParent(null);
			Vector3 desiredShelfOffset = pieceToSpawn.piecePrefab.desiredShelfOffset;
			Vector3 position = displayTransform.position + displayTransform.rotation * desiredShelfOffset;
			Quaternion rotation = displayTransform.rotation * Quaternion.Euler(pieceToSpawn.piecePrefab.desiredShelfRotationOffset);
			spawnedPieceInstance.transform.SetPositionAndRotation(position, rotation);
			spawnedPieceInstance.SetState(BuilderPiece.State.OnShelf);
		}
	}

	public void ShelfPieceRecycled(BuilderPiece piece)
	{
		if (piece != null && spawnedPieceInstance != null && piece.Equals(spawnedPieceInstance))
		{
			piece.shelfOwner = -1;
			spawnedPieceInstance = null;
			hasPiece = false;
			nextSpawnTime = Time.timeAsDouble + (double)OnGrabSpawnDelay;
		}
	}

	public void AssignPieceType(BuilderPieceSet.PieceInfo piece, int inMaterialType)
	{
		playFX = false;
		pieceToSpawn = piece;
		materialType = inMaterialType;
		nextSpawnTime = Time.timeAsDouble + (double)OnGrabSpawnDelay;
		currentAnimation = dispenseDefaultAnimation;
		animateParent.clip = currentAnimation;
		spawnCount = 0;
	}

	private void TrySpawnPiece()
	{
		if ((!(spawnedPieceInstance != null) || spawnedPieceInstance.state != BuilderPiece.State.OnShelf) && !(pieceToSpawn.piecePrefab == null) && table.HasEnoughResources(pieceToSpawn.piecePrefab))
		{
			Vector3 desiredShelfOffset = pieceToSpawn.piecePrefab.desiredShelfOffset;
			Vector3 position = spawnTransform.position + spawnTransform.rotation * desiredShelfOffset;
			Quaternion rotation = spawnTransform.rotation * Quaternion.Euler(pieceToSpawn.piecePrefab.desiredShelfRotationOffset);
			int num = materialType;
			if (pieceToSpawn.overrideSetMaterial && pieceToSpawn.pieceMaterialTypes.Length != 0)
			{
				int num2 = spawnCount % pieceToSpawn.pieceMaterialTypes.Length;
				string text = pieceToSpawn.pieceMaterialTypes[num2];
				num = ((!string.IsNullOrEmpty(text)) ? text.GetHashCode() : (-1));
			}
			table.RequestCreateDispenserShelfPiece(pieceToSpawn.piecePrefab.name.GetStaticHash(), position, rotation, num, shelfID);
		}
	}

	public void ParentPieceToShelf(Transform shelfTransform)
	{
		if (!(spawnedPieceInstance != null))
		{
			return;
		}
		if (spawnedPieceInstance.state != BuilderPiece.State.OnShelf && spawnedPieceInstance.state != BuilderPiece.State.Displayed)
		{
			StopAllCoroutines();
			if (spawnedPieceInstance != null)
			{
				spawnedPieceInstance.shelfOwner = -1;
			}
			nextSpawnTime = Time.timeAsDouble + (double)OnGrabSpawnDelay;
			spawnedPieceInstance = null;
			hasPiece = false;
		}
		else
		{
			spawnedPieceInstance.SetState(BuilderPiece.State.Displayed);
			spawnedPieceInstance.transform.SetParent(shelfTransform);
		}
	}

	public void ClearDispenser()
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		pieceToSpawn = nullPiece;
		hasPiece = false;
		if (spawnedPieceInstance != null)
		{
			if (spawnedPieceInstance.state != BuilderPiece.State.OnShelf && spawnedPieceInstance.state != BuilderPiece.State.Displayed)
			{
				spawnedPieceInstance.shelfOwner = -1;
				nextSpawnTime = Time.timeAsDouble + (double)OnGrabSpawnDelay;
				spawnedPieceInstance = null;
			}
			else
			{
				table.RequestRecyclePiece(spawnedPieceInstance, playFX: false, -1);
			}
		}
	}

	public void OnClearTable()
	{
		playFX = false;
		nextSpawnTime = 0.0;
		hasPiece = false;
		spawnedPieceInstance = null;
	}
}
