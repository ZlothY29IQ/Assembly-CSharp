using System;
using System.Text;
using AOT;
using Viveport.Internal;

namespace Viveport;

public class User
{
	private static Viveport.Internal.StatusCallback isReadyIl2cppCallback;

	private const int MaxIdLength = 256;

	private const int MaxNameLength = 256;

	private const int MaxUrlLength = 512;

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void IsReadyIl2cppCallback(int errorCode)
	{
		isReadyIl2cppCallback(errorCode);
	}

	public static int IsReady(StatusCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		isReadyIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(IsReadyIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.User.IsReady_64(IsReadyIl2cppCallback);
		}
		return Viveport.Internal.User.IsReady(IsReadyIl2cppCallback);
	}

	public static string GetUserId()
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.User.GetUserID_64(stringBuilder, 256);
		}
		else
		{
			Viveport.Internal.User.GetUserID(stringBuilder, 256);
		}
		return stringBuilder.ToString();
	}

	public static string GetUserName()
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.User.GetUserName_64(stringBuilder, 256);
		}
		else
		{
			Viveport.Internal.User.GetUserName(stringBuilder, 256);
		}
		return stringBuilder.ToString();
	}

	public static string GetUserAvatarUrl()
	{
		StringBuilder stringBuilder = new StringBuilder(512);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.User.GetUserAvatarUrl_64(stringBuilder, 512);
		}
		else
		{
			Viveport.Internal.User.GetUserAvatarUrl(stringBuilder, 512);
		}
		return stringBuilder.ToString();
	}
}
