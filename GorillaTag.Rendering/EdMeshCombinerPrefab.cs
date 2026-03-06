using System;
using System.Collections.Generic;
using GorillaExtensions;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GorillaTag.Rendering;

[DefaultExecutionOrder(-2147482648)]
public class EdMeshCombinerPrefab : MonoBehaviour
{
	[Serializable]
	public struct CombinerInfo
	{
		public MeshFilter meshFilter;

		public Renderer renderer;

		public EdMeshCombinerModifierUVOffset uvOffsetModifier;

		public int subMeshIndex;

		public bool isSkinnedMesh;

		public int layer;
	}

	private struct CombinerCriteria
	{
		public Material mat;

		public int staticFlags;

		public int lightmapIndex;

		public bool hasMeshCollider;

		public PhysicsMaterial meshCollPhysicsMat;

		public int surfOverrideIndex;

		public float surfExtraVelMultiplier;

		public float surfExtraVelMaxMultiplier;

		public bool surfSendOnTapEvent;

		public UnityLayer objectLayer;

		public override int GetHashCode()
		{
			return HashCode.Combine(mat.GetInstanceID(), staticFlags, lightmapIndex, hasMeshCollider, surfOverrideIndex, surfExtraVelMultiplier, surfExtraVelMaxMultiplier, surfSendOnTapEvent);
		}
	}

	[BurstCompile]
	private struct CopyMeshJob : IJob
	{
		[ReadOnly]
		public Mesh.MeshDataArray meshDataArray;

		[ReadOnly]
		public NativeArray<int> sourceSubmeshIndices;

		[ReadOnly]
		public NativeArray<Matrix4x4> sourceTransforms;

		[ReadOnly]
		public NativeArray<float4> lightmapScaleOffsets;

		[ReadOnly]
		public NativeArray<Color> baseColors;

		[ReadOnly]
		public NativeArray<int> atlasSlices;

		[ReadOnly]
		public NativeArray<float4> uvModifiersMinMax;

		public bool isCandleFlame;

		public uint randSeed;

		[WriteOnly]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<GTVertexDataStream0> dst0;

		[WriteOnly]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<GTVertexDataStream1> dst1;

		[WriteOnly]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<int> idxDst32;

		[WriteOnly]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<ushort> idxDst16;

		public bool use32BitIndices;

