using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class KnockbackTrigger : MonoBehaviour
{
	[SerializeField]
	private BoxCollider triggerVolume;

	[SerializeField]
	private float knockbackVelocity;

	[SerializeField]
	private Vector3 localAxis;

	[SerializeField]
	private GameObject impactFX;

	[SerializeField]
	private bool onlySmallMonke;

	private bool hasCheckedZone;

	private bool ignoreScale;

	private int lastTriggeredFrame = -1;

	private List<Collider> collidersEntered = new List<Collider>(4);

	public bool TriggeredThisFrame => lastTriggeredFrame == Time.frameCount;

	private void CheckZone()
	{
		if (!hasCheckedZone)
		{
			if (BuilderTable.TryGetBuilderTableForZone(VRRigCache.Instance.localRig.Rig.zoneEntity.currentZone, out var table))
			{
				ignoreScale = !table.isTableMutable;
			}
			hasCheckedZone = true;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.gameObject.IsOnLayer(UnityLayer.GorillaBodyCollider) && !other.gameObject.IsOnLayer(UnityLayer.GorillaHead) && !other.gameObject.IsOnLayer(UnityLayer.GorillaHand))
		{
			return;
		}
		CheckZone();
		if (!ignoreScale && onlySmallMonke && (double)VRRigCache.Instance.localRig.Rig.scaleFactor > 0.99)
		{
			return;
		}
		collidersEntered.Add(other);
		if (collidersEntered.Count <= 1)
		{
			Vector3 vector = triggerVolume.ClosestPoint(GorillaTagger.Instance.headCollider.transform.position);
			Vector3 vector2 = vector - base.transform.TransformPoint(triggerVolume.center);
			vector2 -= Vector3.Project(vector2, base.transform.TransformDirection(localAxis));
			float magnitude = vector2.magnitude;
			Vector3 direction = Vector3.up;
			if (magnitude >= 0.01f)
			{
				direction = vector2 / magnitude;
			}
			GTPlayer.Instance.SetMaximumSlipThisFrame();
			GTPlayer.Instance.ApplyKnockback(direction, knockbackVelocity * VRRigCache.Instance.localRig.Rig.scaleFactor);
			if (impactFX != null)
			{
				ObjectPools.instance.Instantiate(impactFX, vector);
			}
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 2f, Time.fixedDeltaTime);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength / 2f, Time.fixedDeltaTime);
			lastTriggeredFrame = Time.frameCount;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.IsOnLayer(UnityLayer.GorillaBodyCollider) || other.gameObject.IsOnLayer(UnityLayer.GorillaHead) || other.gameObject.IsOnLayer(UnityLayer.GorillaHand))
		{
			collidersEntered.Remove(other);
		}
	}

	private void OnDisable()
	{
		collidersEntered.Clear();
	}
}
