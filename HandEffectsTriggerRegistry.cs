using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaTag.Shared.Scripts.Utilities;
using TagEffects;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class HandEffectsTriggerRegistry : MonoBehaviour, ITickSystemTick, ITickSystemPost
{
	[BurstCompile]
	private struct HandEffectsJob : IJobParallelFor, IDisposable
	{
		[NativeDisableParallelForRestriction]
		public NativeArray<Vector3> positionInput;

		[NativeDisableParallelForRestriction]
		public NativeArray<bool> closeOutput;

		public int actualListSize;

		public void Execute(int i)
		{
			for (int j = i + 1; j < actualListSize; j++)
			{
				closeOutput[i * 50 + j] = (positionInput[i] - positionInput[j]).IsShorterThan(0.5f);
			}
		}

		public void Dispose()
		{
			positionInput.Dispose();
			closeOutput.Dispose();
		}
	}

	private const int MAX_TRIGGERS = 50;

	private const int BIT_ARRAY_SIZE = 2500;

	private const float COOLDOWN_TIME = 0.5f;

	private const float DEFAULT_RADIUS = 0.5f;

	private readonly List<IHandEffectsTrigger> triggers = new List<IHandEffectsTrigger>();

	private readonly float[] triggerTimes = new float[50];

	private readonly GTBitArray existingCollisionBits = new GTBitArray(2500);

	private readonly GTBitArray newCollisionBits = new GTBitArray(2500);

	private int actualListSz;

	private JobHandle jobHandle;

	private HandEffectsJob job;

	public bool TickRunning { get; set; }

	public bool PostTickRunning { get; set; }

	[field: OnEnterPlay_SetNull]
	public static HandEffectsTriggerRegistry Instance { get; private set; }

	[field: OnEnterPlay_Set(false)]
	public static bool HasInstance { get; private set; }

	public static void FindInstance()
	{
		Instance = UnityEngine.Object.FindAnyObjectByType<HandEffectsTriggerRegistry>();
		HasInstance = true;
	}

	private void Awake()
	{
		Instance = this;
		HasInstance = true;
		job = new HandEffectsJob
		{
			positionInput = new NativeArray<Vector3>(50, Allocator.Persistent),
			closeOutput = new NativeArray<bool>(2500, Allocator.Persistent),
			actualListSize = actualListSz
		};
	}

	private void OnEnable()
	{
		TickSystem<object>.AddCallbackTarget(this);
		TickSystem<object>.AddPostTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveCallbackTarget(this);
		TickSystem<object>.RemovePostTickCallback(this);
	}

	public void Register(IHandEffectsTrigger trigger)
	{
		if (triggers.Count < 50)
		{
			actualListSz++;
			triggers.Add(trigger);
		}
	}

	public void Unregister(IHandEffectsTrigger trigger)
	{
		int num = triggers.IndexOf(trigger);
		if (num >= 0)
		{
			actualListSz--;
			triggers.RemoveAt(num);
		}
	}

	private void OnDestroy()
	{
		if (!jobHandle.IsCompleted)
		{
			jobHandle.Complete();
		}
		job.Dispose();
	}

	public void Tick()
	{
		CopyInput();
		jobHandle = IJobParallelForExtensions.Schedule(job, actualListSz, 20);
	}

	public void PostTick()
	{
		jobHandle.Complete();
		CheckForHandEffectOnProcessedOutput();
	}

	public void CheckForHandEffectOnProcessedOutput()
	{
		newCollisionBits.Clear();
		for (int i = 0; i < triggers.Count; i++)
		{
			IHandEffectsTrigger handEffectsTrigger = triggers[i];
			int num = i * 50;
			for (int j = i + 1; j < triggers.Count; j++)
			{
				if (!job.closeOutput[i * 50 + j])
				{
					continue;
				}
				IHandEffectsTrigger handEffectsTrigger2 = triggers[j];
				if (handEffectsTrigger.InTriggerZone(handEffectsTrigger2) || handEffectsTrigger2.InTriggerZone(handEffectsTrigger))
				{
					int idx = num + j;
					newCollisionBits[idx] = true;
					if (!existingCollisionBits[idx] && Time.time - triggerTimes[i] > 0.5f && Time.time - triggerTimes[j] > 0.5f)
					{
						handEffectsTrigger.OnTriggerEntered(handEffectsTrigger2);
						handEffectsTrigger2.OnTriggerEntered(handEffectsTrigger);
						triggerTimes[i] = (triggerTimes[j] = Time.time);
					}
				}
			}
		}
		existingCollisionBits.CopyFrom(newCollisionBits);
	}

	private void CopyInput()
	{
		for (int i = 0; i < actualListSz; i++)
		{
			job.positionInput[i] = triggers[i].Transform.position;
		}
		if (job.actualListSize != actualListSz)
		{
			job.actualListSize = actualListSz;
		}
	}
}
