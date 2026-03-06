using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace MTAssets.EasyMeshCombiner;

[AddComponentMenu("MT Assets/Easy Mesh Combiner/Runtime Mesh Combiner")]
public class RuntimeMeshCombiner : MonoBehaviour
{
	private class GameObjectWithMesh
	{
		public GameObject gameObject;

		public MeshFilter meshFilter;

		public MeshRenderer meshRenderer;

		public GameObjectWithMesh(GameObject gameObject, MeshFilter meshFilter, MeshRenderer meshRenderer)
		{
			this.gameObject = gameObject;
			this.meshFilter = meshFilter;
			this.meshRenderer = meshRenderer;
		}
	}

	private class OriginalGameObjectWithMesh
	{
		public GameObject gameObject;

		public bool originalGoState;

		public MeshRenderer meshRenderer;

		public bool originalMrState;

		public OriginalGameObjectWithMesh(GameObject gameObject, bool originalGoState, MeshRenderer meshRenderer, bool originalMrState)
		{
			this.gameObject = gameObject;
			this.originalGoState = originalGoState;
			this.meshRenderer = meshRenderer;
			this.originalMrState = originalMrState;
		}
	}

	private class SubMeshToCombine
	{
		public Transform transform;

		public MeshFilter meshFilter;

		public MeshRenderer meshRenderer;

		public int subMeshIndex;

		public SubMeshToCombine(Transform transform, MeshFilter meshFilter, MeshRenderer meshRenderer, int subMeshIndex)
		{
			this.transform = transform;
			this.meshFilter = meshFilter;
			this.meshRenderer = meshRenderer;
			this.subMeshIndex = subMeshIndex;
		}
	}

	public enum CombineOnStart
	{
		Disabled,
		OnStart,
		OnAwake
	}

	public enum AfterMerge
	{
		DisableOriginalMeshes,
		DeactiveOriginalGameObjects,
		DoNothing
	}

	private int MAX_VERTICES_FOR_16BITS_MESH = 50000;

	private Vector3 originalPosition = Vector3.zero;

	private Vector3 originalEulerAngles = Vector3.zero;

	private Vector3 originalScale = Vector3.zero;

	private List<OriginalGameObjectWithMesh> originalGameObjectsWithMeshToRestore = new List<OriginalGameObjectWithMesh>();

	private bool targetMeshesMerged;

	[HideInInspector]
	public AfterMerge afterMerge;

	[HideInInspector]
	public bool addMeshColliderAfter = true;

	[HideInInspector]
	public CombineOnStart combineMeshesAtStartUp;

	[HideInInspector]
	public bool combineInChildren;

	[HideInInspector]
	public bool combineInactives;

	[HideInInspector]
	public bool recalculateNormals = true;

	[HideInInspector]
	public bool recalculateTangents = true;

	[HideInInspector]
	public bool optimizeResultingMesh;

	[HideInInspector]
	public List<GameObject> targetMeshes = new List<GameObject>();

	[HideInInspector]
	public bool showDebugLogs = true;

	[HideInInspector]
	public bool garbageCollectorAfterUndo = true;

	public UnityEvent onDoneMerge;

	public UnityEvent onDoneUnmerge;

	private void Awake()
	{
		if (combineMeshesAtStartUp == CombineOnStart.OnAwake)
		{
			if (showDebugLogs)
			{
				Debug.Log("The merge started in Runtime Combiner \"" + base.gameObject.name + "\".");
			}
			CombineMeshes();
		}
	}

	private void Start()
	{
		if (combineMeshesAtStartUp == CombineOnStart.OnStart)
		{
			if (showDebugLogs)
			{
				Debug.Log("The merge started in Runtime Combiner \"" + base.gameObject.name + "\".");
			}
			CombineMeshes();
		}
	}

