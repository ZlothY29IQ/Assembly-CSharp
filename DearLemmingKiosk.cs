using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DearLemmingKiosk : MonoBehaviour
{
	[SerializeField]
	private TypingTarget src;

	[SerializeField]
	private TMP_Text popUp;

	[SerializeField]
	private SimpleCountdown countDown;

	[SerializeField]
	private GameObject ready;

	[SerializeField]
	private UnityEvent Refreshed;

	[SerializeField]
	private UnityEvent SubmitSuccess;

	[SerializeField]
	private UnityEvent SubmitFail;

	private bool canSubmit = true;

	private int nextSubmit;

	private float fetchTime = -300f;

	public int NextSubmit => nextSubmit;

	public bool CanSubmit => canSubmit;

	public void Fetch()
	{
		if (!(Time.time - fetchTime < 300f))
		{
			fetchTime = Time.time;
			DearLemmingController.instance.CheckCanSubmit();
		}
	}

	public void Send()
	{
		if (canSubmit || (float)nextSubmit - Time.time < 0f)
		{
			if (src.Text.Length > 6)
			{
				DearLemmingController.instance.SubmitMessage(src.Text);
				return;
			}
			popUp.text = "That's a little too short... type a little more!";
			popUp.gameObject.SetActive(value: true);
		}
		else
		{
			popUp.text = "Your last message has been delivered. You can send another in " + secondsToTimeSpanString((float)nextSubmit - Time.time) + ".";
			popUp.gameObject.SetActive(value: true);
		}
	}

	private string secondsToTimeSpanString(float s)
	{
		TimeSpan timeSpan = new TimeSpan(0, 0, 0, Mathf.FloorToInt(s));
		if (timeSpan.Days > 0)
		{
			return $"{timeSpan.Days} days, {timeSpan.Hours} hours, {timeSpan.Minutes} minutes, and {timeSpan.Seconds} seconds";
		}
		if (timeSpan.Hours > 0)
		{
			return $"{timeSpan.Hours} hours, {timeSpan.Minutes} minutes, and {timeSpan.Seconds} seconds";
		}
		if (timeSpan.Minutes > 0)
		{
			return $"{timeSpan.Minutes} minutes, and {timeSpan.Seconds} seconds";
		}
		return $"{timeSpan.Seconds} seconds";
	}

	private void OnEnable()
	{
		DearLemmingController.instance.OnSubmitComplete += Instance_OnSubmitComplete;
		DearLemmingController.instance.OnCheckComplete += Instance_OnCheckComplete;
		Fetch();
	}

	private void Instance_OnCheckComplete(DearLemmingController.DearLemmingResponse obj)
	{
		Debug.Log("DearLemmingKiosk :: Instance_OnCheckComplete");
		if (obj.Error.IsNullOrEmpty())
		{
			SetData(obj);
		}
	}

	private void Instance_OnSubmitComplete(DearLemmingController.DearLemmingResponse obj)
	{
		Debug.Log("DearLemmingKiosk :: Instance_OnSubmitComplete");
		if (obj.Error.IsNullOrEmpty())
		{
			SubmitSuccess?.Invoke();
			SetData(obj);
			popUp.text = "message sent!";
			popUp.gameObject.SetActive(value: true);
		}
		else
		{
			SubmitFail?.Invoke();
			popUp.text = "message could not be sent at this time. try again later.";
			popUp.gameObject.SetActive(value: true);
		}
	}

	private void SetData(DearLemmingController.DearLemmingResponse obj)
	{
		SimpleCountdown simpleCountdown = countDown;
		simpleCountdown.ManualCountdownComplete = (Action)Delegate.Remove(simpleCountdown.ManualCountdownComplete, new Action(countDownComplete));
		fetchTime = Time.time;
		nextSubmit = 0;
		canSubmit = obj.CanSubmit;
		if (!canSubmit)
		{
			nextSubmit = Mathf.CeilToInt(Time.time) + (int)obj.SecondsUntilNextSubmit.Value;
			countDown.gameObject.SetActive(value: true);
			countDown.StartCountdown((int)obj.SecondsUntilNextSubmit.Value);
			SimpleCountdown simpleCountdown2 = countDown;
			simpleCountdown2.ManualCountdownComplete = (Action)Delegate.Combine(simpleCountdown2.ManualCountdownComplete, new Action(countDownComplete));
		}
		ready.SetActive(!countDown.gameObject.activeInHierarchy);
		Refreshed?.Invoke();
	}

	private void countDownComplete()
	{
		SimpleCountdown simpleCountdown = countDown;
		simpleCountdown.ManualCountdownComplete = (Action)Delegate.Remove(simpleCountdown.ManualCountdownComplete, new Action(countDownComplete));
		countDown.gameObject.SetActive(value: false);
		ready.SetActive(value: true);
	}

	private void OnDisable()
	{
		DearLemmingController.instance.OnSubmitComplete -= Instance_OnSubmitComplete;
		DearLemmingController.instance.OnCheckComplete -= Instance_OnCheckComplete;
		SimpleCountdown simpleCountdown = countDown;
		simpleCountdown.ManualCountdownComplete = (Action)Delegate.Remove(simpleCountdown.ManualCountdownComplete, new Action(countDownComplete));
	}

	private void OnDestroy()
	{
		DearLemmingController.instance.OnSubmitComplete -= Instance_OnSubmitComplete;
		DearLemmingController.instance.OnCheckComplete -= Instance_OnCheckComplete;
		SimpleCountdown simpleCountdown = countDown;
		simpleCountdown.ManualCountdownComplete = (Action)Delegate.Remove(simpleCountdown.ManualCountdownComplete, new Action(countDownComplete));
	}
}
