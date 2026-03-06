using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class CosmeticWardrobeProximityDetector : MonoBehaviour
{
	[SerializeField]
	private SphereCollider wardrobeNearbyCollider;

	private static List<VRRig> rigs = new List<VRRig>();

	private static List<SphereCollider> wardrobeNearbyDetection = new List<SphereCollider>();

	private static readonly Collider[] overlapColliders = new Collider[20];

	private void OnEnable()
	{
		if (wardrobeNearbyCollider != null)
		{
			wardrobeNearbyDetection.Add(wardrobeNearbyCollider);
		}
	}

	private void OnDisable()
	{
		if (wardrobeNearbyCollider != null)
		{
			wardrobeNearbyDetection.Remove(wardrobeNearbyCollider);
		}
	}

	public static bool IsUserNearWardrobe(int actorNr)
	{
		LayerMask.GetMask("Gorilla Tag Collider");
		LayerMask.GetMask("Gorilla Body Collider");
		VRRigCache.Instance.GetActiveRigs(rigs);
		if (!VRRigCache.Instance.TryGetVrrig(NetPlayer.Get(actorNr).GetPlayerRef(), out var playerRig))
		{
			return false;
		}
		foreach (SphereCollider item in wardrobeNearbyDetection)
		{
			if ((playerRig.HeadCollider.transform.position - item.transform.position).magnitude <= item.radius)
			{
				return true;
			}
		}
		return false;
	}
}
