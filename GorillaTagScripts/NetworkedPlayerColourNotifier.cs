using System;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts;

public static class NetworkedPlayerColourNotifier
{
	private static RigContainer m_localRigContainer;

	private static VRRig m_localRig;

	private static Color m_initialNetColour;

	private static bool m_netColourDirty;

	static NetworkedPlayerColourNotifier()
	{
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerJoinedRoom);
		RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
	}

	public static void SetLocalRigReference(RigContainer rig)
	{
		m_localRigContainer = rig;
		m_localRig = rig.Rig;
		m_localRig.OnColorChanged += OnLocalColourChanged;
		m_netColourDirty = false;
	}

	public static void NotifyOthers()
	{
		if (RoomSystem.JoinedRoom && !m_localRigContainer.netView.IsNull())
		{
			Color playerColor = m_localRig.playerColor;
			float r = playerColor.r;
			float g = playerColor.g;
			float b = playerColor.b;
			m_localRigContainer.netView.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.Others, r, g, b);
		}
	}

	private static void OnLocalColourChanged(Color color)
	{
		if (RoomSystem.JoinedRoom)
		{
			m_netColourDirty = m_initialNetColour != color;
		}
	}

	private static void OnPlayerJoinedRoom(NetPlayer player)
	{
		if (m_netColourDirty && m_localRigContainer.netView.IsNotNull())
		{
			Color playerColor = m_localRig.playerColor;
			float r = playerColor.r;
			float g = playerColor.g;
			float b = playerColor.b;
			m_localRigContainer.netView.SendRPC("RPC_InitializeNoobMaterial", player, r, g, b);
		}
	}

	private static void OnJoinedRoom()
	{
		m_initialNetColour = m_localRig.playerColor;
		m_netColourDirty = false;
	}
}
