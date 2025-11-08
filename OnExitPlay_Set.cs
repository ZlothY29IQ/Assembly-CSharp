using System;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class OnExitPlay_Set : OnExitPlay_Attribute
{
	private object value;

	public OnExitPlay_Set(object value)
	{
		this.value = value;
	}

	public override void OnEnterPlay(FieldInfo field)
	{
		if (!field.IsStatic)
		{
			Debug.LogError($"Can't Set non-static field {field.DeclaringType}.{field.Name}");
		}
		else
		{
			field.SetValue(null, value);
		}
	}
}
