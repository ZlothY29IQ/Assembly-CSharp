using System.Collections.Generic;

namespace Viveport;

public class SubscriptionStatus
{
	public enum Platform
	{
		Windows,
		Android
	}

	public enum TransactionType
	{
		Unknown,
		Paid,
		Redeem,
		FreeTrial
	}

	public List<Platform> Platforms { get; set; }

	public TransactionType Type { get; set; }

	public SubscriptionStatus()
	{
		Platforms = new List<Platform>();
		Type = TransactionType.Unknown;
	}
}
