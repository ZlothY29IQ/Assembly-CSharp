using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GorillaNetworking;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;
using UnityEngine.Networking;
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
		public string Title;

		public DateTime StartTime;

		public string Url;

		public VODNextStream(string name, DateTime startTime, string url)
		{
			Title = name;
			StartTime = startTime;
			Url = url;
		}

		int IComparable<VODNextStream>.CompareTo(VODNextStream other)
		{
			return (int)(StartTime - other.StartTime).TotalSeconds;
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
		public enum VODStreamType
		{
			VIDEO,
			IMAGE
		}

		public string name;

		public string url;

		public VODStreamType type;

		public int duration;
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
			if (serverTime >= startDT)
			{
				return serverTime <= endDT;
			}
			return false;
		}

		internal DateTime ClampedDateTime(DateTime dateTime)
		{
			if (dateTime < startDT)
			{
				return startDT;
			}
			if (dateTime > endDT)
			{
				return endDT;
			}
			return dateTime;
		}
	}

	private const string PlayerPrefKey_Cache = "_VODCache_";

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

	[SerializeField]
	private Material imageMaterial;

	private List<VODTarget> targets = new List<VODTarget>();

	private int lastCheck;

	private List<string> cache = new List<string>();

	private Coroutine _cr_cacheVOD;

	private bool playerBusy;

	private float imageClearTime;

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
			if (imageClearTime > 0f && imageClearTime < Time.time)
			{
				for (int i = 0; i < targets.Count; i++)
				{
					targets[i].Renderer.material = standbyMaterial;
				}
				imageClearTime = 0f;
			}
			DateTime serverTime = GorillaComputer.instance.GetServerTime();
			_ = serverTime.DayOfWeek;
			_ = serverTime.Hour;
			int minute = serverTime.Minute;
			if (imageClearTime == 0f && nextStream != null && !playerBusy && !player.isPlaying && nextStream.Title != string.Empty)
			{
				TimeSpan timeSpan = nextStream.StartTime - serverTime;
				if (timeSpan.TotalMinutes > 0.0 && timeSpan.TotalMinutes <= 60.0)
				{
					for (int j = 0; j < targets.Count; j++)
					{
						if (targets[j].UpNextText != null)
						{
							targets[j].UpNextText.text = $"next: {nextStream.Title} - {timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
						}
					}
				}
			}
			if (minute == lastCheck)
			{
				break;
			}
			lastCheck = minute;
			for (int k = 0; k < schedule.hourly.Length; k++)
			{
				if (schedule.hourly[k].minute - minute == 0 && schedule.hourly[k].IsDateInRange(serverTime))
				{
					StartPlayback(schedule.hourly[k].stream, 1.0);
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
			DateTime dateTime = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, serverTime.Hour, schedule.hourly[i].minute, 0);
			list.Add(new VODNextStream(schedule.hourly[i].stream.name, schedule.hourly[i].ClampedDateTime(dateTime), schedule.hourly[i].stream.url));
			list.Add(new VODNextStream(schedule.hourly[i].stream.name, schedule.hourly[i].ClampedDateTime(dateTime.AddHours(1.0)), schedule.hourly[i].stream.url));
		}
		list.Sort();
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].StartTime > serverTime)
			{
				cacheVOD(list[j].Url);
				return list[j];
			}
		}
		return null;
	}

	private void cacheVOD(string url)
	{
		string text = UrlToCachePath(url, "mp4");
		if (!File.Exists(text) && _cr_cacheVOD == null)
		{
			_cr_cacheVOD = StartCoroutine(cr_cacheVOD(text, url));
		}
	}

	private string UrlToCachePath(string url, string extension)
	{
		return Application.persistentDataPath + Path.DirectorySeparatorChar + $"V{url.GetHashCode():X}.{extension}";
	}

	private IEnumerator cr_cacheVOD(string file, string url)
	{
		UnityWebRequest www = new UnityWebRequest(url)
		{
			downloadHandler = new DownloadHandlerBuffer()
		};
		yield return www.SendWebRequest();
		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError("VOD :: error :: " + www.error);
		}
		else
		{
			File.WriteAllBytes(file, www.downloadHandler.data);
			cache.Add(file);
			PlayerPrefs.SetString("_VODCache_", JsonConvert.SerializeObject(cache));
		}
		_cr_cacheVOD = null;
	}

	private void Start()
	{
		cache = new List<string>();
		string @string = PlayerPrefs.GetString("_VODCache_");
		if (@string.IsNullOrEmpty())
		{
			return;
		}
		List<string> list = JsonConvert.DeserializeObject<List<string>>(@string);
		for (int i = 0; i < list.Count; i++)
		{
			if (File.Exists(list[i]))
			{
				if ((DateTime.Now - File.GetCreationTime(list[i])).TotalDays > 30.0)
				{
					File.Delete(list[i]);
				}
				else
				{
					cache.Add(list[i]);
				}
			}
		}
		PlayerPrefs.SetString("_VODCache_", JsonConvert.SerializeObject(cache));
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
			audioSource.transform.position = vODTarget.transform.position;
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
		Debug.Log($"VOD :: serverTime={serverTime}");
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
			StartPlayback(schedule.hourly[num].stream, serverTime.Subtract(dateTime.AddMinutes(-num2)).TotalSeconds);
		}
	}

	private void StartPlayback(VODStream str, double time = 0.0)
	{
		imageClearTime = 0f;
		switch (str.type)
		{
		case VODStream.VODStreamType.VIDEO:
			Debug.Log("VOD :: StartVideoPlayback :: go");
			StartVideoPlayback(str.url, time);
			break;
		case VODStream.VODStreamType.IMAGE:
			Debug.Log("VOD :: StartImagePlayback :: go");
			StartImagePlayback(str.url, str.duration, time);
			break;
		}
	}

	private async void StartImagePlayback(string url, int duration, double time = 0.0)
	{
		duration -= (int)time;
		if (duration <= 0)
		{
			return;
		}
		for (int i = 0; i < targets.Count; i++)
		{
			targets[i].Renderer.material = busyMaterial;
			if (targets[i].UpNextText != null)
			{
				targets[i].UpNextText.text = string.Empty;
			}
		}
		imageClearTime = Time.time + (float)duration;
		string file = UrlToCachePath(url, "png");
		UnityWebRequest www = ((!File.Exists(file)) ? new UnityWebRequest(url) : new UnityWebRequest(file));
		DownloadHandlerTexture downloadHandlerTexture = (DownloadHandlerTexture)(www.downloadHandler = new DownloadHandlerTexture());
		await www.SendWebRequest();
		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError("VOD :: error :: " + www.error + " :: " + downloadHandlerTexture.error);
			for (int j = 0; j < targets.Count; j++)
			{
				targets[j].Renderer.material = standbyMaterial;
			}
			return;
		}
		imageMaterial.mainTexture = downloadHandlerTexture.texture;
		for (int k = 0; k < targets.Count; k++)
		{
			targets[k].Renderer.material = imageMaterial;
		}
		if (!File.Exists(file))
		{
			File.WriteAllBytes(file, www.downloadHandler.data);
			cache.Add(file);
			PlayerPrefs.SetString("_VODCache_", JsonConvert.SerializeObject(cache));
		}
	}

	private async void StartVideoPlayback(string url, double time = 0.0)
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
			string text = UrlToCachePath(url, "mp4");
			if (File.Exists(text))
			{
				player.url = text;
			}
			else
			{
				player.url = url;
			}
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
			for (int i = 0; i < schedule.hourly.Length; i++)
			{
				schedule.hourly[i].ValidateDate();
			}
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
		Debug.LogError("VOD :: CRASHED :: " + msg);
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
