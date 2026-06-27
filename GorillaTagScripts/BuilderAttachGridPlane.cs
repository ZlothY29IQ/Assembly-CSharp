using System;
using GorillaTagScripts.Builder;
using UnityEngine;

namespace GorillaTagScripts;

public class BuilderAttachGridPlane : MonoBehaviour
{
	[Tooltip("Are the snap points in this grid \"outies\"")]
	public bool male;

	[Tooltip("(Optional) midpoint of the grid")]
	public Transform center;

	[Tooltip("number of snap points wide (local X-axis)")]
	public int width;

	[Tooltip("number of snap points long (local z-axis)")]
	public int length;

	[NonSerialized]
	public int gridPlaneDataIndex;

	[NonSerialized]
	public BuilderItem item;

	[NonSerialized]
	public BuilderPiece piece;

	[NonSerialized]
	public int attachIndex;

	[NonSerialized]
	public float boundingRadius;

	[NonSerialized]
	public Vector3 pieceToGridPosition;

	[NonSerialized]
	public Quaternion pieceToGridRotation;

	[NonSerialized]
	public bool[] connected;

	[NonSerialized]
	public SnapOverlap firstOverlap;

	[NonSerialized]
	public float widthOffset;

	[NonSerialized]
	public float lengthOffset;

	private int childPieceCount;

	[HideInInspector]
	public bool isMoving;

	[HideInInspector]
	public bool movesOnPlace;

	[HideInInspector]
	public BuilderMovingPart movingPart;

	private void Awake()
	{
		if (center == null)
		{
			center = base.transform;
		}
	}

	public void Setup(BuilderPiece piece, int attachIndex, float gridSize)
	{
		this.piece = piece;
		this.attachIndex = attachIndex;
		pieceToGridPosition = piece.transform.InverseTransformPoint(base.transform.position);
		pieceToGridRotation = Quaternion.Inverse(piece.transform.rotation) * base.transform.rotation;
		float num = (float)(width + 2) * gridSize;
		float num2 = (float)(length + 2) * gridSize;
		boundingRadius = Mathf.Sqrt(num * num + num2 * num2);
		connected = new bool[width * length];
		widthOffset = ((width % 2 == 0) ? (gridSize / 2f) : 0f);
		lengthOffset = ((length % 2 == 0) ? (gridSize / 2f) : 0f);
		gridPlaneDataIndex = -1;
		childPieceCount = 0;
	}

	public void OnReturnToPool(BuilderPool pool)
	{
		SnapOverlap nextOverlap = firstOverlap;
		while (nextOverlap != null)
		{
			SnapOverlap snapOverlap = nextOverlap;
			nextOverlap = nextOverlap.nextOverlap;
			if (snapOverlap.otherPlane != null)
			{
				snapOverlap.otherPlane.RemoveSnapsWithPiece(piece, pool);
			}
			SetConnected(snapOverlap.bounds, connect: false);
			pool.DestroySnapOverlap(snapOverlap);
		}
		firstOverlap = null;
		int num = width * length;
		for (int i = 0; i < num; i++)
		{
			connected[i] = false;
		}
		childPieceCount = 0;
	}

	public Vector3 GetGridPosition(int x, int z, float gridSize)
	{
		float num = ((width % 2 == 0) ? (gridSize / 2f) : 0f);
		float num2 = ((length % 2 == 0) ? (gridSize / 2f) : 0f);
		return center.position + center.rotation * new Vector3((float)x * gridSize - num, (male ? 0.002f : (-0.002f)) * gridSize, (float)z * gridSize - num2);
	}

	public int GetChildCount()
	{
		return childPieceCount;
	}

	public void ChangeChildPieceCount(int delta)
	{
		childPieceCount += delta;
		if (!(piece.parentPiece == null) && piece.parentAttachIndex >= 0 && piece.parentAttachIndex < piece.parentPiece.gridPlanes.Count)
		{
			piece.parentPiece.gridPlanes[piece.parentAttachIndex].ChangeChildPieceCount(delta);
		}
	}

