using UnityEngine;

namespace GorillaTagScripts.ObstacleCourse;

public class ObstacleEndLineTrigger : MonoBehaviour
{
	public delegate void ObstacleCourseTriggerEvent(VRRig vrrig);

	public event ObstacleCourseTriggerEvent OnPlayerTriggerEnter;

	private void OnTriggerEnter(Collider other)
	{
		if (other.attachedRigidbody.gameObject.TryGetComponent<VRRig>(out var component))
		{
			this.OnPlayerTriggerEnter?.Invoke(component);
		}
	}
}
