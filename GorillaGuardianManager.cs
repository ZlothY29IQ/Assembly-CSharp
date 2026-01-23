using Fusion;
using GorillaGameModes;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public sealed class GorillaGuardianManager : GorillaGameManager
{
	[Space]
	[SerializeField]
	private float slapFrontAlignmentThreshold = 0.7f;

	[SerializeField]
	private float slapBackAlignmentThreshold = 0.7f;

	[SerializeField]
	private float launchMinimumStrength = 6f;

	[SerializeField]
	private float launchStrengthMultiplier = 1f;

	[SerializeField]
	private float launchGroundHeadCheckDist = 1.2f;

	[SerializeField]
	private float launchGroundHandCheckDist = 0.4f;

	[SerializeField]
	private float launchGroundKickup = 3f;

	[Space]
	[SerializeField]
	private float slamTriggerTapSpeed = 7f;

	[SerializeField]
	private float slamMaxTapSpeed = 16f;

	[SerializeField]
	private float slamTriggerAngle = 0.7f;

	[SerializeField]
	private float slamRadius = 2.4f;

	[SerializeField]
	private float slamMinStrengthMultiplier = 3f;

	[SerializeField]
	private float slamMaxStrengthMultiplier = 10f;

	[Space]
	[SerializeField]
	private GameObject slapImpactPrefab;

	[SerializeField]
	private GameObject slamImpactPrefab;

	[Space]
	[SerializeField]
	private float hapticStrength = 1f;

	[SerializeField]
	private float hapticDuration = 1f;

	private float requiredGuardianDistance = 10f;

	private float maxLaunchVelocity = 20f;

	public bool isPlaying { get; private set; }

	public override void StartPlaying()
	{
		base.StartPlaying();
		isPlaying = true;
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		foreach (GorillaGuardianZoneManager zoneManager in GorillaGuardianZoneManager.zoneManagers)
		{
			zoneManager.StartPlaying();
		}
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		isPlaying = false;
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		foreach (GorillaGuardianZoneManager zoneManager in GorillaGuardianZoneManager.zoneManagers)
		{
			zoneManager.StopPlaying();
		}
	}

	public override void ResetGame()
	{
		base.ResetGame();
	}

	internal override void NetworkLinkSetup(GameModeSerializer netSerializer)
	{
		base.NetworkLinkSetup(netSerializer);
		netSerializer.AddRPCComponent<GuardianRPCs>();
	}

	public override void AddFusionDataBehaviour(NetworkObject behaviour)
	{
	}

	public override void OnSerializeRead(object newData)
	{
	}

	public override object OnSerializeWrite()
	{
		return null;
	}

	public override bool LocalCanTag(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		if (IsPlayerGuardian(myPlayer))
		{
			return !IsHoldingPlayer();
		}
		return false;
	}

	public override bool LocalIsTagged(NetPlayer player)
	{
		return false;
	}

	public override bool CanJoinFrienship(NetPlayer player)
	{
		if (player != null)
		{
			return !IsPlayerGuardian(player);
		}
		return false;
	}

	public bool IsPlayerGuardian(NetPlayer player)
	{
		foreach (GorillaGuardianZoneManager zoneManager in GorillaGuardianZoneManager.zoneManagers)
		{
			if (zoneManager.IsPlayerGuardian(player))
			{
				return true;
			}
		}
		return false;
	}

	public void RequestEjectGuardian(NetPlayer player)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			EjectGuardian(player);
		}
		else
		{
			GorillaGameModes.GameMode.ActiveNetworkHandler.SendRPC("GuardianRequestEject", false);
		}
	}

	public void EjectGuardian(NetPlayer player)
	{
		foreach (GorillaGuardianZoneManager zoneManager in GorillaGuardianZoneManager.zoneManagers)
		{
			if (zoneManager.IsPlayerGuardian(player))
			{
				zoneManager.SetGuardian(null);
			}
		}
	}

	public void LaunchPlayer(NetPlayer launcher, Vector3 velocity)
	{
		if (VRRigCache.Instance.TryGetVrrig(launcher, out var playerRig) && !(Vector3.Magnitude(VRRigCache.Instance.localRig.Rig.transform.position - playerRig.Rig.transform.position) > requiredGuardianDistance + Mathf.Epsilon) && !(velocity.sqrMagnitude > maxLaunchVelocity * maxLaunchVelocity))
		{
			GTPlayer.Instance.DoLaunch(velocity);
		}
	}

	public override void LocalTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer, bool bodyHit, bool leftHand)
	{
		base.LocalTag(taggedPlayer, taggingPlayer, bodyHit, leftHand);
		if (!bodyHit && VRRigCache.Instance.TryGetVrrig(taggedPlayer, out var playerRig) && CheckSlap(taggingPlayer, taggedPlayer, leftHand, out var velocity))
		{
			GorillaGameModes.GameMode.ActiveNetworkHandler.SendRPC("GuardianLaunchPlayer", taggedPlayer, velocity);
			playerRig.Rig.ApplyLocalTrajectoryOverride(velocity);
			GorillaGameModes.GameMode.ActiveNetworkHandler.SendRPC("ShowSlapEffects", true, playerRig.Rig.transform.position, velocity.normalized);
			LocalPlaySlapEffect(playerRig.Rig.transform.position, velocity.normalized);
		}
	}

	private bool CheckSlap(NetPlayer slapper, NetPlayer target, bool leftHand, out Vector3 velocity)
	{
		velocity = Vector3.zero;
		if (IsHoldingPlayer(leftHand))
		{
			return false;
		}
		if (!VRRigCache.Instance.TryGetVrrig(slapper, out var playerRig))
		{
			return false;
		}
		Vector3 averageVelocity = GTPlayer.Instance.GetHandVelocityTracker(leftHand).GetAverageVelocity(worldSpace: true);
		Vector3 rhs = (leftHand ? playerRig.Rig.leftHandHoldsPlayer.transform.right : playerRig.Rig.rightHandHoldsPlayer.transform.right);
		if (Vector3.Dot(averageVelocity.normalized, rhs) < slapFrontAlignmentThreshold && Vector3.Dot(averageVelocity.normalized, rhs) > slapBackAlignmentThreshold)
		{
			return false;
		}
		if (averageVelocity.magnitude < launchMinimumStrength)
		{
			return false;
		}
		averageVelocity = Vector3.ClampMagnitude(averageVelocity, maxLaunchVelocity);
		if (VRRigCache.Instance.TryGetVrrig(target, out var playerRig2))
		{
			if (IsRigBeingHeld(playerRig2.Rig) || playerRig2.Rig.IsLocalTrajectoryOverrideActive())
			{
				return false;
			}
			if (!CheckLaunchRetriggerDelay(playerRig2.Rig))
			{
				return false;
			}
			averageVelocity *= launchStrengthMultiplier;
			if (playerRig2.Rig.IsOnGround(launchGroundHeadCheckDist, launchGroundHandCheckDist, out var groundNormal))
			{
				averageVelocity += groundNormal * launchGroundKickup * Mathf.Clamp01(1f - Vector3.Dot(groundNormal, averageVelocity.normalized));
			}
			velocity = averageVelocity;
			return true;
		}
		return false;
	}

	public override void HandleHandTap(NetPlayer tappingPlayer, Tappable hitTappable, bool leftHand, Vector3 handVelocity, Vector3 tapSurfaceNormal)
	{
		base.HandleHandTap(tappingPlayer, hitTappable, leftHand, handVelocity, tapSurfaceNormal);
		if (hitTappable != null && hitTappable is TappableGuardianIdol { isActivationReady: not false } tappableGuardianIdol)
		{
			tappableGuardianIdol.isActivationReady = false;
			GorillaTagger.Instance.StartVibration(leftHand, GorillaTagger.Instance.tapHapticStrength * hapticStrength, GorillaTagger.Instance.tapHapticDuration * hapticDuration);
		}
		if (!IsPlayerGuardian(tappingPlayer) || IsHoldingPlayer(leftHand))
		{
			return;
		}
		float num = Vector3.Dot(Vector3.down, handVelocity);
		if (num < slamTriggerTapSpeed || Vector3.Dot(Vector3.down, handVelocity.normalized) < slamTriggerAngle || !VRRigCache.Instance.TryGetVrrig(tappingPlayer, out var playerRig))
		{
			return;
		}
		VRMap vRMap = (leftHand ? playerRig.Rig.leftHand : playerRig.Rig.rightHand);
		Vector3 vector = vRMap.rigTarget.rotation * vRMap.trackingPositionOffset * playerRig.Rig.scaleFactor;
		Vector3 vector2 = vRMap.rigTarget.position - vector;
		float t = Mathf.Clamp01((num - slamTriggerTapSpeed) / (slamMaxTapSpeed - slamTriggerTapSpeed));
		t = Mathf.Lerp(slamMinStrengthMultiplier, slamMaxStrengthMultiplier, t);
		for (int i = 0; i < RoomSystem.PlayersInRoom.Count; i++)
		{
			if (RoomSystem.PlayersInRoom[i] == tappingPlayer || !VRRigCache.Instance.TryGetVrrig(RoomSystem.PlayersInRoom[i], out var playerRig2))
			{
				continue;
			}
			VRRig rig = playerRig2.Rig;
			if (!IsRigBeingHeld(rig) && CheckLaunchRetriggerDelay(rig))
			{
				Vector3 position = rig.transform.position;
				if (Vector3.SqrMagnitude(position - vector2) < slamRadius * slamRadius)
				{
					Vector3 vector3 = (position - vector2).normalized * t;
					vector3 = Vector3.ClampMagnitude(vector3, maxLaunchVelocity);
					GorillaGameModes.GameMode.ActiveNetworkHandler.SendRPC("GuardianLaunchPlayer", RoomSystem.PlayersInRoom[i], vector3);
				}
			}
		}
		LocalPlaySlamEffect(vector2, Vector3.up);
		GorillaGameModes.GameMode.ActiveNetworkHandler.SendRPC("ShowSlamEffect", true, vector2, Vector3.up);
	}

	private bool CheckLaunchRetriggerDelay(VRRig launchedRig)
	{
		return launchedRig.fxSettings.callSettings[7].CallLimitSettings.CheckCallTime(Time.time);
	}

	private bool IsHoldingPlayer()
	{
		if (!IsHoldingPlayer(leftHand: true))
		{
			return IsHoldingPlayer(leftHand: false);
		}
		return true;
	}

	private bool IsHoldingPlayer(bool leftHand)
	{
		if (leftHand && EquipmentInteractor.instance.leftHandHeldEquipment != null && EquipmentInteractor.instance.leftHandHeldEquipment is HoldableHand)
		{
			return true;
		}
		if (!leftHand && EquipmentInteractor.instance.rightHandHeldEquipment != null && EquipmentInteractor.instance.rightHandHeldEquipment is HoldableHand)
		{
			return true;
		}
		return false;
	}

	private bool IsRigBeingHeld(VRRig rig)
	{
		if (EquipmentInteractor.instance.leftHandHeldEquipment != null && EquipmentInteractor.instance.leftHandHeldEquipment is HoldableHand holdableHand && holdableHand.Rig == rig)
		{
			return true;
		}
		if (EquipmentInteractor.instance.rightHandHeldEquipment != null && EquipmentInteractor.instance.rightHandHeldEquipment is HoldableHand holdableHand2 && holdableHand2.Rig == rig)
		{
			return true;
		}
		return false;
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public override GameModeType GameType()
	{
		return GameModeType.Guardian;
	}

	public override string GameModeName()
	{
		return "GUARDIAN";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_GUARDIAN_ROOM_LABEL", out var result, "(GUARDIAN GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_GUARDIAN_ROOM_LABEL]");
		}
		return result;
	}

	public void PlaySlapEffect(Vector3 location, Vector3 direction)
	{
		LocalPlaySlapEffect(location, direction);
	}

	private void LocalPlaySlapEffect(Vector3 location, Vector3 direction)
	{
		ObjectPools.instance.Instantiate(slapImpactPrefab, location, Quaternion.LookRotation(direction));
	}

	public void PlaySlamEffect(Vector3 location, Vector3 direction)
	{
		LocalPlaySlamEffect(location, direction);
	}

	private void LocalPlaySlamEffect(Vector3 location, Vector3 direction)
	{
		ObjectPools.instance.Instantiate(slamImpactPrefab, location, Quaternion.LookRotation(direction));
	}
}
