using UnityEngine;

public static class JamUtil
{
	public static bool IsPlaying => Application.isPlaying;

	public static void Destroy(Object obj)
	{
		Object.Destroy(obj);
	}

	public static RaycastHit ToRaycastHit(this Collision collision)
	{
		if (!collision.ConvertToRaycast(out var hit))
		{
			GTDev.LogError($"No hit! ({collision})");
		}
		return hit;
	}

	public static bool ConvertToRaycast(this Collision collision, out RaycastHit hit)
	{
		ContactPoint contact = collision.GetContact(0);
		Vector3 point = contact.point;
		Vector3 normal = contact.normal;
		LayerMask layerMask = 1 << collision.gameObject.layer;
		return Physics.Raycast(new Ray(point + normal * 0.1f, -normal), out hit, 0.2f, layerMask, QueryTriggerInteraction.Ignore);
	}
}
