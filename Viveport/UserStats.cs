using System;
using AOT;
using Viveport.Internal;

namespace Viveport;

public class UserStats
{
	public enum LeaderBoardRequestType
	{
		GlobalData,
		GlobalDataAroundUser,
		LocalData,
		LocalDataAroundUser
	}

	public enum LeaderBoardTimeRange
	{
		AllTime,
		Daily,
		Weekly,
		Monthly
	}

	public enum LeaderBoardSortMethod
	{
		None,
		Ascending,
		Descending
	}

	public enum LeaderBoardDiaplayType
	{
		None,
		Numeric,
		TimeSeconds,
		TimeMilliSeconds
	}

	public enum LeaderBoardScoreMethod
	{
		None,
		KeepBest,
		ForceUpdate
	}

	public enum AchievementDisplayAttribute
	{
		Name,
		Desc,
		Hidden
	}

	private static Viveport.Internal.StatusCallback isReadyIl2cppCallback;

	private static Viveport.Internal.StatusCallback downloadStatsIl2cppCallback;

	private static Viveport.Internal.StatusCallback uploadStatsIl2cppCallback;

	private static Viveport.Internal.StatusCallback downloadLeaderboardScoresIl2cppCallback;

	private static Viveport.Internal.StatusCallback uploadLeaderboardScoreIl2cppCallback;

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void IsReadyIl2cppCallback(int errorCode)
	{
		isReadyIl2cppCallback(errorCode);
	}

	public static int IsReady(StatusCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		isReadyIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(IsReadyIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.UserStats.IsReady_64(IsReadyIl2cppCallback);
		}
		return Viveport.Internal.UserStats.IsReady(IsReadyIl2cppCallback);
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void DownloadStatsIl2cppCallback(int errorCode)
	{
		downloadStatsIl2cppCallback(errorCode);
	}

	public static int DownloadStats(StatusCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		downloadStatsIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(DownloadStatsIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.UserStats.DownloadStats_64(DownloadStatsIl2cppCallback);
		}
		return Viveport.Internal.UserStats.DownloadStats(DownloadStatsIl2cppCallback);
	}

	public static int GetStat(string name, int defaultValue)
	{
		int pnData = defaultValue;
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.UserStats.GetStat_64(name, ref pnData);
		}
		else
		{
			Viveport.Internal.UserStats.GetStat(name, ref pnData);
		}
		return pnData;
	}

