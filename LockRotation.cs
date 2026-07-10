using UnityEngine;

public class LockRotation : MonoBehaviour
{
	private Quaternion lockedRot;

	private void Start()
	{
		lockedRot = base.transform.rotation;
	}

	private void LateUpdate()
	{
		base.transform.rotation = lockedRot;
	}
}
