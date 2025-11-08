using GorillaTag;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class GeodeItem : TransferrableObject
{
	[Tooltip("This GameObject will activate when the geode hits the ground with enough force.")]
	public GameObject effectsGameObject;

	public LayerMask collisionLayerMask;

	[Tooltip("Used to calculate velocity of the geode.")]
	public GorillaVelocityEstimator velocityEstimator;

	public float cooldown = 5f;

	[Tooltip("The velocity of the geode must be greater than this value to activate the effect.")]
	public float minHitVelocity = 0.2f;

	[Tooltip("Geode's full mesh before cracking")]
	public GameObject geodeFullMesh;

	[Tooltip("Geode's cracked open half different meshes, picked randomly")]
	public GameObject[] geodeCrackedMeshes;

	[Tooltip("The distance between te geode and the layer mask to detect whether it hits it")]
	public float rayCastMaxDistance = 0.2f;

	[FormerlySerializedAs("collisionRadius")]
	public float sphereRayRadius = 0.05f;

	[DebugReadout]
	private float cooldownRemaining;

	[DebugReadout]
	private bool hitLastFrame;

	[SerializeField]
	private AudioSource audioSource;

	public bool randomizeGeode = true;

	public UnityEvent<GeodeItem> OnGeodeCracked;

	public UnityEvent<GeodeItem> OnGeodeGrabbed;

	private bool hasEffectsGameObject;

	private bool effectsHaveBeenPlayed;

	private RaycastHit hit;

	private RaycastHit[] collidersHit = new RaycastHit[20];

	private ItemStates currentItemState;

	private ItemStates prevItemState;

	private int index;

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		hasEffectsGameObject = effectsGameObject != null;
		effectsHaveBeenPlayed = false;
	}

	protected override void Start()
	{
		base.Start();
		itemState = ItemStates.State0;
		prevItemState = ItemStates.State0;
		InitToDefault();
	}

	public override void ResetToDefaultState()
	{
		base.ResetToDefaultState();
		InitToDefault();
		itemState = ItemStates.State0;
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (itemState == ItemStates.State0)
		{
			return false;
		}
		if (InHand())
		{
			return false;
		}
		return true;
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		base.OnGrab(pointGrabbed, grabbingHand);
		OnGeodeGrabbed?.Invoke(this);
	}

	private void InitToDefault()
	{
		cooldownRemaining = 0f;
		effectsHaveBeenPlayed = false;
		if (hasEffectsGameObject)
		{
			effectsGameObject.SetActive(value: false);
		}
		geodeFullMesh.SetActive(value: true);
		for (int i = 0; i < geodeCrackedMeshes.Length; i++)
		{
			geodeCrackedMeshes[i].SetActive(value: false);
		}
		hitLastFrame = false;
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (itemState == ItemStates.State1)
		{
			cooldownRemaining -= Time.deltaTime;
			if (cooldownRemaining <= 0f)
			{
				itemState = ItemStates.State0;
				OnItemStateChanged();
			}
		}
		else if (!(velocityEstimator.linearVelocity.magnitude < minHitVelocity))
		{
			if (InHand())
			{
				int num = Physics.SphereCastNonAlloc(geodeFullMesh.transform.position, sphereRayRadius * Mathf.Abs(geodeFullMesh.transform.lossyScale.x), geodeFullMesh.transform.TransformDirection(Vector3.forward), collidersHit, rayCastMaxDistance, collisionLayerMask, QueryTriggerInteraction.Collide);
				hitLastFrame = num > 0;
			}
			if (hitLastFrame && GorillaParent.hasInstance)
			{
				OnGeodeCracked?.Invoke(this);
				itemState = ItemStates.State1;
				cooldownRemaining = cooldown;
				index = (randomizeGeode ? RandomPickCrackedGeode() : 0);
			}
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		currentItemState = itemState;
		if (currentItemState != prevItemState)
		{
			OnItemStateChanged();
		}
		prevItemState = currentItemState;
	}

	private void OnItemStateChanged()
	{
		if (itemState == ItemStates.State0)
		{
			InitToDefault();
			return;
		}
		geodeFullMesh.SetActive(value: false);
		for (int i = 0; i < geodeCrackedMeshes.Length; i++)
		{
			geodeCrackedMeshes[i].SetActive(i == index);
		}
		if (NetworkSystem.Instance.InRoom && GorillaGameManager.instance != null && !effectsHaveBeenPlayed && VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.LocalPlayer, out var playerRig))
		{
			playerRig.Rig.netView.SendRPC("RPC_PlayGeodeEffect", RpcTarget.All, geodeFullMesh.transform.position);
			effectsHaveBeenPlayed = true;
		}
		if (!NetworkSystem.Instance.InRoom && !effectsHaveBeenPlayed)
		{
			if ((bool)audioSource)
			{
				audioSource.GTPlay();
			}
			effectsHaveBeenPlayed = true;
		}
	}

	private int RandomPickCrackedGeode()
	{
		return Random.Range(0, geodeCrackedMeshes.Length);
	}
}
