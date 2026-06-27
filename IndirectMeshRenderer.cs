using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class IndirectMeshRenderer
{
	private struct BatchKey : IEquatable<BatchKey>
	{
		public int meshId;

		public int textureId;

		public int shaderId;

		public bool Equals(BatchKey other)
		{
			if (meshId == other.meshId && textureId == other.textureId)
			{
				return shaderId == other.shaderId;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((meshId * 397) ^ textureId) * 397) ^ shaderId;
		}

		public override bool Equals(object obj)
		{
			if (obj is BatchKey other)
			{
				return Equals(other);
			}
			return false;
		}
	}

	private struct DynamicEntry
	{
		public Transform transform;

		public int matrixIndex;
	}

	private struct DrawBatch
	{
		public Mesh mesh;

		public Material material;

		public int submeshCount;

		public int layer;

		public NativeList<Matrix4x4> matrices;

		public NativeList<int> groupIds;

		public NativeList<byte> visibility;

		public int visibleCount;

		public NativeArray<Matrix4x4> gpuMatrices;

		public GraphicsBuffer matrixBuffer;

		public GraphicsBuffer commandBuffer;

		public NativeArray<GraphicsBuffer.IndirectDrawIndexedArgs> commandData;

		public RenderParams renderParams;

		public bool dirty;

		public bool needsUpload;

		public List<DynamicEntry> dynamicEntries;
	}

	private sealed class PostTickCallback : ITickSystemPost
	{
		public bool PostTickRunning { get; set; }

		public void PostTick()
		{
			_Render();
		}
	}

	private const string SHADER_NAME = "GorillaTag/IndirectLit";

	private const string SHADER_NAME_EMISSIVE = "GorillaTag/IndirectLitEmissive";

	private const int _k_instancesPerXform = 2;

	private static readonly int _spId_Matrices = Shader.PropertyToID("_Matrices");

	private static Shader _shader;

	private static Shader _shaderEmissive;

	private static readonly Dictionary<BatchKey, int> _batchLookup = new Dictionary<BatchKey, int>();

	private static readonly List<DrawBatch> _batchList = new List<DrawBatch>();

	private static bool _loggedFirstRender;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void _Init()
	{
		_DisposeAll();
		_shader = Shader.Find("GorillaTag/IndirectLit");
		if (_shader == null)
		{
			Debug.LogError("[IndirectMeshRenderer] Shader 'GorillaTag/IndirectLit' not found. Add it to Always Included Shaders.");
		}
		_shaderEmissive = Shader.Find("GorillaTag/IndirectLitEmissive");
		if (_shaderEmissive == null)
		{
			Debug.LogError("[IndirectMeshRenderer] Shader 'GorillaTag/IndirectLitEmissive' not found. Add it to Always Included Shaders.");
		}
		Application.quitting += _DisposeAll;
		TickSystem<object>.AddPostTickCallback(new PostTickCallback());
	}

	public static void Register(IndirectMeshInstance inst, int groupId = 0)
	{
		Mesh sharedMesh = inst.meshFilter.sharedMesh;
		if (sharedMesh.subMeshCount > 1)
		{
			Debug.LogError($"[IndirectMeshRenderer] Mesh '{sharedMesh.name}' on '{inst.name}' has {sharedMesh.subMeshCount} submeshes " + "(likely from static batching). Disable Static on objects with IndirectMeshInstance.", inst);
			return;
		}
		Material sharedMaterial = inst.meshRenderer.sharedMaterial;
		Texture texture = (sharedMaterial.HasTexture(ShaderProps._BaseMap) ? sharedMaterial.GetTexture(ShaderProps._BaseMap) : null);
		bool flag = sharedMaterial.IsKeywordEnabled("_EMISSION");
		Shader shader = (flag ? _shaderEmissive : _shader);
		BatchKey key = new BatchKey
		{
			meshId = sharedMesh.GetInstanceID(),
			textureId = ((texture != null) ? texture.GetInstanceID() : 0),
			shaderId = shader.GetInstanceID()
		};
		if (!_batchLookup.TryGetValue(key, out var value))
		{
			DrawBatch item = new DrawBatch
			{
				mesh = sharedMesh,
				submeshCount = sharedMesh.subMeshCount,
				layer = inst.gameObject.layer,
				matrices = new NativeList<Matrix4x4>(2048, Allocator.Persistent),
				groupIds = new NativeList<int>(2048, Allocator.Persistent),
				visibility = new NativeList<byte>(2048, Allocator.Persistent),
				material = new Material(shader)
				{
					name = sharedMaterial.name + " (Indirect)"
				}
			};
			if (texture != null)
			{
				item.material.SetTexture(ShaderProps._BaseMap, texture);
			}
			if (sharedMaterial.HasColor(ShaderProps._BaseColor))
			{
				item.material.SetColor(ShaderProps._BaseColor, sharedMaterial.GetColor(ShaderProps._BaseColor));
			}
			if (flag)
			{
				_CopyEmissionProperties(item.material, sharedMaterial);
			}
			value = _batchList.Count;
			_batchLookup[key] = value;
			_batchList.Add(item);
			Debug.Log(string.Format("[IndirectMeshRenderer] New batch #{0}: mesh='{1}' tex='{2}' shader='{3}' layer={4} submeshes={5}", value, sharedMesh.name, (texture != null) ? texture.name : "null", shader.name, inst.gameObject.layer, sharedMesh.subMeshCount));
		}
		DrawBatch value2 = _batchList[value];
		int length = value2.matrices.Length;
		value2.matrices.Add(inst.transform.localToWorldMatrix);
		value2.groupIds.Add(in groupId);
		value2.visibility.Add((byte)1);
		value2.visibleCount++;
		value2.dirty = true;
		if (inst.dynamic)
		{
			ref List<DynamicEntry> dynamicEntries = ref value2.dynamicEntries;
			if (dynamicEntries == null)
			{
				dynamicEntries = new List<DynamicEntry>();
			}
			value2.dynamicEntries.Add(new DynamicEntry
			{
				transform = inst.transform,
				matrixIndex = length
			});
		}
		_batchList[value] = value2;
	}

	private static void _CopyEmissionProperties(Material dst, Material src)
	{
		if (src.HasTexture(ShaderProps._EmissionMap))
		{
			dst.SetTexture(ShaderProps._EmissionMap, src.GetTexture(ShaderProps._EmissionMap));
		}
		if (src.HasColor(ShaderProps._EmissionColor))
		{
			dst.SetColor(ShaderProps._EmissionColor, src.GetColor(ShaderProps._EmissionColor));
		}
		if (src.HasVector(ShaderProps._EmissionUVScrollSpeed))
		{
			dst.SetVector(ShaderProps._EmissionUVScrollSpeed, src.GetVector(ShaderProps._EmissionUVScrollSpeed));
		}
		if (src.HasFloat(ShaderProps._EmissionDissolveEdgeSize))
		{
			dst.SetFloat(ShaderProps._EmissionDissolveEdgeSize, src.GetFloat(ShaderProps._EmissionDissolveEdgeSize));
		}
		if (src.HasFloat(ShaderProps._EmissionDissolveProgress))
		{
			dst.SetFloat(ShaderProps._EmissionDissolveProgress, src.GetFloat(ShaderProps._EmissionDissolveProgress));
		}
		if (src.HasVector(ShaderProps._EmissionDissolveAnimation))
		{
			dst.SetVector(ShaderProps._EmissionDissolveAnimation, src.GetVector(ShaderProps._EmissionDissolveAnimation));
		}
		if (src.HasFloat(ShaderProps._EmissionMaskByBaseMapAlpha))
		{
			dst.SetFloat(ShaderProps._EmissionMaskByBaseMapAlpha, src.GetFloat(ShaderProps._EmissionMaskByBaseMapAlpha));
		}
	}

	public static void SetGroupVisible(int groupId, bool visible)
	{
		byte b = (byte)(visible ? 1 : 0);
		for (int i = 0; i < _batchList.Count; i++)
		{
			DrawBatch value = _batchList[i];
			bool flag = false;
			int length = value.groupIds.Length;
			for (int j = 0; j < length; j++)
			{
				if (value.groupIds[j] == groupId && value.visibility[j] != b)
				{
					value.visibility[j] = b;
					value.visibleCount += (visible ? 1 : (-1));
					flag = true;
				}
			}
			if (flag)
			{
				value.dirty = true;
				_batchList[i] = value;
			}
		}
	}

	private static void _Render()
	{
		if (_batchList.Count == 0)
		{
			return;
		}
		if (!_loggedFirstRender)
		{
			_loggedFirstRender = true;
			int num = 0;
			for (int i = 0; i < _batchList.Count; i++)
			{
				num += _batchList[i].visibleCount;
			}
			Debug.Log($"[IndirectMeshRenderer] First render: {_batchList.Count} batch(es), {num} visible instance(s), stereoMul={2}");
		}
		for (int j = 0; j < _batchList.Count; j++)
		{
			DrawBatch batch = _batchList[j];
			if (batch.dynamicEntries != null)
			{
				for (int num2 = batch.dynamicEntries.Count - 1; num2 >= 0; num2--)
				{
					DynamicEntry dynamicEntry = batch.dynamicEntries[num2];
					if (dynamicEntry.transform == null)
					{
						if (batch.visibility[dynamicEntry.matrixIndex] != 0)
						{
							batch.visibility[dynamicEntry.matrixIndex] = 0;
							batch.visibleCount--;
							batch.dirty = true;
						}
						int index = batch.dynamicEntries.Count - 1;
						batch.dynamicEntries[num2] = batch.dynamicEntries[index];
						batch.dynamicEntries.RemoveAt(index);
					}
					else
					{
						batch.matrices[dynamicEntry.matrixIndex] = dynamicEntry.transform.localToWorldMatrix;
					}
				}
				if (!batch.dirty && batch.dynamicEntries.Count > 0)
				{
					batch.needsUpload = true;
				}
			}
			if (batch.visibleCount == 0)
			{
				if (batch.dirty)
				{
					_DisposeBatchBuffers(ref batch);
					batch.dirty = false;
					batch.needsUpload = false;
				}
				_batchList[j] = batch;
				continue;
			}
			if (batch.dirty)
			{
				_RebuildBatch(ref batch);
			}
			else if (batch.needsUpload)
			{
				_UploadBatch(ref batch);
			}
			_batchList[j] = batch;
			Graphics.RenderMeshIndirect(in batch.renderParams, batch.mesh, batch.commandBuffer, batch.submeshCount);
		}
	}

	private static void _RebuildBatch(ref DrawBatch batch)
	{
		int length = batch.matrices.Length;
		int num = batch.visibleCount * 2;
		_DisposeBatchBuffers(ref batch);
		batch.matrixBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, num, 64);
		batch.commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, batch.submeshCount, 20);
		if (!batch.gpuMatrices.IsCreated || batch.gpuMatrices.Length < num)
		{
			if (batch.gpuMatrices.IsCreated)
			{
				batch.gpuMatrices.Dispose();
			}
			batch.gpuMatrices = new NativeArray<Matrix4x4>(num, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		}
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		int num2 = 0;
		for (int i = 0; i < length; i++)
		{
			if (batch.visibility[i] != 0)
			{
				Matrix4x4 value = batch.matrices[i];
				Vector3 rhs = new Vector3(value.m03, value.m13, value.m23);
				vector = Vector3.Min(vector, rhs);
				vector2 = Vector3.Max(vector2, rhs);
				int num3 = num2 * 2;
				batch.gpuMatrices[num3] = value;
				batch.gpuMatrices[num3 + 1] = value;
				num2++;
			}
		}
		batch.matrixBuffer.SetData(batch.gpuMatrices, 0, 0, num);
		Vector3 vector3 = Vector3.one * 10f;
		Bounds worldBounds = new Bounds((vector + vector2) * 0.5f, vector2 - vector + vector3);
		if (!batch.commandData.IsCreated || batch.commandData.Length != batch.submeshCount)
		{
			if (batch.commandData.IsCreated)
			{
				batch.commandData.Dispose();
			}
			batch.commandData = new NativeArray<GraphicsBuffer.IndirectDrawIndexedArgs>(batch.submeshCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		}
		for (int j = 0; j < batch.submeshCount; j++)
		{
			batch.commandData[j] = new GraphicsBuffer.IndirectDrawIndexedArgs
			{
				indexCountPerInstance = batch.mesh.GetIndexCount(j),
				startIndex = batch.mesh.GetIndexStart(j),
				baseVertexIndex = batch.mesh.GetBaseVertex(j),
				startInstance = 0u,
				instanceCount = (uint)num
			};
		}
		batch.commandBuffer.SetData(batch.commandData);
		batch.renderParams = new RenderParams(batch.material)
		{
			worldBounds = worldBounds,
			layer = batch.layer,
			shadowCastingMode = ShadowCastingMode.Off,
			receiveShadows = false,
			matProps = new MaterialPropertyBlock()
		};
		batch.renderParams.matProps.SetBuffer(_spId_Matrices, batch.matrixBuffer);
		batch.dirty = false;
		batch.needsUpload = false;
	}

	private static void _UploadBatch(ref DrawBatch batch)
	{
		int length = batch.matrices.Length;
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		int num = 0;
		for (int i = 0; i < length; i++)
		{
			if (batch.visibility[i] != 0)
			{
				Matrix4x4 value = batch.matrices[i];
				Vector3 rhs = new Vector3(value.m03, value.m13, value.m23);
				vector = Vector3.Min(vector, rhs);
				vector2 = Vector3.Max(vector2, rhs);
				int num2 = num * 2;
				batch.gpuMatrices[num2] = value;
				batch.gpuMatrices[num2 + 1] = value;
				num++;
			}
		}
		batch.matrixBuffer.SetData(batch.gpuMatrices, 0, 0, num * 2);
		Vector3 vector3 = Vector3.one * 10f;
		batch.renderParams.worldBounds = new Bounds((vector + vector2) * 0.5f, vector2 - vector + vector3);
		batch.needsUpload = false;
	}

	private static void _DisposeBatchBuffers(ref DrawBatch batch)
	{
		batch.matrixBuffer?.Dispose();
		batch.matrixBuffer = null;
		batch.commandBuffer?.Dispose();
		batch.commandBuffer = null;
	}

	private static void _DisposeBatch(ref DrawBatch batch)
	{
		_DisposeBatchBuffers(ref batch);
		if (batch.matrices.IsCreated)
		{
			batch.matrices.Dispose();
		}
		if (batch.groupIds.IsCreated)
		{
			batch.groupIds.Dispose();
		}
		if (batch.visibility.IsCreated)
		{
			batch.visibility.Dispose();
		}
		if (batch.gpuMatrices.IsCreated)
		{
			batch.gpuMatrices.Dispose();
		}
		if (batch.commandData.IsCreated)
		{
			batch.commandData.Dispose();
		}
		if (batch.material != null)
		{
			UnityEngine.Object.Destroy(batch.material);
		}
		batch.dynamicEntries = null;
	}

	private static void _DisposeAll()
	{
		for (int i = 0; i < _batchList.Count; i++)
		{
			DrawBatch batch = _batchList[i];
			_DisposeBatch(ref batch);
		}
		_batchList.Clear();
		_batchLookup.Clear();
	}
}
