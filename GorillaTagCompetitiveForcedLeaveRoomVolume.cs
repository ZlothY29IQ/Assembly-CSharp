using GorillaGameModes;
using UnityEngine;

public class GorillaTagCompetitiveForcedLeaveRoomVolume : MonoBehaviour
{
	private GorillaTagCompetitiveManager CompetitiveManager;

	private Collider VolumeCollider;

	private void Start()
	{
		VolumeCollider = GetComponent<Collider>();
		CompetitiveManager = GameMode.GetGameModeInstance(GameModeType.InfectionCompetitive) as GorillaTagCompetitiveManager;
		if (CompetitiveManager != null)
		{
			CompetitiveManager.RegisterForcedLeaveVolume(this);
		}
	}

	private void OnDestroy()
	{
		if (CompetitiveManager != null)
		{
			CompetitiveManager.UnregisterForcedLeaveVolume(this);
		}
	}

	public bool ContainsPoint(Vector3 position)
	{
		if (VolumeCollider is SphereCollider sphereCollider)
		{
			return Vector3.SqrMagnitude(position - (sphereCollider.transform.position + sphereCollider.center)) <= sphereCollider.radius * sphereCollider.radius;
		}
		if (VolumeCollider is BoxCollider { bounds: var bounds })
		{
			return bounds.Contains(position);
		}
		return false;
	}
}
