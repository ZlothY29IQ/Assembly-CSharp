using System;
using UnityEngine;

public class PlayerPrefFlags
{
	public enum Flag
	{
		SHOW_1P_COSMETICS = 1,
		SWAP_HELD_COSMETICS
	}

	public static Action<Flag, bool> OnFlagChange;

	private const int defaultValue = 1;

	internal static bool Check(Flag flag)
	{
		return ((uint)PlayerPrefs.GetInt("PlayerPrefFlags0", 1) & (uint)flag) == (uint)flag;
	}

	internal static void Touch(Flag flag)
	{
		bool arg = ((uint)PlayerPrefs.GetInt("PlayerPrefFlags0", 1) & (uint)flag) == (uint)flag;
		if (OnFlagChange != null)
		{
			OnFlagChange(flag, arg);
		}
	}

	internal static void TouchIf(Flag flag, bool value)
	{
		int @int = PlayerPrefs.GetInt("PlayerPrefFlags0", 1);
		if (value == (((uint)@int & (uint)flag) == (uint)flag) && OnFlagChange != null)
		{
			OnFlagChange(flag, value);
		}
	}

	internal static void Set(Flag flag, bool value)
	{
		int @int = PlayerPrefs.GetInt("PlayerPrefFlags0", 1);
		@int = ((!value) ? (@int & (int)(~flag)) : (@int | (int)flag));
		PlayerPrefs.SetInt("PlayerPrefFlags0", @int);
		if (OnFlagChange != null)
		{
			OnFlagChange(flag, value);
		}
	}

	internal static bool Flip(Flag flag)
	{
		int @int = PlayerPrefs.GetInt("PlayerPrefFlags0", 1);
		bool flag2 = ((uint)@int & (uint)flag) != (uint)flag;
		@int = ((!flag2) ? (@int & (int)(~flag)) : (@int | (int)flag));
		PlayerPrefs.SetInt("PlayerPrefFlags0", @int);
		if (OnFlagChange != null)
		{
			OnFlagChange(flag, flag2);
		}
		return flag2;
	}
}
