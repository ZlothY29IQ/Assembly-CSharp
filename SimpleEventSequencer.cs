using System;
using UnityEngine;
using UnityEngine.Events;

public class SimpleEventSequencer : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	private class SimpleEventSequencerNode
	{
		[Tooltip("This is just for legibilty. Doesn't matter what you name it.")]
		[SerializeField]
		private string name = "Untitled Node";

		[Tooltip("Seconds after the previous node's events are dispatched")]
		[SerializeField]
		private int time;

		[SerializeField]
		private UnityEvent unityEvent;

		public int Time => time;

		public UnityEvent UnityEvent => unityEvent;

		public string Name => name;
	}

	[SerializeField]
	private SimpleEventSequencerNode[] nodes;

	[SerializeField]
	private bool startOnEnable = true;

	[SerializeField]
	private bool disableOnComplete = true;

	private float startTime = -1f;

	private int idx;

	public void StartSequence()
	{
		idx = 0;
		startTime = Time.time;
		Debug.Log("SimpleEventSequencer :: " + base.name + " :: Starting");
	}

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		if (startOnEnable)
		{
			StartSequence();
		}
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		if (!(startTime < 0f) && idx != nodes.Length && Time.time >= startTime + (float)nodes[idx].Time)
		{
			Debug.Log("SimpleEventSequencer :: " + base.name + " :: " + nodes[idx].Name + " :: Invoke");
			nodes[idx].UnityEvent?.Invoke();
			startTime = Time.time;
			idx++;
			if (disableOnComplete && idx == nodes.Length)
			{
				base.gameObject.SetActive(value: false);
				Debug.Log("SimpleEventSequencer :: " + base.name + " :: Disabling");
			}
		}
	}
}
