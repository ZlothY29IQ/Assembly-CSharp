using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using GorillaTag;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[Serializable]
public class PhotonEvent : IEquatable<PhotonEvent>
{
	public enum RaiseMode
	{
		Local,
		RemoteOthers,
		RemoteAll
	}

	private const int MAX_EVENT_ARGS = 20;

	private const int INVALID_ID = -1;

	[SerializeField]
	private int _eventId = -1;

	[SerializeField]
	private bool _enabled;

	[SerializeField]
	private bool _reliable;

	[SerializeField]
	private bool _failSilent;

	[NonSerialized]
	private bool _disposed;

	private Action<int, int, object[], PhotonMessageInfoWrapped> _delegate;

	public const byte PHOTON_EVENT_CODE = 176;

	private static readonly RaiseEventOptions gReceiversAll;

	private static readonly RaiseEventOptions gReceiversOthers;

	private static readonly SendOptions gSendReliable;

	private static readonly SendOptions gSendUnreliable;

	private static readonly Dictionary<int, ListProcessor<PhotonEvent>> _photonEvents;

	public bool reliable
	{
		get
		{
			return _reliable;
		}
		set
		{
			_reliable = value;
		}
	}

	public bool failSilent
	{
		get
		{
			return _failSilent;
		}
		set
		{
			_failSilent = value;
		}
	}

	public static event Action<EventData, Exception> OnError;

	private PhotonEvent()
	{
	}

	public PhotonEvent(int eventId)
	{
		if (eventId == -1)
		{
			throw new Exception(string.Format("<{0}> cannot be {1}.", "eventId", -1));
		}
		_eventId = eventId;
		Enable();
	}

	public PhotonEvent(string eventId)
		: this(StaticHash.Compute(eventId))
	{
	}

	public PhotonEvent(int eventId, Action<int, int, object[], PhotonMessageInfoWrapped> callback)
		: this(eventId)
	{
		AddCallback(callback);
	}

	public PhotonEvent(string eventId, Action<int, int, object[], PhotonMessageInfoWrapped> callback)
		: this(eventId)
	{
		AddCallback(callback);
	}

	~PhotonEvent()
	{
		Dispose();
	}

	public void AddCallback(Action<int, int, object[], PhotonMessageInfoWrapped> callback)
	{
		if (_disposed)
		{
			return;
		}
		if (callback == null)
		{
			throw new ArgumentNullException("callback");
		}
		if (_delegate != null)
		{
			Delegate[] invocationList = _delegate.GetInvocationList();
			foreach (Delegate obj in invocationList)
			{
				if ((object)obj != null && obj.Equals(callback))
				{
					return;
				}
			}
		}
		_delegate = (Action<int, int, object[], PhotonMessageInfoWrapped>)Delegate.Combine(_delegate, callback);
	}

	public void RemoveCallback(Action<int, int, object[], PhotonMessageInfoWrapped> callback)
	{
		if (!_disposed && callback != null)
		{
			_delegate = (Action<int, int, object[], PhotonMessageInfoWrapped>)Delegate.Remove(_delegate, callback);
		}
	}

	public void Enable()
	{
		if (!_disposed && !_enabled)
		{
			if (Application.isPlaying)
			{
				AddPhotonEvent(this);
			}
			_enabled = true;
		}
	}

	public void Disable()
	{
		if (!_disposed && _enabled)
		{
			if (Application.isPlaying)
			{
				RemovePhotonEvent(this);
			}
			_enabled = false;
		}
	}

	public void Dispose()
	{
		_delegate = null;
		if (_enabled)
		{
			_enabled = false;
			if (Application.isPlaying)
			{
				RemovePhotonEvent(this);
			}
		}
		_eventId = -1;
		_disposed = true;
	}

	private void InvokeDelegate(int sender, object[] args, PhotonMessageInfoWrapped info)
	{
		_delegate?.Invoke(sender, _eventId, args, info);
	}

	public void RaiseLocal(params object[] args)
	{
		Raise(RaiseMode.Local, args);
	}

	public void RaiseOthers(params object[] args)
	{
		Raise(RaiseMode.RemoteOthers, args);
	}

	public void RaiseAll(params object[] args)
	{
		Raise(RaiseMode.RemoteAll, args);
	}

