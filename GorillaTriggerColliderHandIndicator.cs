using UnityEngine;

public class GorillaTriggerColliderHandIndicator : MonoBehaviourTick
{
	public Vector3 currentVelocity;

	public Vector3 lastPosition = Vector3.zero;

	public bool isLeftHand;

	public GorillaThrowableController throwableController;

	public override void Tick()
	{
		currentVelocity = (base.transform.position - lastPosition) / Time.deltaTime;
		lastPosition = base.transform.position;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (throwableController != null)
		{
			throwableController.GrabbableObjectHover(isLeftHand);
		}
	}
}
