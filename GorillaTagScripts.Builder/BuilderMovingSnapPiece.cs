using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderMovingSnapPiece : MonoBehaviour, IBuilderPieceComponent, IBuilderPieceFunctional
{
	public List<BuilderMovingPart> MovingParts;

	public BuilderPiece myPiece;

	public const int MAX_MOVING_CHILDREN = 5;

	[SerializeField]
	private GameObject startMovingFX;

	[SerializeField]
	private GameObject stopMovingFX;

	private bool activated;

	private bool moving;

	private const byte MOVING_STATE = 0;

	private byte currentPauseNode;

	private void Awake()
	{
		myPiece = GetComponent<BuilderPiece>();
		if (myPiece == null)
		{
			Debug.LogWarning("Missing BuilderPiece component " + base.gameObject.name);
		}
		foreach (BuilderMovingPart movingPart in MovingParts)
		{
			movingPart.myPiece = myPiece;
		}
	}

	public int GetTimeOffset()
	{
		if (myPiece.state != BuilderPiece.State.AttachedAndPlaced)
		{
			return 0;
		}
		foreach (BuilderMovingPart movingPart in MovingParts)
		{
			if (!movingPart.IsAnchoredToTable())
			{
				return movingPart.GetTimeOffsetMS();
			}
		}
		return 0;
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
	}

	public void OnPieceDestroy()
	{
		foreach (BuilderMovingPart movingPart in MovingParts)
		{
			movingPart.OnPieceDestroy();
		}
	}

	public void OnPiecePlacementDeserialized()
	{
		foreach (BuilderMovingPart movingPart in MovingParts)
		{
			movingPart.InitMovingGrid();
			movingPart.SetMoving(isMoving: false);
			if (myPiece.functionalPieceState == 0 && !movingPart.IsAnchoredToTable())
			{
				currentPauseNode = movingPart.GetStartNode();
			}
		}
		moving = false;
		if (!activated)
		{
			BuilderTable table = myPiece.GetTable();
			table.RegisterFunctionalPiece(this);
			table.RegisterFunctionalPieceFixedUpdate(this);
			activated = true;
		}
		OnStateChanged(myPiece.functionalPieceState, NetworkSystem.Instance.MasterClient, myPiece.activatedTimeStamp);
	}

	public void OnPieceActivate()
	{
		BuilderTable table = myPiece.GetTable();
		if (table.GetTableState() != BuilderTable.TableState.Ready && table.GetTableState() != BuilderTable.TableState.ExecuteQueuedCommands)
		{
			return;
		}
		if (!activated)
		{
			table.RegisterFunctionalPiece(this);
			table.RegisterFunctionalPieceFixedUpdate(this);
			activated = true;
		}
		foreach (BuilderMovingPart movingPart in MovingParts)
		{
			movingPart.InitMovingGrid();
			if (movingPart.IsAnchoredToTable())
			{
				continue;
			}
			int num = 0;
			BuilderAttachGridPlane[] myGridPlanes = movingPart.myGridPlanes;
			foreach (BuilderAttachGridPlane builderAttachGridPlane in myGridPlanes)
			{
				num += builderAttachGridPlane.GetChildCount();
			}
			if (num <= 5)
			{
				currentPauseNode = movingPart.GetStartNode();
				if (myPiece.functionalPieceState > 0 && myPiece.functionalPieceState < BuilderMovingPart.NUM_PAUSE_NODES * 2 + 1)
				{
					currentPauseNode = (byte)(myPiece.functionalPieceState - 1);
				}
				myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.MasterClient, myPiece.activatedTimeStamp);
			}
			else
			{
				currentPauseNode = movingPart.GetStartNode();
				if (myPiece.functionalPieceState > 0 && myPiece.functionalPieceState < BuilderMovingPart.NUM_PAUSE_NODES * 2 + 1)
				{
					currentPauseNode = (byte)(myPiece.functionalPieceState - 1);
				}
				myPiece.SetFunctionalPieceState((byte)(currentPauseNode + 1), NetworkSystem.Instance.MasterClient, myPiece.activatedTimeStamp);
			}
		}
	}

	public void OnPieceDeactivate()
	{
		BuilderTable table = myPiece.GetTable();
		table.UnregisterFunctionalPiece(this);
		table.UnregisterFunctionalPieceFixedUpdate(this);
		myPiece.functionalPieceState = 0;
		moving = false;
		foreach (BuilderMovingPart movingPart in MovingParts)
		{
			movingPart.SetMoving(isMoving: false);
		}
		activated = false;
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (!IsStateValid(newState) || myPiece.state != BuilderPiece.State.AttachedAndPlaced || !activated)
		{
			return;
		}
		if (newState == 0 && !moving)
		{
			moving = true;
			if (startMovingFX != null)
			{
				ObjectPools.instance.Instantiate(startMovingFX, base.transform.position);
			}
			{
				foreach (BuilderMovingPart movingPart in MovingParts)
				{
					if (!movingPart.IsAnchoredToTable())
					{
						movingPart.ActivateAtNode(currentPauseNode, timeStamp);
						currentPauseNode = movingPart.GetStartNode();
					}
				}
				return;
			}
		}
		if (moving && stopMovingFX != null)
		{
			ObjectPools.instance.Instantiate(stopMovingFX, base.transform.position);
		}
		moving = false;
		currentPauseNode = (byte)(newState - 1);
		foreach (BuilderMovingPart movingPart2 in MovingParts)
		{
			if (!movingPart2.IsAnchoredToTable())
			{
				movingPart2.PauseMovement(currentPauseNode);
			}
		}
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
	}

	public bool IsStateValid(byte state)
	{
		return state <= BuilderMovingPart.NUM_PAUSE_NODES * 2 + 1;
	}

	public void FunctionalPieceUpdate()
	{
		UpdateMaster();
	}

	public void FunctionalPieceFixedUpdate()
	{
		if (!moving)
		{
			return;
		}
		foreach (BuilderMovingPart movingPart in MovingParts)
		{
			if (!movingPart.IsAnchoredToTable())
			{
				movingPart.UpdateMovingGrid();
			}
		}
	}

	private void UpdateMaster()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		BuilderTable table = myPiece.GetTable();
		foreach (BuilderMovingPart movingPart in MovingParts)
		{
			if (!movingPart.IsAnchoredToTable())
			{
				int num = 0;
				BuilderAttachGridPlane[] myGridPlanes = movingPart.myGridPlanes;
				foreach (BuilderAttachGridPlane builderAttachGridPlane in myGridPlanes)
				{
					num += builderAttachGridPlane.GetChildCount();
				}
				bool num2 = num <= 5;
				if (num2 && !moving)
				{
					table.builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 0, PhotonNetwork.MasterClient, NetworkSystem.Instance.ServerTimestamp);
				}
				if (!num2 && moving)
				{
					byte state = (byte)(movingPart.GetNearestNode() + 1);
					table.builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, state, PhotonNetwork.MasterClient, NetworkSystem.Instance.ServerTimestamp);
				}
			}
		}
	}
}