	private void Raise(RaiseMode mode, params object[] args)
	{
		if (_disposed || !Application.isPlaying || !_enabled)
		{
			return;
		}
		if (args != null && args.Length > 20)
		{
			Debug.LogError(string.Format("{0}: too many event args, max is {1}, trying to send {2}. Stopping!", "PhotonEvent", 20, args.Length));
			return;
		}
		SendOptions sendOptions = (_reliable ? gSendReliable : gSendUnreliable);
		switch (mode)
		{
		case RaiseMode.Local:
			InvokeDelegate(_eventId, args, new PhotonMessageInfoWrapped(PhotonNetwork.LocalPlayer.ActorNumber, PhotonNetwork.ServerTimestamp));
			break;
		case RaiseMode.RemoteOthers:
		{
			object[] eventContent2 = args.Prepend(_eventId).ToArray();
			PhotonNetwork.RaiseEvent(176, eventContent2, gReceiversOthers, sendOptions);
			break;
		}
		case RaiseMode.RemoteAll:
		{
			object[] eventContent = args.Prepend(_eventId).ToArray();
			PhotonNetwork.RaiseEvent(176, eventContent, gReceiversAll, sendOptions);
			break;
		}
		}
	}

	public bool Equals(PhotonEvent other)
	{
		if (other == null)
		{
			return false;
		}
		if (_eventId == other._eventId && _enabled == other._enabled && _reliable == other._reliable && _failSilent == other._failSilent)
		{
			return _disposed == other._disposed;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is PhotonEvent other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int staticHash = _eventId.GetStaticHash();
		int i = StaticHash.Compute(_enabled, _reliable, _failSilent, _disposed);
		return StaticHash.Compute(staticHash, i);
	}

	static PhotonEvent()
	{
		_photonEvents = new Dictionary<int, ListProcessor<PhotonEvent>>(20);
		gReceiversAll = new RaiseEventOptions
		{
			Receivers = ReceiverGroup.All
		};
		gReceiversOthers = new RaiseEventOptions
		{
			Receivers = ReceiverGroup.Others
		};
		gSendUnreliable = SendOptions.SendUnreliable;
		gSendUnreliable.Encrypt = true;
		gSendReliable = SendOptions.SendReliable;
		gSendReliable.Encrypt = true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
	private static void StaticLoadAfterPhotonNetwork()
	{
		PhotonNetwork.NetworkingClient.EventReceived += StaticOnEvent;
	}

	public static bool operator ==(PhotonEvent x, PhotonEvent y)
	{
		return EqualityComparer<PhotonEvent>.Default.Equals(x, y);
	}

	public static bool operator !=(PhotonEvent x, PhotonEvent y)
	{
		return !EqualityComparer<PhotonEvent>.Default.Equals(x, y);
	}

	private static void StaticOnEvent(EventData evData)
	{
		if (evData.Code != 176)
		{
			return;
		}
		try
		{
			if (!(evData.CustomData is object[] array) || array.Length == 0 || array.Length > 21)
			{
				return;
			}
			object obj = array[0];
			if (!(obj is int))
			{
				return;
			}
			int sender = (int)obj;
			if (sender == -1 || !_photonEvents.TryGetValue(sender, out var value))
			{
				return;
			}
			object[] args;
			if (array.Length > 1)
			{
				args = new object[array.Length - 1];
				Array.Copy(array, 1, args, 0, args.Length);
			}
			else
			{
				args = Array.Empty<object>();
			}
			PhotonMessageInfoWrapped info = new PhotonMessageInfoWrapped(evData.Sender, PhotonNetwork.ServerTimestamp);
			value.ItemProcessor = delegate(in PhotonEvent pEv)
			{
				if (pEv._eventId != -1 && !pEv._disposed && pEv._enabled)
				{
					pEv.InvokeDelegate(sender, args, info);
				}
			};
			value.ProcessList();
		}
		catch (Exception arg)
		{
			PhotonEvent.OnError?.Invoke(evData, arg);
		}
	}

	private static void AddPhotonEvent(PhotonEvent photonEvent)
	{
		int eventId = photonEvent._eventId;
		if (eventId != -1)
		{
			if (!_photonEvents.TryGetValue(eventId, out var value))
			{
				value = new ListProcessor<PhotonEvent>(10);
				_photonEvents.Add(eventId, value);
			}
			if (!value.Contains(in photonEvent))
			{
				value.Add(in photonEvent);
			}
		}
	}

	private static void RemovePhotonEvent(PhotonEvent photonEvent)
	{
		if (_photonEvents.TryGetValue(photonEvent._eventId, out var value))
		{
			value.Remove(in photonEvent);
			if (value.Count == 0)
			{
				_photonEvents.Remove(photonEvent._eventId);
			}
		}
	}

	public static PhotonEvent operator +(PhotonEvent photonEvent, Action<int, int, object[], PhotonMessageInfoWrapped> callback)
	{
		if (photonEvent == null)
		{
			throw new ArgumentNullException("photonEvent");
		}
		photonEvent.AddCallback(callback);
		return photonEvent;
	}

	public static PhotonEvent operator -(PhotonEvent photonEvent, Action<int, int, object[], PhotonMessageInfoWrapped> callback)
	{
		if (photonEvent == null)
		{
			throw new ArgumentNullException("photonEvent");
		}
		photonEvent.RemoveCallback(callback);
		return photonEvent;
	}
}
