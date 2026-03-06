using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GorillaExtensions;
using LitJson;
using PlayFab;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaNetworking;

public class PlayFabTitleDataCache : MonoBehaviour
{
	[Serializable]
	public sealed class DataUpdate : UnityEvent<string>
	{
	}

	private class DataRequest
	{
		public string Name { get; set; }

		public Action<string> Callback { get; set; }

		public Action<PlayFabError> ErrorCallback { get; set; }
	}

	private static Action<PlayFabTitleDataCache> k_onnLoaded;

	public DataUpdate OnTitleDataUpdate;

	private const string FileName = "TitleDataCache.json";

	private readonly List<DataRequest> requests = new List<DataRequest>();

	private Dictionary<string, Dictionary<string, string>> localizedTitleData = new Dictionary<string, Dictionary<string, string>>();

	private Dictionary<string, bool> localesUpdated = new Dictionary<string, bool>();

	private bool isFirstLoad = true;

	private Coroutine updateDataCoroutine;

	public static PlayFabTitleDataCache Instance { get; private set; }

	private static string FilePath => Path.Combine(Application.persistentDataPath, "TitleDataCache.json");

	public void GetTitleData(string name, Action<string> callback, Action<PlayFabError> errorCallback, bool ignoreCache = false)
	{
		if (!ignoreCache && !isFirstLoad && localizedTitleData.TryGetValue(LocalisationManager.CurrentLanguage.Identifier.Code, out var value) && value.TryGetValue(name, out var value2))
		{
			callback.SafeInvoke(value2);
			return;
		}
		DataRequest item = new DataRequest
		{
			Name = name,
			Callback = callback,
			ErrorCallback = errorCallback
		};
		requests.Add(item);
		TryUpdateData();
	}

