using System;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public class OnExitPlay_Run : OnExitPlay_Attribute
{
	public override void OnEnterPlay(MethodInfo method)
	{
		if (!method.IsStatic)
		{
			Debug.LogError($"Can't Run non-static method {method.DeclaringType}.{method.Name}");
		}
		else
		{
			method.Invoke(null, new object[0]);
		}
	}
}
