using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaTag;
using GorillaTagScripts;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Rendering;

public class VRRigCache : MonoBehaviour
{
	private const string preLog = "[GT/VRRigCache] ";

	private const string preErr = "[GT/VRRigCache]  ERROR!!!  ";

	private const string preErrBeta = "[GT/VRRigCache]  ERROR!!!  (beta only log) ";

	private const string preErrEd = "[GT/VRRigCache]  ERROR!!!  (editor only log) ";

	public RigContainer localRig;

	[SerializeField]
	private Transform rigParent;

	[SerializeField]
	private Transform networkParent;

	[SerializeField]
	private GameObject rigTemplate;

	private int rigAmount = 19;

	[SerializeField]
	private TickSystemTimer m_ensureNetworkObjectTimer = new TickSystemTimer(0.1f);

	[OnEnterPlay_Clear]
	private static Queue<RigContainer> freeRigs = new Queue<RigContainer>(19);

	[OnEnterPlay_Clear]
	private static Dictionary<NetPlayer, RigContainer> rigsInUse = new Dictionary<NetPlayer, RigContainer>(19);

	[OnEnterPlay_Clear]
	private static readonly List<RigContainer> m_activeRigContainers = new List<RigContainer>(20);

	[OnEnterPlay_Clear]
	private static readonly List<VRRig> m_activeRigs = new List<VRRig>(20);

	[OnEnterPlay_Set(false)]
	private static bool _isBatchingRigActivations;

	private static object[] rigRGBData = new object[3] { 0f, 0f, 0f };

	[field: OnEnterPlay_SetNull]
	public static VRRigCache Instance { get; private set; }

	public Transform NetworkParent => networkParent;

	public static IReadOnlyList<RigContainer> ActiveRigContainers => m_activeRigContainers;

	public static IReadOnlyList<VRRig> ActiveRigs => m_activeRigs;

	[field: OnEnterPlay_Set(false)]
	public static bool isInitialized { get; private set; }

	public static event Action OnActiveRigsChanged;

	public static event Action OnPostInitialize;

	public static event Action OnPostSpawnRig;

	public static event Action<RigContainer> OnRigActivated;

	public static event Action<RigContainer> OnRigDeactivated;

	public static event Action<RigContainer> OnRigNameChanged;

	private void Awake()
	{
		InitializeVRRigCache();
		if (localRig != null && localRig.Rig != null)
		{
			VRRig rig = localRig.Rig;
			rig.OnNameChanged = (Action<RigContainer>)Delegate.Combine(rig.OnNameChanged, VRRigCache.OnRigNameChanged);
			if (localRig.Rig.bodyRenderer != null)
			{
				localRig.Rig.bodyRenderer.SetupAsLocalPlayerBody();
			}
		}
		TickSystemTimer ensureNetworkObjectTimer = m_ensureNetworkObjectTimer;
		ensureNetworkObjectTimer.callback = (Action)Delegate.Combine(ensureNetworkObjectTimer.callback, new Action(InstantiateNetworkObject));
		NetworkedPlayerColourNotifier.SetLocalRigReference(localRig);
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
		isInitialized = false;
		if (localRig != null && localRig.Rig != null)
		{
			VRRig rig = localRig.Rig;
			rig.OnNameChanged = (Action<RigContainer>)Delegate.Remove(rig.OnNameChanged, VRRigCache.OnRigNameChanged);
		}
	}

