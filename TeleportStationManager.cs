using System.Collections.Generic;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts;
using UnityEngine;

public class TeleportStationManager : MonoBehaviour
{
	public class FPTPort
	{
		private Vector3 targetPos;

		private float targetRot;

		private Vector3 targetSlop;

		private GTZone teleportToZone;

		private GorillaFriendCollider sourceFriendCollider;

		private GorillaFriendCollider destinationFriendCollider;

		private GorillaNetworkJoinTrigger destinationJoinTrigger;

		private float effectTime;

		private float effectTimeRemains;

		private GameObject firstPersonEffect;

		private int phase;

		public float EffectTimeRemains => effectTimeRemains;

		public bool Done => phase > 3;

		public FPTPort(Vector3 targetPos, float targetRot, Vector3 targetSlop, GTZone teleportToZone, GorillaFriendCollider sourceFriendCollider, GorillaFriendCollider destinationFriendCollider, GorillaNetworkJoinTrigger destinationJoinTrigger, int effectTime, GameObject firstPersonEffect)
		{
			this.targetPos = targetPos;
			this.targetRot = targetRot;
			this.targetSlop = targetSlop;
			this.teleportToZone = teleportToZone;
			this.sourceFriendCollider = sourceFriendCollider;
			this.destinationFriendCollider = destinationFriendCollider;
			this.destinationJoinTrigger = destinationJoinTrigger;
			this.effectTime = effectTime;
			effectTimeRemains = effectTime;
			this.firstPersonEffect = firstPersonEffect;
		}

		public void Tick(float deltaTime, GameObject go)
		{
			GTPlayer instance = GTPlayer.Instance;
			if (instance == null)
			{
				Debug.LogError("[TeleportStation] GTPlayer.Instance is null.");
				return;
			}
			switch (phase)
			{
			case 0:
				instance.disableMovement = true;
				instance.SetGravityOverride(go, null);
				firstPersonEffect.transform.parent = Camera.main.transform;
				firstPersonEffect.transform.localPosition = Vector3.zero;
				firstPersonEffect.SetActive(value: true);
				phase++;
				break;
			case 1:
				if (effectTimeRemains < effectTime / 2f)
				{
					Physics.SyncTransforms();
					phase++;
				}
				break;
			case 2:
				targetPos.x += Random.Range(0f - targetSlop.x, targetSlop.x);
				targetPos.z += Random.Range(0f - targetSlop.z, targetSlop.z);
				instance.TeleportTo(targetPos, Quaternion.Euler(0f, targetRot + Random.Range(0f - targetSlop.y, targetSlop.y), 0f), keepVelocity: false, center: true);
				if (teleportToZone != GTZone.none)
				{
					ZoneManagement.SetActiveZone(teleportToZone);
				}
				if (NetworkSystem.Instance.InRoom)
				{
					if (!NetworkSystem.Instance.SessionIsPrivate)
					{
						if (FriendshipGroupDetection.Instance.IsInParty)
						{
							FriendshipGroupDetection.Instance.LeaveParty();
						}
						SetupFriendGroup(sourceFriendCollider, destinationFriendCollider);
						PhotonNetworkController.Instance.AttemptToJoinPublicRoom(destinationJoinTrigger, JoinType.JoinWithNearby);
					}
				}
				else
				{
					SetupFriendGroup(sourceFriendCollider, destinationFriendCollider);
					PhotonNetworkController.Instance.AttemptToJoinPublicRoom(destinationJoinTrigger, JoinType.JoinWithNearby);
				}
				phase++;
				break;
			case 3:
				if (effectTimeRemains < 0f)
				{
					firstPersonEffect.SetActive(value: false);
					firstPersonEffect.transform.parent = go.transform;
					firstPersonEffect.transform.localPosition = Vector3.zero;
					instance.disableMovement = false;
					instance.UnsetGravityOverride(go);
					phase++;
				}
				break;
			}
			effectTimeRemains -= deltaTime;
		}

		private int LowestActorNumberInFriendCollider()
		{
			sourceFriendCollider.RefreshPlayersWithinBounds();
			destinationFriendCollider.RefreshPlayersWithinBounds();
			int num = int.MaxValue;
			NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
			for (int i = 0; i < allNetPlayers.Length; i++)
			{
				if (num > allNetPlayers[i].ActorNumber && (sourceFriendCollider.playerIDsCurrentlyTouching.Contains(allNetPlayers[i].UserId) || destinationFriendCollider.playerIDsCurrentlyTouching.Contains(allNetPlayers[i].UserId)))
				{
					num = allNetPlayers[i].ActorNumber;
				}
			}
			return num;
		}

