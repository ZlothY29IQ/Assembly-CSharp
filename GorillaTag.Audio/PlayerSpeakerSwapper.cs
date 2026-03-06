using System;
using UnityEngine;

namespace GorillaTag.Audio;

public class PlayerSpeakerSwapper : MonoBehaviour
{
	[SerializeField]
	private Behaviour _lowPassFilter;

	private void OnEnable()
	{
		NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(OnPlayerCountChanged);
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerCountChanged);
		OnPlayerCountChanged(null);
	}

	private void OnDisable()
	{
		NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(OnPlayerCountChanged);
		NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerCountChanged);
	}

	private void OnPlayerCountChanged(NetPlayer _)
	{
		int num = NetworkSystem.Instance.AllNetPlayers.Length;
		_lowPassFilter.enabled = num >= 10;
	}
}