	public void InitializeVRRigCache()
	{
		if (isInitialized || ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		Instance = this;
		if (rigParent == null)
		{
			rigParent = base.transform;
		}
		if (networkParent == null)
		{
			networkParent = base.transform;
		}
		for (int i = 0; i < rigAmount; i++)
		{
			RigContainer rigContainer = SpawnRig();
			freeRigs.Enqueue(rigContainer);
			rigContainer.Rig.BuildInitialize();
			rigContainer.Rig.transform.parent = null;
		}
		m_activeRigContainers.Add(localRig);
		m_activeRigs.Add(localRig.Rig);
		isInitialized = true;
		VRRigCache.OnPostInitialize?.Invoke();
		VRRigCache.OnPostSpawnRig?.Invoke();
	}

	private RigContainer SpawnRig()
	{
		if (rigTemplate.activeSelf)
		{
			rigTemplate.SetActive(value: false);
		}
		return UnityEngine.Object.Instantiate(rigTemplate, rigParent, worldPositionStays: false)?.GetComponent<RigContainer>();
	}

	internal bool TryGetVrrig(Player targetPlayer, out RigContainer playerRig)
	{
		return TryGetVrrig(NetworkSystem.Instance.GetPlayer(targetPlayer.ActorNumber), out playerRig);
	}

	internal bool TryGetVrrig(int targetPlayerId, out RigContainer playerRig)
	{
		return TryGetVrrig(NetworkSystem.Instance.GetPlayer(targetPlayerId), out playerRig);
	}

	internal bool TryGetVrrig(NetPlayer targetPlayer, out RigContainer playerRig)
	{
		playerRig = null;
		if (ApplicationQuittingState.IsQuitting)
		{
			return false;
		}
		if (targetPlayer == null || targetPlayer.IsNull)
		{
			GTDev.LogError("[GT/VRRigCache]  ERROR!!!  TryGetVrrig: Supplied targetPlayer cannot be null!");
			return false;
		}
		if (targetPlayer.IsLocal)
		{
			playerRig = localRig;
			return true;
		}
		if (!targetPlayer.InRoom)
		{
			return false;
		}
		if (!rigsInUse.TryGetValue(targetPlayer, out playerRig))
		{
			if (freeRigs.Count <= 0)
			{
				return false;
			}
			playerRig = freeRigs.Dequeue();
			playerRig.Creator = targetPlayer;
			rigsInUse.Add(targetPlayer, playerRig);
			m_activeRigContainers.Add(playerRig);
			m_activeRigs.Add(playerRig.Rig);
			VRRig rig = playerRig.Rig;
			rig.OnNameChanged = (Action<RigContainer>)Delegate.Remove(rig.OnNameChanged, VRRigCache.OnRigNameChanged);
			VRRig rig2 = playerRig.Rig;
			rig2.OnNameChanged = (Action<RigContainer>)Delegate.Combine(rig2.OnNameChanged, VRRigCache.OnRigNameChanged);
			playerRig.gameObject.SetActive(value: true);
			playerRig.RigEvents.SendPostEnableEvent();
			if (!_isBatchingRigActivations)
			{
				GamePlayer.UpdateStaticLookupCaches();
			}
			VRRigCache.OnRigActivated?.Invoke(playerRig);
			if (!_isBatchingRigActivations)
			{
				VRRigCache.OnActiveRigsChanged?.Invoke();
			}
		}
		return true;
	}

	public void OnPlayerEnteredRoom(NetPlayer newPlayer)
	{
		if (newPlayer.ActorNumber == -1)
		{
			Debug.LogError("LocalPlayer returned, vrrig no correctly initialised");
		}
		TryGetVrrig(newPlayer, out var _);
	}

	public void OnJoinedRoom()
	{
		_isBatchingRigActivations = true;
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		foreach (NetPlayer targetPlayer in allNetPlayers)
		{
			TryGetVrrig(targetPlayer, out var _);
		}
		_isBatchingRigActivations = false;
		m_ensureNetworkObjectTimer.Start();
		GamePlayer.UpdateStaticLookupCaches();
		VRRigCache.OnActiveRigsChanged?.Invoke();
	}

	public void OnPlayerLeftRoom(NetPlayer leavingPlayer)
	{
		if (leavingPlayer.IsNull)
		{
			Debug.LogError("Leaving players NetPlayer is Null");
			CheckForMissingPlayer();
		}
		if (rigsInUse.TryGetValue(leavingPlayer, out var value))
		{
			value.gameObject.Disable();
			VRRig rig = value.Rig;
			rig.OnNameChanged = (Action<RigContainer>)Delegate.Remove(rig.OnNameChanged, VRRigCache.OnRigNameChanged);
			freeRigs.Enqueue(value);
			rigsInUse.Remove(leavingPlayer);
			m_activeRigContainers.Remove(value);
			m_activeRigs.Remove(value.Rig);
			GamePlayer.UpdateStaticLookupCaches();
			VRRigCache.OnRigDeactivated?.Invoke(value);
			VRRigCache.OnActiveRigsChanged?.Invoke();
		}
		else
		{
			LogError("failed to find player's vrrig who left " + leavingPlayer.UserId);
		}
	}

	private void CheckForMissingPlayer()
	{
		foreach (KeyValuePair<NetPlayer, RigContainer> item in rigsInUse)
		{
			if (item.Key == null || item.Value == null)
			{
				Debug.LogError("Somehow null reference in rigsInUse");
			}
			else if (!item.Key.InRoom)
			{
				item.Value.gameObject.Disable();
				VRRig rig = item.Value.Rig;
				rig.OnNameChanged = (Action<RigContainer>)Delegate.Remove(rig.OnNameChanged, VRRigCache.OnRigNameChanged);
				freeRigs.Enqueue(item.Value);
				rigsInUse.Remove(item.Key);
				m_activeRigContainers.Remove(item.Value);
				m_activeRigs.Remove(item.Value.Rig);
				GamePlayer.UpdateStaticLookupCaches();
				VRRigCache.OnRigDeactivated?.Invoke(item.Value);
				VRRigCache.OnActiveRigsChanged?.Invoke();
			}
		}
	}

	public void OnLeftRoom()
	{
		m_ensureNetworkObjectTimer.Stop();
		Dictionary<NetPlayer, RigContainer> value;
		using (DictionaryPool<NetPlayer, RigContainer>.Get(out value))
		{
			value.EnsureCapacity(rigsInUse.Count);
			value.Clear();
			NetPlayer key;
			RigContainer value2;
			foreach (KeyValuePair<NetPlayer, RigContainer> item in rigsInUse)
			{
				item.Deconstruct(out key, out value2);
				NetPlayer key2 = key;
				RigContainer value3 = value2;
				value.Add(key2, value3);
			}
			foreach (KeyValuePair<NetPlayer, RigContainer> item2 in value)
			{
				item2.Deconstruct(out key, out value2);
				NetPlayer key3 = key;
				RigContainer rigContainer = value2;
				if (!(rigContainer == null))
				{
					_ = rigsInUse[key3].Rig;
					VRRig rig = rigContainer.Rig;
					rig.OnNameChanged = (Action<RigContainer>)Delegate.Remove(rig.OnNameChanged, VRRigCache.OnRigNameChanged);
					rigContainer.gameObject.Disable();
					rigsInUse.Remove(key3);
					freeRigs.Enqueue(rigContainer);
				}
			}
			m_activeRigContainers.Clear();
			m_activeRigContainers.Add(localRig);
			m_activeRigs.Clear();
			m_activeRigs.Add(localRig.Rig);
			GamePlayer.UpdateStaticLookupCaches();
			if (VRRigCache.OnRigDeactivated != null)
			{
				foreach (RigContainer value4 in value.Values)
				{
					VRRigCache.OnRigDeactivated(value4);
				}
			}
			VRRigCache.OnActiveRigsChanged?.Invoke();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal VRRig[] GetAllRigs()
	{
		VRRig[] array = new VRRig[rigsInUse.Count + freeRigs.Count];
		int num = 0;
		foreach (RigContainer value in rigsInUse.Values)
		{
			array[num] = value.Rig;
			num++;
		}
		foreach (RigContainer freeRig in freeRigs)
		{
			array[num] = freeRig.Rig;
			num++;
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetAllUsedRigs(List<VRRig> rigs)
	{
		if (rigs == null)
		{
			return;
		}
		foreach (RigContainer value in rigsInUse.Values)
		{
			rigs.Add(value.Rig);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetActiveRigs(List<VRRig> rigsListToUpdate)
	{
		if (rigsListToUpdate == null)
		{
			return;
		}
		rigsListToUpdate.Clear();
		if (!isInitialized)
		{
			return;
		}
		rigsListToUpdate.Add(Instance.localRig.Rig);
		foreach (RigContainer value in rigsInUse.Values)
		{
			rigsListToUpdate.Add(value.Rig);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ApplyToAllRigs(Action<VRRig> action)
	{
		foreach (RigContainer value in rigsInUse.Values)
		{
			action(value.Rig);
		}
		foreach (RigContainer freeRig in freeRigs)
		{
			action(freeRig.Rig);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ApplyToAllActiveRigs(Action<VRRig> action)
	{
		foreach (RigContainer value in rigsInUse.Values)
		{
			action(value.Rig);
		}
	}

	internal int GetAllRigsHash()
	{
		int num = 0;
		foreach (RigContainer value in rigsInUse.Values)
		{
			num += value.GetInstanceID();
		}
		foreach (RigContainer freeRig in freeRigs)
		{
			num += freeRig.GetInstanceID();
		}
		return num;
	}

	internal void InstantiateNetworkObject()
	{
		if (!localRig.netView.IsNotNull() && NetworkSystem.Instance.InRoom)
		{
			if (!Instance.GetComponent<PhotonPrefabPool>().networkPrefabs.TryGetValue("Player Network Controller", out var value) || value.prefab == null)
			{
				Debug.LogError("OnJoinedRoom: Unable to find player prefab to spawn");
				return;
			}
			GameObject gameObject = GTPlayer.Instance.gameObject;
			Color playerColor = localRig.Rig.playerColor;
			rigRGBData[0] = playerColor.r;
			rigRGBData[1] = playerColor.g;
			rigRGBData[2] = playerColor.b;
			NetworkSystem.Instance.NetInstantiate(value.prefab, gameObject.transform.position, gameObject.transform.rotation, isRoomObject: false, 0, rigRGBData);
		}
	}

	internal void OnVrrigSerializerSuccesfullySpawned()
	{
		GamePlayer.UpdateStaticLookupCaches();
		VRRigCache.OnActiveRigsChanged?.Invoke();
	}

	private void LogInfo(string log)
	{
	}

	private void LogWarning(string log)
	{
	}

	private void LogError(string log)
	{
	}
}
