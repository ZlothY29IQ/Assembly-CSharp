using System;
using AOT;
using Viveport.Internal;

namespace Viveport;

public class Subscription
{
	private static Viveport.Internal.StatusCallback2 isReadyIl2cppCallback;

	[MonoPInvokeCallback(typeof(Viveport.Internal.StatusCallback2))]
	private static void IsReadyIl2cppCallback(int errorCode, string message)
	{
		isReadyIl2cppCallback(errorCode, message);
	}

	public static void IsReady(StatusCallback2 callback)
	{
		if (callback == null)
		{
			throw new InvalidOperationException("callback == null");
		}
		isReadyIl2cppCallback = callback.Invoke;
		Api.InternalStatusCallback2s.Add(IsReadyIl2cppCallback);
		if (IntPtr.Size == 8)
		{
			Viveport.Internal.Subscription.IsReady_64(IsReadyIl2cppCallback);
		}
		else
		{
			Viveport.Internal.Subscription.IsReady(IsReadyIl2cppCallback);
		}
	}

	public static SubscriptionStatus GetUserStatus()
	{
		SubscriptionStatus subscriptionStatus = new SubscriptionStatus();
		if (IntPtr.Size == 8)
		{
			if (Viveport.Internal.Subscription.IsWindowsSubscriber_64())
			{
				subscriptionStatus.Platforms.Add(SubscriptionStatus.Platform.Windows);
			}
			if (Viveport.Internal.Subscription.IsAndroidSubscriber_64())
			{
				subscriptionStatus.Platforms.Add(SubscriptionStatus.Platform.Android);
			}
			switch (Viveport.Internal.Subscription.GetTransactionType_64())
			{
			case ESubscriptionTransactionType.UNKNOWN:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.Unknown;
				break;
			case ESubscriptionTransactionType.PAID:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.Paid;
				break;
			case ESubscriptionTransactionType.REDEEM:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.Redeem;
				break;
			case ESubscriptionTransactionType.FREEE_TRIAL:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.FreeTrial;
				break;
			default:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.Unknown;
				break;
			}
		}
		else
		{
			if (Viveport.Internal.Subscription.IsWindowsSubscriber())
			{
				subscriptionStatus.Platforms.Add(SubscriptionStatus.Platform.Windows);
			}
			if (Viveport.Internal.Subscription.IsAndroidSubscriber())
			{
				subscriptionStatus.Platforms.Add(SubscriptionStatus.Platform.Android);
			}
			switch (Viveport.Internal.Subscription.GetTransactionType())
			{
			case ESubscriptionTransactionType.UNKNOWN:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.Unknown;
				break;
			case ESubscriptionTransactionType.PAID:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.Paid;
				break;
			case ESubscriptionTransactionType.REDEEM:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.Redeem;
				break;
			case ESubscriptionTransactionType.FREEE_TRIAL:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.FreeTrial;
				break;
			default:
				subscriptionStatus.Type = SubscriptionStatus.TransactionType.Unknown;
				break;
			}
		}
		return subscriptionStatus;
	}
}
