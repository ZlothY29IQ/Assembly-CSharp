using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PhotonSignal
{
	private class RefID
	{
		public int intValue;

		private static int gNextID = 1;

		private static int gRefCount = 0;

		private static readonly ConditionalWeakTable<PhotonSignal, RefID> gRefTable = new ConditionalWeakTable<PhotonSignal, RefID>();

		public static int Count => gRefCount;

		public RefID()
		{
			intValue = StaticHash.ComputeTriple32(gNextID++);
			gRefCount++;
		}

		~RefID()
		{
			gRefCount--;
		}

		public static int Register(PhotonSignal ps)
		{
			if (ps == null)
			{
				return 0;
			}
			RefID refID = new RefID();
			gRefTable.Add(ps, refID);
			return refID.intValue;
		}
	}

	protected int _signalID;

	protected bool _enabled;

	[SerializeField]
	protected ReceiverGroup _receivers = ReceiverGroup.All;

	[FormerlySerializedAs("mute")]
	[SerializeField]
	protected bool _mute;

	[SerializeField]
	protected bool _safeInvoke = true;

	[SerializeField]
	protected bool _localOnly;

	[NonSerialized]
	private int _refID;

	private OnSignalReceived _callbacks;

	protected static readonly Dictionary<ReceiverGroup, RaiseEventOptions> gGroupToOptions;

	protected static readonly SendOptions gSendReliable;

	protected static readonly SendOptions gSendUnreliable;

	public const byte EVENT_CODE = 177;

	public const int NULL_SIGNAL = 0;

	protected const int HEADER_SIZE = 2;

	public bool enabled => _enabled;

	public virtual int argCount => 0;

	public event OnSignalReceived OnSignal
	{
		add
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived)Delegate.Remove(_callbacks, value);
				_callbacks = (OnSignalReceived)Delegate.Combine(_callbacks, value);
			}
		}
		remove
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived)Delegate.Remove(_callbacks, value);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4, T5, T6, T7>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4, T5, T6>(OnSignalReceived<T1, T2, T3, T4, T5, T6> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4, T5>(OnSignalReceived<T1, T2, T3, T4, T5> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, arg5, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3, T4>(OnSignalReceived<T1, T2, T3, T4> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, arg4, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2, T3>(OnSignalReceived<T1, T2, T3> _event, T1 arg1, T2 arg2, T3 arg3, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, arg3, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1, T2>(OnSignalReceived<T1, T2> _event, T1 arg1, T2 arg2, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, arg2, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke<T1>(OnSignalReceived<T1> _event, T1 arg1, PhotonSignalInfo info)
	{
		_event?.Invoke(arg1, info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _Invoke(OnSignalReceived _event, PhotonSignalInfo info)
	{
		_event?.Invoke(info);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8, T9>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4, T5, T6, T7, T8>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4, T5, T6, T7>(OnSignalReceived<T1, T2, T3, T4, T5, T6, T7> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4, T5, T6, T7>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4, T5, T6>(OnSignalReceived<T1, T2, T3, T4, T5, T6> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4, T5, T6>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4, T5>(OnSignalReceived<T1, T2, T3, T4, T5> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4, T5>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, arg5, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3, T4>(OnSignalReceived<T1, T2, T3, T4> _event, T1 arg1, T2 arg2, T3 arg3, T4 arg4, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3, T4>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, arg4, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2, T3>(OnSignalReceived<T1, T2, T3> _event, T1 arg1, T2 arg2, T3 arg3, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2, T3>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, arg3, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1, T2>(OnSignalReceived<T1, T2> _event, T1 arg1, T2 arg2, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1, T2>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, arg2, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke<T1>(OnSignalReceived<T1> _event, T1 arg1, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived<T1>[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(arg1, info);
			}
			catch
			{
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void _SafeInvoke(OnSignalReceived _event, PhotonSignalInfo info)
	{
		ref readonly OnSignalReceived[] reference = ref PhotonUtils.FetchDelegatesNonAlloc(_event);
		for (int i = 0; i < reference.Length; i++)
		{
			try
			{
				reference[i]?.Invoke(info);
			}
			catch
			{
			}
		}
	}

	protected PhotonSignal()
	{
		_refID = RefID.Register(this);
	}

	public PhotonSignal(string signalID)
		: this()
	{
		signalID = signalID?.Trim();
		if (string.IsNullOrWhiteSpace(signalID))
		{
			throw new ArgumentNullException("signalID");
		}
		_signalID = XXHash32.Compute(signalID);
	}

	public PhotonSignal(int signalID)
		: this()
	{
		_signalID = signalID;
	}

	public void Raise()
	{
		Raise(_receivers);
	}

	public void Raise(ReceiverGroup receivers)
	{
		if (_enabled && !_mute)
		{
			RaiseEventOptions raiseEventOptions = gGroupToOptions[receivers];
			object[] array = PhotonUtils.FetchScratchArray(2);
			int serverTimestamp = PhotonNetwork.ServerTimestamp;
			array[0] = _signalID;
			array[1] = serverTimestamp;
			if (_localOnly || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			{
				PhotonSignalInfo info = new PhotonSignalInfo(PhotonUtils.LocalNetPlayer, serverTimestamp);
				_Relay(array, info);
			}
			else
			{
				PhotonNetwork.RaiseEvent(177, array, raiseEventOptions, gSendReliable);
			}
		}
	}

	public void Enable()
	{
		PhotonNetwork.NetworkingClient.EventReceived += _EventHandle;
		_enabled = true;
	}

	public void Disable()
	{
		_enabled = false;
		PhotonNetwork.NetworkingClient.EventReceived -= _EventHandle;
	}

	private void _EventHandle(EventData eventData)
	{
		if (_enabled && !_mute && eventData.Code == 177)
		{
			int sender = eventData.Sender;
			if (eventData.CustomData is object[] array && array.Length >= 2 + argCount && array[0] is int num && num != 0 && num == _signalID && array[1] is int timestamp)
			{
				NetPlayer netPlayer = PhotonUtils.GetNetPlayer(sender);
				PhotonSignalInfo info = new PhotonSignalInfo(netPlayer, timestamp);
				_Relay(array, info);
			}
		}
	}

	protected virtual void _Relay(object[] args, PhotonSignalInfo info)
	{
		if (!_safeInvoke)
		{
			_Invoke(_callbacks, info);
		}
		else
		{
			_SafeInvoke(_callbacks, info);
		}
	}

	public virtual void ClearListeners()
	{
		_callbacks = null;
	}

	public virtual void Reset()
	{
		ClearListeners();
		Disable();
	}

	public virtual void Dispose()
	{
		_signalID = 0;
		Reset();
	}

	~PhotonSignal()
	{
		Dispose();
	}

	public static implicit operator PhotonSignal(string s)
	{
		return new PhotonSignal(s);
	}

	public static explicit operator PhotonSignal(int i)
	{
		return new PhotonSignal(i);
	}

	static PhotonSignal()
	{
		gGroupToOptions = new Dictionary<ReceiverGroup, RaiseEventOptions>
		{
			[ReceiverGroup.Others] = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.Others
			},
			[ReceiverGroup.All] = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.All
			},
			[ReceiverGroup.MasterClient] = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.MasterClient
			}
		};
		gSendReliable = SendOptions.SendReliable;
		gSendUnreliable = SendOptions.SendUnreliable;
		gSendReliable.Encrypt = true;
		gSendUnreliable.Encrypt = true;
	}
}
[Serializable]
public class PhotonSignal<T1> : PhotonSignal
{
	private OnSignalReceived<T1> _callbacks;

	private static readonly int kSignature = typeof(PhotonSignal<T1>).FullName.GetStaticHash();

	public override int argCount => 1;

	public new event OnSignalReceived<T1> OnSignal
	{
		add
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1>)Delegate.Remove(_callbacks, value);
				_callbacks = (OnSignalReceived<T1>)Delegate.Combine(_callbacks, value);
			}
		}
		remove
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1>)Delegate.Remove(_callbacks, value);
			}
		}
	}

	public PhotonSignal(string signalID)
		: base(signalID)
	{
	}

	public PhotonSignal(int signalID)
		: base(signalID)
	{
	}

	public override void ClearListeners()
	{
		_callbacks = null;
		base.ClearListeners();
	}

	public void Raise(T1 arg1)
	{
		Raise(_receivers, arg1);
	}

	public void Raise(ReceiverGroup receivers, T1 arg1)
	{
		if (_enabled && !_mute)
		{
			RaiseEventOptions raiseEventOptions = PhotonSignal.gGroupToOptions[receivers];
			object[] array = PhotonUtils.FetchScratchArray(2 + argCount);
			int serverTimestamp = PhotonNetwork.ServerTimestamp;
			array[0] = _signalID;
			array[1] = serverTimestamp;
			array[2] = arg1;
			if (_localOnly || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			{
				PhotonSignalInfo info = new PhotonSignalInfo(PhotonUtils.LocalNetPlayer, serverTimestamp);
				_Relay(array, info);
			}
			else
			{
				PhotonNetwork.RaiseEvent(177, array, raiseEventOptions, PhotonSignal.gSendReliable);
			}
		}
	}

	protected override void _Relay(object[] args, PhotonSignalInfo info)
	{
		if (args.TryParseArgs<T1>(2, out var arg))
		{
			if (!_safeInvoke)
			{
				PhotonSignal._Invoke(_callbacks, arg, info);
			}
			else
			{
				PhotonSignal._SafeInvoke(_callbacks, arg, info);
			}
		}
	}

	public static implicit operator PhotonSignal<T1>(string s)
	{
		return new PhotonSignal<T1>(s);
	}

	public static explicit operator PhotonSignal<T1>(int i)
	{
		return new PhotonSignal<T1>(i);
	}
}
[Serializable]
public class PhotonSignal<T1, T2> : PhotonSignal
{
	private OnSignalReceived<T1, T2> _callbacks;

	private static readonly int kSignature = typeof(PhotonSignal<T1, T2>).FullName.GetStaticHash();

	public override int argCount => 2;

	public new event OnSignalReceived<T1, T2> OnSignal
	{
		add
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1, T2>)Delegate.Remove(_callbacks, value);
				_callbacks = (OnSignalReceived<T1, T2>)Delegate.Combine(_callbacks, value);
			}
		}
		remove
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1, T2>)Delegate.Remove(_callbacks, value);
			}
		}
	}

	public PhotonSignal(string signalID)
		: base(signalID)
	{
	}

	public PhotonSignal(int signalID)
		: base(signalID)
	{
	}

	public override void ClearListeners()
	{
		_callbacks = null;
		base.ClearListeners();
	}

	public void Raise(T1 arg1, T2 arg2)
	{
		Raise(_receivers, arg1, arg2);
	}

	public void Raise(ReceiverGroup receivers, T1 arg1, T2 arg2)
	{
		if (_enabled && !_mute)
		{
			RaiseEventOptions raiseEventOptions = PhotonSignal.gGroupToOptions[receivers];
			object[] array = PhotonUtils.FetchScratchArray(2 + argCount);
			int serverTimestamp = PhotonNetwork.ServerTimestamp;
			array[0] = _signalID;
			array[1] = serverTimestamp;
			array[2] = arg1;
			array[3] = arg2;
			if (_localOnly || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			{
				PhotonSignalInfo info = new PhotonSignalInfo(PhotonUtils.LocalNetPlayer, serverTimestamp);
				_Relay(array, info);
			}
			else
			{
				PhotonNetwork.RaiseEvent(177, array, raiseEventOptions, PhotonSignal.gSendReliable);
			}
		}
	}

	protected override void _Relay(object[] args, PhotonSignalInfo info)
	{
		if (args.TryParseArgs<T1, T2>(2, out var arg, out var arg2))
		{
			if (!_safeInvoke)
			{
				PhotonSignal._Invoke(_callbacks, arg, arg2, info);
			}
			else
			{
				PhotonSignal._SafeInvoke(_callbacks, arg, arg2, info);
			}
		}
	}

	public static implicit operator PhotonSignal<T1, T2>(string s)
	{
		return new PhotonSignal<T1, T2>(s);
	}

	public static explicit operator PhotonSignal<T1, T2>(int i)
	{
		return new PhotonSignal<T1, T2>(i);
	}
}
[Serializable]
public class PhotonSignal<T1, T2, T3> : PhotonSignal
{
	private OnSignalReceived<T1, T2, T3> _callbacks;

	private static readonly int kSignature = typeof(PhotonSignal<T1, T2, T3>).FullName.GetStaticHash();

	public override int argCount => 3;

	public new event OnSignalReceived<T1, T2, T3> OnSignal
	{
		add
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1, T2, T3>)Delegate.Remove(_callbacks, value);
				_callbacks = (OnSignalReceived<T1, T2, T3>)Delegate.Combine(_callbacks, value);
			}
		}
		remove
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1, T2, T3>)Delegate.Remove(_callbacks, value);
			}
		}
	}

	public PhotonSignal(string signalID)
		: base(signalID)
	{
	}

	public PhotonSignal(int signalID)
		: base(signalID)
	{
	}

	public override void ClearListeners()
	{
		_callbacks = null;
		base.ClearListeners();
	}

	public void Raise(T1 arg1, T2 arg2, T3 arg3)
	{
		Raise(_receivers, arg1, arg2, arg3);
	}

	public void Raise(ReceiverGroup receivers, T1 arg1, T2 arg2, T3 arg3)
	{
		if (_enabled && !_mute)
		{
			RaiseEventOptions raiseEventOptions = PhotonSignal.gGroupToOptions[receivers];
			object[] array = PhotonUtils.FetchScratchArray(2 + argCount);
			int serverTimestamp = PhotonNetwork.ServerTimestamp;
			array[0] = _signalID;
			array[1] = serverTimestamp;
			array[2] = arg1;
			array[3] = arg2;
			array[4] = arg3;
			if (_localOnly || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			{
				PhotonSignalInfo info = new PhotonSignalInfo(PhotonUtils.LocalNetPlayer, serverTimestamp);
				_Relay(array, info);
			}
			else
			{
				PhotonNetwork.RaiseEvent(177, array, raiseEventOptions, PhotonSignal.gSendReliable);
			}
		}
	}

	protected override void _Relay(object[] args, PhotonSignalInfo info)
	{
		if (args.TryParseArgs<T1, T2, T3>(2, out var arg, out var arg2, out var arg3))
		{
			if (!_safeInvoke)
			{
				PhotonSignal._Invoke(_callbacks, arg, arg2, arg3, info);
			}
			else
			{
				PhotonSignal._SafeInvoke(_callbacks, arg, arg2, arg3, info);
			}
		}
	}

	public static implicit operator PhotonSignal<T1, T2, T3>(string s)
	{
		return new PhotonSignal<T1, T2, T3>(s);
	}

	public static explicit operator PhotonSignal<T1, T2, T3>(int i)
	{
		return new PhotonSignal<T1, T2, T3>(i);
	}
}
[Serializable]
public class PhotonSignal<T1, T2, T3, T4> : PhotonSignal
{
	private OnSignalReceived<T1, T2, T3, T4> _callbacks;

