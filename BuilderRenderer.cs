using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

public class BuilderRenderer : MonoBehaviourPostTick
{
	[BurstCompile]
	public struct SetupInstanceDataForMesh : IJobParallelForTransform
	{
		[ReadOnly]
		public NativeList<int> texIndex;

		[ReadOnly]
		public NativeList<float> tint;

		[ReadOnly]
		public GraphicsBuffer.IndirectDrawIndexedArgs commandData;

		[ReadOnly]
		public Vector3 cameraPos;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<int> instanceTexIndex;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<Matrix4x4> objectToWorld;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<float> instanceTint;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<int> lodLevel;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<int> lodDirty;

		public void Execute(int index, TransformAccess transform)
		{
			int index2 = index + (int)commandData.startInstance;
			objectToWorld[index2] = transform.localToWorldMatrix;
			instanceTexIndex[index2] = texIndex[index];
			instanceTint[index2] = tint[index];
		}
	}

	[BurstCompile]
	public struct SetupInstanceDataForMeshStatic : IJobParallelForTransform
	{
		[ReadOnly]
		public NativeArray<int> transformIndexToDataIndex;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<Matrix4x4> objectToWorld;

		public void Execute(int index, TransformAccess transform)
		{
			if (transform.isValid)
			{
				int num = transformIndexToDataIndex[index];
				for (int i = 0; i < 2; i++)
				{
					objectToWorld[num + i] = transform.localToWorldMatrix;
				}
			}
		}
	}

	public Material sharedMaterialBase;

	public Material sharedMaterialIndirectBase;

	public const int TEX_SIZE = 256;

	private Shader snapPieceShader;

	public BuilderTableDataRenderData renderData;

	[SerializeField]
	[HideInInspector]
	private List<Mesh> serializeMeshToIndexKeys;

	[SerializeField]
	[HideInInspector]
	private List<int> serializeMeshToIndexValues;

	[SerializeField]
	[HideInInspector]
	private List<Mesh> serializeMeshes;

	[SerializeField]
	[HideInInspector]
	private List<int> serializeMeshInstanceCount;

	[SerializeField]
	[HideInInspector]
	private List<BuilderTableSubMesh> serializeSubMeshes;

	[SerializeField]
	[HideInInspector]
	private Mesh serializeSharedMesh;

	[SerializeField]
	[HideInInspector]
	private List<Texture2D> serializeTextureToIndexKeys;

	[SerializeField]
	[HideInInspector]
	private List<int> serializeTextureToIndexValues;

	[SerializeField]
	[HideInInspector]
	private List<Texture2D> serializeTextures;

	[SerializeField]
	[HideInInspector]
	private List<Material> serializePerTextureMaterial;

	[SerializeField]
	[HideInInspector]
	private List<MaterialPropertyBlock> serializePerTexturePropertyBlock;

	[SerializeField]
	[HideInInspector]
	private Texture2DArray serializeSharedTexArray;

	[SerializeField]
	[HideInInspector]
	private Material serializeSharedMaterial;

	[SerializeField]
	[HideInInspector]
	private Material serializeSharedMaterialIndirect;

	private const string texturePropName = "_BaseMap";

	private const string textureArrayPropName = "_BaseMapArray";

	private const string textureArrayIndexPropName = "_BaseMapArrayIndex";

	private const string transformMatrixPropName = "_TransformMatrix";

	private const string texIndexPropName = "_TexIndex";

	private const string tintPropName = "_Tint";

	public const int MAX_STATIC_INSTANCES = 8192;

	public const int MAX_DYNAMIC_INSTANCES = 8192;

	public const int INSTANCES_PER_TRANSFORM = 2;

	private bool initialized;

	private bool built;

	private bool showing;

	private static List<MeshRenderer> meshRenderers = new List<MeshRenderer>(128);

	private const int MAX_TOTAL_VERTS = 65536;

	private const int MAX_TOTAL_TRIS = 65536;

	private static List<Vector3> verticesAll = new List<Vector3>(65536);

	private static List<Vector3> normalsAll = new List<Vector3>(65536);

	private static List<Vector2> uv1All = new List<Vector2>(65536);

	private static List<int> trianglesAll = new List<int>(65536);

	private static List<Vector3> vertices = new List<Vector3>(65536);

	private static List<Vector3> normals = new List<Vector3>(65536);

	private static List<Vector2> uv1 = new List<Vector2>(65536);

	private static List<int> triangles = new List<int>(65536);

	private void Awake()
	{
		InitIfNeeded();
	}

