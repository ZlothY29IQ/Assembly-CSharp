using GorillaTag;
using UnityEngine;

public class WizardStaffHoldable : TransferrableObject
{
	[Tooltip("This GameObject will activate when the staff hits the ground with enough force.")]
	public GameObject effectsGameObject;

	[Tooltip("The Transform of the staff's tip which will be used to determine if the staff is being slammed. Up axis (Y) should point along the length of the staff.")]
	public Transform tipTransform;

	public float tipCollisionRadius = 0.05f;

	public LayerMask tipCollisionLayerMask;

	[Tooltip("Used to calculate velocity of the staff.")]
	public GorillaVelocityEstimator velocityEstimator;

	public float cooldown = 5f;

	[Tooltip("The velocity of the staff's tip must be greater than this value to activate the effect.")]
	public float minSlamVelocity = 0.5f;

	[Tooltip("The angle (in degrees) between the staff's tip and the ground must be less than this value to activate the effect.")]
	public float minSlamAngle = 5f;

	[DebugReadout]
	private float cooldownRemaining;

	[DebugReadout]
	private bool hitLastFrame;

	private Vector3 tipTargetLocalPosition;

	private bool hasEffectsGameObject;

	private bool effectsHaveBeenPlayed;

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		tipTargetLocalPosition = tipTransform.localPosition;
		hasEffectsGameObject = effectsGameObject != null;
		effectsHaveBeenPlayed = false;
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		InitToDefault();
	}

	public override void ResetToDefaultState()
	{
		base.ResetToDefaultState();
		InitToDefault();
	}

	private void InitToDefault()
	{
		cooldownRemaining = 0f;
		if (hasEffectsGameObject && effectsHaveBeenPlayed)
		{
			effectsGameObject.SetActive(value: false);
		}
		effectsHaveBeenPlayed = false;
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (InHand() && itemState != ItemStates.State1 && GorillaParent.hasInstance && hitLastFrame && !(velocityEstimator.linearVelocity.magnitude < minSlamVelocity))
		{
			Vector3 up = tipTransform.up;
			Vector3 up2 = Vector3.up;
			if (!(Vector3.Angle(up, up2) > minSlamAngle))
			{
				itemState = ItemStates.State1;
				cooldownRemaining = cooldown;
			}
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		cooldownRemaining -= Time.deltaTime;
		if (cooldownRemaining <= 0f)
		{
			itemState = ItemStates.State0;
			if (hasEffectsGameObject)
			{
				effectsGameObject.SetActive(value: false);
			}
			effectsHaveBeenPlayed = false;
		}
		if (InHand())
		{
			Vector3 position = base.transform.position;
			Vector3 end = base.transform.TransformPoint(tipTargetLocalPosition);
			if (Physics.Linecast(position, end, out var hitInfo, tipCollisionLayerMask))
			{
				tipTransform.position = hitInfo.point;
				hitLastFrame = true;
			}
			else
			{
				tipTransform.localPosition = tipTargetLocalPosition;
				hitLastFrame = false;
			}
			if (itemState == ItemStates.State1 && hasEffectsGameObject && !effectsHaveBeenPlayed)
			{
				effectsGameObject.SetActive(value: true);
				effectsHaveBeenPlayed = true;
			}
		}
	}

	protected override void LateUpdateReplicated()
	{
		base.LateUpdateReplicated();
		if (itemState == ItemStates.State1 && !effectsHaveBeenPlayed)
		{
			cooldownRemaining = cooldown;
		}
	}
}
