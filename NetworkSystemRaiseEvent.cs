using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public static class NetworkSystemRaiseEvent
{
	public static readonly NetEventOptions neoOthers = new NetEventOptions
	{
		Reciever = NetEventOptions.RecieverTarget.others
	};

	public static readonly NetEventOptions neoMaster = new NetEventOptions
	{
		Reciever = NetEventOptions.RecieverTarget.master
	};

	public static readonly NetEventOptions neoTarget = new NetEventOptions
	{
		TargetActors = new int[1]
	};

	public static void RaiseEvent(byte code, object data)
	{
		PhotonNetwork.RaiseEvent(code, data, RaiseEventOptions.Default, SendOptions.SendUnreliable);
	}

	public static void RaiseEvent(byte code, object data, NetEventOptions options, bool reliable)
	{
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
		raiseEventOptions.TargetActors = options.TargetActors;
		raiseEventOptions.Receivers = (ReceiverGroup)options.Reciever;
		raiseEventOptions.Flags = options.Flags;
		PhotonNetwork.RaiseEvent(code, data, raiseEventOptions, reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
	}
}