	public void InitIfNeeded()
	{
		if (!initialized)
		{
			initialized = true;
			snapPieceShader = Shader.Find("GorillaTag/SnapPiece");
			if (renderData == null)
			{
				renderData = new BuilderTableDataRenderData();
			}
			renderData.materialToIndex = new Dictionary<Material, int>(256);
			renderData.materials = new List<Material>(256);
			if (renderData.meshToIndex == null)
			{
				renderData.meshToIndex = new Dictionary<Mesh, int>(1024);
			}
			if (renderData.meshInstanceCount == null)
			{
				renderData.meshInstanceCount = new List<int>(1024);
			}
			if (renderData.meshes == null)
			{
				renderData.meshes = new List<Mesh>(4096);
			}
			if (renderData.textureToIndex == null)
			{
				renderData.textureToIndex = new Dictionary<Texture2D, int>(256);
			}
			if (renderData.textures == null)
			{
				renderData.textures = new List<Texture2D>(256);
			}
			if (renderData.perTextureMaterial == null)
			{
				renderData.perTextureMaterial = new List<Material>(256);
			}
			if (renderData.perTexturePropertyBlock == null)
			{
				renderData.perTexturePropertyBlock = new List<MaterialPropertyBlock>(256);
			}
			if (renderData.sharedMaterial == null)
			{
				renderData.sharedMaterial = new Material(sharedMaterialBase);
			}
			if (renderData.sharedMaterialIndirect == null)
			{
				renderData.sharedMaterialIndirect = new Material(sharedMaterialIndirectBase);
			}
			built = false;
			showing = false;
		}
	}

	public void Show(bool show)
	{
		showing = show;
	}

	public void BuildRenderer(List<BuilderPiece> piecePrefabs)
	{
		InitIfNeeded();
		for (int i = 0; i < piecePrefabs.Count; i++)
		{
			if (piecePrefabs[i] != null)
			{
				AddPrefab(piecePrefabs[i]);
				continue;
			}
			Debug.LogErrorFormat("Prefab at {0} is null", i);
		}
		BuildSharedMaterial();
		BuildSharedMesh();
		BuildBuffer();
		built = true;
	}

	public void LogDraws()
	{
		Debug.LogFormat("Builder Renderer Counts {0} {1} {2} {3}", renderData.subMeshes.Length, renderData.textures.Count, renderData.dynamicBatch.totalInstances, renderData.staticBatch.totalInstances);
	}

	public override void PostTick()
	{
		if (built && showing)
		{
			RenderIndirect();
		}
	}

	public void WriteSerializedData()
	{
		if (renderData == null)
		{
			return;
		}
		if (renderData.sharedMesh != null)
		{
			serializeMeshToIndexKeys = new List<Mesh>(renderData.meshToIndex.Count);
			serializeMeshToIndexValues = new List<int>(renderData.meshToIndex.Count);
			foreach (KeyValuePair<Mesh, int> item in renderData.meshToIndex)
			{
				serializeMeshToIndexKeys.Add(item.Key);
				serializeMeshToIndexValues.Add(item.Value);
			}
			serializeMeshes = renderData.meshes;
			serializeMeshInstanceCount = renderData.meshInstanceCount;
			serializeSubMeshes = new List<BuilderTableSubMesh>(512);
			foreach (BuilderTableSubMesh subMesh in renderData.subMeshes)
			{
				serializeSubMeshes.Add(subMesh);
			}
			serializeSharedMesh = renderData.sharedMesh;
		}
		if (!(renderData.sharedMaterial != null))
		{
			return;
		}
		serializeTextureToIndexKeys = new List<Texture2D>(renderData.textureToIndex.Count);
		serializeTextureToIndexValues = new List<int>(renderData.textureToIndex.Count);
		foreach (KeyValuePair<Texture2D, int> item2 in renderData.textureToIndex)
		{
			serializeTextureToIndexKeys.Add(item2.Key);
			serializeTextureToIndexValues.Add(item2.Value);
		}
		serializeTextures = renderData.textures;
		serializePerTextureMaterial = renderData.perTextureMaterial;
		serializePerTexturePropertyBlock = renderData.perTexturePropertyBlock;
		serializeSharedTexArray = renderData.sharedTexArray;
		serializeSharedMaterial = renderData.sharedMaterial;
		serializeSharedMaterialIndirect = renderData.sharedMaterialIndirect;
	}

