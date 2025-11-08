using GorillaLocomotion;
using UnityEngine;

public class GravityOverrideVolume : MonoBehaviour
{
	public enum GravityType
	{
		Directional,
		Radial
	}

	[SerializeField]
	private GravityType gravityType;

	[SerializeField]
	private float strength = 9.8f;

	[SerializeField]
	[Tooltip("In Radial: the center point of gravity, In Directional: the forward vector of this transform defines the direction")]
	private Transform referenceTransform;

	[SerializeField]
	private CompositeTriggerEvents triggerEvents;

	private void OnEnable()
	{
		if (triggerEvents != null)
		{
			triggerEvents.CompositeTriggerEnter += OnColliderEnteredVolume;
			triggerEvents.CompositeTriggerExit += OnColliderExitedVolume;
		}
	}

	private void OnDisable()
	{
		if (triggerEvents != null)
		{
			triggerEvents.CompositeTriggerEnter -= OnColliderEnteredVolume;
			triggerEvents.CompositeTriggerExit -= OnColliderExitedVolume;
		}
	}

	private void OnColliderEnteredVolume(Collider collider)
	{
		GTPlayer instance = GTPlayer.Instance;
		if (instance != null && collider == instance.headCollider)
		{
			instance.SetGravityOverride(this, GravityOverrideFunction);
		}
	}

	private void OnColliderExitedVolume(Collider collider)
	{
		GTPlayer instance = GTPlayer.Instance;
		if (instance != null && collider == instance.headCollider)
		{
			instance.UnsetGravityOverride(this);
		}
	}

	public void GravityOverrideFunction(GTPlayer player)
	{
		switch (gravityType)
		{
		case GravityType.Directional:
		{
			Vector3 forward = referenceTransform.forward;
			player.AddForce(forward * strength, ForceMode.Acceleration);
			break;
		}
		case GravityType.Radial:
		{
			Vector3 normalized = (referenceTransform.position - player.headCollider.transform.position).normalized;
			player.AddForce(normalized * strength, ForceMode.Acceleration);
			break;
		}
		}
	}
}
