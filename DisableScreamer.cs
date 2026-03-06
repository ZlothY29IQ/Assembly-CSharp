using UnityEngine;

public class DisableScreamer : MonoBehaviour
{
	private void OnDisable()
	{
		Debug.LogError("oh my god i've been disabled! aaag!!! AAAAAAAAA!!!!", base.gameObject);
	}
}
