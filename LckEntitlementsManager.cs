using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Liv.Lck;
using Liv.Lck.Core;
using Liv.Lck.Core.Cosmetics;
using Liv.Lck.DependencyInjection;
using Photon.Pun;
using UnityEngine;

public class LckEntitlementsManager : MonoBehaviour
{
	private class PlayerProcessRecord
	{
		public int AttemptCount;

		public float TimeoutUntilTimestamp;

		public float LastSeenTimestamp;
	}

	private enum FeatureState
	{
		Checking,
		Enabled,
		Disabled
	}

	[InjectLck]
	private ILckCosmeticsCoordinator _lckCosmeticsCoordinator;

	[InjectLck]
	private ILckCosmeticsFeatureFlagManager _featureFlagManager;

	private const int MAX_API_CALL_ATTEMPTS = 2;

	private const int MAX_CONSECUTIVE_ATTEMPTS = 3;

	private const float ABUSE_TIMEOUT_MINUTES = 1f;

	private const float BATCH_GET_ENTITLEMENTS_INTERVAL_SECONDS = 15f;

	private const float STALE_PLAYER_TIMEOUT_MINUTES = 5f;

	private const string DEFAULT_SESSION_ID = "DefaultSessionId";

	private FeatureState _currentState;

	private readonly HashSet<string> _remotePlayersToGetEntitlementsFor = new HashSet<string>();

	private Coroutine _getEntitlementsBatchingCoroutine;

	private readonly Dictionary<string, PlayerProcessRecord> _processedPlayers = new Dictionary<string, PlayerProcessRecord>();

	private Coroutine _cleanupProcessedPlayersCoroutine;

	private bool _isProcessingBatch;

	public static bool LckEntitlementsEnabled { get; private set; }

