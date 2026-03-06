using GorillaExtensions;
using GT_CustomMapSupportRuntime;
using UnityEngine;

namespace GorillaTag.Rendering;

public class ZoneShaderSettings : MonoBehaviour, ITickSystemPost
{
	public enum EOverrideMode
	{
		LeaveUnchanged,
		ApplyNewValue,
		ApplyDefaultValue
	}

	public enum EZoneLiquidType
	{
		None,
		Water,
		Lava
	}

	public enum ELiquidShape
	{
		Plane,
		Cylinder
	}

	[OnEnterPlay_Set(false)]
	private static bool isInitialized;

	[Tooltip("Set this to true for cases like it is the first ZoneShaderSettings that should be activated when entering a scene.")]
	[SerializeField]
	private bool _activateOnAwake;

	[Tooltip("These values will be used as the default global values that will be fallen back to when not in a zone and that the other scripts will reference.")]
	public bool isDefaultValues;

	private static readonly int groundFogColor_shaderProp = Shader.PropertyToID("_ZoneGroundFogColor");

	[SerializeField]
	private EOverrideMode groundFogColor_overrideMode;

	[SerializeField]
	private Color groundFogColor = new Color(0.7f, 0.9f, 1f, 1f);

	private static readonly int groundFogDepthFadeSq_shaderProp = Shader.PropertyToID("_ZoneGroundFogDepthFadeSq");

	[SerializeField]
	private EOverrideMode groundFogDepthFade_overrideMode;

	[SerializeField]
	private float _groundFogDepthFadeSize = 20f;

	private static readonly int groundFogHeight_shaderProp = Shader.PropertyToID("_ZoneGroundFogHeight");

	[SerializeField]
	private EOverrideMode groundFogHeight_overrideMode;

	[SerializeField]
	private float groundFogHeight = 7.45f;

	private static readonly int groundFogHeightFade_shaderProp = Shader.PropertyToID("_ZoneGroundFogHeightFade");

	[SerializeField]
	private EOverrideMode groundFogHeightFade_overrideMode;

	[SerializeField]
	private float _groundFogHeightFadeSize = 20f;

	[SerializeField]
	private EOverrideMode zoneLiquidType_overrideMode;

	[SerializeField]
	private EZoneLiquidType zoneLiquidType = EZoneLiquidType.Water;

	[OnEnterPlay_Set(EZoneLiquidType.None)]
	private static EZoneLiquidType liquidType_previousValue = EZoneLiquidType.None;

	[OnEnterPlay_Set(false)]
	private static bool didEverSetLiquidShape;

	[SerializeField]
	private EOverrideMode liquidShape_overrideMode;

	[SerializeField]
	private ELiquidShape liquidShape;

	[OnEnterPlay_Set(ELiquidShape.Plane)]
	private static ELiquidShape liquidShape_previousValue = ELiquidShape.Plane;

	[SerializeField]
	private EOverrideMode liquidShapeRadius_overrideMode;

	[Tooltip("Fog params are: start, distance (end - start), unused, unused")]
	[SerializeField]
	private float liquidShapeRadius = 1f;

	[OnEnterPlay_Set(1f)]
	private static float liquidShapeRadius_previousValue;

	private bool hasLiquidBottomTransform;

	[SerializeField]
	private EOverrideMode liquidBottomTransform_overrideMode;

	[Tooltip("TODO: remove this when there is a way to precalculate the nearest triangle plane per vertex so it will work better for rivers.")]
	[SerializeField]
	private Transform liquidBottomTransform;

	private float liquidBottomPosY_previousValue;

	private static readonly int shaderParam_GlobalZoneLiquidUVScale = Shader.PropertyToID("_GlobalZoneLiquidUVScale");

	[SerializeField]
	private EOverrideMode zoneLiquidUVScale_overrideMode;

	[Tooltip("Fog params are: start, distance (end - start), unused, unused")]
	[SerializeField]
	private float zoneLiquidUVScale = 1f;

	private static readonly int shaderParam_GlobalWaterTintColor = Shader.PropertyToID("_GlobalWaterTintColor");

	[SerializeField]
	private EOverrideMode underwaterTintColor_overrideMode;

	[SerializeField]
	private Color underwaterTintColor = new Color(0.3f, 0.65f, 1f, 0.2f);

	private static readonly int shaderParam_GlobalUnderwaterFogColor = Shader.PropertyToID("_GlobalUnderwaterFogColor");

	[SerializeField]
	private EOverrideMode underwaterFogColor_overrideMode;

	[SerializeField]
	private Color underwaterFogColor = new Color(0.12f, 0.41f, 0.77f);

	private static readonly int shaderParam_GlobalUnderwaterFogParams = Shader.PropertyToID("_GlobalUnderwaterFogParams");

