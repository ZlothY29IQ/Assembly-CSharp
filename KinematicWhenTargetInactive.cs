using UnityEngine;

public class KinematicWhenTargetInactive : MonoBehaviour
{
	public Rigidbody[] rigidBodies;

	public GameObject target;

	private void LateUpdate()
	{
		Rigidbody[] array;
		if (!target.activeSelf)
		{
			array = rigidBodies;
			foreach (Rigidbody rigidbody in array)
			{
				if (!rigidbody.isKinematic)
				{
					rigidbody.isKinematic = true;
				}
			}
			return;
		}
		array = rigidBodies;
		foreach (Rigidbody rigidbody2 in array)
		{
			if (rigidbody2.isKinematic)
			{
				rigidbody2.isKinematic = false;
			}
		}
	}
}
