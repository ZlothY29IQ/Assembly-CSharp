using System;
using GorillaTagScripts;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class GameModeSelectorJoinSubsButton : MonoBehaviour
{
	public Material DisabledButtonMaterial;

	[SerializeField]
	private GorillaPressableButton subsPublicButton;

	[SerializeField]
	private GameObject disabledObject;

	[SerializeField]
	private TextMeshPro disabledText;

	private void OnEnable()
	{
		SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Combine(SubscriptionManager.OnLocalSubscriptionData, new Action(CheckSubscribed));
		RoomSystem.JoinedRoomEvent += new Action(OnJoinRoom);
		RoomSystem.LeftRoomEvent += new Action(OnLeaveRoom);
		CheckSubscribed();
	}

	private void OnDisable()
	{
		SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnLocalSubscriptionData, new Action(CheckSubscribed));
		RoomSystem.JoinedRoomEvent -= new Action(OnJoinRoom);
		RoomSystem.LeftRoomEvent -= new Action(OnLeaveRoom);
	}

	[ContextMenu("Check Subscribed")]
	private void CheckSubscribed()
	{
		if (SubscriptionManager.IsLocalSubscribed())
		{
			if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.MaxPlayers <= 10)
			{
				ShowButton();
			}
			else
			{
				DisableButtonInPublicRoom();
			}
		}
		else
		{
			DisableButtonSubscribers();
		}
	}

	private void OnJoinRoom()
	{
		if (!RoomSystem.WasRoomPrivate)
		{
			CheckSubscribed();
		}
		else
		{
			DisableButtonPrivate();
		}
	}

	private void OnLeaveRoom()
	{
		CheckSubscribed();
	}

	private void ShowButton()
	{
		subsPublicButton.enabled = true;
		subsPublicButton.SetUnpressedMaterial();
		disabledObject.SetActive(value: false);
	}

	private void DisableButtonSubscribers()
	{
		DisableButton("ONLY FOR\nSUBSCRIBERS");
	}

	private void DisableButtonPrivate()
	{
		DisableButton("IN PRIVATE ROOM");
	}

	private void DisableButtonInPublicRoom()
	{
		DisableButton("ALREADY IN\nPUBLIC ROOM");
	}

	private void DisableButton(string disabled)
	{
		subsPublicButton.enabled = false;
		subsPublicButton.SetRendererMaterial(DisabledButtonMaterial);
		disabledObject.SetActive(value: true);
		disabledText.text = disabled;
	}
}
