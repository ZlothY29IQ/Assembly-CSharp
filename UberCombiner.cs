using System;
using System.Collections.Generic;
using System.Linq;
using GorillaTag.Rendering;
using MTAssets.EasyMeshCombiner;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(RuntimeMeshCombiner))]
public class UberCombiner : MonoBehaviour
{
	[SerializeField]
	private RuntimeMeshCombiner _combiner;

	[Space]
	public GameObject[] meshSources = new GameObject[0];

	[Space]
	public GameObject[] objectsToIgnore = new GameObject[0];

	[NonSerialized]
	[Space]
	private MeshRenderer[] renderersToCombine = new MeshRenderer[0];

	[NonSerialized]
	[Space]
	private List<GameObject> invalidObjects = new List<GameObject>();

	public bool includeInactive;

	private static ShaderHashId _BaseMap = "_BaseMap";

	private void CollectRenderers()
	{
		$"Found {(renderersToCombine = FilterRenderers(meshSources.SelectMany((GameObject g) => g.GetComponentsInChildren<MeshRenderer>(includeInactive)).ToArray()).DistinctBy((MeshRenderer mr) => mr.GetInstanceID()).ToArray()).Length} renderers to combine.".Echo();
	}

	private void ValidateRenderers()
	{
		List<GameObject> list = new List<GameObject>(16);
		for (int i = 0; i < renderersToCombine.Length; i++)
		{
			MeshRenderer meshRenderer = renderersToCombine[i];
			GameObject gameObject = meshRenderer.gameObject;
			string text = gameObject.name;
			MeshFilter component = gameObject.GetComponent<MeshFilter>();
			if (meshRenderer == null || component == null)
			{
				Debug.LogError("Ojbect '" + text + "' is missing a MeshRenderer, MeshFilter, or both.", gameObject);
				list.Add(gameObject);
				continue;
			}
			Mesh sharedMesh = component.sharedMesh;
			if (sharedMesh == null)
			{
				Debug.LogError("MeshFilter for '" + text + "' has no shared mesh.", gameObject);
				list.Add(gameObject);
				continue;
			}
			int subMeshCount = sharedMesh.subMeshCount;
			if (subMeshCount == 0)
			{
				Debug.LogError("Shared mesh for '" + text + "' has 0 submeshes.", gameObject);
				list.Add(gameObject);
				continue;
			}
			if (sharedMesh.vertexCount < 3)
			{
				Debug.LogError("Shared mesh for '" + text + "' has less than 3 vertices.", gameObject);
				list.Add(gameObject);
				continue;
			}
			Material[] sharedMaterials = meshRenderer.sharedMaterials;
			if (sharedMaterials.IsNullOrEmpty())
			{
				Debug.LogError("Object '" + text + "' has null or empty shared materials array.", gameObject);
				list.Add(gameObject);
				continue;
			}
			foreach (Material material in sharedMaterials)
			{
				string text2 = material.name;
				Texture mainTexture = material.mainTexture;
				if (!(mainTexture == null) && mainTexture is RenderTexture)
				{
					Debug.LogError("Object '" + text + "' has material (" + text2 + ") that uses a RenderTexture", gameObject);
					list.Add(gameObject);
					break;
				}
				if (material.HasProperty(_BaseMap))
				{
					Texture texture = material.GetTexture(_BaseMap);
					if (!(texture == null) && texture is RenderTexture)
					{
						Debug.LogError("Object '" + text + "' has material (" + text2 + ") that uses a RenderTexture", gameObject);
						list.Add(gameObject);
						break;
					}
				}
				if (UberShader.IsAnimated(material))
				{
					Debug.LogError("Object '" + text + "' has a material (" + text2 + ") that's animated", gameObject);
					list.Add(gameObject);
					break;
				}
			}
			if (subMeshCount != sharedMaterials.Length)
			{
				Debug.LogError("Object '" + text + "' has mismatched number of materials/submeshes" + $" Submeshes: {subMeshCount} Materials: {sharedMaterials.Length}", gameObject);
				list.Add(gameObject);
			}
		}
		invalidObjects = list.DistinctBy((GameObject g) => g.GetHashCode()).ToList();
	}

	private void SendToCombiner()
	{
		List<GameObject> targetMeshes = (from r in renderersToCombine
			select r.gameObject into g
			where !(g == null)
			where !Enumerable.Contains(objectsToIgnore, g)
			where !invalidObjects.Contains(g)
			select g).DistinctBy((GameObject g) => g.GetInstanceID()).ToList();
		_combiner.targetMeshes = targetMeshes;
	}

