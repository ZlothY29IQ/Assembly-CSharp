using UnityEngine;

public class Spinner : MonoBehaviour
{
	public float Speed;

	private float m_angle;

	public void OnEnable()
	{
		m_angle = Random.Range(0f, 360f);
	}

	public void Update()
	{
		m_angle += Speed * 360f * Time.deltaTime;
		base.transform.rotation = Quaternion.Euler(0f, 0f - m_angle, 0f);
	}
}
