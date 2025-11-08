using System;
using ExitGames.Client.Photon;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

public class PUNErrorLogging : MonoBehaviour
{
	[Flags]
	private enum LogFlags
	{
		SerializeView = 1,
		OwnershipTransfer = 2,
		OwnershipRequest = 4,
		OwnershipUpdate = 8,
		RPC = 0x10,
		Instantiate = 0x20,
		Destroy = 0x40,
		DestroyPlayer = 0x80
	}

	[SerializeField]
	private bool m_logSerializeView = true;

	[SerializeField]
	private bool m_logOwnershipTransfer = true;

	[SerializeField]
	private bool m_logOwnershipRequest = true;

	[SerializeField]
	private bool m_logOwnershipUpdate = true;

	[SerializeField]
	private bool m_logRPC = true;

	[SerializeField]
	private bool m_logInstantiate = true;

	[SerializeField]
	private bool m_logDestroy = true;

	[SerializeField]
	private bool m_logDestroyPlayer = true;

	private void Start()
	{
		PhotonNetwork.InternalEventError = (Action<EventData, Exception>)Delegate.Combine(PhotonNetwork.InternalEventError, new Action<EventData, Exception>(PUNError));
		PlayFabTitleDataCache.Instance.GetTitleData("PUNErrorLogging", delegate(string data)
		{
			if (int.TryParse(data, out var result))
			{
				LogFlags logFlags = (LogFlags)result;
				m_logSerializeView = logFlags.HasFlag(LogFlags.SerializeView);
				m_logOwnershipTransfer = logFlags.HasFlag(LogFlags.OwnershipTransfer);
				m_logOwnershipRequest = logFlags.HasFlag(LogFlags.OwnershipRequest);
				m_logOwnershipUpdate = logFlags.HasFlag(LogFlags.OwnershipUpdate);
				m_logRPC = logFlags.HasFlag(LogFlags.RPC);
				m_logInstantiate = logFlags.HasFlag(LogFlags.Instantiate);
				m_logDestroy = logFlags.HasFlag(LogFlags.Destroy);
				m_logDestroyPlayer = logFlags.HasFlag(LogFlags.DestroyPlayer);
			}
		}, delegate
		{
		});
	}

	private void PUNError(EventData data, Exception exception)
	{
		NetworkSystem.Instance.GetPlayer(data.Sender);
		switch (data.Code)
		{
		case 254:
			PrintException(exception, print: true);
			break;
		case 201:
		case 206:
			PrintException(exception, m_logSerializeView);
			break;
		case 210:
			PrintException(exception, m_logOwnershipTransfer);
			break;
		case 209:
			PrintException(exception, m_logOwnershipRequest);
			break;
		case 212:
			PrintException(exception, m_logOwnershipUpdate);
			break;
		case 200:
			PrintException(exception, m_logRPC);
			break;
		case 202:
			PrintException(exception, m_logInstantiate);
			break;
		case 204:
			PrintException(exception, m_logDestroy);
			break;
		case 207:
			PrintException(exception, m_logDestroyPlayer);
			break;
		default:
			PrintException(exception, print: true);
			break;
		}
	}

	private void PrintException(Exception e, bool print)
	{
		if (print)
		{
			Debug.LogException(e);
		}
	}
}
