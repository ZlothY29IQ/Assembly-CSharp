using UnityEngine;

namespace GorillaTag;

public class SimpleAABB : MonoBehaviour
{
	[SerializeField]
	private Vector3 m_center;

	[SerializeField]
	private Vector3 m_size;

	private Bounds m_bounds;

	private void Awake()
	{
		m_bounds = new Bounds(m_center, m_size);
	}

	public bool IsInBounds(Vector3 point)
	{
		Vector3 point2 = base.transform.InverseTransformPoint(point);
		return m_bounds.Contains(point2);
	}
}
