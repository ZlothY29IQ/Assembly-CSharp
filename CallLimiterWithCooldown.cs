using System;
using UnityEngine;

[Serializable]
public class CallLimiterWithCooldown : CallLimiter
{
	[SerializeField]
	private float spamCoolDown;

	public CallLimiterWithCooldown(float coolDownSpam, int historyLength, float coolDown)
		: base(historyLength, coolDown)
	{
		spamCoolDown = coolDownSpam;
	}

	public CallLimiterWithCooldown(float coolDownSpam, int historyLength, float coolDown, float latencyMax)
		: base(historyLength, coolDown, latencyMax)
	{
		spamCoolDown = coolDownSpam;
	}

	public override bool CheckCallTime(float time)
	{
		if (blockCall && time < blockStartTime + spamCoolDown)
		{
			blockStartTime = time;
			return false;
		}
		return base.CheckCallTime(time);
	}
}
