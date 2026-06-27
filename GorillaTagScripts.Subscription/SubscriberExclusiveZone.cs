using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Subscription;

public class SubscriberExclusiveZone : MonoBehaviour, IGorillaSliceableSimple
{
	[Header("Zones")]
	[Tooltip("Inner restricted zone - hard pushback")]
	[SerializeField]
	private GameObject restrictedZone;

	[Tooltip("Outer influence zone - gentle drift")]
	[SerializeField]
	private GameObject warningZone;

	[Header("Safe Exit Point")]
	[SerializeField]
	private Transform ejectionPoint;

	[Header("Tuning")]
	[SerializeField]
	private float driftSpeed = 3f;

	[SerializeField]
	private float shoveCooldown = 0.5f;

	[Header("Safety")]
	[SerializeField]
	private float safetyCheckRadius = 0.5f;

	[SerializeField]
	private LayerMask obstacleLayers;

	[Header("Door Visuals")]
	[SerializeField]
	private GameObject nonSubscribeDoorObject;

	[SerializeField]
	private GameObject subscriberDoorObject;

	public UnityEvent OnWarning;

	public UnityEvent OnEnterRestrictedZone;

	[Header("Debug")]
	[SerializeField]
	private bool showDebugInfo;

	private bool insideRestricted;

	private bool insideInfluence;

	private float lastShoveTime;

	private GameObject tempEjectionObject;

	private Collider restrictedZoneCollider;

	private Collider influenceZoneCollider;

	private bool bodyColliderWasDisabled;

	private List<VRRig> rigs = new List<VRRig>();

	private void Awake()
	{
		if (restrictedZone != null)
		{
			restrictedZoneCollider = restrictedZone.GetComponent<Collider>();
			if (restrictedZoneCollider != null && !restrictedZoneCollider.isTrigger)
			{
				Debug.LogError("restrictedZone must be a trigger collider!", this);
				base.enabled = false;
				return;
			}
			SubscriberZoneTrigger subscriberZoneTrigger = restrictedZone.GetComponent<SubscriberZoneTrigger>();
			if (subscriberZoneTrigger == null)
			{
				subscriberZoneTrigger = restrictedZone.AddComponent<SubscriberZoneTrigger>();
			}
			subscriberZoneTrigger.parentZone = this;
			subscriberZoneTrigger.isRestrictedZone = true;
		}
		if (warningZone != null)
		{
			influenceZoneCollider = warningZone.GetComponent<Collider>();
			if (influenceZoneCollider != null && !influenceZoneCollider.isTrigger)
			{
				Debug.LogError("influenceZone must be a trigger collider!", this);
				base.enabled = false;
				return;
			}
			SubscriberZoneTrigger subscriberZoneTrigger2 = warningZone.GetComponent<SubscriberZoneTrigger>();
			if (subscriberZoneTrigger2 == null)
			{
				subscriberZoneTrigger2 = warningZone.AddComponent<SubscriberZoneTrigger>();
			}
			subscriberZoneTrigger2.parentZone = this;
			subscriberZoneTrigger2.isRestrictedZone = false;
		}
		if (ejectionPoint == null)
		{
			Debug.LogError("Assign an ejectionPoint!", this);
			base.enabled = false;
		}
		else
		{
			UpdateDoor();
		}
	}

