using System;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTag;
using Sirenix.OdinInspector;
using UnityEngine;

public class SynchedMusicController : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	public struct SyncedSongInfo
	{
		[Tooltip("A layer for a song. For no layers, just add a single entry.")]
		[RequiredListLength(1, null)]
		public SyncedSongLayerInfo[] songLayers;
	}

	[Serializable]
	public struct SyncedSongLayerInfo
	{
		[Tooltip("The clip that will be played.")]
		public AudioClip audioClip;

		public AudioSourcePickMode audioSourcePickMode;

		[Tooltip("The audio sources that should play the audio clip.")]
		public AudioSource[] audioSources;
	}

	public enum AudioSourcePickMode
	{
		All,
		Shuffle,
		Specific
	}

	[SerializeField]
	private bool usingNewSyncedSongsCode;

	[SerializeField]
	private bool shufflePlaylist = true;

	[SerializeField]
	private SyncedSongInfo[] syncedSongs;

	[Tooltip("This should be unique per sound post. Sound posts that share the same seed and the same song count will play songs a the same times.")]
	public int mySeed;

	private System.Random randomNumberGenerator = new System.Random();

	[Tooltip("In milliseconds.")]
	public long minimumWait = 900000L;

	[Tooltip("In milliseconds. A random value between 0 and this will be picked. The max wait time is randomInterval + minimumWait.")]
	public int randomInterval = 600000;

	[DebugReadout]
	public long[] songStartTimes;

	[DebugReadout]
	public int[] audioSourcesForPlaying;

	[DebugReadout]
	public int[] audioClipsForPlaying;

	public AudioSource audioSource;

	public AudioSource[] audioSourceArray;

	public AudioClip[] songsArray;

	[DebugReadout]
	public int lastPlayIndex;

	[DebugReadout]
	public long currentTime;

	[DebugReadout]
	public long totalLoopTime;

	public GorillaPressableButton muteButton;

	public GorillaPressableButton[] muteButtons;

	public bool usingMultipleSongs;

	public bool usingMultipleSources;

	[DebugReadout]
	public bool isPlayingCurrently;

	[DebugReadout]
	public bool testPlay;

	public bool twoLayer;

	[Tooltip("Used to store the muted sound posts in player prefs.")]
	public string locationName;

	private const int kPlaylistLength = 256;

	private void Start()
	{
		if (usingNewSyncedSongsCode)
		{
			New_Start();
			return;
		}
		totalLoopTime = 0L;
		AudioSource[] array = audioSourceArray;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].mute = PlayerPrefs.GetInt(locationName + "Muted", 0) != 0;
		}
		audioSource.mute = PlayerPrefs.GetInt(locationName + "Muted", 0) != 0;
		muteButton.isOn = audioSource.mute;
		muteButton.UpdateColor();
		for (int j = 0; j < muteButtons.Length; j++)
		{
			muteButtons[j].isOn = audioSource.mute;
			muteButtons[j].UpdateColor();
		}
		randomNumberGenerator = new System.Random(mySeed);
		GenerateSongStartRandomTimes();
		if (twoLayer)
		{
			array = audioSourceArray;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].clip.LoadAudioData();
			}
		}
	}

	public void SliceUpdate()
	{
		if (usingNewSyncedSongsCode)
		{
			New_Update();
		}
		else
		{
			if (GorillaComputer.instance.startupMillis == 0L || totalLoopTime == 0L || songStartTimes.Length == 0)
			{
				return;
			}
			isPlayingCurrently = audioSource.isPlaying;
			if (testPlay)
			{
				testPlay = false;
				if (usingMultipleSources && usingMultipleSongs)
				{
					audioSource = audioSourceArray[UnityEngine.Random.Range(0, audioSourceArray.Length)];
					audioSource.clip = songsArray[UnityEngine.Random.Range(0, songsArray.Length)];
					audioSource.time = 0f;
				}
				if (twoLayer)
				{
					StartPlayingSongs(0L, 0L);
				}
				else if (audioSource.volume != 0f)
				{
					audioSource.GTPlay();
				}
			}
			if (GorillaComputer.instance == null)
			{
				return;
			}
			currentTime = (GorillaComputer.instance.startupMillis + (long)(Time.realtimeSinceStartup * 1000f)) % totalLoopTime;
			if (audioSource.isPlaying)
			{
				return;
			}
			if (lastPlayIndex >= 0 && songStartTimes[lastPlayIndex % songStartTimes.Length] < currentTime && currentTime < songStartTimes[(lastPlayIndex + 1) % songStartTimes.Length])
			{
				if (twoLayer)
				{
					if (songStartTimes[lastPlayIndex] + (long)(audioSource.clip.length * 1000f) > currentTime)
					{
						StartPlayingSongs(songStartTimes[lastPlayIndex], currentTime);
					}
				}
				else if (usingMultipleSongs && usingMultipleSources)
				{
					if (songStartTimes[lastPlayIndex] + (long)(songsArray[audioClipsForPlaying[lastPlayIndex]].length * 1000f) > currentTime)
					{
						StartPlayingSong(songStartTimes[lastPlayIndex], currentTime, songsArray[audioClipsForPlaying[lastPlayIndex]], audioSourceArray[audioSourcesForPlaying[lastPlayIndex]]);
					}
				}
				else if (songStartTimes[lastPlayIndex] + (long)(audioSource.clip.length * 1000f) > currentTime)
				{
					StartPlayingSong(songStartTimes[lastPlayIndex], currentTime);
				}
				return;
			}
			for (int i = 0; i < songStartTimes.Length; i++)
			{
				if (songStartTimes[i] > currentTime)
				{
					lastPlayIndex = (i - 1) % songStartTimes.Length;
					break;
				}
			}
		}
	}

	private void StartPlayingSong(long timeStarted, long currentTime)
	{
		if (audioSource.volume != 0f)
		{
			audioSource.GTPlay();
		}
		audioSource.time = (float)(currentTime - timeStarted) / 1000f;
	}

	private void StartPlayingSongs(long timeStarted, long currentTime)
	{
		AudioSource[] array = audioSourceArray;
		foreach (AudioSource audioSource in array)
		{
			if (audioSource.volume != 0f)
			{
				audioSource.GTPlay();
			}
			audioSource.time = (float)(currentTime - timeStarted) / 1000f;
		}
	}

	private void StartPlayingSong(long timeStarted, long currentTime, AudioClip clipToPlay, AudioSource sourceToPlay)
	{
		audioSource = sourceToPlay;
		sourceToPlay.clip = clipToPlay;
		if (sourceToPlay.isActiveAndEnabled && sourceToPlay.volume != 0f)
		{
			sourceToPlay.GTPlay();
		}
		sourceToPlay.time = (float)(currentTime - timeStarted) / 1000f;
	}

	private void GenerateSongStartRandomTimes()
	{
		songStartTimes = new long[500];
		audioSourcesForPlaying = new int[500];
		audioClipsForPlaying = new int[500];
		songStartTimes[0] = minimumWait + randomNumberGenerator.Next(randomInterval);
		for (int i = 1; i < songStartTimes.Length; i++)
		{
			songStartTimes[i] = songStartTimes[i - 1] + minimumWait + randomNumberGenerator.Next(randomInterval);
		}
		if (usingMultipleSources)
		{
			for (int j = 0; j < audioSourcesForPlaying.Length; j++)
			{
				audioSourcesForPlaying[j] = randomNumberGenerator.Next(audioSourceArray.Length);
			}
		}
		if (usingMultipleSongs)
		{
			for (int k = 0; k < audioClipsForPlaying.Length; k++)
			{
				audioClipsForPlaying[k] = randomNumberGenerator.Next(songsArray.Length);
			}
		}
		if (usingMultipleSongs)
		{
			totalLoopTime = songStartTimes[songStartTimes.Length - 1] + (long)(songsArray[audioClipsForPlaying[audioClipsForPlaying.Length - 1]].length * 1000f);
		}
		else if (audioSource.clip != null)
		{
			totalLoopTime = songStartTimes[songStartTimes.Length - 1] + (long)(audioSource.clip.length * 1000f);
		}
	}

	public void MuteAudio(GorillaPressableButton pressedButton)
	{
		AudioSource[] array;
		if (audioSource.mute)
		{
			PlayerPrefs.SetInt(locationName + "Muted", 0);
			PlayerPrefs.Save();
			audioSource.mute = false;
			array = audioSourceArray;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].mute = false;
			}
			pressedButton.isOn = false;
			pressedButton.UpdateColor();
			for (int j = 0; j < muteButtons.Length; j++)
			{
				if (muteButtons[j] != null)
				{
					muteButtons[j].isOn = false;
					muteButtons[j].UpdateColor();
				}
			}
			return;
		}
		PlayerPrefs.SetInt(locationName + "Muted", 1);
		PlayerPrefs.Save();
		audioSource.mute = true;
		array = audioSourceArray;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].mute = true;
		}
		pressedButton.isOn = true;
		pressedButton.UpdateColor();
		for (int k = 0; k < muteButtons.Length; k++)
		{
			if (muteButtons[k] != null)
			{
				muteButtons[k].isOn = true;
				muteButtons[k].UpdateColor();
			}
		}
	}

	protected void New_Start()
	{
		string text = New_Validate();
		if (text.Length > 0)
		{
			Debug.LogError("Disabling SynchedMusicController on \"" + base.name + "\" due to invalid setup: " + text + " Path: " + base.transform.GetPathQ(), this);
			base.enabled = false;
		}
		if (usingMultipleSources && this.audioSource == null)
		{
			this.audioSource = audioSourceArray[0];
		}
		totalLoopTime = 0L;
		bool mute = PlayerPrefs.GetInt(locationName + "Muted", 0) != 0;
		if (muteButton == null && muteButtons.Length >= 1 && muteButtons[0] != null)
		{
			muteButton = muteButtons[0];
		}
		if (this.audioSource != null)
		{
			this.audioSource.mute = mute;
			muteButton.isOn = this.audioSource.mute;
		}
		AudioSource[] array = audioSourceArray;
		foreach (AudioSource audioSource in array)
		{
			audioSource.mute = mute;
			muteButton.isOn = audioSource.mute || muteButton.isOn;
		}
		for (int j = 0; j < muteButtons.Length; j++)
		{
			if (!(muteButtons[j] == null))
			{
				muteButtons[j].isOn = muteButton.isOn;
				muteButtons[j].UpdateColor();
			}
		}
		muteButton.UpdateColor();
		randomNumberGenerator = new System.Random(mySeed);
		New_GeneratePlaylistArrays();
		SyncedSongInfo[] array2 = syncedSongs;
		for (int i = 0; i < array2.Length; i++)
		{
			SyncedSongInfo syncedSongInfo = array2[i];
			if (syncedSongInfo.songLayers.Length > 1)
			{
				SyncedSongLayerInfo[] songLayers = syncedSongInfo.songLayers;
				for (int k = 0; k < songLayers.Length; k++)
				{
					songLayers[k].audioClip.LoadAudioData();
				}
			}
		}
	}

	public void OnEnable()
	{
		lastPlayIndex = -1;
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		StopAllAudioSources();
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void StopAllAudioSources()
	{
		for (int i = 0; i < audioSourceArray.Length; i++)
		{
			audioSourceArray[i].Stop();
		}
	}

	private void New_Update()
	{
		if (!GorillaComputer.hasInstance || GorillaComputer.instance.startupMillis == 0L || totalLoopTime <= 0 || songStartTimes.Length == 0)
		{
			return;
		}
		long startupMillis = GorillaComputer.instance.startupMillis;
		if (startupMillis <= 0)
		{
			return;
		}
		long num = startupMillis + (long)(Time.realtimeSinceStartup * 1000f);
		long num2 = ((totalLoopTime > 0) ? (num % totalLoopTime) : 0);
		bool flag = false;
		if (lastPlayIndex < 0)
		{
			flag = true;
			for (int i = 1; i < 256; i++)
			{
				if (songStartTimes[i] > num2)
				{
					lastPlayIndex = (i - 1) % 256;
					break;
				}
			}
			if (lastPlayIndex < 0)
			{
				lastPlayIndex = 255;
			}
		}
		int num3 = (lastPlayIndex + 1) % 256;
		if (songStartTimes[num3] < num2)
		{
			lastPlayIndex = num3;
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		long num4 = songStartTimes[lastPlayIndex];
		SyncedSongInfo syncedSongInfo = syncedSongs[audioClipsForPlaying[lastPlayIndex]];
		float length = syncedSongInfo.songLayers[0].audioClip.length;
		float num5 = (float)(num2 - num4) / 1000f;
		if (num5 < 0f || length < num5)
		{
			return;
		}
		for (int j = 0; j < syncedSongInfo.songLayers.Length; j++)
		{
			SyncedSongLayerInfo syncedSongLayerInfo = syncedSongInfo.songLayers[j];
			if (syncedSongLayerInfo.audioSourcePickMode == AudioSourcePickMode.All)
			{
				AudioSource[] array = audioSourceArray;
				foreach (AudioSource audioSource in array)
				{
					audioSource.clip = syncedSongLayerInfo.audioClip;
					if (audioSource.volume > 0f)
					{
						audioSource.GTPlay();
					}
					audioSource.time = num5;
				}
			}
			else if (syncedSongLayerInfo.audioSourcePickMode == AudioSourcePickMode.Shuffle)
			{
				AudioSource audioSource2 = audioSourceArray[audioSourcesForPlaying[lastPlayIndex]];
				audioSource2.clip = syncedSongLayerInfo.audioClip;
				if (audioSource2.volume > 0f)
				{
					audioSource2.GTPlay();
				}
				audioSource2.time = num5;
			}
			else
			{
				if (syncedSongLayerInfo.audioSourcePickMode != AudioSourcePickMode.Specific)
				{
					continue;
				}
				AudioSource[] array = syncedSongLayerInfo.audioSources;
				foreach (AudioSource audioSource3 in array)
				{
					audioSource3.clip = syncedSongLayerInfo.audioClip;
					if (audioSource3.volume > 0f)
					{
						audioSource3.GTPlay();
					}
					audioSource3.time = num5;
				}
			}
		}
	}

	private string New_Validate()
	{
		if (syncedSongs == null)
		{
			return "syncedSongs array cannot be null.";
		}
		if (syncedSongs.Length == 0)
		{
			return "syncedSongs array cannot be empty.";
		}
		for (int i = 0; i < syncedSongs.Length; i++)
		{
			SyncedSongInfo syncedSongInfo = syncedSongs[i];
			if (syncedSongInfo.songLayers == null)
			{
				return $"Song {i}'s songLayers array is null.";
			}
			if (syncedSongInfo.songLayers.Length == 0)
			{
				return $"Song {i}'s songLayers array is empty.";
			}
			for (int j = 0; j < syncedSongInfo.songLayers.Length; j++)
			{
				SyncedSongLayerInfo syncedSongLayerInfo = syncedSongInfo.songLayers[j];
				if (syncedSongLayerInfo.audioClip == null)
				{
					return $"Song {i}'s song layer {j} does not have an audio clip.";
				}
				if (syncedSongLayerInfo.audioSourcePickMode == AudioSourcePickMode.Specific)
				{
					if (syncedSongLayerInfo.audioSources == null || syncedSongLayerInfo.audioSources.Length == 0)
					{
						return $"Song {i}'s song layer {j} has audioSourcePickMode set to {syncedSongLayerInfo.audioSourcePickMode} " + "but layer's audioSources array is empty or null.";
					}
				}
				else if (audioSourceArray == null || audioSourceArray.Length == 0)
				{
					return string.Format("{0} is null or empty, while Song {1}'s song layer {2} has ", "audioSourceArray", i, j) + $"audioSourcePickMode set to {syncedSongLayerInfo.audioSourcePickMode}, which uses the " + "component's audioSourceArray.";
				}
			}
		}
		return string.Empty;
	}

	private void New_GeneratePlaylistArrays()
	{
		if (syncedSongs == null || syncedSongs.Length == 0)
		{
			return;
		}
		songStartTimes = new long[256];
		songStartTimes[0] = minimumWait + randomNumberGenerator.Next(randomInterval);
		for (int i = 1; i < songStartTimes.Length; i++)
		{
			songStartTimes[i] = songStartTimes[i - 1] + minimumWait + randomNumberGenerator.Next(randomInterval);
		}
		audioSourcesForPlaying = new int[256];
		bool flag = false;
		SyncedSongInfo[] array = syncedSongs;
		for (int j = 0; j < array.Length; j++)
		{
			SyncedSongLayerInfo[] songLayers = array[j].songLayers;
			for (int k = 0; k < songLayers.Length; k++)
			{
				if (songLayers[k].audioSourcePickMode == AudioSourcePickMode.Shuffle)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			for (int l = 0; l < audioSourcesForPlaying.Length; l++)
			{
				audioSourcesForPlaying[l] = randomNumberGenerator.Next(audioSourceArray.Length);
			}
		}
		audioClipsForPlaying = new int[256];
		for (int m = 0; m < audioClipsForPlaying.Length; m++)
		{
			if (shufflePlaylist)
			{
				audioClipsForPlaying[m] = randomNumberGenerator.Next(syncedSongs.Length);
			}
			else
			{
				audioClipsForPlaying[m] = syncedSongs.Length - 1;
			}
		}
		long num = (long)syncedSongs[audioClipsForPlaying[^1]].songLayers[0].audioClip.length * 1000;
		long num2 = songStartTimes[^1];
		totalLoopTime = num + num2;
	}
}
