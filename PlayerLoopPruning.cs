using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public class PlayerLoopPruning : MonoBehaviour
{
	public List<string> removeSubsystemList;

	public List<string> androidSubsystemExtras;

	private bool isAndroid;

	private static Stopwatch sw = new Stopwatch();

	private static float slop = 0.0002f;

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
		playerLoopSystem = default(PlayerLoopSystem);
		playerLoopSystem.type = typeof(PlayerLoopPruning);
		playerLoopSystem.updateDelegate = PhaseSyncDestroyer3000Start;
		PlayerLoopSystem item2 = playerLoopSystem;
		playerLoopSystem = default(PlayerLoopSystem);
		playerLoopSystem.type = typeof(PlayerLoopPruning);
		playerLoopSystem.updateDelegate = PhaseSyncDestroyer3000End;
		PlayerLoopSystem item3 = playerLoopSystem;
		list.Insert(0, item2);
		list.Add(item3);
		result.subSystemList = list.ToArray();
		return result;
	}

	private static void PhaseSyncDestroyer3000Start()
	{
		slop = (float)sw.ElapsedTicks / 10000000f * 0.1f + slop * 0.9f;
		sw.Restart();
	}

	private static void PhaseSyncDestroyer3000End()
	{
		long elapsedTicks = sw.ElapsedTicks;
		long num = (long)((1f / (float)Application.targetFrameRate - slop) * 10000000f);
		long num2 = num - elapsedTicks;
		if (num2 < 0)
		{
			sw.Restart();
			return;
		}
		Thread.Sleep((int)(num2 / 10000));
		while (sw.ElapsedTicks < num)
		{
			Thread.Sleep(0);
		}
		sw.Restart();
	}
}
