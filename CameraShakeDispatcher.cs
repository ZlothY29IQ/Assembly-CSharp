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

	[SerializeField]
	private float maxDistance;

	private void OnEnable()
	{
		if (shakeOnEnable)
		{
			if (maxDistance > 0f)
			{
				ShakeInProximity(maxDistance);
			}
			else
			{
				Shake();
			}
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

	public void ShakeInProximity(float distance)
	{
		CameraShaker.ShakeInProximity(duration, magnitude, freqRange, rollOffOverDuration, base.transform, distance);
	}

	public void Halt()
	{
		CameraShaker.Halt();
	}
}