	public void AddSnapOverlap(SnapOverlap newOverlap)
	{
		if (firstOverlap == null)
		{
			firstOverlap = newOverlap;
		}
		else
		{
			newOverlap.nextOverlap = firstOverlap;
			firstOverlap = newOverlap;
		}
		SetConnected(newOverlap.bounds, connect: true);
	}

	public void RemoveSnapsWithDifferentRoot(BuilderPiece root, BuilderPool pool)
	{
		if (firstOverlap == null || pool == null)
		{
			return;
		}
		SnapOverlap snapOverlap = null;
		SnapOverlap nextOverlap = firstOverlap;
		while (nextOverlap != null)
		{
			if (nextOverlap.otherPlane == null || nextOverlap.otherPlane.piece == null)
			{
				SnapOverlap snapOverlap2 = nextOverlap;
				if (snapOverlap == null)
				{
					firstOverlap = nextOverlap.nextOverlap;
					nextOverlap = firstOverlap;
				}
				else
				{
					snapOverlap.nextOverlap = nextOverlap.nextOverlap;
					nextOverlap = snapOverlap.nextOverlap;
				}
				SetConnected(snapOverlap2.bounds, connect: false);
				pool.DestroySnapOverlap(snapOverlap2);
			}
			else if (root == null || nextOverlap.otherPlane.piece.GetRootPiece() != root)
			{
				SnapOverlap snapOverlap3 = nextOverlap;
				if (snapOverlap == null)
				{
					firstOverlap = nextOverlap.nextOverlap;
					nextOverlap = firstOverlap;
				}
				else
				{
					snapOverlap.nextOverlap = nextOverlap.nextOverlap;
					nextOverlap = snapOverlap.nextOverlap;
				}
				SetConnected(snapOverlap3.bounds, connect: false);
				snapOverlap3.otherPlane.RemoveSnapsWithPiece(piece, pool);
				pool.DestroySnapOverlap(snapOverlap3);
			}
			else
			{
				snapOverlap = nextOverlap;
				nextOverlap = nextOverlap.nextOverlap;
			}
		}
	}

	public void RemoveSnapsWithPiece(BuilderPiece piece, BuilderPool pool)
	{
		if (firstOverlap == null || piece == null || pool == null)
		{
			return;
		}
		SnapOverlap snapOverlap = null;
		SnapOverlap nextOverlap = firstOverlap;
		while (nextOverlap != null)
		{
			if (nextOverlap.otherPlane == null || nextOverlap.otherPlane.piece == null)
			{
				SnapOverlap snapOverlap2 = nextOverlap;
				if (snapOverlap == null)
				{
					firstOverlap = nextOverlap.nextOverlap;
					nextOverlap = firstOverlap;
				}
				else
				{
					snapOverlap.nextOverlap = nextOverlap.nextOverlap;
					nextOverlap = snapOverlap.nextOverlap;
				}
				SetConnected(snapOverlap2.bounds, connect: false);
				pool.DestroySnapOverlap(snapOverlap2);
			}
			else if (nextOverlap.otherPlane.piece == piece)
			{
				SnapOverlap snapOverlap3 = nextOverlap;
				if (snapOverlap == null)
				{
					firstOverlap = nextOverlap.nextOverlap;
					nextOverlap = firstOverlap;
				}
				else
				{
					snapOverlap.nextOverlap = nextOverlap.nextOverlap;
					nextOverlap = snapOverlap.nextOverlap;
				}
				SetConnected(snapOverlap3.bounds, connect: false);
				pool.DestroySnapOverlap(snapOverlap3);
			}
			else
			{
				snapOverlap = nextOverlap;
				nextOverlap = nextOverlap.nextOverlap;
			}
		}
	}

	private void SetConnected(SnapBounds bounds, bool connect)
	{
		int num = width / 2 - ((width % 2 == 0) ? 1 : 0);
		int num2 = length / 2 - ((length % 2 == 0) ? 1 : 0);
		int num3 = connected.Length;
		for (int i = bounds.min.x; i <= bounds.max.x; i++)
		{
			for (int j = bounds.min.y; j <= bounds.max.y; j++)
			{
				int num4 = (num + i) * length + (j + num2);
				if (num4 >= num3 || num4 < 0)
				{
					if (piece != null)
					{
						_ = piece.pieceId;
					}
					return;
				}
				connected[num4] = connect;
			}
		}
	}

