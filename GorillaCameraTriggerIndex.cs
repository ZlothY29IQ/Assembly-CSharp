using UnityEngine;

public class GorillaCameraTriggerIndex : MonoBehaviour
{
	public int sceneTriggerIndex;

	public GorillaCameraSceneTrigger parentTrigger;

	private void Start()
	{
		parentTrigger = GetComponentInParent<GorillaCameraSceneTrigger>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("SceneChanger"))
		{
			parentTrigger.mostRecentSceneTrigger = this;
			parentTrigger.ChangeScene(this);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.CompareTag("SceneChanger"))
		{
			parentTrigger.ChangeScene(this);
		}
	}
}
