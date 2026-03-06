using System;
using UnityEngine;

[Serializable]
public struct KeyValueStringPair(string key, string value)
{
	public string Key = key;

	[Multiline]
	public string Value = value;
}