	[SerializeField]
	private EOverrideMode underwaterFogParams_overrideMode;

	[Tooltip("Fog params are: start, distance (end - start), unused, unused")]
	[SerializeField]
	private Vector4 underwaterFogParams = new Vector4(-5f, 40f, 0f, 0f);

	private static readonly int shaderParam_GlobalUnderwaterCausticsParams = Shader.PropertyToID("_GlobalUnderwaterCausticsParams");

	[SerializeField]
	private EOverrideMode underwaterCausticsParams_overrideMode;

	[Tooltip("Caustics params are: speed1, scale, alpha, unused")]
	[SerializeField]
	private Vector4 underwaterCausticsParams = new Vector4(0.075f, 0.075f, 1f, 0f);

	private static readonly int shaderParam_GlobalUnderwaterCausticsTex = Shader.PropertyToID("_GlobalUnderwaterCausticsTex");

	[SerializeField]
	private EOverrideMode underwaterCausticsTexture_overrideMode;

	[SerializeField]
	private Texture2D underwaterCausticsTexture;

	private static readonly int shaderParam_GlobalUnderwaterEffectsDistanceToSurfaceFade = Shader.PropertyToID("_GlobalUnderwaterEffectsDistanceToSurfaceFade");

	[SerializeField]
	private EOverrideMode underwaterEffectsDistanceToSurfaceFade_overrideMode;

	[SerializeField]
	private Vector2 underwaterEffectsDistanceToSurfaceFade = new Vector2(0.0001f, 50f);

	private const string kEdTooltip_liquidResidueTex = "This is used for things like the charred surface effect when lava burns static geo.";

	private static readonly int shaderParam_GlobalLiquidResidueTex = Shader.PropertyToID("_GlobalLiquidResidueTex");

	[SerializeField]
	[Tooltip("This is used for things like the charred surface effect when lava burns static geo.")]
	private EOverrideMode liquidResidueTex_overrideMode;

	[SerializeField]
	[Tooltip("This is used for things like the charred surface effect when lava burns static geo.")]
	private Texture2D liquidResidueTex;

	private readonly int shaderParam_GlobalMainWaterSurfacePlane = Shader.PropertyToID("_GlobalMainWaterSurfacePlane");

	private bool hasMainWaterSurfacePlane;

	private bool hasDynamicWaterSurfacePlane;

	[SerializeField]
	private EOverrideMode mainWaterSurfacePlane_overrideMode;

	[Tooltip("TODO: remove this when there is a way to precalculate the nearest triangle plane per vertex so it will work better for rivers.")]
	[SerializeField]
	private Transform mainWaterSurfacePlane;

	private static readonly int shaderParam_ZoneWeatherMapDissolveProgress = Shader.PropertyToID("_ZoneWeatherMapDissolveProgress");

	[SerializeField]
	private EOverrideMode zoneWeatherMapDissolveProgress_overrideMode;

	[Tooltip("Fog params are: start, distance (end - start), unused, unused")]
	[Range(0f, 1f)]
	[SerializeField]
	private float zoneWeatherMapDissolveProgress = 1f;

	[DebugReadout]
	[field: OnEnterPlay_SetNull]
	public static ZoneShaderSettings defaultsInstance { get; private set; }

	[field: OnEnterPlay_Set(false)]
	public static bool hasDefaultsInstance { get; private set; }

	[DebugReadout]
	[field: OnEnterPlay_SetNull]
	public static ZoneShaderSettings activeInstance { get; private set; }

	[field: OnEnterPlay_Set(false)]
	public static bool hasActiveInstance { get; private set; }

	public bool isActiveInstance => activeInstance == this;

	[DebugReadout]
	private float GroundFogDepthFadeSq => 1f / Mathf.Max(1E-05f, _groundFogDepthFadeSize * _groundFogDepthFadeSize);

	[DebugReadout]
	private float GroundFogHeightFade => 1f / Mathf.Max(1E-05f, _groundFogHeightFadeSize);

	public static int shaderParam_ZoneLiquidPosRadiusSq { get; private set; } = Shader.PropertyToID("_ZoneLiquidPosRadiusSq");

	bool ITickSystemPost.PostTickRunning { get; set; }

	public void SetZoneLiquidTypeKeywordEnum(EZoneLiquidType liquidType)
	{
		if (liquidType == EZoneLiquidType.None)
		{
			Shader.EnableKeyword("_GLOBAL_ZONE_LIQUID_TYPE__NONE");
		}
		else
		{
			Shader.DisableKeyword("_GLOBAL_ZONE_LIQUID_TYPE__NONE");
		}
		if (liquidType == EZoneLiquidType.Water)
		{
			Shader.EnableKeyword("_GLOBAL_ZONE_LIQUID_TYPE__WATER");
		}
		else
		{
			Shader.DisableKeyword("_GLOBAL_ZONE_LIQUID_TYPE__WATER");
		}
		if (liquidType == EZoneLiquidType.Lava)
		{
			Shader.EnableKeyword("_GLOBAL_ZONE_LIQUID_TYPE__LAVA");
		}
		else
		{
			Shader.DisableKeyword("_GLOBAL_ZONE_LIQUID_TYPE__LAVA");
		}
	}