	private static readonly int kSignature = typeof(PhotonSignal<T1, T2, T3, T4>).FullName.GetStaticHash();

	public override int argCount => 4;

	public new event OnSignalReceived<T1, T2, T3, T4> OnSignal
	{
		add
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1, T2, T3, T4>)Delegate.Remove(_callbacks, value);
				_callbacks = (OnSignalReceived<T1, T2, T3, T4>)Delegate.Combine(_callbacks, value);
			}
		}
		remove
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1, T2, T3, T4>)Delegate.Remove(_callbacks, value);
			}
		}
	}

	public PhotonSignal(string signalID)
		: base(signalID)
	{
	}

	public PhotonSignal(int signalID)
		: base(signalID)
	{
	}

	public override void ClearListeners()
	{
		_callbacks = null;
		base.ClearListeners();
	}

	public void Raise(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		Raise(_receivers, arg1, arg2, arg3, arg4);
	}

	public void Raise(ReceiverGroup receivers, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		if (_enabled && !_mute)
		{
			RaiseEventOptions raiseEventOptions = PhotonSignal.gGroupToOptions[receivers];
			object[] array = PhotonUtils.FetchScratchArray(2 + argCount);
			int serverTimestamp = PhotonNetwork.ServerTimestamp;
			array[0] = _signalID;
			array[1] = serverTimestamp;
			array[2] = arg1;
			array[3] = arg2;
			array[4] = arg3;
			array[5] = arg4;
			if (_localOnly || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			{
				PhotonSignalInfo info = new PhotonSignalInfo(PhotonUtils.LocalNetPlayer, serverTimestamp);
				_Relay(array, info);
			}
			else
			{
				PhotonNetwork.RaiseEvent(177, array, raiseEventOptions, PhotonSignal.gSendReliable);
			}
		}
	}

	protected override void _Relay(object[] args, PhotonSignalInfo info)
	{
		if (args.TryParseArgs<T1, T2, T3, T4>(2, out var arg, out var arg2, out var arg3, out var arg4))
		{
			if (!_safeInvoke)
			{
				PhotonSignal._Invoke(_callbacks, arg, arg2, arg3, arg4, info);
			}
			else
			{
				PhotonSignal._SafeInvoke(_callbacks, arg, arg2, arg3, arg4, info);
			}
		}
	}

	public static implicit operator PhotonSignal<T1, T2, T3, T4>(string s)
	{
		return new PhotonSignal<T1, T2, T3, T4>(s);
	}

	public static explicit operator PhotonSignal<T1, T2, T3, T4>(int i)
	{
		return new PhotonSignal<T1, T2, T3, T4>(i);
	}
}
[Serializable]
public class PhotonSignal<T1, T2, T3, T4, T5> : PhotonSignal
{
	private OnSignalReceived<T1, T2, T3, T4, T5> _callbacks;

