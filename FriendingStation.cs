using GorillaLocomotion;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class FriendingStation : MonoBehaviour
{
	[SerializeField]
	private TriggerEventNotifier triggerNotifier;

	[SerializeField]
	private TextMeshProUGUI player1Text;

	[SerializeField]
	private TextMeshProUGUI player2Text;

	[SerializeField]
	private TextMeshProUGUI statusText;

	[SerializeField]
	private GTZone zone;

	[SerializeField]
	private GorillaPressableButton addFriendButton;

	private FriendingManager.FriendStationData displayedData;

	public TextMeshProUGUI Player1Text => player1Text;

	public TextMeshProUGUI Player2Text => player2Text;

	public TextMeshProUGUI StatusText => statusText;

	public GTZone Zone => zone;

	private void Awake()
	{
		triggerNotifier.TriggerEnterEvent += TriggerEntered;
		triggerNotifier.TriggerExitEvent += TriggerExited;
	}

	private void OnEnable()
	{
		FriendingManager.Instance.RegisterFriendingStation(this);
		if (PhotonNetwork.InRoom)
		{
			displayedData.actorNumberA = -1;
			displayedData.actorNumberB = -1;
			displayedData.state = FriendingManager.FriendStationState.WaitingForPlayers;
		}
		else
		{
			displayedData.actorNumberA = -2;
			displayedData.actorNumberB = -2;
			displayedData.state = FriendingManager.FriendStationState.NotInRoom;
		}
		UpdatePlayerText(player1Text, displayedData.actorNumberA);
		UpdatePlayerText(player2Text, displayedData.actorNumberB);
		UpdateDisplayedState(displayedData.state);
	}

	private void OnDisable()
	{
		FriendingManager.Instance.UnregisterFriendingStation(this);
	}

	private void UpdatePlayerText(TextMeshProUGUI playerText, int playerId)
	{
		switch (playerId)
		{
		case -2:
			playerText.text = "";
			return;
		case -1:
			playerText.text = "PLAYER:\nNONE";
			return;
		}
		NetPlayer netPlayerByID = NetworkSystem.Instance.GetNetPlayerByID(playerId);
		if (netPlayerByID != null)
		{
			playerText.text = "PLAYER:\n" + netPlayerByID.SanitizedNickName;
		}
		else
		{
			playerText.text = "PLAYER:\nNONE";
		}
	}

	private void UpdateDisplayedState(FriendingManager.FriendStationState state)
	{
		switch (state)
		{
		case FriendingManager.FriendStationState.NotInRoom:
			statusText.text = "JOIN A ROOM TO USE";
			break;
		case FriendingManager.FriendStationState.WaitingForPlayers:
			statusText.text = "";
			break;
		case FriendingManager.FriendStationState.WaitingOnFriendStatusBoth:
			statusText.text = "LOADING";
			break;
		case FriendingManager.FriendStationState.WaitingOnFriendStatusPlayerA:
			statusText.text = "LOADING";
			break;
		case FriendingManager.FriendStationState.WaitingOnFriendStatusPlayerB:
			statusText.text = "LOADING";
			break;
		case FriendingManager.FriendStationState.WaitingOnButtonBoth:
			statusText.text = "PRESS [       ] PRESS";
			break;
		case FriendingManager.FriendStationState.WaitingOnButtonPlayerA:
			statusText.text = "PRESS [       ] READY";
			break;
		case FriendingManager.FriendStationState.WaitingOnButtonPlayerB:
			statusText.text = "READY [       ] PRESS";
			break;
		case FriendingManager.FriendStationState.ButtonConfirmationTimer0:
			statusText.text = "READY [       ] READY";
			break;
		case FriendingManager.FriendStationState.ButtonConfirmationTimer1:
			statusText.text = "READY [-     -] READY";
			break;
		case FriendingManager.FriendStationState.ButtonConfirmationTimer2:
			statusText.text = "READY [--   --] READY";
			break;
		case FriendingManager.FriendStationState.ButtonConfirmationTimer3:
			statusText.text = "READY [--- ---] READY";
			break;
		case FriendingManager.FriendStationState.ButtonConfirmationTimer4:
			statusText.text = "READY [-------] READY";
			break;
		case FriendingManager.FriendStationState.WaitingOnRequestBoth:
			statusText.text = " SENT [-------] SENT ";
			break;
		case FriendingManager.FriendStationState.WaitingOnRequestPlayerA:
			statusText.text = " SENT [-------] DONE ";
			break;
		case FriendingManager.FriendStationState.WaitingOnRequestPlayerB:
			statusText.text = " DONE [-------] SENT ";
			break;
		case FriendingManager.FriendStationState.RequestFailed:
			statusText.text = "FRIEND REQUEST FAILED";
			break;
		case FriendingManager.FriendStationState.Friends:
			statusText.text = "\\O/ FRIENDS \\O/";
			break;
		case FriendingManager.FriendStationState.AlreadyFriends:
			statusText.text = "ALREADY FRIENDS";
			break;
		}
	}

	private void UpdateAddFriendButton()
	{
		int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		if ((displayedData.state >= FriendingManager.FriendStationState.ButtonConfirmationTimer0 && displayedData.state <= FriendingManager.FriendStationState.ButtonConfirmationTimer4) || (displayedData.actorNumberA == actorNumber && displayedData.state == FriendingManager.FriendStationState.WaitingOnButtonPlayerB) || (displayedData.actorNumberB == actorNumber && displayedData.state == FriendingManager.FriendStationState.WaitingOnButtonPlayerA))
		{
			addFriendButton.isOn = true;
		}
		else
		{
			addFriendButton.isOn = false;
		}
		addFriendButton.UpdateColor();
	}

	private void UpdateDisplay(ref FriendingManager.FriendStationData data)
	{
		if (displayedData.actorNumberA != data.actorNumberA)
		{
			UpdatePlayerText(player1Text, data.actorNumberA);
		}
		if (displayedData.actorNumberB != data.actorNumberB)
		{
			UpdatePlayerText(player2Text, data.actorNumberB);
		}
		if (displayedData.state != data.state)
		{
			UpdateDisplayedState(data.state);
		}
		displayedData = data;
		UpdateAddFriendButton();
	}

	public void UpdateState(FriendingManager.FriendStationData data)
	{
		UpdateDisplay(ref data);
	}

	public void TriggerEntered(TriggerEventNotifier notifier, Collider other)
	{
		if (PhotonNetwork.InRoom)
		{
			VRRig component = other.GetComponent<VRRig>();
			if (component != null && component.OwningNetPlayer != null)
			{
				addFriendButton.ResetState();
				FriendingManager.Instance.PlayerEnteredStation(zone, component.OwningNetPlayer);
			}
		}
		else if (other == GTPlayer.Instance.headCollider)
		{
			displayedData.state = FriendingManager.FriendStationState.NotInRoom;
			displayedData.actorNumberA = -2;
			displayedData.actorNumberB = -2;
			UpdateDisplayedState(displayedData.state);
			UpdatePlayerText(player1Text, displayedData.actorNumberA);
			UpdatePlayerText(player2Text, displayedData.actorNumberB);
			addFriendButton.ResetState();
		}
	}

	public void TriggerExited(TriggerEventNotifier notifier, Collider other)
	{
		if (PhotonNetwork.InRoom)
		{
			VRRig component = other.GetComponent<VRRig>();
			if (component != null)
			{
				addFriendButton.ResetState();
				FriendingManager.Instance.PlayerExitedStation(zone, component.OwningNetPlayer);
			}
		}
		else if (other == GTPlayer.Instance.headCollider)
		{
			displayedData.state = FriendingManager.FriendStationState.NotInRoom;
			displayedData.actorNumberA = -2;
			displayedData.actorNumberB = -2;
			UpdateDisplayedState(displayedData.state);
			UpdatePlayerText(player1Text, displayedData.actorNumberA);
			UpdatePlayerText(player2Text, displayedData.actorNumberB);
			addFriendButton.ResetState();
		}
	}

	public void FriendButtonPressed()
	{
		if (displayedData.state != FriendingManager.FriendStationState.WaitingForPlayers && displayedData.state != FriendingManager.FriendStationState.Friends && !addFriendButton.isOn)
		{
			FriendingManager.Instance.photonView.RPC("FriendButtonPressedRPC", RpcTarget.MasterClient, zone);
			int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
			if (displayedData.state == FriendingManager.FriendStationState.WaitingOnButtonBoth || (displayedData.actorNumberA == actorNumber && displayedData.state == FriendingManager.FriendStationState.WaitingOnButtonPlayerA) || (displayedData.actorNumberB == actorNumber && displayedData.state == FriendingManager.FriendStationState.WaitingOnButtonPlayerB))
			{
				addFriendButton.isOn = true;
				addFriendButton.UpdateColor();
			}
		}
	}

	public void FriendButtonReleased()
	{
		if (displayedData.state != FriendingManager.FriendStationState.WaitingForPlayers && displayedData.state != FriendingManager.FriendStationState.Friends && addFriendButton.isOn)
		{
			FriendingManager.Instance.photonView.RPC("FriendButtonUnpressedRPC", RpcTarget.MasterClient, zone);
			int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
			if ((displayedData.state >= FriendingManager.FriendStationState.ButtonConfirmationTimer0 && displayedData.state <= FriendingManager.FriendStationState.ButtonConfirmationTimer4) || (displayedData.actorNumberA == actorNumber && displayedData.state == FriendingManager.FriendStationState.WaitingOnButtonPlayerB) || (displayedData.actorNumberB == actorNumber && displayedData.state == FriendingManager.FriendStationState.WaitingOnButtonPlayerA))
			{
				addFriendButton.isOn = false;
				addFriendButton.UpdateColor();
			}
		}
	}
}
