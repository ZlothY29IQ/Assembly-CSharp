using System;
using GorillaNetworking;
using TMPro;
using UnityEngine;

[Serializable]
public class GRShuttleUI
{
	public TMP_Text playerName;

	public TMP_Text playerTitle;

	public TMP_Text destFloorText;

	public TMP_Text infoText;

	public GameObject validScreen;

	public GameObject invalidScreen;

	public GRShuttle shuttle;

	private NetPlayer player;

	private GhostReactor reactor;

	public void Setup(GhostReactor reactor, NetPlayer player)
	{
		this.reactor = reactor;
		this.player = player;
		RefreshUI();
	}

	public void RefreshUI()
	{
		if (playerName != null)
		{
			playerName.text = ((player == null) ? null : player.SanitizedNickName);
		}
		if (playerTitle != null)
		{
			GRPlayer gRPlayer = ((player == null) ? null : GRPlayer.Get(player.ActorNumber));
			if (gRPlayer != null)
			{
				playerTitle.text = GhostReactorProgression.GetTitleName(gRPlayer.CurrentProgression.redeemedPoints);
			}
			else
			{
				playerTitle.text = null;
			}
		}
		if (!(shuttle != null))
		{
			return;
		}
		int targetFloor = shuttle.GetTargetFloor();
		if (destFloorText != null)
		{
			if (targetFloor == -1)
			{
				destFloorText.text = "HQ";
			}
			else
			{
				destFloorText.text = (targetFloor + 1).ToString();
			}
		}
		bool flag = targetFloor <= shuttle.GetMaxDropFloor();
		validScreen.SetActive(flag);
		invalidScreen.SetActive(!flag);
		if (flag)
		{
			infoText.text = "READY!\n\nDROP TO LEVEL";
		}
		else
		{
			infoText.text = "UNSAFE!\n\nUPGRADE DROP CHASSIS";
		}
	}
}
