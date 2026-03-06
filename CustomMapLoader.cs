using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CosmeticRoom;
using CustomMapSupport;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion.Swimming;
using GorillaNetworking;
using GorillaNetworking.Store;
using GorillaTag.Rendering;
using GorillaTagScripts;
using GorillaTagScripts.CustomMapSupport;
using GorillaTagScripts.VirtualStumpCustomMaps;
using GT_CustomMapSupportRuntime;
using Modio;
using Modio.Mods;
using Newtonsoft.Json;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

public class CustomMapLoader : MonoBehaviour, IBuildValidation
{
	private struct LoadZoneRequest
	{
		public int[] sceneIndexesToLoad;

		public int[] sceneIndexesToUnload;

		public Action<string> onSceneLoadedCallback;

		public Action<string> onSceneUnloadedCallback;
	}

	[SerializeField]
	private NexusGroupId defaultNexusGroupId;

	[OnEnterPlay_SetNull]
	private static volatile CustomMapLoader instance;

	[OnEnterPlay_Set(false)]
	private static bool hasInstance;

	public Transform CustomMapsDefaultSpawnLocation;

	public CustomMapAccessDoor accessDoor;

	[FormerlySerializedAs("networkTrigger")]
	public GameObject publicJoinTrigger;

	[SerializeField]
	private BetterDayNightManager dayNightManager;

	[SerializeField]
	private GhostReactorManager ghostReactorManager;

	[SerializeField]
	private GameObject placeholderParent;

	[SerializeField]
	private GliderHoldable[] leafGliders;

	[SerializeField]
	private GameObject leafGlider;

	[SerializeField]
	private GameObject gliderWindVolume;

	[FormerlySerializedAs("waterVolume")]
	[SerializeField]
	private GameObject waterVolumePrefab;

	[SerializeField]
	private WaterParameters defaultWaterParameters;

	[SerializeField]
	private WaterParameters defaultLavaParameters;

	[FormerlySerializedAs("forceVolume")]
	[SerializeField]
	private GameObject forceVolumePrefab;

	[SerializeField]
	private GameObject atmPrefab;

	[SerializeField]
	private GameObject atmNoShellPrefab;

	[SerializeField]
	private GameObject storeDisplayStandPrefab;

	[SerializeField]
	private GameObject storeCheckoutCounterPrefab;

	[SerializeField]
	private GameObject storeTryOnConsolePrefab;

	[SerializeField]
	private GameObject storeTryOnAreaPrefab;

	[SerializeField]
	private GameObject hoverboardDispenserPrefab;

	[SerializeField]
	private GameObject ropeSwingPrefab;

	[SerializeField]
	private GameObject ziplinePrefab;

	[SerializeField]
	private GameObject reviveStationPrefab;

	[SerializeField]
	private GameObject zoneShaderSettingsTrigger;

	[SerializeField]
	private AudioMixerGroup masterAudioMixer;

	[SerializeField]
	private ZoneShaderSettings customMapZoneShaderSettings;

	[SerializeField]
	private CompositeTriggerEvents compositeTryOnArea;

	[SerializeField]
	private GameObject virtualStumpMesh;

	[SerializeField]
	private List<GameModeType> availableModesForOldMaps = new List<GameModeType>
	{
		GameModeType.Infection,
		GameModeType.FreezeTag,
		GameModeType.Paintbrawl
	};

	[SerializeField]
	private GameModeType defaultGameModeForNonCustomOldMaps = GameModeType.Infection;

	public TMP_FontAsset DefaultFont;

	private static readonly int numObjectsToProcessPerFrame = 5;

	private static readonly List<int> APPROVED_LAYERS = new List<int>
	{
		0, 1, 2, 4, 5, 9, 11, 18, 20, 22,
		27, 30
	};

	private static bool isLoading;

	private static bool isUnloading;

	private static bool runningAsyncLoad = false;

	private static long attemptedLoadID = 0L;

	private static string attemptedSceneToLoad;

	private static bool shouldAbortMapLoading = false;

	private static bool shouldAbortSceneLoad = false;

	private static bool errorEncounteredDuringLoad = false;

	private static Action unloadMapCallback;

	private static string cachedExceptionMessage = "";

	private static AssetBundle mapBundle;

	private static List<string> initialSceneNames = new List<string>();

	private static List<int> initialSceneIndexes = new List<int>();

	private static byte maxPlayersForMap = 20;

	private static ModId loadedMapModId;

	private static long loadedMapModFileId;

	private static MapPackageInfo loadedMapPackageInfo;

	private static string cachedLuauScript;

	private static bool devModeEnabled;

	private static bool disableHoldingHandsAllModes;

	private static bool disableHoldingHandsCustomMode;

	private static Action<MapLoadStatus, int, string> mapLoadProgressCallback;

	private static Action<bool> mapLoadFinishedCallback;

	private static Coroutine zoneLoadingCoroutine;

	private static Action<string> sceneLoadedCallback;

	private static Action<string> sceneUnloadedCallback;

	private static List<LoadZoneRequest> queuedLoadZoneRequests = new List<LoadZoneRequest>();

	private static string[] assetBundleSceneFilePaths;

	private static List<string> loadedSceneFilePaths = new List<string>();

	private static List<string> loadedSceneNames = new List<string>();

	private static List<int> loadedSceneIndexes = new List<int>();

	private Coroutine loadScenesCoroutine;

	private static int leafGliderIndex;

	private static bool usingDynamicLighting = false;

	private static bool refreshReviveStations = false;

	private static int totalObjectsInLoadingScene = 0;

	private static int objectsProcessedForLoadingScene = 0;

	private static int objectsProcessedThisFrame = 0;

	private static List<Component> initializePhaseTwoComponents = new List<Component>();

	private static List<MapEntity> entitiesToCreate = new List<MapEntity>(GT_CustomMapSupportRuntime.Constants.aiAgentLimit);

	private static LightmapData[] lightmaps;

	private static List<Texture2D> lightmapsToKeep = new List<Texture2D>();

	private static List<GameObject> placeholderReplacements = new List<GameObject>();

	private static GameObject customMapATM = null;

	private static List<GameObject> storeCheckouts = new List<GameObject>();

	private static List<GameObject> storeDisplayStands = new List<GameObject>();

	private static List<GameObject> storeTryOnConsoles = new List<GameObject>();

	private static List<GameObject> storeTryOnAreas = new List<GameObject>();

	private static List<Component> teleporters = new List<Component>();

	private string dontDestroyOnLoadSceneName = "";

	private static readonly List<Type> componentAllowlist = new List<Type>
	{
		typeof(MeshRenderer),
		typeof(Transform),
		typeof(MeshFilter),
		typeof(MeshRenderer),
		typeof(Collider),
		typeof(BoxCollider),
		typeof(SphereCollider),
		typeof(CapsuleCollider),
		typeof(MeshCollider),
		typeof(Light),
		typeof(ReflectionProbe),
		typeof(AudioSource),
		typeof(Animator),
		typeof(SkinnedMeshRenderer),
		typeof(TextMesh),
		typeof(ParticleSystem),
		typeof(ParticleSystemRenderer),
		typeof(RectTransform),
		typeof(SpriteRenderer),
		typeof(BillboardRenderer),
		typeof(Canvas),
		typeof(CanvasRenderer),
		typeof(CanvasScaler),
		typeof(GraphicRaycaster),
		typeof(Rigidbody),
		typeof(TrailRenderer),
		typeof(LineRenderer),
		typeof(LensFlareComponentSRP),
		typeof(Camera),
		typeof(UniversalAdditionalCameraData),
		typeof(NavMeshAgent),
		typeof(NavMesh),
		typeof(NavMeshObstacle),
		typeof(NavMeshLink),
		typeof(NavMeshModifierVolume),
		typeof(NavMeshModifier),
		typeof(NavMeshSurface),
		typeof(HingeJoint),
		typeof(ConstantForce),
		typeof(LODGroup),
		typeof(MapDescriptor),
		typeof(AccessDoorPlaceholder),
		typeof(MapOrientationPoint),
		typeof(SurfaceOverrideSettings),
		typeof(TeleporterSettings),
		typeof(TagZoneSettings),
		typeof(LuauTriggerSettings),
		typeof(MapBoundarySettings),
		typeof(ObjectActivationTriggerSettings),
		typeof(LoadZoneSettings),
		typeof(GTObjectPlaceholder),
		typeof(CMSZoneShaderSettings),
		typeof(ZoneShaderTriggerSettings),
		typeof(MultiPartFire),
		typeof(HandHoldSettings),
		typeof(CustomMapEjectButtonSettings),
		typeof(CustomMapSupport.BezierSpline),
		typeof(UberShaderDynamicLight),
		typeof(MapEntity),
		typeof(GrabbableEntity),
		typeof(AIAgent),
		typeof(AISpawnManager),
		typeof(AISpawnPoint),
		typeof(MapSpawnPoint),
		typeof(MapSpawnManager),
		typeof(RopeSwingSegment),
		typeof(ZiplineSegment),
		typeof(PlayAnimationTriggerSettings),
		typeof(SurfaceMoverSettings),
		typeof(MovingSurfaceSettings),
		typeof(CustomMapReviveStation),
		typeof(ProBuilderMesh),
		typeof(TMP_Text),
		typeof(TextMeshPro),
		typeof(TextMeshProUGUI),
		typeof(UniversalAdditionalLightData),
		typeof(BakerySkyLight),
		typeof(BakeryDirectLight),
		typeof(BakeryPointLight),
		typeof(ftLightmapsStorage),
		typeof(BakeryAlwaysRender),
		typeof(BakeryLightMesh),
		typeof(BakeryLightmapGroupSelector),
		typeof(BakeryPackAsSingleSquare),
		typeof(BakerySector),
		typeof(BakeryVolume),
		typeof(BakeryLightmapGroup)
	};

	private static readonly List<string> componentTypeStringAllowList = new List<string> { "UnityEngine.Halo" };

	private static readonly Type[] badComponents = new Type[7]
	{
		typeof(EventTrigger),
		typeof(UIBehaviour),
		typeof(GorillaPressableButton),
		typeof(GorillaPressableDelayButton),
		typeof(Camera),
		typeof(AudioListener),
		typeof(VideoPlayer)
	};

	public static ModId LoadedMapModId => loadedMapModId;

	public static long LoadedMapModFileId => loadedMapModFileId;

