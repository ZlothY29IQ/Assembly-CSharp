using System;
using System.Collections.Generic;
using System.Diagnostics;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.UI;

namespace GorillaTag;

[DefaultExecutionOrder(2000)]
public class StaticLodManager : MonoBehaviour, IGorillaSliceableSimple
{
	private struct GroupInfo
	{
		public bool isLoaded;

		public bool componentEnabled;

		public Vector3 center;

		public float radiusSq;

		public Bounds bounds;

		public bool uiEnabled;

		public float uiEnableDistanceSq;

		public Graphic[] uiGraphics;

		public Renderer[] renderers;

		public bool collidersEnabled;

		public float collisionEnableDistanceSq;

		public Collider[] interactableColliders;
	}

	private delegate Bounds _GetBoundsDelegate<in T>(T t) where T : Component;

	[OnEnterPlay_Clear]
	private static readonly List<StaticLodGroup> groupMonoBehaviours = new List<StaticLodGroup>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<int, int> _groupInstId_to_index = new Dictionary<int, int>(256);

	[DebugReadout]
	[OnEnterPlay_Clear]
	private static readonly List<GroupInfo> groupInfos = new List<GroupInfo>(256);

	[OnEnterPlay_Clear]
	private static readonly Stack<int> freeSlots = new Stack<int>();

	private Camera mainCamera;

	private bool hasMainCamera;

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		mainCamera = Camera.main;
		hasMainCamera = mainCamera != null;
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public static int Register(StaticLodGroup lodGroup)
	{
		if (lodGroup == null)
		{
			return -1;
		}
		if (freeSlots.TryPop(out var result))
		{
			groupMonoBehaviours[result] = lodGroup;
			groupInfos[result] = default(GroupInfo);
		}
		else
		{
			result = groupMonoBehaviours.Count;
			groupMonoBehaviours.Add(lodGroup);
			groupInfos.Add(default(GroupInfo));
		}
		_groupInstId_to_index[lodGroup.GetInstanceID()] = result;
		GroupInfo value = groupInfos[result];
		value.isLoaded = true;
		value.componentEnabled = lodGroup.isActiveAndEnabled;
		value.uiEnabled = true;
		value.collidersEnabled = true;
		value.uiEnableDistanceSq = lodGroup.uiFadeDistanceMax * lodGroup.uiFadeDistanceMax;
		value.collisionEnableDistanceSq = lodGroup.collisionEnableDistance * lodGroup.collisionEnableDistance;
		groupInfos[result] = value;
		_TryAddMembersToLodGroup(isNew: true, result);
		value = groupInfos[result];
		if (Mathf.Approximately(value.radiusSq, 0f))
		{
			value.bounds = new Bounds(lodGroup.transform.position, Vector3.one * 0.01f);
			value.center = value.bounds.center;
			value.radiusSq = value.bounds.extents.sqrMagnitude;
			groupInfos[result] = value;
		}
		return result;
	}

