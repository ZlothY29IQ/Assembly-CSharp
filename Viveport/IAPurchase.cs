using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using LitJson;
using Viveport.Core;
using Viveport.Internal;

namespace Viveport;

public class IAPurchase
{
	private class IAPHandler : BaseHandler
	{
		private static IAPurchaseListener listener;

		public IAPHandler(IAPurchaseListener cb)
		{
			listener = cb;
		}

		public IAPurchaseCallback getIsReadyHandler()
		{
			return IsReadyHandler;
		}

		protected override void IsReadyHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[IsReadyHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			string text2 = "";
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text2 = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[IsReadyHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[IsReadyHandler] statusCode =" + num + ",errMessage=" + text2);
				if (num == 0)
				{
					try
					{
						text = (string)jsonData["currencyName"];
					}
					catch (Exception ex2)
					{
						Logger.Log("[IsReadyHandler] currencyName ex=" + ex2);
					}
					Logger.Log("[IsReadyHandler] currencyName=" + text);
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
					listener.OnSuccess(text);
				}
				else
				{
					listener.OnFailure(num, text2);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public IAPurchaseCallback getRequestHandler()
		{
			return RequestHandler;
		}

		protected override void RequestHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[RequestHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			string text2 = "";
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text2 = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[RequestHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[RequestHandler] statusCode =" + num + ",errMessage=" + text2);
				if (num == 0)
				{
					try
					{
						text = (string)jsonData["purchase_id"];
					}
					catch (Exception ex2)
					{
						Logger.Log("[RequestHandler] purchase_id ex=" + ex2);
					}
					Logger.Log("[RequestHandler] purchaseId =" + text);
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
					listener.OnRequestSuccess(text);
				}
				else
				{
					listener.OnFailure(num, text2);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public IAPurchaseCallback getPurchaseHandler()
		{
			return PurchaseHandler;
		}

		protected override void PurchaseHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[PurchaseHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			string text2 = "";
			long num2 = 0L;
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text2 = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[PurchaseHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[PurchaseHandler] statusCode =" + num + ",errMessage=" + text2);
				if (num == 0)
				{
					try
					{
						text = (string)jsonData["purchase_id"];
						num2 = (long)jsonData["paid_timestamp"];
					}
					catch (Exception ex2)
					{
						Logger.Log("[PurchaseHandler] purchase_id,paid_timestamp ex=" + ex2);
					}
					Logger.Log("[PurchaseHandler] purchaseId =" + text + ",paid_timestamp=" + num2);
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
					listener.OnPurchaseSuccess(text);
				}
				else
				{
					listener.OnFailure(num, text2);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public IAPurchaseCallback getQueryHandler()
		{
			return QueryHandler;
		}

		protected override void QueryHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[QueryHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			string text2 = "";
			string text3 = "";
			string text4 = "";
			string text5 = "";
			string text6 = "";
			long paid_timestamp = 0L;
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text2 = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[QueryHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[QueryHandler] statusCode =" + num + ",errMessage=" + text2);
				if (num == 0)
				{
					try
					{
						text = (string)jsonData["purchase_id"];
						text3 = (string)jsonData["order_id"];
						text4 = (string)jsonData["status"];
						text5 = (string)jsonData["price"];
						text6 = (string)jsonData["currency"];
						paid_timestamp = (long)jsonData["paid_timestamp"];
					}
					catch (Exception ex2)
					{
						Logger.Log("[QueryHandler] purchase_id, order_id ex=" + ex2);
					}
					Logger.Log("[QueryHandler] status =" + text4 + ",price=" + text5 + ",currency=" + text6);
					Logger.Log("[QueryHandler] purchaseId =" + text + ",order_id=" + text3 + ",paid_timestamp=" + paid_timestamp);
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
					QueryResponse queryResponse = new QueryResponse();
					queryResponse.purchase_id = text;
					queryResponse.order_id = text3;
					queryResponse.price = text5;
					queryResponse.currency = text6;
					queryResponse.paid_timestamp = paid_timestamp;
					queryResponse.status = text4;
					listener.OnQuerySuccess(queryResponse);
				}
				else
				{
					listener.OnFailure(num, text2);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public IAPurchaseCallback getQueryListHandler()
		{
			return QueryListHandler;
		}

		protected override void QueryListHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[QueryListHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			int total = 0;
			int num2 = 0;
			int to = 0;
			List<QueryResponse2> list = new List<QueryResponse2>();
			string text = "";
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[QueryListHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[QueryListHandler] statusCode =" + num + ",errMessage=" + text);
				if (num == 0)
				{
					try
					{
						JsonData jsonData2 = JsonMapper.ToObject(text);
						total = (int)jsonData2["total"];
						num2 = (int)jsonData2["from"];
						to = (int)jsonData2["to"];
						JsonData jsonData3 = jsonData2["purchases"];
						_ = jsonData3.IsArray;
						foreach (JsonData item in (IEnumerable)jsonData3)
						{
							QueryResponse2 queryResponse = new QueryResponse2();
							IDictionary dictionary = item;
							queryResponse.app_id = (dictionary.Contains("app_id") ? ((string)item["app_id"]) : "");
							queryResponse.currency = (dictionary.Contains("currency") ? ((string)item["currency"]) : "");
							queryResponse.purchase_id = (dictionary.Contains("purchase_id") ? ((string)item["purchase_id"]) : "");
							queryResponse.order_id = (dictionary.Contains("order_id") ? ((string)item["order_id"]) : "");
							queryResponse.price = (dictionary.Contains("price") ? ((string)item["price"]) : "");
							queryResponse.user_data = (dictionary.Contains("user_data") ? ((string)item["user_data"]) : "");
							if (dictionary.Contains("paid_timestamp"))
							{
								if (item["paid_timestamp"].IsLong)
								{
									queryResponse.paid_timestamp = (long)item["paid_timestamp"];
								}
								else if (item["paid_timestamp"].IsInt)
								{
									queryResponse.paid_timestamp = (int)item["paid_timestamp"];
								}
							}
							list.Add(queryResponse);
						}
					}
					catch (Exception ex2)
					{
						Logger.Log("[QueryListHandler] purchase_id, order_id ex=" + ex2);
					}
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
					QueryListResponse queryListResponse = new QueryListResponse();
					queryListResponse.total = total;
					queryListResponse.from = num2;
					queryListResponse.to = to;
					queryListResponse.purchaseList = list;
					listener.OnQuerySuccess(queryListResponse);
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

		public IAPurchaseCallback getBalanceHandler()
		{
			return BalanceHandler;
		}

		protected override void BalanceHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[BalanceHandler] code=" + code + ",message= " + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			string text2 = "";
			string text3 = "";
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text3 = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[BalanceHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[BalanceHandler] statusCode =" + num + ",errMessage=" + text3);
				if (num == 0)
				{
					try
					{
						text = (string)jsonData["currencyName"];
						text2 = (string)jsonData["balance"];
					}
					catch (Exception ex2)
					{
						Logger.Log("[BalanceHandler] currencyName, balance ex=" + ex2);
					}
					Logger.Log("[BalanceHandler] currencyName=" + text + ",balance=" + text2);
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
					listener.OnBalanceSuccess(text2);
				}
				else
				{
					listener.OnFailure(num, text3);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public IAPurchaseCallback getRequestSubscriptionHandler()
		{
			return RequestSubscriptionHandler;
		}

		protected override void RequestSubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[RequestSubscriptionHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			string text2 = "";
			try
			{
				num = (int)jsonData["statusCode"];
				text2 = (string)jsonData["message"];
			}
			catch (Exception ex)
			{
				Logger.Log("[RequestSubscriptionHandler] statusCode, message ex=" + ex);
			}
			Logger.Log("[RequestSubscriptionHandler] statusCode =" + num + ",errMessage=" + text2);
			if (num == 0)
			{
				try
				{
					text = (string)jsonData["subscription_id"];
				}
				catch (Exception ex2)
				{
					Logger.Log("[RequestSubscriptionHandler] subscription_id ex=" + ex2);
				}
				Logger.Log("[RequestSubscriptionHandler] subscription_id =" + text);
			}
			if (listener == null)
			{
				return;
			}
			if (code == 0)
			{
				if (num == 0)
				{
					listener.OnRequestSubscriptionSuccess(text);
				}
				else
				{
					listener.OnFailure(num, text2);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public IAPurchaseCallback getRequestSubscriptionWithPlanIDHandler()
		{
			return RequestSubscriptionWithPlanIDHandler;
		}

		protected override void RequestSubscriptionWithPlanIDHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[RequestSubscriptionWithPlanIDHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			string text2 = "";
			try
			{
				num = (int)jsonData["statusCode"];
				text2 = (string)jsonData["message"];
			}
			catch (Exception ex)
			{
				Logger.Log("[RequestSubscriptionWithPlanIDHandler] statusCode, message ex=" + ex);
			}
			Logger.Log("[RequestSubscriptionWithPlanIDHandler] statusCode =" + num + ",errMessage=" + text2);
			if (num == 0)
			{
				try
				{
					text = (string)jsonData["subscription_id"];
				}
				catch (Exception ex2)
				{
					Logger.Log("[RequestSubscriptionWithPlanIDHandler] subscription_id ex=" + ex2);
				}
				Logger.Log("[RequestSubscriptionWithPlanIDHandler] subscription_id =" + text);
			}
			if (listener == null)
			{
				return;
			}
			if (code == 0)
			{
				if (num == 0)
				{
					listener.OnRequestSubscriptionWithPlanIDSuccess(text);
				}
				else
				{
					listener.OnFailure(num, text2);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public IAPurchaseCallback getSubscribeHandler()
		{
			return SubscribeHandler;
		}

		protected override void SubscribeHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[SubscribeHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			string text2 = "";
			string text3 = "";
			long num2 = 0L;
			try
			{
				num = (int)jsonData["statusCode"];
				text2 = (string)jsonData["message"];
			}
			catch (Exception ex)
			{
				Logger.Log("[SubscribeHandler] statusCode, message ex=" + ex);
			}
			Logger.Log("[SubscribeHandler] statusCode =" + num + ",errMessage=" + text2);
			if (num == 0)
			{
				try
				{
					text = (string)jsonData["subscription_id"];
					text3 = (string)jsonData["plan_id"];
					num2 = (long)jsonData["subscribed_timestamp"];
				}
				catch (Exception ex2)
				{
					Logger.Log("[SubscribeHandler] subscription_id, plan_id ex=" + ex2);
				}
				Logger.Log("[SubscribeHandler] subscription_id =" + text + ", plan_id=" + text3 + ", timestamp=" + num2);
			}
			if (listener == null)
			{
				return;
			}
			if (code == 0)
			{
				if (num == 0)
				{
					listener.OnSubscribeSuccess(text);
				}
				else
				{
					listener.OnFailure(num, text2);
				}
			}
			else
			{
				listener.OnFailure(code, message);
			}
		}

		public IAPurchaseCallback getQuerySubscriptionHandler()
		{
			return QuerySubscriptionHandler;
		}

		protected override void QuerySubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[QuerySubscriptionHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			List<Subscription> list = null;
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[QuerySubscriptionHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[QuerySubscriptionHandler] statusCode =" + num + ",errMessage=" + text);
				if (num == 0)
				{
					try
					{
						list = JsonMapper.ToObject<QuerySubscritionResponse>(message).subscriptions;
					}
					catch (Exception ex2)
					{
						Logger.Log("[QuerySubscriptionHandler] ex =" + ex2);
					}
				}
			}
			if (listener == null)
			{
				return;
			}
			if (code == 0)
			{
				if (num == 0 && list != null && list.Count > 0)
				{
					listener.OnQuerySubscriptionSuccess(list.ToArray());
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

		public IAPurchaseCallback getQuerySubscriptionListHandler()
		{
			return QuerySubscriptionListHandler;
		}

		protected override void QuerySubscriptionListHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[QuerySubscriptionListHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			string text = "";
			List<Subscription> list = null;
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[QuerySubscriptionListHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[QuerySubscriptionListHandler] statusCode =" + num + ",errMessage=" + text);
				if (num == 0)
				{
					try
					{
						list = JsonMapper.ToObject<QuerySubscritionResponse>(message).subscriptions;
					}
					catch (Exception ex2)
					{
						Logger.Log("[QuerySubscriptionListHandler] ex =" + ex2);
					}
				}
			}
			if (listener == null)
			{
				return;
			}
			if (code == 0)
			{
				if (num == 0 && list != null && list.Count > 0)
				{
					listener.OnQuerySubscriptionListSuccess(list.ToArray());
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

		public IAPurchaseCallback getCancelSubscriptionHandler()
		{
			return CancelSubscriptionHandler;
		}

		protected override void CancelSubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message)
		{
			Logger.Log("[CancelSubscriptionHandler] message=" + message);
			JsonData jsonData = JsonMapper.ToObject(message);
			int num = -1;
			bool bCanceled = false;
			string text = "";
			if (code == 0)
			{
				try
				{
					num = (int)jsonData["statusCode"];
					text = (string)jsonData["message"];
				}
				catch (Exception ex)
				{
					Logger.Log("[CancelSubscriptionHandler] statusCode, message ex=" + ex);
				}
				Logger.Log("[CancelSubscriptionHandler] statusCode =" + num + ",errMessage=" + text);
				if (num == 0)
				{
					bCanceled = true;
					Logger.Log("[CancelSubscriptionHandler] isCanceled = " + bCanceled);
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
					listener.OnCancelSubscriptionSuccess(bCanceled);
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

		protected abstract void RequestHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void PurchaseHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void QueryHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void QueryListHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void BalanceHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void RequestSubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void RequestSubscriptionWithPlanIDHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void SubscribeHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void QuerySubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void QuerySubscriptionListHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);

		protected abstract void CancelSubscriptionHandler(int code, [MarshalAs(UnmanagedType.LPStr)] string message);
	}

	public class IAPurchaseListener
	{
		public virtual void OnSuccess(string pchCurrencyName)
		{
		}

		public virtual void OnRequestSuccess(string pchPurchaseId)
		{
		}

		public virtual void OnPurchaseSuccess(string pchPurchaseId)
		{
		}

		public virtual void OnQuerySuccess(QueryResponse response)
		{
		}

		public virtual void OnQuerySuccess(QueryListResponse response)
		{
		}

		public virtual void OnBalanceSuccess(string pchBalance)
		{
		}

		public virtual void OnFailure(int nCode, string pchMessage)
		{
		}

		public virtual void OnRequestSubscriptionSuccess(string pchSubscriptionId)
		{
		}

		public virtual void OnRequestSubscriptionWithPlanIDSuccess(string pchSubscriptionId)
		{
		}

		public virtual void OnSubscribeSuccess(string pchSubscriptionId)
		{
		}

		public virtual void OnQuerySubscriptionSuccess(Subscription[] subscriptionlist)
		{
		}

		public virtual void OnQuerySubscriptionListSuccess(Subscription[] subscriptionlist)
		{
		}

		public virtual void OnCancelSubscriptionSuccess(bool bCanceled)
		{
		}
	}

	public class QueryResponse
	{
		public string order_id { get; set; }

		public string purchase_id { get; set; }

		public string status { get; set; }

		public string price { get; set; }

		public string currency { get; set; }

		public long paid_timestamp { get; set; }
	}

	public class QueryResponse2
	{
		public string order_id { get; set; }

		public string app_id { get; set; }

		public string purchase_id { get; set; }

		public string user_data { get; set; }

		public string price { get; set; }

		public string currency { get; set; }

		public long paid_timestamp { get; set; }
	}

	public class QueryListResponse
	{
		public List<QueryResponse2> purchaseList;

		public int total { get; set; }

		public int from { get; set; }

		public int to { get; set; }
	}

	public class StatusDetailTransaction
	{
		public long create_time { get; set; }

		public string payment_method { get; set; }

		public string status { get; set; }
	}

	public class StatusDetail
	{
		public long date_next_charge { get; set; }

		public StatusDetailTransaction[] transactions { get; set; }

		public string cancel_reason { get; set; }
	}

	public class TimePeriod
	{
		public string time_type { get; set; }

		public int value { get; set; }
	}

	public class Subscription
	{
		public string app_id { get; set; }

		public string order_id { get; set; }

		public string subscription_id { get; set; }

		public string price { get; set; }

		public string currency { get; set; }

		public long subscribed_timestamp { get; set; }

		public TimePeriod free_trial_period { get; set; }

		public TimePeriod charge_period { get; set; }

		public int number_of_charge_period { get; set; }

		public string plan_id { get; set; }

		public string plan_name { get; set; }

		public string status { get; set; }

		public StatusDetail status_detail { get; set; }
	}

	public class QuerySubscritionResponse
	{
		public int statusCode { get; set; }

		public string message { get; set; }

		public List<Subscription> subscriptions { get; set; }
	}

	private static IAPurchaseCallback isReadyIl2cppCallback;

	private static IAPurchaseCallback request01Il2cppCallback;

	private static IAPurchaseCallback request02Il2cppCallback;

	private static IAPurchaseCallback purchaseIl2cppCallback;

	private static IAPurchaseCallback query01Il2cppCallback;

	private static IAPurchaseCallback query02Il2cppCallback;

	private static IAPurchaseCallback getBalanceIl2cppCallback;

	private static IAPurchaseCallback requestSubscriptionIl2cppCallback;

	private static IAPurchaseCallback requestSubscriptionWithPlanIDIl2cppCallback;

	private static IAPurchaseCallback subscribeIl2cppCallback;

	private static IAPurchaseCallback querySubscriptionIl2cppCallback;

	private static IAPurchaseCallback querySubscriptionListIl2cppCallback;

	private static IAPurchaseCallback cancelSubscriptionIl2cppCallback;

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void IsReadyIl2cppCallback(int errorCode, string message)
	{
		isReadyIl2cppCallback(errorCode, message);
	}

	public static void IsReady(IAPurchaseListener listener, string pchAppKey)
	{
		isReadyIl2cppCallback = new IAPHandler(listener).getIsReadyHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.IsReady_64(IsReadyIl2cppCallback, pchAppKey);
		}
		else
		{
			Viveport.Internal.IAPurchase.IsReady(IsReadyIl2cppCallback, pchAppKey);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void Request01Il2cppCallback(int errorCode, string message)
	{
		request01Il2cppCallback(errorCode, message);
	}

	public static void Request(IAPurchaseListener listener, string pchPrice)
	{
		request01Il2cppCallback = new IAPHandler(listener).getRequestHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.Request_64(Request01Il2cppCallback, pchPrice);
		}
		else
		{
			Viveport.Internal.IAPurchase.Request(Request01Il2cppCallback, pchPrice);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void Request02Il2cppCallback(int errorCode, string message)
	{
		request02Il2cppCallback(errorCode, message);
	}

	public static void Request(IAPurchaseListener listener, string pchPrice, string pchUserData)
	{
		request02Il2cppCallback = new IAPHandler(listener).getRequestHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.Request_64(Request02Il2cppCallback, pchPrice, pchUserData);
		}
		else
		{
			Viveport.Internal.IAPurchase.Request(Request02Il2cppCallback, pchPrice, pchUserData);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void PurchaseIl2cppCallback(int errorCode, string message)
	{
		purchaseIl2cppCallback(errorCode, message);
	}

	public static void Purchase(IAPurchaseListener listener, string pchPurchaseId)
	{
		purchaseIl2cppCallback = new IAPHandler(listener).getPurchaseHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.Purchase_64(PurchaseIl2cppCallback, pchPurchaseId);
		}
		else
		{
			Viveport.Internal.IAPurchase.Purchase(PurchaseIl2cppCallback, pchPurchaseId);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void Query01Il2cppCallback(int errorCode, string message)
	{
		query01Il2cppCallback(errorCode, message);
	}

	public static void Query(IAPurchaseListener listener, string pchPurchaseId)
	{
		query01Il2cppCallback = new IAPHandler(listener).getQueryHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.Query_64(Query01Il2cppCallback, pchPurchaseId);
		}
		else
		{
			Viveport.Internal.IAPurchase.Query(Query01Il2cppCallback, pchPurchaseId);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void Query02Il2cppCallback(int errorCode, string message)
	{
		query02Il2cppCallback(errorCode, message);
	}

	public static void Query(IAPurchaseListener listener)
	{
		query02Il2cppCallback = new IAPHandler(listener).getQueryListHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.Query_64(Query02Il2cppCallback);
		}
		else
		{
			Viveport.Internal.IAPurchase.Query(Query02Il2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void GetBalanceIl2cppCallback(int errorCode, string message)
	{
		getBalanceIl2cppCallback(errorCode, message);
	}

	public static void GetBalance(IAPurchaseListener listener)
	{
		getBalanceIl2cppCallback = new IAPHandler(listener).getBalanceHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.GetBalance_64(GetBalanceIl2cppCallback);
		}
		else
		{
			Viveport.Internal.IAPurchase.GetBalance(GetBalanceIl2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void RequestSubscriptionIl2cppCallback(int errorCode, string message)
	{
		requestSubscriptionIl2cppCallback(errorCode, message);
	}

	public static void RequestSubscription(IAPurchaseListener listener, string pchPrice, string pchFreeTrialType, int nFreeTrialValue, string pchChargePeriodType, int nChargePeriodValue, int nNumberOfChargePeriod, string pchPlanId)
	{
		requestSubscriptionIl2cppCallback = new IAPHandler(listener).getRequestSubscriptionHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.RequestSubscription_64(RequestSubscriptionIl2cppCallback, pchPrice, pchFreeTrialType, nFreeTrialValue, pchChargePeriodType, nChargePeriodValue, nNumberOfChargePeriod, pchPlanId);
		}
		else
		{
			Viveport.Internal.IAPurchase.RequestSubscription(RequestSubscriptionIl2cppCallback, pchPrice, pchFreeTrialType, nFreeTrialValue, pchChargePeriodType, nChargePeriodValue, nNumberOfChargePeriod, pchPlanId);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void RequestSubscriptionWithPlanIDIl2cppCallback(int errorCode, string message)
	{
		requestSubscriptionWithPlanIDIl2cppCallback(errorCode, message);
	}

	public static void RequestSubscriptionWithPlanID(IAPurchaseListener listener, string pchPlanId)
	{
		requestSubscriptionWithPlanIDIl2cppCallback = new IAPHandler(listener).getRequestSubscriptionWithPlanIDHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.RequestSubscriptionWithPlanID_64(RequestSubscriptionWithPlanIDIl2cppCallback, pchPlanId);
		}
		else
		{
			Viveport.Internal.IAPurchase.RequestSubscriptionWithPlanID(RequestSubscriptionWithPlanIDIl2cppCallback, pchPlanId);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void SubscribeIl2cppCallback(int errorCode, string message)
	{
		subscribeIl2cppCallback(errorCode, message);
	}

	public static void Subscribe(IAPurchaseListener listener, string pchSubscriptionId)
	{
		subscribeIl2cppCallback = new IAPHandler(listener).getSubscribeHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.Subscribe_64(SubscribeIl2cppCallback, pchSubscriptionId);
		}
		else
		{
			Viveport.Internal.IAPurchase.Subscribe(SubscribeIl2cppCallback, pchSubscriptionId);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void QuerySubscriptionIl2cppCallback(int errorCode, string message)
	{
		querySubscriptionIl2cppCallback(errorCode, message);
	}

	public static void QuerySubscription(IAPurchaseListener listener, string pchSubscriptionId)
	{
		querySubscriptionIl2cppCallback = new IAPHandler(listener).getQuerySubscriptionHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.QuerySubscription_64(QuerySubscriptionIl2cppCallback, pchSubscriptionId);
		}
		else
		{
			Viveport.Internal.IAPurchase.QuerySubscription(QuerySubscriptionIl2cppCallback, pchSubscriptionId);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void QuerySubscriptionListIl2cppCallback(int errorCode, string message)
	{
		querySubscriptionListIl2cppCallback(errorCode, message);
	}

	public static void QuerySubscriptionList(IAPurchaseListener listener)
	{
		querySubscriptionListIl2cppCallback = new IAPHandler(listener).getQuerySubscriptionListHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.QuerySubscriptionList_64(QuerySubscriptionListIl2cppCallback);
		}
		else
		{
			Viveport.Internal.IAPurchase.QuerySubscriptionList(QuerySubscriptionListIl2cppCallback);
		}
	}

	[MonoPInvokeCallback(typeof(IAPurchaseCallback))]
	private static void CancelSubscriptionIl2cppCallback(int errorCode, string message)
	{
		cancelSubscriptionIl2cppCallback(errorCode, message);
	}

	public static void CancelSubscription(IAPurchaseListener listener, string pchSubscriptionId)
	{
		cancelSubscriptionIl2cppCallback = new IAPHandler(listener).getCancelSubscriptionHandler();
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.IAPurchase.CancelSubscription_64(CancelSubscriptionIl2cppCallback, pchSubscriptionId);
		}
		else
		{
			Viveport.Internal.IAPurchase.CancelSubscription(CancelSubscriptionIl2cppCallback, pchSubscriptionId);
		}
	}
}
