using System;
using GorillaLocomotion.Swimming;
using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

public class StickyProjectile : MonoBehaviour, IProjectile, ITickSystemTick
{
	[Flags]
	public enum StickFlags
	{
		Wall = 1,
		LocalPlayer = 2,
		RemotePlayer = 4,
		LocalHeadZone = 8,
		RemoteHeadZone = 0x10
	}

	[SerializeField]
	private Transform stickyPart;

	[Tooltip("Align the positive Z direction of this object to the rigidbody's velocity.")]
	[SerializeField]
	private bool faceVelocityWhileAirborne;

	[Tooltip("Set the rigidbody's angular velocity to a random unit Vector3, multiplied by a random value in this range.")]
	[SerializeField]
	private Vector2 launchRandomSpinSpeedMinMax = new Vector2(90f, 360f);

	[Tooltip("When enabled, the positive Z direction will face away from whatever surface the projectile hit. When disabled, it will keep its original rotation.")]
	[SerializeField]
	private bool alignToHitNormal = true;

	[Space]
	[SerializeField]
	public UnityEvent OnReset;

	[SerializeField]
	public UnityEvent OnLaunch;

	[Tooltip("Scale the 'Sticky Part' by this value when hitting the local player's head. Usually used to prevent things from obscuring your vision too much.")]
	[SerializeField]
	private float scaleOnLocalHead = 0.7f;

	[Tooltip("The radius of the head zone. Can be set to 0 to disable head zone functionality.")]
	[SerializeField]
	private float headZoneRadius = 0.15f;

	[Tooltip("The local origin of the head zone, relative to the player rig's head transform. When a shot hits inside the zone, the 'Sticky Part' will be moved to this position relative to the hit player's head.")]
	[SerializeField]
	private Vector3 headZonePosition = new Vector3(0f, 0.02f, 0.17f);

	[Tooltip("Scale the 'Sticky Part' by this value when hitting the local player's head zone. Can override 'Scale On Local Head' in case you want it to appear larger for emphasis.")]
	[SerializeField]
	private float scaleOnLocalHeadZone = 1f;

	[Tooltip("When a shot hits inside a remote player's head zone, it will be moved to the 'Head Zone Relative Position'. For the local player, it will instead be moved here. This DOES NOT AFFECT the actual origin of the head zone for hit-detection purposes, it is purely visual after-the-fact.")]
	[SerializeField]
	private Vector3 localHeadZonePosition = new Vector3(0f, 0.05f, 0.2f);

	[SerializeField]
	private FlagEvents<StickFlags> stickEvents;

	private readonly Quaternion INVERSE_HEAD_ROTATION = Quaternion.Inverse(Quaternion.Euler(0f, 270f, 252.3229f));

	private Vector3 headZoneInversePosition;

	private Vector3 headZoneInverseLocalPosition;

	private Vector3 stickyPartLocalPosition;

	private Quaternion stickyPartLocalRotation;

	private Vector3 stickyPartLocalScale;

	private Rigidbody rb;

	private RigidbodyWaterInteraction rbwi;

	private Collider collider;

	private PlayerColoredCosmetic pcc;

