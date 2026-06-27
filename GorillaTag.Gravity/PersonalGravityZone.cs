using GorillaLocomotion;
using UnityEngine;

namespace GorillaTag.Gravity;

public class PersonalGravityZone : BasicGravityZone
{
	public void SetLocalPlayerGravityDirection(Vector3 direction)
	{
		GTPlayerTransform.Instance.SetPersonalGravityDirection(direction);
	}

	public void SetLocalPlayerGravityDirection(Transform referenceDir)
	{
		GTPlayerTransform.Instance.SetPersonalGravityDirection(referenceDir);
	}

	protected override Vector3 GetGravityVectorAtPoint(in Vector3 worldPosition, in MonkeGravityController controller)
	{
		return controller.PersonalGravityDirection;
	}

	private void ResetLocalPlayerIfMatch(MonkeGravityController controller)
	{
		if (controller == GTPlayerTransform.Instance)
		{
			GTPlayerTransform.Instance.SetPersonalGravityDirection(Vector3.up);
		}
	}

	protected override void OnTargetExited(MonkeGravityController target)
	{
		ResetLocalPlayerIfMatch(target);
	}

	protected override void OnTargetFilteredOut(MonkeGravityController target)
	{
		ResetLocalPlayerIfMatch(target);
	}
}
