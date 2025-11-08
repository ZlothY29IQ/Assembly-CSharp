using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Sports;

[RequireComponent(typeof(AudioSource))]
[NetworkBehaviourWeaved(2)]
public class SportScoreboard : NetworkComponent
{
	[Serializable]
	private class TeamParameters
	{
		[SerializeField]
		public AudioClip matchWonAudio;

		[SerializeField]
		public AudioClip goalScoredAudio;
	}

	[OnEnterPlay_SetNull]
	public static SportScoreboard Instance;

	[SerializeField]
	private List<TeamParameters> teamParameters = new List<TeamParameters>();

	[SerializeField]
	private int matchEndScore = 3;

	[SerializeField]
	private float matchEndScoreResetDelayTime = 3f;

	private List<int> teamScores = new List<int>();

	private List<int> teamScoresPrev = new List<int>();

	private bool runningMatchEndCoroutine;

	private AudioSource audioSource;

	private SportScoreboardVisuals[] scoreVisuals;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 2)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private int[] _Data;

	[Networked]
	[Capacity(2)]
	[NetworkedWeaved(0, 2)]
	[NetworkedWeavedArray(2, 1, typeof(Fusion.ElementReaderWriterInt32))]
	public unsafe NetworkArray<int> Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing SportScoreboard.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return new NetworkArray<int>((byte*)((NetworkBehaviour)this).Ptr + 0, 2, Fusion.ElementReaderWriterInt32.GetInstance());
		}
	}

	protected override void Awake()
	{
		base.Awake();
		Instance = this;
		audioSource = GetComponent<AudioSource>();
		scoreVisuals = new SportScoreboardVisuals[teamParameters.Count];
		for (int i = 0; i < teamParameters.Count; i++)
		{
			teamScores.Add(0);
			teamScoresPrev.Add(0);
		}
	}

	public void RegisterTeamVisual(int TeamIndex, SportScoreboardVisuals visuals)
	{
		scoreVisuals[TeamIndex] = visuals;
		UpdateScoreboard();
	}

	private void UpdateScoreboard()
	{
		for (int i = 0; i < teamParameters.Count; i++)
		{
			if (!(scoreVisuals[i] == null))
			{
				int num = teamScores[i];
				if (scoreVisuals[i].score1s != null)
				{
					scoreVisuals[i].score1s.SetUVOffset(num % 10);
				}
				if (scoreVisuals[i].score10s != null)
				{
					scoreVisuals[i].score10s.SetUVOffset(num / 10 % 10);
				}
			}
		}
	}

	private void OnScoreUpdated()
	{
		for (int i = 0; i < teamScores.Count; i++)
		{
			if (teamScores[i] > teamScoresPrev[i] && teamParameters[i].goalScoredAudio != null && teamScores[i] < matchEndScore)
			{
				audioSource.GTPlayOneShot(teamParameters[i].goalScoredAudio);
			}
			teamScoresPrev[i] = teamScores[i];
		}
		if (!runningMatchEndCoroutine)
		{
			for (int j = 0; j < teamScores.Count; j++)
			{
				if (teamScores[j] >= matchEndScore)
				{
					StartCoroutine(MatchEndCoroutine(j));
					break;
				}
			}
		}
		UpdateScoreboard();
	}

	public void TeamScored(int team)
	{
		if (base.IsMine && !runningMatchEndCoroutine)
		{
			if (team >= 0 && team < teamScores.Count)
			{
				teamScores[team] += 1;
			}
			OnScoreUpdated();
		}
	}

	public void ResetScores()
	{
		if (base.IsMine && !runningMatchEndCoroutine)
		{
			for (int i = 0; i < teamScores.Count; i++)
			{
				teamScores[i] = 0;
			}
			OnScoreUpdated();
		}
	}

	private IEnumerator MatchEndCoroutine(int winningTeam)
	{
		runningMatchEndCoroutine = true;
		if (winningTeam >= 0 && winningTeam < teamParameters.Count && teamParameters[winningTeam].matchWonAudio != null)
		{
			audioSource.GTPlayOneShot(teamParameters[winningTeam].matchWonAudio);
		}
		yield return new WaitForSeconds(matchEndScoreResetDelayTime);
		runningMatchEndCoroutine = false;
		ResetScores();
	}

	public override void WriteDataFusion()
	{
		Data.CopyFrom(teamScores, 0, teamScores.Count);
	}

	public override void ReadDataFusion()
	{
		teamScores.Clear();
		Data.CopyTo(teamScores);
		OnScoreUpdated();
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		for (int i = 0; i < teamScores.Count; i++)
		{
			stream.SendNext(teamScores[i]);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		for (int i = 0; i < teamScores.Count; i++)
		{
			teamScores[i] = (int)stream.ReceiveNext();
		}
		OnScoreUpdated();
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		NetworkBehaviourUtils.InitializeNetworkArray(Data, _Data, "Data");
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		NetworkBehaviourUtils.CopyFromNetworkArray(Data, ref _Data);
	}
}