	private void ApplySerializedData()
	{
		if (serializeSharedMesh != null)
		{
			if (renderData == null)
			{
				renderData = new BuilderTableDataRenderData();
			}
			renderData.meshToIndex = new Dictionary<Mesh, int>(1024);
			for (int i = 0; i < serializeMeshToIndexKeys.Count; i++)
			{
				renderData.meshToIndex.Add(serializeMeshToIndexKeys[i], serializeMeshToIndexValues[i]);
			}
			renderData.meshes = serializeMeshes;
			renderData.meshInstanceCount = serializeMeshInstanceCount;
			renderData.subMeshes = new NativeList<BuilderTableSubMesh>(512, Allocator.Persistent);
			foreach (BuilderTableSubMesh serializeSubMesh in serializeSubMeshes)
			{
				renderData.subMeshes.AddNoResize(serializeSubMesh);
			}
			renderData.sharedMesh = serializeSharedMesh;
		}
		if (serializeSharedMaterial != null)
		{
			if (renderData == null)
			{
				renderData = new BuilderTableDataRenderData();
			}
			renderData.textureToIndex = new Dictionary<Texture2D, int>(256);
			for (int j = 0; j < serializeTextureToIndexKeys.Count; j++)
			{
				renderData.textureToIndex.Add(serializeTextureToIndexKeys[j], serializeTextureToIndexValues[j]);
			}
			renderData.textures = serializeTextures;
			renderData.perTextureMaterial = serializePerTextureMaterial;
			renderData.perTexturePropertyBlock = serializePerTexturePropertyBlock;
			renderData.sharedTexArray = serializeSharedTexArray;
			renderData.sharedMaterial = serializeSharedMaterial;
			renderData.sharedMaterialIndirect = serializeSharedMaterialIndirect;
		}
	}

	public void AddPrefab(BuilderPiece prefab)
	{
		meshRenderers.Clear();
		prefab.GetComponentsInChildren(includeInactive: true, meshRenderers);
		for (int i = 0; i < meshRenderers.Count; i++)
		{
			MeshRenderer meshRenderer = meshRenderers[i];
			Material sharedMaterial = meshRenderer.sharedMaterial;
			if (sharedMaterial == null)
			{
				if (!prefab.suppressMaterialWarnings)
				{
					Debug.LogErrorFormat("{0} {1} is missing a buidler material", prefab.name, meshRenderer.name);
				}
			}
			else if (!AddMaterial(sharedMaterial, prefab.suppressMaterialWarnings))
			{
				if (!prefab.suppressMaterialWarnings)
				{
					Debug.LogWarningFormat("{0} {1} failed to add builder material", prefab.name, meshRenderer.name);
				}
			}
			else
			{
				if (!(renderData.sharedMesh == null))
				{
					continue;
				}
				MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
				if (!(component != null))
				{
					continue;
				}
				Mesh sharedMesh = component.sharedMesh;
				if (sharedMesh != null && !renderData.meshToIndex.TryGetValue(sharedMesh, out var _))
				{
					renderData.meshToIndex.Add(sharedMesh, renderData.meshToIndex.Count);
					renderData.meshInstanceCount.Add(0);
					for (int j = 0; j < 1; j++)
					{
						renderData.meshes.Add(sharedMesh);
					}
				}
			}
		}
		if (!(prefab.materialOptions != null))
		{
			return;
		}
		for (int k = 0; k < prefab.materialOptions.options.Count; k++)
		{
			Material material = prefab.materialOptions.options[k].material;
			if (!AddMaterial(material, prefab.suppressMaterialWarnings) && !prefab.suppressMaterialWarnings)
			{
				Debug.LogWarningFormat("builder material options {0} bad material index {1}", prefab.materialOptions.name, k);
			}
		}
	}

	private bool AddMaterial(Material material, bool suppressWarnings = false)
	{
		if (material == null)
		{
			return false;
		}
		if (material.shader != snapPieceShader)
		{
			if (!suppressWarnings)
			{
				Debug.LogWarningFormat("builder: material {0} uses non snap piece shader {1}", material.name, material.shader.name);
			}
			return false;
		}
		if (!material.HasTexture("_BaseMap"))
		{
			if (!suppressWarnings)
			{
				Debug.LogWarningFormat("builder material {0} does not have texture property {1}", material.name, "_BaseMap");
			}
			return false;
		}
		Texture texture = material.GetTexture("_BaseMap");
		if (texture == null)
		{
			if (!suppressWarnings)
			{
				Debug.LogWarningFormat("builder material {0} null texture", material.name);
			}
			return false;
		}
		Texture2D texture2D = texture as Texture2D;
		if (texture2D == null)
		{
			if (!suppressWarnings)
			{
				Debug.LogWarningFormat("builder material {0} no texture2d type is {1}", material.name, texture.GetType());
			}
			return false;
		}
		if (texture2D.width != 256 || texture2D.height != 256)
		{
			if (!suppressWarnings)
			{
				Debug.LogWarningFormat("builder texture {0} unexpected size {1} {2}", texture2D.name, texture2D.width, texture2D.height);
			}
			return false;
		}
		if (!renderData.materialToIndex.TryGetValue(material, out var _))
		{
			renderData.materialToIndex.Add(material, renderData.materials.Count);
			renderData.materials.Add(material);
		}
		if (!renderData.textureToIndex.TryGetValue(texture2D, out var _))
		{
			renderData.textureToIndex.Add(texture2D, renderData.textures.Count);
			renderData.textures.Add(texture2D);
			if (renderData.textures.Count == 1)
			{
				renderData.textureFormat = texture2D.format;
				renderData.texWidth = texture2D.width;
				renderData.texHeight = texture2D.height;
			}
		}
		return true;
	}

