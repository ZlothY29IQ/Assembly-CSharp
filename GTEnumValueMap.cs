using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GTEnumValueMap<T> : ISerializationCallbackReceiver
{
	[Serializable]
	private struct EnumValueToUnityObject
	{
		public bool enabled;

		public long enumKey;

		public string enumName;

		public T value;
	}

	private Dictionary<long, T> _enumValue_to_unityObject = new Dictionary<long, T>();

	[Tooltip("The GUID to the Enum script asset which is what is serialized in editor (not used at runtime). This is exposed and editable as a precaution but shouldn't be necessary to have to use.")]
	[SerializeField]
	private string m_enumScriptGuid;

	[SerializeField]
	private List<EnumValueToUnityObject> m_enumValueAndUnityObjectPairs = new List<EnumValueToUnityObject>();

	public IEnumerable<T> Values => _enumValue_to_unityObject.Values;

	public bool TryGet(long i, out T o)
	{
		return _enumValue_to_unityObject.TryGetValue(i, out o);
	}

	void ISerializationCallbackReceiver.OnBeforeSerialize()
	{
	}

	void ISerializationCallbackReceiver.OnAfterDeserialize()
	{
		Init();
	}

	public void Init()
	{
		if (m_enumValueAndUnityObjectPairs == null)
		{
			return;
		}
		if (_enumValue_to_unityObject == null)
		{
			_enumValue_to_unityObject = new Dictionary<long, T>();
		}
		_enumValue_to_unityObject.Clear();
		foreach (EnumValueToUnityObject enumValueAndUnityObjectPair in m_enumValueAndUnityObjectPairs)
		{
			if (enumValueAndUnityObjectPair.enabled && enumValueAndUnityObjectPair.value != null)
			{
				_enumValue_to_unityObject[enumValueAndUnityObjectPair.enumKey] = enumValueAndUnityObjectPair.value;
			}
		}
		if (!Application.isEditor)
		{
			m_enumScriptGuid = null;
			m_enumValueAndUnityObjectPairs = null;
		}
	}
}
