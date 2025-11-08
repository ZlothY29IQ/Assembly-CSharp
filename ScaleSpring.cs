using BoingKit;
using UnityEngine;

public class ScaleSpring : MonoBehaviour
{
	private static readonly float kInterval = 2f;

	private static readonly float kSmallScale = 0.6f;

	private static readonly float kLargeScale = 2f;

	private static readonly float kMoveDistance = 30f;

	private Vector3Spring m_spring;

	private float m_targetScale;

	private float m_lastTickTime;

	public void Tick()
	{
		m_targetScale = ((m_targetScale == kSmallScale) ? kLargeScale : kSmallScale);
		m_lastTickTime = Time.time;
		GetComponent<BoingEffector>().MoveDistance = kMoveDistance * ((m_targetScale == kSmallScale) ? (-1f) : 1f);
	}

	public void Start()
	{
		Tick();
		m_spring.Reset(m_targetScale * Vector3.one);
	}

	public void FixedUpdate()
	{
		if (Time.time - m_lastTickTime > kInterval)
		{
			Tick();
		}
		m_spring.TrackHalfLife(m_targetScale * Vector3.one, 6f, 0.05f, Time.fixedDeltaTime);
		base.transform.localScale = m_spring.Value;
		GetComponent<BoingEffector>().MoveDistance *= Mathf.Min(0.99f, 35f * Time.fixedDeltaTime);
	}
}
