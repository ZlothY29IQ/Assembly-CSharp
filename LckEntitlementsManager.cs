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

	private const string DEFAULT_SESSION_ID = "DefaultSessionId";

	private FeatureState _currentState;

	private readonly HashSet<string> _remotePlayersToGetEntitlementsFor = new HashSet<string>();

	private Coroutine _getEntitlementsBatchingCoroutine;

	private readonly Dictionary<string, PlayerProcessRecord> _processedPlayers = new Dictionary<string, PlayerProcessRecord>();

	private Coroutine _cleanupProcessedPlayersCoroutine;

	public static bool LckEntitlementsEnabled { get; private set; }

	public static LckEntitlementsManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Object.Destroy(base.gameObject);
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
			Debug.Log("LCK: Entitlements feature is " + (LckEntitlementsEnabled ? "Enabled" : "Disabled") + ".");
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
			if (_remotePlayersToGetEntitlementsFor.Add(remoteUserId))
			{
				Debug.Log("LCK: Queued remote player " + remoteUserId + " for batched entitlements check.");
			}
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
		if (Time.time < value.TimeoutUntilTimestamp)
		{
			Debug.LogWarning("LCK: Player " + userId + " is on a timeout. Entitlements Manager will ignore spawn event.");
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
			Debug.LogWarning($"LCK: Player {userId} exceeded max attempts. Applying a {1f}-minute timeout.");
			return false;
		}
		Debug.Log($"LCK: Processing player {userId} (Attempt {value.AttemptCount}/{3}).");
		return true;
	}

	private IEnumerator ProcessBatchedRemotePlayersCoroutine()
	{
		while (true)
		{
			yield return new WaitForSeconds(15f);
			List<string> list;
			lock (_remotePlayersToGetEntitlementsFor)
			{
				if (_remotePlayersToGetEntitlementsFor.Count == 0)
				{
					continue;
				}
				list = _remotePlayersToGetEntitlementsFor.ToList();
				_remotePlayersToGetEntitlementsFor.Clear();
				goto IL_0083;
			}
			IL_0083:
			if (list.Count > 0)
			{
				Debug.Log($"LCK: Processing a batch of {list.Count} remote player(s).");
				StartCoroutine(GetCosmeticsForPlayersCoroutine(list, "ProcessBatchedRemotePlayersCoroutine"));
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
		Debug.Log("LCK: Announcing Presence for local player with UserId: " + localPlayerId + " + Session ID: " + sessionId + ".");
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
			Debug.Log("LCK: Successfully set session entitlement.");
			yield break;
		}
		Debug.LogError("LCK: All attempts to set session entitlement failed.");
	}

	private IEnumerator GetCosmeticsForPlayersCoroutine(IEnumerable<string> playerUserIds, string methodNameForLogging)
	{
		List<string> userIdList = playerUserIds?.ToList() ?? new List<string>();
		if (userIdList.Count == 0)
		{
			yield break;
		}
		if (PhotonNetwork.CurrentRoom == null)
		{
			Debug.LogError("LCK: Called " + methodNameForLogging + " but no room was found.");
			yield break;
		}
		string sessionId = "DefaultSessionId";
		Debug.Log("LCK: Calling " + methodNameForLogging + " for session: " + sessionId + " for players: " + string.Join(", ", userIdList) + ".");
		for (int attempt = 1; attempt <= 2; attempt++)
		{
			Task<Result<bool>> getUserCosmeticsTask = _lckCosmeticsCoordinator.GetUserCosmeticsForSessionAsync(userIdList, sessionId);
			yield return new WaitUntil(() => getUserCosmeticsTask.IsCompleted);
			if (getUserCosmeticsTask.IsFaulted || !getUserCosmeticsTask.Result.IsOk)
			{
				string text = (getUserCosmeticsTask.IsFaulted ? getUserCosmeticsTask.Exception.ToString() : getUserCosmeticsTask.Result.Message.ToString());
				Debug.LogError($"LCK: Error in {methodNameForLogging} (Attempt {attempt}/{2}): {text}");
				continue;
			}
			Debug.Log("LCK: Successfully called " + methodNameForLogging + " endpoint.");
			yield break;
		}
		Debug.LogError("LCK: All attempts to call " + methodNameForLogging + " failed.");
	}

	private IEnumerator CleanupProcessedPlayersCoroutine()
	{
		while (true)
		{
			yield return new WaitForSeconds(60f);
			List<string> list = (from pair in _processedPlayers
				where pair.Value.TimeoutUntilTimestamp > 0f && Time.time > pair.Value.TimeoutUntilTimestamp
				select pair.Key).ToList();
			if (!list.Any())
			{
				continue;
			}
			foreach (string item in list)
			{
				_processedPlayers.Remove(item);
			}
		}
	}
}
