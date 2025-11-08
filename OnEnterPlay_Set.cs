using System;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class OnEnterPlay_Set : OnEnterPlay_Attribute
{
	private object value;

	public OnEnterPlay_Set(object value)
	{
		this.value = value;
	}

	public override void OnEnterPlay(FieldInfo field)
	{
		if (!field.IsStatic)
		{
			Debug.LogError($"Can't Set non-static field {field.DeclaringType}.{field.Name}");
		}
		else if (field.FieldType == typeof(ushort))
		{
			field.SetValue(null, Convert.ToUInt16(value));
		}
		else
		{
			field.SetValue(null, value);
		}
	}
}
