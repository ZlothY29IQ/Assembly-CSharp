using UnityEngine;

public class WASD : MonoBehaviour
{
	public float Speed = 1f;

	public float Omega = 1f;

	public Vector3 m_velocity;

	public Vector3 Velocity => m_velocity;

	public void Update()
	{
		Vector3 zero = Vector3.zero;
		if (Input.GetKey(KeyCode.W))
		{
			zero.z += 1f;
		}
		if (Input.GetKey(KeyCode.A))
		{
			zero.x -= 1f;
		}
		if (Input.GetKey(KeyCode.S))
		{
			zero.z -= 1f;
		}
		if (Input.GetKey(KeyCode.D))
		{
			zero.x += 1f;
		}
		Vector3 vector = ((zero.sqrMagnitude > 0f) ? (zero.normalized * Speed * Time.deltaTime) : Vector3.zero);
		Quaternion quaternion = Quaternion.AngleAxis(0f * Omega * 57.29578f * Time.deltaTime, Vector3.up);
		m_velocity = vector / Time.deltaTime;
		base.transform.position += vector;
		base.transform.rotation = quaternion * base.transform.rotation;
	}
}