	private GameObjectWithMesh[] GetValidatedTargetGameObjects()
	{
		List<Transform> list = new List<Transform>();
		for (int i = 0; i < targetMeshes.Count; i++)
		{
			if (targetMeshes[i] == null)
			{
				continue;
			}
			if (combineInChildren)
			{
				Transform[] componentsInChildren = targetMeshes[i].GetComponentsInChildren<Transform>(includeInactive: true);
				foreach (Transform item in componentsInChildren)
				{
					if (!list.Contains(item))
					{
						list.Add(item);
					}
				}
			}
			if (!combineInChildren)
			{
				Transform component = targetMeshes[i].GetComponent<Transform>();
				if (!list.Contains(component))
				{
					list.Add(component);
				}
			}
		}
		List<GameObjectWithMesh> list2 = new List<GameObjectWithMesh>();
		for (int k = 0; k < list.Count; k++)
		{
			MeshFilter component2 = list[k].GetComponent<MeshFilter>();
			MeshRenderer component3 = list[k].GetComponent<MeshRenderer>();
			if ((component2 != null || component3 != null) && (combineInactives || component3.enabled) && (combineInactives || list[k].gameObject.activeSelf) && (combineInactives || list[k].gameObject.activeInHierarchy))
			{
				list2.Add(new GameObjectWithMesh(list[k].gameObject, component2, component3));
			}
		}
		List<GameObjectWithMesh> list3 = new List<GameObjectWithMesh>();
		for (int l = 0; l < list2.Count; l++)
		{
			bool flag = true;
			if (list2[l].meshFilter == null)
			{
				if (showDebugLogs)
				{
					Debug.LogError("GameObject \"" + list2[l].gameObject.name + "\" does not have the Mesh Filter component, so it is not a valid mesh and will be ignored in the merge process.");
				}
				flag = false;
			}
			if (list2[l].meshRenderer == null)
			{
				if (showDebugLogs)
				{
					Debug.LogError("GameObject \"" + list2[l].gameObject.name + "\" does not have the Mesh Renderer component, so it is not a valid mesh and will be ignored in the merge process.");
				}
				flag = false;
			}
			if (list2[l].meshFilter != null && list2[l].meshFilter.sharedMesh == null)
			{
				if (showDebugLogs)
				{
					Debug.LogError("GameObject \"" + list2[l].gameObject.name + "\" does not have a Mesh in Mesh Filter component, so it is not a valid mesh and will be ignored in the merge process.");
				}
				flag = false;
			}
			if (list2[l].meshFilter != null && list2[l].meshRenderer != null && list2[l].meshFilter.sharedMesh != null && list2[l].meshFilter.sharedMesh.subMeshCount != list2[l].meshRenderer.sharedMaterials.Length)
			{
				if (showDebugLogs)
				{
					Debug.LogError("The Mesh Renderer component found in GameObject \"" + list2[l].gameObject.name + "\" has more or less material needed. The mesh that is in this GameObject has " + list2[l].meshFilter.sharedMesh.subMeshCount + " submeshes, but has a number of " + list2[l].meshRenderer.sharedMaterials.Length + " materials. This mesh will be ignored during the merge process.");
				}
				flag = false;
			}
			if (list2[l].meshRenderer != null)
			{
				for (int m = 0; m < list2[l].meshRenderer.sharedMaterials.Length; m++)
				{
					if (list2[l].meshRenderer.sharedMaterials[m] == null)
					{
						if (showDebugLogs)
						{
							Debug.LogError("Material " + m + " in Mesh Renderer present in component \"" + list2[l].gameObject.name + "\" is null. For the merge process to work well, all materials must be completed. This GameObject will be ignored in the merge process.");
						}
						flag = false;
					}
				}
			}
			if (list2[l].gameObject.GetComponent<CombinedMeshesManager>() != null)
			{
				if (showDebugLogs)
				{
					Debug.LogError("GameObject \"" + list2[l].gameObject.name + "\" is the result of a previous merge, so it will be ignored by this merge.");
				}
				flag = false;
			}
			if (flag)
			{
				list3.Add(list2[l]);
			}
		}
		return list3.ToArray();
	}

