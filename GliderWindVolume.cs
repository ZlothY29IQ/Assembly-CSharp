using UnityEngine;

public class GliderWindVolume : MonoBehaviour
{
	[SerializeField]
	private float maxSpeed = 30f;

	[SerializeField]
	private float maxAccel = 15f;

	[SerializeField]
	private AnimationCurve speedVsAccelCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	[SerializeField]
	private Vector3 localWindDirection = Vector3.up;

	public Vector3 WindDirection => base.transform.TransformDirection(localWindDirection);

	public void SetProperties(float speed, float accel, AnimationCurve svaCurve, Vector3 windDirection)
	{
		maxSpeed = speed;
		maxAccel = accel;
		speedVsAccelCurve.CopyFrom(svaCurve);
		localWindDirection = windDirection;
	}

	public Vector3 GetAccelFromVelocity(Vector3 velocity)
	{
		Vector3 windDirection = WindDirection;
		float time = Mathf.Clamp(Vector3.Dot(velocity, windDirection), 0f - maxSpeed, maxSpeed) / maxSpeed;
		float num = speedVsAccelCurve.Evaluate(time) * maxAccel;
		return windDirection * num;
	}
}
