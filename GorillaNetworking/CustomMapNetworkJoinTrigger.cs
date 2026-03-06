namespace GorillaNetworking;

public class CustomMapNetworkJoinTrigger : GorillaNetworkJoinTrigger
{
	public override string GetFullDesiredGameModeString()
	{
		return networkZone + GorillaComputer.instance.currentQueue + CustomMapLoader.LoadedMapModId.ToString() + "_" + CustomMapLoader.LoadedMapModFileId + GetDesiredGameType();
	}

	public override byte GetRoomSize(bool subscribed)
	{
		return CustomMapLoader.GetRoomSizeForCurrentlyLoadedMap();
	}
}
