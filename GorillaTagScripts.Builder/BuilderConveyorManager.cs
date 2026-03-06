using Photon.Pun;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Splines;

namespace GorillaTagScripts.Builder;

public class BuilderConveyorManager : MonoBehaviour
{
	[BurstCompile]
	public struct EvaluateSplineJob : IJobParallelForTransform
	{
		public NativeSpline conveyorSpline0;

		public NativeSpline conveyorSpline1;

		public NativeSpline conveyorSpline2;

		public NativeSpline conveyorSpline3;

		[ReadOnly]
		public NativeArray<Quaternion> conveyorRotations;

		[ReadOnly]
		public NativeList<int> conveyorIndices;

		[ReadOnly]
		public NativeList<float> splineTimes;

		[ReadOnly]
		public NativeList<Vector3> shelfOffsets;

		public NativeSpline GetSplineAt(int index)
		{
			return index switch
			{
				0 => conveyorSpline0, 
				1 => conveyorSpline1, 
				2 => conveyorSpline2, 
				3 => conveyorSpline3, 
				_ => conveyorSpline0, 
			};
		}

		public void SetSplineAt(int index, NativeSpline s)
		{
			switch (index)
			{
			case 0:
				conveyorSpline0 = s;
				break;
			case 1:
				conveyorSpline1 = s;
				break;
			case 2:
				conveyorSpline2 = s;
				break;
			case 3:
				conveyorSpline3 = s;
				break;
			}
		}

		public void Execute(int index, TransformAccess transform)
		{
			float splineT = splineTimes[index];
			Vector3 vector = shelfOffsets[index];
			int index2 = conveyorIndices[index];
			NativeSpline splineAt = GetSplineAt(index2);
			Quaternion quaternion2 = conveyorRotations[index2];
			float curveT;
			Vector3 position = (Vector3)CurveUtility.EvaluatePosition(splineAt.GetCurve(splineAt.SplineToCurveT(splineT, out curveT)), curveT) + quaternion2 * vector;
			transform.position = position;
		}
	}

	private NativeArray<NativeSpline> conveyorSplines;

	private NativeArray<Quaternion> conveyorRotations;

	private NativeList<int> conveyorIndices;

	private NativeList<float> jobSplineTimes;

	private NativeList<Vector3> jobShelfOffsets;

	private TransformAccessArray pieceTransforms;

	private BuilderTable table;

	private bool isSetup;

	private int maxItemCount;

	private int shelfSlice;

