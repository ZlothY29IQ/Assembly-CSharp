using UnityEngine;

namespace GorillaTagScripts.ObstacleCourse;

public class ObstacleCourseZoneTrigger : MonoBehaviour
{
	public delegate void ObstacleCourseTriggerEvent(Collider collider);

	public LayerMask bodyLayer;

	public event ObstacleCourseTriggerEvent OnPlayerTriggerEnter;

	public event ObstacleCourseTriggerEvent OnPlayerTriggerExit;

	private void OnTriggerEnter(Collider other)
	{
		if ((bool)other.GetComponent<SphereCollider>() && other.attachedRigidbody.gameObject.CompareTag("GorillaPlayer"))
		{
			this.OnPlayerTriggerEnter?.Invoke(other);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((bool)other.GetComponent<SphereCollider>() && other.attachedRigidbody.gameObject.CompareTag("GorillaPlayer"))
		{
			this.OnPlayerTriggerExit?.Invoke(other);
		}
	}
}
