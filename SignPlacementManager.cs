using System;
using System.Collections.Generic;
using GorillaNetworking;
using PlayFab;
using UnityEngine;

public class SignPlacementManager : MonoBehaviour
{
	[Serializable]
	public struct SignData
	{
		public Vector3 position;

		public Quaternion rotation;

		[TextArea(1, 10)]
		public string text;

		public bool IsValid => !string.IsNullOrEmpty(text);
	}

	[Serializable]
	public struct SignDataList
	{
		public List<SignData> signs;
	}

	[SerializeField]
	private RuntimePlaceableSign signPrefab;

	[SerializeField]
	private string titleDataKey = "SignPlacement";

	private SignDataList signData;

	private string json;

	private void Start()
	{
		PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, OnTitleDataRequestComplete, OnTitleDataError);
	}

	private void OnTitleDataRequestComplete(string titleDataResult)
	{
		json = titleDataResult;
		if (TryParseSignData(titleDataResult, out var parsed))
		{
			signData = parsed;
			SpawnSigns(signData.signs);
		}
	}

	public bool TryParseSignData(string source, out SignDataList parsed)
	{
		parsed = default(SignDataList);
		if (string.IsNullOrWhiteSpace(source))
		{
			return false;
		}
		try
		{
			parsed = JsonUtility.FromJson<SignDataList>(source);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception, this);
			return false;
		}
		if (parsed.signs == null)
		{
			return false;
		}
		for (int num = parsed.signs.Count - 1; num >= 0; num--)
		{
			if (!parsed.signs[num].IsValid)
			{
				parsed.signs.RemoveAt(num);
			}
		}
		if (parsed.signs.Count == 0)
		{
			return false;
		}
		return true;
	}

	private void OnTitleDataError(PlayFabError error)
	{
		GTDev.LogError("Error retrieving TitleData key " + titleDataKey + ": " + error.GenerateErrorReport());
	}

	public void SpawnSigns(List<SignData> signs)
	{
		RuntimePlaceableSign[] array = UnityEngine.Object.FindObjectsByType<RuntimePlaceableSign>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.Destroy();
		}
		if (signs == null)
		{
			return;
		}
		foreach (SignData sign in signs)
		{
			SpawnSign(sign);
		}
	}

	public RuntimePlaceableSign SpawnSign(SignData data)
	{
		RuntimePlaceableSign runtimePlaceableSign = signPrefab.Instantiate();
		runtimePlaceableSign.Init(data);
		return runtimePlaceableSign;
	}
}
