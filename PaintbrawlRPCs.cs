using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

internal class PaintbrawlRPCs : RPCNetworkBase
{
	private GameModeSerializer serializer;

	private GorillaPaintbrawlManager paintbrawlManager;

	public override void SetClassTarget(IWrappedSerializable target, GorillaWrappedSerializer netHandler)
	{
		paintbrawlManager = (GorillaPaintbrawlManager)target;
		serializer = (GameModeSerializer)netHandler;
	}

	[PunRPC]
	public void RPC_ReportSlingshotHit(Player taggedPlayer, Vector3 hitLocation, int projectileCount, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RPC_ReportSlingshotHit");
		if (NetworkSystem.Instance.IsMasterClient && taggedPlayer != null)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(taggedPlayer);
			PhotonMessageInfoWrapped info2 = new PhotonMessageInfoWrapped(info);
			paintbrawlManager.ReportSlingshotHit(player, hitLocation, projectileCount, info2);
		}
	}
}
