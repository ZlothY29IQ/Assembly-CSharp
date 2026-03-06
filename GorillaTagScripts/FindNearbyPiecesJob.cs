using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace GorillaTagScripts;

[BurstCompile]
internal struct FindNearbyPiecesJob : IJobParallelForTransform
{
	[ReadOnly]
	public float distanceThreshSq;

	[ReadOnly]
	public Vector3 leftHandPos;

	[ReadOnly]
	public int leftPieceInHandIndex;

	[ReadOnly]
	public Vector3 rightHandPos;

	[ReadOnly]
	public int rightPieceInHandIndex;

	[ReadOnly]
	public int localPlayerPlotIndex;

	[ReadOnly]
	public int localPlayerActorNumber;

	[ReadOnly]
	public NativeArray<BuilderPieceData> pieceData;

	[ReadOnly]
	public NativeArray<BuilderGridPlaneData> gridPlaneData;

	[ReadOnly]
	public NativeArray<BuilderPrivatePlotData> privatePlotData;

	[ReadOnly]
	public NativeArray<BuilderPlayerData> playerData;

	public NativeList<BuilderGridPlaneData>.ParallelWriter leftHandGridPlanes;

	public NativeList<BuilderGridPlaneData>.ParallelWriter rightHandGridPlanes;

	public void Execute(int index, TransformAccess transform)
	{
		if (transform.isValid)
		{
			CheckGridPlane(index, leftPieceInHandIndex, transform, leftHandPos, isLeft: true, leftHandGridPlanes);
			CheckGridPlane(index, rightPieceInHandIndex, transform, rightHandPos, isLeft: false, rightHandGridPlanes);
		}
	}

	private void CheckGridPlane(int gridPlaneIndex, int handPieceIndex, TransformAccess transform, Vector3 handPos, bool isLeft, NativeList<BuilderGridPlaneData>.ParallelWriter checkGridPlanes)
	{
		if (handPieceIndex >= 0 && !((transform.position - handPos).sqrMagnitude > distanceThreshSq))
		{
			BuilderGridPlaneData value = gridPlaneData[gridPlaneIndex];
			int pieceIndex = value.pieceIndex;
			int rootPieceIndex = GetRootPieceIndex(pieceIndex);
			if (rootPieceIndex != handPieceIndex && CanPiecesPotentiallySnap(localPlayerActorNumber, handPieceIndex, pieceIndex, rootPieceIndex, pieceData[pieceIndex].requestedParentPieceIndex, isLeft))
			{
				transform.GetPositionAndRotation(out value.position, out value.rotation);
				checkGridPlanes.AddNoResize(value);
			}
		}
	}

	public bool CanPiecesPotentiallySnap(int localActorNumber, int pieceInHandIndex, int attachToPieceIndex, int attachToPieceRootIndex, int requestedParentPieceIndex, bool isLeft)
	{
		if (!CanPlayerAttachToRootPiece(localActorNumber, attachToPieceRootIndex, isLeft))
		{
			return false;
		}
		if (requestedParentPieceIndex != -1 && pieceInHandIndex == GetRootPieceIndex(requestedParentPieceIndex))
		{
			return false;
		}
		return true;
	}

	public bool CanPlayerAttachToRootPiece(int playerActorNumber, int attachToPieceRootIndex, bool isLeft)
	{
		BuilderPieceData builderPieceData = pieceData[attachToPieceRootIndex];
		if (builderPieceData.state != BuilderPiece.State.AttachedAndPlaced && builderPieceData.privatePlotIndex < 0 && builderPieceData.state != BuilderPiece.State.AttachedToArm)
		{
			return true;
		}
		int attachedBuiltInPiece = GetAttachedBuiltInPiece(attachToPieceRootIndex);
		if (attachedBuiltInPiece == -1)
		{
			return true;
		}
		BuilderPieceData builderPieceData2 = pieceData[attachedBuiltInPiece];
		if (builderPieceData2.privatePlotIndex < 0 && !builderPieceData2.isArmPiece)
		{
			return true;
		}
		if (builderPieceData2.isArmPiece)
		{
			if (builderPieceData2.heldByActorNumber == playerActorNumber)
			{
				int playerIndex = GetPlayerIndex(playerActorNumber);
				if (playerIndex < 0)
				{
					return false;
				}
				return playerData[playerIndex].scale >= 1f;
			}
			return false;
		}
		if (builderPieceData2.privatePlotIndex >= 0)
		{
			if (CanPlayerAttachToPlot(builderPieceData2.privatePlotIndex, playerActorNumber))
			{
				if (!isLeft)
				{
					return privatePlotData[builderPieceData2.privatePlotIndex].isUnderCapacityRight;
				}
				return privatePlotData[builderPieceData2.privatePlotIndex].isUnderCapacityLeft;
			}
			return false;
		}
		return true;
	}

	public bool CanPlayerAttachToPlot(int privatePlotIndex, int actorNumber)
	{
		BuilderPrivatePlotData builderPrivatePlotData = privatePlotData[privatePlotIndex];
		if (builderPrivatePlotData.plotState != BuilderPiecePrivatePlot.PlotState.Occupied || builderPrivatePlotData.ownerActorNumber != actorNumber)
		{
			if (builderPrivatePlotData.plotState == BuilderPiecePrivatePlot.PlotState.Vacant)
			{
				return localPlayerPlotIndex < 0;
			}
			return false;
		}
		return true;
	}

	private int GetPlayerIndex(int playerActorNumber)
	{
		for (int i = 0; i < playerData.Length; i++)
		{
			if (playerData[i].playerActorNumber == playerActorNumber)
			{
				return i;
			}
		}
		return -1;
	}

	public int GetAttachedBuiltInPiece(int pieceIndex)
	{
		BuilderPieceData builderPieceData = pieceData[pieceIndex];
		if (builderPieceData.isBuiltIntoTable)
		{
			return pieceIndex;
		}
		if (builderPieceData.state != BuilderPiece.State.AttachedAndPlaced)
		{
			return -1;
		}
		int num = GetRootPieceIndex(pieceIndex);
		int parentPieceIndex = pieceData[num].parentPieceIndex;
		if (parentPieceIndex != -1)
		{
			num = parentPieceIndex;
		}
		if (pieceData[num].isBuiltIntoTable)
		{
			return num;
		}
		return -1;
	}

	private int GetRootPieceIndex(int pieceIndex)
	{
		int num = pieceIndex;
		while (num != -1 && pieceData[num].parentPieceIndex != -1 && !pieceData[pieceData[num].parentPieceIndex].isBuiltIntoTable)
		{
			num = pieceData[num].parentPieceIndex;
		}
		return num;
	}
}
