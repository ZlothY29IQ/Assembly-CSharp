namespace GorillaNetworking;

public class GorillaNetworkRankedJoinTrigger : GorillaNetworkJoinTrigger
{
	public override string GetFullDesiredGameModeString()
	{
		return networkZone + GetDesiredGameType();
	}

	public override void OnBoxTriggered()
	{
		GorillaComputer.instance.allowedMapsToJoin = myCollider.myAllowedMapsToJoin;
		PhotonNetworkController.Instance.ClearDeferredJoin();
		PhotonNetworkController.Instance.AttemptToJoinRankedPublicRoom(this);
	}
}
