using UnityEngine;

public class BuilderActions
{
	public static BuilderAction CreateAttachToPlayer(int cmdId, int pieceId, Vector3 localPosition, Quaternion localRotation, int actorNumber, bool leftHand)
	{
		BuilderAction result = default(BuilderAction);
		result.type = BuilderActionType.AttachToPlayer;
		result.localCommandId = cmdId;
		result.pieceId = pieceId;
		result.playerActorNumber = actorNumber;
		result.localPosition = localPosition;
		result.localRotation = localRotation;
		result.isLeftHand = leftHand;
		return result;
	}

	public static BuilderAction CreateAttachToPlayerRollback(int cmdId, BuilderPiece piece)
	{
		return CreateAttachToPlayer(cmdId, piece.pieceId, piece.transform.localPosition, piece.transform.localRotation, piece.heldByPlayerActorNumber, piece.heldInLeftHand);
	}

	public static BuilderAction CreateDetachFromPlayer(int cmdId, int pieceId, int actorNumber)
	{
		BuilderAction result = default(BuilderAction);
		result.type = BuilderActionType.DetachFromPlayer;
		result.localCommandId = cmdId;
		result.pieceId = pieceId;
		result.playerActorNumber = actorNumber;
		return result;
	}

	public static BuilderAction CreateAttachToPiece(int cmdId, int pieceId, int parentPieceId, int attachIndex, int parentAttachIndex, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, int actorNumber, int timeStamp)
	{
		BuilderAction result = default(BuilderAction);
		result.type = BuilderActionType.AttachToPiece;
		result.localCommandId = cmdId;
		result.pieceId = pieceId;
		result.parentPieceId = parentPieceId;
		result.attachIndex = attachIndex;
		result.parentAttachIndex = parentAttachIndex;
		result.bumpOffsetx = bumpOffsetX;
		result.bumpOffsetz = bumpOffsetZ;
		result.twist = twist;
		result.playerActorNumber = actorNumber;
		result.timeStamp = timeStamp;
		return result;
	}

	public static BuilderAction CreateAttachToPieceRollback(int cmdId, BuilderPiece piece, int actorNumber)
	{
		byte pieceTwist = piece.GetPieceTwist();
		piece.GetPieceBumpOffset(pieceTwist, out var xOffset, out var zOffset);
		return CreateAttachToPiece(cmdId, piece.pieceId, piece.parentPiece.pieceId, piece.attachIndex, piece.parentAttachIndex, xOffset, zOffset, pieceTwist, actorNumber, piece.activatedTimeStamp);
	}

	public static BuilderAction CreateDetachFromPiece(int cmdId, int pieceId, int actorNumber)
	{
		BuilderAction result = default(BuilderAction);
		result.type = BuilderActionType.DetachFromPiece;
		result.localCommandId = cmdId;
		result.pieceId = pieceId;
		result.playerActorNumber = actorNumber;
		return result;
	}

	public static BuilderAction CreateMakeRoot(int cmdId, int pieceId)
	{
		BuilderAction result = default(BuilderAction);
		result.type = BuilderActionType.MakePieceRoot;
		result.localCommandId = cmdId;
		result.pieceId = pieceId;
		return result;
	}

	public static BuilderAction CreateDropPiece(int cmdId, int pieceId, Vector3 localPosition, Quaternion localRotation, Vector3 velocity, Vector3 angVelocity, int actorNumber)
	{
		BuilderAction result = default(BuilderAction);
		result.type = BuilderActionType.DropPiece;
		result.localCommandId = cmdId;
		result.pieceId = pieceId;
		result.localPosition = localPosition;
		result.localRotation = localRotation;
		result.velocity = velocity;
		result.angVelocity = angVelocity;
		result.playerActorNumber = actorNumber;
		return result;
	}

	public static BuilderAction CreateDropPieceRollback(int cmdId, BuilderPiece rootPiece, int actorNumber)
	{
		Vector3 position = rootPiece.transform.position;
		Quaternion rotation = rootPiece.transform.rotation;
		Vector3 velocity = Vector3.zero;
		Vector3 angVelocity = Vector3.zero;
		Rigidbody component = rootPiece.GetComponent<Rigidbody>();
		if (component != null)
		{
			position = component.position;
			rotation = component.rotation;
			velocity = component.linearVelocity;
			angVelocity = component.angularVelocity;
		}
		return CreateDropPiece(cmdId, rootPiece.pieceId, position, rotation, velocity, angVelocity, actorNumber);
	}

	public static BuilderAction CreateAttachToShelfRollback(int cmdId, BuilderPiece piece, int shelfID, bool isConveyor, int timestamp = 0, float splineTime = 0f)
	{
		BuilderAction result = default(BuilderAction);
		result.type = BuilderActionType.AttachToShelf;
		result.localCommandId = cmdId;
		result.pieceId = piece.pieceId;
		result.attachIndex = shelfID;
		result.parentAttachIndex = timestamp;
		result.isLeftHand = isConveyor;
		result.velocity = new Vector3(splineTime, 0f, 0f);
		result.localPosition = piece.transform.localPosition;
		result.localRotation = piece.transform.localRotation;
		return result;
	}
}
