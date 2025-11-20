using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class FlagEvents<T> where T : Enum
{
	[Serializable]
	private class FlagEvent : ISerializationCallbackReceiver
	{
		public string debugName = "Any flag true";

		[Tooltip("Check this box if only the local player is supposed to run this event.")]
		public bool runOnlyLocally;

		private T flags;

		[HideInInspector]
		public int flagsAsInt;

		public UnityEvent anyFlagTrue;

		private string FlagsLabel => typeof(T).Name;

		public void OnBeforeSerialize()
		{
			flagsAsInt = Convert.ToInt32(flags);
		}

		public void OnAfterDeserialize()
		{
			flags = (T)(object)flagsAsInt;
		}
	}

	[SerializeField]
	private FlagEvent[] list;

	public void InvokeAll(T test, bool isLocal = false)
	{
		int num = Convert.ToInt32(test);
		for (int i = 0; i < list.Length; i++)
		{
			if ((num & list[i].flagsAsInt) != 0 && (!list[i].runOnlyLocally || isLocal))
			{
				list[i].anyFlagTrue?.Invoke();
			}
		}
	}
}
