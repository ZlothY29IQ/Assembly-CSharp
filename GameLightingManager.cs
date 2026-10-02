using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public class GameLightingManager : MonoBehaviourTick, IGorillaSliceableSimple
{
	private struct LightInput
	{
		public Color color;

		public float intensity;

		public float intensityMult;
	}

	private struct LightDataPacked
	{
		public uint posXY;

		public uint posZW;

		public uint colorRG;

		public uint colorBA;

		public float range;
	}

	private struct LightDataLegacy
	{
		public float4 position;

		public float4 color;

		public float4 direction;
	}

	[OnEnterPlay_SetNull]
	public static volatile GameLightingManager instance;

	public const int MAX_VERTEX_LIGHTS = 100;

	public const int USE_MAX_VERTEX_LIGHTS = 50;

	public const int MAX_UPDATE_LIGHTS_PER_FRAME = 10;

	public Transform testLightsCenter;

	[ColorUsage(true, true)]
	public Color testAmbience = Color.black;

	[ColorUsage(true, true)]
	public Color testLightColor = Color.white;

	public float testLightBrightness = 10f;

	public float testLightRadius = 2f;

	public int maxUseTestLights = 1;

	[ReadOnly]
	[SerializeField]
	private List<GameLight> gameLights;

	private bool customVertexLightingEnabled;

	private bool desaturateAndTintEnabled;

	private Transform mainCameraTransform;

	private int zoneDynamicLightingEnableCount;

	private float[] sortKeys;

	private GameLight[] sortValues;

	private NativeArray<LightDataPacked> lightData;

	private NativeArray<LightDataLegacy> lightDataLegacy;

	private GraphicsBuffer lightDataBuffer;

	private GraphicsBuffer lightDataBufferLegacy;

	private const int GRID_CELL_CAPACITY = 10;

	private const int GRID_MAX_DIM = 16;

	private const int GRID_MIN_DIM = 4;

	private const float GLOBAL_LIGHT_RADIUS_THRESHOLD = 20f;

	private const float GLOBAL_LIGHT_RADIUS_THRESHOLD_SQR = 400f;

	private GraphicsBuffer gridCountsBuffer;

	private GraphicsBuffer gridIndicesBuffer;

	private uint[] gridCounts;

	private uint[] gridIndices;

	private float[] gridDistancesSqr;

	private int activeGlobalLightCount;

	private bool skipNextSlice;

	private bool immediateSort;

	private int nextLightUpdate;

	private int nextLightCacheUpdate;

	[SerializeField]
	private Light _GR_NearsightedDimLight;

	private static readonly int _shaderPropId_GameLight_UseMaxLights = Shader.PropertyToID("_GT_GameLight_UseMaxLights");

	private static readonly int _shaderPropId_GameLight_GlobalLightCount = Shader.PropertyToID("_GT_GameLight_GlobalLightCount");

	private static readonly int _shaderPropId_LightGridCounts = Shader.PropertyToID("_GT_LightGridCounts");

	private static readonly int _shaderPropId_LightGridIndices = Shader.PropertyToID("_GT_LightGridIndices");

	private static readonly int _shaderPropId_LightGrid_Origin = Shader.PropertyToID("_GT_LightGrid_Origin");

	private static readonly int _shaderPropId_LightGrid_InvCellSize = Shader.PropertyToID("_GT_LightGrid_InvCellSize");

	private static readonly int _shaderPropId_LightGrid_Dims = Shader.PropertyToID("_GT_LightGrid_Dims");

	private static readonly int _shaderPropId_LightGrid_CellCapacity = Shader.PropertyToID("_GT_LightGrid_CellCapacity");

	private static readonly int _shaderPropId_DesaturateAndTint_TintColor = Shader.PropertyToID("_GT_DesaturateAndTint_TintColor");

	private static readonly int _shaderPropId_DesaturateAndTint_TintAmount = Shader.PropertyToID("_GT_DesaturateAndTint_TintAmount");

	private static readonly int _shaderPropId_GameLight_Ambient_Color = Shader.PropertyToID("_GT_GameLight_Ambient_Color");

	private static readonly int _shaderPropId_GameLight_Lights = Shader.PropertyToID("_GT_GameLight_Lights");

	private static readonly int _shaderPropId_GameLight_LightsPacked = Shader.PropertyToID("_GT_GameLight_LightsPacked");

	public bool IsDynamicLightingEnabled => customVertexLightingEnabled;

	public Light GR_NearsightedDimLight => _GR_NearsightedDimLight;

	private static uint PackHalf2(float a, float b)
	{
		return (uint)(Mathf.FloatToHalf(a) | (Mathf.FloatToHalf(b) << 16));
	}

	private void Awake()
	{
		InitData();
	}

	private void InitData()
	{
		instance = this;
		gameLights = new List<GameLight>(512);
		sortKeys = new float[512];
		sortValues = new GameLight[512];
		lightDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 100, UnsafeUtility.SizeOf<LightDataPacked>());
		lightData = new NativeArray<LightDataPacked>(100, Allocator.Persistent);
		lightDataBufferLegacy = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 100, UnsafeUtility.SizeOf<LightDataLegacy>());
		lightDataLegacy = new NativeArray<LightDataLegacy>(100, Allocator.Persistent);
		int num = 4096;
		gridCounts = new uint[num];
		gridIndices = new uint[num * 10];
		gridDistancesSqr = new float[num * 10];
		gridCountsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, num, 4);
		gridIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, num * 10, 4);
		Shader.SetGlobalBuffer(_shaderPropId_LightGridCounts, gridCountsBuffer);
		Shader.SetGlobalBuffer(_shaderPropId_LightGridIndices, gridIndicesBuffer);
		Shader.SetGlobalInteger(_shaderPropId_LightGrid_CellCapacity, 10);
		nextLightUpdate = 0;
		ClearGameLights();
		SetDesaturateAndTintEnabled(enable: false, Color.black);
		SetAmbientLightDynamic(Color.black);
		SetCustomDynamicLightingEnabled(enable: false);
		SetMaxLights(50);
		StartCoroutine(Preheat());
	}

	private IEnumerator Preheat()
	{
		yield return null;
		SetCustomDynamicLightingEnabled(enable: true);
		yield return null;
		SetCustomDynamicLightingEnabled(enable: false);
	}

	private void OnDestroy()
	{
		ClearGameLights();
		SetDesaturateAndTintEnabled(enable: false, Color.black);
		SetAmbientLightDynamic(Color.black);
		SetCustomDynamicLightingEnabled(enable: false);
		lightDataBuffer?.Dispose();
		if (lightData.IsCreated)
		{
			lightData.Dispose();
		}
		lightDataBufferLegacy?.Dispose();
		if (lightDataLegacy.IsCreated)
		{
			lightDataLegacy.Dispose();
		}
		gridCountsBuffer?.Dispose();
		gridIndicesBuffer?.Dispose();
	}

	public new void OnEnable()
	{
		base.OnEnable();
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public new void OnDisable()
	{
		base.OnDisable();
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void ZoneEnableCustomDynamicLighting(bool enable)
	{
		if (enable)
		{
			if (zoneDynamicLightingEnableCount == 0)
			{
				SetCustomDynamicLightingEnabled(enable: true);
			}
			zoneDynamicLightingEnableCount++;
			return;
		}
		zoneDynamicLightingEnableCount--;
		if (zoneDynamicLightingEnableCount == 0)
		{
			SetCustomDynamicLightingEnabled(enable: false);
		}
		if (zoneDynamicLightingEnableCount < 0)
		{
			Debug.LogErrorFormat("Zone Dynamic Lighting Ref count is {0} and should never be less that 0", zoneDynamicLightingEnableCount);
			zoneDynamicLightingEnableCount = 0;
		}
	}

	public void SetCustomDynamicLightingEnabled(bool enable)
	{
		customVertexLightingEnabled = enable;
		if (customVertexLightingEnabled)
		{
			Shader.EnableKeyword("_ZONE_DYNAMIC_LIGHTS__CUSTOMVERTEX");
		}
		else
		{
			Shader.DisableKeyword("_ZONE_DYNAMIC_LIGHTS__CUSTOMVERTEX");
		}
	}

	public void ToggleCustomDynamicLightingEnabled()
	{
		SetCustomDynamicLightingEnabled(!customVertexLightingEnabled);
	}

	public void SetAmbientLightDynamic(Color color)
	{
		Shader.SetGlobalColor(_shaderPropId_GameLight_Ambient_Color, color);
	}

	public void SetMaxLights(int maxLights)
	{
		maxLights = Mathf.Min(maxLights, 100);
		maxUseTestLights = maxLights;
		Shader.SetGlobalInteger(_shaderPropId_GameLight_UseMaxLights, maxLights);
	}

	public void SetDesaturateAndTintEnabled(bool enable, Color tint)
	{
		Shader.SetGlobalColor(_shaderPropId_DesaturateAndTint_TintColor, tint);
		Shader.SetGlobalFloat(_shaderPropId_DesaturateAndTint_TintAmount, enable ? 1f : 0f);
		desaturateAndTintEnabled = enable;
	}

	public void SliceUpdate()
	{
		if (skipNextSlice)
		{
			skipNextSlice = false;
			return;
		}
		immediateSort = false;
		SortLights();
	}

	public void SortLights()
	{
		int count = gameLights.Count;
		if (count <= maxUseTestLights)
		{
			if (customVertexLightingEnabled)
			{
				int globalCount = PartitionGlobalLightsToFront(count);
				BuildAndUploadLightGrid(globalCount, count);
			}
			return;
		}
		if (mainCameraTransform == null)
		{
			mainCameraTransform = Camera.main.transform;
		}
		Vector3 position = mainCameraTransform.position;
		if (sortKeys == null || sortKeys.Length < count)
		{
			int num = Mathf.Max(count, (sortKeys != null) ? (sortKeys.Length * 2) : 64);
			sortKeys = new float[num];
			sortValues = new GameLight[num];
		}
		int num2 = 0;
		for (int i = 0; i < count; i++)
		{
			GameLight gameLight = gameLights[i];
			if (gameLight == null || gameLight.light == null)
			{
				sortKeys[i] = float.MaxValue;
			}
			else if (IsGlobalLight(gameLight))
			{
				sortKeys[i] = float.MinValue;
				num2++;
			}
			else
			{
				float num3 = Mathf.Clamp(gameLight.cachedColorAndIntensity.x + gameLight.cachedColorAndIntensity.y + gameLight.cachedColorAndIntensity.z, 0.01f, 6f);
				Vector3 vector = position - gameLight.cachedPosition;
				sortKeys[i] = (vector.x * vector.x + vector.y * vector.y + vector.z * vector.z) / num3;
			}
			sortValues[i] = gameLight;
		}
		Array.Sort(sortKeys, sortValues, 0, count);
		for (int j = 0; j < count; j++)
		{
			gameLights[j] = sortValues[j];
		}
		if (customVertexLightingEnabled)
		{
			int num4 = Mathf.Min(count, maxUseTestLights);
			BuildAndUploadLightGrid(Mathf.Min(num2, num4), num4);
		}
	}

	private static float ComputeInfluenceRadiusSqr(GameLight gl)
	{
		float num = ((gl.applyRange && gl.light.range > 0f) ? (0.005f / gl.light.range) : 0.005f);
		return 1f / num;
	}

	private static bool IsGlobalLight(GameLight gl)
	{
		return ComputeInfluenceRadiusSqr(gl) >= 400f;
	}

	private int PartitionGlobalLightsToFront(int count)
	{
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			GameLight gameLight = gameLights[i];
			if (gameLight != null && gameLight.light != null && IsGlobalLight(gameLight))
			{
				if (i != num)
				{
					gameLights[i] = gameLights[num];
					gameLights[num] = gameLight;
				}
				num++;
			}
		}
		return num;
	}

	public override void Tick()
	{
		RefreshLightData();
	}

	private void RefreshLightData()
	{
		if (lightDataBuffer == null || !customVertexLightingEnabled)
		{
			return;
		}
		int numLightsToPull = 10;
		if (immediateSort)
		{
			immediateSort = false;
			skipNextSlice = true;
			CacheAllLightData();
			SortLights();
			numLightsToPull = maxUseTestLights;
		}
		else
		{
			int numLightsToUpdateCache = 5;
			CacheLightDataForNonCloseLights(numLightsToUpdateCache);
		}
		PullLightData(numLightsToPull);
		int num = Mathf.Min(gameLights.Count, maxUseTestLights);
		if (num > 0)
		{
			bool num2 = CustomMapLoader.IsMapLoaded();
			lightDataBuffer.SetData(lightData, 0, 0, num);
			if (num2)
			{
				lightDataBufferLegacy.SetData(lightDataLegacy);
			}
			Shader.SetGlobalBuffer(_shaderPropId_GameLight_LightsPacked, lightDataBuffer);
			if (num2)
			{
				Shader.SetGlobalBuffer(_shaderPropId_GameLight_Lights, lightDataBufferLegacy);
			}
			Shader.SetGlobalInteger(_shaderPropId_GameLight_UseMaxLights, num);
		}
	}

	public void CacheAllLightData()
	{
		for (int i = 0; i < gameLights.Count; i++)
		{
			GameLight gameLight = gameLights[i];
			if (gameLight != null && gameLight.light != null)
			{
				gameLight.cachedPosition = gameLight.transform.position;
				gameLight.cachedColorAndIntensity = (float)gameLight.intensityMult * gameLight.light.intensity * (gameLight.negativeLight ? (-1f) : 1f) * gameLight.light.color;
			}
		}
	}

	private void BuildAndUploadLightGrid(int globalCount, int activeCount)
	{
		if (gridCountsBuffer == null)
		{
			return;
		}
		activeGlobalLightCount = Mathf.Clamp(globalCount, 0, Mathf.Max(activeCount, 0));
		int num = activeGlobalLightCount;
		Vector3 vector = Vector3.zero;
		Vector3 vector2 = Vector3.zero;
		bool flag = false;
		float num2 = 0f;
		for (int i = num; i < activeCount; i++)
		{
			GameLight gameLight = gameLights[i];
			if (!(gameLight == null) && !(gameLight.light == null))
			{
				Vector3 cachedPosition = gameLight.cachedPosition;
				if (!flag)
				{
					vector = cachedPosition;
					vector2 = cachedPosition;
					flag = true;
				}
				else
				{
					vector = Vector3.Min(vector, cachedPosition);
					vector2 = Vector3.Max(vector2, cachedPosition);
				}
				float num3 = ComputeInfluenceRadiusSqr(gameLight);
				if (num3 > num2)
				{
					num2 = num3;
				}
			}
		}
		float num4 = Mathf.Sqrt(num2);
		Vector3 vector3 = new Vector3(num4, num4, num4);
		vector -= vector3;
		vector2 += vector3;
		Vector3 vector4 = vector2 - vector;
		float num5 = Mathf.Max(vector4.x, Mathf.Max(vector4.y, vector4.z));
		float a = Mathf.Max(num4, num5 / 16f);
		a = Mathf.Max(a, 0.0001f);
		float num6 = 1f / a;
		int num7 = (flag ? Mathf.Clamp(Mathf.CeilToInt(vector4.x * num6), 1, 16) : 4);
		int num8 = (flag ? Mathf.Clamp(Mathf.CeilToInt(vector4.y * num6), 1, 16) : 4);
		int num9 = (flag ? Mathf.Clamp(Mathf.CeilToInt(vector4.z * num6), 1, 16) : 4);
		int num10 = num7 * num8 * num9;
		Array.Clear(gridCounts, 0, num10);
		if (flag)
		{
			for (int j = num; j < activeCount; j++)
			{
				GameLight gameLight2 = gameLights[j];
				if (gameLight2 == null || gameLight2.light == null)
				{
					continue;
				}
				Vector3 cachedPosition2 = gameLight2.cachedPosition;
				float num11 = Mathf.Sqrt(ComputeInfluenceRadiusSqr(gameLight2));
				int num12 = Mathf.Clamp(Mathf.FloorToInt((cachedPosition2.x - num11 - vector.x) * num6), 0, num7 - 1);
				int num13 = Mathf.Clamp(Mathf.FloorToInt((cachedPosition2.x + num11 - vector.x) * num6), 0, num7 - 1);
				int num14 = Mathf.Clamp(Mathf.FloorToInt((cachedPosition2.y - num11 - vector.y) * num6), 0, num8 - 1);
				int num15 = Mathf.Clamp(Mathf.FloorToInt((cachedPosition2.y + num11 - vector.y) * num6), 0, num8 - 1);
				int num16 = Mathf.Clamp(Mathf.FloorToInt((cachedPosition2.z - num11 - vector.z) * num6), 0, num9 - 1);
				int num17 = Mathf.Clamp(Mathf.FloorToInt((cachedPosition2.z + num11 - vector.z) * num6), 0, num9 - 1);
				for (int k = num16; k <= num17; k++)
				{
					for (int l = num14; l <= num15; l++)
					{
						for (int m = num12; m <= num13; m++)
						{
							int num18 = m + num7 * (l + num8 * k);
							int num19 = num18 * 10;
							Vector3 vector5 = new Vector3(vector.x + ((float)m + 0.5f) * a, vector.y + ((float)l + 0.5f) * a, vector.z + ((float)k + 0.5f) * a);
							float sqrMagnitude = (cachedPosition2 - vector5).sqrMagnitude;
							uint num20 = gridCounts[num18];
							if (num20 < 10)
							{
								gridIndices[num19 + (int)num20] = (uint)j;
								gridDistancesSqr[num19 + (int)num20] = sqrMagnitude;
								gridCounts[num18] = num20 + 1;
								continue;
							}
							int num21 = -1;
							float num22 = sqrMagnitude;
							for (int n = 0; n < 10; n++)
							{
								if (gridDistancesSqr[num19 + n] > num22)
								{
									num22 = gridDistancesSqr[num19 + n];
									num21 = n;
								}
							}
							if (num21 >= 0)
							{
								gridIndices[num19 + num21] = (uint)j;
								gridDistancesSqr[num19 + num21] = sqrMagnitude;
							}
						}
					}
				}
			}
		}
		gridCountsBuffer.SetData(gridCounts, 0, 0, num10);
		gridIndicesBuffer.SetData(gridIndices, 0, 0, num10 * 10);
		Shader.SetGlobalVector(_shaderPropId_LightGrid_Origin, new Vector4(vector.x, vector.y, vector.z, 0f));
		Shader.SetGlobalFloat(_shaderPropId_LightGrid_InvCellSize, num6);
		Shader.SetGlobalVector(_shaderPropId_LightGrid_Dims, new Vector4(num7, num8, num9, 0f));
		Shader.SetGlobalInteger(_shaderPropId_GameLight_GlobalLightCount, activeGlobalLightCount);
	}

	public void CacheLightDataForNonCloseLights(int numLightsToUpdateCache)
	{
		int num = gameLights.Count - maxUseTestLights;
		if (num <= 0)
		{
			return;
		}
		for (int i = 0; i < numLightsToUpdateCache; i++)
		{
			nextLightCacheUpdate = (nextLightCacheUpdate + 1) % num;
			GameLight gameLight = gameLights[maxUseTestLights + nextLightCacheUpdate];
			if (gameLight != null && gameLight.light != null)
			{
				gameLight.cachedPosition = gameLight.transform.position;
				gameLight.cachedColorAndIntensity = (float)gameLight.intensityMult * gameLight.light.intensity * (gameLight.negativeLight ? (-1f) : 1f) * gameLight.light.color;
			}
		}
	}

	public void PullLightData(int numLightsToPull)
	{
		for (int i = 0; i < maxUseTestLights; i++)
		{
			if (i < gameLights.Count && gameLights[i] != null && gameLights[i].isHighPriorityPlayerLight)
			{
				GetFromLight(i, i);
			}
		}
		for (int j = 0; j < numLightsToPull; j++)
		{
			nextLightUpdate = (nextLightUpdate + 1) % maxUseTestLights;
			if (nextLightUpdate < gameLights.Count)
			{
				GetFromLight(nextLightUpdate, nextLightUpdate);
				if (gameLights[nextLightUpdate] != null && !gameLights[nextLightUpdate].isHighPriorityPlayerLight)
				{
				}
			}
			else
			{
				ResetLight(nextLightUpdate);
			}
		}
	}

	public int AddGameLight(GameLight light, bool ignoreUnityLightDisable = false)
	{
		if (light == null || !light.gameObject.activeInHierarchy || light.light == null || !light.light.enabled)
		{
			return -1;
		}
		if (light.IsRegistered)
		{
			return -1;
		}
		if (!ignoreUnityLightDisable)
		{
			light.light.enabled = false;
		}
		gameLights.Add(light);
		immediateSort = true;
		return gameLights.Count - 1;
	}

	public void RemoveGameLight(GameLight light)
	{
		if (light != null && light.light != null)
		{
			light.light.enabled = true;
		}
		if (light != null)
		{
			light.lightId = -1;
		}
		int num = gameLights.IndexOf(light);
		if (num < 0)
		{
			return;
		}
		gameLights.RemoveAt(num);
		if (CustomMapLoader.IsMapLoaded())
		{
			int count = gameLights.Count;
			if (count < 100)
			{
				lightDataLegacy[count] = default(LightDataLegacy);
			}
		}
	}

	public void ClearGameLights()
	{
		if (gameLights != null)
		{
			gameLights.Clear();
		}
		if (lightDataBuffer != null)
		{
			for (int i = 0; i < 100; i++)
			{
				ResetLight(i);
			}
			lightDataBuffer.SetData(lightData);
			Shader.SetGlobalBuffer(_shaderPropId_GameLight_LightsPacked, lightDataBuffer);
			if (CustomMapLoader.IsMapLoaded())
			{
				lightDataBufferLegacy.SetData(lightDataLegacy);
				Shader.SetGlobalBuffer(_shaderPropId_GameLight_Lights, lightDataBufferLegacy);
			}
		}
	}

	public void GetFromLight(int lightIndex, int gameLightIndex)
	{
		if (lightDataBuffer == null)
		{
			return;
		}
		GameLight gameLight = null;
		if (gameLightIndex >= 0 && gameLightIndex < gameLights.Count)
		{
			gameLight = gameLights[gameLightIndex];
		}
		if (!(gameLight == null) && !(gameLight.light == null))
		{
			gameLight.cachedPosition = gameLight.transform.position;
			gameLight.cachedColorAndIntensity = (float)gameLight.intensityMult * gameLight.light.intensity * (gameLight.negativeLight ? (-1f) : 1f) * gameLight.light.color;
			if (gameLight.applyRange && gameLight.light.range > 0f)
			{
				gameLight.range = 0.005f / gameLight.light.range;
			}
			Vector3 cachedPosition = gameLight.cachedPosition;
			Vector4 cachedColorAndIntensity = gameLight.cachedColorAndIntensity;
			lightData[lightIndex] = new LightDataPacked
			{
				posXY = PackHalf2(cachedPosition.x, cachedPosition.y),
				posZW = PackHalf2(cachedPosition.z, 1f),
				colorRG = PackHalf2(cachedColorAndIntensity.x, cachedColorAndIntensity.y),
				colorBA = PackHalf2(cachedColorAndIntensity.z, cachedColorAndIntensity.w),
				range = gameLight.range
			};
			lightDataLegacy[lightIndex] = new LightDataLegacy
			{
				position = new float4(cachedPosition.x, cachedPosition.y, cachedPosition.z, 1f),
				color = new float4(cachedColorAndIntensity.x, cachedColorAndIntensity.y, cachedColorAndIntensity.z, cachedColorAndIntensity.w),
				direction = float4.zero
			};
		}
	}

	private void ResetLight(int lightIndex)
	{
		lightData[lightIndex] = default(LightDataPacked);
		lightDataLegacy[lightIndex] = default(LightDataLegacy);
	}
}
