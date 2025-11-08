using System;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class HandLink : HoldableObject, IGorillaSliceableSimple
{
	[FormerlySerializedAs("myPlayer")]
	[SerializeField]
	public VRRig myRig;

	[FormerlySerializedAs("leftHand")]
	[SerializeField]
	private bool isLeftHand;

	[SerializeField]
	public GorillaIK myIK;

	private HandLink myOtherHandLink;

	private bool canBeGrabbed;

	public bool isGroundedHand;

	public bool isGroundedButt;

	private bool wasGripPressed;

	private float gripPressedAtTimestamp;

	private float rejectGrabsUntilTimestamp;

	public HandLink grabbedLink;

	public NetPlayer grabbedPlayer;

	public bool grabbedHandIsLeft;

	private const bool DEBUG_GRAB_ANYONE = false;

	[SerializeField]
	private float hapticStrengthOnGrab;

	[SerializeField]
	private float hapticDurationOnGrab;

	[SerializeField]
	private float hapticStrengthOnVicariousTap;

	[SerializeField]
	private float hapticDurationOnVicariousTap;

	[SerializeField]
	private AudioClip audioOnGrab;

	public InteractionPoint interactionPoint;

	public static Action OnHandLinkChanged;

	private int lastReadGrabbedPlayerActorNumber;

	private int snapPositionCalculatedAtFrame = -1;

	public bool IsLocal { get; private set; }

	private void Start()
	{
		myOtherHandLink = (isLeftHand ? myRig.rightHandLink : myRig.leftHandLink);
		if (myRig.isOfflineVRRig)
		{
			base.gameObject.SetActive(value: false);
			IsLocal = true;
		}
		if (interactionPoint == null)
		{
			interactionPoint = GetComponent<InteractionPoint>();
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		interactionPoint.enabled = canBeGrabbed && (myRig.transform.position - VRRig.LocalRig.transform.position).sqrMagnitude < 9f;
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!CanBeGrabbed())
		{
			return;
		}
		if (GameMode.ActiveGameMode is GorillaGuardianManager gorillaGuardianManager && gorillaGuardianManager.IsPlayerGuardian(NetworkSystem.Instance.LocalPlayer))
		{
			(isLeftHand ? myRig.leftHolds : myRig.rightHolds).OnGrab(pointGrabbed, grabbingHand);
			return;
		}
		HandLink handLink = ((grabbingHand == EquipmentInteractor.instance.leftHand) ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
		if (handLink.canBeGrabbed && Time.time - handLink.gripPressedAtTimestamp < 0.1f)
		{
			handLink.CreateLink(this);
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (!myRig.isOfflineVRRig)
		{
			HandLink handLink = ((releasingHand == EquipmentInteractor.instance.leftHand) ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
			bool flag = false;
			HandLinkAuthorityStatus selfHandLinkAuthority = GTPlayer.Instance.GetSelfHandLinkAuthority();
			int stepsToAuth;
			HandLinkAuthorityStatus chainAuthority = handLink.GetChainAuthority(out stepsToAuth);
			if (selfHandLinkAuthority.type >= HandLinkAuthorityType.ButtGrounded && chainAuthority.type < selfHandLinkAuthority.type)
			{
				flag = true;
			}
			else if (handLink.myOtherHandLink.grabbedLink != null)
			{
				int stepsToAuth2;
				HandLinkAuthorityStatus chainAuthority2 = handLink.myOtherHandLink.GetChainAuthority(out stepsToAuth2);
				if (chainAuthority2.type >= HandLinkAuthorityType.ButtGrounded && chainAuthority.type < chainAuthority2.type)
				{
					flag = true;
				}
			}
			if (flag)
			{
				Vector3 averageVelocity = GTPlayer.Instance.GetHandVelocityTracker(handLink.isLeftHand).GetAverageVelocity(worldSpace: true);
				myRig.netView.SendRPC("DroppedByPlayer", myRig.OwningNetPlayer, averageVelocity);
				myRig.ApplyLocalTrajectoryOverride(averageVelocity);
			}
			handLink.BreakLink();
		}
		return true;
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void DropItemCleanup()
	{
		if (grabbedLink != null)
		{
			grabbedLink.BreakLink();
		}
	}

	public bool CanBeGrabbed()
	{
		if (GorillaComputer.instance.IsPlayerInVirtualStump() && CustomMapManager.WantsHoldingHandsDisabled())
		{
			return false;
		}
		if (Time.time < rejectGrabsUntilTimestamp)
		{
			return false;
		}
		if (canBeGrabbed)
		{
			return grabbedPlayer == null;
		}
		return false;
	}

	public bool IsLinkActive()
	{
		return grabbedLink != null;
	}

	private void CreateLink(HandLink remoteLink)
	{
		if (grabbedPlayer == null && myRig.isLocal)
		{
			GRPlayer gRPlayer = GRPlayer.Get(remoteLink.myRig);
			GRPlayer gRPlayer2 = GRPlayer.Get(NetworkSystem.Instance.LocalPlayer);
			if (!(gRPlayer2 != null) || !(gRPlayer != null) || gRPlayer2.State == GRPlayer.GRPlayerState.Ghost == (gRPlayer.State == GRPlayer.GRPlayerState.Ghost))
			{
				EquipmentInteractor.instance.UpdateHandEquipment(remoteLink, isLeftHand);
				grabbedLink = remoteLink;
				grabbedPlayer = remoteLink.myRig.OwningNetPlayer;
				grabbedHandIsLeft = remoteLink.isLeftHand;
				GorillaTagger.Instance.StartVibration(isLeftHand, hapticStrengthOnGrab, hapticDurationOnGrab);
				(isLeftHand ? VRRig.LocalRig.leftHandPlayer : VRRig.LocalRig.rightHandPlayer).GTPlayOneShot(audioOnGrab);
				OnHandLinkChanged?.Invoke();
			}
		}
	}

	public void BreakLinkTo(HandLink targetLink)
	{
		if (grabbedLink == targetLink)
		{
			BreakLink();
		}
	}

	public void BreakLink()
	{
		if (grabbedPlayer != null && !(grabbedLink == null))
		{
			Vector3 velocity = myRig.LatestVelocity();
			GTPlayer.Instance.SetVelocity(velocity);
			grabbedLink = null;
			grabbedPlayer = null;
			grabbedHandIsLeft = false;
			EquipmentInteractor.instance.UpdateHandEquipment(null, isLeftHand);
			OnHandLinkChanged?.Invoke();
		}
	}

	public static bool IsHandInChainWithOtherPlayer(HandLink startingLink, int targetPlayer)
	{
		HandLink handLink = startingLink;
		int num = 0;
		int roomPlayerCount = NetworkSystem.Instance.RoomPlayerCount;
		while (handLink != null && num < roomPlayerCount)
		{
			if (handLink.myRig == null || handLink.myRig.creator == null)
			{
				return false;
			}
			if (handLink.myRig.creator.ActorNumber == targetPlayer)
			{
				return true;
			}
			HandLink handLink2 = null;
			RigContainer playerRig;
			if (handLink.grabbedLink != null && handLink.grabbedLink.myOtherHandLink != null)
			{
				handLink2 = handLink.grabbedLink.myOtherHandLink;
			}
			else if (handLink.grabbedPlayer != null && VRRigCache.Instance.TryGetVrrig(handLink.grabbedPlayer, out playerRig))
			{
				HandLink handLink3 = (handLink.grabbedHandIsLeft ? playerRig.Rig.leftHandLink : playerRig.Rig.rightHandLink);
				if (handLink3 != null && handLink3.myOtherHandLink != null)
				{
					handLink2 = handLink3.myOtherHandLink;
				}
			}
			handLink = handLink2;
			num++;
		}
		return false;
	}

	public void LocalUpdate(bool isGroundedHand, bool isGroundedButt, bool isGripPressed, bool canBeGrabbed)
	{
		if (isGripPressed && !wasGripPressed)
		{
			gripPressedAtTimestamp = Time.time;
		}
		wasGripPressed = isGripPressed;
		this.canBeGrabbed = canBeGrabbed;
		this.isGroundedHand = isGroundedHand;
		this.isGroundedButt = isGroundedButt;
		if (!(grabbedLink != null))
		{
			return;
		}
		if (!grabbedLink.canBeGrabbed && grabbedLink.grabbedPlayer != NetworkSystem.Instance.LocalPlayer)
		{
			BreakLink();
			return;
		}
		if (!isGripPressed || !grabbedLink.myRig.gameObject.activeSelf)
		{
			BreakLink();
			return;
		}
		if (GameMode.ActiveGameMode is GorillaGuardianManager gorillaGuardianManager && gorillaGuardianManager.IsPlayerGuardian(grabbedPlayer))
		{
			BreakLink();
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(grabbedLink.myRig);
		GRPlayer gRPlayer2 = GRPlayer.Get(NetworkSystem.Instance.LocalPlayer);
		if (gRPlayer2 != null && gRPlayer != null && gRPlayer2.State == GRPlayer.GRPlayerState.Ghost != (gRPlayer.State == GRPlayer.GRPlayerState.Ghost))
		{
			BreakLink();
		}
		else if (GorillaComputer.instance.IsPlayerInVirtualStump() && CustomMapManager.WantsHoldingHandsDisabled())
		{
			BreakLink();
		}
	}

	public void RejectGrabsFor(float duration)
	{
		rejectGrabsUntilTimestamp = Mathf.Max(rejectGrabsUntilTimestamp, Time.time + duration);
	}

	public void Write(out bool isGroundedHand, out bool isGroundedButt, out int grabbedPlayerActorNumber, out bool grabbedHandIsLeft)
	{
		isGroundedHand = this.isGroundedHand;
		isGroundedButt = this.isGroundedButt;
		if (grabbedPlayer != null)
		{
			grabbedPlayerActorNumber = grabbedPlayer.ActorNumber;
			grabbedHandIsLeft = this.grabbedHandIsLeft;
		}
		else
		{
			grabbedPlayerActorNumber = 0;
			grabbedHandIsLeft = false;
		}
	}

	public void Read(Vector3 remoteHandLocalPos, Quaternion remoteBodyWorldRot, Vector3 remoteBodyWorldPos, bool isGroundedHand, bool isGroundedButt, bool isGripReady, int grabbedPlayerActorNumber, bool grabbedHandIsLeft)
	{
		this.isGroundedHand = isGroundedHand;
		this.isGroundedButt = isGroundedButt;
		canBeGrabbed = isGripReady;
		if (grabbedPlayerActorNumber == 0)
		{
			if (grabbedPlayer != null && grabbedPlayer.IsLocal)
			{
				(grabbedHandIsLeft ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink).BreakLink();
			}
			bool num = grabbedPlayer != null;
			grabbedPlayer = null;
			grabbedLink = null;
			if (num)
			{
				OnHandLinkChanged?.Invoke();
			}
		}
		else if (lastReadGrabbedPlayerActorNumber == grabbedPlayerActorNumber)
		{
			if (grabbedPlayer != null && grabbedPlayer.IsValid && grabbedPlayer.ActorNumber == grabbedPlayerActorNumber && grabbedPlayer.IsLocal && !IsLocalGrabInRange(grabbedHandIsLeft, remoteHandLocalPos, remoteBodyWorldRot, remoteBodyWorldPos, 7f))
			{
				if (this.grabbedHandIsLeft)
				{
					VRRig.LocalRig.leftHandLink.BreakLink();
				}
				else
				{
					VRRig.LocalRig.rightHandLink.BreakLink();
				}
			}
		}
		else
		{
			if (grabbedPlayer != null && grabbedPlayer.IsLocal)
			{
				VRRig.LocalRig.leftHandLink.BreakLinkTo(this);
				VRRig.LocalRig.rightHandLink.BreakLinkTo(this);
			}
			NetPlayer player = NetworkSystem.Instance.GetPlayer(grabbedPlayerActorNumber);
			if (player != null)
			{
				if (player.IsLocal && !IsLocalGrabInRange(grabbedHandIsLeft, remoteHandLocalPos, remoteBodyWorldRot, remoteBodyWorldPos, 0.25f))
				{
					bool num2 = grabbedPlayer != null;
					grabbedPlayer = null;
					grabbedLink = null;
					if (num2)
					{
						OnHandLinkChanged?.Invoke();
					}
				}
				else if (player == myRig.OwningNetPlayer)
				{
					bool num3 = grabbedPlayer != null;
					grabbedPlayer = null;
					grabbedLink = null;
					if (num3)
					{
						OnHandLinkChanged?.Invoke();
					}
				}
				else
				{
					grabbedPlayer = player;
					this.grabbedHandIsLeft = grabbedHandIsLeft;
					CheckFormLinkWithRemoteGrab();
					OnHandLinkChanged?.Invoke();
				}
			}
			else
			{
				bool num4 = grabbedPlayer != null;
				grabbedPlayer = null;
				grabbedLink = null;
				if (num4)
				{
					OnHandLinkChanged?.Invoke();
				}
			}
		}
		lastReadGrabbedPlayerActorNumber = grabbedPlayerActorNumber;
	}

	private bool IsLocalGrabInRange(bool grabbedLeftHand, Vector3 handLocalPos, Quaternion bodyWorldRot, Vector3 bodyWorldPos, float tolerance)
	{
		return ((grabbedLeftHand ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink).transform.position - (bodyWorldPos + bodyWorldRot * handLocalPos)).IsShorterThan(tolerance);
	}

	private void CheckFormLinkWithRemoteGrab()
	{
		RigContainer playerRig;
		if (grabbedPlayer == NetworkSystem.Instance.LocalPlayer)
		{
			HandLink handLink = (grabbedHandIsLeft ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
			if (handLink.canBeGrabbed && Time.time > handLink.rejectGrabsUntilTimestamp)
			{
				handLink.CreateLink(this);
			}
		}
		else if (VRRigCache.Instance.TryGetVrrig(grabbedPlayer, out playerRig))
		{
			HandLink handLink2 = (grabbedHandIsLeft ? playerRig.Rig.leftHandLink : playerRig.Rig.rightHandLink);
			if (handLink2.grabbedPlayer == myRig.creator)
			{
				grabbedLink = handLink2;
				grabbedLink.grabbedLink = this;
			}
		}
	}

	public HandLinkAuthorityStatus GetChainAuthority(out int stepsToAuth)
	{
		HandLink handLink = grabbedLink;
		int num = 1;
		HandLinkAuthorityStatus handLinkAuthorityStatus = new HandLinkAuthorityStatus(HandLinkAuthorityType.None, -1f, -1);
		stepsToAuth = -1;
		while (handLink != null && num < 10 && !handLink.IsLocal)
		{
			if (handLink.isGroundedHand)
			{
				stepsToAuth = num;
				return new HandLinkAuthorityStatus(HandLinkAuthorityType.HandGrounded, -1f, -1);
			}
			if (handLinkAuthorityStatus.type < HandLinkAuthorityType.ResidualHandGrounded && (double)(handLink.myRig.LastHandTouchedGroundAtNetworkTime + 1f) > PhotonNetwork.Time)
			{
				stepsToAuth = num;
				handLinkAuthorityStatus = new HandLinkAuthorityStatus(HandLinkAuthorityType.ResidualHandGrounded, handLink.myRig.LastHandTouchedGroundAtNetworkTime, handLink.myRig.OwningNetPlayer.ActorNumber);
			}
			else if (handLinkAuthorityStatus.type < HandLinkAuthorityType.ButtGrounded && handLink.isGroundedButt)
			{
				stepsToAuth = num;
				handLinkAuthorityStatus = new HandLinkAuthorityStatus(HandLinkAuthorityType.ButtGrounded, -1f, -1);
			}
			else if (handLinkAuthorityStatus.type == HandLinkAuthorityType.None)
			{
				HandLinkAuthorityStatus handLinkAuthorityStatus2 = new HandLinkAuthorityStatus(HandLinkAuthorityType.None, handLink.myRig.LastTouchedGroundAtNetworkTime, handLink.myRig.OwningNetPlayer.ActorNumber);
				if (handLinkAuthorityStatus2 > handLinkAuthorityStatus)
				{
					stepsToAuth = num;
					handLinkAuthorityStatus = handLinkAuthorityStatus2;
				}
			}
			num++;
			handLink = handLink.myOtherHandLink.grabbedLink;
		}
		return handLinkAuthorityStatus;
	}

	public void SnapHandsTogether()
	{
		if (!(grabbedLink == null))
		{
			if (grabbedLink.snapPositionCalculatedAtFrame == Time.frameCount)
			{
				snapPositionCalculatedAtFrame = Time.frameCount;
				return;
			}
			Vector3 position = base.transform.position;
			Vector3 position2 = grabbedLink.transform.position;
			Vector3 vector = (position + position2) / 2f;
			Vector3 vector2 = (isLeftHand ? myRig.leftHand.rigTarget : myRig.rightHand.rigTarget).position - position;
			Vector3 vector3 = (grabbedLink.isLeftHand ? grabbedLink.myRig.leftHand.rigTarget : grabbedLink.myRig.rightHand.rigTarget).position - position2;
			Vector3 targetWorldPos = vector + vector2;
			Vector3 targetWorldPos2 = vector + vector3;
			myIK.OverrideTargetPos(isLeftHand, targetWorldPos);
			grabbedLink.myIK.OverrideTargetPos(grabbedLink.isLeftHand, targetWorldPos2);
		}
	}

	public void PlayVicariousTapHaptic()
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, hapticStrengthOnVicariousTap, hapticDurationOnVicariousTap);
	}
}
