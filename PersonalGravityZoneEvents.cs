using GorillaLocomotion;
using UnityEngine;

public class PersonalGravityZoneEvents : MonoBehaviour
{
	public void SetLocalPlayerGravityDirection(Vector3 direction)
	{
		GTPlayerTransform.Instance.SetPersonalGravityDirection(direction);
	}

	public void SetLocalPlayerGravityDirection(Transform referenceDir)
	{
		GTPlayerTransform.Instance.SetPersonalGravityDirection(referenceDir);
	}
}
