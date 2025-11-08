using System;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class OnEnterPlay_Clear : OnEnterPlay_Attribute
{
	public override void OnEnterPlay(FieldInfo field)
	{
		if (!field.IsStatic)
		{
			Debug.LogError($"Can't Clear non-static field {field.DeclaringType}.{field.Name}");
		}
		else
		{
			field.FieldType.GetMethod("Clear").Invoke(field.GetValue(null), new object[0]);
		}
	}
}
