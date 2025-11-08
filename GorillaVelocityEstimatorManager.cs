using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

public class GorillaVelocityEstimatorManager : MonoBehaviour
{
	public static GorillaVelocityEstimatorManager instance;

	public static bool hasInstance = false;

	public static readonly List<GorillaVelocityEstimator> estimators = new List<GorillaVelocityEstimator>(1024);

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
		if (GTAppState.isQuitting)
		{
			return;
		}
		for (int i = 0; i < estimators.Count; i++)
		{
			if (estimators[i] != null)
			{
				estimators[i].TriggeredLateUpdate();
			}
		}
	}

	public static void CreateManager()
	{
		if (!GTAppState.isQuitting)
		{
			SetInstance(new GameObject("GorillaVelocityEstimatorManager").AddComponent<GorillaVelocityEstimatorManager>());
		}
	}

	private static void SetInstance(GorillaVelocityEstimatorManager manager)
	{
		if (!GTAppState.isQuitting)
		{
			instance = manager;
			hasInstance = true;
			if (Application.isPlaying)
			{
				Object.DontDestroyOnLoad(manager);
			}
		}
	}

	public static void Register(GorillaVelocityEstimator velEstimator)
	{
		if (!GTAppState.isQuitting)
		{
			if (!hasInstance)
			{
				CreateManager();
			}
			if (!estimators.Contains(velEstimator))
			{
				estimators.Add(velEstimator);
			}
		}
	}

	public static void Unregister(GorillaVelocityEstimator velEstimator)
	{
		if (!GTAppState.isQuitting)
		{
			if (!hasInstance)
			{
				CreateManager();
			}
			if (estimators.Contains(velEstimator))
			{
				estimators.Remove(velEstimator);
			}
		}
	}
}
