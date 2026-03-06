using System;
using System.Collections.Generic;
using System.Text;
using GorillaGameModes;
using GorillaTagScripts;
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

	public float bigRoomYOffset = 32.5f;

	[SerializeField]
	public List<GorillaPlayerScoreboardLine> lines;

	private List<RectTransform> linesRTs = new List<RectTransform>();

	public GameObject textsParent;

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

	private string GetBeginningString()
	{
		string text = $" ({10})";
		if (NetworkSystem.Instance.SessionIsSubscription)
		{
			text = $" ({20})";
		}
		return "ROOM ID: " + (NetworkSystem.Instance.SessionIsPrivate ? "-PRIVATE- GAME: " : (NetworkSystem.Instance.RoomName + "   GAME: ")) + RoomType() + text + "\n  PLAYER     COLOR  MUTE   REPORT";
	}

	private string RoomType()
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
		int num = 0;
		for (int i = 0; i < lines.Count; i++)
		{
			if (lines[i].gameObject.activeInHierarchy)
			{
				num++;
			}
		}
		if (num > 10)
		{
			linesParent.transform.localScale = new Vector3(1f, 0.5f, 1f);
			linesParent.transform.localPosition = new Vector3(0f, bigRoomYOffset, 0f);
			textsParent.transform.localScale = new Vector3(1f, 0.5f, 1f);
		}
		else
		{
			linesParent.transform.localScale = Vector3.one;
			linesParent.transform.localPosition = Vector3.zero;
			textsParent.transform.localScale = Vector3.one;
		}
		for (int j = 0; j < lines.Count; j++)
		{
			try
			{
				if (!lines[j].gameObject.activeInHierarchy)
				{
					continue;
				}
				linesRTs[j].localPosition = new Vector3(0f, startingYValue - lineHeight * j, 0f);
				if (lines[j].linePlayer == null || !lines[j].linePlayer.InRoom)
				{
					continue;
				}
				stringBuilder.Append("\n ");
				SubscriptionManager.SubscriptionDetails subscriptionDetails = SubscriptionManager.GetSubscriptionDetails(lines[j].linePlayer);
				if (subscriptionDetails.active && subscriptionDetails.tier > 0)
				{
					stringBuilder.Append("<color=#ffc600>");
				}
				else
				{
					stringBuilder.Append("<color=#ffffff>");
				}
				stringBuilder.Append(flag ? lines[j].playerNameVisible : lines[j].linePlayer.DefaultName);
				stringBuilder.Append("</color>");
				if (lines[j].linePlayer != NetworkSystem.Instance.LocalPlayer)
				{
					if (lines[j].reportButton.isActiveAndEnabled)
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
		linesRTs.Clear();
		for (int i = 0; i < lines.Count; i++)
		{
			linesRTs.Add(lines[i].GetComponent<RectTransform>());
		}
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
