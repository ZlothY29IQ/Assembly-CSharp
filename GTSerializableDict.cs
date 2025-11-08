using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GTSerializableDict<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver where TKey : IComparable<TKey>
{
	[SerializeField]
	[HideInInspector]
	private List<GTSerializableKeyValue<TKey, TValue>> _m_serializedEntries = new List<GTSerializableKeyValue<TKey, TValue>>();

	public void OnBeforeSerialize()
	{
		_m_serializedEntries.Clear();
		using (Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<TKey, TValue> current = enumerator.Current;
				_m_serializedEntries.Add(new GTSerializableKeyValue<TKey, TValue>(current.Key, current.Value));
			}
		}
		_m_serializedEntries.Sort((GTSerializableKeyValue<TKey, TValue> entry1, GTSerializableKeyValue<TKey, TValue> entry2) => entry1.k.CompareTo(entry2.k));
	}

	public void OnAfterDeserialize()
	{
		Clear();
		foreach (GTSerializableKeyValue<TKey, TValue> m_serializedEntry in _m_serializedEntries)
		{
			try
			{
				Add(m_serializedEntry.k, m_serializedEntry.v);
			}
			catch (ArgumentException ex)
			{
				Debug.LogError("ERROR!!! GTSerializableDict: " + $"Duplicate key found during deserialization: '{m_serializedEntry.k}'. Ignoring duplicate. " + "Exception: " + ex.Message);
			}
		}
		_m_serializedEntries.Clear();
	}
}
