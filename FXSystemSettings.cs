using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/FXSystemSettings", order = 2)]
public class FXSystemSettings : ScriptableObject
{
	private const string preLog = "FXSystemSettings: ";

	private const string preErr = "ERROR!!!  FXSystemSettings: ";

	[SerializeField]
	private LimiterType[] callLimits;

	[SerializeField]
	private CooldownType[] CallLimitsCooldown;

	[NonSerialized]
	public bool forLocalRig;

	[NonSerialized]
	public CallLimitType<CallLimiter>[] callSettings = new CallLimitType<CallLimiter>[24];

	public void Awake()
	{
		int num = ((callLimits != null) ? callLimits.Length : 0);
		int num2 = ((CallLimitsCooldown != null) ? CallLimitsCooldown.Length : 0);
		int i = 0;
		int num3 = 0;
		FXType fXType = FXType.BalloonPop;
		for (; i < num; i++)
		{
			fXType = callLimits[i].Key;
			num3 = (int)fXType;
			if (num3 < 0 || num3 >= 24)
			{
				string text = "NO_PATH_AT_RUNTIME";
				Debug.LogError("FXSystemSettings: (this should never happen) `callLimits.Key` is out of bounds of `callSettings`! Path=\"" + text + "\"", this);
			}
			if (callSettings[num3] != null)
			{
				Debug.Log("FXSystemSettings: call setting for " + fXType.ToString() + " already exists, skipping.");
			}
			else
			{
				callSettings[num3] = callLimits[i];
			}
		}
		i = 0;
		num3 = 0;
		fXType = FXType.BalloonPop;
		for (; i < num2; i++)
		{
			fXType = CallLimitsCooldown[i].Key;
			num3 = (int)fXType;
			if (callSettings[num3] != null)
			{
				Debug.Log("FXSystemSettings: call setting for " + fXType.ToString() + " already exists, skipping");
			}
			else
			{
				callSettings[num3] = CallLimitsCooldown[i];
			}
		}
		for (i = 0; i < callSettings.Length; i++)
		{
			if (callSettings[i] == null)
			{
				callSettings[i] = new LimiterType
				{
					CallLimitSettings = new CallLimiter(0, 0f, 0f),
					Key = (FXType)i
				};
			}
		}
	}
}
