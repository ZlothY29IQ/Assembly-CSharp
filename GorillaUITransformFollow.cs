using UnityEngine;

public class GorillaUITransformFollow : MonoBehaviour
{
	public Transform transformToFollow;

	public Vector3 offset;

	public bool doesMove;

	private void Start()
	{
	}

	private void LateUpdate()
	{
		if (doesMove)
		{
			base.transform.rotation = transformToFollow.rotation;
			base.transform.position = transformToFollow.position + transformToFollow.rotation * offset;
		}
	}
}
