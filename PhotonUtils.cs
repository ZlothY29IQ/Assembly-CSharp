using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExitGames.Client.Photon;
using UnityEngine;

public static class PhotonUtils
{
	private static class EmptyArray<T>
	{
		private static readonly T[] gEmpty = Array.Empty<T>();

		public static ref readonly T[] Ref()
		{
			return ref gEmpty;
		}
	}

	public static class CustomTypes
	{
		private const short LEN_C32 = 4;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void InitOnLoad()
		{
			PhotonPeer.RegisterType(typeof(Color32), 67, SerializeColor32, DeserializeColor32);
		}

		public static byte[] SerializeColor32(object value)
		{
			return CastToBytes((Color32)value);
		}

		public static object DeserializeColor32(byte[] data)
		{
			return CastToStruct<Color32>(data);
		}

		private static T CastToStruct<T>(byte[] bytes) where T : struct
		{
			GCHandle gCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			T result = Marshal.PtrToStructure<T>(gCHandle.AddrOfPinnedObject());
			gCHandle.Free();
			return result;
		}

		private static byte[] CastToBytes<T>(T data) where T : struct
		{
			byte[] array = new byte[Marshal.SizeOf<T>()];
			GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
			IntPtr ptr = gCHandle.AddrOfPinnedObject();
			Marshal.StructureToPtr(data, ptr, fDeleteOld: true);
			gCHandle.Free();
			return array;
		}
	}

	private static NetworkSystem gNetSystem;

	private static NetPlayer gLocalNetPlayer;

	private static readonly Dictionary<int, object[]> gLengthToArgsArray;

	private const int ARG_ARRAYS = 16;

	public static int LocalActorNumber => LocalNetPlayer?.ActorNumber ?? (-1);

	public static NetPlayer LocalNetPlayer
	{
		get
		{
			if (gLocalNetPlayer != null)
			{
				return gLocalNetPlayer;
			}
			if (TryGetNetSystem(out var ns))
			{
				gLocalNetPlayer = ns.GetLocalPlayer();
			}
			return gLocalNetPlayer;
		}
	}

