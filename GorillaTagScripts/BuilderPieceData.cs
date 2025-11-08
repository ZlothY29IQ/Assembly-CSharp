namespace GorillaTagScripts;

public struct BuilderPieceData
{
	public int pieceId;

	public int pieceIndex;

	public int parentPieceIndex;

	public int requestedParentPieceIndex;

	public int heldByActorNumber;

	public int preventSnapUntilMoved;

	public bool isBuiltIntoTable;

	public BuilderPiece.State state;

	public int privatePlotIndex;

	public bool isArmPiece;

	public BuilderPieceData(BuilderPiece piece)
	{
		pieceId = piece.pieceId;
		pieceIndex = piece.pieceDataIndex;
		BuilderPiece parentPiece = piece.parentPiece;
		parentPieceIndex = ((parentPiece == null) ? (-1) : parentPiece.pieceDataIndex);
		BuilderPiece requestedParentPiece = piece.requestedParentPiece;
		requestedParentPieceIndex = ((requestedParentPiece == null) ? (-1) : requestedParentPiece.pieceDataIndex);
		preventSnapUntilMoved = piece.preventSnapUntilMoved;
		isBuiltIntoTable = piece.isBuiltIntoTable;
		state = piece.state;
		privatePlotIndex = piece.privatePlotIndex;
		isArmPiece = piece.isArmShelf;
		heldByActorNumber = piece.heldByPlayerActorNumber;
	}
}
