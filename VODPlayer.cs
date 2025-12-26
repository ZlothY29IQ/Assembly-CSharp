using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaNetworking;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;
using UnityEngine.Video;

public class VODPlayer : MonoBehaviour, IGorillaSliceableSimple
{
	public enum State
	{
		INITIALIZING,
		IDLE,
		RUNNING,
		CRASHED
	}

	[Serializable]
	public class VODNextStream : IComparable<VODNextStream>
	{
		public int Prio;

		public string Title;

		public DateTime StartTime;

		public VODNextStream(int prio, string name, DateTime startTime)
		{
			Prio = prio;
			Title = name;
			StartTime = startTime;
		}

		int IComparable<VODNextStream>.CompareTo(VODNextStream other)
		{
			return (int)(StartTime - other.StartTime).TotalSeconds - (Prio - other.Prio);
		}
	}

	[Serializable]
	public struct VODStreamSchedule
	{
		public VODHourlyStream[] hourly;
	}

	[Serializable]
	public struct VODStream
	{
		public string name;

		public string url;
	}

	[Serializable]
	public struct VODHourlyStream : IComparable<VODHourlyStream>
	{
		public VODStream stream;

		[Range(0f, 59f)]
		public int minute;

		public string startDateTime;

		private DateTime startDT;

		public string endDateTime;

		private DateTime endDT;

		public int CompareTo(VODHourlyStream other)
		{
			return minute - other.minute;
		}

		public void ValidateDate()
		{
			try
			{
				startDT = DateTime.Parse(startDateTime);
			}
			catch
			{
				startDT = DateTime.Parse("1/1/0001");
			}
			try
			{
				endDT = DateTime.Parse(endDateTime);
			}
			catch
			{
				endDT = DateTime.Parse("1/1/3001");
			}
			startDateTime = startDT.ToString();
			endDateTime = endDT.ToString();
		}

		internal bool IsDateInRange(DateTime serverTime)
		{
			ValidateDate();
			if (serverTime >= startDT)
			{
				return serverTime <= endDT;
			}
			return false;
		}
	}

	public static Action OnCrash;

	public static State state;

	private VideoPlayer player;

	private AudioSource audioSource;

	private VODNextStream nextStream;

	[SerializeField]
	private VODStreamSchedule schedule;

	[SerializeField]
	private string titleDataKey;

	[SerializeField]
	private Material standbyMaterial;

	[SerializeField]
	private Material playBackMaterial;

	[SerializeField]
	private Material disconnectedMaterial;

	[SerializeField]
	private Material busyMaterial;

	private List<VODTarget> targets = new List<VODTarget>();

	private int lastCheck;

	private bool playerBusy;

