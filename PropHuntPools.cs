using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using PlayFab;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class PropHuntPools
{
	public enum EState
	{
		None,
		WaitingForTitleData,
		WaitingForLocalPlayerToVisitBayou,
		SpawningProps,
		Ready
	}

	private const string preLog = "PropHuntPools: ";

	private const string preLogEd = "(editor only log) PropHuntPools: ";

	private const string preLogBeta = "(beta only log) PropHuntPools: ";

	private const string preErr = "ERROR!!!  PropHuntPools: ";

	private const string preErrEd = "ERROR!!!  (editor only log) PropHuntPools: ";

	private const string preErrBeta = "ERROR!!!  (beta only log) PropHuntPools: ";

	private const string _k_titleDataKey = "PropHuntProps";

	private const bool _k__GT_PROP_HUNT__USE_POOLING__ = true;

	[OnEnterPlay_Set(EState.None)]
	private static EState _state;

	[OnEnterPlay_Set(false)]
	private static bool _state_isTitleDataLoaded;

	[OnEnterPlay_Set(false)]
	private static bool _state_hasLocalPlayerVisitedBayou;

	public static Action OnReady;

	[OnEnterPlay_SetNull]
	private static AllCosmeticsArraySO _allCosmeticsArraySO;

	private static readonly string[] _g_ph_titleDataSeparators = new string[3] { "\"", " ", "\n" };

	[OnEnterPlay_SetNull]
	private static string[] _allPropCosmeticIds;

	[OnEnterPlay_SetNull]
	private static CosmeticSO _fallbackProp_cosmeticSO;

	public static Dictionary<string, CosmeticSO> propCosmeticId_to_cosmeticSO = new Dictionary<string, CosmeticSO>(256);

	[OnEnterPlay_Clear]
	private static readonly HashSet<string> _propCosmeticIdsWaitingToLoad = new HashSet<string>(256);

	[OnEnterPlay_SetNull]
	private static string[] _propCosmeticIds_uniqueArray;

	[OnEnterPlay_SetNull]
	private static GameObject _fallbackPrefabInstance;

	private const int _k_decoyInitialCountPerPropLine = 10;

	[OnEnterPlay_SetNull]
	private static Transform _decoyTemplatesParent;

	[OnEnterPlay_SetNull]
	private static Transform _decoyInactivePropsParent;

	[OnEnterPlay_Set(0)]
	private static int _debug_decoyMaxCountPerProp;

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, int> _cosmeticId_to_decoyInitialCount = new Dictionary<string, int>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, PropPlacementRB> _cosmeticId_to_decoyTemplate = new Dictionary<string, PropPlacementRB>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, Queue<PropPlacementRB>> _cosmeticId_to_inactiveDecoys = new Dictionary<string, Queue<PropPlacementRB>>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<PropPlacementRB, string> _activeDecoy_to_cosmeticId = new Dictionary<PropPlacementRB, string>(256);

	private const int _k_initialCountPerTaggable = 2;

	[OnEnterPlay_SetNull]
	private static Transform _taggableTemplatesParent;

	[OnEnterPlay_SetNull]
	private static Transform _taggableInactivePropsParent;

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, PropHuntTaggableProp> _cosmeticId_to_taggableTemplate = new Dictionary<string, PropHuntTaggableProp>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, Queue<PropHuntTaggableProp>> _cosmeticId_to_inactiveTaggables = new Dictionary<string, Queue<PropHuntTaggableProp>>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<PropHuntTaggableProp, string> _activeTaggable_to_cosmeticId = new Dictionary<PropHuntTaggableProp, string>(256);

	private const int _k_initialCountPerFollower = 1;

	[OnEnterPlay_SetNull]
	private static Transform _grabbableTemplatesParent;

	[OnEnterPlay_SetNull]
	private static Transform _grabbableInactivePropsParent;

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, PropHuntGrabbableProp> _cosmeticId_to_grabbableTemplate = new Dictionary<string, PropHuntGrabbableProp>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, Queue<PropHuntGrabbableProp>> _cosmeticId_to_inactiveGrabbables = new Dictionary<string, Queue<PropHuntGrabbableProp>>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<PropHuntGrabbableProp, string> _activeGrabbable_to_cosmeticId = new Dictionary<PropHuntGrabbableProp, string>(256);

	[OnEnterPlay_Clear]
	private static readonly List<MeshFilter> _temp_meshFilters = new List<MeshFilter>(8);

	public static EState State => _state;

	public static bool IsReady => State == EState.Ready;

	public static string[] AllPropCosmeticIds => _allPropCosmeticIds;

	public static void StartInitializingPropsList(AllCosmeticsArraySO allCosmeticsArraySO, CosmeticSO fallbackCosmeticSO)
	{
		if (_state == EState.None)
		{
			_ResetPool(_cosmeticId_to_decoyTemplate, _cosmeticId_to_inactiveDecoys, _activeDecoy_to_cosmeticId);
			_ResetPool(_cosmeticId_to_grabbableTemplate, _cosmeticId_to_inactiveGrabbables, _activeGrabbable_to_cosmeticId);
			_ResetPool(_cosmeticId_to_taggableTemplate, _cosmeticId_to_inactiveTaggables, _activeTaggable_to_cosmeticId);
			_propCosmeticIdsWaitingToLoad.Clear();
			_allCosmeticsArraySO = allCosmeticsArraySO;
			_CreateInactivePropsParent(ref _decoyTemplatesParent, "_decoyTemplatesParent");
			_CreateInactivePropsParent(ref _decoyInactivePropsParent, "_decoyInactivePropsParent");
			_CreateInactivePropsParent(ref _grabbableTemplatesParent, "_grabbableTemplatesParent");
			_CreateInactivePropsParent(ref _grabbableInactivePropsParent, "_grabbableInactivePropsParent");
			_CreateInactivePropsParent(ref _taggableTemplatesParent, "_taggableTemplatesParent");
			_CreateInactivePropsParent(ref _taggableInactivePropsParent, "_taggableInactivePropsParent");
			_state = EState.WaitingForTitleData;
			_fallbackProp_cosmeticSO = fallbackCosmeticSO;
			PropHuntPools_Callbacks.instance.ListenForZoneChanged();
			PlayFabTitleDataCache.Instance.GetTitleData("PropHuntProps", _HandleOnTitleDataPropsListLoaded, delegate(PlayFabError e)
			{
				Debug.LogError("ERROR!!!  PropHuntPools: Falling back to interal list because title data with \"PropHuntProps\" failed to load. Error: " + e);
				_HandleOnTitleDataPropsListLoaded("\r\nLMAOY.\r\nLMAKL.\r\nLHABU.\r\nLFAEA.\r\nLMAHO.\r\nLMACQ.\r\nLFAFI.\r\nLHADA.\r\nLMALH.\r\nLFAFA.\r\nLHAED.\r\nLHAHD.\r\nLMAQW.\r\nLFAAW.\r\nLHAFS.\r\nLFABK.\r\nLMAMC.\r\nLMAOX.\r\nLHAFP.\r\nLHACO.\r\nLMAIG.\r\nLMAQG.\r\nLFADC.\r\nLMALL.\r\nLFACA.\r\nLMALO.\r\nLHAFK.\r\nLHAGK.\r\nLMAMI.\r\nLBAAP.\r\nLHAIB.\r\nLHACD.\r\nLFACF.\r\nLMAED.\r\nLMACL.\r\nLHABP.\r\nLMAER.\r\nLMAMR.\r\nLHAFD.\r\nLHACH.\r\nLHADQ.\r\nLMAOP.\r\nLMALY.\r\nLMAHD.\r\nLHAAZ.\r\nLHAGJ.\r\nLMACI.\r\nLFAAO.\r\nLHAEX.\r\nLFAFW.\r\nLMAFR.\r\nLFABC.\r\nLMAKR.\r\nLHAAH.\r\nLHABD.\r\nLHAAB.\r\nLMAKJ.\r\nLHABB.\r\nLHAEN.\r\nLMAGK.\r\nLMAGZ.\r\nLHADR.\r\nLHAFB.\r\nLMAOD.\r\nLHAHT.\r\nLFAFQ.\r\nLFAFP.\r\nLMAOQ.\r\nLMALZ.\r\nLBAAV.\r\nLHAFM.\r\nLHACT.\r\nLFACN.\r\nLHAGH.\r\nLMAJH.\r\nLHADG.\r\nLMAPH.\r\nLHAIU.\r\nLHADW.\r\nLHACJ.\r\nLMAFH.\r\nLHACP.\r\nLFABJ.\r\nLMAQE.\r\nLHAAE.\r\nLMAGW.\r\nLHAFV.\r\nLMABS.\r\nLMADN.\r\nLHAAR.\r\nLMAOL.\r\nLMABM.\r\nLHAEK.\r\nLHAJG.\r\nLFACD.\r\nLHAIE.\r\nLMAPQ.\r\nLFAEZ.\r\nLFABE.\r\nLMAJI.\r\nLFAAU.\r\nLHACX.\r\nLHADP.\r\nLMAGI.\r\nLMAAK.\r\nLFAAJ.\r\nLMACP.\r\nLFADR.\r\nLMAEK.\r\nLMACT.\r\nLMAJS.\r\nLMADH.\r\nLMALI.\r\nLMAEG.\r\nLMAKE.\r\nLMALE.\r\nLHACW.\r\nLMAGS.\r\nLFAEM.\r\nLHAEA.\r\nLHAFU.\r\nLHADL.\r\nLMAGR.\r\nLHAAD.\r\nLHAAU.\r\nLMAFD.\r\nLHAIF.\r\nLHACZ.\r\nLHAIJ.\r\nLHABV.\r\nLMAEM.\r\nLMAKF.\r\nLHAII.\r\nLMALU.\r\nLFAAF.\r\nLHAHR.\r\nLHABJ.\r\nLMAGJ.\r\nLMAHA.\r\nLHAHA.\r\nLFACE.\r\nLMAQR.\r\nLMABK.\r\nLHAGQ.\r\nLHABQ.\r\nLHAEQ.\r\nLMAKD.\r\nLFAHA.\r\nLHAGS.\r\nLMAFF.\r\nLMACW.\r\n");
			});
		}
	}

	private static void _ResetPool<T>(Dictionary<string, T> cosmeticId_to_propTemplate, Dictionary<string, Queue<T>> cosmeticId_to_inactiveProps, Dictionary<T, string> activeProp_to_cosmeticId) where T : Component
	{
		foreach (T value in cosmeticId_to_propTemplate.Values)
		{
			if (value != null)
			{
				UnityEngine.Object.Destroy(value);
			}
		}
		cosmeticId_to_propTemplate.Clear();
		foreach (Queue<T> value2 in cosmeticId_to_inactiveProps.Values)
		{
			foreach (T item in value2)
			{
				if (item != null)
				{
					UnityEngine.Object.Destroy(item.gameObject);
				}
			}
		}
		cosmeticId_to_inactiveProps.Clear();
		foreach (T key in activeProp_to_cosmeticId.Keys)
		{
			if (key != null)
			{
				UnityEngine.Object.Destroy(key.gameObject);
			}
		}
		activeProp_to_cosmeticId.Clear();
	}

	private static void _CreateInactivePropsParent(ref Transform _inactivePropsParent, string name)
	{
		if (_inactivePropsParent != null)
		{
			UnityEngine.Object.Destroy(_inactivePropsParent.gameObject);
		}
		_inactivePropsParent = new GameObject("__PropHunt_-_" + name + "__").transform;
		_inactivePropsParent.gameObject.SetActive(value: false);
		UnityEngine.Object.DontDestroyOnLoad(_inactivePropsParent);
		_inactivePropsParent.gameObject.isStatic = true;
	}

	private static void _HandleOnTitleDataPropsListLoaded(string titleDataPropsString)
	{
		_allPropCosmeticIds = titleDataPropsString.Split(_g_ph_titleDataSeparators, StringSplitOptions.RemoveEmptyEntries);
		_propCosmeticIdsWaitingToLoad.UnionWith(_allPropCosmeticIds);
		string playFabID = _fallbackProp_cosmeticSO.info.playFabID;
		_propCosmeticIdsWaitingToLoad.Add(playFabID);
		int num = 0;
		_propCosmeticIds_uniqueArray = new string[_propCosmeticIdsWaitingToLoad.Count];
		foreach (string item in _propCosmeticIdsWaitingToLoad)
		{
			int num2 = 0;
			for (int i = 0; i < _allPropCosmeticIds.Length; i++)
			{
				num2 += ((item == _allPropCosmeticIds[i]) ? 1 : 0);
			}
			_cosmeticId_to_decoyInitialCount[item] = num2 * 10;
			_propCosmeticIds_uniqueArray[num] = item;
			num++;
		}
		AsyncOperationHandle<GameObject> handle = _fallbackProp_cosmeticSO.info.wardrobeParts[0].prefabAssetRef.InstantiateAsync(_decoyTemplatesParent);
		handle.WaitForCompletion();
		_HandleOnPropTemplateLoaded(handle, playFabID, _fallbackProp_cosmeticSO);
		_state_isTitleDataLoaded = true;
		_state_hasLocalPlayerVisitedBayou |= VRRigCache.Instance != null && VRRigCache.Instance.localRig != null && VRRigCache.Instance.localRig.Rig.zoneEntity.currentZone == GTZone.bayou;
		if (_state_hasLocalPlayerVisitedBayou)
		{
			StartCreatingPools();
		}
		else
		{
			_state = EState.WaitingForLocalPlayerToVisitBayou;
		}
	}

	internal static void OnLocalPlayerEnteredBayou()
	{
		if (!_state_hasLocalPlayerVisitedBayou)
		{
			_state_hasLocalPlayerVisitedBayou = true;
			if (_state_isTitleDataLoaded)
			{
				StartCreatingPools();
			}
		}
	}

	internal static void StartCreatingPools()
	{
		_state = EState.SpawningProps;
		for (int i = 0; i < _propCosmeticIds_uniqueArray.Length; i++)
		{
			string cosmeticId = _propCosmeticIds_uniqueArray[i];
			CosmeticSO cosmeticSO = _allCosmeticsArraySO.SearchForCosmeticSO(cosmeticId);
			propCosmeticId_to_cosmeticSO[cosmeticId] = cosmeticSO;
			if (cosmeticSO == null)
			{
				Debug.LogError("ERROR!!!  PropHuntPools: CosmeticId \"" + cosmeticId + "\" from title data does not exist in AllCosmeticsArraySO. Using backup \"" + _fallbackProp_cosmeticSO.name + "\" instead.");
				cosmeticSO = _fallbackProp_cosmeticSO;
			}
			else
			{
				CosmeticPart[] wardrobeParts = cosmeticSO.info.wardrobeParts;
				if (wardrobeParts == null || wardrobeParts.Length <= 0)
				{
					Debug.LogError("ERROR!!!  PropHuntPools: Prop \"" + cosmeticSO.name + "\" has no wardrobeParts. Using backup \"" + _fallbackProp_cosmeticSO.name + "\" instead.");
					cosmeticSO = _fallbackProp_cosmeticSO;
				}
			}
			GTAssetRef<GameObject> prefabAssetRef = cosmeticSO.info.wardrobeParts[0].prefabAssetRef;
			if (!_cosmeticId_to_inactiveDecoys.TryGetValue(cosmeticId, out var value))
			{
				value = new Queue<PropPlacementRB>(10);
				_cosmeticId_to_inactiveDecoys[cosmeticId] = value;
				AsyncOperationHandle<GameObject> asyncOperationHandle = prefabAssetRef.InstantiateAsync(_taggableTemplatesParent);
				asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> handle)
				{
					_HandleOnPropTemplateLoaded(handle, cosmeticId, cosmeticSO);
				};
			}
		}
	}

	private static void _HandleOnPropTemplateLoaded(AsyncOperationHandle<GameObject> handle, string cosmeticId, CosmeticSO cosmeticSO)
	{
		bool flag = cosmeticSO == _fallbackProp_cosmeticSO;
		if (handle.Status != AsyncOperationStatus.Succeeded)
		{
			Debug.LogError("ERROR!!!  PropHuntPools: " + $"Failed to load asset for pooling: {cosmeticSO.name} with error: {handle.OperationException}", cosmeticSO);
			return;
		}
		GameObject gameObject = handle.Result;
		if (gameObject == null)
		{
			Debug.LogError("ERROR!!!  PropHuntPools: (should never happen) Failed to load asset prop from CosmeticSO \"" + cosmeticSO.name + "\" for pooling but while async op was successful, the resulting GameObject was null!", cosmeticSO);
			return;
		}
		gameObject.SetActive(value: false);
		gameObject.name = "PropHunt_Prop" + cosmeticSO.name;
		gameObject.layer = 14;
		_temp_meshFilters.Clear();
		Component[] componentsInChildren = gameObject.GetComponentsInChildren<Component>(includeInactive: true);
		foreach (Component component in componentsInChildren)
		{
			if (component is Transform transform)
			{
				transform.gameObject.isStatic = false;
			}
			else if (component is MeshRenderer meshRenderer)
			{
				if (meshRenderer.enabled)
				{
					MeshFilter component2 = meshRenderer.GetComponent<MeshFilter>();
					if ((object)component2 != null)
					{
						_temp_meshFilters.Add(component2);
					}
				}
				else
				{
					UnityEngine.Object.Destroy(meshRenderer);
				}
			}
			else
			{
				UnityEngine.Object.Destroy(component);
			}
		}
		if (_temp_meshFilters.Count == 0)
		{
			gameObject = _fallbackPrefabInstance;
		}
		List<Transform> list = new List<Transform>(gameObject.GetComponentsInChildren<Transform>(includeInactive: true));
		list.Sort((Transform a, Transform b) => -a.GetDepth().CompareTo(b.GetDepth()));
		Transform transform2 = gameObject.transform;
		for (int num = 0; num < list.Count; num++)
		{
			Transform transform3 = list[num];
			if (transform3.childCount == 0 && !(transform3 == transform2))
			{
				Component[] components = transform3.GetComponents<Component>();
				int num2 = 0;
				for (int num3 = 0; num3 < components.Length; num3++)
				{
					int num4 = num2;
					Component component3 = components[num3];
					num2 = num4 + ((!(component3 is Transform) && !(component3 is MeshRenderer) && !(component3 is MeshFilter)) ? 1 : 0);
				}
				if (num2 == 0)
				{
					UnityEngine.Object.Destroy(transform3.gameObject);
				}
			}
		}
		if (flag && _fallbackPrefabInstance == null)
		{
			_fallbackPrefabInstance = UnityEngine.Object.Instantiate(gameObject);
		}
		if (!_cosmeticId_to_decoyTemplate.ContainsKey(cosmeticId))
		{
			PropPlacementRB propPlacementRB = UnityEngine.Object.Instantiate(GorillaPropHuntGameManager.instance.PropDecoyPrefab, _decoyTemplatesParent);
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, _decoyTemplatesParent);
			propPlacementRB.name = "__PropHuntPoolProp_Decoy_TEMPLATE__" + cosmeticSO.name + "__";
			PropPlacementRB.TryPrepPropTemplate(propPlacementRB, gameObject2, cosmeticSO);
			propPlacementRB.gameObject.SetActive(value: false);
			gameObject2.transform.SetParent(propPlacementRB.transform);
			_cosmeticId_to_decoyTemplate[cosmeticId] = propPlacementRB;
			int num5 = _cosmeticId_to_decoyInitialCount[cosmeticId];
			Queue<PropPlacementRB> queue = new Queue<PropPlacementRB>(num5);
			string name = "__PropHuntPoolProp_Decoy__" + cosmeticSO.name + "__";
			for (int num6 = 0; num6 < num5; num6++)
			{
				PropPlacementRB propPlacementRB2 = UnityEngine.Object.Instantiate(propPlacementRB, _decoyInactivePropsParent);
				propPlacementRB2.name = name;
				queue.Enqueue(propPlacementRB2);
			}
			_cosmeticId_to_inactiveDecoys[cosmeticId] = queue;
		}
		if (!_cosmeticId_to_grabbableTemplate.ContainsKey(cosmeticId))
		{
			GameObject prop = UnityEngine.Object.Instantiate(gameObject, _grabbableTemplatesParent);
			List<MeshCollider> colliders = new List<MeshCollider>();
			List<InteractionPoint> ref_interactionPoints = new List<InteractionPoint>();
			PropHuntHandFollower.TryPrepPropTemplate(prop, _isLocal: true, cosmeticSO, colliders, ref_interactionPoints, out var grabbableProp, out var _);
			grabbableProp.name = "GrabbableProp_Template_" + cosmeticSO.name;
			_cosmeticId_to_grabbableTemplate[cosmeticId] = grabbableProp;
			Queue<PropHuntGrabbableProp> queue2 = new Queue<PropHuntGrabbableProp>(1);
			string name2 = "__PropHuntPoolProp_Grabbable__" + cosmeticSO.name + "__";
			for (int num7 = 0; num7 < 1; num7++)
			{
				GameObject gameObject3 = UnityEngine.Object.Instantiate(grabbableProp.gameObject, _grabbableInactivePropsParent);
				gameObject3.name = name2;
				queue2.Enqueue(gameObject3.GetComponent<PropHuntGrabbableProp>());
			}
			_cosmeticId_to_inactiveGrabbables[cosmeticId] = queue2;
		}
		if (!_cosmeticId_to_taggableTemplate.ContainsKey(cosmeticId))
		{
			GameObject prop2 = gameObject;
			gameObject = null;
			List<MeshCollider> colliders2 = new List<MeshCollider>();
			List<InteractionPoint> ref_interactionPoints2 = new List<InteractionPoint>();
			PropHuntHandFollower.TryPrepPropTemplate(prop2, _isLocal: false, cosmeticSO, colliders2, ref_interactionPoints2, out var _, out var taggableProp2);
			taggableProp2.name = "__PropHuntPoolProp_Taggable_TEMPLATE__" + cosmeticSO.name + "__";
			_cosmeticId_to_taggableTemplate[cosmeticId] = taggableProp2;
			Queue<PropHuntTaggableProp> queue3 = new Queue<PropHuntTaggableProp>(2);
			string name3 = "__PropHuntPoolProp_Taggable__" + cosmeticSO.name + "__";
			for (int num8 = 0; num8 < 2; num8++)
			{
				GameObject gameObject4 = UnityEngine.Object.Instantiate(taggableProp2.gameObject, _taggableInactivePropsParent);
				gameObject4.name = name3;
				queue3.Enqueue(gameObject4.GetComponent<PropHuntTaggableProp>());
			}
			_cosmeticId_to_inactiveTaggables[cosmeticId] = queue3;
		}
		_propCosmeticIdsWaitingToLoad.Remove(cosmeticId);
		if (_propCosmeticIdsWaitingToLoad.Count == 0)
		{
			_state = EState.Ready;
			OnReady?.Invoke();
		}
	}

	public static bool TryGetDecoyProp(string cosmeticId, out PropPlacementRB out_prop)
	{
		if (!IsReady)
		{
			Debug.LogError("ERROR!!!  PropHuntPools:  TryGetDecoyProp: Cannot get because `PropHuntPools.IsReady` is not true yet!");
			out_prop = null;
			return false;
		}
		if (_cosmeticId_to_inactiveDecoys.TryGetValue(cosmeticId, out var value))
		{
			if (value.Count > 0)
			{
				out_prop = value.Dequeue();
				out_prop.transform.SetParent(null);
				out_prop.gameObject.SetActive(value: true);
				_activeDecoy_to_cosmeticId[out_prop] = cosmeticId;
				return true;
			}
			if (_cosmeticId_to_decoyTemplate.TryGetValue(cosmeticId, out var value2))
			{
				int b = ++_cosmeticId_to_decoyInitialCount[cosmeticId];
				_debug_decoyMaxCountPerProp = Mathf.Max(_debug_decoyMaxCountPerProp, b);
				out_prop = UnityEngine.Object.Instantiate(value2);
				_activeDecoy_to_cosmeticId[out_prop] = cosmeticId;
				return true;
			}
			out_prop = null;
			return false;
		}
		Debug.LogError("ERROR!!!  PropHuntPools: (should never happen) Prop does not exist with cosmeticId \"" + cosmeticId + "\"!");
		out_prop = null;
		return false;
	}

	public static bool TryGetTaggableProp(string cosmeticId, out PropHuntTaggableProp out_prop)
	{
		if (!IsReady)
		{
			Debug.LogError("ERROR!!!  PropHuntPools: TryGetTaggableProp: Cannot get because `PropHuntPools.IsReady` is not true yet!");
			out_prop = null;
			return false;
		}
		if (_cosmeticId_to_inactiveTaggables.TryGetValue(cosmeticId, out var value))
		{
			if (value.Count > 0)
			{
				out_prop = value.Dequeue();
				out_prop.transform.SetParent(null);
				out_prop.gameObject.SetActive(value: true);
				_activeTaggable_to_cosmeticId[out_prop] = cosmeticId;
				return true;
			}
			if (_cosmeticId_to_taggableTemplate.TryGetValue(cosmeticId, out var value2))
			{
				_debug_decoyMaxCountPerProp = ((_debug_decoyMaxCountPerProp >= value.Count + 1) ? _debug_decoyMaxCountPerProp : ((int)((double)value.Count * 1.5)));
				out_prop = UnityEngine.Object.Instantiate(value2);
				_activeTaggable_to_cosmeticId[out_prop] = cosmeticId;
				return true;
			}
		}
		Debug.LogError("ERROR!!!  PropHuntPools: Prop does not exist with cosmeticId \"" + cosmeticId + "\"!");
		out_prop = null;
		return false;
	}

	public static bool TryGetGrabbableProp(string cosmeticId, out PropHuntGrabbableProp out_prop)
	{
		if (!IsReady)
		{
			Debug.LogError("ERROR!!!  PropHuntPools:  TryGetGrabbableProp: Called before pools are ready.");
			out_prop = null;
			return false;
		}
		if (_cosmeticId_to_inactiveGrabbables.TryGetValue(cosmeticId, out var value) && value.Count > 0)
		{
			out_prop = value.Dequeue();
			out_prop.transform.SetParent(null);
			out_prop.gameObject.SetActive(value: true);
			_activeGrabbable_to_cosmeticId[out_prop] = cosmeticId;
			return true;
		}
		if (_cosmeticId_to_grabbableTemplate.TryGetValue(cosmeticId, out var value2))
		{
			out_prop = UnityEngine.Object.Instantiate(value2);
			_activeGrabbable_to_cosmeticId[out_prop] = cosmeticId;
			return true;
		}
		Debug.LogError("ERROR!!!  PropHuntPools: Prop does not exist with cosmeticId \"" + cosmeticId + "\"!");
		out_prop = null;
		return false;
	}

	public static void ReturnDecoyProp(PropPlacementRB prop)
	{
		string value;
		Queue<PropPlacementRB> value2;
		if (prop == null)
		{
			Debug.LogError("ERROR!!!  PropHuntPools: Tried to return a prop but it was null!");
		}
		else if (_activeDecoy_to_cosmeticId.TryGetValue(prop, out value) && _cosmeticId_to_inactiveDecoys.TryGetValue(value, out value2))
		{
			prop.gameObject.SetActive(value: false);
			prop.transform.SetParent(_grabbableInactivePropsParent);
			value2.Enqueue(prop);
			_activeDecoy_to_cosmeticId.Remove(prop);
		}
	}

	public static void ReturnTaggableProp(PropHuntTaggableProp prop)
	{
		string value;
		Queue<PropHuntTaggableProp> value2;
		if (prop == null)
		{
			Debug.LogError("ERROR!!!  PropHuntPools: Tried to return a prop but it was null!");
		}
		else if (_activeTaggable_to_cosmeticId.TryGetValue(prop, out value) && _cosmeticId_to_inactiveTaggables.TryGetValue(value, out value2))
		{
			prop.gameObject.SetActive(value: false);
			prop.transform.SetParent(_grabbableInactivePropsParent);
			value2.Enqueue(prop);
			_activeTaggable_to_cosmeticId.Remove(prop);
		}
	}

	public static void ReturnGrabbableProp(PropHuntGrabbableProp prop)
	{
		if (!(prop == null) && _activeGrabbable_to_cosmeticId.TryGetValue(prop, out var value) && _cosmeticId_to_inactiveGrabbables.TryGetValue(value, out var value2))
		{
			prop.gameObject.SetActive(value: false);
			prop.transform.SetParent(_grabbableInactivePropsParent);
			value2.Enqueue(prop);
			_activeGrabbable_to_cosmeticId.Remove(prop);
		}
	}
}
