using Fusion;
using Photon.Pun;

public struct PhotonMessageInfoWrapped
{
	public readonly int senderID;

	public readonly int sentTick;

	public readonly PhotonMessageInfo punInfo;

	public readonly NetPlayer Sender;

	public double SentServerTime => (double)(uint)sentTick / 1000.0;

	public PhotonMessageInfoWrapped(PhotonMessageInfo info)
	{
		senderID = info.Sender?.ActorNumber ?? (-1);
		Sender = NetPlayer.Get(info.Sender);
		sentTick = info.SentServerTimestamp;
		punInfo = info;
	}

	public PhotonMessageInfoWrapped(RpcInfo info)
	{
		senderID = info.Source.PlayerId;
		Sender = NetPlayer.Get(info.Source);
		sentTick = info.Tick.Raw;
		punInfo = default(PhotonMessageInfo);
	}

	public PhotonMessageInfoWrapped(int playerID, int tick)
	{
		senderID = playerID;
		Sender = NetworkSystem.Instance.GetPlayer(senderID);
		sentTick = tick;
		punInfo = default(PhotonMessageInfo);
	}

	public static implicit operator PhotonMessageInfoWrapped(PhotonMessageInfo info)
	{
		return new PhotonMessageInfoWrapped(info);
	}

	public static implicit operator PhotonMessageInfoWrapped(RpcInfo info)
	{
		return new PhotonMessageInfoWrapped(info);
	}
}