	public async void OnEnable()
	{
		state = State.INITIALIZING;
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		VODTarget.AlertEnabled = (Action<VODTarget>)Delegate.Combine(VODTarget.AlertEnabled, new Action<VODTarget>(VODTarget_AlertEnabled));
		VODTarget.AlertDisabled = (Action<VODTarget>)Delegate.Combine(VODTarget.AlertDisabled, new Action<VODTarget>(VODTarget_AlertDisabled));
		if (player == null)
		{
			player = GetComponent<VideoPlayer>();
			player.loopPointReached += Player_loopPointReached;
			audioSource = GetComponentInChildren<AudioSource>();
			while (PlayFabTitleDataCache.Instance == null)
			{
				await Task.Yield();
			}
			PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onTD, onTDError);
		}
	}

	private async void waitOnServerTime()
	{
		while (GorillaComputer.instance == null || GorillaComputer.instance.GetServerTime().Year < 2000)
		{
			await Task.Yield();
		}
		state = State.IDLE;
	}

	private void VODTarget_AlertEnabled(VODTarget o)
	{
		if (!targets.Contains(o))
		{
			targets.Add(o);
			o.gameObject.SetActive(state != State.CRASHED);
			if (state == State.RUNNING && player.isPlaying)
			{
				o.Renderer.material = playBackMaterial;
			}
			else
			{
				o.Renderer.material = ((o.StandbyOverride == null) ? standbyMaterial : o.StandbyOverride);
			}
		}
	}

	private void VODTarget_AlertDisabled(VODTarget o)
	{
		if (targets.Contains(o))
		{
			targets.Remove(o);
			o.Renderer.material = ((o.StandbyOverride == null) ? disconnectedMaterial : o.StandbyOverride);
			if (o.UpNextText != null)
			{
				o.UpNextText.text = string.Empty;
			}
		}
	}

	private void Player_loopPointReached(VideoPlayer source)
	{
		if (!playerBusy)
		{
			player.Stop();
			for (int i = 0; i < targets.Count; i++)
			{
				targets[i].Renderer.material = ((targets[i].StandbyOverride == null) ? standbyMaterial : targets[i].StandbyOverride);
			}
			nextStream = NextStream();
		}
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		player.loopPointReached -= Player_loopPointReached;
		VODTarget.AlertEnabled = (Action<VODTarget>)Delegate.Remove(VODTarget.AlertEnabled, new Action<VODTarget>(VODTarget_AlertEnabled));
		VODTarget.AlertDisabled = (Action<VODTarget>)Delegate.Remove(VODTarget.AlertDisabled, new Action<VODTarget>(VODTarget_AlertDisabled));
	}

	private void OnDestroy()
	{
		VODTarget.AlertEnabled = (Action<VODTarget>)Delegate.Remove(VODTarget.AlertEnabled, new Action<VODTarget>(VODTarget_AlertEnabled));
		VODTarget.AlertDisabled = (Action<VODTarget>)Delegate.Remove(VODTarget.AlertDisabled, new Action<VODTarget>(VODTarget_AlertDisabled));
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		switch (state)
		{
		case State.INITIALIZING:
		case State.CRASHED:
			break;
		case State.IDLE:
			if (targets.Count > 0)
			{
				nextStream = NextStream();
				PlayPreviouStream();
				state = State.RUNNING;
			}
			break;
		case State.RUNNING:
		{
			if (targets.Count == 0)
			{
				if (!playerBusy)
				{
					player.Stop();
				}
				state = State.IDLE;
				break;
			}
			if (player.isPlaying)
			{
				PositionAudio();
			}
			DateTime serverTime = GorillaComputer.instance.GetServerTime();
			_ = serverTime.DayOfWeek;
			_ = serverTime.Hour;
			int minute = serverTime.Minute;
			if (nextStream != null && !playerBusy && !player.isPlaying && nextStream.Title != string.Empty)
			{
				TimeSpan timeSpan = nextStream.StartTime - serverTime;
				if (timeSpan.TotalMinutes > 0.0 && timeSpan.TotalMinutes <= 60.0)
				{
					for (int i = 0; i < targets.Count; i++)
					{
						if (targets[i].UpNextText != null)
						{
							targets[i].UpNextText.text = $"next: {nextStream.Title} - {timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
						}
					}
				}
			}
			if (minute == lastCheck)
			{
				break;
			}
			lastCheck = minute;
			for (int j = 0; j < schedule.hourly.Length; j++)
			{
				if (schedule.hourly[j].minute - minute == 0 && schedule.hourly[j].IsDateInRange(serverTime))
				{
					StartPlayback(schedule.hourly[j].stream.url, 1.0);
					break;
				}
			}
			break;
		}
		}
	}

	private VODNextStream NextStream()
	{
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		List<VODNextStream> list = new List<VODNextStream>();
		for (int i = 0; i < schedule.hourly.Length; i++)
		{
			if (i == 0)
			{
				list.Add(new VODNextStream(2, schedule.hourly[i].stream.name, new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, serverTime.Hour + 1, schedule.hourly[i].minute, 0)));
			}
			list.Add(new VODNextStream(2, schedule.hourly[i].stream.name, new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, serverTime.Hour, schedule.hourly[i].minute, 0)));
		}
		list.Sort();
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].StartTime > serverTime)
			{
				return list[j];
			}
		}
		return null;
	}

	private void PositionAudio()
	{
		float num = float.MaxValue;
		VODTarget vODTarget = null;
		for (int i = 0; i < targets.Count; i++)
		{
			if (targets[i].AudioSettings.volume > 0f)
			{
				float sqrMagnitude = (VRRig.LocalRig.transform.position - targets[i].transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					vODTarget = targets[i];
					num = sqrMagnitude;
				}
			}
		}
		if (!(vODTarget == null))
		{
			audioSource.transform.parent = vODTarget.transform;
			audioSource.transform.localPosition = Vector3.zero;
			audioSource.volume = vODTarget.AudioSettings.volume;
			audioSource.dopplerLevel = vODTarget.AudioSettings.dopplerLevel;
			audioSource.rolloffMode = vODTarget.AudioSettings.rolloffMode;
			audioSource.minDistance = vODTarget.AudioSettings.minDistance;
			audioSource.maxDistance = vODTarget.AudioSettings.maxDistance;
		}
	}

	private void PlayPreviouStream()
	{
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		int hour = serverTime.Hour;
		int minute = serverTime.Minute;
		DateTime dateTime = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, hour, minute, 0);
		int num = -1;
		for (int i = 0; i < schedule.hourly.Length; i++)
		{
			if (schedule.hourly[i].minute <= minute && schedule.hourly[i].IsDateInRange(serverTime))
			{
				num = i;
			}
		}
		if (num >= 0)
		{
			int num2 = minute - schedule.hourly[num].minute;
			StartPlayback(schedule.hourly[num].stream.url, serverTime.Subtract(dateTime.AddMinutes(-num2)).TotalSeconds);
		}
	}

	private async void StartPlayback(string url, double time = 0.0)
	{
		if (playerBusy)
		{
			return;
		}
		playerBusy = true;
		if (player.isPlaying)
		{
			player.Stop();
		}
		for (int i = 0; i < targets.Count; i++)
		{
			targets[i].Renderer.material = busyMaterial;
			if (targets[i].UpNextText != null)
			{
				targets[i].UpNextText.text = string.Empty;
			}
		}
		try
		{
			player.url = url;
			player.Prepare();
			while (!player.isPrepared && Application.isPlaying)
			{
				await Task.Yield();
			}
			if (time >= player.length || state != State.RUNNING)
			{
				playerBusy = false;
				for (int j = 0; j < targets.Count; j++)
				{
					targets[j].Renderer.material = standbyMaterial;
				}
				return;
			}
			if (time > 0.0)
			{
				player.time = time;
			}
			player.Play();
			PositionAudio();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		for (int k = 0; k < targets.Count; k++)
		{
			targets[k].Renderer.material = playBackMaterial;
		}
		playerBusy = false;
	}

	private void onTD(string s)
	{
		if (s.IsNullOrEmpty())
		{
			Crash("No schedule data");
			return;
		}
		try
		{
			schedule = JsonConvert.DeserializeObject<VODStreamSchedule>(s);
		}
		catch (Exception)
		{
			Crash("Malformed schedule data");
			return;
		}
		if (schedule.hourly.Length == 0)
		{
			Crash("Nothing scheduled in title data");
		}
		else
		{
			waitOnServerTime();
		}
	}

	private void Crash(string msg)
	{
		state = State.CRASHED;
		if (OnCrash != null)
		{
			OnCrash();
		}
		for (int i = 0; i < targets.Count; i++)
		{
			targets[i].gameObject.SetActive(value: false);
		}
	}

	private void onTDError(PlayFabError error)
	{
		Crash(error.ErrorMessage);
	}
}
