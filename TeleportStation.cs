using GorillaNetworking;
using GorillaTagScripts;
using UnityEngine;

public class TeleportStation : MonoBehaviour
{
	[SerializeField]
	private Transform target;

	[SerializeField]
	private Vector3 targetPos;

	[SerializeField]
	private float targetRot;

	[SerializeField]
	private Vector3 targetSlop;

	[SerializeField]
	private GTZone teleportToZone;

	[SerializeField]
	private XSceneRef sourceFriendColliderRef;

	[SerializeField]
	private XSceneRef destinationFriendColliderRef;

	[SerializeField]
	private XSceneRef destinationJoinTriggerRef;

	private GorillaFriendCollider sourceFriendCollider;

	private GorillaFriendCollider destinationFriendCollider;

	private GorillaNetworkJoinTrigger destinationJoinTrigger;

	[SerializeField]
	private int effectTime;

	private void Start()
	{
		if (!sourceFriendColliderRef.TryResolve(out sourceFriendCollider))
		{
			Debug.LogError($"Unable to resolve source friend collider: {sourceFriendColliderRef}!");
		}
		if (!destinationFriendColliderRef.TryResolve(out destinationFriendCollider))
		{
			Debug.LogError($"Unable to resolve source friend collider: {destinationFriendCollider}!");
		}
		if (!destinationJoinTriggerRef.TryResolve(out destinationJoinTrigger))
		{
			Debug.LogError($"Unable to resolve source friend collider: {destinationJoinTriggerRef}!");
		}
	}

	public void Attempt1PTeleport(PhotonMessageInfoWrapped info)
	{
		TeleportStationManager.Instance.FirstPersonTeleport(targetPos, targetRot, targetSlop, teleportToZone, sourceFriendCollider, destinationFriendCollider, destinationJoinTrigger, effectTime);
	}

	public void Attempt3PTeleport(PhotonMessageInfoWrapped info)
	{
		if (!VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig))
		{
			return;
		}
		TeleportStationManager.Instance.ThirdPersonTeleport(playerRig.Rig, effectTime);
		if (!NetworkSystem.Instance.SessionIsPrivate && FriendshipGroupDetection.Instance.IsInParty && FriendshipGroupDetection.Instance.IsInMyGroup(info.Sender.UserId))
		{
			NetPlayer localPlayer = NetworkSystem.Instance.LocalPlayer;
			if (sourceFriendCollider.playerIDsCurrentlyTouching.Contains(localPlayer.UserId))
			{
				Attempt1PTeleport(info);
			}
			else
			{
				FriendshipGroupDetection.Instance.LeaveParty();
			}
		}
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
}
