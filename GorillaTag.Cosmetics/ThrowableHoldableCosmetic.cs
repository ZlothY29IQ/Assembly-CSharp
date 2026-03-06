using System;
using System.Collections;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaTag.Shared.Scripts;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class ThrowableHoldableCosmetic : TransferrableObject
{
	[Tooltip("Projectile prefab from the global object pool that gets spawned when this object is thrown")]
	[FormerlySerializedAs("firecrackerProjectilePrefab")]
	[SerializeField]
	private GameObject projectilePrefab;

	[Tooltip(" A second projectile prefab that will be spawned if UseAlternativeProjectile is called")]
	[SerializeField]
	private GameObject alternativeProjectilePrefab;

	[Tooltip("Objects on the body that should be hidden when the projectile is spawned")]
	[SerializeField]
	private GameObject disableWhenThrown;

	private CallLimiter firecrackerCallLimiter = new CallLimiter(10, 3f);

	[SerializeField]
	private float respawnCooldown = 1f;

	private CosmeticEffectsOnPlayers playersEffect;

	private int projectileHash;

	private int alternativeProjectileHash;

	private int currentProjectileHash;

	private bool forceBackToDock;

	private WaitForSeconds respawnWait;

	private RubberDuckEvents _events;

	internal override void OnEnable()
	{
		base.OnEnable();
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((base.myOnlineRig != null) ? base.myOnlineRig.creator : ((!(base.myRig != null)) ? null : ((base.myRig.creator != null) ? base.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnThrowEvent);
		}
		forceBackToDock = false;
	}

	protected override void Awake()
	{
		base.Awake();
		projectileHash = PoolUtils.GameObjHashCode(projectilePrefab);
		if (alternativeProjectilePrefab != null)
		{
			alternativeProjectileHash = PoolUtils.GameObjHashCode(alternativeProjectilePrefab);
		}
		currentProjectileHash = projectileHash;
		playersEffect = GetComponentInChildren<CosmeticEffectsOnPlayers>();
		respawnWait = new WaitForSeconds(respawnCooldown);
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (disableWhenThrown.gameObject.activeSelf)
		{
			base.OnGrab(pointGrabbed, grabbingHand);
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (VRRigCache.Instance.localRig.Rig != ownerRig)
		{
			return false;
		}
		Vector3 position = base.transform.position;
		Quaternion rotation = base.transform.rotation;
		bool isLeftHand = releasingHand == EquipmentInteractor.instance.leftHand;
		Vector3 averageVelocity = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeftHand).GetAverageVelocity(worldSpace: true);
		float scale = GTPlayer.Instance.scale;
		if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
		{
			_events.Activate.RaiseOthers(position, rotation, averageVelocity, scale);
		}
		OnThrowLocal(position, rotation, averageVelocity, ownerRig);
		return true;
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnThrowEvent);
			_events.Dispose();
			_events = null;
		}
	}

	public void UseAlternativeProjectile()
	{
		if (alternativeProjectilePrefab != null)
		{
			currentProjectileHash = alternativeProjectileHash;
		}
	}

	public void ForceBackToDock()
	{
		forceBackToDock = true;
	}

	private IEnumerator ReEnableAfterDelay(GameObject obj)
	{
		yield return respawnWait;
		obj.SetActive(value: true);
	}

	private void OnThrowEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target || args.Length != 4 || info.senderID != ownerRig.creator.ActorNumber)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnThrowEvent");
		if (firecrackerCallLimiter.CheckCallTime(Time.time) && args[0] is Vector3 v && args[1] is Quaternion q && args[2] is Vector3 inVel && args[3] is float value)
		{
			Vector3 velocity = targetRig.ClampVelocityRelativeToPlayerSafe(inVel, 40f);
			value.ClampSafe(0.01f, 1f);
			if (q.IsValid() && v.IsValid(10000f) && targetRig.IsPositionInRange(v, 4f))
			{
				OnThrowLocal(v, q, velocity, ownerRig);
			}
		}
	}

	private void OnThrowLocal(Vector3 startPos, Quaternion rotation, Vector3 velocity, VRRig ownerRig)
	{
		disableWhenThrown.SetActive(value: false);
		if (forceBackToDock)
		{
			forceBackToDock = false;
			StartCoroutine(ReEnableAfterDelay(disableWhenThrown));
			return;
		}
		IProjectile component = ObjectPools.instance.Instantiate(currentProjectileHash).GetComponent<IProjectile>();
		if (component is FirecrackerProjectile firecrackerProjectile)
		{
			if (networkedStateEvents != SyncOptions.None)
			{
				int state = (int)(itemState & (ItemStates)(-65));
				firecrackerProjectile.SetTransferrableState(networkedStateEvents, state);
			}
			firecrackerProjectile.OnDetonationComplete.AddListener(HitComplete);
			firecrackerProjectile.OnDetonationStart.AddListener(HitStart);
		}
		else if (component is FartBagThrowable fartBagThrowable)
		{
			fartBagThrowable.OnDeflated += HitComplete;
			fartBagThrowable.ParentTransferable = this;
		}
		component.Launch(startPos, rotation, velocity, 1f, ownerRig);
		currentProjectileHash = projectileHash;
	}

	private void HitStart(FirecrackerProjectile firecracker, Vector3 contactPos)
	{
		if (!(firecracker == null) && !(playersEffect == null))
		{
			playersEffect.ApplyAllEffectsByDistance(contactPos);
		}
	}

	private void HitComplete(IProjectile projectile)
	{
		if (projectile == null)
		{
			return;
		}
		if (IsLocalObject() && networkedStateEvents != SyncOptions.None && resetOnDocked)
		{
			switch (networkedStateEvents)
			{
			case SyncOptions.Bool:
				ResetStateBools();
				break;
			case SyncOptions.Int:
				SetItemStateInt(0);
				break;
			}
		}
		if (projectile is FirecrackerProjectile firecrackerProjectile)
		{
			firecrackerProjectile.OnDetonationStart.RemoveListener(HitStart);
			firecrackerProjectile.OnDetonationComplete.RemoveListener(HitComplete);
			ObjectPools.instance.Destroy(firecrackerProjectile.gameObject);
		}
		else if (projectile is FartBagThrowable fartBagThrowable)
		{
			fartBagThrowable.OnDeflated -= HitComplete;
			ObjectPools.instance.Destroy(fartBagThrowable.gameObject);
		}
		StartCoroutine(ReEnableAfterDelay(disableWhenThrown));
	}
}
