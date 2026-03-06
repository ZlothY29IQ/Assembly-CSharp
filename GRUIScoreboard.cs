using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GRUIScoreboard : MonoBehaviour, IGorillaSliceableSimple
{
	public enum ScoreboardScreen
	{
		DefaultInfo,
		ShiftCutCalculation
	}

	public List<GRUIScoreboardEntry> entries;

	public TMP_Text total;

	public TMP_Text buttonText;

	public ScoreboardScreen currentScreen;

	public GameObject infoTextParent;

	public GameObject calcTextParent;

	public void SliceUpdate()
	{
		if (currentScreen == ScoreboardScreen.ShiftCutCalculation)
		{
			Refresh(GhostReactor.instance.vrRigs);
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void Refresh(List<VRRig> vrRigs)
	{
		if (currentScreen == ScoreboardScreen.ShiftCutCalculation)
		{
			GhostReactor.instance.shiftManager.CalculatePlayerPercentages();
		}
		for (int i = 0; i < entries.Count; i++)
		{
			if (!(entries[i] == null))
			{
				if (i < vrRigs.Count && vrRigs[i] != null && vrRigs[i].OwningNetPlayer != null)
				{
					entries[i].gameObject.SetActive(value: true);
					entries[i].Setup(vrRigs[i], vrRigs[i].OwningNetPlayer.ActorNumber, currentScreen);
				}
				else
				{
					entries[i].gameObject.SetActive(value: false);
				}
			}
		}
	}

	public void SwitchToScreen(ScoreboardScreen screenType)
	{
		currentScreen = screenType;
		switch (currentScreen)
		{
		case ScoreboardScreen.DefaultInfo:
			infoTextParent.SetActive(value: true);
			calcTextParent.SetActive(value: false);
			buttonText.text = "SHOW CUT CALC";
			break;
		case ScoreboardScreen.ShiftCutCalculation:
			infoTextParent.SetActive(value: false);
			calcTextParent.SetActive(value: true);
			buttonText.text = "SHOW INFO";
			break;
		}
	}

	public void SwitchState()
	{
		if (currentScreen == ScoreboardScreen.DefaultInfo)
		{
			SwitchToScreen(ScoreboardScreen.ShiftCutCalculation);
		}
		else
		{
			SwitchToScreen(ScoreboardScreen.DefaultInfo);
		}
		Refresh(GhostReactor.instance.vrRigs);
		GhostReactor.instance.UpdateRemoteScoreboardScreen(currentScreen);
	}

	public static bool ValidPage(ScoreboardScreen screen)
	{
		if (screen != ScoreboardScreen.DefaultInfo)
		{
			return screen == ScoreboardScreen.ShiftCutCalculation;
		}
		return true;
	}
}
