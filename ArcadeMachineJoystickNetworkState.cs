using System;
using Fusion;
using Photon.Pun;

[NetworkBehaviourWeaved(0)]
public class ArcadeMachineJoystickNetworkState : NetworkComponent
{
	private ArcadeMachineJoystick joystick;

	private new void Awake()
	{
		joystick = GetComponent<ArcadeMachineJoystick>();
	}

	public override void ReadDataFusion()
	{
		throw new NotImplementedException();
	}

	public override void WriteDataFusion()
	{
		throw new NotImplementedException();
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
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
