using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class TrashcanCosmetic : MonoBehaviour
{
	public float minScoringDistance = 2f;

	public UnityEvent OnScored;

	public void OnBasket(bool isLeftHand, Collider other)
	{
		if (other.TryGetComponent<SlingshotProjectile>(out var component) && component.GetDistanceTraveled() >= minScoringDistance)
		{
			OnScored?.Invoke();
			component.DestroyAfterRelease();
		}
	}
}
