using BoingKit;
using UnityEngine;

public class JellyfishUFOCamera : MonoBehaviour
{
	public Transform Target;

	private Vector3Spring m_spring;

	private void Start()
	{
		if (!(Target == null))
		{
			m_spring.Reset(Target.transform.position);
		}
	}

	private void FixedUpdate()
	{
		if (!(Target == null))
		{
			m_spring.TrackExponential(Target.transform.position, 0.5f, Time.fixedDeltaTime);
			Vector3 normalized = (m_spring.Value - base.transform.position).normalized;
			base.transform.rotation = Quaternion.LookRotation(normalized);
		}
	}
}