	public bool IsConnected(SnapBounds bounds)
	{
		int num = width / 2 - ((width % 2 == 0) ? 1 : 0);
		int num2 = length / 2 - ((length % 2 == 0) ? 1 : 0);
		int num3 = connected.Length;
		for (int i = bounds.min.x; i <= bounds.max.x; i++)
		{
			for (int j = bounds.min.y; j <= bounds.max.y; j++)
			{
				int num4 = (num + i) * length + (j + num2);
				if (num4 < 0 || num4 >= num3)
				{
					if (piece != null)
					{
						_ = piece.pieceId;
					}
					return false;
				}
				if (connected[num4])
				{
					return true;
				}
			}
		}
		return false;
	}

	public void CalcGridOverlap(BuilderAttachGridPlane otherGridPlane, Vector3 otherPieceLocalPos, Quaternion otherPieceLocalRot, float gridSize, out Vector2Int min, out Vector2Int max)
	{
		int num = otherGridPlane.width;
		int num2 = otherGridPlane.length;
		Quaternion quaternion = otherPieceLocalRot * otherGridPlane.pieceToGridRotation;
		_ = base.transform.lossyScale;
		otherPieceLocalPos.Scale(base.transform.lossyScale);
		Vector3 vector = otherPieceLocalPos + otherPieceLocalRot * otherGridPlane.pieceToGridPosition;
		if (Mathf.Abs(Vector3.Dot(quaternion * Vector3.forward, Vector3.forward)) < 0.707f)
		{
			num = otherGridPlane.length;
			num2 = otherGridPlane.width;
		}
		float num3 = ((num % 2 == 0) ? (gridSize / 2f) : 0f);
		float num4 = ((num2 % 2 == 0) ? (gridSize / 2f) : 0f);
		float num5 = ((width % 2 == 0) ? (gridSize / 2f) : 0f);
		float num6 = ((length % 2 == 0) ? (gridSize / 2f) : 0f);
		float num7 = num3 - num5;
		float num8 = num4 - num6;
		int num9 = Mathf.RoundToInt((vector.x - num7) / gridSize);
		int num10 = Mathf.RoundToInt((vector.z - num8) / gridSize);
		int num11 = num9 + Mathf.FloorToInt((float)num / 2f);
		int num12 = num10 + Mathf.FloorToInt((float)num2 / 2f);
		int a = num11 - (num - 1);
		int a2 = num12 - (num2 - 1);
		int num13 = Mathf.FloorToInt((float)width / 2f);
		int num14 = Mathf.FloorToInt((float)length / 2f);
		int b = num13 - (width - 1);
		int b2 = num14 - (length - 1);
		min = new Vector2Int(Mathf.Max(a, b), Mathf.Max(a2, b2));
		max = new Vector2Int(Mathf.Min(num11, num13), Mathf.Min(num12, num14));
	}

	public bool IsAttachedToMovingGrid()
	{
		if (piece.state != BuilderPiece.State.AttachedAndPlaced)
		{
			return false;
		}
		if (piece.isBuiltIntoTable)
		{
			return false;
		}
		if (isMoving)
		{
			return true;
		}
		if (piece.parentPiece == null)
		{
			return false;
		}
		if (piece.parentAttachIndex < 0 || piece.parentAttachIndex >= piece.parentPiece.gridPlanes.Count)
		{
			return false;
		}
		return piece.parentPiece.gridPlanes[piece.parentAttachIndex].IsAttachedToMovingGrid();
	}

	public BuilderAttachGridPlane GetMovingParentGrid()
	{
		if (piece.isBuiltIntoTable)
		{
			return null;
		}
		if (movesOnPlace && movingPart != null && !movingPart.IsAnchoredToTable())
		{
			return this;
		}
		if (piece.parentPiece == null)
		{
			return null;
		}
		if (piece.parentAttachIndex < 0 || piece.parentAttachIndex >= piece.parentPiece.gridPlanes.Count)
		{
			return null;
		}
		return piece.parentPiece.gridPlanes[piece.parentAttachIndex].GetMovingParentGrid();
	}
}
