using System;
using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class SIQuestBoard : MonoBehaviour, IGorillaSliceableSimple
{
	public SuperInfection superInfection;

	public List<SIUIPlayerQuestDisplay> questDisplays;

	public BoxCollider bonusPointArea;

	public Bounds bounds;

	public ParticleSystem celebrateParticle;

	public TextMeshProUGUI timeToNewQuests;

	private int lastHours;

	private int lastMinutes;

	private int lastSeconds;

	public void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		for (int i = 0; i < questDisplays.Count; i++)
		{
			stream.SendNext(questDisplays[i].activePlayerActorNumber);
		}
	}

	public void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		for (int i = 0; i < questDisplays.Count; i++)
		{
			questDisplays[i].activePlayerActorNumber = (int)stream.ReceiveNext();
		}
	}

	public void GrantBonusPointProgress()
	{
		if (bounds.Contains(GTPlayer.Instance.HeadCenterPosition))
		{
			SIPlayer.LocalPlayer.GetBonusProgress(superInfection.siManager);
		}
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		if (superInfection.siManager.gameEntityManager.IsAuthority())
		{
			AuthorityUpdateScreenAssignments();
		}
		DateTime utcNow = DateTime.UtcNow;
		DateTime dateTime = utcNow.Date + SIProgression.Instance.CROSSOVER_TIME_OF_DAY;
		if (dateTime < utcNow)
		{
			dateTime = dateTime.AddDays(1.0);
		}
		TimeSpan timeSpan = dateTime - utcNow;
		if (lastHours != timeSpan.Hours || lastMinutes != timeSpan.Minutes || lastSeconds != timeSpan.Seconds)
		{
			timeToNewQuests.text = "NEW QUESTS IN: " + timeSpan.ToString("hh\\:mm\\:ss");
		}
		lastHours = timeSpan.Hours;
		lastMinutes = timeSpan.Minutes;
		lastSeconds = timeSpan.Seconds;
	}

	private void AuthorityUpdateScreenAssignments()
	{
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		for (int i = 0; i < allNetPlayers.Length; i++)
		{
			list.Add(allNetPlayers[i].ActorNumber);
		}
		for (int j = 0; j < questDisplays.Count; j++)
		{
			int activePlayerActorNumber = questDisplays[j].activePlayerActorNumber;
			if (activePlayerActorNumber != -1)
			{
				if (!list.Contains(activePlayerActorNumber))
				{
					questDisplays[j].activePlayerActorNumber = -1;
				}
				else if (!list2.Contains(activePlayerActorNumber))
				{
					list2.Add(activePlayerActorNumber);
				}
			}
		}
		for (int k = 0; k < allNetPlayers.Length; k++)
		{
			int actorNumber = allNetPlayers[k].ActorNumber;
			if (list2.Contains(actorNumber))
			{
				continue;
			}
			for (int l = 0; l < questDisplays.Count; l++)
			{
				if (questDisplays[l].activePlayerActorNumber == -1)
				{
					questDisplays[l].activePlayerActorNumber = actorNumber;
					break;
				}
			}
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (bonusPointArea.gameObject.activeSelf)
		{
			bounds = bonusPointArea.bounds;
			bonusPointArea.gameObject.SetActive(value: false);
		}
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void ForceCompleteQuest(int index)
	{
	}

	public void CheatAddPoints(int points)
	{
	}

	public void CheatAddBonusPoints(int points)
	{
	}
}
