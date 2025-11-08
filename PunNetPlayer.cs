using System;
using Photon.Pun;
using Photon.Realtime;

[Serializable]
public class PunNetPlayer : NetPlayer
{
	public Player PlayerRef { get; private set; }

	public override bool IsValid => !PlayerRef.IsInactive;

	public override int ActorNumber => PlayerRef?.ActorNumber ?? (-1);

	public override string UserId => PlayerRef.UserId;

	public override bool IsMasterClient => PlayerRef.IsMasterClient;

	public override bool IsLocal => PlayerRef == PhotonNetwork.LocalPlayer;

	public override bool IsNull => PlayerRef == null;

	public override string NickName => PlayerRef.NickName;

	public override string DefaultName => PlayerRef.DefaultName;

	public override bool InRoom => PhotonNetwork.CurrentRoom?.Players.ContainsValue(PlayerRef) ?? false;

	public void InitPlayer(Player playerRef)
	{
		PlayerRef = playerRef;
	}

	public override bool Equals(NetPlayer myPlayer, NetPlayer other)
	{
		if (myPlayer == null || other == null)
		{
			return false;
		}
		return ((PunNetPlayer)myPlayer).PlayerRef.Equals(((PunNetPlayer)other).PlayerRef);
	}

	public override void OnReturned()
	{
		base.OnReturned();
	}

	public override void OnTaken()
	{
		base.OnTaken();
		PlayerRef = null;
	}
}
