using System;
using System.Text;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class GRToolStatusWatch : MonoBehaviour, IGameEntityComponent
{
	private enum WatchState
	{
		Dropped,
		SnappedLocal,
		SnappedRemote
	}

	public GameEntity gameEntity;

	private GRPlayer currentPlayer;

	private int visibleHP;

	private int visibleShield;

	public GameObject disabledVisuals;

	public GameObject enabledVisuals;

	public GameObject[] healthHearts;

	public GameObject shieldSymbol;

	public Vector3 homeBase;

	public Transform gimbaledCompass;

	public TextMeshPro statsText;

	public TextMeshPro disabledText;

	private int lastKills;

	private int lastCredits;

	private int lastJuice;

	private int lastGrade;

	private StringBuilder sb = new StringBuilder();

	private WatchState state;

	private GRToolProgressionManager progression;

	public void OnEntityInit()
	{
		if (gameEntity == null)
		{
			gameEntity = GetComponent<GameEntity>();
		}
		UpdateVisuals();
		progression = gameEntity.manager.GetComponent<GhostReactorManager>().reactor.toolProgression;
		GameEntity obj = gameEntity;
		obj.OnSnapped = (Action)Delegate.Combine(obj.OnSnapped, new Action(UpdateSnappedPlayer));
		GameEntity obj2 = gameEntity;
		obj2.OnUnsnapped = (Action)Delegate.Combine(obj2.OnUnsnapped, new Action(RemoveSnappedPlayer));
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
	}

	public void UpdateSnappedPlayer()
	{
		currentPlayer = GRPlayer.Get(gameEntity.snappedByActorNumber);
		lastKills = -1;
		lastCredits = -1;
		lastJuice = -1;
		lastGrade = -1;
		if (currentPlayer == GRPlayer.GetLocal())
		{
			state = WatchState.SnappedLocal;
		}
		else
		{
			state = WatchState.SnappedRemote;
		}
		disabledText.text = "LEAVE ME ALONE!\n\nTHIS IS ONLY FOR MY OWNER!!!";
		UpdateVisuals();
	}

	public void RemoveSnappedPlayer()
	{
		currentPlayer = null;
		state = WatchState.Dropped;
		disabledText.text = "LOW POWER\n\nPUT ME ON";
		UpdateVisuals();
	}

	private void Update()
	{
		if (!(currentPlayer == null))
		{
			UpdateVisuals();
		}
	}

	private void UpdateVisuals()
	{
		bool flag = state == WatchState.SnappedLocal || state == WatchState.SnappedRemote;
		if (disabledVisuals.activeSelf == flag)
		{
			disabledVisuals.SetActive(!flag);
		}
		if (enabledVisuals.activeSelf != flag)
		{
			enabledVisuals.SetActive(flag);
		}
		if (state != WatchState.SnappedLocal)
		{
			return;
		}
		if (visibleHP != currentPlayer.Hp / 100)
		{
			visibleHP = currentPlayer.Hp / 100;
			for (int i = 0; i < healthHearts.Length; i++)
			{
				if (healthHearts[i].activeSelf != i < visibleHP)
				{
					healthHearts[i].SetActive(i < visibleHP);
				}
			}
		}
		if (visibleShield != currentPlayer.ShieldHp / 100)
		{
			visibleShield = currentPlayer.ShieldHp / 100;
			if (shieldSymbol.activeSelf != visibleShield > 0)
			{
				shieldSymbol.SetActive(visibleShield > 0);
			}
		}
		gimbaledCompass.LookAt(homeBase, Vector3.up);
		int num = (int)currentPlayer.synchronizedSessionStats[5];
		int shiftCredits = currentPlayer.ShiftCredits;
		int numberOfResearchPoints = progression.GetNumberOfResearchPoints();
		var (level, num2, _, _) = GhostReactorProgression.GetGradePointDetails(currentPlayer.CurrentProgression.redeemedPoints);
		if (num != lastKills || shiftCredits != lastCredits || numberOfResearchPoints != lastJuice || num2 != lastGrade)
		{
			sb.Clear();
			sb.Append(num);
			sb.Append("\n\n");
			sb.Append(numberOfResearchPoints);
			sb.Append("\n\n");
			sb.Append(shiftCredits);
			sb.Append("\n\n\n");
			sb.Append(GhostReactorProgression.GetTitleNameFromLevel(level)[0]);
			sb.Append(num2);
			statsText.text = sb.ToString();
			lastKills = num;
			lastCredits = shiftCredits;
			lastJuice = numberOfResearchPoints;
			lastGrade = num2;
		}
	}
}
