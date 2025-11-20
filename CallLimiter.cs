using System;
using Photon.Pun;
using UnityEngine;

[Serializable]
public class CallLimiter
{
	protected const double k_serverMaxTime = 4294967.295;

	[SerializeField]
	protected float[] callTimeHistory;

	[Space]
	[SerializeField]
	protected int callHistoryLength;

	[SerializeField]
	protected float timeCooldown;

	[SerializeField]
	protected double maxLatency;

	private int oldTimeIndex;

	protected bool blockCall;

	protected float blockStartTime;

	public CallLimiter()
	{
	}

	public CallLimiter(int historyLength, float coolDown, float latencyMax = 0.5f)
	{
		callTimeHistory = new float[historyLength];
		callHistoryLength = historyLength;
		for (int i = 0; i < historyLength; i++)
		{
			callTimeHistory[i] = float.MinValue;
		}
		timeCooldown = coolDown;
		maxLatency = latencyMax;
	}

	public bool CheckCallServerTime(double time)
	{
		double currentTime = PhotonNetwork.CurrentTime;
		double num = maxLatency;
		double num2 = 4294967.295 - maxLatency;
		double num3;
		if (currentTime > num || time < num)
		{
			if (time > currentTime + 0.05)
			{
				return false;
			}
			num3 = currentTime - time;
		}
		else
		{
			double num4 = num2 + currentTime;
			if (time > currentTime + 0.5 && time < num4)
			{
				return false;
			}
			num3 = currentTime + (4294967.295 - time);
		}
		if (num3 > maxLatency)
		{
			return false;
		}
		int num5 = ((oldTimeIndex > 0) ? (oldTimeIndex - 1) : (callHistoryLength - 1));
		double num6 = callTimeHistory[num5];
		if (num6 > num2 && time < num6)
		{
			Reset();
		}
		else if (time < num6)
		{
			return false;
		}
		return CheckCallTime((float)time);
	}

	public virtual bool CheckCallTime(float time)
	{
		if (callTimeHistory[oldTimeIndex] > time)
		{
			blockCall = true;
			blockStartTime = time;
			return false;
		}
		callTimeHistory[oldTimeIndex] = time + timeCooldown;
		oldTimeIndex = ++oldTimeIndex % callHistoryLength;
		blockCall = false;
		return true;
	}

	public virtual void Reset()
	{
		if (callTimeHistory != null)
		{
			for (int i = 0; i < callHistoryLength; i++)
			{
				callTimeHistory[i] = float.MinValue;
			}
			oldTimeIndex = 0;
			blockStartTime = 0f;
			blockCall = false;
		}
	}
}
