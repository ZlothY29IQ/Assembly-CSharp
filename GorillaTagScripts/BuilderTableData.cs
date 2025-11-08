using System;
using System.Collections.Generic;

namespace GorillaTagScripts;

[Serializable]
public class BuilderTableData
{
	public const int BUILDER_TABLE_DATA_VERSION = 4;

	public int version;

	public int numEdits;

	public int numPieces;

	public List<int> pieceType;

	public List<int> pieceId;

	public List<int> parentId;

	public List<int> attachIndex;

	public List<int> parentAttachIndex;

	public List<int> placement;

	public List<int> materialType;

	public List<int> overlapingPieces;

	public List<int> overlappedPieces;

	public List<long> overlapInfo;

	public List<int> timeOffset;

	public BuilderTableData()
	{
		version = 4;
		numEdits = 0;
		numPieces = 0;
		pieceType = new List<int>(1024);
		pieceId = new List<int>(1024);
		parentId = new List<int>(1024);
		attachIndex = new List<int>(1024);
		parentAttachIndex = new List<int>(1024);
		placement = new List<int>(1024);
		materialType = new List<int>(1024);
		overlapingPieces = new List<int>(1024);
		overlappedPieces = new List<int>(1024);
		overlapInfo = new List<long>(1024);
		timeOffset = new List<int>(1024);
	}

	public void Clear()
	{
		numPieces = 0;
		pieceType.Clear();
		pieceId.Clear();
		parentId.Clear();
		attachIndex.Clear();
		parentAttachIndex.Clear();
		placement.Clear();
		materialType.Clear();
		overlapingPieces.Clear();
		overlappedPieces.Clear();
		overlapInfo.Clear();
		timeOffset.Clear();
	}
}