		private void SetupFriendGroup(GorillaFriendCollider source, GorillaFriendCollider destination)
		{
			PhotonNetworkController.Instance.shuffler = Random.Range(0, 99).ToString().PadLeft(2, '0') + Random.Range(0, 99999999).ToString().PadLeft(8, '0');
			PhotonNetworkController.Instance.keyStr = Random.Range(0, 99999999).ToString().PadLeft(8, '0');
		}
	}

	public class TPTPort
	{
		private VRRig rig;

		private float effectTimeRemains;

		private GameObject startEffect;

		private GameObject endEffect;

		private int phase;

		public float EffectTimeRemains => effectTimeRemains;

		public bool Done => phase > 1;

		public TPTPort(VRRig rig, int effectTime, GameObject startEffect, GameObject endEffect)
		{
			this.rig = rig;
			effectTimeRemains = effectTime;
			this.startEffect = startEffect;
			this.endEffect = endEffect;
		}

		public void Tick(float deltaTime)
		{
			switch (phase)
			{
			case 0:
				rig.DeactivateAllRenderers();
				startEffect.transform.position = rig.transform.position;
				startEffect.SetActive(value: true);
				phase++;
				break;
			case 1:
				if (!(effectTimeRemains > 0f))
				{
					rig.ReactivateAllRenderers();
					endEffect.transform.position = rig.transform.position;
					endEffect.SetActive(value: true);
					phase++;
				}
				break;
			}
			effectTimeRemains -= deltaTime;
		}
	}

	private static TeleportStationManager __instance;

	private const int _3RD_PERSON_EFFECTS_CACHE_SIZE = 20;

	private GameObject[] thirdPersonEffectStarts = new GameObject[20];

	private GameObject[] thirdPersonEffectEnds = new GameObject[20];

	private GameObject firstPersonEffect;

	private int effectsIndex;

	private FPTPort firstPersonTPort;

	private List<TPTPort> thirdPersonTPort = new List<TPTPort>();

	private bool ready;

	public static TeleportStationManager Instance => __instance;

	private void Awake()
	{
		if (!(__instance != null))
		{
			__instance = this;
		}
	}

	public static void Initialize(GameObject fPersonEffect, GameObject thirdPersonEffectStart, GameObject thirdPersonEffectEnd)
	{
		if (!__instance.ready)
		{
			__instance.firstPersonEffect = Object.Instantiate(fPersonEffect, __instance.transform);
			__instance.thirdPersonEffectStarts = new GameObject[20];
			__instance.thirdPersonEffectEnds = new GameObject[20];
			for (int i = 0; i < 20; i++)
			{
				__instance.thirdPersonEffectStarts[i] = Object.Instantiate(thirdPersonEffectStart, __instance.transform);
				__instance.thirdPersonEffectEnds[i] = Object.Instantiate(thirdPersonEffectEnd, __instance.transform);
			}
			__instance.effectsIndex = 0;
			__instance.ready = true;
		}
	}

	private void Update()
	{
		if (firstPersonTPort != null)
		{
			firstPersonTPort.Tick(Time.deltaTime, base.gameObject);
			if (firstPersonTPort.Done)
			{
				firstPersonTPort = null;
			}
		}
		for (int num = thirdPersonTPort.Count - 1; num >= 0; num--)
		{
			thirdPersonTPort[num].Tick(Time.deltaTime);
			if (thirdPersonTPort[num].Done)
			{
				thirdPersonTPort.RemoveAt(num);
			}
		}
	}

	public void ThirdPersonTeleport(VRRig rig, int effectTime)
	{
		thirdPersonTPort.Add(new TPTPort(rig, effectTime, thirdPersonEffectStarts[effectsIndex], thirdPersonEffectEnds[effectsIndex]));
		effectsIndex = (effectsIndex + 1) % 20;
	}

	public void FirstPersonTeleport(Vector3 targetPos, float targetRot, Vector3 targetSlop, GTZone teleportToZone, GorillaFriendCollider sourceFriendCollider, GorillaFriendCollider destinationFriendCollider, GorillaNetworkJoinTrigger destinationJoinTrigger, int effectTime)
	{
		if (firstPersonTPort == null)
		{
			firstPersonTPort = new FPTPort(targetPos, targetRot, targetSlop, teleportToZone, sourceFriendCollider, destinationFriendCollider, destinationJoinTrigger, effectTime, firstPersonEffect);
		}
	}
}
