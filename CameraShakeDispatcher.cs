using UnityEngine;

public class CameraShakeDispatcher : MonoBehaviour
{
	[SerializeField]
	private float magnitude = 1f;

	[SerializeField]
	private float duration = 0.5f;

	[SerializeField]
	private bool rollOffOverDuration = true;

	[SerializeField]
	private bool shakeOnEnable;

	[SerializeField]
	private bool haltOnDisable;

	[SerializeField]
	private Vector2 freqRange = new Vector2(0.02f, 0.1f);

	private void OnEnable()
	{
		if (shakeOnEnable)
		{
			Shake();
		}
	}

	private void OnDisable()
	{
		if (haltOnDisable)
		{
			Halt();
		}
	}

	public void Shake()
	{
		CameraShaker.Shake(duration, magnitude, freqRange, rollOffOverDuration);
	}

	public void Halt()
	{
		CameraShaker.Halt();
	}
}
