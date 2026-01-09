using UnityEngine;

public class CameraShakeDispatcher : MonoBehaviour
{
	[SerializeField]
	private float magnitude = 1f;

	[SerializeField]
	private float duration = 0.5f;

	[SerializeField]
	private bool shakeOnEnable;

	private void OnEnable()
	{
		if (shakeOnEnable)
		{
			Shake();
		}
	}

	public void Shake()
	{
		CameraShaker.Shake(magnitude, duration);
	}
}
