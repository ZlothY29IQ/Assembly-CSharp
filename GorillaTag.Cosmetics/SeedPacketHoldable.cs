using System;
using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

[RequireComponent(typeof(TransferrableObject))]
public class SeedPacketHoldable : MonoBehaviour
{
	[SerializeField]
	private float cooldown;

	[SerializeField]
	private ParticleSystem particles;

	[SerializeField]
	private float pouringAngle;

	[SerializeField]
	private float pouringRaycastDistance = 5f;

	[SerializeField]
	private LayerMask raycastLayerMask;

	[SerializeField]
	private float placeEffectDelayMultiplier = 10f;

	[SerializeField]
	private GameObject flowerEffectPrefab;

	private List<SeedPacketTriggerHandler> pooledObjects = new List<SeedPacketTriggerHandler>();

	private CallLimiter callLimiter = new CallLimiter(10, 3f);

	private int flowerEffectHash;

	private Vector3 hitPoint;

	private TransferrableObject transferrableObject;

	private bool isPouring = true;

	private float pouringStartedTime;

	private RubberDuckEvents _events;

	private void Awake()
	{
		transferrableObject = GetComponent<TransferrableObject>();
		flowerEffectHash = PoolUtils.GameObjHashCode(flowerEffectPrefab);
	}

	private void OnEnable()
	{
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((transferrableObject.myOnlineRig != null) ? transferrableObject.myOnlineRig.creator : ((transferrableObject.myRig != null) ? (transferrableObject.myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : null));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(SyncTriggerEffect);
		}
	}

	private void OnDisable()
	{
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(SyncTriggerEffect);
			_events.Dispose();
			_events = null;
		}
	}

	private void OnDestroy()
	{
		pooledObjects.Clear();
	}

	private void Update()
	{
		if (!transferrableObject.InHand())
		{
			return;
		}
		if (!isPouring && Vector3.Angle(base.transform.up, Vector3.down) <= pouringAngle)
		{
			StartPouring();
			if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, pouringRaycastDistance, raycastLayerMask))
			{
				hitPoint = hitInfo.point;
				Invoke("SpawnEffect", hitInfo.distance * placeEffectDelayMultiplier);
			}
		}
		if (isPouring && Time.time - pouringStartedTime >= cooldown)
		{
			isPouring = false;
		}
	}

	private void StartPouring()
	{
		if ((bool)particles)
		{
			particles.Play();
		}
		isPouring = true;
		pouringStartedTime = Time.time;
	}

	private void SpawnEffect()
	{
		GameObject obj = ObjectPools.instance.Instantiate(flowerEffectHash);
		obj.transform.position = hitPoint;
		if (obj.TryGetComponent<SeedPacketTriggerHandler>(out var component))
		{
			pooledObjects.Add(component);
			component.onTriggerEntered.AddListener(SyncTriggerEffectForOthers);
		}
	}

	private void SyncTriggerEffectForOthers(SeedPacketTriggerHandler seedPacketTriggerHandlerTriggerHandlerEvent)
	{
		int num = pooledObjects.IndexOf(seedPacketTriggerHandlerTriggerHandlerEvent);
		if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
		{
			_events.Activate.RaiseOthers(num);
		}
	}

	private void SyncTriggerEffect(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target || args.Length != 1)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "SyncTriggerEffect");
		if (callLimiter.CheckCallTime(Time.time))
		{
			int num = (int)args[0];
			if (num >= 0 || num < pooledObjects.Count)
			{
				pooledObjects[num].ToggleEffects();
			}
		}
	}
}
