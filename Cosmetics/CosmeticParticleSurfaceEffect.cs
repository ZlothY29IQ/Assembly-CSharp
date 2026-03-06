using System;
using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

namespace Cosmetics;

[RequireComponent(typeof(TransferrableObject))]
public class CosmeticParticleSurfaceEffect : MonoBehaviour, ITickSystemTick
{
	[Tooltip("autoStop particle system this many seconds after starting")]
	[SerializeField]
	private float stopAfterSeconds = 3f;

	[Tooltip("particle system to play on start particles")]
	[SerializeField]
	private ParticleSystem particles;

	[Tooltip("Distance in meters to check for a surface hit")]
	[SerializeField]
	private float rayCastDistance = 20f;

	[Tooltip("The position for the start of the rayCast.\nThe forward (z+) axis of this transform will be used as the rayCast direction\nThis should visually line up with the spawned particles")]
	[SerializeField]
	private Transform rayCastOrigin;

	[Tooltip("Use a world direction vector for the raycast instead of the rayCastOrigin forward?")]
	[SerializeField]
	private bool useWorldDirection;

	[SerializeField]
	private Vector3 worldDirection = Vector3.down;

	[Tooltip("Layers to check for surface collision")]
	[SerializeField]
	private LayerMask rayCastLayerMask = 513;

	[Tooltip("Prefab from the global object pool to spawn on surface hit\nIf it should be destroyed on touch, add a SeedPacketTriggerHandler to the prefab")]
	[SerializeField]
	private GameObject surfaceEffectPrefab;

	[Tooltip("Seconds per meter to wait before spawning a surface effect on hit.\n A good value would be somewhat close to 1/particle velocity ")]
	[SerializeField]
	private float placeEffectDelayMultiplier = 3f;

	[Tooltip("Time to wait between spawning surface effects")]
	[SerializeField]
	private float placeEffectCooldown = 2f;

	private float particleStartedTime;

	private bool isSpawning;

	private float lastHitTime = float.MinValue;

	private RaycastHit hitPoint;

	private RaycastHit[] hits = new RaycastHit[5];

	private TransferrableObject transferrableObject;

	private bool isLocal;

	private NetPlayer owner;

	private int surfaceEffectHash;

	private RubberDuckEvents _events;

	private CallLimiter spawnCallLimiter = new CallLimiter(10, 3f);

	private CallLimiter destroyCallLimiter = new CallLimiter(10, 3f);

	private SinglePool _pool;

	private bool foundPool;

	private int currentEffect;

	private List<int> surfaceEffectNum = new List<int>();

	private List<SeedPacketTriggerHandler> surfaceEffects = new List<SeedPacketTriggerHandler>(10);

	public bool TickRunning { get; set; }

	private void Awake()
	{
		transferrableObject = GetComponent<TransferrableObject>();
		if (surfaceEffectPrefab != null)
		{
			surfaceEffectHash = PoolUtils.GameObjHashCode(surfaceEffectPrefab);
		}
	}