	public static BuilderConveyorManager instance { get; private set; }

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Object.Destroy(this);
		}
		if (instance == null)
		{
			instance = this;
		}
	}

	public void UpdateManager()
	{
		foreach (BuilderConveyor conveyor in table.conveyors)
		{
			conveyor.UpdateConveyor();
		}
		bool flag = false;
		bool flag2 = pieceTransforms.length >= pieceTransforms.capacity - 5;
		for (int num = jobSplineTimes.Length - 1; num >= 0; num--)
		{
			BuilderConveyor builderConveyor = table.conveyors[conveyorIndices[num]];
			float num2 = Time.deltaTime * builderConveyor.GetFrameMovement();
			float num3 = jobSplineTimes[num] + num2;
			jobSplineTimes[num] = Mathf.Clamp(num3, 0f, 1f);
			if (PhotonNetwork.IsMasterClient && (!flag || flag2) && (double)num3 > 0.999)
			{
				builderConveyor.RemovePieceFromConveyor(pieceTransforms[num]);
				RemovePieceFromJobAtIndex(num);
				flag = true;
			}
		}
		for (int i = shelfSlice; i < table.conveyors.Count; i += BuilderTable.SHELF_SLICE_BUCKETS)
		{
			table.conveyors[i].UpdateShelfSliced();
		}
		shelfSlice = (shelfSlice + 1) % BuilderTable.SHELF_SLICE_BUCKETS;
	}

	public void Setup(BuilderTable mytable)
	{
		if (!isSetup)
		{
			table = mytable;
			conveyorSplines = new NativeArray<NativeSpline>(table.conveyors.Count, Allocator.Persistent);
			conveyorRotations = new NativeArray<Quaternion>(table.conveyors.Count, Allocator.Persistent);
			int num = 0;
			for (int i = 0; i < table.conveyors.Count; i++)
			{
				conveyorSplines[i] = table.conveyors[i].nativeSpline;
				conveyorRotations[i] = table.conveyors[i].GetSpawnTransform().rotation;
				num += table.conveyors[i].GetMaxItemsOnConveyor();
			}
			maxItemCount = num;
			conveyorIndices = new NativeList<int>(maxItemCount, Allocator.Persistent);
			jobSplineTimes = new NativeList<float>(maxItemCount, Allocator.Persistent);
			jobShelfOffsets = new NativeList<Vector3>(maxItemCount, Allocator.Persistent);
			pieceTransforms = new TransformAccessArray(maxItemCount, 3);
			isSetup = true;
		}
	}

	public float GetSplineProgressForPiece(BuilderPiece piece)
	{
		for (int i = 0; i < pieceTransforms.length; i++)
		{
			if (pieceTransforms[i] == piece.transform)
			{
				return jobSplineTimes[i];
			}
		}
		return 1f;
	}

	public int GetPieceCreateTimestamp(BuilderPiece piece)
	{
		for (int i = 0; i < pieceTransforms.length; i++)
		{
			if (pieceTransforms[i] == piece.transform)
			{
				BuilderConveyor builderConveyor = table.conveyors[conveyorIndices[i]];
				int num = Mathf.RoundToInt(jobSplineTimes[i] / builderConveyor.GetFrameMovement() * 1000f);
				return PhotonNetwork.ServerTimestamp - num;
			}
		}
		return PhotonNetwork.ServerTimestamp - 5000;
	}

	public void OnClearTable()
	{
		if (!isSetup)
		{
			return;
		}
		foreach (BuilderConveyor conveyor in table.conveyors)
		{
			conveyor.OnClearTable();
		}
		for (int num = pieceTransforms.length - 1; num >= 0; num--)
		{
			pieceTransforms.RemoveAtSwapBack(num);
		}
		jobSplineTimes.Clear();
		jobShelfOffsets.Clear();
		conveyorIndices.Clear();
	}

	private void OnDestroy()
	{
		conveyorSplines.Dispose();
		conveyorRotations.Dispose();
		conveyorIndices.Dispose();
		jobSplineTimes.Dispose();
		jobShelfOffsets.Dispose();
		pieceTransforms.Dispose();
	}

	public JobHandle ConstructJobHandle()
	{
		EvaluateSplineJob jobData = new EvaluateSplineJob
		{
			conveyorRotations = conveyorRotations,
			conveyorIndices = conveyorIndices,
			shelfOffsets = jobShelfOffsets,
			splineTimes = jobSplineTimes
		};
		for (int i = 0; i < conveyorSplines.Length; i++)
		{
			jobData.SetSplineAt(i, conveyorSplines[i]);
		}
		return jobData.Schedule(pieceTransforms);
	}

	public void AddPieceToJob(BuilderPiece piece, float splineTime, int conveyorID)
	{
		if (pieceTransforms.length >= pieceTransforms.capacity)
		{
			Debug.LogError("Too many pieces on conveyor!");
		}
		pieceTransforms.Add(piece.transform);
		conveyorIndices.Add(in conveyorID);
		jobShelfOffsets.Add(in piece.desiredShelfOffset);
		jobSplineTimes.Add(in splineTime);
	}

	public void RemovePieceFromJobAtIndex(int index)
	{
		BuilderRenderer.RemoveAt(pieceTransforms, index);
		jobShelfOffsets.RemoveAt(index);
		jobSplineTimes.RemoveAt(index);
		conveyorIndices.RemoveAt(index);
	}

	public void RemovePieceFromJob(BuilderPiece piece)
	{
		for (int i = 0; i < pieceTransforms.length; i++)
		{
			if (pieceTransforms[i] == piece.transform)
			{
				BuilderRenderer.RemoveAt(pieceTransforms, i);
				jobShelfOffsets.RemoveAt(i);
				jobSplineTimes.RemoveAt(i);
				conveyorIndices.RemoveAt(i);
				break;
			}
		}
	}
}
