using Fusion;
using GorillaGameModes;
using Photon.Pun;
using UnityEngine;

public sealed class CasualGameMode : GorillaGameManager
{
	public override int MyMatIndex(NetPlayer player)
	{
		return 0;
	}

	public override void OnSerializeRead(object newData)
	{
	}

	public override object OnSerializeWrite()
	{
		return null;
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public override GameModeType GameType()
	{
		return GameModeType.Casual;
	}

	public override void AddFusionDataBehaviour(NetworkObject behaviour)
	{
		behaviour.AddBehaviour<CasualGameModeData>();
	}

	public override string GameModeName()
	{
		return "CASUAL";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_CASUAL_ROOM_LABEL", out var result, "(CASUAL GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_CASUAL_ROOM_LABEL]");
		}
		return result;
	}
}
