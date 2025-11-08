using UnityEngine;

public class FreezePosition : MonoBehaviour
{
	public Transform target;

	public Vector3 localPosition;

	private void FixedUpdate()
	{
		if ((bool)target)
		{
			target.localPosition = localPosition;
		}
	}

	private void LateUpdate()
	{
		if ((bool)target)
		{
			target.localPosition = localPosition;
		}
	}
}
