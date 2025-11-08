using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DevInspectorColor : Attribute
{
	public string Color { get; }

	public DevInspectorColor(string color)
	{
		Color = color;
	}
}
