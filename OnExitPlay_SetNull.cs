using System;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class OnExitPlay_SetNull : OnExitPlay_Attribute
{
	public override void OnEnterPlay(FieldInfo field)
	{
		if (!field.IsStatic)
		{
			Debug.LogError($"Can't SetNull non-static field {field.DeclaringType}.{field.Name}");
		}
		else
		{
			field.SetValue(null, null);
		}
	}
}
