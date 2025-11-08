using UnityEngine;

public class GorillaTriggerBoxTeleport : GorillaTriggerBox
{
	public Vector3 teleportLocation;

	public GameObject cameraOffest;

	public override void OnBoxTriggered()
	{
		cameraOffest.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 0f);
		cameraOffest.transform.position = teleportLocation;
	}
}
