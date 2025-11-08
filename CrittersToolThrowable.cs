using System.Diagnostics;
using Unity.XR.CoreUtils;
using UnityEngine;

public class CrittersToolThrowable : CrittersActor
{
	[Header("Throwable")]
	public bool requiresPlayerGrabBeforeActivate = true;

	public float requiredActivationSpeed = 2f;

	public bool onlyTriggerOnDirectCritterHit;

	public bool destroyOnImpact = true;

	public bool onlyTriggerOncePerGrab = true;

	[Header("Debug")]
	[SerializeField]
	private DelayedDestroyObject debugImpactPrefab;

	private bool hasBeenGrabbedByPlayer;

	protected bool shouldDisable;

	private bool hasTriggeredSinceLastGrab;

	private float _sqrActivationSpeed;

	public override void Initialize()
	{
		base.Initialize();
		hasBeenGrabbedByPlayer = false;
		shouldDisable = false;
		hasTriggeredSinceLastGrab = false;
		_sqrActivationSpeed = requiredActivationSpeed * requiredActivationSpeed;
	}

	public override void GrabbedBy(CrittersActor grabbingActor, bool positionOverride = false, Quaternion localRotation = default(Quaternion), Vector3 localOffset = default(Vector3), bool disableGrabbing = false)
	{
		base.GrabbedBy(grabbingActor, positionOverride, localRotation, localOffset, disableGrabbing);
		hasBeenGrabbedByPlayer = true;
		hasTriggeredSinceLastGrab = false;
		OnPickedUp();
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (CrittersManager.instance.containerLayer.Contains(collision.gameObject.layer) || (requiresPlayerGrabBeforeActivate && !hasBeenGrabbedByPlayer) || (_sqrActivationSpeed > 0f && collision.relativeVelocity.sqrMagnitude < _sqrActivationSpeed) || (onlyTriggerOncePerGrab && hasTriggeredSinceLastGrab))
		{
			return;
		}
		if (onlyTriggerOnDirectCritterHit)
		{
			CrittersPawn component = collision.gameObject.GetComponent<CrittersPawn>();
			if (component != null && component.isActiveAndEnabled)
			{
				hasTriggeredSinceLastGrab = true;
				OnImpactCritter(component);
			}
		}
		else
		{
			Vector3 point = collision.contacts[0].point;
			Vector3 normal = collision.contacts[0].normal;
			hasTriggeredSinceLastGrab = true;
			OnImpact(point, normal);
		}
		if (destroyOnImpact)
		{
			shouldDisable = true;
		}
	}

	protected virtual void OnImpact(Vector3 hitPosition, Vector3 hitNormal)
	{
	}

	protected virtual void OnImpactCritter(CrittersPawn impactedCritter)
	{
	}

	protected virtual void OnPickedUp()
	{
	}

	[Conditional("DRAW_DEBUG")]
	protected void ShowDebugVisualization(Vector3 position, float scale, float duration = 0f)
	{
		if ((bool)debugImpactPrefab)
		{
			DelayedDestroyObject delayedDestroyObject = Object.Instantiate(debugImpactPrefab, position, Quaternion.identity);
			delayedDestroyObject.transform.localScale *= scale;
			if (duration != 0f)
			{
				delayedDestroyObject.lifetime = duration;
			}
		}
	}

	public override bool ProcessLocal()
	{
		bool result = base.ProcessLocal();
		if (shouldDisable)
		{
			base.gameObject.SetActive(value: false);
			return true;
		}
		return result;
	}

	public override void TogglePhysics(bool enable)
	{
		if (enable)
		{
			rb.isKinematic = false;
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
		}
		else
		{
			rb.isKinematic = true;
			rb.interpolation = RigidbodyInterpolation.None;
			rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
		}
	}
}
