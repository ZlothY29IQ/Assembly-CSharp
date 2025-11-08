using System;
using System.Text;
using AOT;
using Viveport.Internal;

namespace Viveport;

public class Deeplink
{
	private const int MaxIdLength = 256;

	private static Viveport.Internal.StatusCallback isReadyIl2cppCallback;

	private static Viveport.Internal.StatusCallback2 goToAppIl2cppCallback;

	private static Viveport.Internal.StatusCallback2 goToAppWithBranchNameIl2cppCallback;

	private static Viveport.Internal.StatusCallback2 goToStoreIl2cppCallback;

	private static Viveport.Internal.StatusCallback2 goToAppOrGoToStoreIl2cppCallback;

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
			Viveport.Internal.Deeplink.IsReady_64(IsReadyIl2cppCallback);
		}
		else
		{
			Viveport.Internal.Deeplink.IsReady(IsReadyIl2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback2))]
	private static void GoToAppIl2cppCallback(int errorCode, string message)
	{
		goToAppIl2cppCallback(errorCode, message);
	}

	public static void GoToApp(StatusCallback2 callback, string viveportId, string launchData)
	{
		if (callback == null || string.IsNullOrEmpty(viveportId))
		{
			throw new InvalidOperationException("callback == null || string.IsNullOrEmpty(viveportId)");
		}
		goToAppIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallback2s.Add(GoToAppIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Deeplink.GoToApp_64(GoToAppIl2cppCallback, viveportId, launchData);
		}
		else
		{
			Viveport.Internal.Deeplink.GoToApp(GoToAppIl2cppCallback, viveportId, launchData);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback2))]
	private static void GoToAppWithBranchNameIl2cppCallback(int errorCode, string message)
	{
		goToAppWithBranchNameIl2cppCallback(errorCode, message);
	}

	public static void GoToApp(StatusCallback2 callback, string viveportId, string launchData, string branchName)
	{
		if (callback == null || string.IsNullOrEmpty(viveportId))
		{
			throw new InvalidOperationException("callback == null || string.IsNullOrEmpty(viveportId)");
		}
		goToAppWithBranchNameIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallback2s.Add(GoToAppWithBranchNameIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Deeplink.GoToApp_64(GoToAppWithBranchNameIl2cppCallback, viveportId, launchData, branchName);
		}
		else
		{
			Viveport.Internal.Deeplink.GoToApp(GoToAppWithBranchNameIl2cppCallback, viveportId, launchData, branchName);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback2))]
	private static void GoToStoreIl2cppCallback(int errorCode, string message)
	{
		goToStoreIl2cppCallback(errorCode, message);
	}

	public static void GoToStore(StatusCallback2 callback, string viveportId = "")
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null || string.IsNullOrEmpty(viveportId)");
		}
		goToStoreIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallback2s.Add(GoToStoreIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Deeplink.GoToStore_64(GoToStoreIl2cppCallback, viveportId);
		}
		else
		{
			Viveport.Internal.Deeplink.GoToStore(GoToStoreIl2cppCallback, viveportId);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback2))]
	private static void GoToAppOrGoToStoreIl2cppCallback(int errorCode, string message)
	{
		goToAppOrGoToStoreIl2cppCallback(errorCode, message);
	}

	public static void GoToAppOrGoToStore(StatusCallback2 callback, string viveportId, string launchData)
	{
		if (callback == null || string.IsNullOrEmpty(viveportId))
		{
			throw new InvalidOperationException("callback == null || string.IsNullOrEmpty(viveportId)");
		}
		goToAppOrGoToStoreIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallback2s.Add(GoToAppOrGoToStoreIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Deeplink.GoToAppOrGoToStore_64(GoToAppOrGoToStoreIl2cppCallback, viveportId, launchData);
		}
		else
		{
			Viveport.Internal.Deeplink.GoToAppOrGoToStore(GoToAppOrGoToStoreIl2cppCallback, viveportId, launchData);
		}
	}

	public static string GetAppLaunchData()
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Deeplink.GetAppLaunchData_64(stringBuilder, 256);
		}
		else
		{
			Viveport.Internal.Deeplink.GetAppLaunchData(stringBuilder, 256);
		}
		return stringBuilder.ToString();
	}
}