	private void OnEnable()
	{
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			owner = ((transferrableObject.myOnlineRig != null) ? transferrableObject.myOnlineRig.creator : ((transferrableObject.myRig != null) ? (transferrableObject.myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : null));
			if (owner != null)
			{
				_events.Init(owner);
				isLocal = owner.IsLocal;
			}
		}
		if (_events != null)
		{
			_events.Activate.reliable = true;
			_events.Deactivate.reliable = true;
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnSpawnReplicated);
			_events.Deactivate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnTriggerEffectReplicated);
		}
		if (ObjectPools.instance == null || !ObjectPools.instance.initialized)
		{
			return;
		}
		if (surfaceEffectHash != 0)
		{
			_pool = ObjectPools.instance.GetPoolByHash(surfaceEffectHash);
			if (_pool != null)
			{
				foundPool = true;
			}
			else
			{
				GTDev.LogError("CosmeticParticleSurfaceEffect " + base.gameObject.name + " no Object pool found for surface effect prefab. Has it been added to Global Object Pools?");
			}
		}
		spawnCallLimiter.Reset();
		destroyCallLimiter.Reset();
		lastHitTime = float.MinValue;
	}

	private void OnDisable()
	{
		StopParticles();
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnSpawnReplicated);
			_events.Deactivate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnTriggerEffectReplicated);
			_events.Dispose();
			_events = null;
		}
		surfaceEffectNum.Clear();
		foreach (SeedPacketTriggerHandler surfaceEffect in surfaceEffects)
		{
			if (!(surfaceEffect == null))
			{
				surfaceEffect.onTriggerEntered.RemoveListener(OnTriggerEffectLocal);
			}
		}
		surfaceEffects.Clear();
	}

	private void OnDestroy()
	{
		surfaceEffectNum.Clear();
		surfaceEffects.Clear();
	}

	public void StartParticles()
	{
		if (!isSpawning)
		{
			isSpawning = true;
			particleStartedTime = Time.time;
			if (!particles.isPlaying)
			{
				particles.Play();
			}
		}
		if (!TickRunning)
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	public void StopParticles()
	{
		if (TickRunning)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
		isSpawning = false;
		particleStartedTime = float.MinValue;
		lastHitTime = float.MinValue;
		if (particles.isPlaying)
		{
			particles.Stop();
		}
	}

	public void Tick()
	{
		if (transferrableObject == null || !transferrableObject.InHand())
		{
			StopParticles();
		}
		else if (isSpawning && stopAfterSeconds > 0f && Time.time >= particleStartedTime + stopAfterSeconds)
		{
			StopParticles();
		}
		else
		{
			if (!isLocal || !isSpawning || !(Time.time > placeEffectCooldown + lastHitTime))
			{
				return;
			}
			int num = Physics.RaycastNonAlloc(rayCastOrigin.position, useWorldDirection ? worldDirection : rayCastOrigin.forward, hits, rayCastDistance, rayCastLayerMask, QueryTriggerInteraction.Ignore);
			if (num <= 0)
			{
				return;
			}
			int num2 = 0;
			float distance = hits[num2].distance;
			for (int i = 1; i < num; i++)
			{
				if (hits[i].distance < distance)
				{
					num2 = i;
					distance = hits[i].distance;
				}
			}
			hitPoint = hits[num2];
			lastHitTime = Time.time;
			Invoke("SpawnEffect", distance * placeEffectDelayMultiplier);
		}
	}

	private void SpawnEffect()
	{
		if (isLocal)
		{
			long num = BitPackUtils.PackWorldPosForNetwork(hitPoint.point);
			long num2 = BitPackUtils.PackWorldPosForNetwork(hitPoint.normal);
			int num3 = currentEffect;
			currentEffect++;
			if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				_events.Activate.RaiseOthers(num, num2, num3);
			}
			SpawnLocal(hitPoint.point, hitPoint.normal, num3);
		}
	}

	private void OnSpawnReplicated(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (!this || sender != target || owner == null || info.senderID != owner.ActorNumber)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnSpawnReplicated");
		if (!spawnCallLimiter.CheckCallTime(Time.time) || args.Length != 3 || !(args[0] is long) || !(args[1] is long) || !(args[2] is int))
		{
			return;
		}
		Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork((long)args[0]);
		Vector3 v2 = BitPackUtils.UnpackWorldPosFromNetwork((long)args[1]);
		if (v.IsValid(10000f) && v2.IsValid(10000f) && !(Vector3.Distance(rayCastOrigin.position, v) > rayCastDistance + 2f))
		{
			v2.Normalize();
			if (v2 == Vector3.zero)
			{
				v2 = Vector3.up;
			}
			int identifier = (int)args[2];
			SpawnLocal(v, v2, identifier);
		}
	}

	private void SpawnLocal(Vector3 position, Vector3 up, int identifier)
	{
		if (surfaceEffectHash != 0 && !foundPool)
		{
			_pool = ObjectPools.instance.GetPoolByHash(surfaceEffectHash);
			if (_pool == null)
			{
				return;
			}
			foundPool = true;
		}
		if (!foundPool || _pool.GetInactiveCount() <= 0)
		{
			return;
		}
		ClearOldObjects();
		GameObject obj = _pool.Instantiate();
		obj.transform.position = position;
		obj.transform.up = up;
		if (obj.TryGetComponent<SeedPacketTriggerHandler>(out var component))
		{
			int num = surfaceEffects.IndexOf(component);
			if (num >= 0)
			{
				surfaceEffectNum[num] = identifier;
			}
			else
			{
				surfaceEffectNum.Add(identifier);
				surfaceEffects.Add(component);
			}
			component.onTriggerEntered.AddListener(OnTriggerEffectLocal);
		}
	}

	private void ClearOldObjects()
	{
		for (int num = surfaceEffects.Count - 1; num >= 0; num--)
		{
			if (surfaceEffects[num] == null)
			{
				surfaceEffects.RemoveAt(num);
				surfaceEffectNum.RemoveAt(num);
			}
			else if (!surfaceEffects[num].gameObject.activeSelf)
			{
				surfaceEffects[num].onTriggerEntered.RemoveListener(OnTriggerEffectLocal);
				surfaceEffects.RemoveAt(num);
				surfaceEffectNum.RemoveAt(num);
			}
		}
	}

	private void OnTriggerEffectLocal(SeedPacketTriggerHandler seedPacketTriggerHandlerTriggerHandlerEvent)
	{
		int num = surfaceEffects.IndexOf(seedPacketTriggerHandlerTriggerHandlerEvent);
		if (num >= 0 && num < surfaceEffectNum.Count)
		{
			int num2 = surfaceEffectNum[num];
			if (PhotonNetwork.InRoom && _events != null && _events.Deactivate != null)
			{
				_events.Deactivate.RaiseOthers(num2);
			}
			surfaceEffects.RemoveAt(num);
			surfaceEffectNum.RemoveAt(num);
		}
	}

	private void OnTriggerEffectReplicated(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnTriggerEffectReplicated");
		if (!destroyCallLimiter.CheckCallTime(Time.time) || args.Length != 1 || !(args[0] is int))
		{
			return;
		}
		ClearOldObjects();
		int item = (int)args[0];
		int num = surfaceEffectNum.IndexOf(item);
		if (num >= 0 && num < surfaceEffects.Count)
		{
			SeedPacketTriggerHandler seedPacketTriggerHandler = surfaceEffects[num];
			if (seedPacketTriggerHandler != null)
			{
				seedPacketTriggerHandler.ToggleEffects();
				seedPacketTriggerHandler.onTriggerEntered.RemoveListener(OnTriggerEffectLocal);
			}
			surfaceEffects.RemoveAt(num);
			surfaceEffectNum.RemoveAt(num);
		}
	}
}
