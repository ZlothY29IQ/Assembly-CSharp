using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HandHoldBehaviourActivation : Tappable
{
	[SerializeField]
	private UnityEvent ActivationStart;

	[SerializeField]
	private UnityEvent ActivationStop;

	private int grabs;

	private readonly Dictionary<int, byte> m_playerGrabCounts = new Dictionary<int, byte>(20);

	protected override void OnEnable()
	{
		base.OnEnable();
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeftRoom);
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	public override void OnGrabLocal(float tapTime, PhotonMessageInfoWrapped sender)
	{
		byte valueOrDefault = m_playerGrabCounts.GetValueOrDefault<int, byte>(sender.Sender.ActorNumber, 0);
		valueOrDefault++;
		if (valueOrDefault <= 2)
		{
			m_playerGrabCounts[sender.Sender.ActorNumber] = valueOrDefault;
			grabs++;
			if (grabs < 2)
			{
				ActivationStart.Invoke();
			}
		}
	}

	public override void OnReleaseLocal(float tapTime, PhotonMessageInfoWrapped sender)
	{
		if (m_playerGrabCounts.TryGetValue(sender.Sender.ActorNumber, out var value) && value >= 1)
		{
			value--;
			m_playerGrabCounts[sender.Sender.ActorNumber] = value;
			bool num = grabs > 0;
			grabs = Mathf.Max(0, grabs - 1);
			if (num && grabs < 1)
			{
				ActivationStop.Invoke();
			}
		}
	}

	private void OnPlayerLeftRoom(NetPlayer player)
	{
		if (m_playerGrabCounts.TryGetValue(player.ActorNumber, out var value))
		{
			bool num = grabs > 0;
			grabs = Mathf.Max(0, grabs - value);
			m_playerGrabCounts.Remove(player.ActorNumber);
			if (num && grabs < 1)
			{
				ActivationStop.Invoke();
			}
		}
	}

	private void OnLeftRoom()
	{
		byte valueOrDefault = m_playerGrabCounts.GetValueOrDefault<int, byte>(NetworkSystem.Instance.LocalPlayer.ActorNumber, 0);
		if (grabs > 0 && valueOrDefault < 1)
		{
			ActivationStop.Invoke();
		}
		grabs = valueOrDefault;
		m_playerGrabCounts.Clear();
		m_playerGrabCounts[NetworkSystem.Instance.LocalPlayer.ActorNumber] = valueOrDefault;
	}
}
