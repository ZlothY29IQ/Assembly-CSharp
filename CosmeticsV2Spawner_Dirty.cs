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
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CosmeticsV2Spawner_Dirty : IDelayedExecListener, ITickSystemTick
{
	private struct LoadOpInfo
	{
		public bool isStarted;

		public AsyncOperationHandle<GameObject> loadOp;

		public GameObject resultGObj;

		public readonly CosmeticAttachInfo attachInfo;

		public readonly CosmeticPart part;

		public readonly int partIndex;

		public readonly CosmeticInfoV2 cosmeticInfoV2;

		public readonly int vrRigIndex;

		public LoadOpInfo(CosmeticAttachInfo attachInfo, CosmeticPart part, int partIndex, CosmeticInfoV2 cosmeticInfoV2, int vrRigIndex)
		{
			isStarted = false;
			loadOp = default(AsyncOperationHandle<GameObject>);
			resultGObj = null;
			this.attachInfo = attachInfo;
			this.part = part;
			this.partIndex = partIndex;
			this.cosmeticInfoV2 = cosmeticInfoV2;
			this.vrRigIndex = vrRigIndex;
		}
	}

	private struct VRRigData
	{
		public readonly VRRig vrRig;

		public readonly Transform[] boneXforms;

		public readonly BodyDockPositions bdPositionsComp;

		public readonly List<GameObject> vrRig_cosmetics;

		public readonly List<GameObject> vrRig_override;

		public readonly Transform parentOfDeactivatedHoldables;

		public readonly List<TransferrableObject> bdPositions_allObjects;

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
			bdPositions_allObjects = new List<TransferrableObject>(20);
			bdPositions_allObjects_length = 0;
		}
	}

	private static CosmeticsV2Spawner_Dirty _instance;

	public static Action OnPostInstantiateAllPrefabs;

	public static Action OnPostInstantiateAllPrefabs2;

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

	[OnEnterPlay_SetNull]
	private static Transform[] g_allInstantiatedParts;

	private static Stopwatch k_stopwatch = new Stopwatch();

	[OnEnterPlay_Clear]
	private static readonly List<VRRigData> _gVRRigDatas = new List<VRRigData>(11);

	private bool _shouldTick;

	[field: OnEnterPlay_Set(false)]
	public static bool startedAllPartsInstantiated { get; private set; }

	[field: OnEnterPlay_Set(false)]
	public static bool allPartsInstantiated { get; private set; }

	[field: OnEnterPlay_Set(false)]
	public static bool completed { get; private set; }

	public bool TickRunning { get; set; }

	void ITickSystemTick.Tick()
	{
		_shouldTick = false;
		if (_g_loadOp_to_index.Count < _g_loadOpInfos.Count)
		{
			_shouldTick = true;
			_Step2_UpdateLoadOpStarting();
		}
		if (!_shouldTick)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
	}

	void IDelayedExecListener.OnDelayedAction(int contextId)
	{
		if (contextId >= 0 && contextId < 1000000)
		{
			_RetryDownload(contextId);
		}
		else if (contextId == -100)
		{
			_DelayedStatusCheck();
		}
		else if (contextId == -Mathf.Abs("_Step5_InitializeVRRigsAndCosmeticsControllerFinalize".GetHashCode()))
		{
			_Step5_InitializeVRRigsAndCosmeticsControllerFinalize();
		}
	}

	public static void StartInstantiatingPrefabs()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (startedAllPartsInstantiated || allPartsInstantiated)
		{
			UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty.StartInstantiatingPrefabs: All parts already started instantiated. Check `startedAllPartsInstantiated` before calling this.");
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
			UnityEngine.Debug.LogError("(should never happen) cannot instantiate prefabs before cosmetics controller instance is available.");
			return;
		}
		if (!CosmeticsController.instance.v2_allCosmeticsInfoAssetRef.IsValid())
		{
			UnityEngine.Debug.LogError("(should never happen) cannot load prefabs before v2_allCosmeticsInfoAssetRef is loaded.");
			return;
		}
		if (!(CosmeticsController.instance.v2_allCosmeticsInfoAssetRef.Asset is AllCosmeticsArraySO allCosmeticsArraySO))
		{
			UnityEngine.Debug.LogError("(should never happen) v2_allCosmeticsInfoAssetRef is valid but null.");
			return;
		}
		if (!GTHardCodedBones.TryGetBoneXforms(VRRig.LocalRig, out var outBoneXforms, out var outErrorMsg))
		{
			UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty: Error getting bone Transforms from local VRRig: " + outErrorMsg, VRRig.LocalRig);
			return;
		}
		_gVRRigDatas.Add(new VRRigData(VRRig.LocalRig, outBoneXforms));
		int vrRigIndex = 0;
		VRRig[] allRigs = VRRigCache.Instance.GetAllRigs();
		foreach (VRRig vrRig in allRigs)
		{
			if (!GTHardCodedBones.TryGetBoneXforms(vrRig, out var outBoneXforms2, out outErrorMsg))
			{
				UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty: Error getting bone Transforms from cached VRRig: " + outErrorMsg, VRRig.LocalRig);
				return;
			}
			_gVRRigDatas.Add(new VRRigData(vrRig, outBoneXforms2));
		}
		_gDeactivatedSpawnParent = GlobalDeactivatedSpawnRoot.GetOrCreate();
		GTDelayedExec.Add(_instance, 2f, -100);
		int partCount = 0;
		int partCount2 = 0;
		int partCount3 = 0;
		GTDirectAssetRef<CosmeticSO>[] sturdyAssetRefs = allCosmeticsArraySO.sturdyAssetRefs;
		foreach (GTDirectAssetRef<CosmeticSO> gTDirectAssetRef in sturdyAssetRefs)
		{
			CosmeticInfoV2 info = gTDirectAssetRef.obj.info;
			if (info.hasHoldableParts)
			{
				for (int j = 0; j < _gVRRigDatas.Count; j++)
				{
					for (int k = 0; k < info.holdableParts.Length; k++)
					{
						CosmeticPart part = info.holdableParts[k];
						if (!part.prefabAssetRef.RuntimeKeyIsValid())
						{
							if (j == 0)
							{
								GTDev.LogError("Cosmetic " + info.displayName + " has missing object reference in wearable parts, skipping load");
							}
						}
						else
						{
							AddEachAttachInfoToLoadOpInfosList(part, k, info, j, ref partCount);
						}
					}
				}
			}
			if (info.hasFunctionalParts)
			{
				for (int l = 0; l < _gVRRigDatas.Count; l++)
				{
					for (int m = 0; m < info.functionalParts.Length; m++)
					{
						CosmeticPart part2 = info.functionalParts[m];
						if (!part2.prefabAssetRef.RuntimeKeyIsValid())
						{
							if (l == 0)
							{
								GTDev.LogError("Cosmetic " + info.displayName + " has missing object reference in functional parts, skipping load");
							}
						}
						else
						{
							AddEachAttachInfoToLoadOpInfosList(part2, m, info, l, ref partCount);
						}
					}
				}
			}
			if (info.hasFirstPersonViewParts)
			{
				for (int n = 0; n < info.firstPersonViewParts.Length; n++)
				{
					CosmeticPart part3 = info.firstPersonViewParts[n];
					if (!part3.prefabAssetRef.RuntimeKeyIsValid())
					{
						GTDev.LogError("Cosmetic " + info.displayName + " has missing object reference in first person parts, skipping load");
					}
					else
					{
						AddEachAttachInfoToLoadOpInfosList(part3, n, info, vrRigIndex, ref partCount2);
					}
				}
			}
			if (!info.hasLocalRigParts)
			{
				continue;
			}
			for (int num = 0; num < info.localRigParts.Length; num++)
			{
				CosmeticPart part4 = info.localRigParts[num];
				if (!part4.prefabAssetRef.RuntimeKeyIsValid())
				{
					GTDev.LogError("Cosmetic " + info.displayName + " has missing object reference in local rig parts, skipping load");
				}
				else
				{
					AddEachAttachInfoToLoadOpInfosList(part4, num, info, vrRigIndex, ref partCount3);
				}
			}
		}
		TickSystem<object>.AddTickCallback(_instance);
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
			_g_loadOpInfos.Add(item);
			partCount++;
			if (part.partType == ECosmeticPartType.Holdable && i == 0)
			{
				break;
			}
		}
	}

	private static void _Step2_UpdateLoadOpStarting()
	{
		int num = _g_loadOp_to_index.Count - _g_loadOpsCountCompleted;
		while (_g_loadOp_to_index.Count < _g_loadOpInfos.Count && num < 1000000)
		{
			num++;
			int count = _g_loadOp_to_index.Count;
			LoadOpInfo value = _g_loadOpInfos[count];
			try
			{
				value.loadOp = value.part.prefabAssetRef.InstantiateAsync(_gDeactivatedSpawnParent);
				value.isStarted = true;
				_g_loadOp_to_index.Add(value.loadOp, count);
				value.loadOp.Completed += _Step3_HandleLoadOpCompleted;
				_g_loadOpInfos[count] = value;
			}
			catch (InvalidKeyException ex)
			{
				UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty: Missing Addressable for " + $"\"{value.cosmeticInfoV2.displayName}\" part index {value.partIndex}. Skipping. {ex.Message}");
				value.isStarted = true;
				value.resultGObj = null;
				_g_loadOpInfos[count] = value;
				_g_loadOpsCountCompleted++;
				num--;
			}
			catch (ArgumentException ex2)
			{
				UnityEngine.Debug.LogError("CosmeticsV2Spawner_Dirty: Invalid Addressable key/config for " + $"\"{value.cosmeticInfoV2.displayName}\" part index {value.partIndex}. Skipping. {ex2.Message}");
				value.isStarted = true;
				value.resultGObj = null;
				_g_loadOpInfos[count] = value;
				_g_loadOpsCountCompleted++;
				num--;
			}
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
		if (eCosmeticSelectSide != 0)
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
				value2.bdPositions_allObjects.Add(transferrableObject);
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
				value2.bdPositions_allObjects_length = math.max(transferrableObject.myIndex + 1, value2.bdPositions_allObjects_length);
				if (transferrableObject is ProjectileWeapon projectileWeapon && loadOpInfo.cosmeticInfoV2.playFabID == "Slingshot")
				{
					value2.vrRig.projectileWeapon = projectileWeapon;
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
		if (_g_loadOpsCountCompleted >= _g_loadOpInfos.Count)
		{
			_Step4_PopulateAllArrays();
		}
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
			break;
		case EHandedness.Right:
			ResizeAndSetAtIndex(vRRigData.bdPositions_rightHandThrowables, throwable.gameObject, throwable.throwableMakerIndex);
			if (flag)
			{
				ResizeAndSetAtIndex(_gSnowballMakerRight_throwables, throwable, throwable.throwableMakerIndex);
			}
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
		if (allPartsInstantiated)
		{
			UnityEngine.Debug.LogError("_Step4_PopulateAllArrays: (should never happen) CALLED MORE THAN ONCE!");
			return;
		}
		foreach (LoadOpInfo g_loadOpInfo in _g_loadOpInfos)
		{
			if (g_loadOpInfo.resultGObj == null)
			{
				continue;
			}
			ISpawnable[] componentsInChildren = g_loadOpInfo.resultGObj.GetComponentsInChildren<ISpawnable>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				try
				{
					componentsInChildren[i].IsSpawned = true;
					componentsInChildren[i].CosmeticSelectedSide = g_loadOpInfo.attachInfo.selectSide;
					componentsInChildren[i].OnSpawn(_gVRRigDatas[g_loadOpInfo.vrRigIndex].vrRig);
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}
		}
		_gSnowballMakerLeft.SetupThrowables(_gSnowballMakerLeft_throwables.ToArray());
		_gSnowballMakerRight.SetupThrowables(_gSnowballMakerRight_throwables.ToArray());
		foreach (VRRigData gVRRigData in _gVRRigDatas)
		{
			gVRRigData.vrRig.cosmetics = gVRRigData.vrRig_cosmetics.ToArray();
			gVRRigData.vrRig.overrideCosmetics = gVRRigData.vrRig_override.ToArray();
			gVRRigData.bdPositionsComp.leftHandThrowables = gVRRigData.bdPositions_leftHandThrowables.ToArray();
			gVRRigData.bdPositionsComp.rightHandThrowables = gVRRigData.bdPositions_rightHandThrowables.ToArray();
			gVRRigData.bdPositionsComp._allObjects = new TransferrableObject[gVRRigData.bdPositions_allObjects_length];
			foreach (TransferrableObject bdPositions_allObject in gVRRigData.bdPositions_allObjects)
			{
				if (bdPositions_allObject.myIndex >= 0 && bdPositions_allObject.myIndex < gVRRigData.bdPositions_allObjects_length)
				{
					gVRRigData.bdPositionsComp._allObjects[bdPositions_allObject.myIndex] = bdPositions_allObject;
				}
			}
		}
		allPartsInstantiated = true;
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
			OnPostInstantiateAllPrefabs2?.Invoke();
		}
		catch (Exception exception3)
		{
			UnityEngine.Debug.LogException(exception3);
		}
		try
		{
			CosmeticsController.instance.UpdateWornCosmetics();
		}
		catch (Exception exception4)
		{
			UnityEngine.Debug.LogException(exception4);
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
			catch (Exception exception5)
			{
				UnityEngine.Debug.LogException(exception5, gVRRigData.vrRig);
			}
		}
		try
		{
			StoreController.instance.InitalizeCosmeticStands();
		}
		catch (Exception exception6)
		{
			UnityEngine.Debug.LogException(exception6);
		}
		completed = true;
		k_stopwatch.Stop();
		UnityEngine.Debug.Log("_Step5_InitializeVRRigsAndCosmeticsControllerFinalize" + $": Done instantiating cosmetics in {(double)k_stopwatch.ElapsedMilliseconds / 1000.0:0.0000} seconds.");
	}

	private void _DelayedStatusCheck()
	{
		int count = _g_loadOpInfos.Count;
		UnityEngine.Debug.Log(ZString.Concat("CosmeticsV2Spawner_Dirty", ".", "_DelayedStatusCheck", ": Load progress ", (double)_g_loadOpsCountCompleted / (double)count * 100.0, "% (", _g_loadOpsCountCompleted, "/", count, ")."));
		if (_g_loadOpsCountCompleted < count)
		{
			GTDelayedExec.Add(this, 2f, -100);
		}
	}
}
