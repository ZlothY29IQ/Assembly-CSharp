using System;
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

	public static Action<VODTarget> AlertEnabled;

	public static Action<VODTarget> AlertDisabled;

	public VODTargetAudioSettings AudioSettings => audioSettings;

	public Renderer Renderer => targetRenderer;

	public TMP_Text UpNextText => upNext;

	public Material StandbyOverride => standbyOverride;

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
		if (AlertDisabled != null)
		{
			AlertDisabled(this);
		}
	}

	protected override void OnBecameObservable()
	{
		if (AlertEnabled != null)
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

	protected override void UnityOnEnable()
	{
		VODPlayer.OnCrash = (Action)Delegate.Combine(VODPlayer.OnCrash, new Action(VODPlayer_OnCrash));
		if (VODPlayer.state == VODPlayer.State.CRASHED)
		{
			base.gameObject.SetActive(value: false);
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
		base.gameObject.SetActive(value: false);
	}

	protected override void ObservableSliceUpdate()
	{
	}
}
