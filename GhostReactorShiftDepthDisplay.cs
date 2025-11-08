using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[Serializable]
public class GhostReactorShiftDepthDisplay
{
	public GhostReactorShiftManager shiftManager;

	public GhostReactor reactor;

	[SerializeField]
	public TMP_Text jumbotronTitle;

	[SerializeField]
	public TMP_Text jumbotronState;

	[SerializeField]
	public TMP_Text jumbotronTime;

	[SerializeField]
	public TMP_Text jumbotronRequirements;

	[SerializeField]
	public TMP_Text jumbotronRewards;

	[SerializeField]
	public List<TMP_Text> logoFrames;

	[SerializeField]
	private GameObject delveDeeperButton;

	[SerializeField]
	private AudioSource delveDeeperAudio;

	[SerializeField]
	private AudioSource delveDeeperNonspatializedAudio;

	[SerializeField]
	private List<Animation> delveDeeperAnims;

	[SerializeField]
	private List<Animator> delveDeeperAnimators;

	[SerializeField]
	private List<ParticleSystem> delveDeeperParticles;

	private static readonly string[] STATE_NAMES = new string[8] { "--", "PREPARING ENTRY", "PREPARING ENTRY", "READY", "ACTIVE", "EVALUATING SHIFT", "PREPARE TO DIVE", "DIVING" };

	private StringBuilder cachedStringBuilder = new StringBuilder(256);

	public void Setup()
	{
		StopDelveDeeperFX();
	}

	public int GetRewardXP()
	{
		return reactor.GetDepthLevel() * 10 + 10;
	}

	public void RefreshDisplay()
	{
		int depthLevel = reactor.GetDepthLevel();
		reactor.GetDepthLevelConfig(depthLevel);
		reactor.GetDepthLevelConfig(depthLevel + 1);
		switch (shiftManager.GetState())
		{
		case GhostReactorShiftManager.State.WaitingForShiftStart:
		case GhostReactorShiftManager.State.WaitingForFirstShiftStart:
		case GhostReactorShiftManager.State.ShiftActive:
		{
			foreach (TMP_Text logoFrame in logoFrames)
			{
				logoFrame.gameObject.SetActive(value: false);
			}
			cachedStringBuilder.Clear();
			cachedStringBuilder.Append("<color=grey>Team Goals:</color>\n");
			int num = 0;
			if (shiftManager.coresRequiredToDelveDeeper > 0)
			{
				cachedStringBuilder.Append($"Deposit {shiftManager.coresRequiredToDelveDeeper} Cores\n");
				num++;
			}
			if (shiftManager.sentientCoresRequiredToDelveDeeper > 0)
			{
				cachedStringBuilder.Append($"Collect {shiftManager.sentientCoresRequiredToDelveDeeper} Seeds\n");
				num++;
			}
			if (shiftManager.maxPlayerDeaths >= 0)
			{
				cachedStringBuilder.Append($"Limit Incidents to {shiftManager.maxPlayerDeaths}");
				num++;
			}
			jumbotronRequirements.text = cachedStringBuilder.ToString();
			int num2 = reactor.GetCurrLevelGenConfig().coresRequired * 5;
			int rewardXP = GetRewardXP();
			cachedStringBuilder.Clear();
			cachedStringBuilder.Append("<color=grey>Rewards:</color>\n");
			cachedStringBuilder.Append($"+⑭{num2}\n");
			cachedStringBuilder.Append($"+{rewardXP} XP\n");
			jumbotronRewards.text = cachedStringBuilder.ToString();
			break;
		}
		case GhostReactorShiftManager.State.PreparingToDrill:
			jumbotronRequirements.text = "";
			jumbotronRewards.text = "";
			break;
		case GhostReactorShiftManager.State.Drilling:
			jumbotronRequirements.text = "";
			jumbotronRewards.text = "";
			break;
		}
		if (jumbotronState != null)
		{
			int state = (int)shiftManager.GetState();
			if (state >= 0 && state < STATE_NAMES.Length)
			{
				jumbotronState.text = STATE_NAMES[state];
			}
			else
			{
				jumbotronState.text = null;
			}
		}
		RefreshObjectives();
	}

	public void RefreshObjectives()
	{
		GRShiftStat shiftStats = shiftManager.shiftStats;
		bool flag = shiftStats.GetShiftStat(GRShiftStatType.CoresCollected) >= shiftManager.coresRequiredToDelveDeeper;
		bool flag2 = shiftStats.GetShiftStat(GRShiftStatType.SentientCoresCollected) >= shiftManager.sentientCoresRequiredToDelveDeeper;
		bool flag3 = shiftManager.maxPlayerDeaths < 0 || shiftStats.GetShiftStat(GRShiftStatType.PlayerDeaths) <= shiftManager.maxPlayerDeaths;
		if (shiftManager.ShiftActive && flag && flag2 && flag3)
		{
			shiftManager.authorizedToDelveDeeper = true;
		}
		if (shiftManager.IsSoaking())
		{
			shiftManager.authorizedToDelveDeeper = true;
		}
		if (shiftManager.authorizedToDelveDeeper && jumbotronRequirements != null)
		{
			jumbotronRequirements.text = "<color=green>AUTHORIZED TO\nDELVE DEEPER</color>";
		}
		bool authorizedToDelveDeeper = shiftManager.authorizedToDelveDeeper;
		if (delveDeeperButton != null)
		{
			delveDeeperButton.SetActive(authorizedToDelveDeeper && !shiftManager.ShiftActive);
		}
	}

	public void StartDelveDeeperFX()
	{
		delveDeeperAudio.Play();
		delveDeeperNonspatializedAudio.Play();
		for (int i = 0; i < delveDeeperAnims.Count; i++)
		{
			delveDeeperAnims[i].Play();
		}
		for (int j = 0; j < delveDeeperAnimators.Count; j++)
		{
			delveDeeperAnimators[j].enabled = true;
		}
		for (int k = 0; k < delveDeeperParticles.Count; k++)
		{
			ParticleSystem.EmissionModule emission = delveDeeperParticles[k].emission;
			emission.enabled = true;
		}
		GorillaTagger.Instance.StartVibration(forLeftController: false, 0.1f, shiftManager.GetDrillingDuration());
		GorillaTagger.Instance.StartVibration(forLeftController: true, 0.1f, shiftManager.GetDrillingDuration());
	}

	public void StopDelveDeeperFX()
	{
		delveDeeperAudio.Stop();
		delveDeeperNonspatializedAudio.Stop();
		for (int i = 0; i < delveDeeperAnimators.Count; i++)
		{
			delveDeeperAnimators[i].enabled = false;
		}
		for (int j = 0; j < delveDeeperParticles.Count; j++)
		{
			ParticleSystem.EmissionModule emission = delveDeeperParticles[j].emission;
			emission.enabled = false;
		}
	}
}
