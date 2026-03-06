using System;
using System.Collections.Generic;
using UnityEngine;

internal class RPCUtil
{
	private struct RPCCallID(string nameOfFunction, int senderId) : IEquatable<RPCCallID>
	{
		private int _senderID = senderId;

		private string _nameOfFunction = nameOfFunction;

		public readonly int SenderID => _senderID;

		public readonly string NameOfFunction => _nameOfFunction;

		bool IEquatable<RPCCallID>.Equals(RPCCallID other)
		{
			if (other.NameOfFunction.Equals(NameOfFunction))
			{
				return other.SenderID.Equals(SenderID);
			}
			return false;
		}
	}

	private static Dictionary<RPCCallID, float> RPCCallLog = new Dictionary<RPCCallID, float>();

	public static bool NotSpam(string id, PhotonMessageInfoWrapped info, float delay)
	{
		RPCCallID key = new RPCCallID(id, info.senderID);
		if (!RPCCallLog.ContainsKey(key))
		{
			RPCCallLog.Add(key, Time.time);
			return true;
		}
		if (Time.time - RPCCallLog[key] > delay)
		{
			RPCCallLog[key] = Time.time;
			return true;
		}
		return false;
	}

	public static bool SafeValue(float v)
	{
		if (float.IsNaN(v))
		{
			return false;
		}
		return float.IsFinite(v);
	}

	public static bool SafeValue(float v, float min, float max)
	{
		if (!SafeValue(v))
		{
			return false;
		}
		if (v <= max)
		{
			return v >= min;
		}
		return false;
	}
}
