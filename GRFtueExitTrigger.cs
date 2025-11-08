using UnityEngine;

public class GRFtueExitTrigger : GorillaTriggerBox
{
	public GRFirstTimeUserExperience ftueObject;

	public float delayTime = 5f;

	private float startTime = -1f;

	public override void OnBoxTriggered()
	{
		startTime = Time.time;
		ftueObject.InterruptWaitingTimer();
		ftueObject.playerLight.GetComponentInChildren<Light>().intensity = 0.25f;
	}

	private void Update()
	{
		if (startTime > 0f && Time.time - startTime > delayTime)
		{
			ftueObject.ChangeState(GRFirstTimeUserExperience.TransitionState.Flicker);
			startTime = -1f;
		}
	}
}