	public bool CombineMeshes()
	{
		if (isTargetMeshesMerged())
		{
			if (showDebugLogs)
			{
				Debug.Log("The Runtime Combiner \"" + base.gameObject.name + "\" meshes are already combined!");
			}
			return true;
		}
		if (!isTargetMeshesMerged())
		{
			if (base.gameObject.GetComponent<MeshFilter>() != null || base.gameObject.GetComponent<MeshRenderer>() != null)
			{
				if (showDebugLogs)
				{
					Debug.LogError("Unable to merge. Apparently the GameObject \"" + base.gameObject.name + "\" already contains the Mesh Filter and/or Mesh Renderer component. The Runtime Mesh Combiner needs a GameObject that does not contain these two components. Please remove them or place the Runtime Mesh Combiner in a new GameObject and try again.");
				}
				return false;
			}
			originalPosition = base.gameObject.transform.position;
			originalEulerAngles = base.gameObject.transform.eulerAngles;
			originalScale = base.gameObject.transform.lossyScale;
			base.gameObject.transform.position = Vector3.zero;
			base.gameObject.transform.eulerAngles = Vector3.zero;
			base.gameObject.transform.localScale = Vector3.one;
			GameObjectWithMesh[] validatedTargetGameObjects = GetValidatedTargetGameObjects();
			if (validatedTargetGameObjects.Length == 0)
			{
				if (showDebugLogs)
				{
					Debug.LogError("No valid, meshed GameObjects were found in the target GameObjects list. Therefore the merge was interrupted.");
				}
				return false;
			}
			Dictionary<Material, List<SubMeshToCombine>> dictionary = new Dictionary<Material, List<SubMeshToCombine>>();
			foreach (GameObjectWithMesh gameObjectWithMesh in validatedTargetGameObjects)
			{
				for (int j = 0; j < gameObjectWithMesh.meshFilter.sharedMesh.subMeshCount; j++)
				{
					Material key = gameObjectWithMesh.meshRenderer.sharedMaterials[j];
					if (dictionary.ContainsKey(key))
					{
						dictionary[key].Add(new SubMeshToCombine(gameObjectWithMesh.gameObject.transform, gameObjectWithMesh.meshFilter, gameObjectWithMesh.meshRenderer, j));
					}
					if (!dictionary.ContainsKey(key))
					{
						dictionary.Add(key, new List<SubMeshToCombine>
						{
							new SubMeshToCombine(gameObjectWithMesh.gameObject.transform, gameObjectWithMesh.meshFilter, gameObjectWithMesh.meshRenderer, j)
						});
					}
				}
			}
			MeshFilter meshFilter = base.gameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
			int num = 0;
			GameObjectWithMesh[] array = validatedTargetGameObjects;
			foreach (GameObjectWithMesh gameObjectWithMesh2 in array)
			{
				num += gameObjectWithMesh2.meshFilter.sharedMesh.vertexCount;
			}
			List<Mesh> list = new List<Mesh>();
			foreach (KeyValuePair<Material, List<SubMeshToCombine>> item in dictionary)
			{
				List<SubMeshToCombine> value = item.Value;
				List<CombineInstance> list2 = new List<CombineInstance>();
				for (int l = 0; l < value.Count; l++)
				{
					list2.Add(new CombineInstance
					{
						mesh = value[l].meshFilter.sharedMesh,
						subMeshIndex = value[l].subMeshIndex,
						transform = value[l].transform.localToWorldMatrix
					});
				}
				Mesh mesh = new Mesh();
				if (num <= MAX_VERTICES_FOR_16BITS_MESH)
				{
					mesh.indexFormat = IndexFormat.UInt16;
				}
				if (num > MAX_VERTICES_FOR_16BITS_MESH)
				{
					mesh.indexFormat = IndexFormat.UInt32;
				}
				mesh.CombineMeshes(list2.ToArray(), mergeSubMeshes: true, useMatrices: true);
				list.Add(mesh);
			}
			List<CombineInstance> list3 = new List<CombineInstance>();
			foreach (Mesh item2 in list)
			{
				list3.Add(new CombineInstance
				{
					mesh = item2,
					subMeshIndex = 0,
					transform = Matrix4x4.identity
				});
			}
			Mesh mesh2 = new Mesh();
			if (num <= MAX_VERTICES_FOR_16BITS_MESH)
			{
				mesh2.indexFormat = IndexFormat.UInt16;
			}
			if (num > MAX_VERTICES_FOR_16BITS_MESH)
			{
				mesh2.indexFormat = IndexFormat.UInt32;
			}
			mesh2.name = base.gameObject.name + " (Temp Merge)";
			mesh2.CombineMeshes(list3.ToArray(), mergeSubMeshes: false);
			mesh2.RecalculateBounds();
			if (recalculateNormals)
			{
				mesh2.RecalculateNormals();
			}
			if (recalculateTangents)
			{
				mesh2.RecalculateTangents();
			}
			if (optimizeResultingMesh)
			{
				mesh2.Optimize();
			}
			meshFilter.sharedMesh = mesh2;
			List<Material> list4 = new List<Material>();
			foreach (KeyValuePair<Material, List<SubMeshToCombine>> item3 in dictionary)
			{
				list4.Add(item3.Key);
			}
			meshRenderer.sharedMaterials = list4.ToArray();
			if (afterMerge == AfterMerge.DeactiveOriginalGameObjects)
			{
				array = validatedTargetGameObjects;
				foreach (GameObjectWithMesh gameObjectWithMesh3 in array)
				{
					originalGameObjectsWithMeshToRestore.Add(new OriginalGameObjectWithMesh(gameObjectWithMesh3.gameObject, gameObjectWithMesh3.gameObject.activeSelf, gameObjectWithMesh3.meshRenderer, gameObjectWithMesh3.meshRenderer.enabled));
					gameObjectWithMesh3.gameObject.SetActive(value: false);
				}
				if (addMeshColliderAfter)
				{
					base.gameObject.AddComponent<MeshCollider>();
				}
			}
			if (afterMerge == AfterMerge.DisableOriginalMeshes)
			{
				array = validatedTargetGameObjects;
				foreach (GameObjectWithMesh gameObjectWithMesh4 in array)
				{
					originalGameObjectsWithMeshToRestore.Add(new OriginalGameObjectWithMesh(gameObjectWithMesh4.gameObject, gameObjectWithMesh4.gameObject.activeSelf, gameObjectWithMesh4.meshRenderer, gameObjectWithMesh4.meshRenderer.enabled));
					gameObjectWithMesh4.meshRenderer.enabled = false;
				}
			}
			_ = afterMerge;
			_ = 2;
			base.gameObject.transform.position = originalPosition;
			base.gameObject.transform.eulerAngles = originalEulerAngles;
			base.gameObject.transform.localScale = originalScale;
			if (showDebugLogs)
			{
				Debug.Log("The merge has been successfully completed in Runtime Combiner \"" + base.gameObject.name + "\"!");
			}
			if (onDoneMerge != null)
			{
				onDoneMerge.Invoke();
			}
			targetMeshesMerged = true;
			return true;
		}
		return false;
	}

