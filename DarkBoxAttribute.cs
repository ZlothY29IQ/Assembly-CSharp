using System;
using System.Diagnostics;

[Conditional("UNITY_EDITOR")]
public class DarkBoxAttribute : Attribute
{
	public readonly bool withBorders;

	public DarkBoxAttribute()
	{
	}

	public DarkBoxAttribute(bool withBorders)
	{
		this.withBorders = withBorders;
	}
}
