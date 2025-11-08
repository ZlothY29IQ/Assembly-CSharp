using UnityEngine;

public class GorillaCameraSceneTrigger : MonoBehaviour
{
	public GorillaSceneCamera sceneCamera;

	public GorillaCameraTriggerIndex currentSceneTrigger;

	public GorillaCameraTriggerIndex mostRecentSceneTrigger;

	public void ChangeScene(GorillaCameraTriggerIndex triggerLeft)
	{
		if (triggerLeft == currentSceneTrigger || currentSceneTrigger == null)
		{
			if (mostRecentSceneTrigger != currentSceneTrigger)
			{
				sceneCamera.SetSceneCamera(mostRecentSceneTrigger.sceneTriggerIndex);
				currentSceneTrigger = mostRecentSceneTrigger;
			}
			else
			{
				currentSceneTrigger = null;
			}
		}
	}
}
