using Photon.Pun;
using UnityEngine;

public class TappableSystem : GTSystem<Tappable>
{
	[PunRPC]
	public void SendOnTapRPC(int key, float tapStrength, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SendOnTapRPC");
		if (key >= 0 && key < _instances.Count && float.IsFinite(tapStrength))
		{
			tapStrength = Mathf.Clamp(tapStrength, 0f, 1f);
			_instances[key].OnTapLocal(tapStrength, Time.time, new PhotonMessageInfoWrapped(info));
		}
	}
}
