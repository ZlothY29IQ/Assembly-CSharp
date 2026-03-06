using System;
using UnityEngine;

public class PlayerPrefFlags
{
	public enum Flag
	{
		SHOW_1P_COSMETICS = 1,
		SWAP_HELD_COSMETICS = 2,
		GAME_MODE_SELECTOR_IS_SUPER = 4
	}

	public static Action<Flag, bool> OnFlagChange;

	private const int defaultValue = 5;

	internal static bool Check(Flag flag)
	{
		return ((uint)PlayerPrefs.GetInt("PlayerPrefFlags0", 5) & (uint)flag) == (uint)flag;
	}

	internal static void Touch(Flag flag)
	{
		bool arg = ((uint)PlayerPrefs.GetInt("PlayerPrefFlags0", 5) & (uint)flag) == (uint)flag;
		if (OnFlagChange != null)
		{
			OnFlagChange(flag, arg);
		}
	}

	internal static void TouchIf(Flag flag, bool value)
	{
		int num = PlayerPrefs.GetInt("PlayerPrefFlags0", 5);
		if (value == (((uint)num & (uint)flag) == (uint)flag) && OnFlagChange != null)
		{
			OnFlagChange(flag, value);
		}
	}

	internal static void Set(Flag flag, bool value)
	{
		int num = PlayerPrefs.GetInt("PlayerPrefFlags0", 5);
		num = ((!value) ? (num & (int)(~flag)) : (num | (int)flag));
		PlayerPrefs.SetInt("PlayerPrefFlags0", num);
		if (OnFlagChange != null)
		{
			OnFlagChange(flag, value);
		}
	}

	internal static bool Flip(Flag flag)
	{
		int num = PlayerPrefs.GetInt("PlayerPrefFlags0", 5);
		bool flag2 = ((uint)num & (uint)flag) != (uint)flag;
		num = ((!flag2) ? (num & (int)(~flag)) : (num | (int)flag));
		PlayerPrefs.SetInt("PlayerPrefFlags0", num);
		if (OnFlagChange != null)
		{
			OnFlagChange(flag, flag2);
		}
		return flag2;
	}
}
