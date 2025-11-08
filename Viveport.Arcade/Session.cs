using System;
using System.Runtime.InteropServices;
using AOT;
using LitJson;
using Viveport.Core;
using Viveport.Internal.Arcade;

namespace Viveport.Arcade;

internal class Session
{
	private class SessionHandler : BaseHandler
	{
		private static SessionListener listener;

		public SessionHandler(SessionListener cb)
		{
			listener = cb;
		}

		public SessionCallback getIsReadyHandler()
		{
			return IsReadyHandler;
		}

		protected override void IsReadyHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[Session IsReadyHandler] message=" + message + ",code=" + code);
			JsonData jsonData = null;
			try
			{
				jsonData = JsonMapper.ToObject(message);
			}
			catch (Exception ex)
			{
				Logger.Log("[Session IsReadyHandler] exception=" + ex);
			}
			int num = -1;
			string text = "";
			string text2 = "";
			if (code == 0 && jsonData != null)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text = (string)jsonData["message"];
				}
				catch (Exception ex2)
				{
					Logger.Log("[IsReadyHandler] statusCode, message ex=" + ex2);
				}
				Logger.Log("[IsReadyHandler] statusCode =" + num + ",errMessage=" + text);
				if (num == 0)
				{
					try
					{
						text2 = (string)jsonData["appID"];
					}
					catch (Exception ex3)
					{
						Logger.Log("[IsReadyHandler] appID ex=" + ex3);
					}
					Logger.Log("[IsReadyHandler] appID=" + text2);
				}
			}
			if (listener == null)
			{
				return;
			}
			if (code == 0)
			{
				if (num == 0)
				{
					listener.OnSuccess(text2);
				}
				else
				{
					listener.OnFailure(num, text);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public SessionCallback getStartHandler()
		{
			return StartHandler;
		}

		protected override void StartHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[Session StartHandler] message=" + message + ",code=" + code);
			JsonData jsonData = null;
			try
			{
				jsonData = JsonMapper.ToObject(message);
			}
			catch (Exception ex)
			{
				Logger.Log("[Session StartHandler] exception=" + ex);
			}
			int num = -1;
			string text = "";
			string text2 = "";
			string text3 = "";
			if (code == 0 && jsonData != null)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text = (string)jsonData["message"];
				}
				catch (Exception ex2)
				{
					Logger.Log("[StartHandler] statusCode, message ex=" + ex2);
				}
				Logger.Log("[StartHandler] statusCode =" + num + ",errMessage=" + text);
				if (num == 0)
				{
					try
					{
						text2 = (string)jsonData["appID"];
						text3 = (string)jsonData["Guid"];
					}
					catch (Exception ex3)
					{
						Logger.Log("[StartHandler] appID, Guid ex=" + ex3);
					}
					Logger.Log("[StartHandler] appID=" + text2 + ",Guid=" + text3);
				}
			}
			if (listener == null)
			{
				return;
			}
			if (code == 0)
			{
				if (num == 0)
				{
					listener.OnStartSuccess(text2, text3);
				}
				else
				{
					listener.OnFailure(num, text);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public SessionCallback getStopHandler()
		{
			return StopHandler;
		}

		protected override void StopHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[Session StopHandler] message=" + message + ",code=" + code);
			JsonData jsonData = null;
			try
			{
				jsonData = JsonMapper.ToObject(message);
			}
			catch (Exception ex)
			{
				Logger.Log("[Session StopHandler] exception=" + ex);
			}
			int num = -1;
			string text = "";
			string text2 = "";
			string text3 = "";
			if (code == 0 && jsonData != null)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text = (string)jsonData["message"];
				}
				catch (Exception ex2)
				{
					Logger.Log("[StopHandler] statusCode, message ex=" + ex2);
				}
				Logger.Log("[StopHandler] statusCode =" + num + ",errMessage=" + text);
				if (num == 0)
				{
					try
					{
						text2 = (string)jsonData["appID"];
						text3 = (string)jsonData["Guid"];
					}
					catch (Exception ex3)
					{
						Logger.Log("[StopHandler] appID, Guid ex=" + ex3);
					}
					Logger.Log("[StopHandler] appID=" + text2 + ",Guid=" + text3);
				}
			}
			if (listener == null)
			{
				return;
			}
			if (code == 0)
			{
				if (num == 0)
				{
					listener.OnStopSuccess(text2, text3);
				}
				else
				{
					listener.OnFailure(num, text);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}
	}

	private abstract class BaseHandler
	{
		protected abstract void IsReadyHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void StartHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void StopHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
	}

	public class SessionListener
	{
		public virtual void OnSuccess(string pchAppID)
		{
		}

		public virtual void OnStartSuccess(string pchAppID, string pchGuid)
		{
		}

		public virtual void OnStopSuccess(string pchAppID, string pchGuid)
		{
		}

		public virtual void OnFailure(int nCode, string pchMessage)
		{
		}
	}

	private static SessionCallback isReadyIl2cppCallback;

	private static SessionCallback startIl2cppCallback;

	private static SessionCallback stopIl2cppCallback;

	[MonoPInvokeCallback(typeof(SessionCallback))]
	private static void IsReadyIl2cppCallback(int errorCode, string message)
	{
		isReadyIl2cppCallback(errorCode, message);
	}

	public static void IsReady(SessionListener listener)
	{
		isReadyIl2cppCallback = new SessionHandler(listener).getIsReadyHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Arcade.Session.IsReady_64(IsReadyIl2cppCallback);
		}
		else
		{
			Viveport.Internal.Arcade.Session.IsReady(IsReadyIl2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(SessionCallback))]
	private static void StartIl2cppCallback(int errorCode, string message)
	{
		startIl2cppCallback(errorCode, message);
	}

	public static void Start(SessionListener listener)
	{
		startIl2cppCallback = new SessionHandler(listener).getStartHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Arcade.Session.Start_64(StartIl2cppCallback);
		}
		else
		{
			Viveport.Internal.Arcade.Session.Start(StartIl2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(SessionCallback))]
	private static void StopIl2cppCallback(int errorCode, string message)
	{
		stopIl2cppCallback(errorCode, message);
	}

	public static void Stop(SessionListener listener)
	{
		stopIl2cppCallback = new SessionHandler(listener).getStopHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Arcade.Session.Stop_64(StopIl2cppCallback);
		}
		else
		{
			Viveport.Internal.Arcade.Session.Stop(StopIl2cppCallback);
		}
	}
}
