using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaNetworking;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;

public class TitleDataActivation : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	public class TitleDataActivationData
	{
		[SerializeField]
		private TitleDataObjectActivationData[] data;

		private bool validated;

		public TitleDataObjectActivationData[] Data
		{
			get
			{
				return data;
			}
			set
			{
				data = value;
			}
		}
	}

	[Serializable]
	public class TitleDataObjectActivationData
	{
		[SerializeField]
		private string titleDataObjectID;

		[SerializeField]
		private AbsoluteDateTimeWindow[] absoluteDateTimeWindow;

		[SerializeField]
		private RelativeDateTimeWindow[] relativeDateTimeWindow;

		private bool validated;

		public string TitleDataObjectID
		{
			get
			{
				return titleDataObjectID;
			}
			set
			{
				titleDataObjectID = value;
			}
		}

		public AbsoluteDateTimeWindow[] AbsoluteDateTimeWindow
		{
			get
			{
				return absoluteDateTimeWindow;
			}
			set
			{
				absoluteDateTimeWindow = value;
			}
		}

		public RelativeDateTimeWindow[] RelativeDateTimeWindow
		{
			get
			{
				return relativeDateTimeWindow;
			}
			set
			{
				relativeDateTimeWindow = value;
			}
		}
	}

	[Serializable]
	public class AbsoluteDateTimeWindow
	{
		protected DateTime dtStart;

		protected DateTime dtEnd;

		[SerializeField]
		private string startDateTime;

		[SerializeField]
		private string endDateTime;

		public string StartDateTime
		{
			get
			{
				return startDateTime;
			}
			set
			{
				if (DateTime.TryParse(value, out dtStart))
				{
					startDateTime = dtStart.ToString();
				}
			}
		}

		public string EndDateTime
		{
			get
			{
				return endDateTime;
			}
			set
			{
				if (DateTime.TryParse(value, out dtEnd))
				{
					endDateTime = dtEnd.ToString();
				}
			}
		}

		public void IsInWindow(DateTime d, out bool inRange, out float delay)
		{
			inRange = d >= dtStart && d <= dtEnd;
			delay = (float)(d - dtStart).TotalSeconds;
		}
	}

	[Serializable]
	public class RelativeDateTimeWindow
	{
		protected DateTime dtStart;

		protected DateTime dtEnd;

		[SerializeField]
		private RelativeDateTime startDateTime;

		[SerializeField]
		private RelativeDateTime endDateTime;

		public RelativeDateTime StartDateTime
		{
			get
			{
				return startDateTime;
			}
			set
			{
				startDateTime = value;
				dtStart = ReferenceDate.AddDays(startDateTime.DaysPast).AddHours(startDateTime.Hour).AddMinutes(startDateTime.Minute);
			}
		}

		public RelativeDateTime EndDateTime
		{
			get
			{
				return endDateTime;
			}
			set
			{
				endDateTime = value;
				dtEnd = ReferenceDate.AddDays(endDateTime.DaysPast).AddHours(endDateTime.Hour).AddMinutes(endDateTime.Minute);
			}
		}

		public void IsInWindow(DateTime d, out bool inRange, out float delay)
		{
			inRange = d >= dtStart && d <= dtEnd;
			delay = (float)(d - dtStart).TotalSeconds;
		}
	}

	[Serializable]
	public struct RelativeDateTime
	{
		public int DaysPast;

		public int Hour;

		public int Minute;
	}

	public static DateTime ReferenceDate = DateTime.MinValue;

	[SerializeField]
	private string titleDataKey;

	[SerializeField]
	private string titleDataObjectID;

	private TitleDataObjectActivationData activationData;

	private GameObject[] gameObjects;

	private bool initialized;

	private bool onOffState;

	[RuntimeInitializeOnLoadMethod]
	private static async void RuntimeInit()
	{
		ReferenceDate = DateTime.MinValue;
		while (PlayFabTitleDataCache.Instance == null)
		{
			await Task.Yield();
		}
		PlayFabTitleDataCache.Instance.GetTitleData("ActivationReferenceDate", onTDReferenceDate, onTDReferenceDateError);
	}

	private static void onTDReferenceDate(string s)
	{
		if (!DateTime.TryParse(s, out ReferenceDate))
		{
			Debug.LogError("TitleDataActivation :: onTDReferenceDate :: No Reference Date Set!!");
		}
	}

	private static void onTDReferenceDateError(PlayFabError error)
	{
		Debug.LogError("TitleDataActivation :: onTDReferenceDateError :: No Reference Date Set!!");
	}

	private async void Initialize()
	{
		if (initialized)
		{
			return;
		}
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			GameObject gameObject = base.transform.GetChild(i).gameObject;
			gameObject.SetActive(value: false);
			list.Add(gameObject);
		}
		gameObjects = list.ToArray();
		initialized = true;
		if (!titleDataKey.IsNullOrEmpty())
		{
			while (PlayFabTitleDataCache.Instance == null)
			{
				await Task.Yield();
			}
			PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onTD, onTDError);
		}
	}

	private void onTD(string s)
	{
		TitleDataActivationData titleDataActivationData = null;
		try
		{
			titleDataActivationData = JsonConvert.DeserializeObject<TitleDataActivationData>(s);
		}
		catch (Exception ex)
		{
			Debug.LogError("TitleDataActivation :: onTD :: " + ex.Message);
			return;
		}
		for (int i = 0; i < titleDataActivationData.Data.Length; i++)
		{
			if (titleDataActivationData.Data[i].TitleDataObjectID == titleDataObjectID)
			{
				activationData = titleDataActivationData.Data[i];
				break;
			}
		}
	}

	private void onTDError(PlayFabError error)
	{
		Debug.LogError($"TitleDataActivation :: onTDError :: {titleDataKey} :: {error}");
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
		if (activationData == null)
		{
			return;
		}
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		if (serverTime.Year >= 2000)
		{
			bool inRange = false;
			float delay = 0f;
			int num = 0;
			while (activationData.AbsoluteDateTimeWindow != null && num < activationData.AbsoluteDateTimeWindow.Length && !inRange)
			{
				activationData.AbsoluteDateTimeWindow[num].IsInWindow(serverTime, out inRange, out delay);
				num++;
			}
			int num2 = 0;
			while (activationData.RelativeDateTimeWindow != null && num2 < activationData.RelativeDateTimeWindow.Length && !inRange)
			{
				activationData.RelativeDateTimeWindow[num2].IsInWindow(serverTime, out inRange, out delay);
				num2++;
			}
			if (inRange != onOffState)
			{
				SetState(inRange, delay);
				onOffState = inRange;
			}
		}
	}

	private void SetState(bool onOff, float delayedActivation)
	{
		for (int i = 0; i < gameObjects.Length; i++)
		{
			gameObjects[i].SetActive(onOff);
			if (!onOff || !(delayedActivation > 0f))
			{
				continue;
			}
			Animator[] componentsInChildren = gameObjects[i].GetComponentsInChildren<Animator>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				int fullPathHash = componentsInChildren[j].GetCurrentAnimatorStateInfo(0).fullPathHash;
				componentsInChildren[j].PlayInFixedTime(fullPathHash, 0, delayedActivation);
			}
			AudioSource[] componentsInChildren2 = gameObjects[i].GetComponentsInChildren<AudioSource>();
			for (int k = 0; k < componentsInChildren2.Length; k++)
			{
				if (componentsInChildren2[k].playOnAwake)
				{
					if (componentsInChildren2[k].clip.length < delayedActivation)
					{
						componentsInChildren2[k].time = delayedActivation;
					}
					else
					{
						componentsInChildren2[k].Stop();
					}
				}
			}
		}
	}
}
