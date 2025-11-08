using System;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using UnityEngine;

public static class ContextLog
{
	public static void Log<T0, T1>(this T0 ctx, T1 arg1)
	{
		Debug.Log(ZString.Concat(GetPrefix(ref ctx), arg1));
	}

	public static void LogCall<T0, T1>(this T0 ctx, T1 arg1, [CallerMemberName] string call = null)
	{
		string prefix = GetPrefix(ref ctx);
		string arg2 = ZString.Concat("{.", call, "()} ");
		Debug.Log(ZString.Concat(prefix, arg2, arg1));
	}

	private static string GetPrefix<T>(ref T ctx)
	{
		if (ctx == null)
		{
			return string.Empty;
		}
		string arg = ((!(ctx is Type type)) ? ((!(ctx is string text)) ? ctx.GetType().Name : text) : type.Name);
		return ZString.Concat("[", arg, "] ");
	}
}
