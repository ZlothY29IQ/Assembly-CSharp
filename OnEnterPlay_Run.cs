using System;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public class OnEnterPlay_Run : OnEnterPlay_Attribute
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
