using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using GorillaExtensions;
using GorillaTag;
using Liv.Lck;
using Liv.Lck.Core;
using Liv.Lck.Core.Cosmetics;
using Liv.Lck.DependencyInjection;
using Photon.Pun;
using UnityEngine;

[NetworkBehaviourWeaved(0)]
public class LckEntitlementsNetworked : NetworkComponent
{
	private enum FeatureState
	{
		Checking,
		Enabled,
		Disabled
	}

	private readonly struct QueuedSpawn
	{
		public readonly RigContainer Rig;

		public readonly PhotonMessageInfoWrapped Info;

		public QueuedSpawn(RigContainer rig, PhotonMessageInfoWrapped info)
		{
			Rig = rig;
			Info = info;
		}
	}

	[Header("Configuration")]
	[SerializeField]
	private VRRigSerializer m_rigNetworkController;

	[InjectLck]
	private ILckCore _lckCore;

	[InjectLck]
	private ILckCosmeticsCoordinator _lckCosmeticsCoordinator;

	[InjectLck]
	private ILckCosmeticsFeatureFlagManager _featureFlagManager;

	private const int MAX_ATTEMPTS = 2;

	private FeatureState _currentState;

	private readonly Queue<QueuedSpawn> _queuedSpawns = new Queue<QueuedSpawn>();

	public static bool LckEntitlementsEnabled { get; private set; }

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void Awake()
	{
		base.Awake();
		if (m_rigNetworkController.IsNull())
		{
			m_rigNetworkController = GetComponentInParent<VRRigSerializer>();
		}
		if (m_rigNetworkController.IsNull())
		{
			Debug.LogError("LCK: Unable to find VRRigSerializer");
		}
		else
		{
			m_rigNetworkController.SuccesfullSpawnEvent.Add(new InAction<RigContainer, PhotonMessageInfoWrapped>(OnSuccessfulSpawn));
		}
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		InitializeFeatureAsync();
	}

	private async Task InitializeFeatureAsync()
	{
		_currentState = FeatureState.Checking;
		bool flag = await _featureFlagManager.IsEnabledAsync();
		if (!(this == null))
		{
			if (flag)
			{
				_currentState = FeatureState.Enabled;
				LckEntitlementsEnabled = true;
				base.OnEnable();
				ProcessSpawnQueue();
			}
			else
			{
				_currentState = FeatureState.Disabled;
				LckEntitlementsEnabled = false;
				_queuedSpawns.Clear();
			}
		}
	}

	private void OnSuccessfulSpawn(in RigContainer rig, in PhotonMessageInfoWrapped info)
	{
		switch (_currentState)
		{
		case FeatureState.Checking:
			_queuedSpawns.Enqueue(new QueuedSpawn(rig, info));
			break;
		case FeatureState.Enabled:
			ProcessSpawn(in rig, in info);
			break;
		case FeatureState.Disabled:
			break;
		}
	}

	private void ProcessSpawn(in RigContainer rig, in PhotonMessageInfoWrapped info)
	{
		if (_lckCosmeticsCoordinator == null)
		{
			Debug.LogError("LCK: ILckCosmeticsCoordinator has not been injected. Entitlement checks will fail.");
		}
		else if (base.IsLocallyOwned)
		{
			StartCoroutine(AnnouncePlayerPresenceForSession());
			StartCoroutine(GetAllOtherPlayerCosmeticsForSession());
		}
		else
		{
			StartCoroutine(GetNewPlayerCosmeticsForSession());
		}
	}

	private void ProcessSpawnQueue()
	{
		while (_queuedSpawns.Count > 0)
		{
			QueuedSpawn queuedSpawn = _queuedSpawns.Dequeue();
			ProcessSpawn(in queuedSpawn.Rig, in queuedSpawn.Info);
		}
	}

	private IEnumerator AnnouncePlayerPresenceForSession()
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			Debug.LogError("LCK: Called AnnouncePlayerPresenceForSession() but no room was found. Player not announced.");
			yield break;
		}
		string localUserId = m_rigNetworkController.VRRig.OwningNetPlayer.UserId;
		string uniqueUserSessionId = PhotonNetwork.CurrentRoom.Name;
		Debug.Log("LCK: Announcing Presence for local player with UserId: " + localUserId + " + Session ID: " + uniqueUserSessionId + ".");
		for (int attempt = 1; attempt <= 2; attempt++)
		{
			Task<Result<bool>> announcementAsync = _lckCosmeticsCoordinator.AnnouncePlayerPresenceForSessionAsync(localUserId, uniqueUserSessionId);
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

	private IEnumerator GetNewPlayerCosmeticsForSession()
	{
		string[] playerUserIds = new string[1] { m_rigNetworkController.VRRig.OwningNetPlayer.UserId };
		yield return GetCosmeticsForPlayersCoroutine(playerUserIds, "GetNewPlayerCosmeticsForSession");
	}

	private IEnumerator GetAllOtherPlayerCosmeticsForSession()
	{
		IEnumerable<string> playerUserIds = NetworkSystem.Instance.PlayerListOthers.Select((NetPlayer p) => p.UserId);
		yield return GetCosmeticsForPlayersCoroutine(playerUserIds, "GetAllOtherPlayerCosmeticsForSession");
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
		string sessionId = PhotonNetwork.CurrentRoom.Name;
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

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		if (m_rigNetworkController != null && m_rigNetworkController.SuccesfullSpawnEvent != null)
		{
			m_rigNetworkController.SuccesfullSpawnEvent.Remove(new InAction<RigContainer, PhotonMessageInfoWrapped>(OnSuccessfulSpawn));
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}
