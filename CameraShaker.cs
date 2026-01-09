using System;
using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
	private bool rumbling;

	private float stopTime;

	private static event Action<float, float> ShakeRequested;

	public static void Shake(float duration, float magnitude)
	{
		if (CameraShaker.ShakeRequested != null)
		{
			CameraShaker.ShakeRequested(duration, magnitude);
		}
	}

	private void OnEnable()
	{
		ShakeRequested += _ShakeRequested;
	}

	private void _ShakeRequested(float duration, float magnitude)
	{
		stopTime = Time.time + duration;
		if (!rumbling)
		{
			StartCoroutine(crRumble(magnitude));
		}
	}

	private void OnDisable()
	{
		ShakeRequested -= _ShakeRequested;
	}

	private void OnDestroy()
	{
		ShakeRequested -= _ShakeRequested;
	}

	private IEnumerator crRumble(float magnitude)
	{
		rumbling = true;
		Vector3 localPosition = base.transform.localPosition;
		while (stopTime > Time.time)
		{
			base.transform.localPosition = UnityEngine.Random.insideUnitSphere * magnitude * (stopTime - Time.time);
			yield return new WaitForSeconds(UnityEngine.Random.Range(0.02f, 0.1f));
		}
		base.transform.localPosition = localPosition;
		rumbling = false;
	}
}