	private int triggerLayer;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		stickyPart.GetLocalPositionAndRotation(out stickyPartLocalPosition, out stickyPartLocalRotation);
		stickyPartLocalScale = stickyPart.localScale;
		headZoneInversePosition = INVERSE_HEAD_ROTATION * headZonePosition;
		headZoneInverseLocalPosition = INVERSE_HEAD_ROTATION * localHeadZonePosition;
		rb = GetComponent<Rigidbody>();
		rbwi = GetComponent<RigidbodyWaterInteraction>();
		collider = GetComponent<Collider>();
		pcc = GetComponent<PlayerColoredCosmetic>();
		triggerLayer = LayerMask.NameToLayer("Gorilla Tag Collider");
		OnReset?.Invoke();
	}

	public void Launch(Vector3 startPosition, Quaternion startRotation, Vector3 velocity, float chargeFrac, VRRig ownerRig, int progress)
	{
		OnLaunch?.Invoke();
		stickyPart.SetParent(base.transform, worldPositionStays: false);
		stickyPart.SetLocalPositionAndRotation(stickyPartLocalPosition, stickyPartLocalRotation);
		stickyPart.localScale = stickyPartLocalScale;
		base.transform.SetPositionAndRotation(startPosition, startRotation);
		base.transform.localScale = Vector3.one * ownerRig.scaleFactor;
		rb.isKinematic = false;
		rb.position = startPosition;
		rb.rotation = startRotation;
		rb.linearVelocity = velocity;
		if (faceVelocityWhileAirborne)
		{
			TickSystem<object>.AddTickCallback(this);
			rb.angularVelocity = Vector3.zero;
		}
		else
		{
			rb.angularVelocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(launchRandomSpinSpeedMinMax.x, launchRandomSpinSpeedMinMax.y);
		}
		rbwi.enabled = true;
		collider.enabled = true;
		if (pcc != null)
		{
			pcc.UpdateColor(ownerRig.playerColor);
		}
	}

	private void StickTo(Transform otherTransform, Vector3 position, Quaternion rotation)
	{
		stickyPart.parent = otherTransform;
		stickyPart.SetPositionAndRotation(position + rotation * stickyPartLocalPosition, rotation * stickyPartLocalRotation);
		rb.isKinematic = true;
		rbwi.enabled = false;
		collider.enabled = false;
	}

	private void OnCollisionEnter(Collision collision)
	{
		TickSystem<object>.RemoveTickCallback(this);
		ContactPoint contact = collision.GetContact(0);
		StickTo(collision.transform, contact.point, alignToHitNormal ? Quaternion.LookRotation(contact.normal, UnityEngine.Random.onUnitSphere) : base.transform.rotation);
		stickEvents.InvokeAll(StickFlags.Wall);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer != triggerLayer)
		{
			return;
		}
		TickSystem<object>.RemoveTickCallback(this);
		Vector3 vector = Time.fixedDeltaTime * 2f * rb.linearVelocity;
		Vector3 vector2 = base.transform.position - vector;
		Vector3 vector3;
		Quaternion rotation;
		if (alignToHitNormal)
		{
			float magnitude = vector.magnitude;
			other.Raycast(new Ray(vector2, vector / magnitude), out var hitInfo, 2f * magnitude);
			vector3 = hitInfo.point;
			rotation = Quaternion.LookRotation(hitInfo.normal, UnityEngine.Random.onUnitSphere);
		}
		else
		{
			vector3 = other.ClosestPoint(vector2);
			rotation = base.transform.rotation;
		}
		VRRig componentInParent = other.GetComponentInParent<VRRig>();
		if (componentInParent != null)
		{
			if (headZoneRadius > 0f && string.Equals(other.name, "SpeakerHeadCollider"))
			{
				other.transform.GetPositionAndRotation(out var position, out var rotation2);
				Vector3 vector4 = rotation2 * headZoneInversePosition + position;
				if ((vector3 - vector4).magnitude <= headZoneRadius * componentInParent.scaleFactor)
				{
					if (componentInParent.isOfflineVRRig)
					{
						StickTo(other.transform, rotation2 * headZoneInverseLocalPosition + position, rotation2 * INVERSE_HEAD_ROTATION);
						stickyPart.localScale *= scaleOnLocalHeadZone;
						stickEvents.InvokeAll(StickFlags.LocalHeadZone);
					}
					else
					{
						StickTo(other.transform, vector4, rotation2 * INVERSE_HEAD_ROTATION);
						stickEvents.InvokeAll(StickFlags.RemoteHeadZone);
					}
					return;
				}
				if (componentInParent.isOfflineVRRig)
				{
					stickyPart.localScale *= scaleOnLocalHead;
				}
			}
			stickEvents.InvokeAll(componentInParent.isOfflineVRRig ? StickFlags.LocalPlayer : StickFlags.RemotePlayer);
		}
		else
		{
			stickEvents.InvokeAll(StickFlags.Wall);
		}
		StickTo(other.transform, vector3, rotation);
	}

	private void OnEnable()
	{
		stickyPart.gameObject.SetActive(value: true);
	}

	private void OnDisable()
	{
		stickyPart.gameObject.SetActive(value: false);
		OnReset?.Invoke();
	}

	public void Tick()
	{
		rb.rotation = Quaternion.LookRotation(rb.linearVelocity);
	}
}
