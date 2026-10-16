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

	[Tooltip("Zone to make active after teleporting to the mark. None = leave zones unchanged.")]
	[SerializeField]
	private GTZone teleportToZone = GTZone.none;

	private void OnEnable()
	{
	}

	public void SetMarkAtLocalPlayer()
	{
		if (base.enabled)
		{
			Transform transform = GTPlayer.Instance.mainCamera.transform;
			base.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
		}
	}

	public void TeleportLocalPlayerToMark()
	{
		if (base.enabled)
		{
			GTPlayer.Instance.TeleportTo(base.transform, matchRotation, maintainVelocity);
			if (teleportToZone != GTZone.none)
			{
				ZoneManagement.SetActiveZone(teleportToZone);
			}
		}
	}
}