	private void OnEnable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			GorillaSlicerSimpleManager.RegisterSliceable(this);
		}
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			GorillaSlicerSimpleManager.UnregisterSliceable(this);
			ClearAllRigOverrides();
		}
	}

	private void Update()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			UpdateDoor();
			if (!SubscriptionManager.IsLocalSubscribed())
			{
				HandleZoneBehavior();
			}
			else if (bodyColliderWasDisabled)
			{
				SetBodyCollider(GTPlayer.Instance, enabled: true);
				bodyColliderWasDisabled = false;
			}
		}
	}

	public void OnZoneEnter(bool isRestricted)
	{
		if (isRestricted)
		{
			insideRestricted = true;
			if (showDebugInfo)
			{
				Debug.Log("[Zone] Entered restricted zone");
			}
		}
		else
		{
			insideInfluence = true;
			if (showDebugInfo)
			{
				Debug.Log("[Zone] Entered warning zone");
			}
		}
	}

	public void OnZoneExit(bool isRestricted)
	{
		if (isRestricted)
		{
			insideRestricted = false;
			if (showDebugInfo)
			{
				Debug.Log("[Zone] Exited restricted zone");
			}
		}
		else
		{
			insideInfluence = false;
			if (showDebugInfo)
			{
				Debug.Log("[Zone] Exited warning zone");
			}
		}
	}

	private void HandleZoneBehavior()
	{
		GTPlayer instance = GTPlayer.Instance;
		if (instance == null)
		{
			return;
		}
		if (insideRestricted)
		{
			if (Time.time - lastShoveTime >= shoveCooldown)
			{
				lastShoveTime = Time.time;
				instance.TeleportTo(ejectionPoint.transform);
				OnEnterRestrictedZone?.Invoke();
			}
		}
		else if (insideInfluence)
		{
			DisplaceToward(instance, ejectionPoint, driftSpeed);
			OnWarning?.Invoke();
		}
	}

	private Vector3 FindSafeEjectionPosition(Vector3 playerPos)
	{
		if (restrictedZoneCollider == null)
		{
			return ejectionPoint.position;
		}
		Bounds bounds = restrictedZoneCollider.bounds;
		Vector3 vector = bounds.ClosestPoint(playerPos);
		Vector3 normalized = (vector - bounds.center).normalized;
		Vector3 vector2 = vector + normalized * (safetyCheckRadius + 0.5f);
		float maxDistance = Vector3.Distance(playerPos, vector2);
		if (Physics.SphereCast(playerPos + Vector3.up * safetyCheckRadius, safetyCheckRadius, normalized, out var hitInfo, maxDistance, obstacleLayers))
		{
			vector2 = playerPos + normalized * Mathf.Max(0.1f, hitInfo.distance - safetyCheckRadius - 0.2f);
		}
		return vector2;
	}

	private void DisplaceToward(GTPlayer player, Transform target, float speed)
	{
		Vector3 normalized = (target.position - player.transform.position).normalized;
		player.transform.position += normalized * speed * Time.deltaTime;
	}

	private void SetBodyCollider(GTPlayer player, bool enabled)
	{
		if (player != null && player.bodyCollider != null)
		{
			player.bodyCollider.enabled = enabled;
			bodyColliderWasDisabled = !enabled;
			if (showDebugInfo && player.bodyCollider.enabled != enabled)
			{
				Debug.Log($"[Zone] Body collider: {enabled}");
			}
		}
	}

	private void UpdateDoor()
	{
		bool flag = SubscriptionManager.IsLocalSubscribed();
		if (nonSubscribeDoorObject.activeSelf == flag)
		{
			nonSubscribeDoorObject.SetActive(!flag);
		}
		if (subscriberDoorObject.activeSelf != flag)
		{
			subscriberDoorObject.SetActive(SubscriptionManager.IsLocalSubscribed());
		}
	}

	public void SliceUpdate()
	{
		VRRigCache.Instance.GetActiveRigs(rigs);
		if (restrictedZoneCollider == null)
		{
			return;
		}
		for (int i = 0; i < rigs.Count; i++)
		{
			if (!rigs[i].isOfflineVRRig && !SubscriptionManager.GetSubscriptionDetails(rigs[i]).active)
			{
				Vector3 vector = restrictedZoneCollider.transform.InverseTransformPoint(rigs[i].syncPos);
				Vector3 vector2 = ((BoxCollider)restrictedZoneCollider).size / 2f;
				Vector3 center = ((BoxCollider)restrictedZoneCollider).center;
				if (vector.x < vector2.x + center.x && vector.x > 0f - vector2.x + center.x && vector.y < vector2.y + center.y && vector.y > 0f - vector2.y + center.y && vector.z < vector2.z + center.z && vector.z > 0f - vector2.z + center.z)
				{
					rigs[i].InOverrideSubscriptionZone = true;
					rigs[i].OverrideSubscriptionZoneLocation = ejectionPoint.position;
				}
				else
				{
					rigs[i].InOverrideSubscriptionZone = false;
					rigs[i].OverrideSubscriptionZoneLocation = Vector3.zero;
				}
			}
		}
	}

	public void ClearAllRigOverrides()
	{
		VRRigCache.Instance.GetActiveRigs(rigs);
		for (int i = 0; i < rigs.Count; i++)
		{
			rigs[i].InOverrideSubscriptionZone = false;
			rigs[i].OverrideSubscriptionZoneLocation = Vector3.zero;
		}
	}

	private void OnDestroy()
	{
		if (tempEjectionObject != null)
		{
			Object.Destroy(tempEjectionObject);
		}
	}

	private void OnDrawGizmos()
	{
		if (restrictedZoneCollider != null)
		{
			Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
			Gizmos.DrawCube(restrictedZoneCollider.bounds.center, restrictedZoneCollider.bounds.size);
		}
		if (influenceZoneCollider != null)
		{
			Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
			Gizmos.DrawCube(influenceZoneCollider.bounds.center, influenceZoneCollider.bounds.size);
		}
		if (ejectionPoint != null)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(ejectionPoint.position, 0.5f);
		}
	}
}
