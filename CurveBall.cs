using BoingKit;
using UnityEngine;

public class CurveBall : MonoBehaviour
{
	public float Interval = 2f;

	private float m_speedX;

	private float m_speedZ;

	private float m_timer;

	public void Reset()
	{
		float f = Random.Range(0f, MathUtil.TwoPi);
		float num = Mathf.Cos(f);
		float num2 = Mathf.Sin(f);
		m_speedX = 40f * num;
		m_speedZ = 40f * num2;
		m_timer = 0f;
		Vector3 position = base.transform.position;
		position.x = -10f * num;
		position.z = -10f * num2;
		base.transform.position = position;
	}

	public void Start()
	{
		Reset();
	}

	public void Update()
	{
		float deltaTime = Time.deltaTime;
		if (m_timer > Interval)
		{
			Reset();
		}
		Vector3 position = base.transform.position;
		position.x += m_speedX * deltaTime;
		position.z += m_speedZ * deltaTime;
		base.transform.position = position;
		m_timer += deltaTime;
	}
}
