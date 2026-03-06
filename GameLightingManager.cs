using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class GameLightingManager : MonoBehaviourTick, IGorillaSliceableSimple
{
	public struct LightInput
	{
		public Color color;

		public float intensity;

		public float intensityMult;
	}

	public struct LightData
	{
		public Vector4 lightPos;

		public Vector4 lightColor;

		public Vector4 lightDirection;
	}

	[OnEnterPlay_SetNull]
	public static volatile GameLightingManager instance;

	public const int MAX_VERTEX_LIGHTS = 50;

	public const int USE_MAX_VERTEX_LIGHTS = 20;

	public const int MAX_UPDATE_LIGHTS_PER_FRAME = 10;

	private const int MAX_LIGHT_POWER = 100;

	private const int LIGHT_POWER_BIN_SIZE = 5;

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

	private List<GameLight>[] lightDistanceBins = new List<GameLight>[20];

	private NativeArray<LightData> lightData;

	private GraphicsBuffer lightDataBuffer;

	private Vector3 cameraPosForSort;

	private bool skipNextSlice;

	private bool immediateSort;

	private int nextLightUpdate;

	private int nextLightCacheUpdate;

	public bool IsDynamicLightingEnabled => customVertexLightingEnabled;

	private void Awake()
	{
		InitData();
	}

	private void InitData()
	{
		instance = this;
		gameLights = new List<GameLight>(512);
		for (int i = 0; i < lightDistanceBins.Length; i++)
		{
			lightDistanceBins[i] = new List<GameLight>();
		}
		lightDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 50, UnsafeUtility.SizeOf<LightData>());
		lightData = new NativeArray<LightData>(50, Allocator.Persistent);
		nextLightUpdate = 0;
		ClearGameLights();
		SetDesaturateAndTintEnabled(enable: false, Color.black);
		SetAmbientLightDynamic(Color.black);
		SetCustomDynamicLightingEnabled(enable: false);
		SetMaxLights(20);
	}

	private void OnDestroy()
	{
		ClearGameLights();
		SetDesaturateAndTintEnabled(enable: false, Color.black);
		SetAmbientLightDynamic(Color.black);
		SetCustomDynamicLightingEnabled(enable: false);
		lightData.Dispose();
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

	public void SetAmbientLightDynamic(Color color)
	{
		Shader.SetGlobalColor("_GT_GameLight_Ambient_Color", color);
	}

	public void SetMaxLights(int maxLights)
	{
		maxLights = Mathf.Min(maxLights, 50);
		maxUseTestLights = maxLights;
		Shader.SetGlobalInteger("_GT_GameLight_UseMaxLights", maxLights);
	}

	public void SetDesaturateAndTintEnabled(bool enable, Color tint)
	{
		Shader.SetGlobalColor("_GT_DesaturateAndTint_TintColor", tint);
		Shader.SetGlobalFloat("_GT_DesaturateAndTint_TintAmount", enable ? 1f : 0f);
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
		if (gameLights.Count > maxUseTestLights)
		{
			if (mainCameraTransform == null)
			{
				mainCameraTransform = Camera.main.transform;
			}
			cameraPosForSort = mainCameraTransform.position;
			gameLights.Sort(CompareDistFromCamera);
		}
	}

	private int CompareDistFromCamera(GameLight a, GameLight b)
	{
		if (a == null || a.light == null)
		{
			if (b == null || b.light == null)
			{
				return 0;
			}
			return -1;
		}
		if (b == null || b.light == null)
		{
			return 1;
		}
		float num = Mathf.Clamp(a.cachedColorAndIntensity.x + a.cachedColorAndIntensity.y + a.cachedColorAndIntensity.z, 0.01f, 6f);
		float num2 = Mathf.Clamp(b.cachedColorAndIntensity.x + b.cachedColorAndIntensity.y + b.cachedColorAndIntensity.z, 0.01f, 6f);
		float num3 = (cameraPosForSort - a.cachedPosition).sqrMagnitude / num;
		float value = (cameraPosForSort - b.cachedPosition).sqrMagnitude / num2;
		return num3.CompareTo(value);
	}

	public override void Tick()
	{
		RefreshLightData();
	}

	private void RefreshLightData()
	{
		_ = lightData;
		if (customVertexLightingEnabled)
		{
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
			lightDataBuffer.SetData(lightData);
			Shader.SetGlobalBuffer("_GT_GameLight_Lights", lightDataBuffer);
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
		if (gameLights.Contains(light))
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
		int num = gameLights.IndexOf(light);
		if (num >= 0)
		{
			gameLights.RemoveAt(num);
		}
	}

	public void ClearGameLights()
	{
		if (gameLights != null)
		{
			gameLights.Clear();
		}
		_ = lightData;
		for (int i = 0; i < lightData.Length; i++)
		{
			ResetLight(i);
		}
		lightDataBuffer.SetData(lightData);
		Shader.SetGlobalBuffer("_GT_GameLight_Lights", lightDataBuffer);
	}

	public void GetFromLight(int lightIndex, int gameLightIndex)
	{
		_ = lightData;
		GameLight gameLight = null;
		if (gameLightIndex >= 0 && gameLightIndex < gameLights.Count)
		{
			gameLight = gameLights[gameLightIndex];
		}
		if (!(gameLight == null) && !(gameLight.light == null))
		{
			gameLight.cachedPosition = gameLight.transform.position;
			gameLight.cachedColorAndIntensity = (float)gameLight.intensityMult * gameLight.light.intensity * (gameLight.negativeLight ? (-1f) : 1f) * gameLight.light.color;
			Vector4 lightPos = gameLight.cachedPosition;
			lightPos.w = 1f;
			Vector4 cachedColorAndIntensity = gameLight.cachedColorAndIntensity;
			Vector3 zero = Vector3.zero;
			LightData value = new LightData
			{
				lightPos = lightPos,
				lightColor = cachedColorAndIntensity,
				lightDirection = zero
			};
			lightData[lightIndex] = value;
		}
	}

	private void ResetLight(int lightIndex)
	{
		LightData value = new LightData
		{
			lightPos = Vector4.zero,
			lightColor = Color.black,
			lightDirection = Vector4.zero
		};
		lightData[lightIndex] = value;
	}
}
