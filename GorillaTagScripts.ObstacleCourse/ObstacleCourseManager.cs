using System;
using System.Collections.Generic;
using Fusion;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.ObstacleCourse;

[NetworkBehaviourWeaved(9)]
public class ObstacleCourseManager : NetworkComponent, ITickSystemTick
{
	public List<ObstacleCourse> allObstaclesCourses = new List<ObstacleCourse>();

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 9)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private ObstacleCourseData _Data;

	public static ObstacleCourseManager Instance { get; private set; }

	public bool TickRunning { get; set; }

	[Networked]
	[NetworkedWeaved(0, 9)]
	public unsafe ObstacleCourseData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing ObstacleCourseManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(ObstacleCourseData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing ObstacleCourseManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(ObstacleCourseData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		Instance = this;
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		TickSystem<object>.AddCallbackTarget(this);
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnEnable();
		TickSystem<object>.RemoveCallbackTarget(this);
	}

	public void Tick()
	{
		foreach (ObstacleCourse allObstaclesCourse in allObstaclesCourses)
		{
			allObstaclesCourse.InvokeUpdate();
		}
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		allObstaclesCourses.Clear();
	}

	public override void WriteDataFusion()
	{
		Data = new ObstacleCourseData(allObstaclesCourses);
	}

	public override void ReadDataFusion()
	{
		for (int i = 0; i < Data.ObstacleCourseCount; i++)
		{
			int winnerActorNumber = Data.WinnerActorNumber[i];
			ObstacleCourse.RaceState raceState = (ObstacleCourse.RaceState)Data.CurrentRaceState[i];
			if (allObstaclesCourses[i].currentState != raceState)
			{
				allObstaclesCourses[i].Deserialize(winnerActorNumber, raceState);
			}
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			stream.SendNext(allObstaclesCourses.Count);
			for (int i = 0; i < allObstaclesCourses.Count; i++)
			{
				stream.SendNext(allObstaclesCourses[i].winnerActorNumber);
				stream.SendNext(allObstaclesCourses[i].currentState);
			}
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender != PhotonNetwork.MasterClient)
		{
			return;
		}
		int num = (int)stream.ReceiveNext();
		for (int i = 0; i < num; i++)
		{
			int winnerActorNumber = (int)stream.ReceiveNext();
			ObstacleCourse.RaceState raceState = (ObstacleCourse.RaceState)stream.ReceiveNext();
			if (allObstaclesCourses[i].currentState != raceState)
			{
				allObstaclesCourses[i].Deserialize(winnerActorNumber, raceState);
			}
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
