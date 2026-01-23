using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class GorillaSlicerSimpleManager : MonoBehaviour
{
	public enum UpdateStep
	{
		FixedUpdate,
		Update,
		LateUpdate
	}

	public static GorillaSlicerSimpleManager instance;

	public static bool hasInstance;

	public List<IGorillaSliceableSimple> fixedUpdateSlice;

	public List<IGorillaSliceableSimple> updateSlice;

	public List<IGorillaSliceableSimple> lateUpdateSlice;

	public long ticksPerFrame = 1000L;

	public long ticksThisFrame;

	public int updateIndex = -1;

	public int startingIndex = -1;

	public Stopwatch sW;

	public Dictionary<IGorillaSliceableSimple, long> lastRunTicks = new Dictionary<IGorillaSliceableSimple, long>();

	protected void Awake()
	{
		if (hasInstance && instance != this)
		{
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			SetInstance(this);
		}
	}

	public static void CreateManager()
	{
		GorillaSlicerSimpleManager gorillaSlicerSimpleManager = new GameObject("GorillaSlicerSimpleManager").AddComponent<GorillaSlicerSimpleManager>();
		gorillaSlicerSimpleManager.fixedUpdateSlice = new List<IGorillaSliceableSimple>();
		gorillaSlicerSimpleManager.updateSlice = new List<IGorillaSliceableSimple>();
		gorillaSlicerSimpleManager.lateUpdateSlice = new List<IGorillaSliceableSimple>();
		gorillaSlicerSimpleManager.sW = new Stopwatch();
		SetInstance(gorillaSlicerSimpleManager);
	}

	private static void SetInstance(GorillaSlicerSimpleManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			UnityEngine.Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterSliceable(IGorillaSliceableSimple gSS)
	{
		RegisterSliceable(gSS, UpdateStep.Update);
	}

	public static void RegisterSliceable(IGorillaSliceableSimple gSS, UpdateStep step)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		instance.lastRunTicks.TryAdd(gSS, 0L);
		switch (step)
		{
		case UpdateStep.FixedUpdate:
			if (!instance.fixedUpdateSlice.Contains(gSS))
			{
				instance.fixedUpdateSlice.Add(gSS);
			}
			break;
		case UpdateStep.Update:
			if (!instance.updateSlice.Contains(gSS))
			{
				instance.updateSlice.Add(gSS);
			}
			break;
		case UpdateStep.LateUpdate:
			if (!instance.lateUpdateSlice.Contains(gSS))
			{
				instance.lateUpdateSlice.Add(gSS);
			}
			break;
		}
	}

	public static bool UnregisterSliceable(IGorillaSliceableSimple gSS)
	{
		if (UnregisterSliceable(gSS, UpdateStep.Update))
		{
			return true;
		}
		if (UnregisterSliceable(gSS, UpdateStep.LateUpdate))
		{
			return true;
		}
		return UnregisterSliceable(gSS, UpdateStep.FixedUpdate);
	}

	public static bool UnregisterSliceable(IGorillaSliceableSimple gSS, UpdateStep step)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		switch (step)
		{
		case UpdateStep.FixedUpdate:
			if (instance.fixedUpdateSlice.Contains(gSS))
			{
				instance.fixedUpdateSlice.Remove(gSS);
				return true;
			}
			break;
		case UpdateStep.Update:
			if (instance.updateSlice.Contains(gSS))
			{
				instance.updateSlice.Remove(gSS);
				return true;
			}
			break;
		case UpdateStep.LateUpdate:
			if (instance.lateUpdateSlice.Contains(gSS))
			{
				instance.lateUpdateSlice.Remove(gSS);
				return true;
			}
			break;
		}
		return false;
	}

	public void FixedUpdate()
	{
		startingIndex = updateIndex;
		if (updateIndex < 0 || updateIndex >= fixedUpdateSlice.Count + updateSlice.Count + lateUpdateSlice.Count)
		{
			updateIndex = 0;
		}
		sW.Restart();
		while (ticksThisFrame + sW.ElapsedTicks < ticksPerFrame && updateIndex < fixedUpdateSlice.Count)
		{
			IGorillaSliceableSimple gorillaSliceableSimple = fixedUpdateSlice[updateIndex];
			if (startingIndex != updateIndex && ticksThisFrame + sW.ElapsedTicks + lastRunTicks[gorillaSliceableSimple] >= ticksPerFrame)
			{
				ticksThisFrame = ticksPerFrame;
				break;
			}
			long elapsedTicks = sW.ElapsedTicks;
			if (0 <= updateIndex && updateIndex < fixedUpdateSlice.Count && !(gorillaSliceableSimple is MonoBehaviour { isActiveAndEnabled: false }))
			{
				try
				{
					gorillaSliceableSimple.SliceUpdate();
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}
			lastRunTicks[gorillaSliceableSimple] = sW.ElapsedTicks - elapsedTicks;
			updateIndex++;
		}
		ticksThisFrame += sW.ElapsedTicks;
		sW.Stop();
	}

	public void Update()
	{
		int count = fixedUpdateSlice.Count;
		int count2 = updateSlice.Count;
		int num = count + count2;
		sW.Restart();
		while (ticksThisFrame + sW.ElapsedTicks < ticksPerFrame && count <= updateIndex && updateIndex < num)
		{
			IGorillaSliceableSimple gorillaSliceableSimple = updateSlice[updateIndex - count];
			if (startingIndex != updateIndex && ticksThisFrame + sW.ElapsedTicks + lastRunTicks[gorillaSliceableSimple] >= ticksPerFrame)
			{
				ticksThisFrame = ticksPerFrame;
				break;
			}
			long elapsedTicks = sW.ElapsedTicks;
			if (0 <= updateIndex - count && updateIndex - count < updateSlice.Count && !(gorillaSliceableSimple is MonoBehaviour { isActiveAndEnabled: false }))
			{
				try
				{
					gorillaSliceableSimple.SliceUpdate();
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}
			lastRunTicks[gorillaSliceableSimple] = sW.ElapsedTicks - elapsedTicks;
			updateIndex++;
		}
		ticksThisFrame += sW.ElapsedTicks;
		sW.Stop();
	}

	public void LateUpdate()
	{
		int count = fixedUpdateSlice.Count;
		int count2 = updateSlice.Count;
		int count3 = lateUpdateSlice.Count;
		int num = count + count2;
		int num2 = num + count3;
		sW.Restart();
		while (ticksThisFrame + sW.ElapsedTicks < ticksPerFrame && num <= updateIndex && updateIndex < num2)
		{
			IGorillaSliceableSimple gorillaSliceableSimple = lateUpdateSlice[updateIndex - num];
			if (startingIndex != updateIndex && ticksThisFrame + sW.ElapsedTicks + lastRunTicks[gorillaSliceableSimple] >= ticksPerFrame)
			{
				ticksThisFrame = ticksPerFrame;
				break;
			}
			long elapsedTicks = sW.ElapsedTicks;
			if (0 <= updateIndex - num && updateIndex - num < lateUpdateSlice.Count && !(gorillaSliceableSimple is MonoBehaviour { isActiveAndEnabled: false }))
			{
				try
				{
					gorillaSliceableSimple.SliceUpdate();
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}
			lastRunTicks[gorillaSliceableSimple] = sW.ElapsedTicks - elapsedTicks;
			updateIndex++;
		}
		sW.Stop();
		if (updateIndex >= num2)
		{
			updateIndex = -1;
		}
		ticksThisFrame = 0L;
	}
}
