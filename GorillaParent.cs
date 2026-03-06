using System;
using UnityEngine;

public class GorillaParent : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	public static volatile GorillaParent instance;

	[OnEnterPlay_Set(false)]
	public static bool hasInstance;

	private int i;

	private static bool replicatedClientReady;

	private static Action onReplicatedClientReady;

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
			hasInstance = true;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
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

	public static void ReplicatedClientReady()
	{
		replicatedClientReady = true;
		onReplicatedClientReady?.Invoke();
	}

	public static void OnReplicatedClientReady(Action action)
	{
		if (replicatedClientReady)
		{
			action();
		}
		else
		{
			onReplicatedClientReady = (Action)Delegate.Combine(onReplicatedClientReady, action);
		}
	}
}
