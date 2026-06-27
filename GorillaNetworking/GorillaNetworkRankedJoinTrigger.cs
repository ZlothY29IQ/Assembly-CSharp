using GorillaGameModes;

namespace GorillaNetworking;

public class GorillaNetworkRankedJoinTrigger : GorillaNetworkJoinTrigger
{
	public override string GetFullDesiredGameModeString()
	{
		return new GameModeString
		{
			zone = networkZone,
			gameType = GetDesiredGameType()
		}.ToString();
	}

	public override void OnBoxTriggered()
	{
		GorillaComputer.instance.allowedMapsToJoin = myCollider.myAllowedMapsToJoin;
		PhotonNetworkController.Instance.ClearDeferredJoin();
		PhotonNetworkController.Instance.AttemptToJoinRankedPublicRoom(this);
	}
}
