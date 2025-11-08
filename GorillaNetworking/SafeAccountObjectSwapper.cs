using System;
using UnityEngine;

namespace GorillaNetworking;

public class SafeAccountObjectSwapper : MonoBehaviour
{
	public GameObject[] UnSafeGameObjects;

	public GameObject[] UnSafeTexts;

	public GameObject[] SafeTexts;

	public GameObject[] SafeModeObjects;

	public void Start()
	{
		if (PlayFabAuthenticator.instance.GetSafety())
		{
			SwitchToSafeMode();
		}
		PlayFabAuthenticator instance = PlayFabAuthenticator.instance;
		instance.OnSafetyUpdate = (Action<bool>)Delegate.Combine(instance.OnSafetyUpdate, new Action<bool>(SafeAccountUpdated));
	}

	public void SafeAccountUpdated(bool isSafety)
	{
		if (isSafety)
		{
			SwitchToSafeMode();
		}
	}

	public void SwitchToSafeMode()
	{
		GameObject[] unSafeGameObjects = UnSafeGameObjects;
		foreach (GameObject gameObject in unSafeGameObjects)
		{
			if (gameObject != null)
			{
				gameObject.SetActive(value: false);
			}
		}
		unSafeGameObjects = UnSafeTexts;
		foreach (GameObject gameObject2 in unSafeGameObjects)
		{
			if (gameObject2 != null)
			{
				gameObject2.SetActive(value: false);
			}
		}
		unSafeGameObjects = SafeTexts;
		foreach (GameObject gameObject3 in unSafeGameObjects)
		{
			if (gameObject3 != null)
			{
				gameObject3.SetActive(value: true);
			}
		}
		unSafeGameObjects = SafeModeObjects;
		foreach (GameObject gameObject4 in unSafeGameObjects)
		{
			if (gameObject4 != null)
			{
				gameObject4.SetActive(value: true);
			}
		}
	}
}
