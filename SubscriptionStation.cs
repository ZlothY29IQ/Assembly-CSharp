using System;
using GorillaTagScripts;
using TMPro;
using UnityEngine;

[Obsolete("DEPRECATED! Use SubscriptionKiosk instead")]
public class SubscriptionStation : MonoBehaviour
{
	[SerializeField]
	private TMP_Text screenText;

	private string formatString;

	private void Awake()
	{
		formatString = screenText.text;
		screenText.text = string.Format(formatString, "*", "*", "*", "*");
	}

	private void UpdateScreen()
	{
		Debug.Log(":::SubscriptionStation::UpdateScreen");
		bool num = SubscriptionManager.GetSubscriptionDetails(VRRig.LocalRig).tier > 0;
		int daysAccrued = SubscriptionManager.GetSubscriptionDetails(VRRig.LocalRig).daysAccrued;
		bool subsOnlyMatchmaking = SubscriptionManager.SubsOnlyMatchmaking;
		bool showGoldNameTag = VRRig.LocalRig.ShowGoldNameTag;
		if (num)
		{
			screenText.text = string.Format(formatString, "Y", subsOnlyMatchmaking ? "Y" : "N", showGoldNameTag ? "Y" : "N", daysAccrued);
		}
		else
		{
			screenText.text = string.Format(formatString, "N", "*", "*", "*");
		}
	}

	public void ToggleSubscriptionStatus()
	{
		SubscriptionManager.ForceRecheck();
		UpdateScreen();
	}

	public void ToggleSubsOnly()
	{
		SubscriptionManager.SubsOnlyMatchmaking = !SubscriptionManager.SubsOnlyMatchmaking;
		UpdateScreen();
	}

	public void ToggleSubsDecoration()
	{
		UpdateScreen();
	}
}
