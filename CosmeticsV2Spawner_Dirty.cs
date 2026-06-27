using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Text;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaNetworking.Store;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CosmeticsV2Spawner_Dirty : IDelayedExecListener
{
	private struct LoadOpInfo(CosmeticAttachInfo attachInfo, CosmeticPart part, int partIndex, CosmeticInfoV2 cosmeticInfoV2, int vrRigIndex)
	{
		public bool isStarted = false;

		public AsyncOperationHandle<GameObject> loadOp = default(AsyncOperationHandle<GameObject>);

		public GameObject resultGObj = null;

		public readonly CosmeticAttachInfo attachInfo = attachInfo;

		public readonly CosmeticPart part = part;

		public readonly int partIndex = partIndex;

		public readonly CosmeticInfoV2 cosmeticInfoV2 = cosmeticInfoV2;

		public readonly int vrRigIndex = vrRigIndex;
	}

	public struct VRRigData
	{
		public readonly VRRig vrRig;

		public readonly Transform[] boneXforms;

		public readonly BodyDockPositions bdPositionsComp;

		public readonly List<GameObject> vrRig_cosmetics;

		public readonly List<GameObject> vrRig_override;

		public readonly Transform parentOfDeactivatedHoldables;

		public int bdPositions_allObjects_length;

		public readonly List<GameObject> bdPositions_leftHandThrowables;

		public readonly List<GameObject> bdPositions_rightHandThrowables;

		public VRRigData(VRRig vrRig, Transform[] boneXforms)
		{
			this.vrRig = vrRig;
			this.boneXforms = boneXforms;
			if (!vrRig.transform.TryFindByPath("./**/Holdables", out parentOfDeactivatedHoldables))
			{
				UnityEngine.Debug.LogError("Could not find parent for deactivated holdables. Falling back to VRRig transform: \"" + vrRig.transform.GetPath() + "\"");
			}
			bdPositionsComp = vrRig.GetComponentInChildren<BodyDockPositions>(includeInactive: true);
			vrRig_cosmetics = new List<GameObject>(500);
			vrRig_override = new List<GameObject>(500);
			bdPositions_leftHandThrowables = new List<GameObject>(20);
			bdPositions_rightHandThrowables = new List<GameObject>(20);
			bdPositions_allObjects_length = 2000;
		}
	}

	private static CosmeticsV2Spawner_Dirty _instance;

	public static Action OnPostInstantiateAllPrefabs;

	[OnEnterPlay_SetNull]
	private static Transform _gDeactivatedSpawnParent;

	[OnEnterPlay_Set(0)]
	private static int _g_loadOpsCountCompleted = 0;

	private const int _k_maxActiveLoadOps = 1000000;

	private const int _k_maxTotalLoadOps = 1000000;

	private const int _k_delayedStatusCheckContextId = -100;

	[OnEnterPlay_Clear]
	private static readonly List<LoadOpInfo> _g_loadOpInfos = new List<LoadOpInfo>(100000);

	[OnEnterPlay_Clear]
	private static Dictionary<string, List<LoadOpInfo>>[] _g_loadOpInfosForRigAndCosmeticIDDicts;

	[OnEnterPlay_Clear]
	private static readonly Dictionary<AsyncOperationHandle<GameObject>, int> _g_loadOp_to_index = new Dictionary<AsyncOperationHandle<GameObject>, int>(100000);

	[OnEnterPlay_SetNull]
	private static SnowballMaker _gSnowballMakerLeft;

	[OnEnterPlay_Clear]
	private static readonly List<SnowballThrowable> _gSnowballMakerLeft_throwables = new List<SnowballThrowable>(20);

	[OnEnterPlay_SetNull]
	private static SnowballMaker _gSnowballMakerRight;

	[OnEnterPlay_Clear]
	private static readonly List<SnowballThrowable> _gSnowballMakerRight_throwables = new List<SnowballThrowable>(20);

	[OnEnterPlay_SetNull]
	private static GTPlayer g_gorillaPlayer;

	private static Stopwatch k_stopwatch = new Stopwatch();

	[OnEnterPlay_Clear]
	public static readonly List<VRRigData> _gVRRigDatas = new List<VRRigData>(20);

	private static Dictionary<VRRig, int> _gVRRigDatasIndexByRig = new Dictionary<VRRig, int>();

	[OnEnterPlay_Clear]
	private static Dictionary<int, string> materialIndexToSnowballThrowablePlayfabIdStringLeft;

	[OnEnterPlay_Clear]
	private static Dictionary<int, string> materialIndexToSnowballThrowablePlayfabIdStringRight;

	[OnEnterPlay_Clear]
	private static Dictionary<int, string> throwableIndexPlayfabIdStringRight;

	[OnEnterPlay_Clear]
	private static Dictionary<int, string> throwableIndexPlayfabIdStringLeft;

	private static Dictionary<VRRig, HashSet<string>> processedIdsByRig = new Dictionary<VRRig, HashSet<string>>();

	private static Dictionary<CosmeticItemRegistry, List<GameObject>> currentGOBatchByRegistry = new Dictionary<CosmeticItemRegistry, List<GameObject>>();

	private static Dictionary<CosmeticItemRegistry, List<StringEnum<ECosmeticSelectSide>>> sides = new Dictionary<CosmeticItemRegistry, List<StringEnum<ECosmeticSelectSide>>>();

	private static Dictionary<CosmeticItemRegistry, List<bool>> overrides = new Dictionary<CosmeticItemRegistry, List<bool>>();

	[field: OnEnterPlay_Set(false)]
	public static bool isPrepared { get; private set; }

	void IDelayedExecListener.OnDelayedAction(int contextId)
	{
		if (contextId >= 0 && contextId < 1000000)
		{
			_RetryDownload(contextId);
		}
		else if (contextId == -Mathf.Abs("_Step5_InitializeVRRigsAndCosmeticsControllerFinalize".GetHashCode()))
		{
			_Step5_InitializeVRRigsAndCosmeticsControllerFinalize();
		}
	}

	public static void PrepareLoadOpInfos()
	{
		if (isPrepared || ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (_instance == null)
		{
			_instance = new CosmeticsV2Spawner_Dirty();
		}
		k_stopwatch.Restart();
		g_gorillaPlayer = UnityEngine.Object.FindAnyObjectByType<GTPlayer>();
		SnowballMaker[] componentsInChildren = g_gorillaPlayer.GetComponentsInChildren<SnowballMaker>(includeInactive: true);
		foreach (SnowballMaker snowballMaker in componentsInChildren)
		{
			if (snowballMaker.isLeftHand)
			{
				_gSnowballMakerLeft = snowballMaker;
			}
			else
			{
				_gSnowballMakerRight = snowballMaker;
			}
		}
		if (!CosmeticsController.hasInstance)
		{
			UnityEngine.Debug.LogError("(Should never happen) Cannot instantiate prefabs before cosmetics controller instance is available.");
			return;
		}
		if (!CosmeticsController.instance.v2_allCosmeticsInfoAssetRef.IsValid())
		{
			UnityEngine.Debug.LogError("(Should never happen) Cannot load prefabs before v2_allCosmeticsInfoAssetRef is loaded.");
			return;
		}
		if (!(CosmeticsController.instance.v2_allCosmeticsInfoAssetRef.Asset is AllCosmeticsArraySO allCosmeticsArraySO))
		{
			UnityEngine.Debug.LogError("(Should never happen) v2_allCosmeticsInfoAssetRef is valid but null.");
			return;
		}
		if (!GTHardCodedBones.TryGetBoneXforms(VRRig.LocalRig, out var outBoneXforms, out var outErrorMsg))
		{
			UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty: Error getting bone Transforms from local VRRig: " + outErrorMsg, VRRig.LocalRig);
			return;
		}
		_gVRRigDatas.Add(new VRRigData(VRRig.LocalRig, outBoneXforms));
		_gVRRigDatasIndexByRig[VRRig.LocalRig] = 0;
		int vrRigIndex = 0;
		VRRig[] allRigs = VRRigCache.Instance.GetAllRigs();
		foreach (VRRig vRRig in allRigs)
		{
			if (!GTHardCodedBones.TryGetBoneXforms(vRRig, out var outBoneXforms2, out outErrorMsg))
			{
				UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty: Error getting bone Transforms from cached VRRig: " + outErrorMsg, VRRig.LocalRig);
				return;
			}
			_gVRRigDatasIndexByRig[vRRig] = _gVRRigDatas.Count;
			_gVRRigDatas.Add(new VRRigData(vRRig, outBoneXforms2));
		}
		_gDeactivatedSpawnParent = GlobalDeactivatedSpawnRoot.GetOrCreate();
		GTDelayedExec.Add(_instance, 2f, -100);
		materialIndexToSnowballThrowablePlayfabIdStringLeft = new Dictionary<int, string>();
		materialIndexToSnowballThrowablePlayfabIdStringRight = new Dictionary<int, string>();
		throwableIndexPlayfabIdStringLeft = new Dictionary<int, string>();
		throwableIndexPlayfabIdStringRight = new Dictionary<int, string>();
		_g_loadOpInfosForRigAndCosmeticIDDicts = new Dictionary<string, List<LoadOpInfo>>[20];
		int partCount = 0;
		int partCount2 = 0;
		int partCount3 = 0;
		GTDirectAssetRef<CosmeticSO>[] sturdyAssetRefs = allCosmeticsArraySO.sturdyAssetRefs;
		foreach (GTDirectAssetRef<CosmeticSO> gTDirectAssetRef in sturdyAssetRefs)
		{
			CosmeticInfoV2 info = gTDirectAssetRef.obj.info;
			if (info.hasHoldableParts)
			{
				for (int k = 0; k < _gVRRigDatas.Count; k++)
				{
					for (int l = 0; l < info.holdableParts.Length; l++)
					{
						CosmeticPart part = info.holdableParts[l];
						if (!part.prefabAssetRef.RuntimeKeyIsValid())
						{
							if (k == 0)
							{
								GTDev.LogError("Cosmetic " + info.displayName + " has missing object reference in wearable parts, skipping load");
							}
						}
						else
						{
							AddEachAttachInfoToLoadOpInfosList(part, l, info, k, ref partCount);
						}
					}
				}
			}
			if (info.hasFunctionalParts)
			{
				for (int m = 0; m < _gVRRigDatas.Count; m++)
				{
					for (int n = 0; n < info.functionalParts.Length; n++)
					{
						CosmeticPart part2 = info.functionalParts[n];
						if (!part2.prefabAssetRef.RuntimeKeyIsValid())
						{
							if (m == 0)
							{
								GTDev.LogError("Cosmetic " + info.displayName + " has missing object reference in functional parts, skipping load");
							}
						}
						else
						{
							AddEachAttachInfoToLoadOpInfosList(part2, n, info, m, ref partCount);
						}
					}
				}
			}
			if (info.hasFirstPersonViewParts)
			{
				for (int num = 0; num < info.firstPersonViewParts.Length; num++)
				{
					CosmeticPart part3 = info.firstPersonViewParts[num];
					if (!part3.prefabAssetRef.RuntimeKeyIsValid())
					{
						GTDev.LogError("Cosmetic " + info.displayName + " has missing object reference in first person parts, skipping load");
					}
					else
					{
						AddEachAttachInfoToLoadOpInfosList(part3, num, info, vrRigIndex, ref partCount2);
					}
				}
			}
			if (!info.hasLocalRigParts)
			{
				continue;
			}
			for (int num2 = 0; num2 < info.localRigParts.Length; num2++)
			{
				CosmeticPart part4 = info.localRigParts[num2];
				if (!part4.prefabAssetRef.RuntimeKeyIsValid())
				{
					GTDev.LogError("Cosmetic " + info.displayName + " has missing object reference in local rig parts, skipping load");
				}
				else
				{
					AddEachAttachInfoToLoadOpInfosList(part4, num2, info, vrRigIndex, ref partCount3);
				}
			}
		}
		_Step4_PopulateAllArrays();
	}

	private static void AddEachAttachInfoToLoadOpInfosList(CosmeticPart part, int partIndex, CosmeticInfoV2 cosmeticInfo, int vrRigIndex, ref int partCount)
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		for (int i = 0; i < part.attachAnchors.Length; i++)
		{
			LoadOpInfo item = new LoadOpInfo(part.attachAnchors[i], part, partIndex, cosmeticInfo, vrRigIndex);
			if (cosmeticInfo.isThrowable)
			{
				if (GTHardCodedBones.GetHandednessFromBone(item.attachInfo.parentBone) == EHandedness.Right)
				{
					for (int j = 0; j < cosmeticInfo.throwableMaterialGrabIndices.Length; j++)
					{
						int key = cosmeticInfo.throwableMaterialGrabIndices[j];
						if (!materialIndexToSnowballThrowablePlayfabIdStringRight.ContainsKey(key))
						{
							materialIndexToSnowballThrowablePlayfabIdStringRight[key] = cosmeticInfo.playFabID;
						}
					}
					throwableIndexPlayfabIdStringRight.TryAdd(cosmeticInfo.throwableIndex, cosmeticInfo.playFabID);
				}
				else
				{
					for (int k = 0; k < cosmeticInfo.throwableMaterialGrabIndices.Length; k++)
					{
						int key2 = cosmeticInfo.throwableMaterialGrabIndices[k];
						if (!materialIndexToSnowballThrowablePlayfabIdStringLeft.ContainsKey(key2))
						{
							materialIndexToSnowballThrowablePlayfabIdStringLeft[key2] = cosmeticInfo.playFabID;
						}
					}
					throwableIndexPlayfabIdStringLeft.TryAdd(cosmeticInfo.throwableIndex, cosmeticInfo.playFabID);
				}
			}
			_g_loadOpInfos.Add(item);
			if (_g_loadOpInfosForRigAndCosmeticIDDicts[vrRigIndex] == null)
			{
				_g_loadOpInfosForRigAndCosmeticIDDicts[vrRigIndex] = new Dictionary<string, List<LoadOpInfo>>();
			}
			if (!_g_loadOpInfosForRigAndCosmeticIDDicts[vrRigIndex].ContainsKey(cosmeticInfo.playFabID))
			{
				_g_loadOpInfosForRigAndCosmeticIDDicts[vrRigIndex].Add(cosmeticInfo.playFabID, new List<LoadOpInfo>());
			}
			_g_loadOpInfosForRigAndCosmeticIDDicts[vrRigIndex][cosmeticInfo.playFabID].Add(item);
			partCount++;
			if (part.partType == ECosmeticPartType.Holdable && i == 0)
			{
				break;
			}
		}
	}

	public static bool GetPlayfabIdFromThrowableIndex(bool isLeft, int throwableIndex, out string playfabId)
	{
		if (throwableIndexPlayfabIdStringLeft == null || throwableIndexPlayfabIdStringRight == null)
		{
			playfabId = "null";
			return false;
		}
		if (isLeft && throwableIndexPlayfabIdStringLeft.TryGetValue(throwableIndex, out playfabId))
		{
			return true;
		}
		if (!isLeft && throwableIndexPlayfabIdStringRight.TryGetValue(throwableIndex, out playfabId))
		{
			return true;
		}
		playfabId = "null";
		return false;
	}

	public static bool GetThrowableIDFromMaterialIndex(bool isLeft, int matIndex, out string throwableId)
	{
		if (materialIndexToSnowballThrowablePlayfabIdStringLeft == null || materialIndexToSnowballThrowablePlayfabIdStringRight == null)
		{
			throwableId = "null";
			return false;
		}
		if (isLeft && materialIndexToSnowballThrowablePlayfabIdStringLeft.TryGetValue(matIndex, out throwableId))
		{
			return true;
		}
		if (!isLeft && materialIndexToSnowballThrowablePlayfabIdStringRight.TryGetValue(matIndex, out throwableId))
		{
			return true;
		}
		throwableId = "null";
		return false;
	}

	public static void ProcessLoadOpInfos(VRRig rig, string playfabId, CosmeticItemRegistry registry)
	{
		if (!processedIdsByRig.ContainsKey(rig))
		{
			processedIdsByRig.Add(rig, new HashSet<string>());
		}
		else if (processedIdsByRig[rig].Contains(playfabId))
		{
			return;
		}
		processedIdsByRig[rig].Add(playfabId);
		if (!currentGOBatchByRegistry.ContainsKey(registry))
		{
			currentGOBatchByRegistry[registry] = new List<GameObject>();
		}
		if (!sides.ContainsKey(registry))
		{
			sides[registry] = new List<StringEnum<ECosmeticSelectSide>>();
		}
		if (!overrides.ContainsKey(registry))
		{
			overrides[registry] = new List<bool>();
		}
		List<LoadOpInfo> list = _g_loadOpInfosForRigAndCosmeticIDDicts[_gVRRigDatasIndexByRig[rig]][playfabId];
		for (int i = 0; i < list.Count; i++)
		{
			int currentIndex = _g_loadOp_to_index.Count;
			_ProcessLoadOpInfo(currentIndex, list[i]);
			LoadOpInfo loadOpInfo = _g_loadOpInfos[currentIndex];
			loadOpInfo.loadOp.Completed += AddToRegistryWhenCompleted;
			void AddToRegistryWhenCompleted(AsyncOperationHandle<GameObject> loadOp)
			{
				GameObject item = ObjectToInitialize(_g_loadOpInfos[currentIndex]);
				overrides[registry].Add(_g_loadOpInfos[currentIndex].part.partType == ECosmeticPartType.LocalRig || _g_loadOpInfos[currentIndex].part.partType == ECosmeticPartType.FirstPerson);
				sides[registry].Add(_g_loadOpInfos[currentIndex].attachInfo.selectSide);
				currentGOBatchByRegistry[registry].Add(item);
				if (_g_loadOpsCountCompleted >= _g_loadOp_to_index.Count)
				{
					PostCompletionProcess();
				}
			}
		}
		static GameObject ObjectToInitialize(LoadOpInfo loadOpInfo2)
		{
			if (loadOpInfo2.resultGObj == null)
			{
				return null;
			}
			Transform transform = loadOpInfo2.resultGObj.transform;
			CosmeticPart[] holdableParts = loadOpInfo2.cosmeticInfoV2.holdableParts;
			if (holdableParts != null && holdableParts.Length > 0)
			{
				TransferrableObject componentInChildren = loadOpInfo2.resultGObj.GetComponentInChildren<TransferrableObject>(includeInactive: true);
				if ((bool)componentInChildren && componentInChildren.gameObject != loadOpInfo2.resultGObj)
				{
					transform = componentInChildren.transform;
					transform.gameObject.SetActive(value: false);
					loadOpInfo2.resultGObj.SetActive(value: true);
				}
			}
			if (loadOpInfo2.cosmeticInfoV2.isThrowable)
			{
				SnowballThrowable componentInChildren2 = loadOpInfo2.resultGObj.GetComponentInChildren<SnowballThrowable>(includeInactive: true);
				if ((bool)componentInChildren2 && componentInChildren2.gameObject != loadOpInfo2.resultGObj)
				{
					transform = componentInChildren2.transform;
					transform.gameObject.SetActive(value: false);
					loadOpInfo2.resultGObj.SetActive(value: true);
				}
			}
			return transform.gameObject;
		}
		static void PostCompletionProcess()
		{
			foreach (KeyValuePair<CosmeticItemRegistry, List<GameObject>> item2 in currentGOBatchByRegistry)
			{
				List<GameObject> value = item2.Value;
				CosmeticItemRegistry key = item2.Key;
				for (int j = 0; j < value.Count; j++)
				{
					if (!(value[j] == null))
					{
						try
						{
							if (overrides[key][j])
							{
								key.InitializeCosmetic(value[j], isOverride: true);
							}
						}
						catch (Exception exception)
						{
							UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty.PostCompletionProcess: InitializeCosmetic (override) threw for \"" + value[j].name + "\" on rig \"" + ((key.Rig != null) ? key.Rig.name : "<null rig>") + "\". Skipping this cosmetic so the rest of the batch still initializes. Exception follows:", value[j]);
							UnityEngine.Debug.LogException(exception, value[j]);
						}
					}
				}
				for (int k = 0; k < value.Count; k++)
				{
					if (!(value[k] == null))
					{
						try
						{
							if (!overrides[key][k])
							{
								key.InitializeCosmetic(value[k], isOverride: false);
							}
						}
						catch (Exception exception2)
						{
							UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty.PostCompletionProcess: InitializeCosmetic threw for \"" + value[k].name + "\" on rig \"" + ((key.Rig != null) ? key.Rig.name : "<null rig>") + "\". Skipping this cosmetic so the rest of the batch still initializes. Exception follows:", value[k]);
							UnityEngine.Debug.LogException(exception2, value[k]);
						}
					}
				}
				for (int l = 0; l < value.Count; l++)
				{
					if (!(value[l] == null))
					{
						ISpawnable[] componentsInChildren = value[l].GetComponentsInChildren<ISpawnable>(includeInactive: true);
						for (int m = 0; m < componentsInChildren.Length; m++)
						{
							if (!componentsInChildren[m].IsSpawned)
							{
								try
								{
									componentsInChildren[m].IsSpawned = true;
									componentsInChildren[m].CosmeticSelectedSide = sides[key][l];
									componentsInChildren[m].OnSpawn(key.Rig);
								}
								catch (Exception exception3)
								{
									UnityEngine.Debug.LogException(exception3);
								}
							}
						}
					}
				}
				value.Clear();
				sides[key].Clear();
				overrides[key].Clear();
				key.RefreshRig();
				if (key.Rig != null && key.Rig.myBodyDockPositions != null)
				{
					key.Rig.myBodyDockPositions.RefreshTransferrableItems();
				}
			}
		}
	}

	private static void _ProcessLoadOpInfo(int currentIndex, LoadOpInfo loadOpInfo)
	{
		try
		{
			loadOpInfo.loadOp = loadOpInfo.part.prefabAssetRef.InstantiateAsync(_gDeactivatedSpawnParent);
			loadOpInfo.isStarted = true;
			_g_loadOp_to_index.Add(loadOpInfo.loadOp, currentIndex);
			loadOpInfo.loadOp.Completed += _Step3_HandleLoadOpCompleted;
			_g_loadOpInfos[currentIndex] = loadOpInfo;
		}
		catch (InvalidKeyException ex)
		{
			UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty: Missing Addressable for " + $"\"{loadOpInfo.cosmeticInfoV2.displayName}\" part index {loadOpInfo.partIndex}. Skipping. {ex.Message}");
			loadOpInfo.isStarted = true;
			loadOpInfo.resultGObj = null;
			_g_loadOpInfos[currentIndex] = loadOpInfo;
			_g_loadOpsCountCompleted++;
		}
		catch (ArgumentException ex2)
		{
			UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty: Invalid Addressable key/config for " + $"\"{loadOpInfo.cosmeticInfoV2.displayName}\" part index {loadOpInfo.partIndex}. Skipping. {ex2.Message}");
			loadOpInfo.isStarted = true;
			loadOpInfo.resultGObj = null;
			_g_loadOpInfos[currentIndex] = loadOpInfo;
			_g_loadOpsCountCompleted++;
		}
	}

	private static void _Step3_HandleLoadOpCompleted(AsyncOperationHandle<GameObject> loadOp)
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (!_g_loadOp_to_index.TryGetValue(loadOp, out var value))
		{
			throw new Exception("(this should never happen) could not find LoadOpInfo in `_g_loadOpInfos`.");
		}
		LoadOpInfo loadOpInfo = _g_loadOpInfos[value];
		if (loadOp.Status == AsyncOperationStatus.Failed)
		{
			UnityEngine.Debug.LogWarning("CosmeticsV2Spawner_Dirty: Failed to load part " + $"\"{loadOpInfo.cosmeticInfoV2.displayName}\" (key: {loadOpInfo.part.prefabAssetRef.RuntimeKey}). Skipping.");
			_g_loadOpsCountCompleted++;
			_g_loadOp_to_index.Remove(loadOp);
			return;
		}
		_g_loadOpsCountCompleted++;
		ECosmeticSelectSide eCosmeticSelectSide = loadOpInfo.attachInfo.selectSide;
		string name = loadOpInfo.cosmeticInfoV2.playFabID;
		if (eCosmeticSelectSide != ECosmeticSelectSide.Both)
		{
			string playFabID = loadOpInfo.cosmeticInfoV2.playFabID;
			name = ZString.Concat(playFabID, eCosmeticSelectSide switch
			{
				ECosmeticSelectSide.Left => " LEFT.", 
				ECosmeticSelectSide.Right => " RIGHT.", 
				_ => "", 
			});
		}
		loadOpInfo.resultGObj = loadOp.Result;
		loadOpInfo.resultGObj.SetActive(value: false);
		Transform transform = loadOpInfo.resultGObj.transform;
		Transform transform2 = transform;
		CosmeticPart[] holdableParts = loadOpInfo.cosmeticInfoV2.holdableParts;
		if (holdableParts != null && holdableParts.Length > 0)
		{
			TransferrableObject componentInChildren = loadOpInfo.resultGObj.GetComponentInChildren<TransferrableObject>(includeInactive: true);
			if ((bool)componentInChildren && componentInChildren.gameObject != loadOpInfo.resultGObj)
			{
				transform2 = componentInChildren.transform;
				transform2.gameObject.SetActive(value: false);
				loadOpInfo.resultGObj.SetActive(value: true);
			}
		}
		if (loadOpInfo.cosmeticInfoV2.isThrowable)
		{
			SnowballThrowable componentInChildren2 = loadOpInfo.resultGObj.GetComponentInChildren<SnowballThrowable>(includeInactive: true);
			if ((bool)componentInChildren2 && componentInChildren2.gameObject != loadOpInfo.resultGObj)
			{
				transform2 = componentInChildren2.transform;
				transform2.gameObject.SetActive(value: false);
				loadOpInfo.resultGObj.SetActive(value: true);
			}
		}
		transform2.name = name;
		VRRigData value2 = ((loadOpInfo.vrRigIndex != -1) ? _gVRRigDatas[loadOpInfo.vrRigIndex] : default(VRRigData));
		if (loadOpInfo.cosmeticInfoV2.category != CosmeticsController.CosmeticCategory.Collectable)
		{
			Transform transform3 = loadOpInfo.part.partType switch
			{
				ECosmeticPartType.Holdable => ((GTHardCodedBones.EBone)loadOpInfo.attachInfo.parentBone != GTHardCodedBones.EBone.body_AnchorFront_StowSlot) ? value2.parentOfDeactivatedHoldables : value2.boneXforms[(int)loadOpInfo.attachInfo.parentBone], 
				ECosmeticPartType.Functional => value2.boneXforms[(int)loadOpInfo.attachInfo.parentBone], 
				ECosmeticPartType.FirstPerson => g_gorillaPlayer.CosmeticsHeadTarget, 
				ECosmeticPartType.LocalRig => value2.boneXforms[(int)loadOpInfo.attachInfo.parentBone], 
				_ => throw new ArgumentOutOfRangeException("partType", "unhandled part type."), 
			};
			if ((bool)transform3)
			{
				transform.SetParent(transform3, worldPositionStays: false);
				transform.localPosition = loadOpInfo.attachInfo.offset.pos;
				transform.localRotation = loadOpInfo.attachInfo.offset.rot;
				transform.localScale = loadOpInfo.attachInfo.offset.scale;
			}
			else
			{
				UnityEngine.Debug.LogError($"Bone transform not found for cosmetic part type {loadOpInfo.part.partType}. Cosmetic: " + "\"" + loadOpInfo.cosmeticInfoV2.displayName + "\"," + $"part: \"{loadOpInfo.part.prefabAssetRef.RuntimeKey}\"");
			}
		}
		switch (loadOpInfo.part.partType)
		{
		case ECosmeticPartType.Holdable:
		{
			value2.vrRig_cosmetics.Add(transform2.gameObject);
			HoldableObject componentInChildren3 = loadOpInfo.resultGObj.GetComponentInChildren<HoldableObject>(includeInactive: true);
			if (!(componentInChildren3 is SnowballThrowable throwable))
			{
				if (!(componentInChildren3 is TransferrableObject transferrableObject))
				{
					if ((object)componentInChildren3 != null)
					{
						throw new Exception("Encountered unexpected HoldableObject derived type on cosmetic part: \"" + loadOpInfo.cosmeticInfoV2.displayName + "\"");
					}
					break;
				}
				string playFabID2 = loadOpInfo.cosmeticInfoV2.playFabID;
				if (CosmeticsLegacyV1Info.TryGetBodyDockAllObjectsIndexes(playFabID2, out var bdAllIndexes))
				{
					if (loadOpInfo.partIndex < bdAllIndexes.Length && loadOpInfo.partIndex >= 0)
					{
						transferrableObject.myIndex = bdAllIndexes[loadOpInfo.partIndex];
					}
				}
				else if (playFabID2.Length >= 5 && playFabID2[0] == 'L')
				{
					if (playFabID2[1] != 'M')
					{
						throw new Exception("(this should never happen) A TransferrableObject cosmetic added sometime after 2024-06 does not use the expected PlayFabID format where the string starts with \"LM\" and ends with \".\". Path: " + transform2.GetPathQ());
					}
					string text = playFabID2;
					playFabID2 = ((text[text.Length - 1] == '.') ? playFabID2 : (playFabID2 + "."));
					int num = 224;
					transferrableObject.myIndex = num + CosmeticIDUtils.PlayFabIdToIndexInCategory(playFabID2);
				}
				else
				{
					transferrableObject.myIndex = -2;
					if (!(playFabID2 == "STICKABLE TARGET"))
					{
						UnityEngine.Debug.LogError("Cosmetic \"" + loadOpInfo.cosmeticInfoV2.displayName + "\" cannot derive `TransferrableObject.myIndex` from playFabId \"" + playFabID2 + "\" and so will not be included in `BodyDockPositions.allObjects` array.");
					}
				}
				if (transferrableObject is ProjectileWeapon projectileWeapon && loadOpInfo.cosmeticInfoV2.playFabID == "Slingshot")
				{
					value2.vrRig.projectileWeapon = projectileWeapon;
				}
				if (transferrableObject.myIndex > 0 && transferrableObject.myIndex < value2.bdPositions_allObjects_length)
				{
					value2.bdPositionsComp._allObjects[transferrableObject.myIndex] = transferrableObject;
					if (!value2.vrRig.isOfflineVRRig)
					{
						value2.bdPositionsComp.RefreshTransferrableItems();
					}
				}
			}
			else
			{
				AddPartToThrowableLists(loadOpInfo, throwable);
			}
			break;
		}
		case ECosmeticPartType.Functional:
			value2.vrRig_cosmetics.Add(transform2.gameObject);
			break;
		case ECosmeticPartType.FirstPerson:
		case ECosmeticPartType.LocalRig:
			value2.vrRig_override.Add(transform2.gameObject);
			break;
		default:
			throw new ArgumentOutOfRangeException("Unexpected ECosmeticPartType value encountered: " + $"{loadOpInfo.part.partType}, " + $"int: {(int)loadOpInfo.part.partType}.");
		}
		if (loadOpInfo.vrRigIndex > -1)
		{
			_gVRRigDatas[loadOpInfo.vrRigIndex] = value2;
		}
		CosmeticRefRegistry cosmeticReferences = _gVRRigDatas[loadOpInfo.vrRigIndex].vrRig.cosmeticReferences;
		CosmeticRefTarget[] componentsInChildren = loadOpInfo.resultGObj.GetComponentsInChildren<CosmeticRefTarget>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			cosmeticReferences.Register(componentsInChildren[i].id, componentsInChildren[i].gameObject);
		}
		_g_loadOpInfos[value] = loadOpInfo;
	}

	private static void _RetryDownload(int loadOpIndex)
	{
		if (loadOpIndex < 0 || loadOpIndex >= _g_loadOpInfos.Count)
		{
			UnityEngine.Debug.LogError("(should never happen) Unexpected! While trying to recover from a failed download, the value " + string.Format("{0}={1} was out of range of ", "loadOpIndex", loadOpIndex) + string.Format("{0}.Count={1}.", "_g_loadOpInfos", _g_loadOpInfos.Count));
			return;
		}
		LoadOpInfo value = _g_loadOpInfos[loadOpIndex];
		if (!_g_loadOp_to_index.Remove(value.loadOp))
		{
			UnityEngine.Debug.LogWarning("(should never happen) Unexpected! Could not find the loadOp to remove it in the _g_loadOp_to_index. If you see this message then comparison does not work the way I thought and we need a different way to store/retrieve loadOpInfos. Happened while trying to retry failed download prefab part of cosmetic \"" + value.cosmeticInfoV2.displayName + "\" with guid \"" + value.part.prefabAssetRef.AssetGUID + "\".");
		}
		UnityEngine.Debug.Log("Retrying prefab part of cosmetic \"" + value.cosmeticInfoV2.displayName + "\" with guid \"" + value.part.prefabAssetRef.AssetGUID + "\".");
		value.loadOp = value.part.prefabAssetRef.InstantiateAsync(_gDeactivatedSpawnParent);
		_g_loadOpInfos[loadOpIndex] = value;
		_g_loadOp_to_index[value.loadOp] = loadOpIndex;
		value.loadOp.Completed += _Step3_HandleLoadOpCompleted;
	}

	private static void AddPartToThrowableLists(LoadOpInfo loadOpInfo, SnowballThrowable throwable)
	{
		VRRigData vRRigData = _gVRRigDatas[loadOpInfo.vrRigIndex];
		EHandedness handednessFromBone = GTHardCodedBones.GetHandednessFromBone(loadOpInfo.attachInfo.parentBone);
		bool flag = vRRigData.vrRig == _gVRRigDatas[0].vrRig;
		throwable.SpawnOffset = loadOpInfo.attachInfo.offset;
		switch (handednessFromBone)
		{
		case EHandedness.Left:
			ResizeAndSetAtIndex(vRRigData.bdPositions_leftHandThrowables, throwable.gameObject, throwable.throwableMakerIndex);
			if (flag)
			{
				ResizeAndSetAtIndex(_gSnowballMakerLeft_throwables, throwable, throwable.throwableMakerIndex);
			}
			vRRigData.bdPositionsComp.leftHandThrowables = vRRigData.bdPositions_leftHandThrowables.ToArray();
			_gSnowballMakerLeft.SetupThrowables(_gSnowballMakerLeft_throwables.ToArray());
			break;
		case EHandedness.Right:
			ResizeAndSetAtIndex(vRRigData.bdPositions_rightHandThrowables, throwable.gameObject, throwable.throwableMakerIndex);
			if (flag)
			{
				ResizeAndSetAtIndex(_gSnowballMakerRight_throwables, throwable, throwable.throwableMakerIndex);
			}
			vRRigData.bdPositionsComp.rightHandThrowables = vRRigData.bdPositions_rightHandThrowables.ToArray();
			_gSnowballMakerRight.SetupThrowables(_gSnowballMakerRight_throwables.ToArray());
			break;
		case EHandedness.None:
			throw new ArgumentException("Encountered throwable cosmetic \"" + loadOpInfo.cosmeticInfoV2.displayName + "\" where handedness " + $"could not be determined from bone `{loadOpInfo.attachInfo.parentBone}`. " + "Path: \"" + throwable.transform.GetPath() + "\"");
		default:
			throw new ArgumentOutOfRangeException("Unexpected ECosmeticSelectSide value encountered: " + $"{handednessFromBone}, " + $"int: {(int)handednessFromBone}.");
		}
	}

	private static void ResizeAndSetAtIndex<T>(List<T> list, T item, int index)
	{
		if (index >= list.Count)
		{
			int num = index - list.Count + 1;
			for (int i = 0; i < num; i++)
			{
				list.Add(default(T));
			}
		}
		list[index] = item;
	}

	private static void _Step4_PopulateAllArrays()
	{
		foreach (VRRigData gVRRigData in _gVRRigDatas)
		{
			gVRRigData.bdPositionsComp._allObjects = new TransferrableObject[2000];
		}
		GTDelayedExec.Add(_instance, 1f, -Mathf.Abs("_Step5_InitializeVRRigsAndCosmeticsControllerFinalize".GetHashCode()));
	}

	private static void _Step5_InitializeVRRigsAndCosmeticsControllerFinalize()
	{
		CosmeticsController.instance.UpdateWardrobeModelsAndButtons();
		try
		{
			OnPostInstantiateAllPrefabs?.Invoke();
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception);
		}
		try
		{
			CosmeticsController.instance.InitializeCosmeticStands();
		}
		catch (Exception exception2)
		{
			UnityEngine.Debug.LogException(exception2);
		}
		try
		{
			CosmeticsController.instance.UpdateWornCosmetics();
			StartupRerun();
		}
		catch (Exception exception3)
		{
			UnityEngine.Debug.LogException(exception3);
		}
		foreach (VRRigData gVRRigData in _gVRRigDatas)
		{
			try
			{
				if (gVRRigData.bdPositionsComp.isActiveAndEnabled)
				{
					gVRRigData.bdPositionsComp.RefreshTransferrableItems();
				}
			}
			catch (Exception exception4)
			{
				UnityEngine.Debug.LogException(exception4, gVRRigData.vrRig);
			}
		}
		try
		{
			StoreController.instance.InitalizeCosmeticStands();
		}
		catch (Exception exception5)
		{
			UnityEngine.Debug.LogException(exception5);
		}
		isPrepared = true;
		k_stopwatch.Stop();
		UnityEngine.Debug.Log("_Step5_InitializeVRRigsAndCosmeticsControllerFinalize" + $": Done preparing cosmetics system in {(double)k_stopwatch.ElapsedMilliseconds / 1000.0:0.0000} seconds.");
		static async void StartupRerun()
		{
			await Awaitable.WaitForSecondsAsync(2f);
			CosmeticsController.instance.UpdateWornCosmetics();
		}
	}

	public static VRRigData RigDataForRig(VRRig rig)
	{
		return _gVRRigDatas[_gVRRigDatasIndexByRig[rig]];
	}
}
