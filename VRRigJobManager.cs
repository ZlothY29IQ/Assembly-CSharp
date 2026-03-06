using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

[DefaultExecutionOrder(0)]
public class VRRigJobManager : MonoBehaviour
{
	private struct VRRigTransformInput
	{
		public Vector3 rigPosition;

		public Quaternion rigRotaton;
	}

	[BurstCompile]
	private struct VRRigTransformJob : IJobParallelForTransform
	{
		[ReadOnly]
		public NativeArray<VRRigTransformInput> input;

		public void Execute(int i, TransformAccess tA)
		{
			if (i < input.Length)
			{
				tA.position = input[i].rigPosition;
				tA.rotation = input[i].rigRotaton;
			}
		}
	}

	[OnEnterPlay_SetNull]
	private static VRRigJobManager _instance;

	private const int MaxSize = 19;

	private const int questJobThreads = 2;

	private List<VRRig> rigList = new List<VRRig>(19);

	private NativeArray<VRRigTransformInput> cachedInput;

	private TransformAccessArray tAA;

	private int actualListSz;

	private JobHandle jobHandle;

	private VRRigTransformJob job;

	public static VRRigJobManager Instance => _instance;

	private void Awake()
	{
		_instance = this;
		cachedInput = new NativeArray<VRRigTransformInput>(19, Allocator.Persistent);
		tAA = new TransformAccessArray(19, 2);
		job = default(VRRigTransformJob);
	}

	private void OnDestroy()
	{
		jobHandle.Complete();
		cachedInput.Dispose();
		tAA.Dispose();
	}

	public void RegisterVRRig(VRRig rig)
	{
		rigList.Add(rig);
		tAA.Add(rig.transform);
		actualListSz++;
	}

	public void DeregisterVRRig(VRRig rig)
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		rigList.Remove(rig);
		for (int num = actualListSz - 1; num >= 0; num--)
		{
			if (tAA[num] == rig.transform)
			{
				tAA.RemoveAtSwapBack(num);
				break;
			}
		}
		actualListSz--;
	}

	private void CopyInput()
	{
		for (int i = 0; i < actualListSz; i++)
		{
			cachedInput[i] = new VRRigTransformInput
			{
				rigPosition = rigList[i].jobPos,
				rigRotaton = rigList[i].jobRotation
			};
			tAA[i] = rigList[i].transform;
		}
	}

	public void Update()
	{
		jobHandle.Complete();
		for (int i = 0; i < rigList.Count; i++)
		{
			rigList[i].RemoteRigUpdate();
		}
		CopyInput();
		job.input = cachedInput;
		jobHandle = job.Schedule(tAA);
	}
}