		public void Execute()
		{
			int num = 0;
			int num2 = 0;
			Unity.Mathematics.Random random = new Unity.Mathematics.Random(randSeed);
			for (int i = 0; i < meshDataArray.Length; i++)
			{
				Mesh.MeshData meshData = meshDataArray[i];
				int num3 = sourceSubmeshIndices[i];
				SubMeshDescriptor subMesh = meshData.GetSubMesh(num3);
				int vertexCount = meshData.vertexCount;
				int indexCount = subMesh.indexCount;
				Matrix4x4 matrix4x = sourceTransforms[i];
				bool flag = math.determinant(matrix4x) < 0f;
				NativeArray<Vector3> outVertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				if (meshData.HasVertexAttribute(VertexAttribute.Position))
				{
					meshData.GetVertices(outVertices);
				}
				else
				{
					for (int j = 0; j < vertexCount; j++)
					{
						outVertices[j] = Vector3.zero;
					}
				}
				NativeArray<Vector3> outNormals = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
				if (meshData.HasVertexAttribute(VertexAttribute.Normal))
				{
					meshData.GetNormals(outNormals);
				}
				else
				{
					for (int k = 0; k < vertexCount; k++)
					{
						outNormals[k] = Vector3.up;
					}
				}
				NativeArray<Vector4> outTangents = new NativeArray<Vector4>(vertexCount, Allocator.Temp);
				if (meshData.HasVertexAttribute(VertexAttribute.Tangent))
				{
					meshData.GetTangents(outTangents);
				}
				else
				{
					for (int l = 0; l < vertexCount; l++)
					{
						outTangents[l] = new Vector4(1f, 0f, 0f, 1f);
					}
				}
				NativeArray<Color> outColors = new NativeArray<Color>(vertexCount, Allocator.Temp);
				if (meshData.HasVertexAttribute(VertexAttribute.Color))
				{
					meshData.GetColors(outColors);
				}
				else
				{
					for (int m = 0; m < vertexCount; m++)
					{
						outColors[m] = Color.white;
					}
				}
				NativeArray<Vector2> outUVs = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
				if (meshData.HasVertexAttribute(VertexAttribute.TexCoord0))
				{
					meshData.GetUVs(0, outUVs);
				}
				else
				{
					for (int n = 0; n < vertexCount; n++)
					{
						outUVs[n] = Vector2.zero;
					}
				}
				NativeArray<Vector2> outUVs2 = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
				if (meshData.HasVertexAttribute(VertexAttribute.TexCoord1))
				{
					meshData.GetUVs(1, outUVs2);
				}
				else
				{
					for (int num4 = 0; num4 < vertexCount; num4++)
					{
						outUVs2[num4] = Vector2.zero;
					}
				}
				Color color = baseColors[i];
				int num5 = atlasSlices[i];
				Vector4 vector = uvModifiersMinMax[i];
				Vector2 vector2 = new Vector2(random.NextFloat(vector.x, vector.z), random.NextFloat(vector.y, vector.w));
				float num6 = (isCandleFlame ? random.NextFloat(0f, 1f) : 1f);
				Matrix4x4 transpose = matrix4x.inverse.transpose;
				for (int num7 = 0; num7 < vertexCount; num7++)
				{
					Vector3 point = outVertices[num7];
					Vector3 vector3 = outNormals[num7];
					Vector4 vector4 = outTangents[num7];
					Color color2 = outColors[num7];
					Vector2 vector5 = outUVs[num7];
					Vector3 vector6 = matrix4x.MultiplyPoint3x4(point);
					Vector3 vector7 = transpose.MultiplyVector(vector3).normalized;
					Vector3 vector8 = transpose.MultiplyVector(new Vector3(vector4.x, vector4.y, vector4.z)).normalized;
					if (flag)
					{
						vector7 = -vector7;
						vector8 = -vector8;
						vector4.w = 0f - vector4.w;
					}
					GTVertexDataStream0 value = new GTVertexDataStream0
					{
						position = vector6,
						color = new Color(color2.r * color.r, color2.g * color.g, color2.b * color.b, isCandleFlame ? num6 : (color2.a * color.a)),
						uv1 = new half4((half)(vector5.x + vector2.x), (half)(vector5.y + vector2.y), (half)num5, (half)num6),
						lightmapUv = new half2((half)(outUVs2[num7].x * lightmapScaleOffsets[i].x + lightmapScaleOffsets[i].z), (half)(outUVs2[num7].y * lightmapScaleOffsets[i].y + lightmapScaleOffsets[i].w))
					};
					dst0[num + num7] = value;
					GTVertexDataStream1 value2 = new GTVertexDataStream1
					{
						normal = vector7,
						tangent = new Color(vector8.x, vector8.y, vector8.z, vector4.w)
					};
					dst1[num + num7] = value2;
				}
				if (use32BitIndices)
				{
					NativeArray<int> outIndices = new NativeArray<int>(indexCount, Allocator.Temp);
					meshData.GetIndices(outIndices, num3);
					if (!flag)
					{
						for (int num8 = 0; num8 < indexCount; num8++)
						{
							idxDst32[num2 + num8] = num + outIndices[num8];
						}
					}
					else
					{
						for (int num9 = 0; num9 < indexCount; num9 += 3)
						{
							idxDst32[num2 + num9] = num + outIndices[num9 + 2];
							idxDst32[num2 + num9 + 1] = num + outIndices[num9 + 1];
							idxDst32[num2 + num9 + 2] = num + outIndices[num9];
						}
					}
					outIndices.Dispose();
				}
				else
				{
					NativeArray<ushort> outIndices2 = new NativeArray<ushort>(indexCount, Allocator.Temp);
					meshData.GetIndices(outIndices2, num3);
					if (!flag)
					{
						for (int num10 = 0; num10 < indexCount; num10++)
						{
							idxDst16[num2 + num10] = (ushort)(num + outIndices2[num10]);
						}
					}
					else
					{
						for (int num11 = 0; num11 < indexCount; num11 += 3)
						{
							idxDst16[num2 + num11] = (ushort)(num + outIndices2[num11 + 2]);
							idxDst16[num2 + num11 + 1] = (ushort)(num + outIndices2[num11 + 1]);
							idxDst16[num2 + num11 + 2] = (ushort)(num + outIndices2[num11]);
						}
					}
					outIndices2.Dispose();
				}
				outVertices.Dispose();
				outNormals.Dispose();
				outTangents.Dispose();
				outColors.Dispose();
				outUVs.Dispose();
				outUVs2.Dispose();
				num += vertexCount;
				num2 += indexCount;
			}
		}
	}