	private static readonly int kSignature = typeof(PhotonSignal<T1, T2, T3, T4, T5>).FullName.GetStaticHash();

	public override int argCount => 5;

	public new event OnSignalReceived<T1, T2, T3, T4, T5> OnSignal
	{
		add
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1, T2, T3, T4, T5>)Delegate.Remove(_callbacks, value);
				_callbacks = (OnSignalReceived<T1, T2, T3, T4, T5>)Delegate.Combine(_callbacks, value);
			}
		}
		remove
		{
			if (value != null)
			{
				_callbacks = (OnSignalReceived<T1, T2, T3, T4, T5>)Delegate.Remove(_callbacks, value);
			}
		}
	}

	public PhotonSignal(string signalID)
		: base(signalID)
	{
	}

	public PhotonSignal(int signalID)
		: base(signalID)
	{
	}

	public override void ClearListeners()
	{
		_callbacks = null;
		base.ClearListeners();
	}

	public void Raise(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
	{
		Raise(_receivers, arg1, arg2, arg3, arg4, arg5);
	}

	public void Raise(ReceiverGroup receivers, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
	{
		if (_enabled && !_mute)
		{
			RaiseEventOptions raiseEventOptions = PhotonSignal.gGroupToOptions[receivers];
			object[] array = PhotonUtils.FetchScratchArray(2 + argCount);
			int serverTimestamp = PhotonNetwork.ServerTimestamp;
			array[0] = _signalID;
			array[1] = serverTimestamp;
			array[2] = arg1;
			array[3] = arg2;
			array[4] = arg3;
			array[5] = arg4;
			array[6] = arg5;
			if (_localOnly || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			{
				PhotonSignalInfo info = new PhotonSignalInfo(PhotonUtils.LocalNetPlayer, serverTimestamp);
				_Relay(array, info);
			}
			else
			{
				PhotonNetwork.RaiseEvent(177, array, raiseEventOptions, PhotonSignal.gSendReliable);
			}
		}
	}

	protected override void _Relay(object[] args, PhotonSignalInfo info)
	{
		if (args.TryParseArgs<T1, T2, T3, T4, T5>(2, out var arg, out var arg2, out var arg3, out var arg4, out var arg5))
		{
			if (!_safeInvoke)
			{
				PhotonSignal._Invoke(_callbacks, arg, arg2, arg3, arg4, arg5, info);
			}
			else
			{
				PhotonSignal._SafeInvoke(_callbacks, arg, arg2, arg3, arg4, arg5, info);
			}
		}
	}

	public static implicit operator PhotonSignal<T1, T2, T3, T4, T5>(string s)
	{
		return new PhotonSignal<T1, T2, T3, T4, T5>(s);
	}

	public static explicit operator PhotonSignal<T1, T2, T3, T4, T5>(int i)
	{
		return new PhotonSignal<T1, T2, T3, T4, T5>(i);
	}
}