	public static LckEntitlementsManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			Instance = this;
		}
	}

	private void OnEnable()
	{
		InitializeFeatureAsync();
		_cleanupProcessedPlayersCoroutine = StartCoroutine(CleanupProcessedPlayersCoroutine());
		_getEntitlementsBatchingCoroutine = StartCoroutine(ProcessBatchedRemotePlayersCoroutine());
	}

	private void OnDisable()
	{
		if (_cleanupProcessedPlayersCoroutine != null)
		{
			StopCoroutine(_cleanupProcessedPlayersCoroutine);
			_cleanupProcessedPlayersCoroutine = null;
		}
		if (_getEntitlementsBatchingCoroutine != null)
		{
			StopCoroutine(_getEntitlementsBatchingCoroutine);
			_getEntitlementsBatchingCoroutine = null;
		}
	}

	private async Task InitializeFeatureAsync()
	{
		_currentState = FeatureState.Checking;
		bool flag = await _featureFlagManager.IsEnabledAsync();
		if (!(this == null))
		{
			_currentState = (flag ? FeatureState.Enabled : FeatureState.Disabled);
			LckEntitlementsEnabled = flag;
		}
	}

	public void OnLocalPlayerSpawned(string localUserId)
	{
		if (ShouldProcessPlayer(localUserId))
		{
			StartCoroutine(ProcessLocalPlayerSpawn(localUserId));
		}
	}

	public void OnRemotePlayerSpawned(string remoteUserId)
	{
		if (_currentState == FeatureState.Disabled || !ShouldProcessPlayer(remoteUserId))
		{
			return;
		}
		lock (_remotePlayersToGetEntitlementsFor)
		{
			_remotePlayersToGetEntitlementsFor.Add(remoteUserId);
		}
	}

	private IEnumerator ProcessLocalPlayerSpawn(string userId)
	{
		yield return new WaitUntil(() => _currentState != FeatureState.Checking);
		if (_currentState != FeatureState.Disabled)
		{
			StartCoroutine(AnnouncePlayerPresenceForSession(userId));
		}
	}

	private bool ShouldProcessPlayer(string userId)
	{
		if (!_processedPlayers.TryGetValue(userId, out var value))
		{
			value = new PlayerProcessRecord();
			_processedPlayers[userId] = value;
		}
		value.LastSeenTimestamp = Time.time;
		if (Time.time < value.TimeoutUntilTimestamp)
		{
			return false;
		}
		if (value.AttemptCount > 3)
		{
			value.AttemptCount = 0;
		}
		value.AttemptCount++;
		if (value.AttemptCount > 3)
		{
			value.TimeoutUntilTimestamp = Time.time + 60f;
			return false;
		}
		return true;
	}

	private IEnumerator ProcessBatchedRemotePlayersCoroutine()
	{
		while (true)
		{
			yield return new WaitForSeconds(15f);
			if (_isProcessingBatch)
			{
				continue;
			}
			List<string> list;
			lock (_remotePlayersToGetEntitlementsFor)
			{
				if (_remotePlayersToGetEntitlementsFor.Count == 0)
				{
					continue;
				}
				list = _remotePlayersToGetEntitlementsFor.ToList();
				_remotePlayersToGetEntitlementsFor.Clear();
				goto IL_008b;
			}
			IL_008b:
			if (list.Count > 0)
			{
				_isProcessingBatch = true;
				GetCosmeticsForPlayersAsync(list, "ProcessBatchedRemotePlayers");
			}
		}
	}

	private IEnumerator AnnouncePlayerPresenceForSession(string localPlayerId)
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			Debug.LogError("LCK: Called AnnouncePlayerPresenceForSession() but no room was found. Player not announced.");
			yield break;
		}
		string sessionId = "DefaultSessionId";
		for (int attempt = 1; attempt <= 2; attempt++)
		{
			Task<Result<bool>> announcementAsync = _lckCosmeticsCoordinator.AnnouncePlayerPresenceForSessionAsync(localPlayerId, sessionId);
			yield return new WaitUntil(() => announcementAsync.IsCompleted);
			if (announcementAsync.IsFaulted || !announcementAsync.Result.IsOk)
			{
				string arg = (announcementAsync.IsFaulted ? announcementAsync.Exception.ToString() : announcementAsync.Result.Message.ToString());
				Debug.LogError($"LCK: Error setting session entitlement (Attempt {attempt}/{2}): {arg}");
				continue;
			}
			yield break;
		}
		Debug.LogError("LCK: All attempts to set session entitlement failed.");
	}

	private async Task GetCosmeticsForPlayersAsync(List<string> userIdList, string methodNameForLogging)
	{
		try
		{
			if (userIdList == null || userIdList.Count == 0)
			{
				return;
			}
			if (PhotonNetwork.CurrentRoom == null)
			{
				Debug.LogError("LCK: Called " + methodNameForLogging + " but no room was found.");
				return;
			}
			string sessionId = "DefaultSessionId";
			await Task.Run(async delegate
			{
				for (int attempt = 1; attempt <= 2; attempt++)
				{
					Result<bool> result = await _lckCosmeticsCoordinator.GetUserCosmeticsForSessionAsync(userIdList, sessionId);
					if (result.IsOk)
					{
						return;
					}
					Debug.LogError($"LCK: Error in {methodNameForLogging} (Attempt {attempt}/{2}): {result.Message}");
				}
				Debug.LogError("LCK: All attempts to call " + methodNameForLogging + " failed.");
			});
		}
		catch (Exception arg)
		{
			Debug.LogError($"LCK: An exception occurred in GetCosmeticsForPlayersAsync: {arg}");
		}
		finally
		{
			_isProcessingBatch = false;
		}
	}

	private IEnumerator CleanupProcessedPlayersCoroutine()
	{
		List<string> playersToRemove = new List<string>();
		while (true)
		{
			yield return new WaitForSeconds(60f);
			playersToRemove.Clear();
			float time = Time.time;
			foreach (KeyValuePair<string, PlayerProcessRecord> processedPlayer in _processedPlayers)
			{
				if (time > processedPlayer.Value.LastSeenTimestamp + 300f)
				{
					playersToRemove.Add(processedPlayer.Key);
				}
			}
			if (playersToRemove.Count <= 0)
			{
				continue;
			}
			foreach (string item in playersToRemove)
			{
				_processedPlayers.Remove(item);
			}
		}
	}
}
