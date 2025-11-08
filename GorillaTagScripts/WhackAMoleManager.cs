using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class WhackAMoleManager : MonoBehaviour, IGorillaSliceableSimple
{
	public static WhackAMoleManager instance;

	public HashSet<WhackAMole> allGames = new HashSet<WhackAMole>();

	private void Awake()
	{
		instance = this;
		allGames.Clear();
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		foreach (WhackAMole allGame in allGames)
		{
			allGame.InvokeUpdate();
		}
	}

	private void OnDestroy()
	{
		instance = null;
	}

	public void Register(WhackAMole whackAMole)
	{
		allGames.Add(whackAMole);
	}

	public void Unregister(WhackAMole whackAMole)
	{
		allGames.Remove(whackAMole);
	}
}
