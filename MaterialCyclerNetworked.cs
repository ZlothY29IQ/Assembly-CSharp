using System;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class MaterialCyclerNetworked : MonoBehaviour
{
	[SerializeField]
	private float syncTimeOut = 1f;

	private PhotonView photonView;

	[SerializeField]
	private bool masterClientOnly;

	public float SyncTimeOut => syncTimeOut;

	public event Action<int, int3> OnSynchronize;

	private void Awake()
	{
		photonView = GetComponent<PhotonView>();
	}

	public void Synchronize(int materialIndex, Color c)
	{
		if (!masterClientOnly || PhotonNetwork.IsMasterClient)
		{
			int num = Mathf.CeilToInt(c.r * 9f);
			int num2 = Mathf.CeilToInt(c.g * 9f);
			int num3 = Mathf.CeilToInt(c.b * 9f);
			int num4 = num | (num2 << 8) | (num3 << 16);
			photonView.RPC("RPC_SynchronizePacked", RpcTarget.Others, materialIndex, num4);
		}
	}

	[PunRPC]
	public void RPC_SynchronizePacked(int index, int colourPacked, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RPC_SynchronizePacked");
		if (this.OnSynchronize != null && (!masterClientOnly || info.Sender.IsMasterClient) && VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig) && playerRig.Rig.IsPositionInRange(base.transform.position, 5f) && FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 21, info.SentServerTime))
		{
			int value = colourPacked & 0xFF;
			int value2 = (colourPacked >> 8) & 0xFF;
			int value3 = (colourPacked >> 16) & 0xFF;
			value = Mathf.Clamp(value, 0, 9);
			value2 = Mathf.Clamp(value2, 0, 9);
			value3 = Mathf.Clamp(value3, 0, 9);
			this.OnSynchronize(index, new int3(value, value2, value3));
		}
	}
}
