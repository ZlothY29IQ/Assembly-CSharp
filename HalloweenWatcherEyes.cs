using System;
using System.Collections;
using GorillaLocomotion;
using UnityEngine;

public class HalloweenWatcherEyes : MonoBehaviour
{
	public float timeBetweenUpdates = 5f;

	public float watchRange;

	public float watchMaxAngle;

	public float lerpDuration = 1f;

	public float playersViewCenterAngle = 30f;

	public float durationToBeNormalWhenPlayerLooks = 3f;

	public GameObject leftEye;

	public GameObject rightEye;

	private float playersViewCenterCosAngle;

	private float watchMinCosAngle;

	private float pretendingToBeNormalUntilTimestamp;

	private float lerpValue;

	private void Start()
	{
		playersViewCenterCosAngle = Mathf.Cos(playersViewCenterAngle * (MathF.PI / 180f));
		watchMinCosAngle = Mathf.Cos(watchMaxAngle * (MathF.PI / 180f));
		StartCoroutine(CheckIfNearPlayer(UnityEngine.Random.Range(0f, timeBetweenUpdates)));
		base.enabled = false;
	}

	private IEnumerator CheckIfNearPlayer(float initialSleep)
	{
		yield return new WaitForSeconds(initialSleep);
		while (true)
		{
			base.enabled = (base.transform.position - GTPlayer.Instance.transform.position).sqrMagnitude < watchRange * watchRange;
			if (!base.enabled)
			{
				LookNormal();
			}
			yield return new WaitForSeconds(timeBetweenUpdates);
		}
	}

	private void Update()
	{
		Vector3 normalized = (GTPlayer.Instance.headCollider.transform.position - base.transform.position).normalized;
		if (Vector3.Dot(GTPlayer.Instance.headCollider.transform.forward, -normalized) > playersViewCenterCosAngle)
		{
			LookNormal();
			pretendingToBeNormalUntilTimestamp = Time.time + durationToBeNormalWhenPlayerLooks;
		}
		if (pretendingToBeNormalUntilTimestamp > Time.time)
		{
			return;
		}
		if (Vector3.Dot(base.transform.forward, normalized) < watchMinCosAngle)
		{
			LookNormal();
			return;
		}
		Quaternion b = Quaternion.LookRotation(normalized, base.transform.up);
		Quaternion rotation = Quaternion.Lerp(base.transform.rotation, b, lerpValue);
		leftEye.transform.rotation = rotation;
		rightEye.transform.rotation = rotation;
		if (lerpDuration > 0f)
		{
			lerpValue = Mathf.MoveTowards(lerpValue, 1f, Time.deltaTime / lerpDuration);
		}
		else
		{
			lerpValue = 1f;
		}
	}

	private void LookNormal()
	{
		leftEye.transform.localRotation = Quaternion.identity;
		rightEye.transform.localRotation = Quaternion.identity;
		lerpValue = 0f;
	}
}
