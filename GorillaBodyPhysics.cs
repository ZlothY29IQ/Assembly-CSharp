using UnityEngine;

public class GorillaBodyPhysics : MonoBehaviour
{
	public GameObject bodyCollider;

	public Vector3 bodyColliderOffset;

	public Transform headsetTransform;

	private void FixedUpdate()
	{
		bodyCollider.transform.position = headsetTransform.position + bodyColliderOffset;
	}
}
