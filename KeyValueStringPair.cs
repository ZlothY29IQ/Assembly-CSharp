using System;
using UnityEngine;

[Serializable]
public struct KeyValueStringPair
{
	public string Key;

	[Multiline]
	public string Value;

	public KeyValueStringPair(string key, string value)
	{
		Key = key;
		Value = value;
	}
}
