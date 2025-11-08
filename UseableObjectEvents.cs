using System;
using GorillaNetworking;
using UnityEngine;

public class UseableObjectEvents : MonoBehaviour
{
	[NonSerialized]
	private string PlayerIdString;

	[NonSerialized]
	private int PlayerId;

	public PhotonEvent Activate;

	public PhotonEvent Deactivate;

	public void Init(NetPlayer player)
	{
		bool isLocal = player.IsLocal;
		PlayFabAuthenticator instance = PlayFabAuthenticator.instance;
		string text = ((!isLocal || !(instance != null)) ? player.NickName : instance.GetPlayFabPlayerId());
		PlayerIdString = text + "." + base.gameObject.name;
		PlayerId = PlayerIdString.GetStaticHash();
		DisposeEvents();
		Activate = new PhotonEvent(PlayerId + ".Activate");
		Deactivate = new PhotonEvent(PlayerId + ".Deactivate");
		Activate.reliable = false;
		Deactivate.reliable = false;
	}

	private void OnEnable()
	{
		Activate?.Enable();
		Deactivate?.Enable();
	}

	private void OnDisable()
	{
		Activate?.Disable();
		Deactivate?.Disable();
	}

	private void OnDestroy()
	{
		DisposeEvents();
	}

	private void DisposeEvents()
	{
		Activate?.Dispose();
		Activate = null;
		Deactivate?.Dispose();
		Deactivate = null;
	}
}
