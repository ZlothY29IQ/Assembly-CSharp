using System;
using UnityEngine;

public class MonkeVoteProximityTrigger : GorillaTriggerBox
{
	private float triggerTime = float.MinValue;

	private float retriggerDelay = 0.25f;

	public bool isPlayerNearby { get; private set; }

	public event Action OnEnter;

	public override void OnBoxTriggered()
	{
		isPlayerNearby = true;
		if (triggerTime + retriggerDelay < Time.unscaledTime)
		{
			triggerTime = Time.unscaledTime;
			this.OnEnter?.Invoke();
		}
	}

	public override void OnBoxExited()
	{
		isPlayerNearby = false;
	}
}
