using System;
using System.Collections;
using GorillaLocomotion.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.ScavengerHunt;

public class ScavengerTarget : MonoBehaviour, IGorillaGrabable
{
	public string HuntName;

	public string TargetName;

	public string DisplayName;

	public UnityEvent[] TargetCollected;

	public UnityEvent<ScavengerTarget>[] TargetCollectedArg;

	private ScavengerManager _manager;

	private void Awake()
	{
		StartCoroutine(ConnectToScavengerManager());
	}

	private IEnumerator ConnectToScavengerManager()
	{
		int i = 0;
		while (i < 30)
		{
			if (ScavengerManager.Instance == null)
			{
				yield return null;
				int num = i + 1;
				i = num;
				continue;
			}
			ScavengerManager.Instance.RegisterTarget(this);
			_manager = ScavengerManager.Instance;
			yield break;
		}
		UnityEngine.Object.Destroy(this);
		throw new Exception($"No ScavengerManager found within {30} frames of attempts.");
	}

	public void Collect()
	{
		_manager.Collect(this);
	}

	public bool MomentaryGrabOnly()
	{
		return true;
	}

	public bool CanBeGrabbed(GorillaGrabber grabber)
	{
		return !_manager.IsCollected(this);
	}

	public void OnGrabbed(GorillaGrabber grabber, out Transform grabbedTransform, out Vector3 localGrabbedPosition)
	{
		Collect();
		grabbedTransform = base.transform;
		localGrabbedPosition = base.transform.InverseTransformPoint(grabber.transform.position);
	}

	public void OnGrabReleased(GorillaGrabber grabber)
	{
	}
}