	public static float GetStat(string name, float defaultValue)
	{
		float pfData = defaultValue;
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.UserStats.GetStat_64(name, ref pfData);
		}
		else
		{
			Viveport.Internal.UserStats.GetStat(name, ref pfData);
		}
		return pfData;
	}

	public static void SetStat(string name, int value)
	{
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.UserStats.SetStat_64(name, value);
		}
		else
		{
			Viveport.Internal.UserStats.SetStat(name, value);
		}
	}

	public static void SetStat(string name, float value)
	{
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.UserStats.SetStat_64(name, value);
		}
		else
		{
			Viveport.Internal.UserStats.SetStat(name, value);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void UploadStatsIl2cppCallback(int errorCode)
	{
		uploadStatsIl2cppCallback(errorCode);
	}

	public static int UploadStats(StatusCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		uploadStatsIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(UploadStatsIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.UserStats.UploadStats_64(UploadStatsIl2cppCallback);
		}
		return Viveport.Internal.UserStats.UploadStats(UploadStatsIl2cppCallback);
	}

	public static bool GetAchievement(string pchName)
	{
		int pbAchieved = 0;
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.UserStats.GetAchievement_64(pchName, ref pbAchieved);
		}
		else
		{
			Viveport.Internal.UserStats.GetAchievement(pchName, ref pbAchieved);
		}
		return pbAchieved == 1;
	}

	public static int GetAchievementUnlockTime(string pchName)
	{
		int punUnlockTime = 0;
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.UserStats.GetAchievementUnlockTime_64(pchName, ref punUnlockTime);
		}
		else
		{
			Viveport.Internal.UserStats.GetAchievementUnlockTime(pchName, ref punUnlockTime);
		}
		return punUnlockTime;
	}

	public static string GetAchievementIcon(string pchName)
	{
		return "";
	}

	public static string GetAchievementDisplayAttribute(string pchName, AchievementDisplayAttribute attr)
	{
		return "";
	}

	public static string GetAchievementDisplayAttribute(string pchName, AchievementDisplayAttribute attr, Locale locale)
	{
		return "";
	}

	public static int SetAchievement(string pchName)
	{
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.UserStats.SetAchievement_64(pchName);
		}
		return Viveport.Internal.UserStats.SetAchievement(pchName);
	}

	public static int ClearAchievement(string pchName)
	{
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.UserStats.ClearAchievement_64(pchName);
		}
		return Viveport.Internal.UserStats.ClearAchievement(pchName);
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void DownloadLeaderboardScoresIl2cppCallback(int errorCode)
	{
		downloadLeaderboardScoresIl2cppCallback(errorCode);
	}

	public static int DownloadLeaderboardScores(StatusCallback callback, string pchLeaderboardName, LeaderBoardRequestType eLeaderboardDataRequest, LeaderBoardTimeRange eLeaderboardDataTimeRange, int nRangeStart, int nRangeEnd)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		downloadLeaderboardScoresIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(DownloadLeaderboardScoresIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.UserStats.DownloadLeaderboardScores_64(DownloadLeaderboardScoresIl2cppCallback, pchLeaderboardName, (ELeaderboardDataRequest)eLeaderboardDataRequest, (ELeaderboardDataTimeRange)eLeaderboardDataTimeRange, nRangeStart, nRangeEnd);
		}
		return Viveport.Internal.UserStats.DownloadLeaderboardScores(DownloadLeaderboardScoresIl2cppCallback, pchLeaderboardName, (ELeaderboardDataRequest)eLeaderboardDataRequest, (ELeaderboardDataTimeRange)eLeaderboardDataTimeRange, nRangeStart, nRangeEnd);
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void UploadLeaderboardScoreIl2cppCallback(int errorCode)
	{
		uploadLeaderboardScoreIl2cppCallback(errorCode);
	}

	public static int UploadLeaderboardScore(StatusCallback callback, string pchLeaderboardName, int nScore)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		uploadLeaderboardScoreIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(UploadLeaderboardScoreIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.UserStats.UploadLeaderboardScore_64(UploadLeaderboardScoreIl2cppCallback, pchLeaderboardName, nScore);
		}
		return Viveport.Internal.UserStats.UploadLeaderboardScore(UploadLeaderboardScoreIl2cppCallback, pchLeaderboardName, nScore);
	}

	public static Leaderboard GetLeaderboardScore(int index)
	{
		LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
		pLeaderboardEntry.m_nGlobalRank = 0;
		pLeaderboardEntry.m_nScore = 0;
		pLeaderboardEntry.m_pUserName = "";
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.UserStats.GetLeaderboardScore_64(index, ref pLeaderboardEntry);
		}
		else
		{
			Viveport.Internal.UserStats.GetLeaderboardScore(index, ref pLeaderboardEntry);
		}
		return new Leaderboard
		{
			Rank = pLeaderboardEntry.m_nGlobalRank,
			Score = pLeaderboardEntry.m_nScore,
			UserName = pLeaderboardEntry.m_pUserName
		};
	}

	public static int GetLeaderboardScoreCount()
	{
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.UserStats.GetLeaderboardScoreCount_64();
		}
		return Viveport.Internal.UserStats.GetLeaderboardScoreCount();
	}

	public static LeaderBoardSortMethod GetLeaderboardSortMethod()
	{
		if (IntPtr.Size == 8)
		{
			return (LeaderBoardSortMethod)Viveport.Internal.UserStats.GetLeaderboardSortMethod_64();
		}
		return (LeaderBoardSortMethod)Viveport.Internal.UserStats.GetLeaderboardSortMethod();
	}

	public static LeaderBoardDiaplayType GetLeaderboardDisplayType()
	{
		if (IntPtr.Size == 8)
		{
			return (LeaderBoardDiaplayType)Viveport.Internal.UserStats.GetLeaderboardDisplayType_64();
		}
		return (LeaderBoardDiaplayType)Viveport.Internal.UserStats.GetLeaderboardDisplayType();
	}
}
