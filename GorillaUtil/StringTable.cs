using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaUtil;

[CreateAssetMenu(fileName = "StringTable", menuName = "Scriptable Objects/StringTable")]
public class StringTable : ScriptableObject
{
	[Serializable]
	private struct StringPair
	{
		public string Key;

		public string Value;
	}

	[SerializeField]
	private StringPair[] entries;

	private Dictionary<string, string> dict;

	private string keyList;

	public int Count => entries.Length;

	public string KeyList
	{
		get
		{
			if (keyList.IsNullOrEmpty() && entries.Length != 0)
			{
				return buildKeyList();
			}
			return keyList;
		}
	}

	private string buildKeyList()
	{
		keyList = string.Empty;
		if (entries.Length != 0)
		{
			for (int i = 0; i < entries.Length - 1; i++)
			{
				keyList = keyList + entries[i].Key + ", ";
			}
			keyList += entries[entries.Length - 1].Key;
		}
		return keyList;
	}

	public bool ContainsKey(string key)
	{
		if (dict == null)
		{
			dict = new Dictionary<string, string>();
			for (int i = 0; i < entries.Length; i++)
			{
				dict.Add(entries[i].Key, entries[i].Value);
			}
		}
		return dict.ContainsKey(key);
	}

	public string FetchValue(string key)
	{
		if (ContainsKey(key))
		{
			return dict[key];
		}
		return null;
	}
}
