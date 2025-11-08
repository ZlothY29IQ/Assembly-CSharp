using System;
using Fusion;
using Photon.Pun;

[NetworkBehaviourWeaved(0)]
public class NetworkComponentCallbacks : NetworkComponent
{
	public Action ReadData;

	public Action WriteData;

	public Action<PhotonStream, PhotonMessageInfo> ReadPunData;

	public Action<PhotonStream, PhotonMessageInfo> WritePunData;

	public override void ReadDataFusion()
	{
		ReadData();
	}

	public override void WriteDataFusion()
	{
		WriteData();
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		ReadPunData(stream, info);
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		WritePunData(stream, info);
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}
