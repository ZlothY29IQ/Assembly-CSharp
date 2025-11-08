using System;
using Fusion;
using Photon.Pun;
using UnityEngine;

[NetworkBehaviourWeaved(1)]
public class TransformViewTeleportSerializer : NetworkComponent
{
	private bool willTeleport;

	private GorillaNetworkTransform transformView;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private NetworkBool _Data;

	[Networked]
	[NetworkedWeaved(0, 1)]
	public unsafe NetworkBool Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing TransformViewTeleportSerializer.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(NetworkBool*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing TransformViewTeleportSerializer.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(NetworkBool*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Start()
	{
		base.Start();
		transformView = GetComponent<GorillaNetworkTransform>();
	}

	public void SetWillTeleport()
	{
		willTeleport = true;
	}

	public override void WriteDataFusion()
	{
		Data = willTeleport;
		willTeleport = false;
	}

	public override void ReadDataFusion()
	{
		if ((bool)Data)
		{
			transformView.GTAddition_DoTeleport();
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!transformView.RespectOwnership || info.Sender == info.photonView.Owner)
		{
			stream.SendNext(willTeleport);
			willTeleport = false;
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if ((!transformView.RespectOwnership || info.Sender == info.photonView.Owner) && (bool)stream.ReceiveNext())
		{
			transformView.GTAddition_DoTeleport();
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}
}
