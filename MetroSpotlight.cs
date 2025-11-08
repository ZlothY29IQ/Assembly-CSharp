using UnityEngine;
using UnityEngine.Serialization;

public class MetroSpotlight : MonoBehaviour
{
	[SerializeField]
	private Transform _blimp;

	[SerializeField]
	private Transform _light;

	[SerializeField]
	private Transform _target;

	[FormerlySerializedAs("_scale")]
	[SerializeField]
	private float _radius = 1f;

	[SerializeField]
	private float _offset;

	[SerializeField]
	private float _theta;

	public float speed = 16f;

	[Space]
	private float _time;

	public void Tick()
	{
		if ((bool)_light && (bool)_target)
		{
			_time += speed * Time.deltaTime * Time.deltaTime;
			Vector3 position = _target.position;
			Vector3 normalized = (position - _light.position).normalized;
			Vector3 vector = Vector3.Cross(normalized, _blimp.forward);
			Vector3 yDir = Vector3.Cross(normalized, vector);
			Vector3 worldPosition = Figure8(position, vector, yDir, _radius, _time, _offset, _theta);
			_light.LookAt(worldPosition);
		}
	}

	private static Vector3 Figure8(Vector3 origin, Vector3 xDir, Vector3 yDir, float scale, float t, float offset, float theta)
	{
		float num = 2f / (3f - Mathf.Cos(2f * (t + offset)));
		float num2 = scale * num * Mathf.Cos(t + offset);
		float num3 = scale * num * Mathf.Sin(2f * (t + offset)) / 2f;
		Vector3 axis = Vector3.Cross(xDir, yDir);
		Quaternion quaternion = Quaternion.AngleAxis(theta, axis);
		xDir = quaternion * xDir;
		yDir = quaternion * yDir;
		Vector3 vector = xDir * num2 + yDir * num3;
		return origin + vector;
	}
}
