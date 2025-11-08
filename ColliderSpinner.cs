using BoingKit;
using UnityEngine;

public class ColliderSpinner : MonoBehaviour
{
	public Transform Target;

	private Vector3 m_targetOffset;

	private Vector3Spring m_spring;

	private void Start()
	{
		m_targetOffset = ((Target != null) ? (base.transform.position - Target.position) : Vector3.zero);
		m_spring.Reset(base.transform.position);
	}

	private void FixedUpdate()
	{
		Vector3 targetValue = Target.position + m_targetOffset;
		base.transform.position = m_spring.TrackExponential(targetValue, 0.02f, Time.fixedDeltaTime);
	}
}
