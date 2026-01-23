using System;
using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
	private bool rumbling;

	private float stopTime;

	private bool rollOff;

	private float magnitude;

	private float duration;

	private Vector2 freqRange;

	private static event Action<float, float, Vector2, bool> ShakeRequested;

	private static event Action HaltRequested;

	public static void Shake(float duration, float magnitude)
	{
		if (CameraShaker.ShakeRequested != null)
		{
			CameraShaker.ShakeRequested(duration, magnitude, new Vector2(0.02f, 0.1f), arg4: true);
		}
	}

	public static void Shake(float duration, float magnitude, Vector2 freqRange)
	{
		if (CameraShaker.ShakeRequested != null)
		{
			CameraShaker.ShakeRequested(duration, magnitude, freqRange, arg4: true);
		}
	}

	public static void Shake(float duration, float magnitude, Vector2 freqRange, bool rollOffOverDuration)
	{
		if (CameraShaker.ShakeRequested != null)
		{
			CameraShaker.ShakeRequested(duration, magnitude, freqRange, rollOffOverDuration);
		}
	}

	public static void Halt()
	{
		if (CameraShaker.HaltRequested != null)
		{
			CameraShaker.HaltRequested();
		}
	}

	private void OnEnable()
	{
		ShakeRequested += _ShakeRequested;
		HaltRequested += _HaltRequested;
	}

	private void _ShakeRequested(float _duration, float _magnitude, Vector2 _freqRange, bool _rollOff)
	{
		stopTime = Time.time + _duration;
		duration = _duration;
		magnitude = _magnitude;
		freqRange = _freqRange;
		rollOff = _rollOff;
		if (!rumbling)
		{
			StartCoroutine(crRumble());
		}
	}

	private void _HaltRequested()
	{
		stopTime = Time.time;
	}

	private void OnDisable()
	{
		ShakeRequested -= _ShakeRequested;
		HaltRequested -= _HaltRequested;
	}

	private void OnDestroy()
	{
		ShakeRequested -= _ShakeRequested;
		HaltRequested -= _HaltRequested;
	}

	private IEnumerator crRumble()
	{
		rumbling = true;
		while (stopTime > Time.time)
		{
			Vector3 vector = UnityEngine.Random.insideUnitSphere * magnitude;
			if (rollOff)
			{
				vector *= (stopTime - Time.time) / duration;
			}
			base.transform.localPosition += vector;
			yield return new WaitForSeconds(UnityEngine.Random.Range(freqRange.x, freqRange.y));
		}
		rumbling = false;
	}
}
