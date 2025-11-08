using System;
using AOT;
using Viveport.Internal;

namespace Viveport;

public class ArcadeLeaderboard
{
	public enum LeaderboardTimeRange
	{
		AllTime
	}

	private static Viveport.Internal.StatusCallback isReadyIl2cppCallback;

	private static Viveport.Internal.StatusCallback downloadLeaderboardScoresIl2cppCallback;

	private static Viveport.Internal.StatusCallback uploadLeaderboardScoreIl2cppCallback;

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void IsReadyIl2cppCallback(int errorCode)
	{
		isReadyIl2cppCallback(errorCode);
	}

	public static void IsReady(StatusCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		isReadyIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(IsReadyIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.ArcadeLeaderboard.IsReady_64(IsReadyIl2cppCallback);
		}
		else
		{
			Viveport.Internal.ArcadeLeaderboard.IsReady(IsReadyIl2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void DownloadLeaderboardScoresIl2cppCallback(int errorCode)
	{
		downloadLeaderboardScoresIl2cppCallback(errorCode);
	}

	public static void DownloadLeaderboardScores(StatusCallback callback, string pchLeaderboardName, LeaderboardTimeRange eLeaderboardDataTimeRange, int nCount)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		downloadLeaderboardScoresIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(DownloadLeaderboardScoresIl2cppCallback);
		eLeaderboardDataTimeRange = LeaderboardTimeRange.AllTime;
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.ArcadeLeaderboard.DownloadLeaderboardScores_64(DownloadLeaderboardScoresIl2cppCallback, pchLeaderboardName, (ELeaderboardDataTimeRange)eLeaderboardDataTimeRange, nCount);
		}
		else
		{
			Viveport.Internal.ArcadeLeaderboard.DownloadLeaderboardScores(DownloadLeaderboardScoresIl2cppCallback, pchLeaderboardName, (ELeaderboardDataTimeRange)eLeaderboardDataTimeRange, nCount);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void UploadLeaderboardScoreIl2cppCallback(int errorCode)
	{
		uploadLeaderboardScoreIl2cppCallback(errorCode);
	}

	public static void UploadLeaderboardScore(StatusCallback callback, string pchLeaderboardName, string pchUserName, int nScore)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		uploadLeaderboardScoreIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(UploadLeaderboardScoreIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.ArcadeLeaderboard.UploadLeaderboardScore_64(UploadLeaderboardScoreIl2cppCallback, pchLeaderboardName, pchUserName, nScore);
		}
		else
		{
			Viveport.Internal.ArcadeLeaderboard.UploadLeaderboardScore(UploadLeaderboardScoreIl2cppCallback, pchLeaderboardName, pchUserName, nScore);
		}
	}

	public static Leaderboard GetLeaderboardScore(int index)
	{
		LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
		pLeaderboardEntry.m_nGlobalRank = 0;
		pLeaderboardEntry.m_nScore = 0;
		pLeaderboardEntry.m_pUserName = "";
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.ArcadeLeaderboard.GetLeaderboardScore_64(index, ref pLeaderboardEntry);
		}
		else
		{
			Viveport.Internal.ArcadeLeaderboard.GetLeaderboardScore(index, ref pLeaderboardEntry);
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
			return Viveport.Internal.ArcadeLeaderboard.GetLeaderboardScoreCount_64();
		}
		return Viveport.Internal.ArcadeLeaderboard.GetLeaderboardScoreCount();
	}

	public static int GetLeaderboardUserRank()
	{
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.ArcadeLeaderboard.GetLeaderboardUserRank_64();
		}
		return Viveport.Internal.ArcadeLeaderboard.GetLeaderboardUserRank();
	}

	public static int GetLeaderboardUserScore()
	{
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.ArcadeLeaderboard.GetLeaderboardUserScore_64();
		}
		return Viveport.Internal.ArcadeLeaderboard.GetLeaderboardUserScore();
	}
}
