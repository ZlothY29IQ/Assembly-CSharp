using System;
using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using UnityEngine;

public class PhotonPrefabPool : MonoBehaviour, IPunPrefabPoolVerify, IPunPrefabPool, ITickSystemPre
{
	[SerializeField]
	private PrefabType[] networkPrefabsData;

	public Dictionary<string, PrefabType> networkPrefabs = new Dictionary<string, PrefabType>();

	private Queue<GameObject> objectsWaiting = new Queue<GameObject>(20);

	private Queue<GameObject> queueBeingProcssed = new Queue<GameObject>(20);

	private HashSet<GameObject> objectsQueued = new HashSet<GameObject>();

	private HashSet<GameObject> netInstantiedObjects = new HashSet<GameObject>();

	private List<PhotonView> tempViews = new List<PhotonView>(5);

	private List<GameObject> m_invalidCreatePool = new List<GameObject>(100);

	private HashSet<GameObject> m_m_invalidCreatePoolLookup = new HashSet<GameObject>(100);

	private bool waiting;

	bool ITickSystemPre.PreTickRunning { get; set; }

	private void Awake()
	{
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	private void Start()
	{
		PhotonNetwork.PrefabPool = this;
		for (int i = 0; i < networkPrefabsData.Length; i++)
		{
			ref PrefabType reference = ref networkPrefabsData[i];
			if ((bool)reference.prefab)
			{
				if (string.IsNullOrEmpty(reference.prefabName))
				{
					reference.prefabName = reference.prefab.name;
				}
				int photonViewCount = reference.prefab.GetComponentsInChildren<PhotonView>().Length;
				reference.photonViewCount = photonViewCount;
				networkPrefabs.Add(reference.prefabName, reference);
			}
		}
	}

	bool IPunPrefabPoolVerify.VerifyInstantiation(Player sender, string prefabName, Vector3 position, Quaternion rotation, int[] viewIDs, out GameObject prefab)
	{
		prefab = null;
		if (viewIDs == null || !position.IsValid(10000f) || !rotation.IsValid() || !networkPrefabs.TryGetValue(prefabName, out var value) || viewIDs.Length != value.photonViewCount)
		{
			return false;
		}
		int num = sender?.ActorNumber ?? 0;
		int num2 = viewIDs[0] / PhotonNetwork.MAX_VIEW_IDS;
		for (int i = 0; i < viewIDs.Length; i++)
		{
			int num3 = viewIDs[i];
			if (PhotonNetwork.ViewIDExists(num3))
			{
				return false;
			}
			for (int j = 0; j < viewIDs.Length; j++)
			{
				if (j != i && viewIDs[j] == num3)
				{
					return false;
				}
			}
			int num4 = num3 / PhotonNetwork.MAX_VIEW_IDS;
			if (num4 != num2)
			{
				return false;
			}
			if (num4 == 0)
			{
				if (!value.roomObject)
				{
					return false;
				}
			}
			else if (num4 != num)
			{
				return false;
			}
		}
		prefab = value.prefab;
		return true;
	}

	GameObject IPunPrefabPoolVerify.Instantiate(GameObject prefabInstance, Vector3 position, Quaternion rotation)
	{
		bool activeSelf = prefabInstance.activeSelf;
		if (activeSelf)
		{
			prefabInstance.SetActive(value: false);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(prefabInstance, position, rotation);
		netInstantiedObjects.Add(gameObject);
		if (activeSelf)
		{
			prefabInstance.SetActive(value: true);
		}
		return gameObject;
	}

	GameObject IPunPrefabPool.Instantiate(string prefabId, Vector3 position, Quaternion rotation)
	{
		if (!networkPrefabs.TryGetValue(prefabId, out var value))
		{
			return null;
		}
		return ((IPunPrefabPoolVerify)this).Instantiate(value.prefab, position, rotation);
	}

	void IPunPrefabPool.Destroy(GameObject netObj)
	{
		if (netObj.IsNull())
		{
			return;
		}
		if (netInstantiedObjects.Remove(netObj))
		{
			if (m_invalidCreatePool.Count < 200 && netObj.TryGetComponent<PhotonViewCache>(out var component) && !component.Initialized)
			{
				if (m_m_invalidCreatePoolLookup.Add(netObj))
				{
					m_invalidCreatePool.Add(netObj);
				}
			}
			else
			{
				UnityEngine.Object.Destroy(netObj);
			}
			return;
		}
		if (!netObj.TryGetComponent<PhotonView>(out var component2) || component2.isRuntimeInstantiated)
		{
			UnityEngine.Object.Destroy(netObj);
			return;
		}
		if (!objectsQueued.Contains(netObj))
		{
			objectsWaiting.Enqueue(netObj);
			objectsQueued.Add(netObj);
		}
		if (!waiting)
		{
			waiting = true;
			TickSystem<object>.AddPreTickCallback(this);
		}
	}

	void ITickSystemPre.PreTick()
	{
		if (waiting)
		{
			waiting = false;
			return;
		}
		Queue<GameObject> queue = queueBeingProcssed;
		Queue<GameObject> queue2 = objectsWaiting;
		objectsWaiting = queue;
		queueBeingProcssed = queue2;
		GameObject gameObject = null;
		while (queueBeingProcssed.Count > 0)
		{
			gameObject = queueBeingProcssed.Dequeue();
			objectsQueued.Remove(gameObject);
			if (!gameObject.IsNull())
			{
				gameObject.SetActive(value: true);
				gameObject.GetComponents(tempViews);
				for (int i = 0; i < tempViews.Count; i++)
				{
					PhotonNetwork.RegisterPhotonView(tempViews[i]);
				}
			}
		}
		if (objectsQueued.Count < 1)
		{
			TickSystem<object>.RemovePreTickCallback(this);
		}
		else
		{
			waiting = true;
		}
	}

	private void OnLeftRoom()
	{
		foreach (GameObject item in m_invalidCreatePool)
		{
			if (!item.IsNull())
			{
				UnityEngine.Object.Destroy(item);
			}
		}
		m_invalidCreatePool.Clear();
		m_m_invalidCreatePoolLookup.Clear();
	}

	private void CheckVOIPSettings(RemoteVoiceLink voiceLink)
	{
		try
		{
			NetPlayer netPlayer = null;
			if (voiceLink.Info.UserData != null)
			{
				if (int.TryParse(voiceLink.Info.UserData.ToString(), out var result))
				{
					netPlayer = NetworkSystem.Instance.GetPlayer(result / PhotonNetwork.MAX_VIEW_IDS);
				}
			}
			else
			{
				netPlayer = NetworkSystem.Instance.GetPlayer(voiceLink.PlayerId);
			}
			if (netPlayer != null && (voiceLink.Info.Bitrate > 20000 || voiceLink.Info.SamplingRate > 16000) && VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig))
			{
				playerRig.ForceMute = true;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.ToString());
		}
	}
}
