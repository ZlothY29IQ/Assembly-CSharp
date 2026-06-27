using GorillaGameModes;

namespace GorillaNetworking;

public class CustomMapNetworkJoinTrigger : GorillaNetworkJoinTrigger
{
	public override string GetFullDesiredGameModeString()
	{
		return new GameModeString
		{
			zone = networkZone,
			queue = GorillaComputer.instance.currentQueue,
			gameType = GetDesiredGameType(),
			modId = CustomMapLoader.LoadedMapModId.ToString(),
			modFileId = CustomMapLoader.LoadedMapModFileId.ToString()
		}.ToString();
	}

	public override byte GetRoomSize(bool subscribed)
	{
		return CustomMapLoader.GetRoomSizeForCurrentlyLoadedMap();
	}
}
