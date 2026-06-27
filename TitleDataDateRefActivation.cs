using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaNetworking;
using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TitleDataDateRefActivation : MonoBehaviour, IGorillaSliceableSimple
{
	private enum ReadyState
	{
		None,
		Initializing,
		Ready,
		Crashed
	}

	[Serializable]
	private class TitleDataDateRefActivationTarget : IComparable<TitleDataDateRefActivationTarget>
	{
		[SerializeField]
		private bool activationState;

		[SerializeField]
		private GameObject gameObject;

		[SerializeField]
		private int hrs;

		[SerializeField]
		private int min;

		[SerializeField]
		private int sec;

		[SerializeField]
		private UnityEvent payload;

		[SerializeField]
		private UnityEvent<float> persistantPayload;

		private DateTime dateTime = DateTime.MaxValue;

		public GameObject GameObject => gameObject;

		public DateTime ActivationTime => dateTime;

		public void Initialize(DateTime refTime)
		{
			dateTime = refTime.AddHours(hrs).AddMinutes(min).AddSeconds(sec);
		}

		public void Activate(DateTime now)
		{
			float late = (float)(now - dateTime).TotalSeconds;
			Activate(late);
		}

		public void Activate()
		{
			Activate(0f);
		}

		private void Activate(float late)
		{
			if (gameObject != null)
			{
				gameObject.SetActive(activationState);
			}
			if (late < 1f)
			{
				payload?.Invoke();
			}
			persistantPayload?.Invoke(late);
		}

		int IComparable<TitleDataDateRefActivationTarget>.CompareTo(TitleDataDateRefActivationTarget other)
		{
			return (hrs * 3600 + min * 60 + sec).CompareTo(other.hrs * 3600 + other.min * 60 + other.sec);
		}
	}

	[SerializeField]
	private string titleDataKey;

	[SerializeField]
	private TitleDataDateRefActivationTarget[] nodes;

	[SerializeField]
	private TMP_Text tmpStatus;

	private ReadyState readyState;

	private List<TitleDataDateRefActivationTarget> nodeList = new List<TitleDataDateRefActivationTarget>();

	private int activations;

	private async void Initialize()
	{
		if (readyState == ReadyState.Initializing || readyState == ReadyState.Ready)
		{
			return;
		}
		readyState = ReadyState.Initializing;
		if (titleDataKey.IsNullOrEmpty())
		{
			onTD("1/1/3001");
			return;
		}
		while (PlayFabTitleDataCache.Instance == null)
		{
			await Task.Yield();
		}
		PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onTD, onTDError);
	}

	private void onTD(string s)
	{
		try
		{
			setStartDate(DateTime.Parse(s));
			readyState = ReadyState.Ready;
		}
		catch (Exception ex)
		{
			Debug.Log("TitleDataDateRefActivation :: onTD :: " + ex.Message + " :: " + ex.StackTrace);
			readyState = ReadyState.Crashed;
		}
	}

	public void StartNow(float delay)
	{
		setStartDate(GorillaComputer.instance.GetServerTime().AddSeconds(delay));
	}

	private void setStartDate(DateTime d)
	{
		nodeList.Clear();
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].Initialize(d);
			nodeList.Add(nodes[i]);
		}
		nodeList.Sort();
		activations = 0;
	}

	private void onTDError(PlayFabError error)
	{
		Debug.Log($"TitleDataDateRefActivation :: onTDError :: {error}");
		readyState = ReadyState.Crashed;
	}

	private void OnEnable()
	{
		Initialize();
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		if (readyState != ReadyState.Ready || nodeList.Count == 0)
		{
			return;
		}
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		if (serverTime.Year >= 2000)
		{
			if (tmpStatus != null)
			{
				tmpStatus.text = $"action {activations + 1} of {nodes.Length} in {nodeList[0].ActivationTime - GorillaComputer.instance.GetServerTime():g} s";
			}
			if (nodeList[0].ActivationTime <= serverTime)
			{
				nodeList[0].Activate(serverTime);
				nodeList.RemoveAt(0);
				activations++;
			}
		}
	}
}