	private void MergeMeshes()
	{
		_combiner.CombineMeshes();
	}

	private void UndoMerge()
	{
		_combiner.UndoMerge();
	}

	private void MergeAndExtractPerMaterialMeshes()
	{
		_combiner.onDoneMerge.AddListener(OnPostMerge);
		_combiner.CombineMeshes();
	}

	private void QuickMerge()
	{
		CollectRenderers();
		ValidateRenderers();
		SendToCombiner();
		MergeAndExtractPerMaterialMeshes();
	}

	private void OnPostMerge()
	{
		MeshFilter component = GetComponent<MeshFilter>();
		MeshRenderer component2 = GetComponent<MeshRenderer>();
		Mesh sharedMesh = component.sharedMesh;
		int subMeshCount = sharedMesh.subMeshCount;
		string text = component2.name;
		Material[] sharedMaterials = component2.sharedMaterials;
		GameObject gameObject = new GameObject(text + "_PerMaterialMeshes");
		this.GetOrAddComponent<UberCombinerPerMaterialMeshes>(out var result);
		result.rootObject = gameObject;
		result.objects = new GameObject[subMeshCount];
		result.filters = new MeshFilter[subMeshCount];
		result.renderers = new MeshRenderer[subMeshCount];
		result.materials = new Material[subMeshCount];
		GTMeshData gTMeshData = GTMeshData.Parse(sharedMesh);
		for (int i = 0; i < subMeshCount; i++)
		{
			GameObject gameObject2 = new GameObject($"{i}_{sharedMaterials[i].name}");
			gameObject2.transform.parent = gameObject.transform;
			gameObject2.isStatic = true;
			MeshFilter meshFilter = gameObject2.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = gameObject2.AddComponent<MeshRenderer>();
			Mesh sharedMesh2 = gTMeshData.ExtractSubmesh(i);
			meshFilter.sharedMesh = sharedMesh2;
			meshRenderer.sharedMaterial = sharedMaterials[i];
			meshRenderer.lightProbeUsage = LightProbeUsage.Off;
			meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
			result.objects[i] = gameObject2;
			result.filters[i] = meshFilter;
			result.renderers[i] = meshRenderer;
			result.materials[i] = sharedMaterials[i];
		}
	}

	private void OnValidate()
	{
		if (!base.transform.position.Approx0())
		{
			base.transform.position = Vector3.zero;
		}
		if (_combiner == null)
		{
			_combiner = GetComponent<RuntimeMeshCombiner>();
			_combiner.recalculateNormals = false;
			_combiner.recalculateTangents = false;
			_combiner.combineInactives = false;
			_combiner.garbageCollectorAfterUndo = true;
			_combiner.afterMerge = RuntimeMeshCombiner.AfterMerge.DoNothing;
		}
	}

	private static IEnumerable<MeshRenderer> FilterRenderers(IList<MeshRenderer> renderers)
	{
		Shader uberShader = UberShader.ReferenceShader;
		Shader uberShaderNonSRP = UberShader.ReferenceShaderNonSRP;
		RenderQueueRange transQueue = RenderQueueRange.transparent;
		int i = 0;
		while (i < renderers.Count)
		{
			MeshRenderer mr = renderers[i];
			int num;
			if (!(mr == null) && mr.enabled && mr.gameObject.isStatic && !mr.GetComponent<EdDoNotMeshCombine>())
			{
				MeshFilter component = mr.GetComponent<MeshFilter>();
				if (!(component == null))
				{
					Mesh sharedMesh = component.sharedMesh;
					if (!(sharedMesh == null) && sharedMesh.vertexCount >= 3)
					{
						Material[] sharedMats = mr.sharedMaterials;
						if (!sharedMats.IsNullOrEmpty())
						{
							for (int j = 0; j < sharedMats.Length; j = num)
							{
								Material material = sharedMats[j];
								if (!(material == null))
								{
									int renderQueue = material.renderQueue;
									if ((renderQueue < transQueue.lowerBound || renderQueue > transQueue.upperBound) && (renderQueue < 2450 || renderQueue > 2500))
									{
										Shader shader = material.shader;
										if (shader == uberShader)
										{
											yield return mr;
										}
										else if (shader == uberShaderNonSRP)
										{
											yield return mr;
										}
									}
								}
								num = j + 1;
							}
						}
					}
				}
			}
			num = i + 1;
			i = num;
		}
	}
}
