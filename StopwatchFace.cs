using System;
using UnityEngine;
using UnityEngine.UI;

public class StopwatchFace : MonoBehaviour
{
	[SerializeField]
	private Transform _hand;

	[SerializeField]
	private Text _text;

	[Space]
	[SerializeField]
	private StopwatchCosmetic _cosmetic;

	[Space]
	[SerializeField]
	private AudioClip _audioClick;

	[SerializeField]
	private AudioClip _audioReset;

	[SerializeField]
	private AudioClip _audioTick;

	[NonSerialized]
	[Space]
	private int _millisElapsed;

	[NonSerialized]
	private bool _watchActive;

	[NonSerialized]
	private LerpTask<int> _lerpToZero;

	public bool watchActive => _watchActive;

	public int millisElapsed => _millisElapsed;

	public Vector3Int digitsMmSsMs => ParseDigits(TimeSpan.FromMilliseconds(_millisElapsed));

	public void SetMillisElapsed(int millis, bool updateFace = true)
	{
		_millisElapsed = millis;
		if (updateFace)
		{
			UpdateText();
			UpdateHand();
		}
	}

	private void Awake()
	{
		_lerpToZero = new LerpTask<int>();
		_lerpToZero.onLerp = OnLerpToZero;
		_lerpToZero.onLerpEnd = OnLerpEnd;
	}

	private void OnLerpToZero(int a, int b, float t)
	{
		_millisElapsed = Mathf.FloorToInt(Mathf.Lerp(a, b, t * t));
		UpdateText();
		UpdateHand();
	}

	private void OnLerpEnd()
	{
		WatchReset(doLerp: false);
	}

	private void OnEnable()
	{
		WatchReset(doLerp: false);
	}

	private void OnDisable()
	{
		WatchReset(doLerp: false);
	}

	private void Update()
	{
		if (_lerpToZero.active)
		{
			_lerpToZero.Update();
		}
		else if (_watchActive)
		{
			_millisElapsed += Mathf.FloorToInt(Time.deltaTime * 1000f);
			UpdateText();
			UpdateHand();
		}
	}

	private static Vector3Int ParseDigits(TimeSpan time)
	{
		int num = (int)time.TotalMinutes % 100;
		double num2 = 60.0 * (time.TotalMinutes - (double)num);
		int num3 = (int)num2;
		int value = (int)(100.0 * (num2 - (double)num3));
		num = Math.Clamp(num, 0, 99);
		num3 = Math.Clamp(num3, 0, 59);
		value = Math.Clamp(value, 0, 99);
		return new Vector3Int(num, num3, value);
	}

	private void UpdateText()
	{
		Vector3Int vector3Int = ParseDigits(TimeSpan.FromMilliseconds(_millisElapsed));
		string text = vector3Int.x.ToString("D2");
		string text2 = vector3Int.y.ToString("D2");
		string text3 = vector3Int.z.ToString("D2");
		_text.text = text + ":" + text2 + ":" + text3;
	}

	private void UpdateHand()
	{
		float z = (float)(_millisElapsed % 60000) / 60000f * 360f;
		_hand.localEulerAngles = new Vector3(0f, 0f, z);
	}

	public void WatchToggle()
	{
		if (!_watchActive)
		{
			WatchStart();
		}
		else
		{
			WatchStop();
		}
	}

	public void WatchStart()
	{
		if (!_lerpToZero.active)
		{
			_watchActive = true;
		}
	}

	public void WatchStop()
	{
		if (!_lerpToZero.active)
		{
			_watchActive = false;
		}
	}

	public void WatchReset()
	{
		WatchReset(doLerp: true);
	}

	public void WatchReset(bool doLerp)
	{
		if (!Application.isPlaying)
		{
			return;
		}
		if (doLerp)
		{
			if (!_lerpToZero.active)
			{
				_lerpToZero.Start(_millisElapsed % 60000, 0, 0.36f);
			}
		}
		else
		{
			_watchActive = false;
			_millisElapsed = 0;
			UpdateText();
			UpdateHand();
		}
	}
}