	public EdMeshCombinedPrefabData combinedData;

	private const uint _k_maxVertsForUInt16 = 65535u;

	private const uint _k_maxVertsForUInt32 = uint.MaxValue;

	private const uint _k_maxVertCount = 65535u;

	private void Awake()
	{
		if (combinedData == null)
		{
			combinedData = new EdMeshCombinedPrefabData();
		}
		CombineMeshesRuntime(this, undo: false, combinedData);
	}

	private static void Special_MarkDoNotCombine(Component component)
	{
		if (component != null)
		{
			GameObject gameObject = component.gameObject;
			if (gameObject.GetComponent<EdDoNotMeshCombine>() == null)
			{
				gameObject.AddComponent<EdDoNotMeshCombine>();
			}
		}
	}

	public static void CombineMeshesRuntime(EdMeshCombinerPrefab combiner, bool undo = false, EdMeshCombinedPrefabData combinedPrefabData = null)
	{
		bool flag = true;
		Campfire[] componentsInChildren = combiner.GetComponentsInChildren<Campfire>(includeInactive: true);
		foreach (Campfire obj in componentsInChildren)
		{
			Special_MarkDoNotCombine(obj.baseFire);
			Special_MarkDoNotCombine(obj.middleFire);
			Special_MarkDoNotCombine(obj.topFire);
		}
		GameEntity[] componentsInChildren2 = combiner.GetComponentsInChildren<GameEntity>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			Special_MarkDoNotCombine(componentsInChildren2[i]);
		}
		StaticLodGroup[] componentsInChildren3 = combiner.GetComponentsInChildren<StaticLodGroup>(includeInactive: true);
		for (int i = 0; i < componentsInChildren3.Length; i++)
		{
			Special_MarkDoNotCombine(componentsInChildren3[i]);
		}
		GorillaCaveCrystalVisuals[] componentsInChildren4 = combiner.GetComponentsInChildren<GorillaCaveCrystalVisuals>(includeInactive: false);
		for (int i = 0; i < componentsInChildren4.Length; i++)
		{
			Special_MarkDoNotCombine(componentsInChildren4[i]);
		}
		WaterSurfaceMaterialController[] componentsInChildren5 = combiner.GetComponentsInChildren<WaterSurfaceMaterialController>(includeInactive: false);
		for (int i = 0; i < componentsInChildren5.Length; i++)
		{
			Special_MarkDoNotCombine(componentsInChildren5[i]);
		}
		List<Renderer> componentsInChildrenUntil = combiner.GetComponentsInChildrenUntil<Renderer, EdDoNotMeshCombine, EdMeshCombinerPrefab, TMP_Text>(includeInactive: false, stopAtRoot: false);
		List<Renderer> list = new List<Renderer>(componentsInChildrenUntil.Count);
		foreach (Renderer item3 in componentsInChildrenUntil)
		{
			if (item3 is SkinnedMeshRenderer || item3 is MeshRenderer)
			{
				list.Add(item3);
			}
		}
		Dictionary<CombinerCriteria, List<List<CombinerInfo>>> dictionary = new Dictionary<CombinerCriteria, List<List<CombinerInfo>>>(list.Count);
		List<Transform> list2 = new List<Transform>(list.Count);
		CombinerCriteria key;
		foreach (Renderer item4 in list)
		{
			if (!item4.enabled)
			{
				continue;
			}
			GameObject gameObject = item4.gameObject;
			int staticFlags = (gameObject.isStatic ? 1 : 0);
			if (!gameObject.isStatic)
			{
				continue;
			}
			SkinnedMeshRenderer skinnedMeshRenderer = item4 as SkinnedMeshRenderer;
			bool flag2 = skinnedMeshRenderer != null;
			MeshFilter meshFilter = null;
			Mesh sharedMesh;
			if (flag2)
			{
				sharedMesh = skinnedMeshRenderer.sharedMesh;
			}
			else
			{
				meshFilter = item4.GetComponent<MeshFilter>();
				if (meshFilter == null)
				{
					continue;
				}
				sharedMesh = meshFilter.sharedMesh;
			}
			if (sharedMesh == null || (long)sharedMesh.vertexCount >= 65535L)
			{
				continue;
			}
			MeshCollider component = item4.GetComponent<MeshCollider>();
			bool flag3 = component != null;
			if (!flag && flag3 && (component.sharedMesh == null || component.convex || component.sharedMesh != sharedMesh))
			{
				continue;
			}
			GorillaSurfaceOverride component2 = item4.GetComponent<GorillaSurfaceOverride>();
			int num = ((component2 != null) ? component2.overrideIndex : 0);
			int num2 = Mathf.Min(item4.sharedMaterials.Length, sharedMesh.subMeshCount);
			if (num2 == 0)
			{
				continue;
			}
			int num3 = 0;
			int num4 = 0;
			for (int j = 0; j < num2; j++)
			{
				num3 += ((sharedMesh.GetSubMesh(j).topology != MeshTopology.Triangles) ? 1 : 0);
				num4 += ((item4.sharedMaterials[j] == null) ? 1 : 0);
			}
			if (num3 > 0)
			{
				string text = "?????";
				Debug.LogError($"Cannot combine mesh \"{sharedMesh.name}\" because it has {num3} submeshes with " + "a non-triangle topology. Verify FBX import settings does not have \"Keep Quads\" on.\n  - Asset path=\"" + text + "\"\n  - Path in scene=" + item4.transform.GetPathQ(), sharedMesh);
				continue;
			}
			if (num4 > 0)
			{
				Debug.LogError("EdMeshCombinerPrefab: Cannot combine Renderer \"" + combiner.name + "\" because it does not have " + $"{num4} materials assigned. Path in scene={combiner.transform.GetPathQ()}", combiner);
				continue;
			}
			for (int k = 0; k < num2; k++)
			{
				Material mat = item4.sharedMaterials[k];
				int layer = item4.gameObject.layer;
				key = new CombinerCriteria
				{
					mat = mat,
					staticFlags = staticFlags,
					lightmapIndex = item4.lightmapIndex,
					hasMeshCollider = (!flag && flag3),
					meshCollPhysicsMat = (flag ? null : (flag3 ? component.sharedMaterial : null)),
					surfOverrideIndex = ((!flag) ? num : 0),
					surfExtraVelMultiplier = (flag ? 0f : ((component2 != null) ? component2.extraVelMultiplier : 1f)),
					surfExtraVelMaxMultiplier = (flag ? 0f : ((component2 != null) ? component2.extraVelMaxMultiplier : 1f)),
					surfSendOnTapEvent = (!flag && component2 != null && component2.sendOnTapEvent),
					objectLayer = ((layer == 27) ? UnityLayer.NoMirror : UnityLayer.Default)
				};
				CombinerCriteria key2 = key;
				if (!dictionary.TryGetValue(key2, out var value))
				{
					value = (dictionary[key2] = new List<List<CombinerInfo>>
					{
						new List<CombinerInfo>(1)
					});
				}
				int index = value.Count - 1;
				int num5 = sharedMesh.vertexCount;
				foreach (CombinerInfo item5 in value[index])
				{
					if (item5.isSkinnedMesh)
					{
						SkinnedMeshRenderer skinnedMeshRenderer2 = (SkinnedMeshRenderer)item5.renderer;
						num5 += skinnedMeshRenderer2.sharedMesh.vertexCount;
					}
					else
					{
						num5 += item5.meshFilter.sharedMesh.vertexCount;
					}
				}
				if ((long)num5 >= 65535L)
				{
					index = value.Count;
					value.Add(new List<CombinerInfo>(1));
				}
				list2.Add(gameObject.transform);
				value[index].Add(new CombinerInfo
				{
					meshFilter = meshFilter,
					renderer = item4,
					uvOffsetModifier = item4.GetComponent<EdMeshCombinerModifierUVOffset>(),
					subMeshIndex = k,
					isSkinnedMesh = flag2,
					layer = item4.sortingLayerID
				});
			}
		}
		Matrix4x4 worldToLocalMatrix = combiner.transform.worldToLocalMatrix;
		PerSceneRenderData perSceneRenderData = null;
		bool flag4 = false;
		new Unity.Mathematics.Random(6746u);
		foreach (KeyValuePair<CombinerCriteria, List<List<CombinerInfo>>> item6 in dictionary)
		{
			item6.Deconstruct(out key, out var value2);
			CombinerCriteria combinerCriteria = key;
			List<List<CombinerInfo>> list4 = value2;
			bool isCandleFlame = false;
			foreach (List<CombinerInfo> item7 in list4)
			{
				List<Mesh> list5 = new List<Mesh>(item7.Count);
				List<int> list6 = new List<int>(item7.Count);
				List<Matrix4x4> list7 = new List<Matrix4x4>(item7.Count);
				List<Color> list8 = new List<Color>(item7.Count);
				List<int> list9 = new List<int>(item7.Count);
				List<float4> list10 = new List<float4>(item7.Count);
				List<float4> list11 = new List<float4>(item7.Count);
				Dictionary<(Renderer, int), (Color, int)> dictionary2 = new Dictionary<(Renderer, int), (Color, int)>();
				foreach (CombinerInfo item8 in item7)
				{
					if (item8.renderer.TryGetComponent<MaterialCombinerPerRendererMono>(out var component3) && component3.TryGetData(item8.renderer, item8.subMeshIndex, out var data))
					{
						dictionary2[(item8.renderer, item8.subMeshIndex)] = (data.baseColor, data.sliceIndex);
					}
					else
					{
						dictionary2[(item8.renderer, item8.subMeshIndex)] = (Color.white, -1);
					}
				}
				for (int l = 0; l < item7.Count; l++)
				{
					CombinerInfo combinerInfo = item7[l];
					Mesh mesh;
					if (combinerInfo.isSkinnedMesh)
					{
						SkinnedMeshRenderer obj2 = (SkinnedMeshRenderer)combinerInfo.renderer;
						mesh = new Mesh();
						obj2.BakeMesh(mesh, useScale: true);
					}
					else
					{
						mesh = combinerInfo.meshFilter.sharedMesh;
					}
					if (mesh.vertexCount != 0)
					{
						if (perSceneRenderData != null && perSceneRenderData.representativeRenderer == combinerInfo.renderer)
						{
							flag4 = true;
						}
						list5.Add(mesh);
						list6.Add(combinerInfo.subMeshIndex);
						list7.Add(worldToLocalMatrix * combinerInfo.renderer.transform.localToWorldMatrix);
						list11.Add(((object)combinerInfo.uvOffsetModifier == null) ? float4.zero : new float4(combinerInfo.uvOffsetModifier.minUvOffset.x, combinerInfo.uvOffsetModifier.minUvOffset.y, combinerInfo.uvOffsetModifier.maxUvOffset.x, combinerInfo.uvOffsetModifier.maxUvOffset.y));
						list10.Add(combinerInfo.renderer.lightmapScaleOffset);
						var (item, item2) = dictionary2[(combinerInfo.renderer, combinerInfo.subMeshIndex)];
						list8.Add(item);
						list9.Add(item2);
					}
				}
				using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(list5);
				int num6 = 0;
				int num7 = 0;
				for (int m = 0; m < meshDataArray.Length; m++)
				{
					Mesh.MeshData meshData = meshDataArray[m];
					num6 += meshData.vertexCount;
					num7 += meshData.GetSubMesh(list6[m]).indexCount;
				}
				Mesh.MeshDataArray data2 = Mesh.AllocateWritableMeshData(1);
				Mesh.MeshData writeData = data2[0];
				IndexFormat indexFormat = ((num6 > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16);
				GTVertexDataStreams_Descriptors.DoSetVertexBufferParams(ref writeData, num6);
				writeData.SetIndexBufferParams(num7, indexFormat);
				writeData.subMeshCount = 1;
				NativeArray<int> idxDst = default(NativeArray<int>);
				NativeArray<ushort> idxDst2 = default(NativeArray<ushort>);
				if (indexFormat == IndexFormat.UInt32)
				{
					idxDst = writeData.GetIndexData<int>();
				}
				else
				{
					idxDst2 = writeData.GetIndexData<ushort>();
				}
				CopyMeshJob jobData = new CopyMeshJob
				{
					meshDataArray = meshDataArray,
					sourceSubmeshIndices = new NativeArray<int>(list6.ToArray(), Allocator.TempJob),
					sourceTransforms = new NativeArray<Matrix4x4>(list7.ToArray(), Allocator.TempJob),
					lightmapScaleOffsets = new NativeArray<float4>(list10.ToArray(), Allocator.TempJob),
					baseColors = new NativeArray<Color>(list8.ToArray(), Allocator.TempJob),
					atlasSlices = new NativeArray<int>(list9.ToArray(), Allocator.TempJob),
					uvModifiersMinMax = new NativeArray<float4>(list11.ToArray(), Allocator.TempJob),
					isCandleFlame = isCandleFlame,
					randSeed = 6746u,
					dst0 = writeData.GetVertexData<GTVertexDataStream0>(),
					dst1 = writeData.GetVertexData<GTVertexDataStream1>(1),
					idxDst32 = idxDst,
					idxDst16 = idxDst2,
					use32BitIndices = (indexFormat == IndexFormat.UInt32)
				};
				jobData.Schedule().Complete();
				jobData.sourceSubmeshIndices.Dispose();
				jobData.sourceTransforms.Dispose();
				jobData.baseColors.Dispose();
				jobData.atlasSlices.Dispose();
				jobData.uvModifiersMinMax.Dispose();
				writeData.SetSubMesh(0, new SubMeshDescriptor(0, num7));
				Mesh mesh2 = new Mesh();
				Mesh.ApplyAndDisposeWritableMeshData(data2, mesh2);
				mesh2.RecalculateBounds();
				GameObject gameObject2 = new GameObject(combinerCriteria.mat.name + " (combined by EdMeshCombinerPrefab)");
				combinedPrefabData?.combined.Add(gameObject2);
				if (combiner.transform != null)
				{
					gameObject2.transform.parent = combiner.transform;
				}
				else
				{
					SceneManager.MoveGameObjectToScene(gameObject2, combiner.gameObject.scene);
				}
				gameObject2.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
				gameObject2.transform.localScale = Vector3.one;
				gameObject2.isStatic = true;
				gameObject2.layer = (int)combinerCriteria.objectLayer;
				MeshRenderer meshRenderer = gameObject2.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = combinerCriteria.mat;
				meshRenderer.lightmapIndex = combinerCriteria.lightmapIndex;
				if (flag4)
				{
					perSceneRenderData.representativeRenderer = meshRenderer;
				}
				if (perSceneRenderData != null)
				{
					perSceneRenderData.AddMeshToList(gameObject2, meshRenderer);
				}
				MeshFilter meshFilter2 = gameObject2.AddComponent<MeshFilter>();
				meshFilter2.sharedMesh = mesh2;
				if (!flag && combinerCriteria.hasMeshCollider)
				{
					MeshCollider meshCollider = gameObject2.AddComponent<MeshCollider>();
					meshCollider.sharedMesh = meshFilter2.sharedMesh;
					meshCollider.convex = false;
					meshCollider.sharedMaterial = combinerCriteria.meshCollPhysicsMat;
					GorillaSurfaceOverride gorillaSurfaceOverride = gameObject2.AddComponent<GorillaSurfaceOverride>();
					gorillaSurfaceOverride.overrideIndex = combinerCriteria.surfOverrideIndex;
					gorillaSurfaceOverride.extraVelMultiplier = combinerCriteria.surfExtraVelMultiplier;
					gorillaSurfaceOverride.extraVelMaxMultiplier = combinerCriteria.surfExtraVelMaxMultiplier;
					gorillaSurfaceOverride.sendOnTapEvent = combinerCriteria.surfSendOnTapEvent;
				}
			}
		}
		list2.Sort((Transform a, Transform b) => -a.GetDepth().CompareTo(b.GetDepth()));
		foreach (Transform item9 in list2)
		{
			if (!(item9 == null) && combinedPrefabData != null)
			{
				MeshRenderer component4 = item9.GetComponent<MeshRenderer>();
				if (component4 != null)
				{
					component4.enabled = false;
					combinedPrefabData.disabled.Add(component4);
				}
				SkinnedMeshRenderer component5 = item9.GetComponent<SkinnedMeshRenderer>();
				if (component5 != null)
				{
					component5.enabled = false;
					combinedPrefabData.disabled.Add(component5);
				}
			}
		}
	}

	protected void OnEnable()
	{
	}
}
