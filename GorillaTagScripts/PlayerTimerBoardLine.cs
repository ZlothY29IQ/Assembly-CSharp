using System;
using GorillaExtensions;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTagScripts;

public class PlayerTimerBoardLine : MonoBehaviour
{
	public string playerNameVisible;

	public string playerTimeStr;

	private float playerTimeSeconds;

	public NetPlayer linePlayer;

	public VRRig playerVRRig;

	public PlayerTimerBoard parentBoard;

	internal RigContainer rigContainer;

	private string currentNickname;

	public void ResetData()
	{
		linePlayer = null;
		currentNickname = string.Empty;
		playerTimeStr = string.Empty;
		playerTimeSeconds = 0f;
	}

	public void SetLineData(NetPlayer netPlayer)
	{
		if (netPlayer.InRoom && netPlayer != linePlayer)
		{
			linePlayer = netPlayer;
			if (VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig))
			{
				rigContainer = playerRig;
				playerVRRig = playerRig.Rig;
			}
			InitializeLine();
		}
	}

	public void InitializeLine()
	{
		currentNickname = string.Empty;
		UpdatePlayerText();
		UpdateTimeText();
	}

	public void UpdateLine()
	{
		if (linePlayer != null)
		{
			if (playerNameVisible != playerVRRig.playerNameVisible)
			{
				UpdatePlayerText();
				parentBoard.IsDirty = true;
			}
			string value = playerTimeStr;
			UpdateTimeText();
			if (!playerTimeStr.Equals(value))
			{
				parentBoard.IsDirty = true;
			}
		}
	}

	private void UpdatePlayerText()
	{
		try
		{
			if (rigContainer.IsNull() || playerVRRig.IsNull())
			{
				playerNameVisible = NormalizeName(linePlayer.NickName != currentNickname, linePlayer.NickName);
				currentNickname = linePlayer.NickName;
			}
			else if (rigContainer.Initialized)
			{
				playerNameVisible = playerVRRig.playerNameVisible;
			}
			else if (currentNickname.IsNullOrEmpty() || GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(linePlayer.UserId))
			{
				playerNameVisible = NormalizeName(linePlayer.NickName != currentNickname, linePlayer.NickName);
			}
		}
		catch (Exception)
		{
			playerNameVisible = linePlayer.DefaultName;
			MonkeAgent.instance.SendReport("NmError", linePlayer.UserId, linePlayer.NickName);
		}
	}

	private void UpdateTimeText()
	{
		if (linePlayer != null && PlayerTimerManager.instance != null)
		{
			playerTimeSeconds = PlayerTimerManager.instance.GetLastDurationForPlayer(linePlayer.ActorNumber);
			if (playerTimeSeconds > 0f)
			{
				playerTimeStr = TimeSpan.FromSeconds(playerTimeSeconds).ToString("mm\\:ss\\:ff");
			}
			else
			{
				playerTimeStr = "--:--:--";
			}
		}
		else
		{
			playerTimeStr = "--:--:--";
		}
	}

	public string NormalizeName(bool doIt, string text)
	{
		if (doIt)
		{
			if (GorillaComputer.instance.CheckAutoBanListForName(text))
			{
				text = new string(Array.FindAll(text.ToCharArray(), (char c) => Utils.IsASCIILetterOrDigit(c)));
				if (text.Length > 12)
				{
					text = text.Substring(0, 12);
				}
				text = text.ToUpper();
			}
			else
			{
				text = "BADGORILLA";
				MonkeAgent.instance.SendReport("evading the name ban", linePlayer.UserId, linePlayer.NickName);
			}
		}
		return text;
	}

	public static int CompareByTotalTime(PlayerTimerBoardLine lineA, PlayerTimerBoardLine lineB)
	{
		if (lineA.playerTimeSeconds > 0f && lineB.playerTimeSeconds > 0f)
		{
			return lineA.playerTimeSeconds.CompareTo(lineB.playerTimeSeconds);
		}
		if (lineA.playerTimeSeconds <= 0f)
		{
			return 1;
		}
		if (lineB.playerTimeSeconds <= 0f)
		{
			return -1;
		}
		return 0;
	}
}
