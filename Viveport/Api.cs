using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using AOT;
using LitJson;
using PublicKeyConvert;
using Viveport.Core;
using Viveport.Internal;

namespace Viveport;

public class Api
{
	public abstract class LicenseChecker
	{
		public abstract void OnSuccess(long issueTime, long expirationTime, int latestVersion, bool updateRequired);

		public abstract void OnFailure(int errorCode, string errorMessage);
	}

	internal static readonly List<GetLicenseCallback> InternalGetLicenseCallbacks = new List<GetLicenseCallback>();

	internal static readonly List<Viveport.Internal.StatusCallback> InternalStatusCallbacks = new List<Viveport.Internal.StatusCallback>();

	internal static readonly List<Viveport.Internal.QueryRuntimeModeCallback> InternalQueryRunTimeCallbacks = new List<Viveport.Internal.QueryRuntimeModeCallback>();

	internal static readonly List<Viveport.Internal.StatusCallback2> InternalStatusCallback2s = new List<Viveport.Internal.StatusCallback2>();

	internal static readonly List<LicenseChecker> InternalLicenseCheckers = new List<LicenseChecker>();

	private static Viveport.Internal.StatusCallback initIl2cppCallback;

	private static Viveport.Internal.StatusCallback shutdownIl2cppCallback;

	private static Viveport.Internal.QueryRuntimeModeCallback queryRuntimeModeIl2cppCallback;

	private static readonly string VERSION = "1.7.2.30";

	private static string _appId = "";

	private static string _appKey = "";

