using Unity.Collections;

namespace GorillaTagScripts;

public class BuilderTableJobs
{
	public static void BuildTestPieceListForJob(BuilderPiece testPiece, NativeList<BuilderPieceData> testPieceList, NativeList<BuilderGridPlaneData> testGridPlaneList)
	{
		if (!(testPiece == null))
		{
			int length = testPieceList.Length;
			testPieceList.Add(new BuilderPieceData(testPiece));
			for (int i = 0; i < testPiece.gridPlanes.Count; i++)
			{
				testGridPlaneList.Add(new BuilderGridPlaneData(testPiece.gridPlanes[i], length));
			}
			BuilderPiece builderPiece = testPiece.firstChildPiece;
			while (builderPiece != null)
			{
				BuildTestPieceListForJob(builderPiece, testPieceList, testGridPlaneList);
				builderPiece = builderPiece.nextSiblingPiece;
			}
		}
	}

	public static void BuildTestPieceListForJob(BuilderPiece testPiece, NativeList<BuilderGridPlaneData> testGridPlaneList)
	{
		if (!(testPiece == null))
		{
			for (int i = 0; i < testPiece.gridPlanes.Count; i++)
			{
				testGridPlaneList.Add(new BuilderGridPlaneData(testPiece.gridPlanes[i], -1));
			}
			BuilderPiece builderPiece = testPiece.firstChildPiece;
			while (builderPiece != null)
			{
				BuildTestPieceListForJob(builderPiece, testGridPlaneList);
				builderPiece = builderPiece.nextSiblingPiece;
			}
		}
	}
}
