using System;
using AOT;
using Viveport.Internal;

namespace Viveport;

internal class Token
{
	private static Viveport.Internal.StatusCallback isReadyIl2cppCallback;

	private static Viveport.Internal.StatusCallback2 getSessionTokenIl2cppCallback;

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void IsReadyIl2cppCallback(int errorCode)
	{
		isReadyIl2cppCallback(errorCode);
	}

	public static void IsReady(StatusCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		isReadyIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(IsReadyIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Token.IsReady_64(IsReadyIl2cppCallback);
		}
		else
		{
			Viveport.Internal.Token.IsReady(IsReadyIl2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback2))]
	private static void GetSessionTokenIl2cppCallback(int errorCode, string message)
	{
		getSessionTokenIl2cppCallback(errorCode, message);
	}

	public static void GetSessionToken(StatusCallback2 callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		getSessionTokenIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallback2s.Add(GetSessionTokenIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Token.GetSessionToken_64(GetSessionTokenIl2cppCallback);
		}
		else
		{
			Viveport.Internal.Token.GetSessionToken(GetSessionTokenIl2cppCallback);
		}
	}
}