	public static bool CanLoadEntities { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitOnLoad()
	{
		GTDev.Log("CML::InitOnLoad");
		instance = null;
		hasInstance = false;
		isLoading = false;
		isUnloading = false;
		runningAsyncLoad = false;
		attemptedLoadID = 0L;
		attemptedSceneToLoad = null;
		shouldAbortMapLoading = false;
		shouldAbortSceneLoad = false;
		errorEncounteredDuringLoad = false;
		unloadMapCallback = null;
		cachedExceptionMessage = "";
		mapBundle = null;
		initialSceneNames = new List<string>();
		initialSceneIndexes = new List<int>();
		maxPlayersForMap = 20;
		loadedMapModId = ModId.Null;
		loadedMapModFileId = -1L;
		loadedMapPackageInfo = null;
		cachedLuauScript = null;
		devModeEnabled = false;
		disableHoldingHandsAllModes = false;
		disableHoldingHandsCustomMode = false;
		mapLoadProgressCallback = null;
		mapLoadFinishedCallback = null;
		zoneLoadingCoroutine = null;
		sceneLoadedCallback = null;
		sceneUnloadedCallback = null;
		queuedLoadZoneRequests = new List<LoadZoneRequest>();
		assetBundleSceneFilePaths = null;
		loadedSceneFilePaths = new List<string>();
		loadedSceneNames = new List<string>();
		loadedSceneIndexes = new List<int>();
		leafGliderIndex = 0;
		usingDynamicLighting = false;
		totalObjectsInLoadingScene = 0;
		objectsProcessedForLoadingScene = 0;
		objectsProcessedThisFrame = 0;
		initializePhaseTwoComponents = new List<Component>();
		entitiesToCreate = new List<MapEntity>(GT_CustomMapSupportRuntime.Constants.aiAgentLimit);
		lightmaps = null;
		lightmapsToKeep = new List<Texture2D>();
		placeholderReplacements = new List<GameObject>();
		customMapATM = null;
		storeCheckouts = new List<GameObject>();
		storeDisplayStands = new List<GameObject>();
		storeTryOnConsoles = new List<GameObject>();
		storeTryOnAreas = new List<GameObject>();
	}

	private void Awake()
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

	private void Start()
	{
		byte[] bytes = new byte[17]
		{
			Convert.ToByte(68),
			Convert.ToByte(111),
			Convert.ToByte(110),
			Convert.ToByte(116),
			Convert.ToByte(68),
			Convert.ToByte(101),
			Convert.ToByte(115),
			Convert.ToByte(116),
			Convert.ToByte(114),
			Convert.ToByte(111),
			Convert.ToByte(121),
			Convert.ToByte(79),
			Convert.ToByte(110),
			Convert.ToByte(76),
			Convert.ToByte(111),
			Convert.ToByte(97),
			Convert.ToByte(100)
		};
		dontDestroyOnLoadSceneName = Encoding.ASCII.GetString(bytes);
		if (publicJoinTrigger != null)
		{
			publicJoinTrigger.SetActive(value: false);
		}
	}

	public static void Initialize(Action<MapLoadStatus, int, string> onLoadProgress, Action<bool> onLoadFinished, Action<string> onSceneLoaded, Action<string> onSceneUnloaded)
	{
		mapLoadProgressCallback = onLoadProgress;
		mapLoadFinishedCallback = onLoadFinished;
		sceneLoadedCallback = onSceneLoaded;
		sceneUnloadedCallback = onSceneUnloaded;
	}

	public static void LoadMap(long mapModId, string mapFilePath)
	{
		if (hasInstance && !isLoading)
		{
			if (isUnloading)
			{
				mapLoadFinishedCallback?.Invoke(obj: false);
				return;
			}
			if (IsMapLoaded(mapModId))
			{
				mapLoadFinishedCallback?.Invoke(obj: true);
				return;
			}
			GorillaNetworkJoinTrigger.DisableTriggerJoins();
			CanLoadEntities = false;
			instance.StartCoroutine(LoadAssetBundle(mapModId, mapFilePath, OnAssetBundleLoaded));
		}
	}

	public static bool OpenDoorToMap()
	{
		if (!hasInstance)
		{
			return false;
		}
		if (instance.accessDoor != null)
		{
			instance.accessDoor.OpenDoor();
			return true;
		}
		return false;
	}

	private static IEnumerator LoadAssetBundle(long mapModID, string packageInfoFilePath, Action<bool, bool> OnLoadComplete)
	{
		isLoading = true;
		errorEncounteredDuringLoad = false;
		attemptedLoadID = mapModID;
		refreshReviveStations = false;
		instance.ghostReactorManager.reactor.RefreshReviveStations();
		mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, 1, "CACHING LIGHTMAP DATA");
		CacheLightmaps();
		mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, 2, "LOADING PACKAGE INFO");
		try
		{
			loadedMapPackageInfo = GetPackageInfo(packageInfoFilePath);
		}
		catch (Exception ex)
		{
			Debug.LogError($"[CML.LoadAssetBundle] GetPackageInfo Exception: {ex}");
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, ex.ToString());
			OnLoadComplete(arg1: false, arg2: false);
			yield break;
		}
		if (loadedMapPackageInfo == null)
		{
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, "FAILED TO READ FILE AT " + packageInfoFilePath);
			OnLoadComplete(arg1: false, arg2: false);
			yield break;
		}
		LoadInitialSceneNames();
		mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, 3, "PACKAGE INFO LOADED");
		string path = Path.GetDirectoryName(packageInfoFilePath) + "/" + loadedMapPackageInfo.pcFileName;
		mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, 4, "LOADING MAP ASSET BUNDLE");
		AssetBundleCreateRequest loadBundleRequest = AssetBundle.LoadFromFileAsync(path);
		yield return loadBundleRequest;
		mapBundle = loadBundleRequest.assetBundle;
		if (shouldAbortMapLoading || shouldAbortSceneLoad)
		{
			yield return AbortSceneLoad(-1);
			OnLoadComplete(arg1: false, arg2: true);
			yield break;
		}
		if (mapBundle == null)
		{
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, "CUSTOM MAP ASSET BUNDLE FAILED TO LOAD");
			OnLoadComplete(arg1: false, arg2: false);
			yield break;
		}
		if (!mapBundle.isStreamedSceneAssetBundle)
		{
			mapBundle.Unload(unloadAllLoadedObjects: true);
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, "AssetBundle does not contain a Unity Scene file");
			OnLoadComplete(arg1: false, arg2: false);
			yield break;
		}
		mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, 10, "MAP ASSET BUNDLE LOADED");
		assetBundleSceneFilePaths = mapBundle.GetAllScenePaths();
		if (assetBundleSceneFilePaths.Length == 0)
		{
			mapBundle.Unload(unloadAllLoadedObjects: true);
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, "AssetBundle does not contain a Unity Scene file");
			OnLoadComplete(arg1: false, arg2: false);
			yield break;
		}
		string[] array = assetBundleSceneFilePaths;
		foreach (string text in array)
		{
			if (text.Equals(instance.dontDestroyOnLoadSceneName, StringComparison.OrdinalIgnoreCase))
			{
				mapBundle.Unload(unloadAllLoadedObjects: true);
				mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, "Map name is " + text + " this is an invalid name");
				OnLoadComplete(arg1: false, arg2: false);
				yield break;
			}
		}
		OnLoadComplete(arg1: true, arg2: false);
	}

	private static void LoadInitialSceneNames()
	{
		initialSceneNames.Clear();
		if (loadedMapPackageInfo != null)
		{
			if (loadedMapPackageInfo.customMapSupportVersion <= 2)
			{
				initialSceneNames.Add(loadedMapPackageInfo.initialScene);
			}
			else if (loadedMapPackageInfo.customMapSupportVersion > 2)
			{
				initialSceneNames.AddRange(loadedMapPackageInfo.initialScenes);
			}
		}
	}

	private static void OnAssetBundleLoaded(bool loadSucceeded, bool loadAborted)
	{
		if (loadAborted || !loadSucceeded)
		{
			return;
		}
		loadedMapModId = attemptedLoadID;
		loadedMapModFileId = 0L;
		ModIOManager.GetMod(new ModId(loadedMapModId), forceUpdate: false, delegate(Error error, Mod mod)
		{
			if (!error && mod != null && mod.File != null)
			{
				loadedMapModFileId = mod.File.Id;
			}
		});
		foreach (string initialSceneName in initialSceneNames)
		{
			int num = -1;
			if (initialSceneName != string.Empty)
			{
				num = GetSceneIndex(initialSceneName);
			}
			if (num == -1)
			{
				GTDev.LogError("[CustomMapLoader::OnAssetBundleLoaded] Encountered invalid initial scene, could not get scene index for: \"" + initialSceneName + "\"");
			}
			else
			{
				initialSceneIndexes.Add(num);
			}
		}
		if (initialSceneIndexes.Count == 0)
		{
			if (assetBundleSceneFilePaths.Length == 1)
			{
				GTDev.LogWarning("[CustomMapLoader::OnAssetBundleLoaded] Asset Bundle only contains 1 Scene, but it isn't marked as an initial scene. Treating it as an initial scene...");
				initialSceneIndexes.Add(0);
			}
			else if (mapBundle != null)
			{
				string arg = "";
				if (assetBundleSceneFilePaths.Length == 0)
				{
					arg = "MAP ASSET BUNDLE CONTAINS NO VALID SCENES.";
				}
				else if (assetBundleSceneFilePaths.Length > 1)
				{
					arg = "MAP ASSET BUNDLE CONTAINS MULTIPLE SCENES, BUT NONE ARE SET AS INITIAL SCENE.";
				}
				mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, arg);
				OnInitialLoadComplete(loadSucceeded: false, loadAborted: true);
			}
		}
		instance.StartCoroutine(LoadInitialScenesCoroutine(initialSceneIndexes.ToArray()));
	}

	private static IEnumerator LoadInitialScenesCoroutine(int[] sceneIndexes)
	{
		if (!loadedSceneIndexes.IsNullOrEmpty())
		{
			GTDev.LogError("[CustomMapLoader::LoadInitialScenesCoroutine] loadedSceneIndexes is not empty, LoadInitialScenes should not be called in this case!");
			yield break;
		}
		int progressAmountPerScene = 89 / sceneIndexes.Length;
		GTDev.Log($"[CustomMapLoader::LoadInitialScenesCoroutine] loading {sceneIndexes.Length} scenes...");
		for (int i = 0; i < sceneIndexes.Length; i++)
		{
			int num = 10 + i * progressAmountPerScene;
			int endingProgress = num + progressAmountPerScene;
			bool isLastScene = i == sceneIndexes.Length - 1;
			bool stopLoading = false;
			bool initialLoadAborted = false;
			yield return LoadSceneFromAssetBundle(sceneIndexes[i], delegate(bool loadSucceeded, bool loadAborted, string loadedSceneName)
			{
				if (!loadSucceeded || loadAborted)
				{
					GTDev.Log("[CustomMapLoader::LoadInitialScenesCoroutine] failed to load scene at index " + $"\"{sceneIndexes[i]}\", aborting initial load...");
					stopLoading = true;
					initialLoadAborted = loadAborted;
				}
				else if (isLastScene)
				{
					OnInitialLoadComplete(loadSucceeded: true, loadAborted: false);
				}
			}, useProgressCallback: true, num, endingProgress);
			if (stopLoading || shouldAbortMapLoading)
			{
				OnInitialLoadComplete(loadSucceeded: false, initialLoadAborted);
				break;
			}
		}
	}

	private static void OnInitialLoadComplete(bool loadSucceeded, bool loadAborted)
	{
		if (loadAborted || !loadSucceeded)
		{
			if (!loadAborted)
			{
				instance.StartCoroutine(AbortMapLoad());
			}
			else
			{
				mapLoadFinishedCallback?.Invoke(obj: false);
			}
			return;
		}
		if (loadedMapPackageInfo != null && loadedMapPackageInfo.customMapSupportVersion >= 3)
		{
			maxPlayersForMap = (byte)System.Math.Clamp(loadedMapPackageInfo.maxPlayers, 1, 20);
			if (loadedMapPackageInfo.customMapSupportVersion >= 5)
			{
				CustomMapModeSelector.SetAvailableGameModes(loadedMapPackageInfo.availableGameModes, loadedMapPackageInfo.defaultGameMode);
				if (RoomSystem.JoinedRoom && NetworkSystem.Instance.LocalPlayer.IsMasterClient && NetworkSystem.Instance.SessionIsPrivate)
				{
					if (GameMode.ActiveGameMode.IsNull())
					{
						GameModeType defaultGameMode = (GameModeType)loadedMapPackageInfo.defaultGameMode;
						GameMode.ChangeGameMode(defaultGameMode.ToString());
					}
					else if (GameMode.ActiveGameMode.GameType() != (GameModeType)loadedMapPackageInfo.defaultGameMode)
					{
						GameModeType defaultGameMode = (GameModeType)loadedMapPackageInfo.defaultGameMode;
						GameMode.ChangeGameMode(defaultGameMode.ToString());
					}
				}
			}
			else
			{
				List<int> list = new List<int>();
				foreach (GameModeType availableModesForOldMap in instance.availableModesForOldMaps)
				{
					list.Add((int)availableModesForOldMap);
				}
				GameModeType gameModeType = instance.defaultGameModeForNonCustomOldMaps;
				if (!loadedMapPackageInfo.customGamemodeScript.IsNullOrEmpty())
				{
					gameModeType = GameModeType.Custom;
					list.Add(7);
				}
				CustomMapModeSelector.SetAvailableGameModes(list.ToArray(), (int)gameModeType);
				if (RoomSystem.JoinedRoom && NetworkSystem.Instance.LocalPlayer.IsMasterClient && NetworkSystem.Instance.SessionIsPrivate)
				{
					if (GameMode.ActiveGameMode.IsNull())
					{
						GameMode.ChangeGameMode(gameModeType.ToString());
					}
					else if (GameMode.ActiveGameMode.GameType() != gameModeType)
					{
						GameMode.ChangeGameMode(gameModeType.ToString());
					}
				}
			}
			cachedLuauScript = loadedMapPackageInfo.customGamemodeScript;
			devModeEnabled = loadedMapPackageInfo.devMode;
			disableHoldingHandsAllModes = loadedMapPackageInfo.disableHoldingHandsAllModes;
			disableHoldingHandsCustomMode = loadedMapPackageInfo.disableHoldingHandsCustomMode;
			Color ambientLightDynamic = new Color(loadedMapPackageInfo.uberShaderAmbientDynamicLight_R, loadedMapPackageInfo.uberShaderAmbientDynamicLight_G, loadedMapPackageInfo.uberShaderAmbientDynamicLight_B, loadedMapPackageInfo.uberShaderAmbientDynamicLight_A);
			if (loadedMapPackageInfo.useUberShaderDynamicLighting)
			{
				GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: true);
				GameLightingManager.instance.SetAmbientLightDynamic(ambientLightDynamic);
				usingDynamicLighting = true;
			}
			VirtualStumpReturnWatch.SetWatchProperties(loadedMapPackageInfo.GetReturnToVStumpWatchProps());
		}
		isLoading = false;
		CanLoadEntities = true;
		GorillaNetworkJoinTrigger.EnableTriggerJoins();
		mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, 100, "LOAD COMPLETE");
		if (instance.publicJoinTrigger != null)
		{
			instance.publicJoinTrigger.SetActive(value: true);
		}
		foreach (string loadedSceneName in loadedSceneNames)
		{
			sceneLoadedCallback?.Invoke(loadedSceneName);
		}
		mapLoadFinishedCallback?.Invoke(obj: true);
	}

	private static IEnumerator LoadScenesCoroutine(int[] sceneIndexes, Action<bool, bool, List<string>> loadCompleteCallback = null)
	{
		if (sceneIndexes.IsNullOrEmpty())
		{
			loadCompleteCallback?.Invoke(arg1: false, arg2: false, null);
			yield break;
		}
		isLoading = true;
		List<string> successfullyLoadedSceneNames = new List<string>();
		bool successfullyLoadedAllScenes = true;
		for (int i = 0; i < sceneIndexes.Length; i++)
		{
			if (loadedSceneIndexes.Contains(sceneIndexes[i]))
			{
				GTDev.LogWarning("[CustomMapLoader::LoadScenesCoroutine] Cannot load scene " + $"{sceneIndexes[i]}:\"{assetBundleSceneFilePaths[sceneIndexes[i]]}\" because it's already loaded!");
				continue;
			}
			bool shouldAbortLoad = false;
			bool isLastScene = i == sceneIndexes.Length - 1;
			yield return LoadSceneFromAssetBundle(sceneIndexes[i], delegate(bool loadSucceeded, bool loadAborted, string loadedSceneName)
			{
				if (!loadSucceeded || loadAborted)
				{
					successfullyLoadedAllScenes = false;
				}
				else
				{
					sceneLoadedCallback?.Invoke(loadedSceneName);
					successfullyLoadedSceneNames.Add(loadedSceneName);
				}
				if (loadAborted)
				{
					shouldAbortLoad = true;
				}
				else if (isLastScene)
				{
					loadCompleteCallback?.Invoke(successfullyLoadedAllScenes, arg2: false, successfullyLoadedSceneNames);
				}
			});
			if (shouldAbortLoad)
			{
				isLoading = false;
				loadCompleteCallback?.Invoke(arg1: false, arg2: true, successfullyLoadedSceneNames);
				break;
			}
		}
		isLoading = false;
	}

	private static IEnumerator LoadSceneFromAssetBundle(int sceneIndex, Action<bool, bool, string> OnLoadComplete, bool useProgressCallback = false, int startingProgress = 10, int endingProgress = 90)
	{
		int progressAmount = endingProgress - startingProgress;
		int currentProgress = startingProgress;
		refreshReviveStations = false;
		LoadSceneParameters parameters = new LoadSceneParameters
		{
			loadSceneMode = LoadSceneMode.Additive,
			localPhysicsMode = LocalPhysicsMode.None
		};
		if (shouldAbortSceneLoad)
		{
			yield return AbortSceneLoad(sceneIndex);
			OnLoadComplete(arg1: false, arg2: true, "");
			yield break;
		}
		runningAsyncLoad = true;
		if (useProgressCallback)
		{
			int arg = startingProgress + Mathf.RoundToInt((float)progressAmount * 0.02f);
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, arg, "LOADING MAP SCENE");
		}
		attemptedSceneToLoad = assetBundleSceneFilePaths[sceneIndex];
		string sceneName = GetSceneNameFromFilePath(attemptedSceneToLoad);
		yield return SceneManager.LoadSceneAsync(attemptedSceneToLoad, parameters);
		runningAsyncLoad = false;
		if (shouldAbortSceneLoad)
		{
			yield return AbortSceneLoad(sceneIndex);
			OnLoadComplete(arg1: false, arg2: true, "");
			yield break;
		}
		if (useProgressCallback)
		{
			currentProgress += Mathf.RoundToInt((float)progressAmount * 0.28f);
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, currentProgress, "SANITIZING MAP");
		}
		GameObject[] rootGameObjects = SceneManager.GetSceneByName(sceneName).GetRootGameObjects();
		List<MapDescriptor> list = new List<MapDescriptor>();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			MapDescriptor component = rootGameObjects[i].GetComponent<MapDescriptor>();
			if (component.IsNotNull())
			{
				list.Add(component);
			}
		}
		MapDescriptor mapDescriptor = null;
		bool flag = false;
		foreach (MapDescriptor item in list)
		{
			if (mapDescriptor.IsNull())
			{
				mapDescriptor = item;
				continue;
			}
			flag = true;
			break;
		}
		if (flag)
		{
			GTDev.LogWarning("[CustomMapLoader::LoadSceneFromAssetBundle] Found multiple MapDescriptor components in Scene \"" + sceneName + "\". Only the first one found will be used...");
		}
		if (mapDescriptor.IsNull())
		{
			yield return AbortSceneLoad(sceneIndex);
			if (useProgressCallback)
			{
				mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, "SCENE \"" + sceneName + "\" DOES NOT CONTAIN A MAP DESCRIPTOR ON ONE OF ITS ROOT GAME OBJECTS.");
			}
			OnLoadComplete(arg1: false, arg2: false, "");
			yield break;
		}
		GameObject gameObject = mapDescriptor.gameObject;
		if (!SanitizeObject(gameObject, gameObject))
		{
			yield return AbortSceneLoad(sceneIndex);
			if (useProgressCallback)
			{
				mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, "MAP DESCRIPTOR GAME OBJECT ON SCENE \"" + sceneName + "\" HAS UNAPPROVED COMPONENTS ON IT");
			}
			OnLoadComplete(arg1: false, arg2: false, "");
			yield break;
		}
		if (loadedMapPackageInfo.customMapSupportVersion < 4)
		{
			TextMeshPro[] componentsInChildren = gameObject.transform.GetComponentsInChildren<TextMeshPro>(includeInactive: true);
			foreach (TextMeshPro textMeshPro in componentsInChildren)
			{
				if (textMeshPro.font == null || textMeshPro.font.material == null)
				{
					textMeshPro.font = instance.DefaultFont;
				}
			}
			TextMeshProUGUI[] componentsInChildren2 = gameObject.transform.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
			foreach (TextMeshProUGUI textMeshProUGUI in componentsInChildren2)
			{
				if (textMeshProUGUI.font == null || textMeshProUGUI.font.material == null)
				{
					textMeshProUGUI.font = instance.DefaultFont;
				}
			}
		}
		totalObjectsInLoadingScene = 0;
		for (int l = 0; l < rootGameObjects.Length; l++)
		{
			SanitizeObjectRecursive(rootGameObjects[l], gameObject);
		}
		ResolveVirtualStumpColliderOverlaps(sceneName);
		if (useProgressCallback)
		{
			currentProgress += Mathf.RoundToInt((float)progressAmount * 0.2f);
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, currentProgress, "MAP SCENE LOADED");
		}
		leafGliderIndex = 0;
		yield return FinalizeSceneLoad(mapDescriptor, useProgressCallback, currentProgress, endingProgress);
		yield return null;
		if (shouldAbortSceneLoad)
		{
			yield return AbortSceneLoad(sceneIndex);
			OnLoadComplete(arg1: false, arg2: true, "");
			if (cachedExceptionMessage.Length > 0 && useProgressCallback)
			{
				mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, cachedExceptionMessage);
			}
			yield break;
		}
		if (errorEncounteredDuringLoad)
		{
			OnLoadComplete(arg1: false, arg2: false, "");
			if (cachedExceptionMessage.Length > 0 && useProgressCallback)
			{
				mapLoadProgressCallback?.Invoke(MapLoadStatus.Error, 0, cachedExceptionMessage);
			}
			yield break;
		}
		if (useProgressCallback)
		{
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, endingProgress, "FINALIZING MAP");
		}
		loadedSceneFilePaths.AddIfNew(attemptedSceneToLoad);
		loadedSceneNames.AddIfNew(sceneName);
		loadedSceneIndexes.AddIfNew(sceneIndex);
		if (refreshReviveStations)
		{
			instance.ghostReactorManager.reactor.RefreshReviveStations(searchScene: true);
		}
		OnLoadComplete(arg1: true, arg2: false, sceneName);
	}

	private static void SanitizeObjectRecursive(GameObject rootObject, GameObject mapRoot)
	{
		if (!SanitizeObject(rootObject, mapRoot))
		{
			return;
		}
		totalObjectsInLoadingScene++;
		for (int i = 0; i < rootObject.transform.childCount; i++)
		{
			GameObject gameObject = rootObject.transform.GetChild(i).gameObject;
			if (gameObject.IsNotNull())
			{
				SanitizeObjectRecursive(gameObject, mapRoot);
			}
		}
	}

	private static bool SanitizeObject(GameObject gameObject, GameObject mapRoot)
	{
		if (gameObject == null)
		{
			Debug.LogError("CustomMapLoader::SanitizeObject gameobject null");
			return false;
		}
		if (!APPROVED_LAYERS.Contains(gameObject.layer))
		{
			gameObject.layer = 0;
		}
		Component[] components = gameObject.GetComponents<Component>();
		foreach (Component component in components)
		{
			if (component == null)
			{
				UnityEngine.Object.DestroyImmediate(gameObject, allowDestroyingAssets: true);
				return false;
			}
			bool flag = true;
			foreach (Type item in componentAllowlist)
			{
				if (!(component.GetType() == item))
				{
					continue;
				}
				if (item == typeof(Camera))
				{
					Camera camera = (Camera)component;
					if (camera.IsNotNull() && camera.targetTexture.IsNull())
					{
						break;
					}
				}
				flag = false;
				break;
			}
			if (flag)
			{
				foreach (string componentTypeStringAllow in componentTypeStringAllowList)
				{
					if (component.GetType().ToString().Contains(componentTypeStringAllow))
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				UnityEngine.Object.DestroyImmediate(gameObject, allowDestroyingAssets: true);
				return false;
			}
		}
		if (gameObject.transform.parent.IsNull() && gameObject.transform != mapRoot.transform)
		{
			gameObject.transform.SetParent(mapRoot.transform);
		}
		return true;
	}

	private static void ResolveVirtualStumpColliderOverlaps(string sceneName)
	{
		Vector3 localScale = new Vector3(5.15f, 0.72f, 5.15f);
		Vector3 vector = new Vector3(0f, 0.73f, 0f);
		float radius = localScale.x * 0.5f + 2f;
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		gameObject.transform.position = instance.virtualStumpMesh.transform.position + vector;
		gameObject.transform.localScale = localScale;
		Collider[] array = Physics.OverlapSphere(gameObject.transform.position, radius);
		if (array == null || array.Length == 0)
		{
			UnityEngine.Object.DestroyImmediate(gameObject);
			return;
		}
		MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
		meshCollider.convex = true;
		Collider[] array2 = array;
		foreach (Collider collider in array2)
		{
			if (!(collider == null) && !(collider.gameObject == gameObject) && !(collider.gameObject.scene.name != sceneName) && Physics.ComputePenetration(meshCollider, gameObject.transform.position, gameObject.transform.rotation, collider, collider.transform.position, collider.transform.rotation, out var _, out var _) && !collider.isTrigger)
			{
				GTDev.Log("[CustomMapLoader::ResolveVirtualStumpColliderOverlaps] Gameobject " + collider.name + " has a collider overlapping with the virtual stump. Collider will be removed");
				UnityEngine.Object.DestroyImmediate(collider);
			}
		}
		UnityEngine.Object.DestroyImmediate(gameObject);
	}

	private static IEnumerator FinalizeSceneLoad(MapDescriptor sceneDescriptor, bool useProgressCallback = false, int startingProgress = 50, int endingProgress = 90)
	{
		int num = endingProgress - startingProgress;
		int num2 = startingProgress;
		if (useProgressCallback)
		{
			num2 += Mathf.RoundToInt((float)num * 0.02f);
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, num2, "PROCESSING ROOT MAP OBJECT");
		}
		objectsProcessedForLoadingScene = 0;
		objectsProcessedThisFrame = 0;
		if (useProgressCallback)
		{
			num2 += Mathf.RoundToInt((float)num * 0.03f);
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, num2, "PROCESSING CHILD OBJECTS");
		}
		int processChildrenEndingProgress = endingProgress - Mathf.RoundToInt((float)num * 0.02f);
		initializePhaseTwoComponents.Clear();
		entitiesToCreate.Clear();
		yield return ProcessChildObjects(sceneDescriptor.gameObject, useProgressCallback, num2, processChildrenEndingProgress);
		if (shouldAbortSceneLoad || errorEncounteredDuringLoad)
		{
			yield break;
		}
		if (useProgressCallback)
		{
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, processChildrenEndingProgress, "PROCESSING COMPLETE");
		}
		yield return null;
		InitializeComponentsPhaseTwo();
		placeholderReplacements.Clear();
		if (useProgressCallback)
		{
			mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, endingProgress, "PROCESSING COMPLETE");
		}
		if (loadedMapPackageInfo == null || loadedMapPackageInfo.customMapSupportVersion >= 3 || !sceneDescriptor.IsInitialScene)
		{
			yield break;
		}
		maxPlayersForMap = (byte)System.Math.Clamp(sceneDescriptor.MaxPlayers, 1, 20);
		cachedLuauScript = ((sceneDescriptor.CustomGamemode != null) ? sceneDescriptor.CustomGamemode.text : "");
		devModeEnabled = sceneDescriptor.DevMode;
		disableHoldingHandsAllModes = sceneDescriptor.DisableHoldingHandsAllGameModes;
		disableHoldingHandsCustomMode = sceneDescriptor.DisableHoldingHandsCustomOnly;
		if (sceneDescriptor.UseUberShaderDynamicLighting)
		{
			GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: true);
			GameLightingManager.instance.SetAmbientLightDynamic(sceneDescriptor.UberShaderAmbientDynamicLight);
			usingDynamicLighting = true;
		}
		List<int> list = new List<int>();
		foreach (GameModeType availableModesForOldMap in instance.availableModesForOldMaps)
		{
			list.Add((int)availableModesForOldMap);
		}
		GameModeType gameModeType = instance.defaultGameModeForNonCustomOldMaps;
		if (!cachedLuauScript.IsNullOrEmpty())
		{
			gameModeType = GameModeType.Custom;
			list.Add(7);
		}
		CustomMapModeSelector.SetAvailableGameModes(list.ToArray(), (int)gameModeType);
		if (RoomSystem.JoinedRoom && NetworkSystem.Instance.LocalPlayer.IsMasterClient && NetworkSystem.Instance.SessionIsPrivate)
		{
			if (GameMode.ActiveGameMode.IsNull())
			{
				GameMode.ChangeGameMode(gameModeType.ToString());
			}
			else if (GameMode.ActiveGameMode.GameType() != gameModeType)
			{
				GameMode.ChangeGameMode(gameModeType.ToString());
			}
		}
		VirtualStumpReturnWatch.SetWatchProperties(sceneDescriptor.GetReturnToVStumpWatchProps());
	}

	private static IEnumerator ProcessChildObjects(GameObject parent, bool useProgressCallback = false, int startingProgress = 75, int endingProgress = 90)
	{
		if (parent == null || placeholderReplacements.Contains(parent))
		{
			yield break;
		}
		int progressAmount = endingProgress - startingProgress;
		for (int i = 0; i < parent.transform.childCount; i++)
		{
			Transform child = parent.transform.GetChild(i);
			if (child == null)
			{
				continue;
			}
			GameObject gameObject = child.gameObject;
			if (gameObject == null || placeholderReplacements.Contains(gameObject))
			{
				continue;
			}
			try
			{
				InitializeComponentsPhaseOne(gameObject);
			}
			catch (Exception ex)
			{
				errorEncounteredDuringLoad = true;
				cachedExceptionMessage = ex.ToString();
				Debug.LogError("[CML.LoadMap] Exception: " + ex.ToString());
				break;
			}
			if (gameObject.transform.childCount > 0)
			{
				yield return ProcessChildObjects(gameObject, useProgressCallback, startingProgress, endingProgress);
				if (shouldAbortSceneLoad || errorEncounteredDuringLoad)
				{
					break;
				}
			}
			if (shouldAbortSceneLoad)
			{
				break;
			}
			objectsProcessedForLoadingScene++;
			objectsProcessedThisFrame++;
			if (objectsProcessedThisFrame >= numObjectsToProcessPerFrame)
			{
				objectsProcessedThisFrame = 0;
				if (useProgressCallback)
				{
					float num = (float)objectsProcessedForLoadingScene / (float)totalObjectsInLoadingScene;
					int arg = startingProgress + Mathf.FloorToInt((float)progressAmount * num);
					mapLoadProgressCallback?.Invoke(MapLoadStatus.Loading, arg, "PROCESSING CHILD OBJECTS");
				}
				yield return null;
			}
		}
	}

	private static void InitializeComponentsPhaseOne(GameObject childGameObject)
	{
		SetupCollisions(childGameObject);
		ReplaceDataOnlyScripts(childGameObject);
		ReplacePlaceholders(childGameObject);
		SetupDynamicLight(childGameObject);
		StoreMapEntity(childGameObject);
		SetupReviveStation(childGameObject);
	}

	private static void InitializeComponentsPhaseTwo()
	{
		for (int i = 0; i < initializePhaseTwoComponents.Count; i++)
		{
		}
		initializePhaseTwoComponents.Clear();
		if (entitiesToCreate.Count > 0)
		{
			for (int j = 0; j < entitiesToCreate.Count; j++)
			{
				entitiesToCreate[j].gameObject.SetActive(value: false);
			}
			CustomMapsGameManager.AddAgentsToCreate(entitiesToCreate);
		}
	}

	private static void SetupReviveStation(GameObject gameObject)
	{
		if (gameObject == null)
		{
			return;
		}
		CustomMapReviveStation component = gameObject.GetComponent<CustomMapReviveStation>();
		if (component == null)
		{
			return;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(instance.reviveStationPrefab, gameObject.transform.parent);
		if (gameObject2 == null)
		{
			return;
		}
		gameObject2.transform.position = gameObject.transform.position;
		gameObject2.transform.rotation = gameObject.transform.rotation;
		gameObject.transform.SetParent(gameObject2.transform);
		GRReviveStation component2 = gameObject2.GetComponent<GRReviveStation>();
		if (component2 == null)
		{
			return;
		}
		component2.audioSource = component.audioSource;
		if (!component.particleEffects.IsNullOrEmpty())
		{
			component2.particleEffects = new ParticleSystem[component.particleEffects.Length];
			for (int i = 0; i < component.particleEffects.Length; i++)
			{
				component2.particleEffects[i] = component.particleEffects[i];
			}
		}
		component2.SetReviveCooldownSeconds(component.reviveCooldownSeconds);
		refreshReviveStations = true;
	}

	private static void SetupCollisions(GameObject gameObject)
	{
		if (gameObject == null || placeholderReplacements.Contains(gameObject))
		{
			return;
		}
		Collider[] components = gameObject.GetComponents<Collider>();
		if (components == null)
		{
			return;
		}
		bool flag = true;
		Collider[] array = components;
		foreach (Collider collider in array)
		{
			if (collider == null)
			{
				continue;
			}
			if (collider.isTrigger)
			{
				if (gameObject.layer != UnityLayer.GorillaInteractable.ToLayerIndex())
				{
					gameObject.layer = UnityLayer.GorillaTrigger.ToLayerIndex();
					break;
				}
				continue;
			}
			if (gameObject.layer == UnityLayer.GorillaTrigger.ToLayerIndex())
			{
				collider.isTrigger = true;
			}
			flag = false;
			if (gameObject.GetComponent<GrabbableEntity>().IsNotNull())
			{
				gameObject.layer = UnityLayer.Default.ToLayerIndex();
				return;
			}
		}
		if (!flag)
		{
			SurfaceOverrideSettings component = gameObject.GetComponent<SurfaceOverrideSettings>();
			GorillaSurfaceOverride gorillaSurfaceOverride = gameObject.AddComponent<GorillaSurfaceOverride>();
			if (component == null)
			{
				gorillaSurfaceOverride.overrideIndex = 0;
				return;
			}
			gorillaSurfaceOverride.overrideIndex = (int)component.soundOverride;
			gorillaSurfaceOverride.extraVelMultiplier = component.extraVelMultiplier;
			gorillaSurfaceOverride.extraVelMaxMultiplier = component.extraVelMaxMultiplier;
			gorillaSurfaceOverride.slidePercentageOverride = component.slidePercentage;
			gorillaSurfaceOverride.disablePushBackEffect = component.disablePushBackEffect;
			UnityEngine.Object.Destroy(component);
		}
	}

	private static bool ValidateTeleporterDestination(Transform teleportTarget)
	{
		foreach (GameObject storeCheckout in storeCheckouts)
		{
			if (Vector3.Distance(storeCheckout.transform.position, teleportTarget.position) < GT_CustomMapSupportRuntime.Constants.minTeleportDistFromStorePlaceholder)
			{
				return false;
			}
		}
		foreach (GameObject storeDisplayStand in storeDisplayStands)
		{
			if (Vector3.Distance(storeDisplayStand.transform.position, teleportTarget.position) < GT_CustomMapSupportRuntime.Constants.minTeleportDistFromStorePlaceholder)
			{
				return false;
			}
		}
		if (customMapATM.IsNotNull() && Vector3.Distance(customMapATM.transform.position, teleportTarget.position) < GT_CustomMapSupportRuntime.Constants.minTeleportDistFromStorePlaceholder)
		{
			return false;
		}
		return true;
	}

	private static bool ValidateStorePlaceholderPosition(GameObject storePlaceholder)
	{
		foreach (Component teleporter in teleporters)
		{
			if (teleporter == null)
			{
				continue;
			}
			List<Transform> list = null;
			if (teleporter.GetType() == typeof(CMSMapBoundary))
			{
				CMSMapBoundary cMSMapBoundary = (CMSMapBoundary)teleporter;
				if (cMSMapBoundary != null)
				{
					list = cMSMapBoundary.TeleportPoints;
				}
			}
			else if (teleporter.GetType() == typeof(CMSTeleporter))
			{
				CMSTeleporter cMSTeleporter = (CMSTeleporter)teleporter;
				if (cMSTeleporter != null)
				{
					list = cMSTeleporter.TeleportPoints;
				}
			}
			if (list == null)
			{
				continue;
			}
			for (int i = 0; i < list.Count; i++)
			{
				Transform transform = list[i];
				if (Vector3.Distance(storePlaceholder.transform.position, transform.position) < GT_CustomMapSupportRuntime.Constants.minTeleportDistFromStorePlaceholder)
				{
					return false;
				}
			}
		}
		return true;
	}

	private static void ReplaceDataOnlyScripts(GameObject gameObject)
	{
		MapBoundarySettings[] components = gameObject.GetComponents<MapBoundarySettings>();
		if (components != null)
		{
			MapBoundarySettings[] array = components;
			foreach (MapBoundarySettings mapBoundarySettings in array)
			{
				bool flag = false;
				for (int j = 0; j < mapBoundarySettings.TeleportPoints.Count; j++)
				{
					if (!mapBoundarySettings.TeleportPoints[j].IsNull() && !ValidateTeleporterDestination(mapBoundarySettings.TeleportPoints[j]))
					{
						flag = true;
						UnityEngine.Object.Destroy(mapBoundarySettings);
						break;
					}
				}
				if (!flag)
				{
					CMSMapBoundary cMSMapBoundary = gameObject.AddComponent<CMSMapBoundary>();
					if (cMSMapBoundary != null)
					{
						cMSMapBoundary.CopyTriggerSettings(mapBoundarySettings);
						teleporters.Add(cMSMapBoundary);
					}
					UnityEngine.Object.Destroy(mapBoundarySettings);
				}
			}
		}
		TagZoneSettings[] components2 = gameObject.GetComponents<TagZoneSettings>();
		if (components2 != null)
		{
			TagZoneSettings[] array2 = components2;
			foreach (TagZoneSettings tagZoneSettings in array2)
			{
				CMSTagZone cMSTagZone = gameObject.AddComponent<CMSTagZone>();
				if (cMSTagZone != null)
				{
					cMSTagZone.CopyTriggerSettings(tagZoneSettings);
				}
				UnityEngine.Object.Destroy(tagZoneSettings);
			}
		}
		TeleporterSettings[] components3 = gameObject.GetComponents<TeleporterSettings>();
		if (components3 != null)
		{
			TeleporterSettings[] array3 = components3;
			foreach (TeleporterSettings teleporterSettings in array3)
			{
				bool flag2 = false;
				for (int k = 0; k < teleporterSettings.TeleportPoints.Count; k++)
				{
					if (!teleporterSettings.TeleportPoints[k].IsNull() && !ValidateTeleporterDestination(teleporterSettings.TeleportPoints[k]))
					{
						flag2 = true;
						UnityEngine.Object.Destroy(teleporterSettings);
						break;
					}
				}
				if (!flag2)
				{
					CMSTeleporter cMSTeleporter = gameObject.AddComponent<CMSTeleporter>();
					if (cMSTeleporter != null)
					{
						cMSTeleporter.CopyTriggerSettings(teleporterSettings);
					}
					UnityEngine.Object.Destroy(teleporterSettings);
				}
			}
		}
		ObjectActivationTriggerSettings[] components4 = gameObject.GetComponents<ObjectActivationTriggerSettings>();
		if (components4 != null)
		{
			ObjectActivationTriggerSettings[] array4 = components4;
			foreach (ObjectActivationTriggerSettings objectActivationTriggerSettings in array4)
			{
				CMSObjectActivationTrigger cMSObjectActivationTrigger = gameObject.AddComponent<CMSObjectActivationTrigger>();
				if (cMSObjectActivationTrigger != null)
				{
					cMSObjectActivationTrigger.CopyTriggerSettings(objectActivationTriggerSettings);
				}
				UnityEngine.Object.Destroy(objectActivationTriggerSettings);
			}
		}
		LuauTriggerSettings[] components5 = gameObject.GetComponents<LuauTriggerSettings>();
		if (components5 != null)
		{
			LuauTriggerSettings[] array5 = components5;
			foreach (LuauTriggerSettings luauTriggerSettings in array5)
			{
				CMSLuau cMSLuau = gameObject.AddComponent<CMSLuau>();
				if (cMSLuau != null)
				{
					cMSLuau.CopyTriggerSettings(luauTriggerSettings);
				}
				UnityEngine.Object.Destroy(luauTriggerSettings);
			}
		}
		PlayAnimationTriggerSettings[] components6 = gameObject.GetComponents<PlayAnimationTriggerSettings>();
		if (components6 != null)
		{
			PlayAnimationTriggerSettings[] array6 = components6;
			foreach (PlayAnimationTriggerSettings playAnimationTriggerSettings in array6)
			{
				CMSPlayAnimationTrigger cMSPlayAnimationTrigger = gameObject.AddComponent<CMSPlayAnimationTrigger>();
				if (cMSPlayAnimationTrigger != null)
				{
					cMSPlayAnimationTrigger.CopyTriggerSettings(playAnimationTriggerSettings);
				}
				UnityEngine.Object.Destroy(playAnimationTriggerSettings);
			}
		}
		LoadZoneSettings[] components7 = gameObject.GetComponents<LoadZoneSettings>();
		if (components7 != null)
		{
			LoadZoneSettings[] array7 = components7;
			foreach (LoadZoneSettings loadZoneSettings in array7)
			{
				CMSLoadingZone cMSLoadingZone = gameObject.AddComponent<CMSLoadingZone>();
				if (cMSLoadingZone != null)
				{
					cMSLoadingZone.SetupLoadingZone(loadZoneSettings, in assetBundleSceneFilePaths);
				}
				UnityEngine.Object.Destroy(loadZoneSettings);
			}
		}
		ZoneShaderTriggerSettings[] components8 = gameObject.GetComponents<ZoneShaderTriggerSettings>();
		if (components8 != null)
		{
			ZoneShaderTriggerSettings[] array8 = components8;
			foreach (ZoneShaderTriggerSettings zoneShaderTriggerSettings in array8)
			{
				gameObject.AddComponent<CMSZoneShaderSettingsTrigger>().CopySettings(zoneShaderTriggerSettings);
				UnityEngine.Object.Destroy(zoneShaderTriggerSettings);
			}
		}
		CMSZoneShaderSettings component = gameObject.GetComponent<CMSZoneShaderSettings>();
		if (component.IsNotNull())
		{
			ZoneShaderSettings zoneShaderSettings = gameObject.AddComponent<ZoneShaderSettings>();
			zoneShaderSettings.CopySettings(component);
			if (component.isDefaultValues)
			{
				CustomMapManager.SetDefaultZoneShaderSettings(zoneShaderSettings, component.GetProperties());
			}
			CustomMapManager.AddZoneShaderSettings(zoneShaderSettings);
			UnityEngine.Object.Destroy(component);
		}
		HandHoldSettings component2 = gameObject.GetComponent<HandHoldSettings>();
		if (component2.IsNotNull())
		{
			gameObject.AddComponent<HandHold>().CopyProperties(component2);
			UnityEngine.Object.Destroy(component2);
		}
		CustomMapEjectButtonSettings component3 = gameObject.GetComponent<CustomMapEjectButtonSettings>();
		if (component3.IsNotNull())
		{
			CustomMapEjectButton customMapEjectButton = gameObject.AddComponent<CustomMapEjectButton>();
			customMapEjectButton.gameObject.layer = UnityLayer.GorillaInteractable.ToLayerIndex();
			customMapEjectButton.CopySettings(component3);
			UnityEngine.Object.Destroy(component3);
		}
		MovingSurfaceSettings component4 = gameObject.GetComponent<MovingSurfaceSettings>();
		if (component4.IsNotNull())
		{
			MovingSurface movingSurface = gameObject.AddComponent<MovingSurface>();
			if (movingSurface.IsNotNull())
			{
				movingSurface.CopySettings(component4);
				UnityEngine.Object.Destroy(component4);
			}
		}
		SurfaceMoverSettings component5 = gameObject.GetComponent<SurfaceMoverSettings>();
		if (component5.IsNotNull())
		{
			gameObject.AddComponent<SurfaceMover>().CopySettings(component5);
			UnityEngine.Object.Destroy(component5);
		}
	}

	private static void ReplacePlaceholders(GameObject placeholderGameObject)
	{
		if (placeholderGameObject.IsNull())
		{
			return;
		}
		GTObjectPlaceholder component = placeholderGameObject.GetComponent<GTObjectPlaceholder>();
		if (component.IsNull())
		{
			return;
		}
		List<Collider> list = null;
		switch (component.PlaceholderObject)
		{
		case GTObject.LeafGlider:
			if (leafGliderIndex < instance.leafGliders.Length)
			{
				instance.leafGliders[leafGliderIndex].enabled = true;
				instance.leafGliders[leafGliderIndex].CustomMapLoad(component.transform, component.maxDistanceBeforeRespawn);
				instance.leafGliders[leafGliderIndex].transform.GetChild(0).gameObject.SetActive(value: true);
				leafGliderIndex++;
			}
			break;
		case GTObject.GliderWindVolume:
			list = new List<Collider>(component.GetComponents<Collider>());
			if (component.useDefaultPlaceholder || list.Count == 0)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(instance.gliderWindVolume, placeholderGameObject.transform.position, placeholderGameObject.transform.rotation);
				if (gameObject2 != null)
				{
					placeholderReplacements.Add(gameObject2);
					gameObject2.transform.localScale = placeholderGameObject.transform.localScale;
					placeholderGameObject.transform.localScale = Vector3.one;
					gameObject2.transform.SetParent(placeholderGameObject.transform);
					GliderWindVolume component3 = gameObject2.GetComponent<GliderWindVolume>();
					if (!(component3 == null))
					{
						component3.SetProperties(component.maxSpeed, component.maxAccel, component.SpeedVSAccelCurve, component.localWindDirection);
					}
				}
			}
			else
			{
				placeholderGameObject.layer = UnityLayer.GorillaTrigger.ToLayerIndex();
				GliderWindVolume gliderWindVolume = placeholderGameObject.AddComponent<GliderWindVolume>();
				if (gliderWindVolume.IsNotNull())
				{
					gliderWindVolume.SetProperties(component.maxSpeed, component.maxAccel, component.SpeedVSAccelCurve, component.localWindDirection);
				}
			}
			break;
		case GTObject.WaterVolume:
		{
			list = new List<Collider>(component.GetComponents<Collider>());
			if (component.useDefaultPlaceholder || list.Count == 0)
			{
				GameObject gameObject9 = UnityEngine.Object.Instantiate(instance.waterVolumePrefab, placeholderGameObject.transform.position, placeholderGameObject.transform.rotation);
				if (!(gameObject9 != null))
				{
					break;
				}
				placeholderReplacements.Add(gameObject9);
				gameObject9.layer = UnityLayer.Water.ToLayerIndex();
				gameObject9.transform.localScale = placeholderGameObject.transform.localScale;
				placeholderGameObject.transform.localScale = Vector3.one;
				gameObject9.transform.SetParent(placeholderGameObject.transform);
				MeshRenderer component7 = gameObject9.GetComponent<MeshRenderer>();
				if (component7.IsNull())
				{
					break;
				}
				if (component.useWaterMesh)
				{
					component7.enabled = true;
					WaterSurfaceMaterialController component8 = gameObject9.GetComponent<WaterSurfaceMaterialController>();
					if (!component8.IsNull())
					{
						component8.ScrollX = component.scrollTextureX;
						component8.ScrollY = component.scrollTextureY;
						component8.Scale = component.scaleTexture;
					}
				}
				else
				{
					component7.enabled = false;
				}
				break;
			}
			placeholderGameObject.layer = UnityLayer.Water.ToLayerIndex();
			WaterVolume waterVolume = placeholderGameObject.AddComponent<WaterVolume>();
			if (waterVolume.IsNotNull())
			{
				WaterParameters parameters = null;
				switch (component.liquidType)
				{
				case CMSZoneShaderSettings.EZoneLiquidType.Water:
					parameters = instance.defaultWaterParameters;
					break;
				case CMSZoneShaderSettings.EZoneLiquidType.Lava:
					parameters = instance.defaultLavaParameters;
					break;
				}
				waterVolume.SetPropertiesFromPlaceholder(component.GetWaterVolumeProperties(), list, parameters);
				waterVolume.RefreshColliders();
			}
			break;
		}
		case GTObject.ForceVolume:
		{
			list = new List<Collider>(component.GetComponents<Collider>());
			if (component.useDefaultPlaceholder || list.Count == 0)
			{
				GameObject gameObject12 = UnityEngine.Object.Instantiate(instance.forceVolumePrefab, placeholderGameObject.transform.position, placeholderGameObject.transform.rotation);
				if (gameObject12.IsNotNull())
				{
					placeholderReplacements.Add(gameObject12);
					gameObject12.transform.localScale = placeholderGameObject.transform.localScale;
					placeholderGameObject.transform.localScale = Vector3.one;
					gameObject12.transform.SetParent(placeholderGameObject.transform);
					ForceVolume component9 = gameObject12.GetComponent<ForceVolume>();
					if (!component9.IsNull())
					{
						component9.SetPropertiesFromPlaceholder(component.GetForceVolumeProperties(), null, null);
					}
				}
				break;
			}
			ForceVolume forceVolume = placeholderGameObject.AddComponent<ForceVolume>();
			if (forceVolume.IsNotNull())
			{
				AudioSource audioSource = placeholderGameObject.GetComponent<AudioSource>();
				if (audioSource.IsNull())
				{
					audioSource = placeholderGameObject.AddComponent<AudioSource>();
					audioSource.spatialize = true;
					audioSource.playOnAwake = false;
					audioSource.priority = 128;
					audioSource.volume = 0.522f;
					audioSource.pitch = 1f;
					audioSource.panStereo = 0f;
					audioSource.spatialBlend = 1f;
					audioSource.reverbZoneMix = 1f;
					audioSource.dopplerLevel = 1f;
					audioSource.spread = 0f;
					audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
					audioSource.minDistance = 8.2f;
					audioSource.maxDistance = 43.94f;
					audioSource.enabled = true;
				}
				audioSource.outputAudioMixerGroup = instance.masterAudioMixer;
				for (int num2 = list.Count - 1; num2 >= 0; num2--)
				{
					if (num2 == 0)
					{
						list[num2].isTrigger = true;
					}
					else
					{
						UnityEngine.Object.Destroy(list[num2]);
					}
				}
				placeholderGameObject.layer = UnityLayer.GorillaBoundary.ToLayerIndex();
				forceVolume.SetPropertiesFromPlaceholder(component.GetForceVolumeProperties(), audioSource, component.GetComponent<Collider>());
			}
			else
			{
				Debug.LogError("[CustomMapLoader::ReplacePlaceholders] Failed to add ForceVolume component to Placeholder!");
			}
			break;
		}
		case GTObject.ATM:
		{
			if (customMapATM.IsNotNull())
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			if (!ValidateStorePlaceholderPosition(placeholderGameObject))
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			GameObject gameObject4 = instance.atmPrefab;
			if (component.useCustomMesh)
			{
				gameObject4 = instance.atmNoShellPrefab;
			}
			if (gameObject4.IsNull())
			{
				break;
			}
			GameObject gameObject5 = UnityEngine.Object.Instantiate(gameObject4, placeholderGameObject.transform.position, placeholderGameObject.transform.rotation);
			if (!gameObject5.IsNotNull())
			{
				break;
			}
			gameObject5.transform.SetParent(instance.compositeTryOnArea.transform, worldPositionStays: true);
			gameObject5.transform.localScale = Vector3.one;
			ATM_UI componentInChildren = gameObject5.GetComponentInChildren<ATM_UI>();
			if (componentInChildren.IsNotNull() && ATM_Manager.instance.IsNotNull())
			{
				componentInChildren.SetCustomMapScene(placeholderGameObject.scene);
				customMapATM = gameObject5;
				ATM_Manager.instance.AddATM(componentInChildren);
				if (!component.defaultCreatorCode.IsNullOrEmpty())
				{
					ATM_Manager.instance.SetTemporaryCreatorCode(component.defaultCreatorCode);
				}
			}
			break;
		}
		case GTObject.HoverboardArea:
		{
			if (!component.AddComponent<HoverboardAreaTrigger>().IsNotNull())
			{
				break;
			}
			component.gameObject.layer = UnityLayer.GorillaBoundary.ToLayerIndex();
			list = new List<Collider>(component.GetComponents<Collider>());
			if (list.Count == 0)
			{
				BoxCollider boxCollider = component.AddComponent<BoxCollider>();
				if (boxCollider.IsNotNull())
				{
					boxCollider.isTrigger = true;
				}
				break;
			}
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (num == 0)
				{
					list[num].isTrigger = true;
				}
				else
				{
					UnityEngine.Object.Destroy(list[num]);
				}
			}
			break;
		}
		case GTObject.HoverboardDispenser:
		{
			if (instance.hoverboardDispenserPrefab.IsNull())
			{
				Debug.LogError("[CustomMapLoader::ReplacePlaceholders] hoverboardDispenserPrefab is NULL!");
				break;
			}
			GameObject gameObject3 = UnityEngine.Object.Instantiate(instance.hoverboardDispenserPrefab, placeholderGameObject.transform.position, placeholderGameObject.transform.rotation);
			if (gameObject3.IsNotNull())
			{
				placeholderReplacements.Add(gameObject3);
				gameObject3.transform.SetParent(placeholderGameObject.transform);
			}
			break;
		}
		case GTObject.RopeSwing:
		{
			GameObject gameObject7 = UnityEngine.Object.Instantiate(instance.ropeSwingPrefab, placeholderGameObject.transform.position, placeholderGameObject.transform.rotation);
			if (gameObject7.IsNull())
			{
				break;
			}
			gameObject7.transform.SetParent(placeholderGameObject.transform);
			CustomMapsGorillaRopeSwing component5 = gameObject7.GetComponent<CustomMapsGorillaRopeSwing>();
			if (component5.IsNull())
			{
				UnityEngine.Object.DestroyImmediate(gameObject7);
				break;
			}
			component.ropeLength = System.Math.Clamp(component.ropeLength, 3, 31);
			if (component.useDefaultPlaceholder)
			{
				component5.SetRopeLength(component.ropeLength);
			}
			else
			{
				component5.SetRopeProperties(component);
			}
			placeholderReplacements.Add(gameObject7);
			break;
		}
		case GTObject.ZipLine:
		{
			GameObject gameObject8 = UnityEngine.Object.Instantiate(instance.ziplinePrefab, placeholderGameObject.transform.position, placeholderGameObject.transform.rotation);
			if (gameObject8.IsNull())
			{
				break;
			}
			gameObject8.transform.SetParent(placeholderGameObject.transform);
			CustomMapsGorillaZipline component6 = gameObject8.GetComponent<CustomMapsGorillaZipline>();
			if (component6.IsNull())
			{
				UnityEngine.Object.DestroyImmediate(gameObject8);
				break;
			}
			if (component.useDefaultPlaceholder)
			{
				if (!component6.GenerateZipline(component.spline))
				{
					UnityEngine.Object.DestroyImmediate(gameObject8);
					break;
				}
			}
			else
			{
				component6.Init(component);
			}
			placeholderReplacements.Add(gameObject8);
			break;
		}
		case GTObject.Store_DisplayStand:
		{
			if (instance.storeDisplayStandPrefab.IsNull())
			{
				break;
			}
			if (storeDisplayStands.Count >= GT_CustomMapSupportRuntime.Constants.storeDisplayStandLimit)
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			if (placeholderGameObject.transform.lossyScale != Vector3.one)
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			if (!ValidateStorePlaceholderPosition(placeholderGameObject))
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			GameObject gameObject6 = UnityEngine.Object.Instantiate(instance.storeDisplayStandPrefab, placeholderGameObject.transform);
			if (!gameObject6.IsNull())
			{
				gameObject6.transform.SetParent(instance.compositeTryOnArea.transform, worldPositionStays: true);
				gameObject6.transform.localScale = Vector3.one;
				DynamicCosmeticStand component4 = gameObject6.GetComponent<DynamicCosmeticStand>();
				if (component4.IsNull())
				{
					UnityEngine.Object.DestroyImmediate(gameObject6);
					break;
				}
				component4.InitializeForCustomMapCosmeticItem(component.CosmeticItem, placeholderGameObject.scene);
				storeDisplayStands.Add(gameObject6);
				placeholderReplacements.Add(gameObject6);
			}
			break;
		}
		case GTObject.Store_Checkout:
		{
			if (instance.storeCheckoutCounterPrefab.IsNull())
			{
				break;
			}
			if (storeCheckouts.Count >= GT_CustomMapSupportRuntime.Constants.storeCheckoutCounterLimit)
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			if (placeholderGameObject.transform.lossyScale != Vector3.one)
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			if (!ValidateStorePlaceholderPosition(placeholderGameObject))
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			GameObject gameObject10 = UnityEngine.Object.Instantiate(instance.storeCheckoutCounterPrefab, placeholderGameObject.transform);
			if (!gameObject10.IsNull())
			{
				gameObject10.transform.SetParent(instance.compositeTryOnArea.transform);
				gameObject10.transform.localScale = Vector3.one;
				ItemCheckout componentInChildren2 = gameObject10.GetComponentInChildren<ItemCheckout>();
				if (componentInChildren2.IsNull())
				{
					UnityEngine.Object.DestroyImmediate(gameObject10);
					break;
				}
				componentInChildren2.InitializeForCustomMap(instance.compositeTryOnArea, placeholderGameObject.scene, component.useCustomMesh);
				storeCheckouts.Add(gameObject10);
				placeholderReplacements.Add(gameObject10);
			}
			break;
		}
		case GTObject.Store_TryOnConsole:
		{
			if (instance.storeTryOnConsolePrefab.IsNull())
			{
				break;
			}
			if (storeTryOnConsoles.Count >= GT_CustomMapSupportRuntime.Constants.storeTryOnConsoleLimit)
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			GameObject gameObject11 = UnityEngine.Object.Instantiate(instance.storeTryOnConsolePrefab, placeholderGameObject.transform);
			if (!gameObject11.IsNull())
			{
				FittingRoom componentInChildren3 = gameObject11.GetComponentInChildren<FittingRoom>();
				if (componentInChildren3.IsNull())
				{
					UnityEngine.Object.DestroyImmediate(gameObject11);
					break;
				}
				componentInChildren3.InitializeForCustomMap(component.useCustomMesh);
				storeTryOnConsoles.Add(gameObject11);
				placeholderReplacements.Add(gameObject11);
			}
			break;
		}
		case GTObject.Store_TryOnArea:
		{
			if (instance.storeTryOnAreaPrefab.IsNull() || instance.compositeTryOnArea.IsNull())
			{
				break;
			}
			if (storeTryOnAreas.Count >= GT_CustomMapSupportRuntime.Constants.storeTryOnAreaLimit)
			{
				UnityEngine.Object.Destroy(component);
				break;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(instance.storeTryOnAreaPrefab, placeholderGameObject.transform);
			gameObject.transform.SetParent(instance.compositeTryOnArea.transform);
			CMSTryOnArea component2 = gameObject.GetComponent<CMSTryOnArea>();
			if (component2.IsNull() || component2.tryOnAreaCollider.IsNull())
			{
				UnityEngine.Object.DestroyImmediate(gameObject);
				break;
			}
			BoxCollider tryOnAreaCollider = component2.tryOnAreaCollider;
			Vector3 zero = Vector3.zero;
			zero.x = tryOnAreaCollider.size.x * tryOnAreaCollider.transform.lossyScale.x;
			zero.y = tryOnAreaCollider.size.y * tryOnAreaCollider.transform.lossyScale.y;
			zero.z = tryOnAreaCollider.size.z * tryOnAreaCollider.transform.lossyScale.z;
			if (System.Math.Abs(zero.x * zero.y * zero.z) > GT_CustomMapSupportRuntime.Constants.storeTryOnAreaVolumeLimit)
			{
				UnityEngine.Object.DestroyImmediate(gameObject);
				break;
			}
			component2.InitializeForCustomMap(instance.compositeTryOnArea, placeholderGameObject.scene);
			storeTryOnAreas.Add(gameObject);
			placeholderReplacements.Add(gameObject);
			break;
		}
		}
	}

	private static void SetupDynamicLight(GameObject dynamicLightGameObject)
	{
		if (!dynamicLightGameObject.IsNull())
		{
			UberShaderDynamicLight component = dynamicLightGameObject.GetComponent<UberShaderDynamicLight>();
			if (!component.IsNull() && !component.dynamicLight.IsNull())
			{
				GameObject obj = new GameObject(dynamicLightGameObject.name + "GameLight");
				GameLight gameLight = obj.AddComponent<GameLight>();
				gameLight.light = component.dynamicLight;
				GameLightingManager.instance.AddGameLight(gameLight);
				obj.transform.SetParent(dynamicLightGameObject.transform.parent);
				obj.transform.position = component.transform.position;
			}
		}
	}

	private static void StoreMapEntity(GameObject entityGameObject)
	{
		if (entityGameObject.IsNull() || CustomMapsGameManager.instance.IsNull())
		{
			return;
		}
		MapEntity component = entityGameObject.GetComponent<MapEntity>();
		if (component.IsNull())
		{
			return;
		}
		if (component is AIAgent)
		{
			AIAgent aIAgent = (AIAgent)component;
			if (!aIAgent.IsNull())
			{
				_ = $" | AgentID: {aIAgent.enemyTypeId}";
			}
		}
		if (!component.isTemplate)
		{
			entitiesToCreate.Add(component);
		}
	}

	private static void CacheLightmaps()
	{
		lightmaps = new LightmapData[LightmapSettings.lightmaps.Length];
		if (lightmapsToKeep.Count > 0)
		{
			lightmapsToKeep.Clear();
		}
		lightmapsToKeep = new List<Texture2D>(LightmapSettings.lightmaps.Length * 2);
		for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
		{
			lightmaps[i] = LightmapSettings.lightmaps[i];
			if (LightmapSettings.lightmaps[i].lightmapColor != null)
			{
				lightmapsToKeep.Add(LightmapSettings.lightmaps[i].lightmapColor);
			}
			if (LightmapSettings.lightmaps[i].lightmapDir != null)
			{
				lightmapsToKeep.Add(LightmapSettings.lightmaps[i].lightmapDir);
			}
		}
	}

	private static void LoadLightmaps(Texture2D[] colorMaps, Texture2D[] dirMaps)
	{
		if (colorMaps.Length == 0)
		{
			return;
		}
		UnloadLightmaps();
		List<LightmapData> list = new List<LightmapData>(LightmapSettings.lightmaps);
		for (int i = 0; i < colorMaps.Length; i++)
		{
			bool flag = false;
			LightmapData lightmapData = new LightmapData();
			if (colorMaps[i] != null)
			{
				lightmapData.lightmapColor = colorMaps[i];
				flag = true;
				if (i < dirMaps.Length && dirMaps[i] != null)
				{
					lightmapData.lightmapDir = dirMaps[i];
				}
			}
			if (flag)
			{
				list.Add(lightmapData);
			}
		}
		LightmapSettings.lightmaps = list.ToArray();
	}

	public static void ResetToInitialZone(Action<string> onSceneLoaded, Action<string> onSceneUnloaded)
	{
		List<int> list = new List<int>(initialSceneIndexes);
		List<int> list2 = new List<int>(loadedSceneIndexes);
		foreach (int loadedSceneIndex in loadedSceneIndexes)
		{
			if (initialSceneIndexes.Contains(loadedSceneIndex))
			{
				list2.Remove(loadedSceneIndex);
				list.Remove(loadedSceneIndex);
			}
		}
		if (loadedMapPackageInfo.customMapSupportVersion <= 2 && loadedSceneIndexes.Contains(initialSceneIndexes[0]))
		{
			MapDescriptor[] array = UnityEngine.Object.FindObjectsByType<MapDescriptor>(FindObjectsSortMode.None);
			bool flag = false;
			int num = 0;
			for (num = 0; num < array.Length; num++)
			{
				if (array[num].IsInitialScene && array[num].UseUberShaderDynamicLighting)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: true);
				GameLightingManager.instance.SetAmbientLightDynamic(array[num].UberShaderAmbientDynamicLight);
				usingDynamicLighting = true;
			}
			else
			{
				GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: false);
				GameLightingManager.instance.SetAmbientLightDynamic(Color.black);
				usingDynamicLighting = false;
			}
		}
		else if (loadedMapPackageInfo.customMapSupportVersion > 2)
		{
			if (loadedMapPackageInfo.useUberShaderDynamicLighting)
			{
				Color ambientLightDynamic = new Color(loadedMapPackageInfo.uberShaderAmbientDynamicLight_R, loadedMapPackageInfo.uberShaderAmbientDynamicLight_G, loadedMapPackageInfo.uberShaderAmbientDynamicLight_B, loadedMapPackageInfo.uberShaderAmbientDynamicLight_A);
				GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: true);
				GameLightingManager.instance.SetAmbientLightDynamic(ambientLightDynamic);
				usingDynamicLighting = true;
			}
			else
			{
				GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: false);
				GameLightingManager.instance.SetAmbientLightDynamic(Color.black);
				usingDynamicLighting = false;
			}
		}
		if (!list.IsNullOrEmpty() || !list2.IsNullOrEmpty())
		{
			if (zoneLoadingCoroutine != null)
			{
				LoadZoneRequest item = new LoadZoneRequest
				{
					sceneIndexesToLoad = list.ToArray(),
					sceneIndexesToUnload = list2.ToArray(),
					onSceneLoadedCallback = onSceneLoaded,
					onSceneUnloadedCallback = onSceneUnloaded
				};
				queuedLoadZoneRequests.Add(item);
			}
			else
			{
				sceneLoadedCallback = onSceneLoaded;
				sceneUnloadedCallback = onSceneUnloaded;
				zoneLoadingCoroutine = instance.StartCoroutine(LoadZoneCoroutine(list.ToArray(), list2.ToArray()));
			}
		}
	}

	public static void LoadZoneTriggered(int[] loadSceneIndexes, int[] unloadSceneIndexes, Action<string> onSceneLoaded, Action<string> onSceneUnloaded)
	{
		string text = "";
		for (int i = 0; i < loadSceneIndexes.Length; i++)
		{
			text += loadSceneIndexes[i];
			if (i != loadSceneIndexes.Length - 1)
			{
				text += ", ";
			}
		}
		string text2 = "";
		for (int j = 0; j < unloadSceneIndexes.Length; j++)
		{
			text2 += unloadSceneIndexes[j];
			if (j != unloadSceneIndexes.Length - 1)
			{
				text2 += ", ";
			}
		}
		if (zoneLoadingCoroutine != null)
		{
			LoadZoneRequest item = new LoadZoneRequest
			{
				sceneIndexesToLoad = loadSceneIndexes,
				sceneIndexesToUnload = unloadSceneIndexes,
				onSceneLoadedCallback = onSceneLoaded,
				onSceneUnloadedCallback = onSceneUnloaded
			};
			queuedLoadZoneRequests.Add(item);
		}
		else
		{
			sceneLoadedCallback = onSceneLoaded;
			sceneUnloadedCallback = onSceneUnloaded;
			zoneLoadingCoroutine = instance.StartCoroutine(LoadZoneCoroutine(loadSceneIndexes, unloadSceneIndexes));
		}
	}

	private static IEnumerator LoadZoneCoroutine(int[] loadScenes, int[] unloadScenes)
	{
		if (!unloadScenes.IsNullOrEmpty())
		{
			yield return UnloadScenesCoroutine(unloadScenes);
		}
		if (!loadScenes.IsNullOrEmpty())
		{
			yield return LoadScenesCoroutine(loadScenes, delegate(bool successfullyLoadedAllScenes, bool loadAborted, List<string> successfullyLoadedSceneNames)
			{
				if (loadAborted)
				{
					queuedLoadZoneRequests.Clear();
				}
			});
		}
		zoneLoadingCoroutine = null;
		if (queuedLoadZoneRequests.Count > 0)
		{
			LoadZoneRequest loadZoneRequest = queuedLoadZoneRequests[0];
			queuedLoadZoneRequests.RemoveAt(0);
			LoadZoneTriggered(loadZoneRequest.sceneIndexesToLoad, loadZoneRequest.sceneIndexesToUnload, loadZoneRequest.onSceneLoadedCallback, loadZoneRequest.onSceneUnloadedCallback);
		}
	}

	public static void CloseDoorAndUnloadMap(Action unloadCompleted = null)
	{
		if (IsMapLoaded() || isLoading)
		{
			if (unloadCompleted != null)
			{
				unloadMapCallback = unloadCompleted;
			}
			if (isLoading)
			{
				RequestAbortMapLoad();
			}
			else
			{
				instance.StartCoroutine(CloseDoorAndUnloadMapCoroutine());
			}
		}
	}

	private static IEnumerator CloseDoorAndUnloadMapCoroutine()
	{
		if (IsMapLoaded())
		{
			if (instance.accessDoor != null)
			{
				instance.accessDoor.CloseDoor();
			}
			if (instance.publicJoinTrigger != null)
			{
				instance.publicJoinTrigger.SetActive(value: false);
			}
			shouldAbortMapLoading = true;
			if (!IsLoading())
			{
				yield return UnloadMapCoroutine();
			}
		}
	}

	private static void RequestAbortMapLoad()
	{
		shouldAbortSceneLoad = true;
		shouldAbortMapLoading = true;
	}

	private static IEnumerator AbortMapLoad()
	{
		GTDev.Log("[CML.AbortMapLoad] Aborting map load...");
		shouldAbortSceneLoad = true;
		shouldAbortMapLoading = true;
		yield return AbortSceneLoad(-1);
		mapLoadFinishedCallback?.Invoke(obj: false);
	}

	private static IEnumerator UnloadMapCoroutine()
	{
		GTDev.Log("[CML.UnloadMap_Co] Unloading Custom Map...");
		if (zoneLoadingCoroutine != null)
		{
			queuedLoadZoneRequests.Clear();
			instance.StopCoroutine(zoneLoadingCoroutine);
			zoneLoadingCoroutine = null;
		}
		isUnloading = true;
		CanLoadEntities = false;
		CustomMapTelemetry.EndMapTracking();
		ZoneShaderSettings.ActivateDefaultSettings();
		CleanupPlaceholders();
		CMSSerializer.ResetSyncedMapObjects();
		instance.ghostReactorManager.reactor.RefreshReviveStations();
		if (!assetBundleSceneFilePaths.IsNullOrEmpty())
		{
			for (int sceneIndex = 0; sceneIndex < assetBundleSceneFilePaths.Length; sceneIndex++)
			{
				yield return UnloadSceneCoroutine(sceneIndex);
			}
		}
		GorillaNetworkJoinTrigger.EnableTriggerJoins();
		LightmapSettings.lightmaps = lightmaps;
		UnloadLightmaps();
		yield return ResetLightmaps();
		usingDynamicLighting = false;
		GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: false);
		GameLightingManager.instance.SetAmbientLightDynamic(Color.black);
		if (mapBundle != null)
		{
			mapBundle.Unload(unloadAllLoadedObjects: true);
		}
		mapBundle = null;
		Resources.UnloadUnusedAssets();
		cachedLuauScript = "";
		devModeEnabled = false;
		disableHoldingHandsAllModes = false;
		disableHoldingHandsCustomMode = false;
		queuedLoadZoneRequests.Clear();
		assetBundleSceneFilePaths = new string[1] { "" };
		loadedMapPackageInfo = null;
		loadedMapModId = 0L;
		loadedSceneFilePaths.Clear();
		loadedSceneNames.Clear();
		loadedSceneIndexes.Clear();
		initialSceneIndexes.Clear();
		initialSceneNames.Clear();
		maxPlayersForMap = 20;
		CustomMapModeSelector.ResetButtons();
		if (RoomSystem.JoinedRoom && NetworkSystem.Instance.LocalPlayer.IsMasterClient && NetworkSystem.Instance.SessionIsPrivate)
		{
			if (GameMode.ActiveGameMode.IsNull())
			{
				GameMode.ChangeGameMode(GameModeType.Casual.ToString());
			}
			else if (GameMode.ActiveGameMode.GameType() != GameModeType.Casual)
			{
				GameMode.ChangeGameMode(GameModeType.Casual.ToString());
			}
		}
		shouldAbortMapLoading = false;
		shouldAbortSceneLoad = false;
		isUnloading = false;
		if (unloadMapCallback != null)
		{
			unloadMapCallback?.Invoke();
			unloadMapCallback = null;
		}
	}

	private static IEnumerator AbortSceneLoad(int sceneIndex)
	{
		if (sceneIndex == -1)
		{
			shouldAbortMapLoading = true;
		}
		isLoading = false;
		if (shouldAbortMapLoading)
		{
			yield return UnloadMapCoroutine();
		}
		else
		{
			yield return UnloadSceneCoroutine(sceneIndex);
		}
		shouldAbortSceneLoad = false;
	}

	private static IEnumerator UnloadScenesCoroutine(int[] sceneIndexes)
	{
		for (int i = 0; i < sceneIndexes.Length; i++)
		{
			yield return UnloadSceneCoroutine(sceneIndexes[i]);
		}
	}

	private static IEnumerator UnloadSceneCoroutine(int sceneIndex, Action OnUnloadComplete = null)
	{
		if (!hasInstance)
		{
			yield break;
		}
		if (sceneIndex < 0 || sceneIndex >= assetBundleSceneFilePaths.Length)
		{
			Debug.LogError($"[CustomMapLoader::UnloadSceneCoroutine] SceneIndex of {sceneIndex} is invalid! " + $"The currently loaded AssetBundle contains {assetBundleSceneFilePaths.Length} scenes.");
			yield break;
		}
		while (runningAsyncLoad)
		{
			yield return null;
		}
		UnloadSceneOptions options = UnloadSceneOptions.UnloadAllEmbeddedSceneObjects;
		string scenePathWithExtension = assetBundleSceneFilePaths[sceneIndex];
		string[] array = scenePathWithExtension.Split(".");
		string text = "";
		string sceneName = "";
		if (!array.IsNullOrEmpty())
		{
			text = array[0];
			if (text.Length > 0)
			{
				sceneName = Path.GetFileName(text);
			}
		}
		Scene sceneByName = SceneManager.GetSceneByName(text);
		if (!sceneByName.IsValid())
		{
			yield break;
		}
		RemoveUnloadingStorePrefabs(sceneByName);
		for (int num = teleporters.Count - 1; num >= 0; num--)
		{
			if (teleporters[num].gameObject.scene == sceneByName)
			{
				teleporters.RemoveAt(num);
			}
		}
		yield return SceneManager.UnloadSceneAsync(scenePathWithExtension, options);
		loadedSceneFilePaths.Remove(scenePathWithExtension);
		loadedSceneNames.Remove(sceneName);
		loadedSceneIndexes.Remove(sceneIndex);
		sceneUnloadedCallback?.Invoke(sceneName);
		OnUnloadComplete?.Invoke();
	}

	private static void RemoveUnloadingStorePrefabs(Scene unloadingScene)
	{
		if (customMapATM.IsNotNull())
		{
			ATM_UI componentInChildren = customMapATM.GetComponentInChildren<ATM_UI>();
			if (componentInChildren.IsNotNull() && componentInChildren.IsFromCustomMapScene(unloadingScene) && ATM_Manager.instance.IsNotNull())
			{
				ATM_Manager.instance.RemoveATM(componentInChildren);
				ATM_Manager.instance.SetTemporaryCreatorCode(null);
			}
			UnityEngine.Object.Destroy(customMapATM);
			customMapATM = null;
		}
		int num = 0;
		for (num = storeDisplayStands.Count - 1; num >= 0; num--)
		{
			if (storeDisplayStands[num].IsNull())
			{
				storeDisplayStands.RemoveAt(num);
			}
			else
			{
				DynamicCosmeticStand componentInChildren2 = storeDisplayStands[num].GetComponentInChildren<DynamicCosmeticStand>();
				if (componentInChildren2.IsNotNull() && componentInChildren2.IsFromCustomMapScene(unloadingScene))
				{
					if (componentInChildren2.IsNotNull())
					{
						StoreController.instance.RemoveStandFromPlayFabIDDictionary(componentInChildren2);
					}
					UnityEngine.Object.Destroy(storeDisplayStands[num]);
					storeDisplayStands.RemoveAt(num);
				}
			}
		}
		for (num = storeCheckouts.Count - 1; num >= 0; num--)
		{
			if (storeCheckouts[num].IsNull())
			{
				storeCheckouts.RemoveAt(num);
			}
			else
			{
				ItemCheckout componentInChildren3 = storeCheckouts[num].GetComponentInChildren<ItemCheckout>();
				if (componentInChildren3.IsNotNull() && componentInChildren3.IsFromScene(unloadingScene))
				{
					componentInChildren3.RemoveFromCustomMap(instance.compositeTryOnArea);
					CosmeticsController.instance.RemoveItemCheckout(componentInChildren3);
					UnityEngine.Object.Destroy(storeCheckouts[num]);
					storeCheckouts.RemoveAt(num);
				}
			}
		}
		for (num = storeTryOnConsoles.Count - 1; num >= 0; num--)
		{
			if (storeTryOnConsoles[num].IsNull())
			{
				storeTryOnConsoles.RemoveAt(num);
			}
			else if (storeTryOnConsoles[num].scene.Equals(unloadingScene))
			{
				FittingRoom componentInChildren4 = storeTryOnConsoles[num].GetComponentInChildren<FittingRoom>();
				if (componentInChildren4.IsNotNull())
				{
					CosmeticsController.instance.RemoveFittingRoom(componentInChildren4);
				}
				storeTryOnConsoles.RemoveAt(num);
			}
		}
		for (num = storeTryOnAreas.Count - 1; num >= 0; num--)
		{
			if (storeTryOnAreas[num].IsNull())
			{
				storeTryOnAreas.RemoveAt(num);
			}
			else
			{
				CMSTryOnArea component = storeTryOnAreas[num].GetComponent<CMSTryOnArea>();
				if (component.IsNotNull() && component.IsFromScene(unloadingScene))
				{
					component.RemoveFromCustomMap(instance.compositeTryOnArea);
					UnityEngine.Object.Destroy(storeTryOnAreas[num]);
					storeTryOnAreas.RemoveAt(num);
				}
			}
		}
	}

	private static void CleanupPlaceholders()
	{
		for (int i = 0; i < instance.leafGliders.Length; i++)
		{
			instance.leafGliders[i].CustomMapUnload();
			instance.leafGliders[i].enabled = false;
			instance.leafGliders[i].transform.GetChild(0).gameObject.SetActive(value: false);
		}
	}

	private static IEnumerator ResetLightmaps()
	{
		instance.dayNightManager.RequestRepopulateLightmaps();
		LoadSceneParameters parameters = new LoadSceneParameters
		{
			loadSceneMode = LoadSceneMode.Additive,
			localPhysicsMode = LocalPhysicsMode.None
		};
		yield return SceneManager.LoadSceneAsync(10, parameters);
		yield return SceneManager.UnloadSceneAsync(10);
	}

	private static void UnloadLightmaps()
	{
		LightmapData[] array = LightmapSettings.lightmaps;
		foreach (LightmapData lightmapData in array)
		{
			if (lightmapData.lightmapColor != null && !lightmapsToKeep.Contains(lightmapData.lightmapColor))
			{
				Resources.UnloadAsset(lightmapData.lightmapColor);
			}
			if (lightmapData.lightmapDir != null && !lightmapsToKeep.Contains(lightmapData.lightmapDir))
			{
				Resources.UnloadAsset(lightmapData.lightmapDir);
			}
		}
	}

	private static int GetSceneIndex(string sceneName)
	{
		int result = -1;
		if (assetBundleSceneFilePaths.Length == 1)
		{
			return 0;
		}
		for (int i = 0; i < assetBundleSceneFilePaths.Length; i++)
		{
			string sceneNameFromFilePath = GetSceneNameFromFilePath(assetBundleSceneFilePaths[i]);
			if (sceneNameFromFilePath != null && sceneNameFromFilePath.Equals(sceneName))
			{
				result = i;
				break;
			}
		}
		_ = -1;
		return result;
	}

	private static string GetSceneNameFromFilePath(string filePath)
	{
		return filePath.Split("/")[^1].Split(".")[0];
	}

	public static MapPackageInfo GetPackageInfo(string packageInfoFilePath)
	{
		using StreamReader streamReader = new StreamReader(File.OpenRead(packageInfoFilePath), Encoding.Default);
		return JsonConvert.DeserializeObject<MapPackageInfo>(streamReader.ReadToEnd());
	}

	public static bool IsMapLoaded()
	{
		return IsMapLoaded(ModId.Null);
	}

	public static bool IsMapLoaded(ModId mapModId)
	{
		if (mapModId.IsValid())
		{
			if (!IsLoading())
			{
				return LoadedMapModId == mapModId;
			}
			return false;
		}
		if (!IsLoading())
		{
			return LoadedMapModId.IsValid();
		}
		return false;
	}

	public static bool IsLoading()
	{
		return isLoading;
	}

	public static long GetLoadingMapModId()
	{
		return attemptedLoadID;
	}

	public static byte GetRoomSizeForCurrentlyLoadedMap()
	{
		if (!IsMapLoaded())
		{
			return 20;
		}
		return maxPlayersForMap;
	}

	public static bool IsCustomScene(string sceneName)
	{
		return loadedSceneNames.Contains(sceneName);
	}

	public static string GetLuauGamemodeScript()
	{
		if (!IsMapLoaded())
		{
			return "";
		}
		return cachedLuauScript;
	}

	public static bool IsDevModeEnabled()
	{
		if (IsMapLoaded())
		{
			return devModeEnabled;
		}
		return false;
	}

	public static Transform GetCustomMapsDefaultSpawnLocation()
	{
		if (hasInstance)
		{
			return instance.CustomMapsDefaultSpawnLocation;
		}
		return null;
	}

	public static bool LoadedMapWantsHoldingHandsDisabled()
	{
		if (!IsMapLoaded())
		{
			return false;
		}
		if (disableHoldingHandsAllModes)
		{
			return true;
		}
		if (disableHoldingHandsCustomMode && GorillaGameManager.instance.IsNotNull() && GorillaGameManager.instance.GameType() == GameModeType.Custom)
		{
			return true;
		}
		return false;
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		if (defaultNexusGroupId == null)
		{
			Debug.LogError("You have to set defaultNexusGroupId in " + base.name + " or things will not work!");
			return false;
		}
		return true;
	}
}
