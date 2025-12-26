using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public class PlayerLoopPruning : MonoBehaviour
{
	public List<string> removeSubsystemList;

	public List<string> androidSubsystemExtras;

	private bool isAndroid;

	private void Start()
	{
		isAndroid = Application.platform == RuntimePlatform.Android;
		PlayerLoop.SetPlayerLoop(RemoveSystem<PreLateUpdate>(PlayerLoop.GetCurrentPlayerLoop()));
	}

	private PlayerLoopSystem RemoveSystem<T>(in PlayerLoopSystem loopSystem) where T : struct
	{
		PlayerLoopSystem playerLoopSystem = default(PlayerLoopSystem);
		playerLoopSystem.loopConditionFunction = loopSystem.loopConditionFunction;
		playerLoopSystem.type = loopSystem.type;
		playerLoopSystem.updateDelegate = loopSystem.updateDelegate;
		playerLoopSystem.updateFunction = loopSystem.updateFunction;
		PlayerLoopSystem result = playerLoopSystem;
		List<PlayerLoopSystem> list = new List<PlayerLoopSystem>();
		if (loopSystem.subSystemList != null)
		{
			for (int i = 0; i < loopSystem.subSystemList.Length; i++)
			{
				PlayerLoopSystem playerLoopSystem2 = loopSystem.subSystemList[i];
				playerLoopSystem = default(PlayerLoopSystem);
				playerLoopSystem.loopConditionFunction = playerLoopSystem2.loopConditionFunction;
				playerLoopSystem.type = playerLoopSystem2.type;
				playerLoopSystem.updateDelegate = playerLoopSystem2.updateDelegate;
				playerLoopSystem.updateFunction = playerLoopSystem2.updateFunction;
				PlayerLoopSystem item = playerLoopSystem;
				if (playerLoopSystem2.subSystemList != null)
				{
					List<PlayerLoopSystem> list2 = new List<PlayerLoopSystem>();
					for (int j = 0; j < playerLoopSystem2.subSystemList.Length; j++)
					{
						if (!removeSubsystemList.Contains(playerLoopSystem2.subSystemList[j].type.Name) && (!isAndroid || !androidSubsystemExtras.Contains(playerLoopSystem2.subSystemList[j].type.Name)))
						{
							list2.Add(playerLoopSystem2.subSystemList[j]);
						}
					}
					item.subSystemList = list2.ToArray();
				}
				list.Add(item);
			}
		}
		result.subSystemList = list.ToArray();
		return result;
	}
}
