using UnityEngine;

public class PlayerGameEventLocationTrigger : MonoBehaviour
{
	[SerializeField]
	private string locationName;

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject == GorillaTagger.Instance.headCollider.gameObject)
		{
			PlayerGameEvents.TriggerEnterLocation(locationName);
		}
	}
}
