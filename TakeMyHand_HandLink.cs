using System;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class TakeMyHand_HandLink : HoldableObject, IGorillaSliceableSimple
{
	[FormerlySerializedAs("myPlayer")]
	[SerializeField]
	public VRRig myRig;

	[FormerlySerializedAs("leftHand")]
	[SerializeField]
	private bool isLeftHand;

	[SerializeField]
	public GorillaIK myIK;

	private TakeMyHand_HandLink myOtherHandLink;

	private bool isReadyForGrabbing;

	public bool isGroundedHand;

	public bool isGroundedButt;

	private bool wasGripPressed;

	private float gripPressedAtTimestamp;

	private float rejectGrabsUntilTimestamp;

	public TakeMyHand_HandLink grabbedLink;

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

	public bool IsTentacleGrab { get; private set; }

	public bool IsLocal { get; private set; }

	public Vector3 TentacleOffset { get; set; }

	public Vector3 LinkPosition => base.transform.position + TentacleOffset;

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
		interactionPoint.enabled = isReadyForGrabbing && (myRig.transform.position - VRRig.LocalRig.transform.position).sqrMagnitude < 9f;
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
		TakeMyHand_HandLink takeMyHand_HandLink = ((grabbingHand == EquipmentInteractor.instance.leftHand) ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
		if (takeMyHand_HandLink.isReadyForGrabbing && Time.time - takeMyHand_HandLink.gripPressedAtTimestamp < 0.1f)
		{
			takeMyHand_HandLink.LocalCreateLink(this);
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
			TakeMyHand_HandLink takeMyHand_HandLink = ((releasingHand == EquipmentInteractor.instance.leftHand) ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
			bool flag = false;
			HandLinkAuthorityStatus handLinkAuthorityStatus = GTPlayer.Instance.TakeMyHand_GetSelfHandLinkAuthority();
			int stepsToAuth;
			HandLinkAuthorityStatus chainAuthority = takeMyHand_HandLink.GetChainAuthority(out stepsToAuth);
			if (handLinkAuthorityStatus.type >= HandLinkAuthorityType.ButtGrounded && chainAuthority.type < handLinkAuthorityStatus.type)
			{
				flag = true;
			}
			else if (takeMyHand_HandLink.myOtherHandLink.grabbedLink != null)
			{
				int stepsToAuth2;
				HandLinkAuthorityStatus chainAuthority2 = takeMyHand_HandLink.myOtherHandLink.GetChainAuthority(out stepsToAuth2);
				if (chainAuthority2.type >= HandLinkAuthorityType.ButtGrounded && chainAuthority.type < chainAuthority2.type)
				{
					flag = true;
				}
			}
			if (flag)
			{
				Vector3 averageVelocity = GTPlayer.Instance.GetHandVelocityTracker(takeMyHand_HandLink.isLeftHand).GetAverageVelocity(worldSpace: true);
				myRig.netView.SendRPC("DroppedByPlayer", myRig.OwningNetPlayer, averageVelocity);
				myRig.ApplyLocalTrajectoryOverride(averageVelocity);
			}
			takeMyHand_HandLink.BreakLink();
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
		if (isReadyForGrabbing)
		{
			return grabbedPlayer == null;
		}
		return false;
	}

	public bool IsLinkActive()
	{
		return grabbedLink != null;
	}

	public bool TentacleTryCreateLink(TakeMyHand_HandLink remoteLink)
	{
		if (!myRig.isLocal || grabbedPlayer != null)
		{
			return false;
		}
		if (GorillaComputer.instance.IsPlayerInVirtualStump() && CustomMapManager.WantsHoldingHandsDisabled())
		{
			return false;
		}
		if (Time.time < rejectGrabsUntilTimestamp)
		{
			return false;
		}
		if (!remoteLink.CanBeGrabbed())
		{
			return false;
		}
		GRPlayer gRPlayer = GRPlayer.Get(remoteLink.myRig);
		GRPlayer gRPlayer2 = GRPlayer.Get(NetworkSystem.Instance.LocalPlayer);
		if (gRPlayer2 != null && gRPlayer != null && gRPlayer2.State == GRPlayer.GRPlayerState.Ghost != (gRPlayer.State == GRPlayer.GRPlayerState.Ghost))
		{
			return false;
		}
		IsTentacleGrab = true;
		grabbedLink = remoteLink;
		grabbedLink.TentacleOffset = Vector3.zero;
		grabbedPlayer = remoteLink.myRig.OwningNetPlayer;
		grabbedHandIsLeft = remoteLink.isLeftHand;
		OnHandLinkChanged?.Invoke();
		return true;
	}

	private void LocalCreateLink(TakeMyHand_HandLink remoteLink)
	{
		if (grabbedPlayer != null || !myRig.isLocal)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(remoteLink.myRig);
		GRPlayer gRPlayer2 = GRPlayer.Get(NetworkSystem.Instance.LocalPlayer);
		if (!(gRPlayer2 != null) || !(gRPlayer != null) || gRPlayer2.State == GRPlayer.GRPlayerState.Ghost == (gRPlayer.State == GRPlayer.GRPlayerState.Ghost))
		{
			EquipmentInteractor.instance.UpdateHandEquipment(remoteLink, isLeftHand);
			grabbedLink = remoteLink;
			grabbedPlayer = remoteLink.myRig.OwningNetPlayer;
			grabbedHandIsLeft = remoteLink.isLeftHand;
			TentacleOffset = Vector3.zero;
			if (remoteLink.IsTentacleGrab)
			{
				remoteLink.TentacleOffset = base.transform.position - remoteLink.transform.position;
			}
			else
			{
				remoteLink.TentacleOffset = Vector3.zero;
			}
			GorillaTagger.Instance.StartVibration(isLeftHand, hapticStrengthOnGrab, hapticDurationOnGrab);
			(isLeftHand ? VRRig.LocalRig.leftHandPlayer : VRRig.LocalRig.rightHandPlayer).GTPlayOneShot(audioOnGrab);
			OnHandLinkChanged?.Invoke();
		}
	}

	public void BreakLinkTo(TakeMyHand_HandLink targetLink)
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
			IsTentacleGrab = false;
			TentacleOffset = Vector3.zero;
			grabbedLink = null;
			grabbedPlayer = null;
			grabbedHandIsLeft = false;
			EquipmentInteractor.instance.UpdateHandEquipment(null, isLeftHand);
			OnHandLinkChanged?.Invoke();
		}
	}

	public static bool IsHandInChainWithOtherPlayer(TakeMyHand_HandLink startingLink, int targetPlayer)
	{
		TakeMyHand_HandLink takeMyHand_HandLink = startingLink;
		int num = 0;
		int roomPlayerCount = NetworkSystem.Instance.RoomPlayerCount;
		while (takeMyHand_HandLink != null && num < roomPlayerCount)
		{
			if (takeMyHand_HandLink.myRig == null || takeMyHand_HandLink.myRig.creator == null)
			{
				return false;
			}
			if (takeMyHand_HandLink.myRig.creator.ActorNumber == targetPlayer)
			{
				return true;
			}
			TakeMyHand_HandLink takeMyHand_HandLink2 = null;
			RigContainer playerRig;
			if (takeMyHand_HandLink.grabbedLink != null && takeMyHand_HandLink.grabbedLink.myOtherHandLink != null)
			{
				takeMyHand_HandLink2 = takeMyHand_HandLink.grabbedLink.myOtherHandLink;
			}
			else if (takeMyHand_HandLink.grabbedPlayer != null && VRRigCache.Instance.TryGetVrrig(takeMyHand_HandLink.grabbedPlayer, out playerRig))
			{
				TakeMyHand_HandLink takeMyHand_HandLink3 = (takeMyHand_HandLink.grabbedHandIsLeft ? playerRig.Rig.leftHandLink : playerRig.Rig.rightHandLink);
				if (takeMyHand_HandLink3 != null && takeMyHand_HandLink3.myOtherHandLink != null)
				{
					takeMyHand_HandLink2 = takeMyHand_HandLink3.myOtherHandLink;
				}
			}
			takeMyHand_HandLink = takeMyHand_HandLink2;
			num++;
		}
		return false;
	}

	public void LocalUpdate(bool isGroundedHand, bool isGroundedButt, bool isGripPressed, bool isReadyForGrabbing)
	{
		if (isGripPressed && !wasGripPressed)
		{
			gripPressedAtTimestamp = Time.time;
		}
		wasGripPressed = isGripPressed;
		this.isReadyForGrabbing = isReadyForGrabbing && Time.time >= rejectGrabsUntilTimestamp;
		this.isGroundedHand = isGroundedHand;
		this.isGroundedButt = isGroundedButt;
		if (!(grabbedLink != null))
		{
			return;
		}
		if (!grabbedLink.isReadyForGrabbing && grabbedLink.grabbedPlayer != NetworkSystem.Instance.LocalPlayer)
		{
			BreakLink();
			return;
		}
		if ((!IsTentacleGrab && !isGripPressed) || !grabbedLink.myRig.gameObject.activeSelf)
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

	public void Read(Vector3 remoteHandLocalPos, Quaternion remoteBodyWorldRot, Vector3 remoteBodyWorldPos, bool isGroundedHand, bool isGroundedButt, bool isReadyForGrabbing, bool isTentacleGrab, int grabbedPlayerActorNumber, bool grabbedHandIsLeft)
	{
		this.isGroundedHand = isGroundedHand;
		this.isGroundedButt = isGroundedButt;
		this.isReadyForGrabbing = isReadyForGrabbing;
		if (grabbedPlayerActorNumber == 0)
		{
			if (grabbedPlayer != null && grabbedPlayer.IsLocal)
			{
				(this.grabbedHandIsLeft ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink).BreakLink();
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
				bool flag = true;
				if (player.IsLocal && !isTentacleGrab && !(grabbedHandIsLeft ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink).IsTentacleGrab)
				{
					flag = IsLocalGrabInRange(grabbedHandIsLeft, remoteHandLocalPos, remoteBodyWorldRot, remoteBodyWorldPos, 0.25f);
				}
				if (!flag)
				{
					(grabbedHandIsLeft ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink).RejectGrabsFor(0.5f);
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
					IsTentacleGrab = isTentacleGrab;
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
			TakeMyHand_HandLink takeMyHand_HandLink = (grabbedHandIsLeft ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink);
			if (takeMyHand_HandLink.isReadyForGrabbing)
			{
				takeMyHand_HandLink.LocalCreateLink(this);
			}
			else
			{
				takeMyHand_HandLink.RejectGrabsFor(0.5f);
			}
		}
		else if (VRRigCache.Instance.TryGetVrrig(grabbedPlayer, out playerRig))
		{
			TakeMyHand_HandLink takeMyHand_HandLink2 = (grabbedHandIsLeft ? playerRig.Rig.leftHandLink : playerRig.Rig.rightHandLink);
			if (takeMyHand_HandLink2.grabbedPlayer == myRig.creator)
			{
				grabbedLink = takeMyHand_HandLink2;
				grabbedLink.grabbedLink = this;
			}
		}
	}

	public HandLinkAuthorityStatus GetChainAuthority(out int stepsToAuth)
	{
		TakeMyHand_HandLink takeMyHand_HandLink = grabbedLink;
		int num = 1;
		HandLinkAuthorityStatus handLinkAuthorityStatus = new HandLinkAuthorityStatus(HandLinkAuthorityType.None, -1f, -1);
		stepsToAuth = -1;
		while (takeMyHand_HandLink != null && num < 10 && !takeMyHand_HandLink.IsLocal)
		{
			if (takeMyHand_HandLink.isGroundedHand)
			{
				stepsToAuth = num;
				return new HandLinkAuthorityStatus(HandLinkAuthorityType.HandGrounded, -1f, -1);
			}
			if (handLinkAuthorityStatus.type < HandLinkAuthorityType.ResidualHandGrounded && (double)(takeMyHand_HandLink.myRig.LastHandTouchedGroundAtNetworkTime + 1f) > PhotonNetwork.Time)
			{
				stepsToAuth = num;
				handLinkAuthorityStatus = new HandLinkAuthorityStatus(HandLinkAuthorityType.ResidualHandGrounded, takeMyHand_HandLink.myRig.LastHandTouchedGroundAtNetworkTime, takeMyHand_HandLink.myRig.OwningNetPlayer.ActorNumber);
			}
			else if (handLinkAuthorityStatus.type < HandLinkAuthorityType.ButtGrounded && takeMyHand_HandLink.isGroundedButt)
			{
				stepsToAuth = num;
				handLinkAuthorityStatus = new HandLinkAuthorityStatus(HandLinkAuthorityType.ButtGrounded, -1f, -1);
			}
			else if (handLinkAuthorityStatus.type == HandLinkAuthorityType.None)
			{
				HandLinkAuthorityStatus handLinkAuthorityStatus2 = new HandLinkAuthorityStatus(HandLinkAuthorityType.None, takeMyHand_HandLink.myRig.LastTouchedGroundAtNetworkTime, takeMyHand_HandLink.myRig.OwningNetPlayer.ActorNumber);
				if (handLinkAuthorityStatus2 > handLinkAuthorityStatus)
				{
					stepsToAuth = num;
					handLinkAuthorityStatus = handLinkAuthorityStatus2;
				}
			}
			num++;
			takeMyHand_HandLink = takeMyHand_HandLink.myOtherHandLink.grabbedLink;
		}
		return handLinkAuthorityStatus;
	}

	public void VisuallySnapHandsTogether()
	{
		if (!(grabbedLink == null) && !IsTentacleGrab && !grabbedLink.IsTentacleGrab)
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
