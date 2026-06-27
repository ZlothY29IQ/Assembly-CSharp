using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GorillaNetworking;
using KID.Model;
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

	public struct VODNextStreamData(string title, DateTime startTime)
	{
		public string Title = title;

		public DateTime StartTime = startTime;
	}

	[Serializable]
	public struct VODStreamSchedule
	{
		public VODHourlyStream[] hourly;

		internal void Merge(VODStreamSchedule subSchedule)
		{
			List<VODHourlyStream> list = new List<VODHourlyStream>();
			if (hourly != null)
			{
				list.AddRange(hourly);
			}
			for (int i = 0; i < subSchedule.hourly.Length; i++)
			{
				list.Add(subSchedule.hourly[i]);
				int num = 0;
				while (subSchedule.hourly[i].repeats != null && num < subSchedule.hourly[i].repeats.Length)
				{
					VODHourlyStream item = default(VODHourlyStream);
					item.stream = subSchedule.hourly[i].stream;
					item.minute = subSchedule.hourly[i].repeats[num];
					item.startDateTime = subSchedule.hourly[i].startDateTime;
					item.endDateTime = subSchedule.hourly[i].endDateTime;
					item.ValidateDate();
					list.Add(item);
					num++;
				}
			}
			list.Sort();
			hourly = list.ToArray();
		}
	}

	[Serializable]
	public struct VODStream
	{
		public enum VODStreamType
		{
			VIDEO,
			IMAGE
		}

		public enum VODStreamChannel
		{
			DEFAULT,
			VIM,
			MM,
			GCORP,
			EVENT,
			FEATURED
		}

		public string name;

		public bool hideUpNext;

		public string id;

		[Obsolete]
		public string url;

		public VODStreamType type;

		public int duration;

		public VODStreamChannel ch;

		public string displayTitle
		{
			get
			{
				if (!hideUpNext)
				{
					return name;
				}
				return string.Empty;
			}
		}
	}

	[Serializable]
	public struct VODHourlyStream : IComparable<VODHourlyStream>
	{
		public VODStream stream;

		[Range(0f, 59f)]
		public int minute;

		[Range(0f, 59f)]
		public int[] repeats;

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

	private static Material _standbyMaterial;

	private const string PlayerPrefKey_Cache = "_VODCache_";

	public static Action OnCrash;

	public static State state;

	private VideoPlayer player;

	private AudioSource audioSource;

	private VODStreamSchedule schedule;

	[SerializeField]
	private string[] titleDataKey;

	[SerializeField]
	private Material standbyMaterial;

	[SerializeField]
	private Material playBackMaterial;

	[SerializeField]
	private Material busyMaterial;

	[SerializeField]
	private Material imageMaterial;

	[SerializeField]
	private VODStream.VODStreamChannel[] voiceChatPermRequired;

	private List<VODTarget> targets = new List<VODTarget>();

	private List<VODStream.VODStreamChannel> voiceChatPermRequiredList;

	private int lastCheck;

	private List<string> cache = new List<string>();

	private bool playerBusy;

	private VODStream.VODStreamChannel playerChannel;

	private int tdGot;

	private Permission voiceChatPerm;

	public static Material StandbyMaterial => _standbyMaterial;

	private void Awake()
	{
		_standbyMaterial = standbyMaterial;
	}

	public async void OnEnable()
	{
		state = State.INITIALIZING;
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		VODTarget.AlertEnabled = (Action<VODTarget>)Delegate.Combine(VODTarget.AlertEnabled, new Action<VODTarget>(VODTarget_AlertEnabled));
		VODTarget.AlertDisabled = (Action<VODTarget>)Delegate.Combine(VODTarget.AlertDisabled, new Action<VODTarget>(VODTarget_AlertDisabled));
		if (voiceChatPermRequiredList == null)
		{
			voiceChatPermRequiredList = new List<VODStream.VODStreamChannel>(voiceChatPermRequired);
		}
		if (player == null)
		{
			player = GetComponent<VideoPlayer>();
			player.loopPointReached += Player_loopPointReached;
			audioSource = GetComponentInChildren<AudioSource>();
			while (PlayFabTitleDataCache.Instance == null)
			{
				await Task.Yield();
			}
			tdGot = 0;
			for (int i = 0; i < titleDataKey.Length; i++)
			{
				PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey[i], onTD, onTDError);
			}
			waitOnServerTimeAndSchedule();
		}
	}

	private async void waitOnServerTimeAndSchedule()
	{
		while (tdGot < titleDataKey.Length || GorillaComputer.instance == null || GorillaComputer.instance.GetServerTime().Year < 2000)
		{
			await Task.Yield();
		}
		if (schedule.hourly.Length == 0)
		{
			state = State.CRASHED;
			if (OnCrash != null)
			{
				OnCrash();
			}
			Debug.LogError("VOD :: CRASHED :: Nothing Scheduled");
			for (int i = 0; i < targets.Count; i++)
			{
				targets[i].ShowStatic(on: true);
			}
		}
		state = State.IDLE;
	}

	private Material getStandby(VODTarget o)
	{
		if (!(o.StandbyOverride == null))
		{
			return o.StandbyOverride;
		}
		return standbyMaterial;
	}

	private void VODTarget_AlertEnabled(VODTarget o)
	{
		if (targets.Contains(o))
		{
			return;
		}
		VODStream.VODStreamChannel[] priorityChannelArray = getPriorityChannelArray();
		targets.Add(o);
		bool flag = !Enumerable.SequenceEqual(priorityChannelArray, getPriorityChannelArray());
		if (state == State.RUNNING && player.isPlaying && o.VerifyChannel(playerChannel))
		{
			o.Renderer.material = playBackMaterial;
			return;
		}
		o.Renderer.material = getStandby(o);
		o.SetNext(GetNextStream(o.Channel));
		if ((!player.isPlaying && targets.Count == 1) || flag)
		{
			PlayPreviouStream();
		}
	}

	private void VODTarget_AlertDisabled(VODTarget o)
	{
		if (!targets.Contains(o))
		{
			return;
		}
		VODStream.VODStreamChannel[] priorityChannelArray = getPriorityChannelArray();
		targets.Remove(o);
		bool flag = !Enumerable.SequenceEqual(priorityChannelArray, getPriorityChannelArray());
		o.Renderer.material = ((o.StandbyOverride == null) ? standbyMaterial : o.StandbyOverride);
		o.ClearNext();
		if (!playerBusy)
		{
			if (player.isPlaying && (targets.Count == 0 || flag))
			{
				player.Stop();
			}
			if (targets.Count > 0 && flag)
			{
				PlayPreviouStream();
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
				targets[i].Renderer.material = getStandby(targets[i]);
				targets[i].SetNext(GetNextStream(targets[i].Channel));
			}
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
			if (minute == lastCheck)
			{
				break;
			}
			lastCheck = minute;
			List<VODStream> list = new List<VODStream>();
			for (int i = 0; i < schedule.hourly.Length; i++)
			{
				if (schedule.hourly[i].minute - minute == 0 && schedule.hourly[i].IsDateInRange(serverTime))
				{
					list.Add(schedule.hourly[i].stream);
				}
			}
			if (list.Count == 0)
			{
				break;
			}
			if (list.Count == 1)
			{
				StartPlayback(list[0], 1.0);
				break;
			}
			List<VODStream.VODStreamChannel> priorityChannels = getPriorityChannels();
			for (int j = 0; j < list.Count; j++)
			{
				if (priorityChannels.Contains(list[j].ch))
				{
					StartPlayback(list[j], 1.0);
					break;
				}
			}
			break;
		}
		}
	}

	private List<VODStream.VODStreamChannel> getPriorityChannels()
	{
		return new List<VODStream.VODStreamChannel>(getPriorityChannelArray());
	}

	private VODStream.VODStreamChannel[] getPriorityChannelArray()
	{
		float num = float.MaxValue;
		VODTarget vODTarget = null;
		for (int i = 0; i < targets.Count; i++)
		{
			if (targets[i].Distance < num)
			{
				num = targets[i].Distance;
				vODTarget = targets[i];
			}
		}
		if (vODTarget != null)
		{
			return vODTarget.Channel;
		}
		return new VODStream.VODStreamChannel[0];
	}

	private bool CheckForCachedFile(string fileId, string extension, out string filePath)
	{
		string path = $"V{fileId.GetHashCode():X}.{extension}";
		filePath = Path.Combine(Application.persistentDataPath, path);
		if (File.Exists(filePath))
		{
			return true;
		}
		string text = Path.Combine(Application.persistentDataPath, "GTv_Cache");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		filePath = Path.Combine(text, path);
		return File.Exists(filePath);
	}

	private async Task<string> GetCachedFile(string url, string fileId, string extension)
	{
		if (CheckForCachedFile(fileId, extension, out var filePath))
		{
			return filePath;
		}
		UnityWebRequest www = new UnityWebRequest(url)
		{
			downloadHandler = new DownloadHandlerBuffer()
		};
		await www.SendWebRequest();
		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError("VOD :: error :: " + www.error);
			return null;
		}
		File.WriteAllBytes(filePath, www.downloadHandler.data);
		cache.Add(filePath);
		PlayerPrefs.SetString("_VODCache_", JsonConvert.SerializeObject(cache));
		return filePath;
	}

	private void Start()
	{
		cache = new List<string>();
		string text = PlayerPrefs.GetString("_VODCache_");
		if (text.IsNullOrEmpty())
		{
			return;
		}
		List<string> list = JsonConvert.DeserializeObject<List<string>>(text);
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
			if (targets[i].AudioSettings.volume > 0f && targets[i].VerifyChannel(playerChannel))
			{
				float sqrMagnitude = (VRRig.LocalRig.transform.position - targets[i].transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					vODTarget = targets[i];
					num = sqrMagnitude;
				}
			}
		}
		if (vODTarget == null)
		{
			audioSource.volume = 0f;
			return;
		}
		audioSource.transform.position = vODTarget.transform.position;
		if (voiceChatPerm == null)
		{
			voiceChatPerm = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Voice_Chat);
		}
		audioSource.volume = ((!voiceChatPermRequiredList.Contains(playerChannel) || voiceChatPerm.Enabled) ? vODTarget.AudioSettings.volume : 0f);
		audioSource.dopplerLevel = vODTarget.AudioSettings.dopplerLevel;
		audioSource.rolloffMode = vODTarget.AudioSettings.rolloffMode;
		audioSource.minDistance = vODTarget.AudioSettings.minDistance;
		audioSource.maxDistance = vODTarget.AudioSettings.maxDistance;
	}

	private void PlayPreviouStream()
	{
		DateTime serverTime = GorillaComputer.instance.GetServerTime();
		int hour = serverTime.Hour;
		int minute = serverTime.Minute;
		DateTime dateTime = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, hour, minute, 0);
		int num = -1;
		List<VODStream.VODStreamChannel> priorityChannels = getPriorityChannels();
		for (int i = 0; i < schedule.hourly.Length; i++)
		{
			if (priorityChannels.Contains(schedule.hourly[i].stream.ch) && schedule.hourly[i].minute <= minute && schedule.hourly[i].IsDateInRange(serverTime))
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
		switch (str.type)
		{
		case VODStream.VODStreamType.VIDEO:
		{
			if (str.id.IsNullOrEmpty())
			{
				StartVideoPlayback(str.url, str.url, str.ch, time, null);
				break;
			}
			if (CheckForCachedFile(str.id, "mp4", out var filePath2))
			{
				StartVideoPlayback(filePath2, str.id, str.ch, time, filePath2);
				break;
			}
			MothershipClientApiUnity.GetDLCFileDetails(str.id, delegate(SharedDownloadableFileResult result)
			{
				StartVideoPlayback(result.url, str.id, str.ch, time, null);
			}, delegate(MothershipError error, int status)
			{
				Debug.Log("VOD: FILE FETCH FAILED Mothership Error: " + error.MothershipErrorCode + " Trace ID: " + error.TraceId + " Message: " + error.Message);
				if (!str.url.IsNullOrEmpty())
				{
					StartVideoPlayback(str.url, str.url, str.ch, time, null);
				}
			});
			break;
		}
		case VODStream.VODStreamType.IMAGE:
		{
			if (str.id.IsNullOrEmpty())
			{
				StartImagePlayback(str.url, str.url, str.duration, str.ch, time, null);
				break;
			}
			if (CheckForCachedFile(str.id, "mp4", out var filePath))
			{
				StartImagePlayback(filePath, str.id, str.duration, str.ch, time, filePath);
				break;
			}
			MothershipClientApiUnity.GetDLCFileDetails(str.id, delegate(SharedDownloadableFileResult result)
			{
				StartImagePlayback(result.url, str.id, str.duration, str.ch, time, null);
			}, delegate(MothershipError error, int status)
			{
				Debug.Log("VOD: FILE FETCH FAILED Mothership Error: " + error.MothershipErrorCode + " Trace ID: " + error.TraceId + " Message: " + error.Message);
				if (!str.url.IsNullOrEmpty())
				{
					StartImagePlayback(str.url, str.url, str.duration, str.ch, time, null);
				}
			});
			break;
		}
		}
	}

	private async void StartImagePlayback(string url, string fileId, int duration, VODStream.VODStreamChannel ch, double time, string cachedUrl)
	{
		duration -= (int)time;
		if (duration <= 0)
		{
			return;
		}
		List<VODTarget> imageTargets = new List<VODTarget>();
		for (int i = 0; i < targets.Count; i++)
		{
			if (targets[i].VerifyChannel(ch))
			{
				imageTargets.Add(targets[i]);
				targets[i].Renderer.material = busyMaterial;
				targets[i].ClearNext();
			}
		}
		if (cachedUrl == null)
		{
			cachedUrl = await GetCachedFile(url, fileId, "png");
		}
		if (cachedUrl == null)
		{
			Debug.LogError("VOD :: cache error :: " + url);
			for (int j = 0; j < imageTargets.Count; j++)
			{
				imageTargets[j].Renderer.material = getStandby(imageTargets[j]);
			}
			return;
		}
		UnityWebRequest www = new UnityWebRequest(cachedUrl);
		DownloadHandlerTexture downloadHandlerTexture = (DownloadHandlerTexture)(www.downloadHandler = new DownloadHandlerTexture());
		await www.SendWebRequest();
		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError("VOD :: error :: " + www.error + " :: " + downloadHandlerTexture.error);
			for (int k = 0; k < imageTargets.Count; k++)
			{
				imageTargets[k].Renderer.material = getStandby(imageTargets[k]);
			}
			return;
		}
		imageMaterial.mainTexture = downloadHandlerTexture.texture;
		for (int l = 0; l < imageTargets.Count; l++)
		{
			imageTargets[l].Renderer.material = imageMaterial;
		}
		await Task.Delay(duration * 1000);
		for (int m = 0; m < imageTargets.Count; m++)
		{
			if (imageTargets[m].Renderer.material == imageMaterial)
			{
				imageTargets[m].Renderer.material = getStandby(imageTargets[m]);
			}
		}
	}

	private async void StartVideoPlayback(string url, string fileId, VODStream.VODStreamChannel ch, double time, string cachedUrl)
	{
		if (playerBusy)
		{
			return;
		}
		playerBusy = true;
		if (player.isPlaying)
		{
			if (!getPriorityChannels().Contains(ch))
			{
				playerBusy = false;
				return;
			}
			player.Stop();
		}
		for (int i = 0; i < targets.Count; i++)
		{
			if (targets[i].VerifyChannel(ch))
			{
				targets[i].Renderer.material = busyMaterial;
				targets[i].ClearNext();
			}
		}
		try
		{
			if (cachedUrl == null)
			{
				cachedUrl = await GetCachedFile(url, fileId, "mp4");
			}
			if (cachedUrl == null)
			{
				Debug.LogError("VOD :: cache error :: " + url);
				playerBusy = false;
				return;
			}
			player.url = cachedUrl;
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
					targets[j].Renderer.material = getStandby(targets[j]);
				}
				return;
			}
			if (time > 0.0)
			{
				player.time = time;
			}
			player.Play();
			playerChannel = ch;
			PositionAudio();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		for (int k = 0; k < targets.Count; k++)
		{
			if (targets[k].VerifyChannel(ch))
			{
				targets[k].Renderer.material = playBackMaterial;
			}
		}
		playerBusy = false;
	}

	private void onTD(string s)
	{
		tdGot++;
		if (s.IsNullOrEmpty())
		{
			Debug.LogError("Crash(\"No schedule data\")");
			return;
		}
		VODStreamSchedule subSchedule = default(VODStreamSchedule);
		try
		{
			subSchedule = JsonConvert.DeserializeObject<VODStreamSchedule>(s);
		}
		catch (Exception ex)
		{
			Debug.LogError("Crash(\"Malformed schedule data\") :: " + ex.Message + " :: " + s);
		}
		for (int i = 0; i < subSchedule.hourly.Length; i++)
		{
			subSchedule.hourly[i].ValidateDate();
		}
		schedule.Merge(subSchedule);
	}

	private void onTDError(PlayFabError error)
	{
		tdGot++;
		Debug.LogError("TD Error: " + error.ErrorMessage);
	}

	private VODNextStreamData GetNextStream(VODStream.VODStreamChannel[] ch)
	{
		return GetNextStream(ch, (GorillaComputer.instance == null || GorillaComputer.instance.GetServerTime().Year < 2000) ? DateTime.UtcNow : GorillaComputer.instance.GetServerTime());
	}

	private VODNextStreamData GetNextStream(VODStream.VODStreamChannel[] ch, DateTime now)
	{
		List<VODStream.VODStreamChannel> list = new List<VODStream.VODStreamChannel>(ch);
		for (int i = 0; i < schedule.hourly.Length; i++)
		{
			if (!schedule.hourly[i].stream.hideUpNext && list.Contains(schedule.hourly[i].stream.ch) && schedule.hourly[i].minute > now.Minute)
			{
				return new VODNextStreamData(startTime: new DateTime(now.Year, now.Month, now.Day, now.Hour, schedule.hourly[i].minute, 0), title: schedule.hourly[i].stream.name);
			}
		}
		for (int j = 0; j < schedule.hourly.Length; j++)
		{
			if (!schedule.hourly[j].stream.hideUpNext && list.Contains(schedule.hourly[j].stream.ch) && schedule.hourly[j].minute < now.Minute)
			{
				DateTime startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, schedule.hourly[j].minute, 0).AddHours(1.0);
				return new VODNextStreamData(schedule.hourly[j].stream.name, startTime);
			}
		}
		return new VODNextStreamData(string.Empty, DateTime.MinValue);
	}

	public static string[] GetSchedule(VODStreamSchedule schedule, VODStream.VODStreamChannel[] ch)
	{
		return GetSchedule(schedule, ch, (GorillaComputer.instance == null || GorillaComputer.instance.GetServerTime().Year < 2000) ? DateTime.UtcNow : GorillaComputer.instance.GetServerTime());
	}

	public static string[] GetSchedule(VODStreamSchedule schedule, VODStream.VODStreamChannel[] ch, DateTime now)
	{
		List<string> list = new List<string>();
		List<VODStream.VODStreamChannel> list2 = new List<VODStream.VODStreamChannel>(ch);
		for (int i = 0; i < schedule.hourly.Length; i++)
		{
			if (!schedule.hourly[i].stream.hideUpNext && list2.Contains(schedule.hourly[i].stream.ch) && schedule.hourly[i].minute > now.Minute)
			{
				list.Add(new DateTime(now.Year, now.Month, now.Day, now.Hour, schedule.hourly[i].minute, 0).ToShortTimeString() + " --- " + schedule.hourly[i].stream.name);
			}
		}
		for (int j = 0; j < schedule.hourly.Length; j++)
		{
			if (!schedule.hourly[j].stream.hideUpNext && list2.Contains(schedule.hourly[j].stream.ch) && schedule.hourly[j].minute < now.Minute)
			{
				list.Add(new DateTime(now.Year, now.Month, now.Day, now.Hour, schedule.hourly[j].minute, 0).AddHours(1.0).ToShortTimeString() + " --- " + schedule.hourly[j].stream.name);
			}
		}
		return list.ToArray();
	}

	public static Dictionary<VODStream.VODStreamChannel, List<string>> GetSchedule(VODStreamSchedule schedule)
	{
		return GetSchedule(schedule, (GorillaComputer.instance == null || GorillaComputer.instance.GetServerTime().Year < 2000) ? DateTime.UtcNow : GorillaComputer.instance.GetServerTime());
	}

	public static Dictionary<VODStream.VODStreamChannel, List<string>> GetSchedule(VODStreamSchedule schedule, DateTime now)
	{
		Dictionary<VODStream.VODStreamChannel, List<string>> dictionary = new Dictionary<VODStream.VODStreamChannel, List<string>>();
		for (int i = 0; i < schedule.hourly.Length; i++)
		{
			if (!schedule.hourly[i].stream.hideUpNext && schedule.hourly[i].minute > now.Minute)
			{
				DateTime dateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, schedule.hourly[i].minute, 0);
				if (!dictionary.ContainsKey(schedule.hourly[i].stream.ch))
				{
					dictionary.Add(schedule.hourly[i].stream.ch, new List<string>());
				}
				dictionary[schedule.hourly[i].stream.ch].Add(dateTime.ToShortTimeString() + " --- " + schedule.hourly[i].stream.name);
			}
		}
		for (int j = 0; j < schedule.hourly.Length; j++)
		{
			if (!schedule.hourly[j].stream.hideUpNext && schedule.hourly[j].minute < now.Minute)
			{
				DateTime dateTime2 = new DateTime(now.Year, now.Month, now.Day, now.Hour, schedule.hourly[j].minute, 0).AddHours(1.0);
				if (!dictionary.ContainsKey(schedule.hourly[j].stream.ch))
				{
					dictionary.Add(schedule.hourly[j].stream.ch, new List<string>());
				}
				dictionary[schedule.hourly[j].stream.ch].Add(dateTime2.ToShortTimeString() + " --- " + schedule.hourly[j].stream.name);
			}
		}
		return dictionary;
	}
}