	public static int OldRegister(StaticLodGroup lodGroup)
	{
		StaticLodGroupExcluder componentInParent = lodGroup.GetComponentInParent<StaticLodGroupExcluder>();
		List<Graphic> pooledList;
		using (((Component)lodGroup).GTGetComponentsListPool(true, out pooledList))
		{
			for (int num = pooledList.Count - 1; num >= 0; num--)
			{
				StaticLodGroupExcluder componentInParent2 = pooledList[num].GetComponentInParent<StaticLodGroupExcluder>(includeInactive: true);
				if (componentInParent2 != null && componentInParent2 != componentInParent)
				{
					pooledList.RemoveAt(num);
				}
			}
			Graphic[] array = pooledList.ToArray();
			List<Renderer> pooledList2;
			using (((Component)lodGroup).GTGetComponentsListPool(true, out pooledList2))
			{
				for (int num2 = pooledList2.Count - 1; num2 >= 0; num2--)
				{
					int layer = pooledList2[num2].gameObject.layer;
					if ((layer != 5 && layer != 18) || !pooledList2[num2].enabled)
					{
						pooledList2.RemoveAt(num2);
					}
					else
					{
						StaticLodGroupExcluder componentInParent3 = pooledList[num2].GetComponentInParent<StaticLodGroupExcluder>(includeInactive: true);
						if (componentInParent3 != null && componentInParent3 != componentInParent)
						{
							pooledList2.RemoveAt(num2);
						}
					}
				}
				Renderer[] array2 = pooledList2.ToArray();
				List<Collider> pooledList3;
				using (((Component)lodGroup).GTGetComponentsListPool(true, out pooledList3))
				{
					for (int i = 0; i < pooledList3.Count; i++)
					{
						Collider collider = pooledList3[i];
						if (!collider.gameObject.IsOnLayer(UnityLayer.GorillaInteractable))
						{
							pooledList3.RemoveAt(i);
							continue;
						}
						StaticLodGroupExcluder componentInParent4 = collider.GetComponentInParent<StaticLodGroupExcluder>();
						if (componentInParent4 != null && componentInParent4 != componentInParent)
						{
							pooledList3.RemoveAt(i);
						}
					}
					Collider[] array3 = pooledList3.ToArray();
					Bounds bounds = ((array2.Length != 0) ? array2[0].bounds : ((array3.Length != 0) ? array3[0].bounds : ((array.Length != 0) ? new Bounds(array[0].transform.position, Vector3.one * 0.01f) : new Bounds(lodGroup.transform.position, Vector3.one * 0.01f))));
					for (int j = 0; j < array.Length; j++)
					{
						bounds.Encapsulate(array[j].transform.position);
					}
					for (int k = 0; k < array2.Length; k++)
					{
						bounds.Encapsulate(array2[k].bounds);
					}
					for (int l = 0; l < array3.Length; l++)
					{
						bounds.Encapsulate(array3[l].bounds);
					}
					GroupInfo groupInfo = new GroupInfo
					{
						isLoaded = true,
						componentEnabled = lodGroup.isActiveAndEnabled,
						center = bounds.center,
						radiusSq = bounds.extents.sqrMagnitude,
						uiEnabled = true,
						uiEnableDistanceSq = lodGroup.uiFadeDistanceMax * lodGroup.uiFadeDistanceMax,
						uiGraphics = array,
						renderers = array2,
						collidersEnabled = true,
						collisionEnableDistanceSq = lodGroup.collisionEnableDistance * lodGroup.collisionEnableDistance,
						interactableColliders = array3
					};
					if (freeSlots.TryPop(out var result))
					{
						groupMonoBehaviours[result] = lodGroup;
						groupInfos[result] = groupInfo;
					}
					else
					{
						result = groupMonoBehaviours.Count;
						groupMonoBehaviours.Add(lodGroup);
						groupInfos.Add(groupInfo);
					}
					_groupInstId_to_index[lodGroup.GetInstanceID()] = result;
					return result;
				}
			}
		}
	}

	public static void Unregister(int lodGroupIndex)
	{
		StaticLodGroup staticLodGroup = groupMonoBehaviours[lodGroupIndex];
		if (staticLodGroup != null)
		{
			_groupInstId_to_index.Remove(staticLodGroup.GetInstanceID());
		}
		groupMonoBehaviours[lodGroupIndex] = null;
		groupInfos[lodGroupIndex] = default(GroupInfo);
		freeSlots.Push(lodGroupIndex);
	}

	public static bool TryAddLateInstantiatedMembers(GameObject root)
	{
		StaticLodGroup componentInParent = root.GetComponentInParent<StaticLodGroup>(includeInactive: true);
		if (componentInParent == null)
		{
			return false;
		}
		if (!_groupInstId_to_index.TryGetValue(componentInParent.GetInstanceID(), out var value))
		{
			return false;
		}
		if (componentInParent.gameObject != root)
		{
			StaticLodGroupExcluder componentInParent2 = root.GetComponentInParent<StaticLodGroupExcluder>(includeInactive: true);
			if (componentInParent2 != null && componentInParent.transform.GetDepth() < componentInParent2.transform.GetDepth())
			{
				return false;
			}
		}
		return _TryAddMembersToLodGroup(isNew: false, value);
	}

	private static bool _TryAddMembersToLodGroup(bool isNew, int groupIndex)
	{
		StaticLodGroup lodGroup = groupMonoBehaviours[groupIndex];
		GroupInfo ref_groupInfo = groupInfos[groupIndex];
		int result = (int)(0u | (_TryAddComponentsToGroup(lodGroup, ref ref_groupInfo, ref ref_groupInfo.interactableColliders, (Collider coll) => coll.gameObject.IsOnLayer(UnityLayer.GorillaInteractable), (Collider coll) => coll.bounds) ? 1u : 0u) | (_TryAddComponentsToGroup(lodGroup, ref ref_groupInfo, ref ref_groupInfo.renderers, delegate(Renderer rend)
		{
			int layer = rend.gameObject.layer;
			return (layer == 5 || layer == 18) && rend.enabled;
		}, (Renderer rend) => rend.bounds) ? 1u : 0u)) | (_TryAddComponentsToGroup(lodGroup, ref ref_groupInfo, ref ref_groupInfo.uiGraphics, (Graphic _) => true, (Graphic gfx) => new Bounds(gfx.transform.position, Vector3.one * 0.01f)) ? 1 : 0);
		groupInfos[groupIndex] = ref_groupInfo;
		return (byte)result != 0;
	}