	public void SetZoneLiquidShapeKeywordEnum(ELiquidShape shape)
	{
		if (shape == ELiquidShape.Plane)
		{
			Shader.EnableKeyword("_ZONE_LIQUID_SHAPE__PLANE");
		}
		else
		{
			Shader.DisableKeyword("_ZONE_LIQUID_SHAPE__PLANE");
		}
		if (shape == ELiquidShape.Cylinder)
		{
			Shader.EnableKeyword("_ZONE_LIQUID_SHAPE__CYLINDER");
		}
		else
		{
			Shader.DisableKeyword("_ZONE_LIQUID_SHAPE__CYLINDER");
		}
	}

	public static float GetWaterY()
	{
		return activeInstance.mainWaterSurfacePlane.position.y;
	}

	protected void Awake()
	{
		hasMainWaterSurfacePlane = mainWaterSurfacePlane != null && (mainWaterSurfacePlane_overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues);
		hasDynamicWaterSurfacePlane = hasMainWaterSurfacePlane && !mainWaterSurfacePlane.gameObject.isStatic;
		hasLiquidBottomTransform = liquidBottomTransform != null && (liquidBottomTransform_overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues);
		if (CheckDefaultsInstance() && _activateOnAwake)
		{
			BecomeActiveInstance();
		}
	}

	protected void OnEnable()
	{
		if (hasDynamicWaterSurfacePlane)
		{
			TickSystem<object>.AddPostTickCallback(this);
		}
	}

	protected void OnDisable()
	{
		TickSystem<object>.RemovePostTickCallback(this);
	}

	protected void OnDestroy()
	{
		if (defaultsInstance == this)
		{
			hasDefaultsInstance = false;
		}
		if (activeInstance == this)
		{
			hasActiveInstance = false;
		}
		TickSystem<object>.RemovePostTickCallback(this);
	}

	void ITickSystemPost.PostTick()
	{
		if (activeInstance == this && Application.isPlaying && !ApplicationQuittingState.IsQuitting)
		{
			UpdateMainPlaneShaderProperty();
		}
	}

	private void UpdateMainPlaneShaderProperty()
	{
		Transform transform = null;
		bool flag = false;
		if (hasMainWaterSurfacePlane && (mainWaterSurfacePlane_overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues))
		{
			flag = true;
			transform = mainWaterSurfacePlane;
		}
		else if (mainWaterSurfacePlane_overrideMode == EOverrideMode.ApplyDefaultValue && hasDefaultsInstance && defaultsInstance.hasMainWaterSurfacePlane)
		{
			flag = true;
			transform = defaultsInstance.mainWaterSurfacePlane;
		}
		if (flag)
		{
			Vector3 position = transform.position;
			Vector3 up = transform.up;
			float w = 0f - Vector3.Dot(up, position);
			Shader.SetGlobalVector(shaderParam_GlobalMainWaterSurfacePlane, new Vector4(up.x, up.y, up.z, w));
			ELiquidShape eLiquidShape = (liquidShape_previousValue = ((liquidShape_overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues) ? liquidShape : ((liquidShape_overrideMode != EOverrideMode.ApplyDefaultValue || !hasDefaultsInstance) ? liquidShape_previousValue : defaultsInstance.liquidShape)));
			float y = (((liquidBottomTransform_overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues) && hasLiquidBottomTransform) ? liquidBottomTransform.position.y : ((liquidBottomTransform_overrideMode != EOverrideMode.ApplyDefaultValue || !hasDefaultsInstance || !defaultsInstance.hasLiquidBottomTransform) ? liquidBottomPosY_previousValue : defaultsInstance.liquidBottomTransform.position.y));
			float num = ((liquidShapeRadius_overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues) ? liquidShapeRadius : ((liquidShape_overrideMode != EOverrideMode.ApplyDefaultValue || !hasDefaultsInstance) ? liquidShapeRadius_previousValue : defaultsInstance.liquidShapeRadius));
			if (eLiquidShape == ELiquidShape.Cylinder)
			{
				Shader.SetGlobalVector(shaderParam_ZoneLiquidPosRadiusSq, new Vector4(position.x, y, position.z, num * num));
				liquidShapeRadius_previousValue = num;
			}
		}
	}