	public static void ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10, out T11 arg11, out T12 arg12)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
		arg5 = (T5)args[startIndex + 4];
		arg6 = (T6)args[startIndex + 5];
		arg7 = (T7)args[startIndex + 6];
		arg8 = (T8)args[startIndex + 7];
		arg9 = (T9)args[startIndex + 8];
		arg10 = (T10)args[startIndex + 9];
		arg11 = (T11)args[startIndex + 10];
		arg12 = (T12)args[startIndex + 11];
	}

	public static void ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10, out T11 arg11)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
		arg5 = (T5)args[startIndex + 4];
		arg6 = (T6)args[startIndex + 5];
		arg7 = (T7)args[startIndex + 6];
		arg8 = (T8)args[startIndex + 7];
		arg9 = (T9)args[startIndex + 8];
		arg10 = (T10)args[startIndex + 9];
		arg11 = (T11)args[startIndex + 10];
	}

	public static void ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
		arg5 = (T5)args[startIndex + 4];
		arg6 = (T6)args[startIndex + 5];
		arg7 = (T7)args[startIndex + 6];
		arg8 = (T8)args[startIndex + 7];
		arg9 = (T9)args[startIndex + 8];
		arg10 = (T10)args[startIndex + 9];
	}

	public static void ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
		arg5 = (T5)args[startIndex + 4];
		arg6 = (T6)args[startIndex + 5];
		arg7 = (T7)args[startIndex + 6];
		arg8 = (T8)args[startIndex + 7];
		arg9 = (T9)args[startIndex + 8];
	}

	public static void ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
		arg5 = (T5)args[startIndex + 4];
		arg6 = (T6)args[startIndex + 5];
		arg7 = (T7)args[startIndex + 6];
		arg8 = (T8)args[startIndex + 7];
	}

	public static void ParseArgs<T1, T2, T3, T4, T5, T6, T7>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
		arg5 = (T5)args[startIndex + 4];
		arg6 = (T6)args[startIndex + 5];
		arg7 = (T7)args[startIndex + 6];
	}

	public static void ParseArgs<T1, T2, T3, T4, T5, T6>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
		arg5 = (T5)args[startIndex + 4];
		arg6 = (T6)args[startIndex + 5];
	}

	public static void ParseArgs<T1, T2, T3, T4, T5>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
		arg5 = (T5)args[startIndex + 4];
	}

	public static void ParseArgs<T1, T2, T3, T4>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
		arg4 = (T4)args[startIndex + 3];
	}

	public static void ParseArgs<T1, T2, T3>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
		arg3 = (T3)args[startIndex + 2];
	}

	public static void ParseArgs<T1, T2>(this object[] args, int startIndex, out T1 arg1, out T2 arg2)
	{
		arg1 = (T1)args[startIndex];
		arg2 = (T2)args[startIndex + 1];
	}

	public static void ParseArgs<T1>(this object[] args, int startIndex, out T1 arg1)
	{
		arg1 = (T1)args[startIndex];
	}

	public static bool TryParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10, out T11 arg11, out T12 arg12)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		arg5 = default(T5);
		arg6 = default(T6);
		arg7 = default(T7);
		arg8 = default(T8);
		arg9 = default(T9);
		arg10 = default(T10);
		arg11 = default(T11);
		arg12 = default(T12);
		if (args == null || args.Length < startIndex + 12)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(startIndex, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7, out arg8, out arg9, out arg10, out arg11, out arg12);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10, out T11 arg11)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		arg5 = default(T5);
		arg6 = default(T6);
		arg7 = default(T7);
		arg8 = default(T8);
		arg9 = default(T9);
		arg10 = default(T10);
		arg11 = default(T11);
		if (args == null || args.Length < startIndex + 11)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(startIndex, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7, out arg8, out arg9, out arg10, out arg11);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		arg5 = default(T5);
		arg6 = default(T6);
		arg7 = default(T7);
		arg8 = default(T8);
		arg9 = default(T9);
		arg10 = default(T10);
		if (args == null || args.Length < startIndex + 10)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(startIndex, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7, out arg8, out arg9, out arg10);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		arg5 = default(T5);
		arg6 = default(T6);
		arg7 = default(T7);
		arg8 = default(T8);
		arg9 = default(T9);
		if (args == null || args.Length < startIndex + 9)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8, T9>(startIndex, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7, out arg8, out arg9);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3, T4, T5, T6, T7, T8>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		arg5 = default(T5);
		arg6 = default(T6);
		arg7 = default(T7);
		arg8 = default(T8);
		if (args == null || args.Length < startIndex + 8)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4, T5, T6, T7, T8>(startIndex, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7, out arg8);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3, T4, T5, T6, T7>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		arg5 = default(T5);
		arg6 = default(T6);
		arg7 = default(T7);
		if (args == null || args.Length < startIndex + 7)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4, T5, T6, T7>(startIndex, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3, T4, T5, T6>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		arg5 = default(T5);
		arg6 = default(T6);
		if (args == null || args.Length < startIndex + 6)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4, T5, T6>(startIndex, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3, T4, T5>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		arg5 = default(T5);
		if (args == null || args.Length < startIndex + 5)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4, T5>(startIndex, out arg1, out arg2, out arg3, out arg4, out arg5);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3, T4>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		arg4 = default(T4);
		if (args == null || args.Length < startIndex + 4)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3, T4>(startIndex, out arg1, out arg2, out arg3, out arg4);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2, T3>(this object[] args, int startIndex, out T1 arg1, out T2 arg2, out T3 arg3)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		arg3 = default(T3);
		if (args == null || args.Length < startIndex + 3)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2, T3>(startIndex, out arg1, out arg2, out arg3);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1, T2>(this object[] args, int startIndex, out T1 arg1, out T2 arg2)
	{
		arg1 = default(T1);
		arg2 = default(T2);
		if (args == null || args.Length < startIndex + 2)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1, T2>(startIndex, out arg1, out arg2);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool TryParseArgs<T1>(this object[] args, int startIndex, out T1 arg1)
	{
		arg1 = default(T1);
		if (args == null || args.Length < startIndex + 1)
		{
			return false;
		}
		try
		{
			args.ParseArgs<T1>(startIndex, out arg1);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static ref readonly T[] FetchDelegatesNonAlloc<T>(T @delegate) where T : MulticastDelegate
	{
		if (@delegate == null)
		{
			return ref EmptyArray<T>.Ref();
		}
		return ref @delegate.GetInvocationListUnsafe();
	}

	public static object[] FetchScratchArray(int size)
	{
		if (size < 0)
		{
			throw new Exception("Size cannot be less than 0.");
		}
		if (!gLengthToArgsArray.TryGetValue(size, out var value))
		{
			value = new object[size];
			gLengthToArgsArray.Add(size, value);
		}
		return value;
	}

	public static NetPlayer GetNetPlayer(int actorNumber)
	{
		if (!TryGetNetSystem(out var ns))
		{
			return null;
		}
		return ns.GetPlayer(actorNumber);
	}

	private static bool TryGetNetSystem(out NetworkSystem ns)
	{
		if (!gNetSystem)
		{
			gNetSystem = NetworkSystem.Instance;
		}
		if (!gNetSystem)
		{
			ns = null;
			return false;
		}
		ns = gNetSystem;
		return true;
	}

	static PhotonUtils()
	{
		gLengthToArgsArray = new Dictionary<int, object[]>(16);
		for (int i = 0; i <= 16; i++)
		{
			gLengthToArgsArray.Add(i, new object[i]);
		}
	}
}