	public static void GetLicense(LicenseChecker checker, string appId, string appKey)
	{
		if (checker == null || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey))
		{
			throw new InvalidOperationException("checker == null || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey)");
		}
		_appId = appId;
		_appKey = appKey;
		InternalLicenseCheckers.Add(checker);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Api.GetLicense_64(GetLicenseHandler, _appId, _appKey);
		}
		else
		{
			Viveport.Internal.Api.GetLicense(GetLicenseHandler, _appId, _appKey);
		}
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void InitIl2cppCallback(int errorCode)
	{
		initIl2cppCallback(errorCode);
	}

	public static int Init(StatusCallback callback, string appId)
	{
		if (callback == null || string.IsNullOrEmpty(appId))
		{
			throw new InvalidOperationException("callback == null || string.IsNullOrEmpty(appId)");
		}
		initIl2cppCallback = callback.Invoke;
		InternalStatusCallbacks.Add(InitIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.Api.Init_64(InitIl2cppCallback, appId);
		}
		return Viveport.Internal.Api.Init(InitIl2cppCallback, appId);
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback))]
	private static void ShutdownIl2cppCallback(int errorCode)
	{
		shutdownIl2cppCallback(errorCode);
	}

	public static int Shutdown(StatusCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		shutdownIl2cppCallback = callback.Invoke;
		InternalStatusCallbacks.Add(ShutdownIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			return Viveport.Internal.Api.Shutdown_64(ShutdownIl2cppCallback);
		}
		return Viveport.Internal.Api.Shutdown(ShutdownIl2cppCallback);
	}

	public static string Version()
	{
		string text = "";
		try
		{
			text = ((IntPtr.Size != 8) ? (text + Marshal.PtrToStringAnsi(Viveport.Internal.Api.Version())) : (text + Marshal.PtrToStringAnsi(Viveport.Internal.Api.Version_64())));
		}
		catch (Exception)
		{
			Logger.Log("Can not load version from native library");
		}
		return "C# version: " + VERSION + ", Native version: " + text;
	}

	[MonoPInvokeCallback(typeof(Viveport.Internal.QueryRuntimeModeCallback))]
	private static void QueryRuntimeModeIl2cppCallback(int errorCode, int mode)
	{
		queryRuntimeModeIl2cppCallback(errorCode, mode);
	}

	public static void QueryRuntimeMode(QueryRuntimeModeCallback callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		queryRuntimeModeIl2cppCallback = callback.Invoke;
		InternalQueryRunTimeCallbacks.Add(QueryRuntimeModeIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Api.QueryRuntimeMode_64(QueryRuntimeModeIl2cppCallback);
		}
		else
		{
			Viveport.Internal.Api.QueryRuntimeMode(QueryRuntimeModeIl2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(GetLicenseCallback))]
	private static void GetLicenseHandler([MarshalAs(UnmanagedType.LPStr)] string message, [MarshalAs(UnmanagedType.LPStr)] string signature)
	{
		if (string.IsNullOrEmpty(message))
		{
			for (int num = InternalLicenseCheckers.Count - 1; num >= 0; num--)
			{
				LicenseChecker licenseChecker = InternalLicenseCheckers[num];
				licenseChecker.OnFailure(90003, "License message is empty");
				InternalLicenseCheckers.Remove(licenseChecker);
			}
			return;
		}
		if (string.IsNullOrEmpty(signature))
		{
			JsonData jsonData = JsonMapper.ToObject(message);
			int errorCode = 99999;
			string errorMessage = "";
			try
			{
				errorCode = int.Parse((string)jsonData["code"]);
			}
			catch
			{
			}
			try
			{
				errorMessage = (string)jsonData["message"];
			}
			catch
			{
			}
			for (int num2 = InternalLicenseCheckers.Count - 1; num2 >= 0; num2--)
			{
				LicenseChecker licenseChecker2 = InternalLicenseCheckers[num2];
				licenseChecker2.OnFailure(errorCode, errorMessage);
				InternalLicenseCheckers.Remove(licenseChecker2);
			}
			return;
		}
		if (!VerifyMessage(_appId, _appKey, message, signature))
		{
			for (int num3 = InternalLicenseCheckers.Count - 1; num3 >= 0; num3--)
			{
				LicenseChecker licenseChecker3 = InternalLicenseCheckers[num3];
				licenseChecker3.OnFailure(90001, "License verification failed");
				InternalLicenseCheckers.Remove(licenseChecker3);
			}
			return;
		}
		string @string = Encoding.UTF8.GetString(Convert.FromBase64String(message.Substring(message.IndexOf("\n", StringComparison.Ordinal) + 1)));
		JsonData jsonData2 = JsonMapper.ToObject(@string);
		Logger.Log("License: " + @string);
		long issueTime = -1L;
		long expirationTime = -1L;
		int latestVersion = -1;
		bool updateRequired = false;
		try
		{
			issueTime = (long)jsonData2["issueTime"];
		}
		catch
		{
		}
		try
		{
			expirationTime = (long)jsonData2["expirationTime"];
		}
		catch
		{
		}
		try
		{
			latestVersion = (int)jsonData2["latestVersion"];
		}
		catch
		{
		}
		try
		{
			updateRequired = (bool)jsonData2["updateRequired"];
		}
		catch
		{
		}
		for (int num4 = InternalLicenseCheckers.Count - 1; num4 >= 0; num4--)
		{
			LicenseChecker licenseChecker4 = InternalLicenseCheckers[num4];
			licenseChecker4.OnSuccess(issueTime, expirationTime, latestVersion, updateRequired);
			InternalLicenseCheckers.Remove(licenseChecker4);
		}
	}

	private static bool VerifyMessage(string appId, string appKey, string message, string signature)
	{
		try
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = PEMKeyLoader.CryptoServiceProviderFromPublicKeyInfo(appKey);
			byte[] signature2 = Convert.FromBase64String(signature);
			SHA1Managed halg = new SHA1Managed();
			byte[] bytes = Encoding.UTF8.GetBytes(appId + "\n" + message);
			return rSACryptoServiceProvider.VerifyData(bytes, halg, signature2);
		}
		catch (Exception ex)
		{
			Logger.Log(ex.ToString());
		}
		return false;
	}
}
