using UnityEngine;

namespace GorillaTag.Sports;

public class SportGoalExitTrigger : MonoBehaviour
{
	[SerializeField]
	private SportGoalTrigger goalTrigger;

	private void OnTriggerExit(Collider other)
	{
		SportBall componentInParent = other.GetComponentInParent<SportBall>();
		if (componentInParent != null && goalTrigger != null)
		{
			goalTrigger.BallExitedGoalTrigger(componentInParent);
		}
	}
}
