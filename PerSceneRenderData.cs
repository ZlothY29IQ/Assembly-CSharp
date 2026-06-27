using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PerSceneRenderData : MonoBehaviour
{
	public Renderer representativeRenderer;

	public string lightmapsResourcePath;

	public Texture2D singleLightmap;

	private int lastLightmapIndex = -1;

	public GameObject[] gO = new GameObject[5000];

	public MeshRenderer[] mRenderers = new MeshRenderer[5000];

	public int mRendererIndex;

	private readonly Dictionary<string, ResourceRequest> resourceRequests = new Dictionary<string, ResourceRequest>(8);

	private readonly Dictionary<string, Texture2D> lightmapsCache = new Dictionary<string, Texture2D>(8);

	private Dictionary<string, List<Action<Texture2D>>> _momentName_to_callbacks = new Dictionary<string, List<Action<Texture2D>>>(8);

	private static readonly HashSet<PerSceneRenderData> _g_allScenesPopulateLightmaps_renderDatasHashSet = new HashSet<PerSceneRenderData>(32);

	public static Action g_OnAllScenesPopulateLightmapsCompleted;

	private string _populateLightmaps_fromMomentName;

	private string _populateLightmaps_toMomentName;

	private Texture2D _populateLightmaps_fromMomentLightmap;

	private Texture2D _populateLightmaps_toMomentLightmap;

	public Action<PerSceneRenderData> OnPopulateToAndFromLightmapsCompleted;

	public string sceneName => base.gameObject.scene.name;

	public int sceneIndex => base.gameObject.scene.buildIndex;

	public bool IsLoadingLightmaps => resourceRequests.Count != 0;

	public int LoadingLightmapsCount => resourceRequests.Count;

	public static int g_AllScenesPopulatingLightmapsLoadCount => _g_allScenesPopulateLightmaps_renderDatasHashSet.Count;

	private void RefreshRenderer()
	{
		int num = sceneIndex;
		new List<Renderer>();
		Renderer[] array = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
		foreach (Renderer renderer in array)
		{
			if (renderer.gameObject.scene.buildIndex == num)
			{
				representativeRenderer = renderer;
				break;
			}
		}
	}

	private void Awake()
	{
		for (int i = 0; i < mRendererIndex; i++)
		{
			mRenderers[i] = gO[i].GetComponent<MeshRenderer>();
		}
	}

	private void OnEnable()
	{
		BetterDayNightManager.Register(this);
	}

	private void OnDisable()
	{
		BetterDayNightManager.Unregister(this);
	}

	public void AddMeshToList(GameObject _gO, MeshRenderer mR)
	{
		try
		{
			if (mR.lightmapIndex != -1)
			{
				gO[mRendererIndex] = _gO;
				mRendererIndex++;
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public bool CheckShouldRepopulate()
	{
		return representativeRenderer.lightmapIndex != lastLightmapIndex;
	}

	private Texture2D GetLightmap(string timeOfDay)
	{
		if (singleLightmap != null)
		{
			return singleLightmap;
		}
		if (!lightmapsCache.TryGetValue(timeOfDay, out var value))
		{
			if (resourceRequests.TryGetValue(timeOfDay, out var request))
			{
				return null;
			}
			request = Resources.LoadAsync<Texture2D>(Path.Combine(lightmapsResourcePath, timeOfDay));
			resourceRequests.Add(timeOfDay, request);
			request.completed += delegate
			{
				if (!(this == null))
				{
					lightmapsCache.Add(timeOfDay, (Texture2D)request.asset);
					resourceRequests.Remove(timeOfDay);
					if (BetterDayNightManager.instance != null)
					{
						BetterDayNightManager.instance.RequestRepopulateLightmaps();
					}
				}
			};
		}
		return value;
	}

	public void PopulateLightmaps(string fromTimeOfDay, string toTimeOfDay, LightmapData[] lightmaps)
	{
		LightmapData lightmapData = new LightmapData();
		lightmapData.lightmapColor = GetLightmap(fromTimeOfDay);
		lightmapData.lightmapDir = GetLightmap(toTimeOfDay);
		if (representativeRenderer == null)
		{
			RefreshRenderer();
		}
		if (representativeRenderer == null)
		{
			return;
		}
		if (lightmapData.lightmapColor != null && lightmapData.lightmapDir != null && representativeRenderer.lightmapIndex >= 0 && representativeRenderer.lightmapIndex < lightmaps.Length)
		{
			lightmaps[representativeRenderer.lightmapIndex] = lightmapData;
		}
		lastLightmapIndex = representativeRenderer.lightmapIndex;
		for (int i = 0; i < mRendererIndex; i++)
		{
			if (i < mRenderers.Length && gO[i] != null)
			{
				if (mRenderers[i] == null)
				{
					mRenderers[i] = gO[i].GetComponent<MeshRenderer>();
				}
				if (mRenderers[i] == null)
				{
					gO[i] = null;
				}
				else
				{
					mRenderers[i].lightmapIndex = lastLightmapIndex;
				}
			}
		}
	}

	public void ReleaseLightmap(string oldTimeOfDay)
	{
		if (lightmapsCache.Remove(oldTimeOfDay, out var value))
		{
			Resources.UnloadAsset(value);
		}
	}

	private void TryGetLightmapOrAsyncLoad(string momentName, Action<Texture2D> callback)
	{
		if (singleLightmap != null)
		{
			callback(singleLightmap);
		}
		if (lightmapsCache.TryGetValue(momentName, out var value))
		{
			callback(value);
		}
		if (!_momentName_to_callbacks.TryGetValue(momentName, out var callbacks))
		{
			callbacks = new List<Action<Texture2D>>(8);
			_momentName_to_callbacks[momentName] = callbacks;
		}
		if (!callbacks.Contains(callback))
		{
			callbacks.Add(callback);
		}
		if (resourceRequests.TryGetValue(momentName, out var request))
		{
			return;
		}
		request = Resources.LoadAsync<Texture2D>(Path.Combine(lightmapsResourcePath, momentName));
		resourceRequests.Add(momentName, request);
		request.completed += delegate
		{
			if (!(this == null) && !ApplicationQuittingState.IsQuitting)
			{
				Texture2D texture2D = (Texture2D)request.asset;
				lightmapsCache.Add(momentName, texture2D);
				resourceRequests.Remove(momentName);
				foreach (Action<Texture2D> item in callbacks)
				{
					item?.Invoke(texture2D);
				}
				callbacks.Clear();
			}
		};
	}

	public bool IsLightmapWithNameLoaded(string lightmapName)
	{
		if (singleLightmap != null)
		{
			return true;
		}
		GetFromAndToLightmapNames(out var fromLightmapName, out var toLightmapName);
		if (!string.IsNullOrEmpty(lightmapName))
		{
			if (string.IsNullOrEmpty(fromLightmapName) || !(fromLightmapName == lightmapName))
			{
				if (!string.IsNullOrEmpty(toLightmapName))
				{
					return toLightmapName == lightmapName;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public bool IsLightmapsWithNamesLoaded(string fromLightmapName, string toLightmapName)
	{
		if (singleLightmap != null)
		{
			return true;
		}
		GetFromAndToLightmapNames(out var fromLightmapName2, out var toLightmapName2);
		if (!string.IsNullOrEmpty(fromLightmapName) && !string.IsNullOrEmpty(toLightmapName) && !string.IsNullOrEmpty(fromLightmapName2) && fromLightmapName2 == fromLightmapName)
		{
			if (!string.IsNullOrEmpty(toLightmapName2))
			{
				return toLightmapName2 == toLightmapName;
			}
			return false;
		}
		return false;
	}

	public void GetFromAndToLightmapNames(out string fromLightmapName, out string toLightmapName)
	{
		if (singleLightmap != null)
		{
			fromLightmapName = null;
			toLightmapName = null;
			return;
		}
		LightmapData[] lightmaps = LightmapSettings.lightmaps;
		if (representativeRenderer.lightmapIndex < 0 || representativeRenderer.lightmapIndex >= lightmaps.Length)
		{
			fromLightmapName = null;
			toLightmapName = null;
			return;
		}
		Texture2D lightmapColor = lightmaps[representativeRenderer.lightmapIndex].lightmapColor;
		Texture2D lightmapDir = lightmaps[representativeRenderer.lightmapIndex].lightmapDir;
		fromLightmapName = ((lightmapColor != null) ? lightmapColor.name : null);
		toLightmapName = ((lightmapDir != null) ? lightmapDir.name : null);
	}

	public static void g_StartAllScenesPopulateLightmaps(string fromLightmapName, string toLightmapName)
	{
		_g_allScenesPopulateLightmaps_renderDatasHashSet.Clear();
		PerSceneRenderData[] array = UnityEngine.Object.FindObjectsByType<PerSceneRenderData>(FindObjectsSortMode.None);
		_g_allScenesPopulateLightmaps_renderDatasHashSet.UnionWith(array);
		PerSceneRenderData[] array2 = array;
		foreach (PerSceneRenderData obj in array2)
		{
			obj.StartPopulateLightmaps(fromLightmapName, toLightmapName);
			obj.OnPopulateToAndFromLightmapsCompleted = (Action<PerSceneRenderData>)Delegate.Combine(obj.OnPopulateToAndFromLightmapsCompleted, new Action<PerSceneRenderData>(_g_AllScenesPopulateLightmaps_OnOneCompleted));
		}
	}

	private static void _g_AllScenesPopulateLightmaps_OnOneCompleted(PerSceneRenderData perSceneRenderData)
	{
		int count = _g_allScenesPopulateLightmaps_renderDatasHashSet.Count;
		_g_allScenesPopulateLightmaps_renderDatasHashSet.Remove(perSceneRenderData);
		int count2 = _g_allScenesPopulateLightmaps_renderDatasHashSet.Count;
		if (count2 == 0 && count2 != count)
		{
			g_OnAllScenesPopulateLightmapsCompleted?.Invoke();
		}
	}

	public void StartPopulateLightmaps(string fromMomentName, string toMomentName)
	{
		_g_allScenesPopulateLightmaps_renderDatasHashSet.Clear();
		_populateLightmaps_fromMomentLightmap = null;
		_populateLightmaps_toMomentLightmap = null;
		_populateLightmaps_fromMomentName = fromMomentName;
		_populateLightmaps_toMomentName = toMomentName;
		TryGetLightmapOrAsyncLoad(fromMomentName, _PopulateLightmaps_OnLoadLightmap);
		TryGetLightmapOrAsyncLoad(toMomentName, _PopulateLightmaps_OnLoadLightmap);
	}

	private void _PopulateLightmaps_OnLoadLightmap(Texture2D lightmapTex)
	{
		if (this == null || ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (_populateLightmaps_fromMomentName != lightmapTex.name)
		{
			_populateLightmaps_fromMomentLightmap = lightmapTex;
		}
		if (_populateLightmaps_toMomentName != lightmapTex.name)
		{
			_populateLightmaps_toMomentLightmap = lightmapTex;
		}
		if (!(_populateLightmaps_fromMomentLightmap != null) || !(_populateLightmaps_toMomentLightmap != null))
		{
			return;
		}
		LightmapData[] lightmaps = LightmapSettings.lightmaps;
		LightmapData lightmapData = new LightmapData
		{
			lightmapColor = _populateLightmaps_fromMomentLightmap,
			lightmapDir = _populateLightmaps_toMomentLightmap
		};
		if (representativeRenderer.lightmapIndex >= 0 && representativeRenderer.lightmapIndex < lightmaps.Length)
		{
			lightmaps[representativeRenderer.lightmapIndex] = lightmapData;
		}
		LightmapSettings.lightmaps = lightmaps;
		lastLightmapIndex = representativeRenderer.lightmapIndex;
		for (int i = 0; i < mRendererIndex; i++)
		{
			if (i < mRenderers.Length && mRenderers[i] != null)
			{
				mRenderers[i].lightmapIndex = lastLightmapIndex;
			}
		}
		OnPopulateToAndFromLightmapsCompleted?.Invoke(this);
	}
}
