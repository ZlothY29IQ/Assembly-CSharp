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
	private enum State
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
		public VODWeeklyStream[] weekly;

		public VODDailyStream[] daily;

		public VODHourlyStream[] hourly;
	}

	[Serializable]
	public struct VODStream
	{
		public string name;

		public string url;
	}

	[Serializable]
	public struct VODWeeklyStream : IComparable<VODWeeklyStream>
	{
		public VODStream stream;

		[Range(0f, 6f)]
		public int day;

		[Range(0f, 23f)]
		public int hour;

		[Range(0f, 59f)]
		public int minute;

		public int CompareTo(VODWeeklyStream other)
		{
			return day + hour + minute - (other.day + other.hour + other.minute);
		}
	}

	[Serializable]
	public struct VODDailyStream : IComparable<VODDailyStream>
	{
		public VODStream stream;

		[Range(0f, 23f)]
		public int hour;

		[Range(0f, 59f)]
		public int minute;

		public int CompareTo(VODDailyStream other)
		{
			return hour + minute - (other.hour + other.minute);
		}
	}

	[Serializable]
	public struct VODHourlyStream : IComparable<VODHourlyStream>
	{
		public VODStream stream;

		[Range(0f, 59f)]
		public int minute;

		public int CompareTo(VODHourlyStream other)
		{
			return minute - other.minute;
		}
	}

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

	private State state;

	private bool playerBusy;

	private int currentStreamPrio;

	public async void OnEnable()
	{
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
			currentStreamPrio = 0;
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
			int dayOfWeek = (int)serverTime.DayOfWeek;
			int hour = serverTime.Hour;
			int minute = serverTime.Minute;
			if (nextStream != null && !playerBusy && !player.isPlaying && nextStream.Title != string.Empty)
			{
				TimeSpan timeSpan = nextStream.StartTime - serverTime;
				if (timeSpan.TotalSeconds > 0.0 && timeSpan.TotalSeconds <= 3600.0)
				{
					for (int i = 0; i < targets.Count; i++)
					{
						if (targets[i].UpNextText != null)
						{
							targets[i].UpNextText.text = $"next: {nextStream.Title} - {timeSpan.TotalSeconds / 60.0:00}:{timeSpan.TotalSeconds % 60.0:00}";
						}
					}
				}
			}
			if (minute == lastCheck)
			{
				break;
			}
			lastCheck = minute;
			for (int j = 0; j < schedule.weekly.Length; j++)
			{
				if (1440 * (schedule.weekly[j].day - dayOfWeek) + 60 * (schedule.weekly[j].hour - hour) + (schedule.weekly[j].minute - minute) == 0)
				{
					StartPlayback(schedule.weekly[j].stream.url, 3);
					break;
				}
			}
			for (int k = 0; k < schedule.daily.Length; k++)
			{
				if (60 * (schedule.daily[k].hour - hour) + (schedule.daily[k].minute - minute) == 0)
				{
					StartPlayback(schedule.daily[k].stream.url, 2);
					break;
				}
			}
			for (int l = 0; l < schedule.hourly.Length; l++)
			{
				if (schedule.hourly[l].minute - minute == 0)
				{
					StartPlayback(schedule.hourly[l].stream.url, 1);
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
		for (int i = 0; i < schedule.weekly.Length; i++)
		{
			if (i == 0)
			{
				list.Add(new VODNextStream(3, schedule.weekly[i].stream.name, new DateTime(serverTime.Year, serverTime.Month, (int)(7 + serverTime.Day + (schedule.weekly[i].day - serverTime.DayOfWeek)), schedule.weekly[i].hour, schedule.weekly[i].minute, 0)));
			}
			list.Add(new VODNextStream(3, schedule.weekly[i].stream.name, new DateTime(serverTime.Year, serverTime.Month, (int)(serverTime.Day + (schedule.weekly[i].day - serverTime.DayOfWeek)), schedule.weekly[i].hour, schedule.weekly[i].minute, 0)));
		}
		for (int j = 0; j < schedule.daily.Length; j++)
		{
			if (j == 0)
			{
				list.Add(new VODNextStream(2, schedule.daily[j].stream.name, new DateTime(serverTime.Year, serverTime.Month, serverTime.Day + 1, schedule.daily[j].hour, schedule.daily[j].minute, 0)));
			}
			list.Add(new VODNextStream(2, schedule.daily[j].stream.name, new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, schedule.daily[j].hour, schedule.daily[j].minute, 0)));
		}
		for (int k = 0; k < schedule.hourly.Length; k++)
		{
			if (k == 0)
			{
				list.Add(new VODNextStream(2, schedule.hourly[k].stream.name, new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, serverTime.Hour + 1, schedule.hourly[k].minute, 0)));
			}
			list.Add(new VODNextStream(2, schedule.hourly[k].stream.name, new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, serverTime.Hour, schedule.hourly[k].minute, 0)));
		}
		list.Sort();
		for (int l = 0; l < list.Count; l++)
		{
			if (list[l].StartTime > serverTime)
			{
				return list[l];
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
			audioSource.spread = vODTarget.AudioSettings.spread;
			audioSource.rolloffMode = vODTarget.AudioSettings.rolloffMode;
			audioSource.minDistance = vODTarget.AudioSettings.minDistance;
			audioSource.maxDistance = vODTarget.AudioSettings.maxDistance;
		}
	}

	private void PlayPreviouStream()
	{
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		int dayOfWeek = (int)serverTime.DayOfWeek;
		int hour = serverTime.Hour;
		int minute = serverTime.Minute;
		DateTime dateTime = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, hour, minute, 0);
		int num = -1;
		int num2 = -1;
		int num3 = -1;
		for (int i = 0; i < schedule.weekly.Length; i++)
		{
			if (schedule.weekly[i].day <= dayOfWeek && schedule.weekly[i].hour <= hour && schedule.weekly[i].minute <= minute)
			{
				num = i;
			}
		}
		for (int j = 0; j < schedule.daily.Length; j++)
		{
			if (schedule.daily[j].hour <= hour && schedule.daily[j].minute <= minute)
			{
				num2 = j;
			}
		}
		for (int k = 0; k < schedule.hourly.Length; k++)
		{
			if (schedule.hourly[k].minute <= minute)
			{
				num3 = k;
			}
		}
		int num4 = int.MaxValue;
		int num5 = int.MaxValue;
		int num6 = int.MaxValue;
		if (num >= 0)
		{
			int num7 = 1440 * (dayOfWeek - schedule.weekly[num].day) + 60 * (hour - schedule.weekly[num].hour) + (minute - schedule.weekly[num].minute);
			if (num7 < num4)
			{
				num4 = num7;
			}
		}
		else if (schedule.weekly.Length != 0)
		{
			num = schedule.weekly.Length;
			int num8 = 10080 - (1440 * (dayOfWeek - schedule.weekly[num].day) + 60 * (hour - schedule.weekly[num].hour) + (minute - schedule.weekly[num].minute));
			if (num8 < num4)
			{
				num4 = num8;
			}
		}
		if (num2 >= 0)
		{
			int num9 = 60 * (hour - schedule.daily[num2].hour) + (minute - schedule.daily[num2].minute);
			if (num9 < num5)
			{
				num5 = num9;
			}
		}
		else if (schedule.daily.Length != 0)
		{
			num2 = schedule.daily.Length - 1;
			int num10 = 1440 - (60 * (hour - schedule.daily[num2].hour) + (minute - schedule.daily[num2].minute));
			if (num10 < num5)
			{
				num5 = num10;
			}
		}
		if (num3 >= 0)
		{
			int num11 = minute - schedule.hourly[num3].minute;
			if (num11 < num6)
			{
				num6 = num11;
			}
		}
		else if (schedule.daily.Length != 0)
		{
			num3 = schedule.hourly.Length - 1;
			int num12 = 60 - (minute - schedule.hourly[num3].minute);
			if (num12 < num6)
			{
				num6 = num12;
			}
		}
		if (num3 >= 0 && num6 < num5 && num6 < num4)
		{
			StartPlayback(schedule.hourly[num3].stream.url, 1, serverTime.Subtract(dateTime.AddMinutes(-num6)).TotalSeconds);
		}
		else if (num2 >= 0 && num5 < num4)
		{
			StartPlayback(schedule.hourly[num2].stream.url, 2, serverTime.Subtract(dateTime.AddMinutes(-num5)).TotalSeconds);
		}
		else if (num >= 0)
		{
			StartPlayback(schedule.hourly[num].stream.url, 3, serverTime.Subtract(dateTime.AddMinutes(-num4)).TotalSeconds);
		}
	}

	private async void StartPlayback(string url, int priority, double time = 0.0)
	{
		if (playerBusy)
		{
			return;
		}
		playerBusy = true;
		if (player.isPlaying)
		{
			if (priority <= currentStreamPrio)
			{
				playerBusy = false;
				return;
			}
			player.Stop();
		}
		currentStreamPrio = priority;
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
		if (schedule.weekly.Length + schedule.daily.Length + schedule.hourly.Length == 0)
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
