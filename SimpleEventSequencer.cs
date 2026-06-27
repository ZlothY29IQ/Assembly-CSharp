using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.Events;

public class SimpleEventSequencer : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	private class SimpleEventSequencerNode
	{
		[Tooltip("Uncheck to skip this node")]
		[SerializeField]
		private bool enabled = true;

		[Tooltip("Seconds after the previous node's events are dispatched")]
		[SerializeField]
		private float time;

		[Tooltip("This is just for legibilty. Doesn't matter what you name it.")]
		[SerializeField]
		private string name = "New Node";

		[SerializeField]
		private UnityEvent unityEvent;

		[SerializeField]
		[TextArea(5, 10)]
		private string notes = "Notes";

		private string fancyName = "New Node";

		private float totalTime;

		private string nameTrim
		{
			get
			{
				if (name.Length <= 33)
				{
					return name;
				}
				return name.Substring(0, 30) + "...";
			}
		}

		private string notesTrim
		{
			get
			{
				if (notes.Length <= 50)
				{
					return notes;
				}
				return notes.Substring(0, 47) + "...";
			}
		}

		public float Time => time;

		public UnityEvent UnityEvent => unityEvent;

		public string Name => name;

		public float TotalTime
		{
			set
			{
				totalTime = value;
			}
		}

		public bool Enabled => enabled;

		public void onValueChanged()
		{
			if (enabled)
			{
				fancyName = $"T+{totalTime} ({time}) : {nameTrim}";
			}
			else
			{
				fancyName = $"Skip ({time}) : {nameTrim}";
			}
		}
	}

	private enum OnCompleteAction
	{
		None,
		Disable,
		Repeat
	}

	[SerializeField]
	private SimpleEventSequencerNode[] nodes;

	[SerializeField]
	private bool startOnEnable = true;

	[SerializeField]
	private OnCompleteAction onComplete = OnCompleteAction.Disable;

	[SerializeField]
	private ServerTimeSyncRule serverTimeSync;

	private float startTime;

	private int idx = -1;

	private List<SimpleEventSequencerNode> enabledNodes = new List<SimpleEventSequencerNode>();

	private SimpleEventSequencerNode activeNode;

	private void StartSequence()
	{
		StartSequenceDelayed(0f);
	}

	public async void StartSequenceDelayed(float delay)
	{
		startTime = Time.time + delay;
		if (serverTimeSync != null)
		{
			while (GorillaComputer.instance == null || GorillaComputer.instance.GetServerTime().Year < 2000)
			{
				await Task.Yield();
			}
			DateTime serverTime = GorillaComputer.instance.GetServerTime();
			serverTime.AddSeconds(delay);
			DateTime next = serverTimeSync.GetNext(serverTime);
			startTime += (float)(next - serverTime).TotalSeconds;
		}
		idx = 0;
	}

	private void startSequenceImmediate()
	{
		startTime = Time.time;
		idx = 0;
	}

	private void startSequenceFrom(int i)
	{
		startTime = Time.time;
		idx = i;
	}

	private void stop(int i)
	{
		idx = -1;
	}

	private void Awake()
	{
		enabledNodes.Clear();
		for (int i = 0; i < nodes.Length; i++)
		{
			if (nodes[i].Enabled)
			{
				enabledNodes.Add(nodes[i]);
			}
		}
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
		if (idx < 0 || idx == enabledNodes.Count || !(Time.time >= startTime + enabledNodes[idx].Time))
		{
			return;
		}
		enabledNodes[idx].UnityEvent?.Invoke();
		startTime = Time.time;
		idx++;
		if (idx == enabledNodes.Count)
		{
			switch (onComplete)
			{
			case OnCompleteAction.Repeat:
				StartSequenceDelayed(enabledNodes[idx - 1].Time);
				break;
			case OnCompleteAction.Disable:
				base.gameObject.SetActive(value: false);
				break;
			}
		}
	}

	private void onValueChanged()
	{
		float num = 0f;
		for (int i = 0; i < nodes.Length; i++)
		{
			if (nodes[i].Enabled)
			{
				num += nodes[i].Time;
			}
			nodes[i].TotalTime = num;
			nodes[i].onValueChanged();
		}
	}

	public void SetOnCompleteActionDisable()
	{
		onComplete = OnCompleteAction.Disable;
	}

	public void SetOnCompleteActionRepeat()
	{
		onComplete = OnCompleteAction.Repeat;
	}

	public void ClearOnCompleteAction()
	{
		onComplete = OnCompleteAction.None;
	}

	public void TempAudio(string text)
	{
		Debug.Log("SimpleEventSequencer :: " + base.name + " :: TempAudio :: " + text);
	}

	public void TempVFX(string text)
	{
		Debug.Log("SimpleEventSequencer :: " + base.name + " :: TempVFX :: " + text);
	}

	public void Temp(string text)
	{
		Debug.Log("SimpleEventSequencer :: " + base.name + " :: Temp :: " + text);
	}

	public void DebugLog(string text)
	{
		Debug.Log("SimpleEventSequencer :: " + base.name + " :: DEBUG :: " + text);
	}
}
