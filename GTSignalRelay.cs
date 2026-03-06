using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GTSignalRelay : MonoBehaviourStatic<GTSignalRelay>, IOnEventCallback
{
	private static List<GTSignalListener> gActiveListeners = new List<GTSignalListener>(128);

	private static HashSet<GTSignalListener> gListenerSet = new HashSet<GTSignalListener>(128);

	private static Dictionary<int, List<GTSignalListener>> gSignalIdToListeners = new Dictionary<int, List<GTSignalListener>>(128);

	public static IReadOnlyList<GTSignalListener> ActiveListeners => gActiveListeners;

	private void OnEnable()
	{
		if (Application.isPlaying)
		{
			PhotonNetwork.AddCallbackTarget(this);
		}
	}

	private void OnDisable()
	{
		if (Application.isPlaying)
		{
			PhotonNetwork.RemoveCallbackTarget(this);
		}
	}

	public static void Register(GTSignalListener listener)
	{
		if (listener == null)
		{
			return;
		}
		int num = listener.signal;
		if (num != 0 && gListenerSet.Add(listener))
		{
			gActiveListeners.Add(listener);
			if (!gSignalIdToListeners.TryGetValue(num, out var value))
			{
				value = new List<GTSignalListener>(64);
				gSignalIdToListeners.Add(num, value);
			}
			value.Add(listener);
		}
	}

	public static void Unregister(GTSignalListener listener)
	{
		if (!(listener == null))
		{
			gListenerSet.Remove(listener);
			gActiveListeners.Remove(listener);
			if (gSignalIdToListeners.TryGetValue(listener.signal, out var value))
			{
				value.Remove(listener);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitializeOnLoad()
	{
		UnityEngine.Object.DontDestroyOnLoad(new GameObject("GTSignalRelay").AddComponent<GTSignalRelay>());
	}

	void IOnEventCallback.OnEvent(EventData eventData)
	{
		if (eventData.Code != 186 || !(eventData.CustomData is object[] array))
		{
			return;
		}
		int key = (int)array[0];
		if (!gSignalIdToListeners.TryGetValue(key, out var value))
		{
			return;
		}
		int sender = eventData.Sender;
		for (int i = 0; i < value.Count; i++)
		{
			try
			{
				GTSignalListener gTSignalListener = value[i];
				if (!gTSignalListener.deafen && gTSignalListener.IsReady() && (!gTSignalListener.ignoreSelf || sender != gTSignalListener.rigActorID) && (!gTSignalListener.listenToSelfOnly || sender == gTSignalListener.rigActorID))
				{
					gTSignalListener.HandleSignalReceived(sender, array);
					if (gTSignalListener.callUnityEvent)
					{
						gTSignalListener.onSignalReceived?.Invoke();
					}
				}
			}
			catch (Exception)
			{
			}
		}
	}
}
