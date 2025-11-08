using System.Collections.Generic;
using Fusion;
using GorillaGameModes;
using UnityEngine;

public class CustomObjectProvider : NetworkObjectProviderDefault
{
	public const int GameModeFlag = 1;

	public const int PlayerFlag = 2;

	private static NetworkObjectBaker baker;

	internal List<GameObject> SceneObjects;

	private static NetworkObjectBaker Baker => baker ?? (baker = new NetworkObjectBaker());

	public override NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject instance)
	{
		NetworkObjectAcquireResult num = base.AcquirePrefabInstance(runner, in context, out instance);
		if (num == NetworkObjectAcquireResult.Success)
		{
			IsGameMode(instance);
			return num;
		}
		instance = null;
		return num;
	}

	private void IsGameMode(NetworkObject instance)
	{
		if (instance.gameObject.GetComponent<GameModeSerializer>() != null)
		{
			GorillaGameModes.GameMode.GetGameModeInstance(GorillaGameModes.GameMode.GetGameModeKeyFromRoomProp()).AddFusionDataBehaviour(instance);
			Baker.Bake(instance.gameObject);
		}
	}

	protected override void DestroySceneObject(NetworkRunner runner, NetworkSceneObjectId sceneObjectId, NetworkObject instance)
	{
		if (SceneObjects == null || !SceneObjects.Contains(instance.gameObject))
		{
			base.DestroySceneObject(runner, sceneObjectId, instance);
		}
	}

	protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
	{
		base.DestroyPrefabInstance(runner, prefabId, instance);
	}
}
