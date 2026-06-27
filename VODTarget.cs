using System;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class VODTarget : ObservableBehavior, IBuildValidation
{
	[Serializable]
	public class VODTargetAudioSettings
	{
		[Range(0f, 1f)]
		public float volume;

		[Range(0f, 5f)]
		public float dopplerLevel = 1f;

		[Range(0f, 360f)]
		public float spread;

		public AudioRolloffMode rolloffMode;

		public float minDistance = 0.5f;

		public float maxDistance = 5f;
	}

	[SerializeField]
	private Renderer targetRenderer;

	[SerializeField]
	private Material standbyOverride;

	[SerializeField]
	private VODTargetAudioSettings audioSettings;

	[SerializeField]
	private TMP_Text upNext;

	[SerializeField]
	private VODPlayer.VODStream.VODStreamChannel[] channel;

	[SerializeField]
	private GameObject staticScreen;

	public static Action<VODTarget> AlertEnabled;

	public static Action<VODTarget> AlertDisabled;

	private VODPlayer.VODNextStreamData upNextData;

	public VODTargetAudioSettings AudioSettings => audioSettings;

	public Renderer Renderer => targetRenderer;

	public Material StandbyOverride => standbyOverride;

	public VODPlayer.VODStream.VODStreamChannel[] Channel
	{
		get
		{
			if (channel.Length != 0)
			{
				return channel;
			}
			return new VODPlayer.VODStream.VODStreamChannel[1];
		}
	}

	public void SetNext(VODPlayer.VODNextStreamData data)
	{
		upNextData = data;
	}

	public void ClearNext()
	{
		upNextData = default(VODPlayer.VODNextStreamData);
	}

	public bool VerifyChannel(VODPlayer.VODStream.VODStreamChannel ch)
	{
		if (channel.Length == 0 && ch == VODPlayer.VODStream.VODStreamChannel.DEFAULT)
		{
			return true;
		}
		for (int i = 0; i < channel.Length; i++)
		{
			if (channel[i] == ch)
			{
				return true;
			}
		}
		return false;
	}

	protected override void OnLostObservable()
	{
		if (!staticScreen.activeInHierarchy && AlertDisabled != null)
		{
			AlertDisabled(this);
		}
	}

	protected override void OnBecameObservable()
	{
		if (!staticScreen.activeInHierarchy && AlertEnabled != null)
		{
			AlertEnabled(this);
		}
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		if (targetRenderer == null)
		{
			Debug.LogError("VODTarget " + base.name + " must set a Target Renderer");
			return false;
		}
		return true;
	}

	private void Start()
	{
		targetRenderer.material = ((standbyOverride == null) ? VODPlayer.StandbyMaterial : standbyOverride);
	}

	protected override void UnityOnEnable()
	{
		VODPlayer.OnCrash = (Action)Delegate.Combine(VODPlayer.OnCrash, new Action(VODPlayer_OnCrash));
		if (VODPlayer.state == VODPlayer.State.CRASHED)
		{
			staticScreen.SetActive(value: true);
		}
	}

	protected override void UnityOnDisable()
	{
		VODPlayer.OnCrash = (Action)Delegate.Remove(VODPlayer.OnCrash, new Action(VODPlayer_OnCrash));
	}

	private void OnDestroy()
	{
		VODPlayer.OnCrash = (Action)Delegate.Remove(VODPlayer.OnCrash, new Action(VODPlayer_OnCrash));
	}

	private void VODPlayer_OnCrash()
	{
		staticScreen.SetActive(value: true);
	}

	protected override void ObservableSliceUpdate()
	{
		if (upNextData.Title.IsNullOrEmpty())
		{
			if (upNext.text.Length > 0)
			{
				upNext.text = string.Empty;
			}
		}
		else if (!(GorillaComputer.instance == null))
		{
			TimeSpan timeSpan = upNextData.StartTime - GorillaComputer.instance.GetServerTime();
			upNext.text = $"next: {upNextData.Title} - {timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
		}
	}

	public void ShowStatic(bool on)
	{
		staticScreen.SetActive(on);
		if (on)
		{
			if (observable && AlertDisabled != null)
			{
				AlertDisabled(this);
			}
		}
		else if (observable && AlertEnabled != null)
		{
			AlertEnabled(this);
		}
	}
}
