using System;

public class ComponentMember
{
	private Func<string> getValue;

	public string computedPrefix;

	public string computedSuffix;

	public string Name { get; }

	public string Value => getValue();

	public bool IsStarred { get; }

	public string Color { get; }

	public ComponentMember(string name, Func<string> getValue, bool isStarred, string color)
	{
		Name = name;
		this.getValue = getValue;
		IsStarred = isStarred;
		Color = color;
	}
}
