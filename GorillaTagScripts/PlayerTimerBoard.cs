using System.Collections.Generic;
using System.Text;
using KID.Model;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts;

public class PlayerTimerBoard : MonoBehaviour
{
	[SerializeField]
	private GameObject linesParent;

	public List<PlayerTimerBoardLine> lines;

	public TextMeshPro notInRoomText;

	public TextMeshPro playerColumn;

	public TextMeshPro timeColumn;

	[SerializeField]
	private int startingYValue;

	[SerializeField]
	private int lineHeight;

	private StringBuilder stringBuilder = new StringBuilder(220);

	private StringBuilder stringBuilderTime = new StringBuilder(220);

	private const string MONKE_BLOCKS_TIMER_BOARD_COLUMN_PLAYER_KEY = "MONKE_BLOCKS_TIMER_BOARD_COLUMN_PLAYER";

	private const string MONKE_BLOCKS_TIMER_BOARD_COLUMN_TIMES_KEY = "MONKE_BLOCKS_TIMER_BOARD_COLUMN_TIMES";

	private bool isInitialized;

	public bool IsDirty { get; set; } = true;

	private void Start()
	{
		TryInit();
	}

	private void OnEnable()
	{
		TryInit();
		LocalisationManager.RegisterOnLanguageChanged(RedrawPlayerLines);
	}

	private void TryInit()
	{
		if (!isInitialized && !(PlayerTimerManager.instance == null))
		{
			PlayerTimerManager.instance.RegisterTimerBoard(this);
			isInitialized = true;
		}
	}

	private void OnDisable()
	{
		if (PlayerTimerManager.instance != null)
		{
			PlayerTimerManager.instance.UnregisterTimerBoard(this);
		}
		isInitialized = false;
		LocalisationManager.UnregisterOnLanguageChanged(RedrawPlayerLines);
	}

	public void SetSleepState(bool awake)
	{
		playerColumn.enabled = awake;
		timeColumn.enabled = awake;
		if (linesParent != null)
		{
			linesParent.SetActive(awake);
		}
	}

	public void SortLines()
	{
		lines.Sort(PlayerTimerBoardLine.CompareByTotalTime);
	}

	public void RedrawPlayerLines()
	{
		stringBuilder.Clear();
		stringBuilderTime.Clear();
		if (!LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_TIMER_BOARD_COLUMN_PLAYER", out var result, "<b><color=yellow>PLAYER</color></b>"))
		{
			Debug.LogError("[LOCALIZATION::MONKE_BLOCKS::TIMER] Failed to get key for Game Mode [MONKE_BLOCKS_TIMER_BOARD_COLUMN_PLAYER]");
		}
		stringBuilder.Append("<b><color=yellow>");
		stringBuilder.Append(result);
		stringBuilder.Append("</color></b>");
		if (!LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_TIMER_BOARD_COLUMN_TIMES", out result, "<b><color=yellow>LATEST TIME</color></b>"))
		{
			Debug.LogError("[LOCALIZATION::MONKE_BLOCKS::TIMER] Failed to get key for Game Mode [MONKE_BLOCKS_TIMER_BOARD_COLUMN_TIMES]");
		}
		stringBuilderTime.Append("<b><color=yellow>");
		stringBuilderTime.Append(result);
		stringBuilderTime.Append("</color></b>");
		SortLines();
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Custom_Nametags);
		bool flag = (permissionDataByFeature.Enabled || permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PLAYER) && permissionDataByFeature.ManagedBy != Permission.ManagedByEnum.PROHIBITED;
		for (int i = 0; i < lines.Count; i++)
		{
			try
			{
				if (lines[i].gameObject.activeInHierarchy)
				{
					lines[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0f, startingYValue - lineHeight * i, 0f);
					if (lines[i].linePlayer != null && lines[i].linePlayer.InRoom)
					{
						stringBuilder.Append("\n ");
						stringBuilder.Append(flag ? lines[i].playerNameVisible : lines[i].linePlayer.DefaultName);
						stringBuilderTime.Append("\n ");
						stringBuilderTime.Append(lines[i].playerTimeStr);
					}
				}
			}
			catch
			{
			}
		}
		playerColumn.text = stringBuilder.ToString();
		timeColumn.text = stringBuilderTime.ToString();
		IsDirty = false;
	}
}