	private bool CheckDefaultsInstance()
	{
		if (!isDefaultValues)
		{
			return true;
		}
		if (hasDefaultsInstance && defaultsInstance != null && defaultsInstance != this)
		{
			string path = defaultsInstance.transform.GetPath();
			Debug.LogError("ZoneShaderSettings: Destroying conflicting defaults instance.\n- keeping: \"" + path + "\"\n- destroying (this): \"" + base.transform.GetPath() + "\"", this);
			Object.Destroy(base.gameObject);
			return false;
		}
		defaultsInstance = this;
		hasDefaultsInstance = true;
		BecomeActiveInstance();
		return true;
	}

	public void BecomeActiveInstance(bool force = false)
	{
		if (!(activeInstance == this) || force)
		{
			if (activeInstance.IsNotNull())
			{
				TickSystem<object>.RemovePostTickCallback(activeInstance);
			}
			if (hasDynamicWaterSurfacePlane)
			{
				TickSystem<object>.AddPostTickCallback(this);
			}
			ApplyValues();
			activeInstance = this;
			hasActiveInstance = true;
		}
	}

	public static void ActivateDefaultSettings()
	{
		if (hasDefaultsInstance)
		{
			defaultsInstance.BecomeActiveInstance();
		}
	}

	public void SetGroundFogValue(Color fogColor, float fogDepthFade, float fogHeight, float fogHeightFade)
	{
		groundFogColor_overrideMode = EOverrideMode.ApplyNewValue;
		groundFogColor = fogColor;
		groundFogDepthFade_overrideMode = EOverrideMode.ApplyNewValue;
		_groundFogDepthFadeSize = fogDepthFade;
		groundFogHeight_overrideMode = EOverrideMode.ApplyNewValue;
		groundFogHeight = fogHeight;
		groundFogHeightFade_overrideMode = EOverrideMode.ApplyNewValue;
		_groundFogHeightFadeSize = fogHeightFade;
		BecomeActiveInstance(force: true);
	}

