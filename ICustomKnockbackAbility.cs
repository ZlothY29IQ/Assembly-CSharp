using UnityEngine;

public interface ICustomKnockbackAbility
{
	Vector3? CalculateImpulse(Transform targetTransform);
}
