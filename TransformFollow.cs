using UnityEngine;

[DefaultExecutionOrder(100)]
public class TransformFollow : MonoBehaviour
{
	public Transform transformToFollow;

	public Vector3 offset;

	public Vector3 prevPos;

	public bool rotationOnly;

	private bool forRigRecording;

	private TransformFollow parentFollow;

	private void Awake()
	{
		prevPos = base.transform.position;
		if (rotationOnly && base.transform.parent != null && base.transform.parent.GetComponent<TransformFollow>() != null)
		{
			forRigRecording = true;
		}
		if (forRigRecording)
		{
			parentFollow = base.transform.parent.GetComponent<TransformFollow>();
		}
	}

	private void LateUpdate()
	{
		prevPos = base.transform.position;
		if (rotationOnly)
		{
			if (forRigRecording)
			{
				base.transform.localRotation = Quaternion.Inverse(parentFollow.transformToFollow.rotation) * transformToFollow.rotation;
			}
			else
			{
				base.transform.rotation = transformToFollow.rotation;
			}
		}
		else
		{
			transformToFollow.GetPositionAndRotation(out var position, out var rotation);
			base.transform.SetPositionAndRotation(position + rotation * offset, rotation);
		}
	}
}
