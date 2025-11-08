using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace GorillaTagScripts.Builder;

public class BuilderSmallHandTrigger : MonoBehaviour
{
	[Tooltip("Optional timeline to play to animate the thing getting activated, play sound, particles, etc...")]
	public PlayableDirector timeline;

	[Tooltip("Optional animation to play")]
	public Animation animation;

	private int lastTriggeredFrame = -1;

	public bool onlySmallHands;

	[SerializeField]
	protected bool requireMinimumVelocity;

	[SerializeField]
	protected float minimumVelocityMagnitude = 0.1f;

	private bool hasCheckedZone;

	private bool ignoreScale;

	internal UnityEvent TriggeredEvent = new UnityEvent();

	[SerializeField]
	private BuilderPiece myPiece;

	public bool TriggeredThisFrame => lastTriggeredFrame == Time.frameCount;

	private void OnTriggerEnter(Collider other)
	{
		if (!base.enabled)
		{
			return;
		}
		GorillaTriggerColliderHandIndicator componentInParent = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if (componentInParent == null)
		{
			return;
		}
		if (!hasCheckedZone)
		{
			if (BuilderTable.TryGetBuilderTableForZone(VRRigCache.Instance.localRig.Rig.zoneEntity.currentZone, out var table))
			{
				ignoreScale = !table.isTableMutable;
			}
			hasCheckedZone = true;
		}
		if (onlySmallHands && !ignoreScale && (double)VRRigCache.Instance.localRig.Rig.scaleFactor > 0.99)
		{
			return;
		}
		if (requireMinimumVelocity)
		{
			float num = minimumVelocityMagnitude * GorillaTagger.Instance.offlineVRRig.scaleFactor;
			if (GTPlayer.Instance.GetHandVelocityTracker(componentInParent.isLeftHand).GetAverageVelocity(worldSpace: true, 0.1f).sqrMagnitude < num * num)
			{
				return;
			}
		}
		GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, GorillaTagger.Instance.tapHapticStrength, GorillaTagger.Instance.tapHapticDuration * 1.5f);
		lastTriggeredFrame = Time.frameCount;
		TriggeredEvent?.Invoke();
		if (timeline != null && (timeline.time == 0.0 || timeline.time >= timeline.duration))
		{
			timeline.Play();
		}
		if (animation != null && animation.clip != null)
		{
			animation.Play();
		}
	}
}