	public bool UndoMerge()
	{
		if (!isTargetMeshesMerged())
		{
			if (showDebugLogs)
			{
				Debug.Log("The Runtime Combiner \"" + base.gameObject.name + "\" meshes are already uncombined!");
			}
			return true;
		}
		if (isTargetMeshesMerged())
		{
			if (afterMerge == AfterMerge.DisableOriginalMeshes)
			{
				foreach (OriginalGameObjectWithMesh item in originalGameObjectsWithMeshToRestore)
				{
					if (!(item.meshRenderer == null))
					{
						item.meshRenderer.enabled = item.originalMrState;
					}
				}
			}
			if (afterMerge == AfterMerge.DeactiveOriginalGameObjects)
			{
				foreach (OriginalGameObjectWithMesh item2 in originalGameObjectsWithMeshToRestore)
				{
					if (!(item2.gameObject == null))
					{
						item2.gameObject.SetActive(item2.originalGoState);
					}
				}
				if (addMeshColliderAfter)
				{
					MeshCollider component = GetComponent<MeshCollider>();
					if (component != null)
					{
						UnityEngine.Object.Destroy(component);
					}
				}
			}
			_ = afterMerge;
			_ = 2;
			originalGameObjectsWithMeshToRestore.Clear();
			UnityEngine.Object.Destroy(GetComponent<MeshRenderer>());
			UnityEngine.Object.Destroy(GetComponent<MeshFilter>());
			if (garbageCollectorAfterUndo)
			{
				Resources.UnloadUnusedAssets();
				GC.Collect();
			}
			if (showDebugLogs)
			{
				Debug.Log("The Runtime Combiner \"" + base.gameObject.name + "\" merge was successfully undone!");
			}
			if (onDoneUnmerge != null)
			{
				onDoneUnmerge.Invoke();
			}
			targetMeshesMerged = false;
			return true;
		}
		return false;
	}

	public bool isTargetMeshesMerged()
	{
		return targetMeshesMerged;
	}
}
