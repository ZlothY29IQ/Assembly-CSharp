using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using Photon.Pun;
using UnityEngine;

[NetworkBehaviourWeaved(2)]
public class RandomTimedSeedManager : NetworkComponent, ITickSystemTick
{
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	[NetworkStructWeaved(2)]
	private struct RandomTimedSeedManagerData(int seed, float currentSyncTime) : INetworkStruct
	{
		[FieldOffset(0)]
		public int seed = seed;

		[FieldOffset(4)]
		public float currentSyncTime = currentSyncTime;
	}

	private List<Action> callbacksOnSeedChanged = new List<Action>();

	private float idealSyncTime;

	private int cachedSeed;

	private const int SeedMin = -1000000;

	private const int SeedMax = -1000000;

	private const float MaxSyncTime = 1E+09f;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 2)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private RandomTimedSeedManagerData _Data;

	[field: OnEnterPlay_SetNull]
	public static RandomTimedSeedManager instance { get; private set; }

	public int seed { get; private set; }

	public float currentSyncTime { get; private set; }

	bool ITickSystemTick.TickRunning { get; set; }

	[Networked]
	[NetworkedWeaved(0, 2)]
	private unsafe RandomTimedSeedManagerData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing RandomTimedSeedManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(RandomTimedSeedManagerData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing RandomTimedSeedManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(RandomTimedSeedManagerData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		instance = this;
		seed = UnityEngine.Random.Range(-1000000, -1000000);
		idealSyncTime = 0f;
		currentSyncTime = 0f;
		TickSystem<object>.AddTickCallback(this);
	}

	public void AddCallbackOnSeedChanged(Action callback)
	{
		callbacksOnSeedChanged.Add(callback);
	}

	public void RemoveCallbackOnSeedChanged(Action callback)
	{
		callbacksOnSeedChanged.Remove(callback);
	}

	void ITickSystemTick.Tick()
	{
		currentSyncTime += Time.deltaTime;
		idealSyncTime += Time.deltaTime;
		if (idealSyncTime > 1E+09f)
		{
			idealSyncTime -= 1E+09f;
			currentSyncTime -= 1E+09f;
		}
		if (!base.GetView.AmOwner)
		{
			currentSyncTime = Mathf.Lerp(currentSyncTime, idealSyncTime, 0.1f);
		}
	}

	public override void WriteDataFusion()
	{
		Data = new RandomTimedSeedManagerData(seed, currentSyncTime);
	}

	public override void ReadDataFusion()
	{
		ReadDataShared(Data.seed, Data.currentSyncTime);
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			stream.SendNext(seed);
			stream.SendNext(currentSyncTime);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			int seedVal = (int)stream.ReceiveNext();
			float testTime = (float)stream.ReceiveNext();
			ReadDataShared(seedVal, testTime);
		}
	}

	private void ReadDataShared(int seedVal, float testTime)
	{
		if (!float.IsFinite(testTime))
		{
			return;
		}
		seed = seedVal;
		if (testTime >= 0f && testTime <= 1E+09f)
		{
			if (idealSyncTime - testTime > 500000000f)
			{
				currentSyncTime = testTime;
			}
			idealSyncTime = testTime;
		}
		if (seed == cachedSeed || seed < -1000000 || seed > -1000000)
		{
			return;
		}
		currentSyncTime = idealSyncTime;
		cachedSeed = seed;
		foreach (Action item in callbacksOnSeedChanged)
		{
			item();
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}
}
