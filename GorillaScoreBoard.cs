using System;
using System.Collections.Generic;
using System.Text;
using GorillaGameModes;
using TMPro;
using UnityEngine;

public class GorillaScoreBoard : MonoBehaviour
{
	public GameObject scoreBoardLinePrefab;

	public int startingYValue;

	public int lineHeight;

	public bool includeMMR;

	public bool isActive;

	public GameObject linesParent;

	[SerializeField]
	public List<GorillaPlayerScoreboardLine> lines;

	public TextMeshPro boardText;

	public TextMeshPro buttonText;

	public bool needsUpdate;

	public TextMeshPro notInRoomText;

	public string initialGameMode;

	private string tempGmName;

	private string gmName;

	private const string error = "ERROR";

	private List<string> gmNames;

	private bool _isDirty = true;

	private StringBuilder stringBuilder = new StringBuilder(220);

	private StringBuilder buttonStringBuilder = new StringBuilder(720);

	public bool IsDirty
	{
		get
		{
			if (!_isDirty)
			{
				return string.IsNullOrEmpty(initialGameMode);
			}
			return true;
		}
		set
		{
			_isDirty = value;
		}
	}

	public void SetSleepState(bool awake)
	{
		boardText.enabled = awake;
		buttonText.enabled = awake;
		if (linesParent != null)
		{
			linesParent.SetActive(awake);
		}
	}

	private void OnDestroy()
	{
	}

	public string GetBeginningString()
	{
		return "ROOM ID: " + (NetworkSystem.Instance.SessionIsPrivate ? "-PRIVATE- GAME: " : (NetworkSystem.Instance.RoomName + "   GAME: ")) + RoomType() + "\n  PLAYER     COLOR  MUTE   REPORT";
	}

	public string RoomType()
	{
		initialGameMode = RoomSystem.RoomGameMode;
		gmNames = GameMode.gameModeNames;
		gmName = "ERROR";
		int count = gmNames.Count;
		int num = initialGameMode.LastIndexOf('|');
		if (num >= 0)
		{
			tempGmName = initialGameMode.Substring(num + 1);
			for (int i = 0; i < count; i++)
			{
				if (tempGmName == gmNames[i])
				{
					gmName = tempGmName;
					break;
				}
			}
		}
		else
		{
			for (int j = 0; j < count; j++)
			{
				tempGmName = gmNames[j];
				if (initialGameMode.Contains(tempGmName))
				{
					gmName = tempGmName;
					break;
				}
			}
		}
		return gmName;
	}

	public void RedrawPlayerLines()
	{
		stringBuilder.Clear();
		stringBuilder.Append(GetBeginningString());
		buttonStringBuilder.Clear();
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags);
		for (int i = 0; i < lines.Count; i++)
		{
			try
			{
				if (!lines[i].gameObject.activeInHierarchy)
				{
					continue;
				}
				lines[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0f, startingYValue - lineHeight * i, 0f);
				if (lines[i].linePlayer == null || !lines[i].linePlayer.InRoom)
				{
					continue;
				}
				stringBuilder.Append("\n ");
				stringBuilder.Append(flag ? lines[i].playerNameVisible : lines[i].linePlayer.DefaultName);
				if (lines[i].linePlayer != NetworkSystem.Instance.LocalPlayer)
				{
					if (lines[i].reportButton.isActiveAndEnabled)
					{
						buttonStringBuilder.Append("MUTE                                REPORT\n");
					}
					else
					{
						buttonStringBuilder.Append("MUTE                HATE SPEECH    TOXICITY     CHEATING       CANCEL\n");
					}
				}
				else
				{
					buttonStringBuilder.Append("\n");
				}
			}
			catch
			{
			}
		}
		boardText.text = stringBuilder.ToString();
		buttonText.text = buttonStringBuilder.ToString();
		_isDirty = false;
	}

	public string NormalizeName(bool doIt, string text)
	{
		if (doIt)
		{
			text = new string(Array.FindAll(text.ToCharArray(), (char c) => Utils.IsASCIILetterOrDigit(c)));
			if (text.Length > 12)
			{
				text = text.Substring(0, 10);
			}
			text = text.ToUpper();
		}
		return text;
	}

	private void Start()
	{
		GorillaScoreboardTotalUpdater.RegisterScoreboard(this);
	}

	private void OnEnable()
	{
		GorillaScoreboardTotalUpdater.RegisterScoreboard(this);
		_isDirty = true;
	}

	private void OnDisable()
	{
		GorillaScoreboardTotalUpdater.UnregisterScoreboard(this);
	}
}
