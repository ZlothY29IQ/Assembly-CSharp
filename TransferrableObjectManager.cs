using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1549)]
public class TransferrableObjectManager : MonoBehaviour
{
	public static TransferrableObjectManager instance;

	public static bool hasInstance = false;

	public static readonly List<TransferrableObject> transObs = new List<TransferrableObject>(1024);

	protected void Awake()
	{
		if (hasInstance && instance != this)
		{
			Object.Destroy(this);
		}
		else
		{
			SetInstance(this);
		}
	}

	protected void OnDestroy()
	{
		if (instance == this)
		{
			hasInstance = false;
			instance = null;
		}
	}

	protected void LateUpdate()
	{
		for (int i = 0; i < transObs.Count; i++)
		{
			transObs[i].TriggeredLateUpdate();
		}
	}

	private static void CreateManager()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			SetInstance(new GameObject("TransferrableObjectManager").AddComponent<TransferrableObjectManager>());
		}
	}

	private static void SetInstance(TransferrableObjectManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void Register(TransferrableObject transOb)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!transObs.Contains(transOb))
		{
			transObs.Add(transOb);
		}
	}

	public static void Unregister(TransferrableObject transOb)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (transObs.Contains(transOb))
		{
			transObs.Remove(transOb);
		}
	}
}
