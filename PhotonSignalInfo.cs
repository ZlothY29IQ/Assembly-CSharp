using System;
using Photon.Pun;

[Serializable]
public struct PhotonSignalInfo
{
	public readonly int timestamp;

	public readonly NetPlayer sender;

	public double sentServerTime => (double)(uint)timestamp / 1000.0;

	public PhotonSignalInfo(NetPlayer sender, int timestamp)
	{
		this.sender = sender;
		this.timestamp = timestamp;
	}

	public override string ToString()
	{
		return string.Format("[{0}: Sender = '{1}' sentTime = {2}]", "PhotonSignalInfo", sender.ActorNumber, sentServerTime);
	}

	public static implicit operator PhotonMessageInfo(PhotonSignalInfo psi)
	{
		return new PhotonMessageInfo(psi.sender.GetPlayerRef(), psi.timestamp, null);
	}
}
