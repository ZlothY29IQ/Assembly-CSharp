using UnityEngine;

public struct GameNoiseEvent
{
	public Vector3 position;

	public double eventTime;

	public float duration;

	public float magnitude;

	public bool IsValid()
	{
		return (float)(Time.timeAsDouble - eventTime) <= duration;
	}
}
