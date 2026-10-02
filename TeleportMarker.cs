using GorillaLocomotion;
using UnityEngine;

public class TeleportMarker : MonoBehaviour
{
	[Tooltip("Face the player the way they were facing when the mark was set.")]
	[SerializeField]
	private bool matchRotation = true;

	[Tooltip("Keep the player's velocity through the teleport.")]
	[SerializeField]
	private bool maintainVelocity;

	public void SetMarkAtLocalPlayer()
	{
		Transform transform = GTPlayer.Instance.mainCamera.transform;
		base.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
	}

	public void TeleportLocalPlayerToMark()
	{
		GTPlayer.Instance.TeleportTo(base.transform, matchRotation, maintainVelocity);
	}
}
