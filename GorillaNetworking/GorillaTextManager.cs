using System.Collections.Generic;
using UnityEngine;

namespace GorillaNetworking;

public class GorillaTextManager : MonoBehaviourPostTick
{
	public static GorillaTextManager instance;

	public List<GorillaText> gorillaTexts = new List<GorillaText>();

	public static void RegisterText(GorillaText text)
	{
		if (instance == null)
		{
			CreateManager();
		}
		if (!instance.gorillaTexts.Contains(text))
		{
			instance.gorillaTexts.Add(text);
		}
	}

	private void Awake()
	{
		if (instance != null)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			instance = this;
		}
	}

	public override void PostTick()
	{
		for (int i = 0; i < gorillaTexts.Count; i++)
		{
			gorillaTexts[i].InvokeIfUpdated();
		}
	}

	public static void CreateManager()
	{
		GorillaTextManager gorillaTextManager = new GameObject("GorillaTextManager").AddComponent<GorillaTextManager>();
		gorillaTextManager.gorillaTexts = new List<GorillaText>();
		instance = gorillaTextManager;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(gorillaTextManager);
		}
	}
}
