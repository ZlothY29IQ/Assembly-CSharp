using System;
using System.Text;
using AOT;
using Viveport.Internal;

namespace Viveport;

public class DLC
{
	private static Viveport.Internal.StatusCallback isDlcReadyIl2cppCallback;

	private const int AppIdLength = 37;

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void IsDlcReadyIl2cppCallback(int errorCode)
	{
		isDlcReadyIl2cppCallback(errorCode);
	}

	public static int IsDlcReady(StatusCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		isDlcReadyIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallbacks.Add(IsDlcReadyIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.DLC.IsReady_64(IsDlcReadyIl2cppCallback);
		}
		return Viveport.Internal.DLC.IsReady(IsDlcReadyIl2cppCallback);
	}

	public static int GetCount()
	{
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.DLC.GetCount_64();
		}
		return Viveport.Internal.DLC.GetCount();
	}

	public static bool GetIsAvailable(int index, out string appId, out bool isAvailable)
	{
		bool flag = false;
		StringBuilder stringBuilder = new StringBuilder(37);
		flag = ((IntPtr.Size != 8) ? Viveport.Internal.DLC.GetIsAvailable(index, stringBuilder, out isAvailable) : Viveport.Internal.DLC.GetIsAvailable_64(index, stringBuilder, out isAvailable));
		appId = stringBuilder.ToString();
		return flag;
	}

	public static int IsSubscribed()
	{
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.DLC.IsSubscribed_64();
		}
		return Viveport.Internal.DLC.IsSubscribed();
	}
}
