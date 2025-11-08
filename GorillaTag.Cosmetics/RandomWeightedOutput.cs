using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

[RequireComponent(typeof(NetworkedRandomProvider))]
public class RandomWeightedOutput : MonoBehaviour
{
	[Serializable]
	public class WeightedOutput
	{
		[SerializeField]
		public string name = "Event";

		[SerializeField]
		[Range(0f, 100f)]
		public float weight = 1f;

		[SerializeField]
		public bool enabled = true;

		[SerializeField]
		public UnityEvent onPick = new UnityEvent();
	}

	[Header("Network Provider")]
	[Tooltip("For best result, pick Float01 or Double01 as the output mode in your NetworkedRandomProvider")]
	[SerializeField]
	private NetworkedRandomProvider networkProvider;

	[Header("Weighted Outputs")]
	[SerializeField]
	private List<WeightedOutput> outputs = new List<WeightedOutput>();

	[Header("Event")]
	[SerializeField]
	public UnityEvent<int> onAnyPick = new UnityEvent<int>();

	[SerializeField]
	private bool debugLog;

	private void Awake()
	{
		if (networkProvider == null)
		{
			networkProvider = GetComponentInParent<NetworkedRandomProvider>();
		}
	}

	public void PickNextRandom()
	{
		int deterministicPickIndex = GetDeterministicPickIndex();
		if (deterministicPickIndex >= 0)
		{
			outputs[deterministicPickIndex].onPick?.Invoke();
			onAnyPick?.Invoke(deterministicPickIndex);
			if (debugLog)
			{
				Debug.Log($"[RandomWeightedOutput] Picked '{outputs[deterministicPickIndex].name}' (idx={deterministicPickIndex})");
			}
		}
	}

	private int GetDeterministicPickIndex()
	{
		if (networkProvider == null)
		{
			return -1;
		}
		List<int> list = new List<int>(outputs.Count);
		for (int i = 0; i < outputs.Count; i++)
		{
			WeightedOutput weightedOutput = outputs[i];
			if (weightedOutput != null && weightedOutput.enabled && weightedOutput.weight > 0f)
			{
				list.Add(i);
			}
		}
		if (list.Count == 0)
		{
			return -1;
		}
		double num = 0.0;
		foreach (int item in list)
		{
			num += (double)outputs[item].weight;
		}
		if (num <= 0.0)
		{
			return list[0];
		}
		double num2 = (double)networkProvider.GetSelectedAsFloat() * num;
		double num3 = 0.0;
		for (int j = 0; j < list.Count; j++)
		{
			int num4 = list[j];
			num3 += (double)outputs[num4].weight;
			if (num2 < num3)
			{
				return num4;
			}
		}
		return list[list.Count - 1];
	}
}
