using Photon.Pun;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class ArcadeMachineJoystick : HandHold, ISnapTurnOverride, IRequestableOwnershipGuardCallbacks
{
	private XRNode xrNode;

	private ArcadeMachine machine;

	private RequestableOwnershipGuard guard;

	private GorillaSnapTurn snapTurn;

	private bool snapTurnOverride;

	public bool heldByLocalPlayer { get; private set; }

	public bool IsHeldLeftHanded
	{
		get
		{
			if (heldByLocalPlayer)
			{
				return xrNode == XRNode.LeftHand;
			}
			return false;
		}
	}

	public ArcadeButtons currentButtonState { get; private set; }

	public int player { get; private set; }

	public void Init(ArcadeMachine machine, int player)
	{
		this.machine = machine;
		this.player = player;
		guard = GetComponent<RequestableOwnershipGuard>();
		guard.AddCallbackTarget(this);
	}

	public void BindController(bool leftHand)
	{
		xrNode = (leftHand ? XRNode.LeftHand : XRNode.RightHand);
		heldByLocalPlayer = true;
		if (!leftHand)
		{
			if (!snapTurn)
			{
				snapTurn = GorillaTagger.Instance.GetComponent<GorillaSnapTurn>();
			}
			if (snapTurn != null)
			{
				snapTurnOverride = true;
				snapTurn.SetTurningOverride(this);
			}
		}
		if (PhotonNetwork.IsMasterClient)
		{
			guard.TransferOwnership(PhotonNetwork.LocalPlayer);
		}
		else if (!guard.isMine)
		{
			guard.RequestOwnership(OnOwnershipSuccess, OnOwnershipFail);
		}
		ControllerInputPoller.AddUpdateCallback(OnInputUpdate);
		PlayerGameEvents.MiscEvent("PlayArcadeGame");
	}

	private void OnOwnershipSuccess()
	{
	}

	private void OnOwnershipFail()
	{
		ForceRelease();
	}

	public void UnbindController()
	{
		heldByLocalPlayer = false;
		if (snapTurnOverride)
		{
			snapTurnOverride = false;
			snapTurn.UnsetTurningOverride(this);
		}
		OnInputUpdate();
		ControllerInputPoller.RemoveUpdateCallback(OnInputUpdate);
	}

	private void OnInputUpdate()
	{
		ArcadeButtons arcadeButtons = (ArcadeButtons)0;
		if (heldByLocalPlayer)
		{
			arcadeButtons |= ArcadeButtons.GRAB;
			if (ControllerInputPoller.Primary2DAxis(xrNode).y > 0.5f)
			{
				arcadeButtons |= ArcadeButtons.UP;
			}
			if (ControllerInputPoller.Primary2DAxis(xrNode).y < -0.5f)
			{
				arcadeButtons |= ArcadeButtons.DOWN;
			}
			if (ControllerInputPoller.Primary2DAxis(xrNode).x < -0.5f)
			{
				arcadeButtons |= ArcadeButtons.LEFT;
			}
			if (ControllerInputPoller.Primary2DAxis(xrNode).x > 0.5f)
			{
				arcadeButtons |= ArcadeButtons.RIGHT;
			}
			if (ControllerInputPoller.PrimaryButtonPress(xrNode))
			{
				arcadeButtons |= ArcadeButtons.B0;
			}
			if (ControllerInputPoller.SecondaryButtonPress(xrNode))
			{
				arcadeButtons |= ArcadeButtons.B1;
			}
			if (ControllerInputPoller.TriggerFloat(xrNode) > 0.5f)
			{
				arcadeButtons |= ArcadeButtons.TRIGGER;
			}
		}
		if (arcadeButtons != currentButtonState)
		{
			machine.OnJoystickStateChange(player, arcadeButtons);
		}
		currentButtonState = arcadeButtons;
	}

	public void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == info.photonView.Owner)
		{
			ArcadeButtons arcadeButtons = (ArcadeButtons)(int)stream.ReceiveNext();
			if (arcadeButtons != currentButtonState && machine != null)
			{
				machine.OnJoystickStateChange(player, arcadeButtons);
			}
			currentButtonState = arcadeButtons;
			machine.ReadPlayerDataPUN(player, stream, info);
		}
	}

	public void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext((int)currentButtonState);
		machine.WritePlayerDataPUN(player, stream, info);
	}

	public void ReceiveRemoteState(ArcadeButtons newState)
	{
	}

	public bool TurnOverrideActive()
	{
		return snapTurnOverride;
	}

	public override bool CanBeGrabbed(GorillaGrabber grabber)
	{
		return !machine.IsControllerInUse(player);
	}

	public void ForceRelease()
	{
		heldByLocalPlayer = false;
		currentButtonState = (ArcadeButtons)0;
	}

	public void OnOwnershipTransferred(NetPlayer toPlayer, NetPlayer fromPlayer)
	{
		if (heldByLocalPlayer && (toPlayer == null || !toPlayer.IsLocal))
		{
			ForceRelease();
		}
	}

	public bool OnOwnershipRequest(NetPlayer fromPlayer)
	{
		return !heldByLocalPlayer;
	}

	public bool OnMasterClientAssistedTakeoverRequest(NetPlayer fromPlayer, NetPlayer toPlayer)
	{
		return !heldByLocalPlayer;
	}

	public void OnMyOwnerLeft()
	{
	}

	public void OnMyCreatorLeft()
	{
	}
}
