using UnityEngine;

public class NamedTriggerZone : MonoBehaviour
{
	public string TriggerName = "Trigger";

	private void Reset()
	{
		ConfigureCollider();
	}

	private void ConfigureCollider()
	{
		Collider collider = GetComponent<Collider>();
		if (!collider)
		{
			collider = base.gameObject.AddComponent<BoxCollider>();
		}
		collider.isTrigger = true;
		base.gameObject.layer = LayerMask.NameToLayer("Gorilla Trigger");
	}
}
