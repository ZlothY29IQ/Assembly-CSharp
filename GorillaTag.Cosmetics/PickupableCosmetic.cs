using System;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class PickupableCosmetic : PickupableVariant
{
	[SerializeField]
	private InteractionPoint interactionPoint;

	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private Transform raycastOrigin;

	[Tooltip("Allow player to grab the placed object")]
	[SerializeField]
	private bool allowPickupFromGround = true;

	[SerializeField]
	private float autoPickupAfterSeconds;

	[SerializeField]
	private float autoPickupDistance;

	[Tooltip("Amount to offset the placed object from the hit position in the hit normal direction")]
	[SerializeField]
	private float placementOffset;

	[Tooltip("Prevent sticking if the hit surface normal is not within 40 degrees of world up")]
	[SerializeField]
	private bool dontStickToWall;

	[Tooltip("Layers to raycast against for placement")]
	[SerializeField]
	private LayerMask floorLayerMask = 134218241;

	[Tooltip("The distance to check if the banner is close to the floor (from a raycast check).")]
	public float RaycastCheckDist = 0.2f;

	[Tooltip("How many checks should we attempt for a raycast.")]
	public int RaycastChecksMax = 12;

	[FormerlySerializedAs("OnPickup")]
	[Space]
	public UnityEvent OnPickupShared;

	[FormerlySerializedAs("OnPlaced")]
	public UnityEvent OnPlacedShared;

	[SerializeField]
	private bool isBreakable;

	[Tooltip("Particle system played OnBrokenShared")]
	[SerializeField]
	private ParticleSystem breakEffect;

	[Tooltip("Renderers disabled OnBrokenShared and enabled OnPickupShared")]
	[SerializeField]
	private Renderer[] hideOnBreak = new Renderer[0];

	[Tooltip("Time after BreakPlaceable to reset item")]
	[SerializeField]
	private float respawnDelay = 0.5f;

	[FormerlySerializedAs("OnBroken")]
	[Space]
	public UnityEvent OnBrokenShared;

	private static int breakableBitmask = 32;

	private bool placedOnFloor;

	private float placedOnFloorTime = -1f;

	private bool broken;

	private float brokenTime = -1f;

	private VRRig cachedLocalRig;

	private HoldableObject holdableParent;

	private TransferrableObject transferrableParent;

	private RigOwnedPhysicsBody rigOwnedPhysicsBody;

	private double throwSettledTime = -1.0;

	private int landingSide;

	private float scale;

	private Collider bodyCollider;

	[Tooltip("How many directions to test per physics tick (spreads work across frames).")]
	[SerializeField]
	[Min(1f)]
	private int raysPerStep = 3;

	[Tooltip("Run a raycast step only every N physics ticks (1 = every FixedUpdate).")]
	[SerializeField]
	[Min(1f)]
	private int stepEveryNFrames = 2;

	[Tooltip("Small skin so rays start just outside our own collider volume.")]
	[SerializeField]
	[Range(0.005f, 0.1f)]
	private float selfSkinOffset = 0.02f;

	[SerializeField]
	private bool debugPlacementRays;

	private int currentRayIndex;

	private int frameCounter;

	private static readonly Dictionary<int, Vector3[]> directionCache = new Dictionary<int, Vector3[]>();

	private static readonly Vector3[] tmpEmpty = Array.Empty<Vector3>();

	private void Awake()
	{
		rigOwnedPhysicsBody = GetComponent<RigOwnedPhysicsBody>();
		bodyCollider = GetComponent<Collider>();
	}

	private void Start()
	{
		base.enabled = false;
	}

	private void OnEnable()
	{
		if (rigOwnedPhysicsBody != null)
		{
			rigOwnedPhysicsBody.enabled = true;
		}
	}

	private void OnDisable()
	{
		if (rigOwnedPhysicsBody != null)
		{
			rigOwnedPhysicsBody.enabled = false;
		}
	}

	protected internal override void Pickup(bool isAutoPickup = false)
	{
		if (!isAutoPickup)
		{
			OnPickupShared?.Invoke();
		}
		rb.linearVelocity = Vector3.zero;
		rb.isKinematic = true;
		if (holdableParent != null)
		{
			base.transform.parent = holdableParent.transform;
		}
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
		scale = 1f;
		placedOnFloorTime = -1f;
		placedOnFloor = false;
		broken = false;
		brokenTime = -1f;
		if (isBreakable && transferrableParent != null && transferrableParent.IsLocalObject())
		{
			int itemState = (int)transferrableParent.itemState;
			itemState &= ~breakableBitmask;
			transferrableParent.itemState = (TransferrableObject.ItemStates)itemState;
			if (breakEffect != null && breakEffect.isPlaying)
			{
				breakEffect.Stop();
			}
		}
		ShowRenderers(visible: true);
		if (interactionPoint != null)
		{
			interactionPoint.enabled = true;
		}
		base.enabled = false;
	}

	protected internal override void DelayedPickup()
	{
		DelayedPickup_Internal();
	}

	private async void DelayedPickup_Internal()
	{
		await Awaitable.WaitForSecondsAsync(1f);
		Pickup();
	}

	protected internal override void Release(HoldableObject holdable, Vector3 startPosition, Vector3 velocity, float playerScale)
	{
		holdableParent = holdable;
		base.transform.parent = null;
		base.transform.position = startPosition;
		base.transform.localScale = Vector3.one * playerScale;
		rb.isKinematic = false;
		rb.useGravity = true;
		rb.linearVelocity = velocity;
		rb.detectCollisions = true;
		if (!allowPickupFromGround && interactionPoint != null)
		{
			interactionPoint.enabled = false;
		}
		scale = playerScale;
		base.enabled = true;
		transferrableParent = holdableParent as TransferrableObject;
		currentRayIndex = 0;
		frameCounter = 0;
	}

	private void FixedUpdate()
	{
		if (isBreakable && broken)
		{
			if (Time.time > respawnDelay + brokenTime)
			{
				Pickup();
			}
			return;
		}
		if (isBreakable && placedOnFloor)
		{
			bool flag = ((uint)transferrableParent.itemState & (uint)breakableBitmask) != 0;
			if (flag != broken && flag)
			{
				OnBreakReplicated();
			}
		}
		if (autoPickupAfterSeconds > 0f && placedOnFloor && Time.time - placedOnFloorTime > autoPickupAfterSeconds)
		{
			Pickup(isAutoPickup: true);
			ThrowablePickupableCosmetic throwablePickupableCosmetic = transferrableParent as ThrowablePickupableCosmetic;
			if ((bool)throwablePickupableCosmetic)
			{
				throwablePickupableCosmetic.OnReturnToDockPositionShared?.Invoke();
			}
		}
		if (autoPickupDistance > 0f && transferrableParent != null && (transferrableParent.ownerRig.transform.position - base.transform.position).IsLongerThan(autoPickupDistance))
		{
			Pickup();
		}
		if (placedOnFloor || !base.enabled)
		{
			return;
		}
		frameCounter++;
		if (frameCounter % stepEveryNFrames != 0)
		{
			return;
		}
		float maxDistance = RaycastCheckDist * scale;
		int value = floorLayerMask.value;
		Vector3[] cachedDirections = GetCachedDirections(RaycastChecksMax);
		int num = 0;
		while (num < raysPerStep && currentRayIndex < cachedDirections.Length)
		{
			Vector3 vector = cachedDirections[currentRayIndex];
			currentRayIndex++;
			num++;
			if (Physics.Raycast(GetSafeRayOrigin(raycastOrigin.position, vector), vector, out var hitInfo, maxDistance, value, QueryTriggerInteraction.Ignore) && (!dontStickToWall || !(Vector3.Angle(hitInfo.normal, Vector3.up) >= 40f)))
			{
				SettleBanner(hitInfo);
				OnPlacedShared?.Invoke();
				placedOnFloor = true;
				placedOnFloorTime = Time.time;
				break;
			}
		}
		if (currentRayIndex >= cachedDirections.Length)
		{
			currentRayIndex = 0;
		}
	}

	private void SettleBanner(RaycastHit hitInfo)
	{
		rb.isKinematic = true;
		rb.useGravity = false;
		rb.detectCollisions = false;
		Vector3 normal = hitInfo.normal;
		base.transform.position = hitInfo.point + normal * placementOffset;
		Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(base.transform.forward, normal).normalized, normal);
		base.transform.rotation = rotation;
	}

	private Vector3 GetFibonacciSphereDirection(int index, int total)
	{
		float f = Mathf.Acos(1f - 2f * ((float)index + 0.5f) / (float)total);
		float f2 = MathF.PI * (1f + Mathf.Sqrt(5f)) * ((float)index + 0.5f);
		float x = Mathf.Sin(f) * Mathf.Cos(f2);
		float y = Mathf.Sin(f) * Mathf.Sin(f2);
		float z = Mathf.Cos(f);
		return new Vector3(x, y, z).normalized;
	}

	private Vector3[] GetCachedDirections(int count)
	{
		if (count <= 0)
		{
			return tmpEmpty;
		}
		if (directionCache.TryGetValue(count, out var value))
		{
			return value;
		}
		value = new Vector3[count];
		for (int i = 0; i < count; i++)
		{
			value[i] = GetFibonacciSphereDirection(i, count);
		}
		directionCache[count] = value;
		return value;
	}

	private Vector3 GetSafeRayOrigin(Vector3 rawOrigin, Vector3 dir)
	{
		float num = selfSkinOffset;
		if (bodyCollider != null)
		{
			float magnitude = bodyCollider.bounds.extents.magnitude;
			num = Mathf.Max(selfSkinOffset, magnitude * 0.05f);
		}
		return rawOrigin - dir.normalized * num;
	}

	public void BreakPlaceable()
	{
		if (isBreakable && placedOnFloor)
		{
			if (transferrableParent != null && transferrableParent.IsLocalObject())
			{
				int itemState = (int)transferrableParent.itemState;
				itemState |= breakableBitmask;
				transferrableParent.itemState = (TransferrableObject.ItemStates)itemState;
			}
			else
			{
				GTDev.LogError("PickupableCosmetic " + base.gameObject.name + " has no TransferrableObject parent. Break effects cannot be replicated");
			}
		}
	}

	private void OnBreakReplicated()
	{
		PlayBreakEffects();
	}

	protected virtual void PlayBreakEffects()
	{
		if (!isBreakable || !placedOnFloor || broken)
		{
			return;
		}
		broken = true;
		brokenTime = Time.time;
		if (breakEffect != null)
		{
			if (breakEffect.isPlaying)
			{
				breakEffect.Stop();
			}
			breakEffect.Play();
		}
		if (interactionPoint != null)
		{
			interactionPoint.enabled = false;
		}
		ShowRenderers(visible: false);
		OnBrokenShared?.Invoke();
	}

	protected virtual void ShowRenderers(bool visible)
	{
		if (hideOnBreak.IsNullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < hideOnBreak.Length; i++)
		{
			Renderer renderer = hideOnBreak[i];
			if (!(renderer == null))
			{
				renderer.forceRenderingOff = !visible;
			}
		}
	}
}
