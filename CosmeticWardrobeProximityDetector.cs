using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class CosmeticWardrobeProximityDetector : MonoBehaviour
{
	[SerializeField]
	private SphereCollider wardrobeNearbyCollider;

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

	public static bool IsUserNearWardrobe(string userID)
	{
		int layerMask = LayerMask.GetMask("Gorilla Tag Collider") | LayerMask.GetMask("Gorilla Body Collider");
		foreach (SphereCollider item in wardrobeNearbyDetection)
		{
			int a = Physics.OverlapSphereNonAlloc(item.transform.position, item.radius, overlapColliders, layerMask);
			a = Mathf.Min(a, overlapColliders.Length);
			if (a <= 0)
			{
				continue;
			}
			for (int i = 0; i < a; i++)
			{
				Collider collider = overlapColliders[i];
				if (collider == null)
				{
					continue;
				}
				GameObject gameObject = collider.attachedRigidbody.gameObject;
				VRRig component = gameObject.GetComponent<VRRig>();
				if (component == null || component.creator == null || component.creator.IsNull || string.IsNullOrEmpty(component.creator.UserId))
				{
					if (gameObject.GetComponent<GTPlayer>() == null || NetworkSystem.Instance.LocalPlayer == null)
					{
						continue;
					}
					if (userID == NetworkSystem.Instance.LocalPlayer.UserId)
					{
						return true;
					}
				}
				else if (userID == component.creator.UserId)
				{
					return true;
				}
				overlapColliders[i] = null;
			}
		}
		return false;
	}
}