	private static bool _TryAddComponentsToGroup<T>(StaticLodGroup lodGroup, ref GroupInfo ref_groupInfo, ref T[] ref_components, Predicate<T> includeIf, _GetBoundsDelegate<T> getBounds) where T : Component
	{
		List<T> componentsInChildrenUntil = lodGroup.GetComponentsInChildrenUntil<T, StaticLodGroup, StaticLodGroupExcluder>(includeInactive: true, stopAtRoot: false);
		for (int num = componentsInChildrenUntil.Count - 1; num >= 0; num--)
		{
			if (!includeIf(componentsInChildrenUntil[num]))
			{
				componentsInChildrenUntil.RemoveAt(num);
			}
		}
		if (componentsInChildrenUntil.Count == 0)
		{
			if (ref_components == null)
			{
				ref_components = Array.Empty<T>();
			}
			return false;
		}
		T[] obj = ref_components;
		int num2 = ((obj != null) ? obj.Length : 0);
		if (num2 == 0)
		{
			ref_components = componentsInChildrenUntil.ToArray();
		}
		else
		{
			Array.Resize(ref ref_components, num2 + componentsInChildrenUntil.Count);
			for (int i = num2; i < ref_components.Length; i++)
			{
				ref_components[i] = componentsInChildrenUntil[i - num2];
			}
		}
		if (Mathf.Approximately(ref_groupInfo.radiusSq, 0f))
		{
			ref_groupInfo.bounds = getBounds(ref_components[0]);
		}
		for (int j = num2; j < ref_components.Length; j++)
		{
			ref_groupInfo.bounds.Encapsulate(getBounds(ref_components[j]));
		}
		ref_groupInfo.center = ref_groupInfo.bounds.center;
		ref_groupInfo.radiusSq = ref_groupInfo.bounds.extents.sqrMagnitude;
		return true;
	}

	[Conditional("UNITY_EDITOR")]
	private static void _EdAddPathsToGroup<T>(T[] components, ref string[] ref_edDebugPaths) where T : Component
	{
	}

	public static void SetEnabled(int index, bool enable)
	{
		if (!ApplicationQuittingState.IsQuitting && groupInfos != null && index >= 0 && index < groupInfos.Count)
		{
			GroupInfo value = groupInfos[index];
			value.componentEnabled = enable;
			groupInfos[index] = value;
		}
	}

	public void SliceUpdate()
	{
		if (!hasMainCamera)
		{
			return;
		}
		Vector3 position = mainCamera.transform.position;
		for (int i = 0; i < groupInfos.Count; i++)
		{
			GroupInfo value = groupInfos[i];
			if (!value.isLoaded || !value.componentEnabled)
			{
				continue;
			}
			float num = Mathf.Max(0f, (value.center - position).sqrMagnitude - value.radiusSq);
			float num2 = (value.uiEnabled ? 0.010000001f : 0f);
			bool flag = num < value.uiEnableDistanceSq + num2;
			if (flag != value.uiEnabled)
			{
				for (int j = 0; j < value.uiGraphics.Length; j++)
				{
					Graphic graphic = value.uiGraphics[j];
					if (!(graphic == null))
					{
						graphic.enabled = flag;
					}
				}
				for (int k = 0; k < value.renderers.Length; k++)
				{
					Renderer renderer = value.renderers[k];
					if (!(renderer == null))
					{
						renderer.enabled = flag;
					}
				}
			}
			value.uiEnabled = flag;
			num2 = (value.collidersEnabled ? 0.010000001f : 0f);
			bool flag2 = num < value.collisionEnableDistanceSq + num2;
			if (flag2 != value.collidersEnabled)
			{
				for (int l = 0; l < value.interactableColliders.Length; l++)
				{
					if (!(value.interactableColliders[l] == null))
					{
						value.interactableColliders[l].enabled = flag2;
					}
				}
			}
			value.collidersEnabled = flag2;
			groupInfos[i] = value;
		}
	}
}
