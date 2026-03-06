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
		PlayerLoopSystem result = new PlayerLoopSystem
		{
			loopConditionFunction = loopSystem.loopConditionFunction,
			type = loopSystem.type,
			updateDelegate = loopSystem.updateDelegate,
			updateFunction = loopSystem.updateFunction
		};
		List<PlayerLoopSystem> list = new List<PlayerLoopSystem>();
		if (loopSystem.subSystemList != null)
		{
			for (int i = 0; i < loopSystem.subSystemList.Length; i++)
			{
				PlayerLoopSystem playerLoopSystem = loopSystem.subSystemList[i];
				PlayerLoopSystem item = new PlayerLoopSystem
				{
					loopConditionFunction = playerLoopSystem.loopConditionFunction,
					type = playerLoopSystem.type,
					updateDelegate = playerLoopSystem.updateDelegate,
					updateFunction = playerLoopSystem.updateFunction
				};
				if (playerLoopSystem.subSystemList != null)
				{
					List<PlayerLoopSystem> list2 = new List<PlayerLoopSystem>();
					for (int j = 0; j < playerLoopSystem.subSystemList.Length; j++)
					{
						if (!removeSubsystemList.Contains(playerLoopSystem.subSystemList[j].type.Name) && (!isAndroid || !androidSubsystemExtras.Contains(playerLoopSystem.subSystemList[j].type.Name)))
						{
							list2.Add(playerLoopSystem.subSystemList[j]);
						}
					}
					item.subSystemList = list2.ToArray();
				}
				list.Add(item);
			}
		}
		PlayerLoopSystem item2 = new PlayerLoopSystem
		{
			type = typeof(PlayerLoopPruning),
			updateDelegate = PhaseSyncDestroyer3000Start
		};
		PlayerLoopSystem item3 = new PlayerLoopSystem
		{
			type = typeof(PlayerLoopPruning),
			updateDelegate = PhaseSyncDestroyer3000End
		};
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
		num2 -= GorillaSimpleBackgroundWorkerManager.DoWork(num2);
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
