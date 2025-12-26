using UnityEngine;

[DefaultExecutionOrder(100)]
public class TransformFollow : MonoBehaviour
{
	public Transform transformToFollow;

	public Vector3 offset;

	public Vector3 prevPos;

	private void Awake()
	{
		prevPos = base.transform.position;
	}

	private void LateUpdate()
	{
		prevPos = base.transform.position;
		transformToFollow.GetPositionAndRotation(out var position, out var rotation);
		base.transform.SetPositionAndRotation(position + rotation * offset, rotation);
	}
}