	private void Awake()
	{
		if (Instance != null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		Instance = this;
		k_onnLoaded?.Invoke(this);
		k_onnLoaded = null;
	}

	private void Start()
	{
		UpdateData();
		LocalisationManager.RegisterOnLanguageChanged(TryUpdateData);
	}

	private void OnDestroy()
	{
		LocalisationManager.UnregisterOnLanguageChanged(TryUpdateData);
	}

	private void TryUpdateData()
	{
		if (!isFirstLoad && updateDataCoroutine == null)
		{
			UpdateData();
		}
	}

	public CacheImport LoadDataFromFile()
	{
		try
		{
			if (!File.Exists(FilePath))
			{
				UnityEngine.Debug.LogWarning("[PlayFabTitleDataCache::LoadDataFromFile] Title data file " + FilePath + " does not exist!");
				return null;
			}
			return JsonMapper.ToObject<CacheImport>(File.ReadAllText(FilePath)) ?? new CacheImport();
		}
		catch (Exception arg)
		{
			UnityEngine.Debug.LogError($"[PlayFabTitleDataCache::LoadDataFromFile] Error reading PlayFab title data from file: {arg}");
			return null;
		}
	}

	private static void SaveDataToFile(string filepath, Dictionary<string, Dictionary<string, string>> titleData)
	{
		try
		{
			string contents = JsonMapper.ToJson(new CacheImport
			{
				DeploymentId = MothershipClientApiUnity.DeploymentId,
				TitleData = titleData
			});
			File.WriteAllText(filepath, contents);
		}
		catch (Exception arg)
		{
			UnityEngine.Debug.LogError($"[PlayFabTitleDataCache::SaveDataToFile] Error writing PlayFab title data to file: {arg}");
		}
	}

	public void UpdateData()
	{
		updateDataCoroutine = StartCoroutine(UpdateDataCo());
	}

	private IEnumerator UpdateDataCo()
	{
		try
		{
			CacheImport oldCache = LoadDataFromFile();
			string currentLocale = LocalisationManager.CurrentLanguage.Identifier.Code;
			if (!localizedTitleData.TryGetValue(currentLocale, out var titleData))
			{
				localizedTitleData[currentLocale] = new Dictionary<string, string>();
				titleData = localizedTitleData[currentLocale];
			}
			if (oldCache == null || oldCache.TitleData == null || !oldCache.TitleData.TryGetValue(currentLocale, out var oldLocalizedCache))
			{
				oldLocalizedCache = new Dictionary<string, string>();
			}
			yield return new WaitUntil(() => MothershipClientApiUnity.IsClientLoggedIn());
			bool wipeOldData = oldCache == null || oldCache.DeploymentId != MothershipClientApiUnity.DeploymentId;
			Dictionary<string, string> newTitleData = null;
			string mothershipError = null;
			Stopwatch.StartNew();
			StringVector stringVector = new StringVector();
			if (!isFirstLoad)
			{
				foreach (DataRequest request in requests)
				{
					stringVector.Add(request.Name);
				}
			}
			bool finished = false;
			if (!MothershipClientApiUnity.ListMothershipTitleData(MothershipClientApiUnity.TitleId, MothershipClientApiUnity.EnvironmentId, MothershipClientApiUnity.DeploymentId, stringVector, delegate(ListClientMothershipTitleDataResponse response)
			{
				if (response != null && response.Results != null)
				{
					newTitleData = new Dictionary<string, string>();
					for (int i = 0; i < response.Results.Count; i++)
					{
						MothershipTitleDataShort mothershipTitleDataShort = response.Results[i];
						if (!string.IsNullOrEmpty(mothershipTitleDataShort.key))
						{
							newTitleData[mothershipTitleDataShort.key] = mothershipTitleDataShort.data;
						}
					}
					mothershipError = null;
				}
				else
				{
					mothershipError = "Failed to fetch title data - response or results were null";
					UnityEngine.Debug.LogError("[PlayFabTitleDataCache::UpdateDataCo] " + mothershipError);
				}
				finished = true;
			}, delegate(MothershipError error, int statusCode)
			{
				mothershipError = string.Format("Error fetching title data: {0} (Status: {1})", error?.Message ?? "Unknown error", statusCode);
				UnityEngine.Debug.LogError("[PlayFabTitleDataCache::UpdateDataCo] Mothership API error callback - " + mothershipError);
				finished = true;
			}))
			{
				mothershipError = "Mothership API call was not sent.";
				UnityEngine.Debug.LogError("[PlayFabTitleDataCache::UpdateDataCo] " + mothershipError);
			}
			yield return new WaitUntil(() => finished);
			if (newTitleData == null)
			{
				yield break;
			}
			if (wipeOldData)
			{
				localizedTitleData.Clear();
				localizedTitleData[currentLocale] = new Dictionary<string, string>();
				titleData = localizedTitleData[currentLocale];
			}
			if (!localesUpdated.ContainsKey(currentLocale))
			{
				titleData.Clear();
			}
			foreach (var (text3, text4) in newTitleData)
			{
				string text5 = (titleData[text3] = text4);
				for (int num = requests.Count - 1; num >= 0; num--)
				{
					DataRequest dataRequest = requests[num];
					if (dataRequest.Name == text3)
					{
						dataRequest.Callback?.Invoke(text5);
						requests.RemoveAt(num);
						break;
					}
				}
				if (oldLocalizedCache.TryGetValue(text3, out var value) && value != text5)
				{
					OnTitleDataUpdate?.Invoke(text3);
				}
			}
			localesUpdated[currentLocale] = true;
			SaveDataToFile(FilePath, localizedTitleData);
		}
		finally
		{
			PlayFabTitleDataCache playFabTitleDataCache = this;
			playFabTitleDataCache.ClearRequestWithError();
			playFabTitleDataCache.isFirstLoad = false;
			playFabTitleDataCache.updateDataCoroutine = null;
		}
	}

	private static string MD5(string value)
	{
		MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
		byte[] bytes = Encoding.Default.GetBytes(value);
		byte[] array = mD5CryptoServiceProvider.ComputeHash(bytes);
		StringBuilder stringBuilder = new StringBuilder();
		byte[] array2 = array;
		foreach (byte b in array2)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	private void ClearRequestWithError(PlayFabError e = null)
	{
		if (e == null)
		{
			e = new PlayFabError();
		}
		foreach (DataRequest request in requests)
		{
			request.ErrorCallback.SafeInvoke(e);
		}
		requests.Clear();
	}

	public static void RegisterOnLoad(Action<PlayFabTitleDataCache> callback)
	{
		if (Instance.IsNotNull())
		{
			callback(Instance);
		}
		else
		{
			k_onnLoaded = (Action<PlayFabTitleDataCache>)Delegate.Combine(k_onnLoaded, callback);
		}
	}
}
