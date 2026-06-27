using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneManagement : MonoBehaviour
{
	public delegate void ZoneChangeEvent(ZoneData[] zones);

	private const string preLog = "[GT/ZoneManagement]  ";

	private const string preErr = "ERROR!!!  ";

	private const string preErrBeta = "(beta only log)  ";

	public static ZoneManagement instance;

	[SerializeField]
	private ZoneData[] zones;

	private GameObject[] allObjects;

	private bool[] objectActivationState;

	public Action onZoneChanged;

	public Action OnSceneLoadsCompleted;

	public List<GTZone> activeZones = new List<GTZone>(20);

	private HashSet<string> scenesLoaded = new HashSet<string>();

	private HashSet<string> scenesRequested = new HashSet<string>();

	private HashSet<string> sceneForceStayLoaded = new HashSet<string>(8);

	private List<string> scenesToUnload = new List<string>();

	private Dictionary<string, AsyncOperation> _scenes_to_loadOps = new Dictionary<string, AsyncOperation>(32);

	private Dictionary<string, AsyncOperation> _scenes_to_unloadOps = new Dictionary<string, AsyncOperation>(32);

	private Camera mainCamera;

	public bool hasInstance { get; private set; }

	public bool Initialized { get; private set; }

	public static event ZoneChangeEvent OnZoneChange;

	private void Awake()
	{
		if (instance == null)
		{
			Initialize();
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public static void SetActiveZone(GTZone zone)
	{
		SetActiveZones(new GTZone[1] { zone });
	}

	public static void SetActiveZones(GTZone[] zones)
	{
		if (instance == null)
		{
			FindInstance();
		}
		if (zones != null && zones.Length != 0)
		{
			instance.SetZones(zones);
			instance.onZoneChanged?.Invoke();
			if (ZoneManagement.OnZoneChange != null)
			{
				ZoneManagement.OnZoneChange(instance.zones);
			}
		}
	}

	public static bool IsInZone(GTZone zone)
	{
		if (instance == null)
		{
			FindInstance();
		}
		return instance.GetZoneData(zone)?.active ?? false;
	}

	public static bool IsZoneLoaded(GTZone zone)
	{
		if (!instance)
		{
			FindInstance();
		}
		ZoneData zoneData = instance.GetZoneData(zone);
		if (zoneData != null && zoneData.active)
		{
			return SceneManager.GetSceneByName(zoneData.sceneName).isLoaded;
		}
		return false;
	}

	public GameObject GetPrimaryGameObject(GTZone zone)
	{
		return GetZoneData(zone).rootGameObjects[0];
	}

	public static void AddSceneToForceStayLoaded(string sceneName)
	{
		if (instance == null)
		{
			FindInstance();
		}
		instance.sceneForceStayLoaded.Add(sceneName);
	}

	public static void RemoveSceneFromForceStayLoaded(string sceneName)
	{
		if (instance == null)
		{
			FindInstance();
		}
		instance.sceneForceStayLoaded.Remove(sceneName);
	}

	public static void FindInstance()
	{
		ZoneManagement zoneManagement = UnityEngine.Object.FindAnyObjectByType<ZoneManagement>();
		if (zoneManagement == null)
		{
			throw new NullReferenceException("Unable to find ZoneManagement object in scene.");
		}
		Debug.LogWarning("ZoneManagement accessed before MonoBehaviour awake function called; consider delaying zone management functions to avoid FindObject lookup.");
		zoneManagement.Initialize();
	}

	public bool IsSceneLoaded(GTZone gtZone)
	{
		ZoneData[] array = zones;
		foreach (ZoneData zoneData in array)
		{
			if (zoneData.zone == gtZone && scenesLoaded.Contains(zoneData.sceneName))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsZoneActive(GTZone zone)
	{
		return GetZoneData(zone)?.active ?? false;
	}

	public HashSet<string> GetAllLoadedScenes()
	{
		return scenesLoaded;
	}

	public bool IsSceneLoaded(string sceneName)
	{
		return scenesLoaded.Contains(sceneName);
	}

	private void Initialize()
	{
		instance = this;
		hasInstance = true;
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		List<GameObject> list = new List<GameObject>(8);
		for (int i = 0; i < zones.Length; i++)
		{
			list.Clear();
			ZoneData zoneData = zones[i];
			if (zoneData == null || zoneData.rootGameObjects == null)
			{
				continue;
			}
			hashSet.UnionWith(zoneData.rootGameObjects);
			for (int j = 0; j < zoneData.rootGameObjects.Length; j++)
			{
				GameObject gameObject = zoneData.rootGameObjects[j];
				if (!(gameObject == null))
				{
					list.Add(gameObject);
				}
			}
			hashSet.UnionWith(list);
		}
		allObjects = hashSet.ToArray();
		objectActivationState = new bool[allObjects.Length];
		AddSceneToForceStayLoaded("City");
		Initialized = true;
	}

	private void SetZones(GTZone[] newActiveZones)
	{
		for (int i = 0; i < objectActivationState.Length; i++)
		{
			objectActivationState[i] = false;
		}
		activeZones.Clear();
		for (int j = 0; j < newActiveZones.Length; j++)
		{
			activeZones.Add(newActiveZones[j]);
		}
		scenesRequested.Clear();
		scenesRequested.Add("GorillaTag");
		float num = 0f;
		for (int k = 0; k < zones.Length; k++)
		{
			ZoneData zoneData = zones[k];
			if (zoneData == null)
			{
				continue;
			}
			if (zoneData.rootGameObjects == null || !Enumerable.Contains(newActiveZones, zoneData.zone))
			{
				zoneData.active = false;
				continue;
			}
			zoneData.active = true;
			num = Mathf.Max(num, zoneData.CameraFarClipPlane);
			if (!string.IsNullOrEmpty(zoneData.sceneName))
			{
				scenesRequested.Add(zoneData.sceneName);
			}
			GameObject[] rootGameObjects = zoneData.rootGameObjects;
			foreach (GameObject gameObject in rootGameObjects)
			{
				if (gameObject == null)
				{
					continue;
				}
				for (int m = 0; m < allObjects.Length; m++)
				{
					if (gameObject == allObjects[m])
					{
						objectActivationState[m] = true;
						break;
					}
				}
			}
		}
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
		}
		mainCamera.farClipPlane = num;
		int loadedSceneCount = SceneManager.loadedSceneCount;
		for (int n = 0; n < loadedSceneCount; n++)
		{
			scenesLoaded.Add(SceneManager.GetSceneAt(n).name);
		}
		foreach (string item in scenesRequested)
		{
			if (scenesLoaded.Add(item))
			{
				AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(item, LoadSceneMode.Additive);
				_scenes_to_loadOps[item] = asyncOperation;
				asyncOperation.completed += HandleOnSceneLoadCompleted;
			}
		}
		scenesToUnload.Clear();
		foreach (string item2 in scenesLoaded)
		{
			if (!scenesRequested.Contains(item2) && !sceneForceStayLoaded.Contains(item2))
			{
				scenesToUnload.Add(item2);
			}
		}
		foreach (string item3 in scenesToUnload)
		{
			scenesLoaded.Remove(item3);
			AsyncOperation value = SceneManager.UnloadSceneAsync(item3);
			_scenes_to_unloadOps[item3] = value;
		}
		for (int num2 = 0; num2 < objectActivationState.Length; num2++)
		{
			if (!(allObjects[num2] == null))
			{
				allObjects[num2].SetActive(objectActivationState[num2]);
			}
		}
	}

	private void HandleOnSceneLoadCompleted(AsyncOperation thisLoadOp)
	{
		string key;
		AsyncOperation value;
		foreach (KeyValuePair<string, AsyncOperation> scenes_to_loadOp in _scenes_to_loadOps)
		{
			scenes_to_loadOp.Deconstruct(out key, out value);
			string text = key;
			AsyncOperation asyncOperation = value;
			if (asyncOperation == null)
			{
				Debug.LogError("ERROR!!!  HandleOnSceneLoadCompleted: Why is `loadOp` null in `_scenes_to_loadOps` for scene \"" + text + "\"?????");
			}
			else if (!asyncOperation.isDone)
			{
				return;
			}
		}
		foreach (KeyValuePair<string, AsyncOperation> scenes_to_unloadOp in _scenes_to_unloadOps)
		{
			scenes_to_unloadOp.Deconstruct(out key, out value);
			string text2 = key;
			AsyncOperation asyncOperation2 = value;
			if (asyncOperation2 == null)
			{
				Debug.LogError("ERROR!!!  HandleOnSceneLoadCompleted: Why is `unloadOps` null in `_scenes_to_unloadOps` for scene \"" + text2 + "\"?????");
			}
			else if (!asyncOperation2.isDone)
			{
				return;
			}
		}
		OnSceneLoadsCompleted?.Invoke();
	}

	public bool AnyActiveLoadOps()
	{
		return _scenes_to_loadOps.Values.Any((AsyncOperation op) => !op.isDone);
	}

	private ZoneData GetZoneData(GTZone zone)
	{
		for (int i = 0; i < zones.Length; i++)
		{
			if (zones[i].zone == zone)
			{
				return zones[i];
			}
		}
		return null;
	}

	public string GetSceneNameForZone(GTZone zone)
	{
		return GetZoneData(zone)?.sceneName;
	}

	public static bool IsValidZoneInt(int zoneInt)
	{
		if (zoneInt >= 11)
		{
			return zoneInt <= 24;
		}
		return false;
	}
}
