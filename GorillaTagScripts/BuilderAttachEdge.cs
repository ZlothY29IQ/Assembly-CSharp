using UnityEngine;

namespace GorillaTagScripts;

public class BuilderAttachEdge : MonoBehaviour
{
	public Transform center;

	public float length;

	private void Awake()
	{
		if (center == null)
		{
			center = base.transform;
		}
	}

	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Transform transform = center;
		if (transform == null)
		{
			transform = base.transform;
		}
		Vector3 vector = transform.rotation * Vector3.right;
		Gizmos.DrawLine(transform.position - vector * length * 0.5f, transform.position + vector * length * 0.5f);
	}
}