	private void ApplyValues()
	{
		if (!hasDefaultsInstance || ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		ApplyColor(groundFogColor_shaderProp, groundFogColor_overrideMode, groundFogColor, defaultsInstance.groundFogColor);
		ApplyFloat(groundFogDepthFadeSq_shaderProp, groundFogDepthFade_overrideMode, GroundFogDepthFadeSq, defaultsInstance.GroundFogDepthFadeSq);
		ApplyFloat(groundFogHeight_shaderProp, groundFogHeight_overrideMode, groundFogHeight, defaultsInstance.groundFogHeight);
		ApplyFloat(groundFogHeightFade_shaderProp, groundFogHeightFade_overrideMode, GroundFogHeightFade, defaultsInstance.GroundFogHeightFade);
		if (zoneLiquidType_overrideMode != EOverrideMode.LeaveUnchanged)
		{
			EZoneLiquidType eZoneLiquidType = ((zoneLiquidType_overrideMode == EOverrideMode.ApplyNewValue) ? zoneLiquidType : defaultsInstance.zoneLiquidType);
			if (eZoneLiquidType != liquidType_previousValue || !isInitialized)
			{
				SetZoneLiquidTypeKeywordEnum(eZoneLiquidType);
				liquidType_previousValue = eZoneLiquidType;
			}
		}
		if (liquidShape_overrideMode != EOverrideMode.LeaveUnchanged)
		{
			ELiquidShape eLiquidShape = ((liquidShape_overrideMode == EOverrideMode.ApplyNewValue) ? liquidShape : defaultsInstance.liquidShape);
			if (eLiquidShape != liquidShape_previousValue || !isInitialized)
			{
				SetZoneLiquidShapeKeywordEnum(eLiquidShape);
				liquidShape_previousValue = eLiquidShape;
			}
		}
		ApplyFloat(shaderParam_GlobalZoneLiquidUVScale, zoneLiquidUVScale_overrideMode, zoneLiquidUVScale, defaultsInstance.zoneLiquidUVScale);
		ApplyColor(shaderParam_GlobalWaterTintColor, underwaterTintColor_overrideMode, underwaterTintColor, defaultsInstance.underwaterTintColor);
		ApplyColor(shaderParam_GlobalUnderwaterFogColor, underwaterFogColor_overrideMode, underwaterFogColor, defaultsInstance.underwaterFogColor);
		ApplyVector(shaderParam_GlobalUnderwaterFogParams, underwaterFogParams_overrideMode, underwaterFogParams, defaultsInstance.underwaterFogParams);
		ApplyVector(shaderParam_GlobalUnderwaterCausticsParams, underwaterCausticsParams_overrideMode, underwaterCausticsParams, defaultsInstance.underwaterCausticsParams);
		ApplyTexture(shaderParam_GlobalUnderwaterCausticsTex, underwaterCausticsTexture_overrideMode, underwaterCausticsTexture, defaultsInstance.underwaterCausticsTexture);
		ApplyVector(shaderParam_GlobalUnderwaterEffectsDistanceToSurfaceFade, underwaterEffectsDistanceToSurfaceFade_overrideMode, underwaterEffectsDistanceToSurfaceFade, defaultsInstance.underwaterEffectsDistanceToSurfaceFade);
		ApplyTexture(shaderParam_GlobalLiquidResidueTex, liquidResidueTex_overrideMode, liquidResidueTex, defaultsInstance.liquidResidueTex);
		ApplyFloat(shaderParam_ZoneWeatherMapDissolveProgress, zoneWeatherMapDissolveProgress_overrideMode, zoneWeatherMapDissolveProgress, defaultsInstance.zoneWeatherMapDissolveProgress);
		UpdateMainPlaneShaderProperty();
		isInitialized = true;
	}

	private void ApplyColor(int shaderProp, EOverrideMode overrideMode, Color value, Color defaultValue)
	{
		if (overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues)
		{
			Shader.SetGlobalColor(shaderProp, value.linear);
		}
		else if (overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			Shader.SetGlobalColor(shaderProp, defaultValue.linear);
		}
	}

	private void ApplyFloat(int shaderProp, EOverrideMode overrideMode, float value, float defaultValue)
	{
		if (overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues)
		{
			Shader.SetGlobalFloat(shaderProp, value);
		}
		else if (overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			Shader.SetGlobalFloat(shaderProp, defaultValue);
		}
	}

	private void ApplyVector(int shaderProp, EOverrideMode overrideMode, Vector2 value, Vector2 defaultValue)
	{
		if (overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues)
		{
			Shader.SetGlobalVector(shaderProp, value);
		}
		else if (overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			Shader.SetGlobalVector(shaderProp, defaultValue);
		}
	}

	private void ApplyVector(int shaderProp, EOverrideMode overrideMode, Vector3 value, Vector3 defaultValue)
	{
		if (overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues)
		{
			Shader.SetGlobalVector(shaderProp, value);
		}
		else if (overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			Shader.SetGlobalVector(shaderProp, defaultValue);
		}
	}

	private void ApplyVector(int shaderProp, EOverrideMode overrideMode, Vector4 value, Vector4 defaultValue)
	{
		if (overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues)
		{
			Shader.SetGlobalVector(shaderProp, value);
		}
		else if (overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			Shader.SetGlobalVector(shaderProp, defaultValue);
		}
	}

	private void ApplyTexture(int shaderProp, EOverrideMode overrideMode, Texture2D value, Texture2D defaultValue)
	{
		if (overrideMode == EOverrideMode.ApplyNewValue || isDefaultValues)
		{
			Shader.SetGlobalTexture(shaderProp, value);
		}
		else if (overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			Shader.SetGlobalTexture(shaderProp, defaultValue);
		}
	}

	public void CopySettings(CMSZoneShaderSettings cmsZoneShaderSettings, bool rerunAwake = false)
	{
		_activateOnAwake = cmsZoneShaderSettings.activateOnLoad;
		if (cmsZoneShaderSettings.applyGroundFog)
		{
			groundFogColor_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetGroundFogColorOverrideMode();
			groundFogColor = cmsZoneShaderSettings.groundFogColor;
			groundFogHeight_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetGroundFogHeightOverrideMode();
			if (cmsZoneShaderSettings.groundFogHeightPlane.IsNotNull())
			{
				groundFogHeight = cmsZoneShaderSettings.groundFogHeightPlane.position.y;
			}
			else
			{
				groundFogHeight = cmsZoneShaderSettings.groundFogHeight;
			}
			groundFogHeightFade_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetGroundFogHeightFadeOverrideMode();
			_groundFogHeightFadeSize = cmsZoneShaderSettings.groundFogHeightFadeSize;
			groundFogDepthFade_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetGroundFogDepthFadeOverrideMode();
			_groundFogDepthFadeSize = cmsZoneShaderSettings.groundFogDepthFadeSize;
		}
		else
		{
			groundFogColor_overrideMode = EOverrideMode.ApplyNewValue;
			groundFogColor = new Color(0f, 0f, 0f, 0f);
			groundFogHeight = -9999f;
		}
		if (cmsZoneShaderSettings.applyLiquidEffects)
		{
			zoneLiquidType_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetZoneLiquidTypeOverrideMode();
			zoneLiquidType = (EZoneLiquidType)cmsZoneShaderSettings.GetZoneLiquidType();
			liquidShape_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetLiquidShapeOverrideMode();
			liquidShape = (ELiquidShape)cmsZoneShaderSettings.GetZoneLiquidShape();
			liquidShapeRadius_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetLiquidShapeRadiusOverrideMode();
			liquidShapeRadius = cmsZoneShaderSettings.liquidShapeRadius;
			liquidBottomTransform_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetLiquidBottomTransformOverrideMode();
			liquidBottomTransform = cmsZoneShaderSettings.liquidBottomTransform;
			zoneLiquidUVScale_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetZoneLiquidUVScaleOverrideMode();
			zoneLiquidUVScale = cmsZoneShaderSettings.zoneLiquidUVScale;
			underwaterTintColor_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetUnderwaterTintColorOverrideMode();
			underwaterTintColor = cmsZoneShaderSettings.underwaterTintColor;
			underwaterFogColor_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetUnderwaterFogColorOverrideMode();
			underwaterFogColor = cmsZoneShaderSettings.underwaterFogColor;
			underwaterFogParams_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetUnderwaterFogParamsOverrideMode();
			underwaterFogParams = cmsZoneShaderSettings.underwaterFogParams;
			underwaterCausticsParams_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetUnderwaterCausticsParamsOverrideMode();
			underwaterCausticsParams = cmsZoneShaderSettings.underwaterCausticsParams;
			underwaterCausticsTexture_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetUnderwaterCausticsTextureOverrideMode();
			underwaterCausticsTexture = cmsZoneShaderSettings.underwaterCausticsTexture;
			underwaterEffectsDistanceToSurfaceFade_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetUnderwaterEffectsDistanceToSurfaceFadeOverrideMode();
			underwaterEffectsDistanceToSurfaceFade = cmsZoneShaderSettings.underwaterEffectsDistanceToSurfaceFade;
			liquidResidueTex_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetLiquidResidueTextureOverrideMode();
			liquidResidueTex = cmsZoneShaderSettings.liquidResidueTex;
			mainWaterSurfacePlane_overrideMode = (EOverrideMode)cmsZoneShaderSettings.GetMainWaterSurfacePlaneOverrideMode();
			mainWaterSurfacePlane = cmsZoneShaderSettings.mainWaterSurfacePlane;
		}
		else
		{
			underwaterTintColor_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterTintColor = new Color(0f, 0f, 0f, 0f);
			underwaterFogColor_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterFogColor = new Color(0f, 0f, 0f, 0f);
			mainWaterSurfacePlane_overrideMode = EOverrideMode.ApplyNewValue;
			Transform transform = base.gameObject.transform.Find("DummyWaterPlane");
			GameObject gameObject = null;
			if (transform != null)
			{
				gameObject = transform.gameObject;
			}
			else
			{
				gameObject = new GameObject("DummyWaterPlane");
				gameObject.transform.SetParent(base.gameObject.transform);
				gameObject.transform.rotation = Quaternion.identity;
				gameObject.transform.position = new Vector3(0f, -9999f, 0f);
			}
			mainWaterSurfacePlane = gameObject.transform;
		}
		zoneWeatherMapDissolveProgress_overrideMode = EOverrideMode.LeaveUnchanged;
		if (rerunAwake)
		{
			Awake();
		}
	}

	public void CopySettings(ZoneShaderSettings zoneShaderSettings, bool rerunAwake = false)
	{
		_activateOnAwake = zoneShaderSettings._activateOnAwake;
		groundFogColor_overrideMode = zoneShaderSettings.groundFogColor_overrideMode;
		groundFogColor = zoneShaderSettings.groundFogColor;
		groundFogHeight_overrideMode = zoneShaderSettings.groundFogHeight_overrideMode;
		groundFogHeight = zoneShaderSettings.groundFogHeight;
		groundFogHeightFade_overrideMode = zoneShaderSettings.groundFogHeightFade_overrideMode;
		_groundFogHeightFadeSize = zoneShaderSettings._groundFogHeightFadeSize;
		groundFogDepthFade_overrideMode = zoneShaderSettings.groundFogDepthFade_overrideMode;
		_groundFogDepthFadeSize = zoneShaderSettings._groundFogDepthFadeSize;
		zoneLiquidType_overrideMode = zoneShaderSettings.zoneLiquidType_overrideMode;
		zoneLiquidType = zoneShaderSettings.zoneLiquidType;
		liquidShape_overrideMode = zoneShaderSettings.liquidShape_overrideMode;
		liquidShape = zoneShaderSettings.liquidShape;
		liquidShapeRadius_overrideMode = zoneShaderSettings.liquidShapeRadius_overrideMode;
		liquidShapeRadius = zoneShaderSettings.liquidShapeRadius;
		liquidBottomTransform_overrideMode = zoneShaderSettings.liquidBottomTransform_overrideMode;
		liquidBottomTransform = zoneShaderSettings.liquidBottomTransform;
		zoneLiquidUVScale_overrideMode = zoneShaderSettings.zoneLiquidUVScale_overrideMode;
		zoneLiquidUVScale = zoneShaderSettings.zoneLiquidUVScale;
		underwaterTintColor_overrideMode = zoneShaderSettings.underwaterTintColor_overrideMode;
		underwaterTintColor = zoneShaderSettings.underwaterTintColor;
		underwaterFogColor_overrideMode = zoneShaderSettings.underwaterFogColor_overrideMode;
		underwaterFogColor = zoneShaderSettings.underwaterFogColor;
		underwaterFogParams_overrideMode = zoneShaderSettings.underwaterFogParams_overrideMode;
		underwaterFogParams = zoneShaderSettings.underwaterFogParams;
		underwaterCausticsParams_overrideMode = zoneShaderSettings.underwaterCausticsParams_overrideMode;
		underwaterCausticsParams = zoneShaderSettings.underwaterCausticsParams;
		underwaterCausticsTexture_overrideMode = zoneShaderSettings.underwaterCausticsTexture_overrideMode;
		underwaterCausticsTexture = zoneShaderSettings.underwaterCausticsTexture;
		underwaterEffectsDistanceToSurfaceFade_overrideMode = zoneShaderSettings.underwaterEffectsDistanceToSurfaceFade_overrideMode;
		underwaterEffectsDistanceToSurfaceFade = zoneShaderSettings.underwaterEffectsDistanceToSurfaceFade;
		liquidResidueTex_overrideMode = zoneShaderSettings.liquidResidueTex_overrideMode;
		liquidResidueTex = zoneShaderSettings.liquidResidueTex;
		mainWaterSurfacePlane_overrideMode = zoneShaderSettings.mainWaterSurfacePlane_overrideMode;
		mainWaterSurfacePlane = zoneShaderSettings.mainWaterSurfacePlane;
		zoneWeatherMapDissolveProgress_overrideMode = zoneShaderSettings.zoneWeatherMapDissolveProgress_overrideMode;
		zoneWeatherMapDissolveProgress = zoneShaderSettings.zoneWeatherMapDissolveProgress;
		if (rerunAwake)
		{
			Awake();
		}
	}

	public void ReplaceDefaultValues(ZoneShaderSettings defaultZoneShaderSettings, bool rerunAwake = false)
	{
		if (groundFogColor_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			groundFogColor_overrideMode = EOverrideMode.ApplyNewValue;
			groundFogColor = defaultZoneShaderSettings.groundFogColor;
		}
		if (groundFogHeight_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			groundFogHeight_overrideMode = EOverrideMode.ApplyNewValue;
			groundFogHeight = defaultZoneShaderSettings.groundFogHeight;
		}
		if (groundFogHeightFade_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			groundFogHeightFade_overrideMode = EOverrideMode.ApplyNewValue;
			_groundFogHeightFadeSize = defaultZoneShaderSettings._groundFogHeightFadeSize;
		}
		if (groundFogDepthFade_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			groundFogDepthFade_overrideMode = EOverrideMode.ApplyNewValue;
			_groundFogDepthFadeSize = defaultZoneShaderSettings._groundFogDepthFadeSize;
		}
		if (zoneLiquidType_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			zoneLiquidType_overrideMode = EOverrideMode.ApplyNewValue;
			zoneLiquidType = defaultZoneShaderSettings.zoneLiquidType;
		}
		if (liquidShape_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			liquidShape_overrideMode = EOverrideMode.ApplyNewValue;
			liquidShape = defaultZoneShaderSettings.liquidShape;
		}
		if (liquidShapeRadius_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			liquidShapeRadius_overrideMode = EOverrideMode.ApplyNewValue;
			liquidShapeRadius = defaultZoneShaderSettings.liquidShapeRadius;
		}
		if (liquidBottomTransform_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			liquidBottomTransform_overrideMode = EOverrideMode.ApplyNewValue;
			liquidBottomTransform = defaultZoneShaderSettings.liquidBottomTransform;
		}
		if (zoneLiquidUVScale_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			zoneLiquidUVScale_overrideMode = EOverrideMode.ApplyNewValue;
			zoneLiquidUVScale = defaultZoneShaderSettings.zoneLiquidUVScale;
		}
		if (underwaterTintColor_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterTintColor_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterTintColor = defaultZoneShaderSettings.underwaterTintColor;
		}
		if (underwaterFogColor_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterFogColor_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterFogColor = defaultZoneShaderSettings.underwaterFogColor;
		}
		if (underwaterFogParams_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterFogParams_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterFogParams = defaultZoneShaderSettings.underwaterFogParams;
		}
		if (underwaterCausticsParams_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterCausticsParams_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterCausticsParams = defaultZoneShaderSettings.underwaterCausticsParams;
		}
		if (underwaterCausticsTexture_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterCausticsTexture_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterCausticsTexture = defaultZoneShaderSettings.underwaterCausticsTexture;
		}
		if (underwaterEffectsDistanceToSurfaceFade_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterEffectsDistanceToSurfaceFade_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterEffectsDistanceToSurfaceFade = defaultZoneShaderSettings.underwaterEffectsDistanceToSurfaceFade;
		}
		if (liquidResidueTex_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			liquidResidueTex_overrideMode = EOverrideMode.ApplyNewValue;
			liquidResidueTex = defaultZoneShaderSettings.liquidResidueTex;
		}
		if (mainWaterSurfacePlane_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			mainWaterSurfacePlane_overrideMode = EOverrideMode.ApplyNewValue;
			mainWaterSurfacePlane = defaultZoneShaderSettings.mainWaterSurfacePlane;
		}
		if (rerunAwake)
		{
			Awake();
		}
	}

	public void ReplaceDefaultValues(CMSZoneShaderSettings.CMSZoneShaderProperties defaultZoneShaderProperties, bool rerunAwake = false)
	{
		if (groundFogColor_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			groundFogColor_overrideMode = EOverrideMode.ApplyNewValue;
			groundFogColor = defaultZoneShaderProperties.groundFogColor;
		}
		if (groundFogHeight_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			groundFogHeight_overrideMode = EOverrideMode.ApplyNewValue;
			groundFogHeight = defaultZoneShaderProperties.groundFogHeight;
		}
		if (groundFogHeightFade_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			groundFogHeightFade_overrideMode = EOverrideMode.ApplyNewValue;
			_groundFogHeightFadeSize = defaultZoneShaderProperties.groundFogHeightFadeSize;
		}
		if (groundFogDepthFade_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			groundFogDepthFade_overrideMode = EOverrideMode.ApplyNewValue;
			_groundFogDepthFadeSize = defaultZoneShaderProperties.groundFogDepthFadeSize;
		}
		if (zoneLiquidType_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			zoneLiquidType_overrideMode = EOverrideMode.ApplyNewValue;
			zoneLiquidType = (EZoneLiquidType)defaultZoneShaderProperties.zoneLiquidType;
		}
		if (liquidShape_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			liquidShape_overrideMode = EOverrideMode.ApplyNewValue;
			liquidShape = (ELiquidShape)defaultZoneShaderProperties.liquidShape;
		}
		if (liquidShapeRadius_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			liquidShapeRadius_overrideMode = EOverrideMode.ApplyNewValue;
			liquidShapeRadius = defaultZoneShaderProperties.liquidShapeRadius;
		}
		if (liquidBottomTransform_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			liquidBottomTransform_overrideMode = EOverrideMode.ApplyNewValue;
			liquidBottomTransform = defaultZoneShaderProperties.liquidBottomTransform;
		}
		if (zoneLiquidUVScale_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			zoneLiquidUVScale_overrideMode = EOverrideMode.ApplyNewValue;
			zoneLiquidUVScale = defaultZoneShaderProperties.zoneLiquidUVScale;
		}
		if (underwaterTintColor_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterTintColor_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterTintColor = defaultZoneShaderProperties.underwaterTintColor;
		}
		if (underwaterFogColor_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterFogColor_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterFogColor = defaultZoneShaderProperties.underwaterFogColor;
		}
		if (underwaterFogParams_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterFogParams_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterFogParams = defaultZoneShaderProperties.underwaterFogParams;
		}
		if (underwaterCausticsParams_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterCausticsParams_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterCausticsParams = defaultZoneShaderProperties.underwaterCausticsParams;
		}
		if (underwaterCausticsTexture_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterCausticsTexture_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterCausticsTexture = defaultZoneShaderProperties.underwaterCausticsTexture;
		}
		if (underwaterEffectsDistanceToSurfaceFade_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			underwaterEffectsDistanceToSurfaceFade_overrideMode = EOverrideMode.ApplyNewValue;
			underwaterEffectsDistanceToSurfaceFade = defaultZoneShaderProperties.underwaterEffectsDistanceToSurfaceFade;
		}
		if (liquidResidueTex_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			liquidResidueTex_overrideMode = EOverrideMode.ApplyNewValue;
			liquidResidueTex = defaultZoneShaderProperties.liquidResidueTex;
		}
		if (mainWaterSurfacePlane_overrideMode == EOverrideMode.ApplyDefaultValue)
		{
			mainWaterSurfacePlane_overrideMode = EOverrideMode.ApplyNewValue;
			mainWaterSurfacePlane = defaultZoneShaderProperties.mainWaterSurfacePlane;
		}
		if (rerunAwake)
		{
			Awake();
		}
	}
}
