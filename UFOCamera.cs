using BoingKit;
using UnityEngine;

public class UFOCamera : MonoBehaviour
{
	public Transform Target;

	private Vector3 m_targetOffset;

	private Vector3Spring m_spring;

	private void Start()
	{
		if (!(Target == null))
		{
			m_targetOffset = base.transform.position - Target.position;
			m_spring.Reset(base.transform.position);
		}
	}

	private void FixedUpdate()
	{
		if (!(Target == null))
		{
			Vector3 targetValue = Target.position + m_targetOffset;
			base.transform.position = m_spring.TrackExponential(targetValue, 0.02f, Time.fixedDeltaTime);
		}
	}
}
