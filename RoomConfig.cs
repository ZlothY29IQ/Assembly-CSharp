using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Fusion;
using Photon.Realtime;

public class RoomConfig
{
	public const string Room_GameModePropKey = "gameMode";

	public const string Room_PlatformPropKey = "platform";

	public bool isPublic;

	public bool isJoinable;

	public byte MaxPlayers;

	public ExitGames.Client.Photon.Hashtable CustomProps = new ExitGames.Client.Photon.Hashtable();

	public bool createIfMissing;

	public string[] joinFriendIDs;

	public bool IsJoiningWithFriends
	{
		get
		{
			if (joinFriendIDs != null)
			{
				return joinFriendIDs.Length != 0;
			}
			return false;
		}
	}

	public void SetFriendIDs(List<string> friendIDs)
	{
		for (int i = 0; i < friendIDs.Count; i++)
		{
			if (friendIDs[i] == NetworkSystem.Instance.GetMyNickName())
			{
				friendIDs.RemoveAt(i);
				i--;
			}
		}
		joinFriendIDs = new string[friendIDs.Count];
		for (int j = 0; j < friendIDs.Count; j++)
		{
			joinFriendIDs[j] = friendIDs[j];
		}
	}

	public void ClearExpectedUsers()
	{
		if (joinFriendIDs != null && joinFriendIDs.Length != 0)
		{
			joinFriendIDs = new string[0];
		}
	}

	public RoomOptions ToPUNOpts()
	{
		return new RoomOptions
		{
			IsVisible = isPublic,
			IsOpen = isJoinable,
			MaxPlayers = MaxPlayers,
			CustomRoomProperties = CustomProps,
			PublishUserId = true,
			CustomRoomPropertiesForLobby = AutoCustomLobbyProps()
		};
	}

	public void SetFusionOpts(NetworkRunner runnerInst)
	{
		runnerInst.SessionInfo.IsVisible = isPublic;
		runnerInst.SessionInfo.IsOpen = isJoinable;
	}

	public static RoomConfig SPConfig()
	{
		return new RoomConfig
		{
			isPublic = false,
			isJoinable = false,
			MaxPlayers = 1
		};
	}

	public static RoomConfig AnyPublicConfig()
	{
		return new RoomConfig
		{
			isPublic = true,
			isJoinable = true,
			createIfMissing = true,
			MaxPlayers = 10
		};
	}

	private string[] AutoCustomLobbyProps()
	{
		string[] array = new string[CustomProps.Count];
		int num = 0;
		foreach (DictionaryEntry customProp in CustomProps)
		{
			array[num] = (string)customProp.Key;
			num++;
		}
		return array;
	}
}