	public void BuildSharedMaterial()
	{
		if (renderData.sharedTexArray != null)
		{
			Debug.Log("Already have shared material. Not building new one.");
			return;
		}
		TextureFormat textureFormat = TextureFormat.RGBA32;
		renderData.sharedTexArray = new Texture2DArray(renderData.texWidth, renderData.texHeight, renderData.textures.Count, textureFormat, mipChain: true);
		renderData.sharedTexArray.filterMode = FilterMode.Point;
		for (int i = 0; i < renderData.textures.Count; i++)
		{
			renderData.sharedTexArray.SetPixels(renderData.textures[i].GetPixels(), i);
		}
		renderData.sharedTexArray.Apply(updateMipmaps: true, makeNoLongerReadable: true);
		renderData.sharedMaterial.SetTexture("_BaseMapArray", renderData.sharedTexArray);
		renderData.sharedMaterialIndirect.SetTexture("_BaseMapArray", renderData.sharedTexArray);
		renderData.sharedMaterialIndirect.enableInstancing = true;
		for (int j = 0; j < renderData.textures.Count; j++)
		{
			Material material = new Material(renderData.sharedMaterial);
			material.SetInt("_BaseMapArrayIndex", j);
			renderData.perTextureMaterial.Add(material);
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			materialPropertyBlock.SetInt("_BaseMapArrayIndex", j);
			renderData.perTexturePropertyBlock.Add(materialPropertyBlock);
		}
	}

	public void BuildSharedMesh()
	{
		if (renderData.sharedMesh != null)
		{
			Debug.Log("Already have shared mesh. Not building new one.");
			return;
		}
		renderData.sharedMesh = new Mesh();
		renderData.sharedMesh.indexFormat = IndexFormat.UInt32;
		verticesAll.Clear();
		normalsAll.Clear();
		uv1All.Clear();
		trianglesAll.Clear();
		renderData.subMeshes = new NativeList<BuilderTableSubMesh>(512, Allocator.Persistent);
		for (int i = 0; i < renderData.meshes.Count; i++)
		{
			Mesh mesh = renderData.meshes[i];
			int count = trianglesAll.Count;
			int count2 = verticesAll.Count;
			vertices.Clear();
			normals.Clear();
			uv1.Clear();
			triangles.Clear();
			mesh.GetVertices(vertices);
			mesh.GetNormals(normals);
			mesh.GetUVs(0, uv1);
			mesh.GetTriangles(triangles, 0);
			verticesAll.AddRange(vertices);
			normalsAll.AddRange(normals);
			uv1All.AddRange(uv1);
			trianglesAll.AddRange(triangles);
			int indexCount = trianglesAll.Count - count;
			BuilderTableSubMesh value = new BuilderTableSubMesh
			{
				startIndex = count,
				indexCount = indexCount,
				startVertex = count2
			};
			renderData.subMeshes.Add(in value);
		}
		renderData.sharedMesh.SetVertices(verticesAll);
		renderData.sharedMesh.SetNormals(normalsAll);
		renderData.sharedMesh.SetUVs(0, uv1All);
		renderData.sharedMesh.SetTriangles(trianglesAll, 0);
	}

	public void BuildBuffer()
	{
		renderData.dynamicBatch = new BuilderTableDataRenderIndirectBatch();
		BuildBatch(renderData.dynamicBatch, renderData.meshes.Count, 8192, renderData.sharedMaterialIndirect);
		renderData.staticBatch = new BuilderTableDataRenderIndirectBatch();
		BuildBatch(renderData.staticBatch, renderData.meshes.Count, 8192, renderData.sharedMaterialIndirect);
	}

