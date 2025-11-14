using GorillaNetworking;
using UnityEngine;

public class RubberDuckEvents : MonoBehaviour
{
	public int PlayerId;

	public string PlayerIdString;

	public PhotonEvent Activate;

	public PhotonEvent Deactivate;

	public void Init(NetPlayer player)
	{
		string text = player.UserId;
		if (string.IsNullOrEmpty(text))
		{
			bool isLocal = player.IsLocal;
			PlayFabAuthenticator instance = PlayFabAuthenticator.instance;
			text = ((!isLocal || !(instance != null)) ? player.NickName : instance.GetPlayFabPlayerId());
		}
		PlayerIdString = text + "." + base.gameObject.name;
		PlayerId = PlayerIdString.GetStaticHash();
		Dispose();
		Activate = new PhotonEvent(string.Format("{0}.{1}", PlayerId, "Activate"));
		Deactivate = new PhotonEvent(string.Format("{0}.{1}", PlayerId, "Deactivate"));
		Activate.reliable = true;
		Deactivate.reliable = true;
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
		Dispose();
	}

	public void Dispose()
	{
		Activate?.Dispose();
		Activate = null;
		Deactivate?.Dispose();
		Deactivate = null;
	}
}
