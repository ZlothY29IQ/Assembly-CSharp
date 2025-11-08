using UnityEngine;

public class LowEffortZone : GorillaTriggerBox
{
	public GameObject[] objectsToEnable;

	public GameObject[] objectsToDisable;

	public bool triggerOnAwake;

	private void Awake()
	{
		if (triggerOnAwake)
		{
			OnBoxTriggered();
		}
	}

	public override void OnBoxTriggered()
	{
		for (int i = 0; i < objectsToEnable.Length; i++)
		{
			if (objectsToEnable[i] != null)
			{
				objectsToEnable[i].SetActive(value: true);
			}
		}
		for (int j = 0; j < objectsToDisable.Length; j++)
		{
			if (objectsToDisable[j] != null)
			{
				objectsToDisable[j].SetActive(value: false);
			}
		}
	}
}