	public static void BuildBatch(BuilderTableDataRenderIndirectBatch indirectBatch, int meshCount, int maxInstances, Material sharedMaterialIndirect)
	{
		indirectBatch.totalInstances = 0;
		indirectBatch.commandCount = meshCount;
		indirectBatch.commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, indirectBatch.commandCount, 20);
		indirectBatch.commandData = new NativeArray<GraphicsBuffer.IndirectDrawIndexedArgs>(indirectBatch.commandCount, Allocator.Persistent);
		indirectBatch.matrixBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxInstances * 2, 64);
		indirectBatch.texIndexBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxInstances * 2, 4);
		indirectBatch.tintBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxInstances * 2, 4);
		indirectBatch.instanceTransform = new TransformAccessArray(maxInstances, 3);
		indirectBatch.instanceTransformIndexToDataIndex = new NativeArray<int>(maxInstances, Allocator.Persistent);
		for (int i = 0; i < maxInstances; i++)
		{
			indirectBatch.instanceTransformIndexToDataIndex[i] = -1;
		}
		indirectBatch.pieceIDPerTransform = new List<int>(maxInstances);
		indirectBatch.instanceObjectToWorld = new NativeArray<Matrix4x4>(maxInstances * 2, Allocator.Persistent);
		indirectBatch.instanceTexIndex = new NativeArray<int>(maxInstances * 2, Allocator.Persistent);
		indirectBatch.instanceTint = new NativeArray<float>(maxInstances * 2, Allocator.Persistent);
		indirectBatch.renderMeshes = new NativeList<BuilderTableMeshInstances>(512, Allocator.Persistent);
		for (int j = 0; j < meshCount; j++)
		{
			BuilderTableMeshInstances value = new BuilderTableMeshInstances
			{
				transforms = new TransformAccessArray(maxInstances, 3),
				texIndex = new NativeList<int>(Allocator.Persistent),
				tint = new NativeList<float>(Allocator.Persistent)
			};
			indirectBatch.renderMeshes.Add(in value);
		}
		indirectBatch.rp = new RenderParams(sharedMaterialIndirect);
		indirectBatch.rp.worldBounds = new Bounds(Vector3.zero, 10000f * Vector3.one);
		indirectBatch.rp.matProps = new MaterialPropertyBlock();
		indirectBatch.rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.identity);
		indirectBatch.matrixBuf.SetData(indirectBatch.instanceObjectToWorld);
		indirectBatch.texIndexBuf.SetData(indirectBatch.instanceTexIndex);
		indirectBatch.tintBuf.SetData(indirectBatch.instanceTint);
		indirectBatch.rp.matProps.SetBuffer("_TransformMatrix", indirectBatch.matrixBuf);
		indirectBatch.rp.matProps.SetBuffer("_TexIndex", indirectBatch.texIndexBuf);
		indirectBatch.rp.matProps.SetBuffer("_Tint", indirectBatch.tintBuf);
	}

	private void OnDestroy()
	{
		DestroyBuffer();
		renderData.subMeshes.Dispose();
	}

	public void DestroyBuffer()
	{
		DestroyBatch(renderData.staticBatch);
		DestroyBatch(renderData.dynamicBatch);
	}

	public static void DestroyBatch(BuilderTableDataRenderIndirectBatch indirectBatch)
	{
		indirectBatch.commandBuf.Dispose();
		indirectBatch.commandData.Dispose();
		indirectBatch.matrixBuf.Dispose();
		indirectBatch.texIndexBuf.Dispose();
		indirectBatch.tintBuf.Dispose();
		indirectBatch.instanceTransform.Dispose();
		indirectBatch.instanceTransformIndexToDataIndex.Dispose();
		indirectBatch.instanceObjectToWorld.Dispose();
		indirectBatch.instanceTexIndex.Dispose();
		indirectBatch.instanceTint.Dispose();
		foreach (BuilderTableMeshInstances renderMesh in indirectBatch.renderMeshes)
		{
			TransformAccessArray transforms = renderMesh.transforms;
			transforms.Dispose();
			NativeList<int> texIndex = renderMesh.texIndex;
			texIndex.Dispose();
			NativeList<float> tint = renderMesh.tint;
			tint.Dispose();
		}
		indirectBatch.renderMeshes.Dispose();
	}

	public void PreRenderIndirect()
	{
		if (built && showing)
		{
			renderData.setupInstancesJobs = default(JobHandle);
			SetupIndirectBatchArgs(renderData.staticBatch, renderData.subMeshes);
			SetupInstanceDataForMeshStatic jobData = new SetupInstanceDataForMeshStatic
			{
				transformIndexToDataIndex = renderData.staticBatch.instanceTransformIndexToDataIndex,
				objectToWorld = renderData.staticBatch.instanceObjectToWorld
			};
			renderData.setupInstancesJobs = jobData.ScheduleReadOnly(renderData.staticBatch.instanceTransform, 32);
			JobHandle.ScheduleBatchedJobs();
		}
	}

	public void RenderIndirect()
	{
		renderData.setupInstancesJobs.Complete();
		RenderIndirectBatch(renderData.staticBatch);
	}

	private static void SetupIndirectBatchArgs(BuilderTableDataRenderIndirectBatch indirectBatch, NativeList<BuilderTableSubMesh> subMeshes)
	{
		uint num = 0u;
		for (int i = 0; i < indirectBatch.commandCount; i++)
		{
			BuilderTableMeshInstances builderTableMeshInstances = indirectBatch.renderMeshes[i];
			BuilderTableSubMesh builderTableSubMesh = subMeshes[i];
			GraphicsBuffer.IndirectDrawIndexedArgs value = new GraphicsBuffer.IndirectDrawIndexedArgs
			{
				indexCountPerInstance = (uint)builderTableSubMesh.indexCount,
				startIndex = (uint)builderTableSubMesh.startIndex,
				baseVertexIndex = (uint)builderTableSubMesh.startVertex,
				startInstance = num,
				instanceCount = (uint)(builderTableMeshInstances.transforms.length * 2)
			};
			num += value.instanceCount;
			indirectBatch.commandData[i] = value;
		}
	}

	private void RenderIndirectBatch(BuilderTableDataRenderIndirectBatch indirectBatch)
	{
		indirectBatch.matrixBuf.SetData(indirectBatch.instanceObjectToWorld);
		indirectBatch.texIndexBuf.SetData(indirectBatch.instanceTexIndex);
		indirectBatch.tintBuf.SetData(indirectBatch.instanceTint);
		indirectBatch.commandBuf.SetData(indirectBatch.commandData);
		Graphics.RenderMeshIndirect(in indirectBatch.rp, renderData.sharedMesh, indirectBatch.commandBuf, indirectBatch.commandCount);
	}

	public void AddPiece(BuilderPiece piece)
	{
		bool isStatic = piece.isStatic;
		meshRenderers.Clear();
		piece.GetComponentsInChildren(includeInactive: false, meshRenderers);
		for (int i = 0; i < meshRenderers.Count; i++)
		{
			MeshRenderer meshRenderer = meshRenderers[i];
			if (!meshRenderer.enabled)
			{
				continue;
			}
			Material material = meshRenderer.material;
			if (!material.HasTexture("_BaseMap"))
			{
				continue;
			}
			Texture2D texture2D = material.GetTexture("_BaseMap") as Texture2D;
			if (texture2D == null)
			{
				continue;
			}
			if (!renderData.textureToIndex.TryGetValue(texture2D, out var value))
			{
				if (!piece.suppressMaterialWarnings)
				{
					Debug.LogWarningFormat("builder piece {0} material {1} texture not found in render data", piece.displayName, material.name);
				}
				continue;
			}
			MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
			if (component == null)
			{
				continue;
			}
			Mesh sharedMesh = component.sharedMesh;
			if (sharedMesh == null)
			{
				continue;
			}
			if (!renderData.meshToIndex.TryGetValue(sharedMesh, out var value2))
			{
				Debug.LogWarningFormat("builder piece {0} mesh {1} not found in render data", piece.displayName, meshRenderer.name);
				continue;
			}
			int num = renderData.meshInstanceCount[value2] % 1;
			renderData.meshInstanceCount[value2] = renderData.meshInstanceCount[value2] + 1;
			value2 += num;
			int num2 = -1;
			if (isStatic)
			{
				NativeArray<int> instanceTransformIndexToDataIndex = renderData.staticBatch.instanceTransformIndexToDataIndex;
				int length = renderData.staticBatch.instanceTransform.length;
				if (length + 2 >= instanceTransformIndexToDataIndex.Length)
				{
					GTDev.LogError("Too Many Builder Mesh Instances");
					break;
				}
				num2 = length;
				BuilderTableMeshInstances builderTableMeshInstances = renderData.staticBatch.renderMeshes[value2];
				int num3 = 0;
				for (int j = 0; j <= value2; j++)
				{
					num3 += renderData.staticBatch.renderMeshes[j].transforms.length * 2;
				}
				for (int k = 0; k < length; k++)
				{
					if (renderData.staticBatch.instanceTransformIndexToDataIndex[k] >= num3)
					{
						renderData.staticBatch.instanceTransformIndexToDataIndex[k] = renderData.staticBatch.instanceTransformIndexToDataIndex[k] + 2;
					}
				}
				renderData.staticBatch.pieceIDPerTransform.Add(piece.pieceId);
				renderData.staticBatch.instanceTransform.Add(meshRenderer.transform);
				renderData.staticBatch.instanceTransformIndexToDataIndex[num2] = num3;
				builderTableMeshInstances.transforms.Add(meshRenderer.transform);
				builderTableMeshInstances.texIndex.Add(in value);
				builderTableMeshInstances.tint.Add(in piece.tint);
				int num4 = renderData.staticBatch.totalInstances - 1;
				for (int num5 = num4; num5 >= num3; num5--)
				{
					renderData.staticBatch.instanceTexIndex[num5 + 2] = renderData.staticBatch.instanceTexIndex[num5];
				}
				for (int num6 = num4; num6 >= num3; num6--)
				{
					renderData.staticBatch.instanceObjectToWorld[num6 + 2] = renderData.staticBatch.instanceObjectToWorld[num6];
				}
				for (int num7 = num4; num7 >= num3; num7--)
				{
					renderData.staticBatch.instanceTint[num7 + 2] = renderData.staticBatch.instanceTint[num7];
				}
				for (int l = 0; l < 2; l++)
				{
					renderData.staticBatch.instanceObjectToWorld[num3 + l] = meshRenderer.transform.localToWorldMatrix;
					renderData.staticBatch.instanceTexIndex[num3 + l] = value;
					renderData.staticBatch.instanceTint[num3 + l] = 1f;
					renderData.staticBatch.totalInstances++;
				}
			}
			else
			{
				BuilderTableMeshInstances builderTableMeshInstances2 = renderData.dynamicBatch.renderMeshes[value2];
				builderTableMeshInstances2.transforms.Add(meshRenderer.transform);
				builderTableMeshInstances2.texIndex.Add(in value);
				builderTableMeshInstances2.tint.Add(in piece.tint);
				renderData.dynamicBatch.totalInstances++;
			}
			piece.renderingIndirect.Add(meshRenderer);
			piece.renderingDirect.Remove(meshRenderer);
			piece.renderingIndirectTransformIndex.Add(num2);
			meshRenderer.enabled = false;
		}
	}

	public void RemovePiece(BuilderPiece piece)
	{
		bool isStatic = piece.isStatic;
		for (int i = 0; i < piece.renderingIndirect.Count; i++)
		{
			MeshRenderer meshRenderer = piece.renderingIndirect[i];
			if (meshRenderer == null)
			{
				continue;
			}
			Material sharedMaterial = meshRenderer.sharedMaterial;
			if (!sharedMaterial.HasTexture("_BaseMap"))
			{
				continue;
			}
			Texture2D texture2D = sharedMaterial.GetTexture("_BaseMap") as Texture2D;
			if (texture2D == null || !renderData.textureToIndex.TryGetValue(texture2D, out var _))
			{
				continue;
			}
			MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
			if (component == null)
			{
				continue;
			}
			Mesh sharedMesh = component.sharedMesh;
			if (!renderData.meshToIndex.TryGetValue(sharedMesh, out var value2))
			{
				continue;
			}
			Transform transform = meshRenderer.transform;
			bool flag = false;
			int num = 0;
			int num2 = -1;
			if (isStatic)
			{
				for (int j = 0; j < value2; j++)
				{
					num += renderData.staticBatch.renderMeshes[j].transforms.length;
				}
				TransformAccessArray instanceTransform = renderData.staticBatch.instanceTransform;
				int length = instanceTransform.length;
				int num3 = piece.renderingIndirectTransformIndex[i];
				num2 = renderData.staticBatch.instanceTransformIndexToDataIndex[num3];
				int num4 = renderData.staticBatch.instanceTransform.length - 1;
				int pieceId = renderData.staticBatch.pieceIDPerTransform[num4];
				renderData.staticBatch.instanceTransform.RemoveAtSwapBack(num3);
				renderData.staticBatch.pieceIDPerTransform.RemoveAtSwapBack(num3);
				renderData.staticBatch.instanceTransformIndexToDataIndex[num3] = renderData.staticBatch.instanceTransformIndexToDataIndex[num4];
				renderData.staticBatch.instanceTransformIndexToDataIndex[num4] = -1;
				BuilderPiece piece2 = piece.GetTable().GetPiece(pieceId);
				if (piece2 != null)
				{
					for (int k = 0; k < piece2.renderingIndirectTransformIndex.Count; k++)
					{
						if (piece2.renderingIndirectTransformIndex[k] == num4)
						{
							piece2.renderingIndirectTransformIndex[k] = num3;
						}
					}
				}
				for (int l = 0; l < length; l++)
				{
					if (renderData.staticBatch.instanceTransformIndexToDataIndex[l] > num2)
					{
						renderData.staticBatch.instanceTransformIndexToDataIndex[l] = renderData.staticBatch.instanceTransformIndexToDataIndex[l] - 2;
					}
				}
			}
			for (int m = 0; m < 1; m++)
			{
				int index = value2 + m;
				if (isStatic)
				{
					BuilderTableMeshInstances builderTableMeshInstances = renderData.staticBatch.renderMeshes[index];
					for (int n = 0; n < builderTableMeshInstances.transforms.length; n++)
					{
						if (builderTableMeshInstances.transforms[n] == transform)
						{
							num += n;
							RemoveAt(builderTableMeshInstances.transforms, n);
							builderTableMeshInstances.texIndex.RemoveAt(n);
							builderTableMeshInstances.tint.RemoveAt(n);
							flag = true;
							renderData.staticBatch.totalInstances -= 2;
							break;
						}
					}
				}
				else
				{
					BuilderTableMeshInstances builderTableMeshInstances2 = renderData.dynamicBatch.renderMeshes[index];
					for (int num5 = 0; num5 < builderTableMeshInstances2.transforms.length; num5++)
					{
						if (builderTableMeshInstances2.transforms[num5] == transform)
						{
							RemoveAt(builderTableMeshInstances2.transforms, num5);
							builderTableMeshInstances2.texIndex.RemoveAt(num5);
							builderTableMeshInstances2.tint.RemoveAt(num5);
							flag = true;
							renderData.dynamicBatch.totalInstances--;
							break;
						}
					}
				}
				if (flag)
				{
					piece.renderingDirect.Add(meshRenderer);
					break;
				}
			}
			if (flag && isStatic)
			{
				int num6 = renderData.staticBatch.totalInstances + 1;
				for (int num7 = num2; num7 < num6; num7++)
				{
					renderData.staticBatch.instanceTexIndex[num7] = renderData.staticBatch.instanceTexIndex[num7 + 2];
				}
				for (int num8 = num2; num8 < num6; num8++)
				{
					renderData.staticBatch.instanceObjectToWorld[num8] = renderData.staticBatch.instanceObjectToWorld[num8 + 2];
				}
				for (int num9 = num2; num9 < num6; num9++)
				{
					renderData.staticBatch.instanceTint[num9] = renderData.staticBatch.instanceTint[num9 + 2];
				}
			}
			meshRenderer.enabled = true;
		}
		piece.renderingIndirect.Clear();
		piece.renderingIndirectTransformIndex.Clear();
	}

	public void ChangePieceIndirectMaterial(BuilderPiece piece, List<MeshRenderer> targetRenderers, Material targetMaterial)
	{
		if (targetMaterial == null)
		{
			return;
		}
		if (!targetMaterial.HasTexture("_BaseMap"))
		{
			Debug.LogError("New Material is missing a texture");
			return;
		}
		Texture2D texture2D = targetMaterial.GetTexture("_BaseMap") as Texture2D;
		if (texture2D == null)
		{
			Debug.LogError("New Material does not have a \"_BaseMap\" property");
			return;
		}
		if (!renderData.textureToIndex.TryGetValue(texture2D, out var value))
		{
			Debug.LogError("New Material is not in the texture array");
			return;
		}
		bool isStatic = piece.isStatic;
		for (int i = 0; i < piece.renderingIndirect.Count; i++)
		{
			MeshRenderer meshRenderer = piece.renderingIndirect[i];
			if (!targetRenderers.Contains(meshRenderer))
			{
				Debug.Log("renderer not in target list");
				continue;
			}
			meshRenderer.material = targetMaterial;
			MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
			if (component == null)
			{
				continue;
			}
			Mesh sharedMesh = component.sharedMesh;
			if (!renderData.meshToIndex.TryGetValue(sharedMesh, out var value2))
			{
				continue;
			}
			Transform transform = meshRenderer.transform;
			bool flag = false;
			if (isStatic)
			{
				int index = piece.renderingIndirectTransformIndex[i];
				int num = renderData.staticBatch.instanceTransformIndexToDataIndex[index];
				if (num >= 0)
				{
					for (int j = 0; j < 2; j++)
					{
						renderData.staticBatch.instanceTexIndex[num + j] = value;
					}
				}
				continue;
			}
			for (int k = 0; k < 1; k++)
			{
				int index2 = value2 + k;
				BuilderTableMeshInstances builderTableMeshInstances = renderData.dynamicBatch.renderMeshes[index2];
				for (int l = 0; l < builderTableMeshInstances.transforms.length; l++)
				{
					if (builderTableMeshInstances.transforms[l] == transform)
					{
						renderData.dynamicBatch.renderMeshes.ElementAt(index2).texIndex[l] = value;
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
		}
	}

	public static void RemoveAt(TransformAccessArray a, int i)
	{
		int length = a.length;
		for (int j = i; j < length - 1; j++)
		{
			a[j] = a[j + 1];
		}
		a.RemoveAtSwapBack(length - 1);
	}

	public void SetPieceTint(BuilderPiece piece, float tint)
	{
		for (int i = 0; i < piece.renderingIndirect.Count; i++)
		{
			MeshRenderer meshRenderer = piece.renderingIndirect[i];
			Material sharedMaterial = meshRenderer.sharedMaterial;
			if (!sharedMaterial.HasTexture("_BaseMap"))
			{
				continue;
			}
			Texture2D texture2D = sharedMaterial.GetTexture("_BaseMap") as Texture2D;
			if (texture2D == null || !renderData.textureToIndex.TryGetValue(texture2D, out var _))
			{
				continue;
			}
			MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
			if (component == null)
			{
				continue;
			}
			Mesh sharedMesh = component.sharedMesh;
			if (!renderData.meshToIndex.TryGetValue(sharedMesh, out var value2))
			{
				continue;
			}
			Transform transform = meshRenderer.transform;
			if (piece.isStatic)
			{
				int index = piece.renderingIndirectTransformIndex[i];
				int num = renderData.staticBatch.instanceTransformIndexToDataIndex[index];
				if (num >= 0)
				{
					for (int j = 0; j < 2; j++)
					{
						renderData.staticBatch.instanceTint[num + j] = tint;
					}
				}
				continue;
			}
			for (int k = 0; k < 1; k++)
			{
				int index2 = value2 + k;
				BuilderTableMeshInstances builderTableMeshInstances = renderData.dynamicBatch.renderMeshes[index2];
				for (int l = 0; l < builderTableMeshInstances.transforms.length; l++)
				{
					if (builderTableMeshInstances.transforms[l] == transform)
					{
						builderTableMeshInstances.tint[l] = tint;
						break;
					}
				}
			}
		}
	}
}
