using Unity.Mathematics;
using UnityEngine;

public struct UberShaderMatUsedProps
{
	public Material material;

	public GTUberShader_MaterialKeywordStates kw;

	public MaterialFingerprint fingerprint;

	public bool IsValid;

	private readonly int _notAProp;

	public int _TransparencyMode;

	public int _Cutoff;

	public int _ColorSource;

	public int _BaseColor;

	public int _GChannelColor;

	public int _BChannelColor;

	public int _AChannelColor;

	public int _BaseMap;

	public int _BaseMap_ST;

	public int _SettingsPreset;

	public int _AdvancedOptions;

	public int _TexMipBias;

	public int _BaseMap_WH;

	public int _TexelSnapToggle;

	public int _TexelSnap_Factor;

	public int _UVSource;

	public int _AlphaDetailToggle;

	public int _AlphaDetail_ST;

	public int _AlphaDetail_Opacity;

	public int _AlphaDetail_WorldSpace;

	public int _MaskMapToggle;

	public int _MaskMap;

	public int _MaskMap_ST;

	public int _MaskMap_WH;

	public int _LavaLampToggle;

	public int _GradientMapToggle;

	public int _GradientMap;

	public int _DoTextureRotation;

	public int _RotateAngle;

	public int _RotateAnim;

	public int _UseWaveWarp;

	public int _WaveAmplitude;

	public int _WaveFrequency;

	public int _WaveScale;

	public int _WaveTimeScale;

	public int _ReflectToggle;

	public int _ReflectBoxProjectToggle;

	public int _ReflectBoxCubePos;

	public int _ReflectBoxSize;

	public int _ReflectBoxRotation;

	public int _ReflectMatcapToggle;

	public int _ReflectMatcapPerspToggle;

	public int _ReflectNormalToggle;

	public int _ReflectTex;

	public int _ReflectNormalTex;

	public int _ReflectAlbedoTint;

	public int _ReflectTint;

	public int _ReflectOpacity;

	public int _ReflectExposure;

	public int _ReflectOffset;

	public int _ReflectScale;

	public int _ReflectRotate;

	public int _HalfLambertToggle;

	public int _ParallaxPlanarToggle;

	public int _ParallaxToggle;

	public int _ParallaxAAToggle;

	public int _ParallaxAABias;

	public int _DepthMap;

	public int _ParallaxAmplitude;

	public int _ParallaxSamplesMinMax;

	public int _UvShiftToggle;

	public int _UvShiftSteps;

	public int _UvShiftRate;

	public int _UvShiftOffset;

	public int _UseGridEffect;

	public int _UseCrystalEffect;

	public int _CrystalPower;

	public int _CrystalRimColor;

	public int _LiquidVolume;

	public int _LiquidFill;

	public int _LiquidFillNormal;

	public int _LiquidSurfaceColor;

	public int _LiquidSwayX;

	public int _LiquidSwayY;

	public int _LiquidContainer;

	public int _LiquidPlanePosition;

	public int _LiquidPlaneNormal;

	public int _VertexFlapToggle;

	public int _VertexFlapAxis;

	public int _VertexFlapDegreesMinMax;

	public int _VertexFlapSpeed;

	public int _VertexFlapPhaseOffset;

	public int _VertexWaveToggle;

	public int _VertexWaveDebug;

	public int _VertexWaveEnd;

	public int _VertexWaveParams;

	public int _VertexWaveFalloff;

	public int _VertexWaveSphereMask;

	public int _VertexWavePhaseOffset;

	public int _VertexWaveAxes;

	public int _VertexRotateToggle;

	public int _VertexRotateAngles;

	public int _VertexRotateAnim;

	public int _VertexLightToggle;

	public int _InnerGlowOn;

	public int _InnerGlowColor;

	public int _InnerGlowParams;

	public int _InnerGlowTap;

	public int _InnerGlowSine;

	public int _InnerGlowSinePeriod;

	public int _InnerGlowSinePhaseShift;

	public int _StealthEffectOn;

	public int _UseEyeTracking;

	public int _EyeTileOffsetUV;

	public int _EyeOverrideUV;

	public int _EyeOverrideUVTransform;

	public int _UseMouthFlap;

	public int _MouthMap;

	public int _MouthMap_ST;

	public int _UseVertexColor;

	public int _WaterEffect;

	public int _HeightBasedWaterEffect;

	public int _WaterCaustics;

	public int _UseDayNightLightmap;

	public int _DAY_CYCLE_BRIGHTNESS_;

	public int _UseWeatherMap;

	public int _WeatherMap;

	public int _WeatherMapDissolveEdgeSize;

	public int _UseSpecular;

	public int _UseSpecularAlphaChannel;

	public int _Smoothness;

	public int _UseSpecHighlight;

	public int _SpecularDir;

	public int _SpecularPowerIntensity;

	public int _SpecularColor;

	public int _SpecularUseDiffuseColor;

	public int _EmissionToggle;

	public int _EmissionColor;

	public int _EmissionMap;

	public int _EmissionMaskByBaseMapAlpha;

	public int _EmissionUVScrollSpeed;

	public int _EmissionDissolveProgress;

	public int _EmissionDissolveAnimation;

	public int _EmissionDissolveEdgeSize;

	public int _EmissionIntensityInDynamic;

	public int _EmissionUseUVWaveWarp;

	public int _GreyZoneException;

	public int _Cull;

	public int _StencilReference;

	public int _StencilComparison;

	public int _StencilPassFront;

	public int _USE_DEFORM_MAP;

	public int _DeformMap;

	public int _DeformMapIntensity;

	public int _DeformMapMaskByVertColorRAmount;

	public int _DeformMapScrollSpeed;

	public int _DeformMapUV0Influence;

	public int _DeformMapObjectSpaceOffsetsU;

	public int _DeformMapObjectSpaceOffsetsV;

	public int _DeformMapWorldSpaceOffsetsU;

	public int _DeformMapWorldSpaceOffsetsV;

	public int _RotateOnYAxisBySinTime;

	public int _USE_TEX_ARRAY_ATLAS;

	public int _BaseMap_Atlas;

	public int _BaseMap_AtlasSlice;

	public int _BaseMap_AtlasSliceSource;

	public int _EmissionMap_Atlas;

	public int _EmissionMap_AtlasSlice;

	public int _DeformMap_Atlas;

	public int _DeformMap_AtlasSlice;

	public int _WeatherMap_Atlas;

	public int _WeatherMap_AtlasSlice;

	public int _DEBUG_PAWN_DATA;

	public int _SrcBlend;

	public int _DstBlend;

	public int _SrcBlendAlpha;

	public int _DstBlendAlpha;

	public int _ZWrite;

	public int _AlphaToMask;

	public int _Color;

	public int _Surface;

	public int _Metallic;

	public int _SpecColor;

	public int _DayNightLightmapArray;

	public int _DayNightLightmapArray_ST;

	public int _DayNightLightmapArray_AtlasSlice;

	public UberShaderMatUsedProps(Material mat)
	{
		material = mat;
		kw = new GTUberShader_MaterialKeywordStates(mat);
		_notAProp = 0;
		_TransparencyMode = 1;
		_Cutoff = 0;
		_ColorSource = 0;
		_BaseColor = 0;
		_GChannelColor = 0;
		_BChannelColor = 0;
		_AChannelColor = 0;
		_BaseMap = 0;
		_BaseMap_ST = 0;
		_SettingsPreset = 0;
		_AdvancedOptions = 0;
		_TexMipBias = 0;
		_BaseMap_WH = 0;
		_TexelSnapToggle = 0;
		_TexelSnap_Factor = 0;
		_UVSource = 0;
		_AlphaDetailToggle = 0;
		_AlphaDetail_ST = 0;
		_AlphaDetail_Opacity = 0;
		_AlphaDetail_WorldSpace = 0;
		_MaskMapToggle = 0;
		_MaskMap = 0;
		_MaskMap_ST = 0;
		_MaskMap_WH = 0;
		_LavaLampToggle = 0;
		_GradientMapToggle = 0;
		_GradientMap = 0;
		_DoTextureRotation = 0;
		_RotateAngle = 0;
		_RotateAnim = 0;
		_UseWaveWarp = 0;
		_WaveAmplitude = 0;
		_WaveFrequency = 0;
		_WaveScale = 0;
		_WaveTimeScale = 0;
		_ReflectToggle = 0;
		_ReflectBoxProjectToggle = 0;
		_ReflectBoxCubePos = 0;
		_ReflectBoxSize = 0;
		_ReflectBoxRotation = 0;
		_ReflectMatcapToggle = 0;
		_ReflectMatcapPerspToggle = 0;
		_ReflectNormalToggle = 0;
		_ReflectTex = 0;
		_ReflectNormalTex = 0;
		_ReflectAlbedoTint = 0;
		_ReflectTint = 0;
		_ReflectOpacity = 0;
		_ReflectExposure = 0;
		_ReflectOffset = 0;
		_ReflectScale = 0;
		_ReflectRotate = 0;
		_HalfLambertToggle = 0;
		_ParallaxPlanarToggle = 0;
		_ParallaxToggle = 0;
		_ParallaxAAToggle = 0;
		_ParallaxAABias = 0;
		_DepthMap = 0;
		_ParallaxAmplitude = 0;
		_ParallaxSamplesMinMax = 0;
		_UvShiftToggle = 0;
		_UvShiftSteps = 0;
		_UvShiftRate = 0;
		_UvShiftOffset = 0;
		_UseGridEffect = 0;
		_UseCrystalEffect = 0;
		_CrystalPower = 0;
		_CrystalRimColor = 0;
		_LiquidVolume = 0;
		_LiquidFill = 0;
		_LiquidFillNormal = 0;
		_LiquidSurfaceColor = 0;
		_LiquidSwayX = 0;
		_LiquidSwayY = 0;
		_LiquidContainer = 0;
		_LiquidPlanePosition = 0;
		_LiquidPlaneNormal = 0;
		_VertexFlapToggle = 0;
		_VertexFlapAxis = 0;
		_VertexFlapDegreesMinMax = 0;
		_VertexFlapSpeed = 0;
		_VertexFlapPhaseOffset = 0;
		_VertexWaveToggle = 0;
		_VertexWaveDebug = 0;
		_VertexWaveEnd = 0;
		_VertexWaveParams = 0;
		_VertexWaveFalloff = 0;
		_VertexWaveSphereMask = 0;
		_VertexWavePhaseOffset = 0;
		_VertexWaveAxes = 0;
		_VertexRotateToggle = 0;
		_VertexRotateAngles = 0;
		_VertexRotateAnim = 0;
		_VertexLightToggle = 0;
		_InnerGlowOn = 0;
		_InnerGlowColor = 0;
		_InnerGlowParams = 0;
		_InnerGlowTap = 0;
		_InnerGlowSine = 0;
		_InnerGlowSinePeriod = 0;
		_InnerGlowSinePhaseShift = 0;
		_StealthEffectOn = 0;
		_UseEyeTracking = 0;
		_EyeTileOffsetUV = 0;
		_EyeOverrideUV = 0;
		_EyeOverrideUVTransform = 0;
		_UseMouthFlap = 0;
		_MouthMap = 0;
		_MouthMap_ST = 0;
		_UseVertexColor = 0;
		_WaterEffect = 0;
		_HeightBasedWaterEffect = 0;
		_WaterCaustics = 0;
		_UseDayNightLightmap = 0;
		_DAY_CYCLE_BRIGHTNESS_ = 0;
		_UseWeatherMap = 0;
		_WeatherMap = 0;
		_WeatherMapDissolveEdgeSize = 0;
		_UseSpecular = 0;
		_UseSpecularAlphaChannel = 0;
		_Smoothness = 0;
		_UseSpecHighlight = 0;
		_SpecularDir = 0;
		_SpecularPowerIntensity = 0;
		_SpecularColor = 0;
		_SpecularUseDiffuseColor = 0;
		_EmissionToggle = 0;
		_EmissionColor = 0;
		_EmissionMap = 0;
		_EmissionMaskByBaseMapAlpha = 0;
		_EmissionUVScrollSpeed = 0;
		_EmissionDissolveProgress = 0;
		_EmissionDissolveAnimation = 0;
		_EmissionDissolveEdgeSize = 0;
		_EmissionIntensityInDynamic = 0;
		_EmissionUseUVWaveWarp = 0;
		_GreyZoneException = 0;
		_Cull = 1;
		_StencilReference = 1;
		_StencilComparison = 1;
		_StencilPassFront = 1;
		_USE_DEFORM_MAP = 0;
		_DeformMap = 0;
		_DeformMapIntensity = 0;
		_DeformMapMaskByVertColorRAmount = 0;
		_DeformMapScrollSpeed = 0;
		_DeformMapUV0Influence = 0;
		_DeformMapObjectSpaceOffsetsU = 0;
		_DeformMapObjectSpaceOffsetsV = 0;
		_DeformMapWorldSpaceOffsetsU = 0;
		_DeformMapWorldSpaceOffsetsV = 0;
		_RotateOnYAxisBySinTime = 0;
		_USE_TEX_ARRAY_ATLAS = 0;
		_BaseMap_Atlas = 0;
		_BaseMap_AtlasSlice = 0;
		_BaseMap_AtlasSliceSource = 0;
		_EmissionMap_Atlas = 0;
		_EmissionMap_AtlasSlice = 0;
		_DeformMap_Atlas = 0;
		_DeformMap_AtlasSlice = 0;
		_WeatherMap_Atlas = 0;
		_WeatherMap_AtlasSlice = 0;
		_DEBUG_PAWN_DATA = 0;
		_SrcBlend = 1;
		_DstBlend = 1;
		_SrcBlendAlpha = 1;
		_DstBlendAlpha = 1;
		_ZWrite = 1;
		_AlphaToMask = 1;
		_Color = 0;
		_Surface = 0;
		_Metallic = 0;
		_SpecColor = 0;
		_DayNightLightmapArray = 0;
		_DayNightLightmapArray_ST = 0;
		_DayNightLightmapArray_AtlasSlice = 0;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		if (!kw._USE_TEXTURE)
		{
			_ = kw.USE_TEXTURE__AS_MASK;
		}
		_g_Macro_DECLARE_ATLASABLE_TEX2D(in kw, ref _BaseMap, ref _BaseMap_Atlas);
		if (kw._MASK_MAP_ON)
		{
			_MaskMap++;
		}
		if (kw._GRADIENT_MAP_ON)
		{
			_GradientMap++;
		}
		if (kw._USE_WEATHER_MAP)
		{
			_g_Macro_DECLARE_ATLASABLE_TEX2D(in kw, ref _WeatherMap, ref _WeatherMap_Atlas);
		}
		if (kw._EMISSION || kw._CRYSTAL_EFFECT)
		{
			_g_Macro_DECLARE_ATLASABLE_TEX2D(in kw, ref _EmissionMap, ref _EmissionMap_Atlas);
		}
		if (kw._USE_DEFORM_MAP)
		{
			_g_Macro_DECLARE_ATLASABLE_TEX2D(in kw, ref _DeformMap, ref _DeformMap_Atlas);
		}
		flag5 = kw._ALPHA_DETAIL_MAP && (kw._USE_TEXTURE || kw.USE_TEXTURE__AS_MASK);
		flag3 = kw._WATER_EFFECT || kw._STEALTH_EFFECT || kw._ALPHA_BLUE_LIVE_ON;
		flag2 = kw._LIQUID_VOLUME || kw._INNER_GLOW || kw._VERTEX_ANIM_WAVE_DEBUG;
		flag4 = kw._WATER_EFFECT || kw._STEALTH_EFFECT;
		if (kw._REFLECTIONS)
		{
			_ReflectTex++;
			if (kw._REFLECTIONS_USE_NORMAL_TEX)
			{
				_ReflectNormalTex++;
			}
		}
		if (kw._PARALLAX)
		{
			_DepthMap++;
		}
		if (kw.LIGHTMAP_ON)
		{
			_ = kw._USE_DAY_NIGHT_LIGHTMAP;
		}
		if (kw.LIGHTMAP_ON)
		{
			_ = kw.DIRLIGHTMAP_COMBINED;
		}
		_ = kw._USE_WEATHER_MAP;
		if (kw._WATER_EFFECT)
		{
			if (!kw._WATER_CAUSTICS)
			{
				_ = kw._GLOBAL_ZONE_LIQUID_TYPE__LAVA;
			}
			if (kw._HEIGHT_BASED_WATER_EFFECT)
			{
				_ = kw._ZONE_LIQUID_SHAPE__CYLINDER;
			}
		}
		_ = kw._EYECOMP;
		if (kw._MOUTHCOMP)
		{
			_MouthMap++;
		}
		if (kw._USE_TEXTURE || kw.USE_TEXTURE__AS_MASK || kw._USE_WEATHER_MAP || kw._EMISSION || kw._USE_DEFORM_MAP || kw._REFLECTIONS)
		{
			_ = kw._GT_BASE_MAP_ATLAS_SLICE_SOURCE__UV1_Z;
		}
		if (!kw._USE_VERTEX_COLOR && !kw._USE_DEFORM_MAP && !kw._VERTEX_ANIM_FLAP)
		{
			_ = kw._VERTEX_ANIM_WAVE;
		}
		_ = kw.LIGHTMAP_ON;
		if (1 == 0 && !kw._PARALLAX)
		{
			_ = kw._PARALLAX_PLANAR;
		}
		_ = kw._MOUTHCOMP;
		if (kw._USE_TEXTURE || kw.USE_TEXTURE__AS_MASK || kw._EMISSION || kw._REFLECTIONS)
		{
			_ = kw._GT_BASE_MAP_ATLAS_SLICE_SOURCE__UV1_Z;
		}
		_ = kw._INNER_GLOW;
		if (!kw._USE_VERTEX_COLOR && !kw._VERTEX_ANIM_FLAP)
		{
			_ = kw._VERTEX_ANIM_WAVE;
		}
		_ = kw.LIGHTMAP_ON;
		if (1 == 0 && !kw._PARALLAX)
		{
			_ = kw._PARALLAX_PLANAR;
		}
		if (!kw._PARALLAX)
		{
			_ = kw._PARALLAX_PLANAR;
		}
		_ = kw._WATER_EFFECT;
		if (!kw._EMISSION)
		{
			_ = kw._CRYSTAL_EFFECT;
		}
		_ = kw._LIQUID_VOLUME;
		if (kw._REFLECTIONS)
		{
			_ = kw._REFLECTIONS_MATCAP;
		}
		_ = kw._MOUTHCOMP;
		_ = kw._ZONE_DYNAMIC_LIGHTS__CUSTOMVERTEX;
		if (kw._VERTEX_ROTATE)
		{
			_VertexRotateAngles++;
		}
		if (kw._USE_DEFORM_MAP)
		{
			_DeformMapUV0Influence++;
			_DeformMapObjectSpaceOffsetsU++;
			_DeformMapObjectSpaceOffsetsV++;
			_DeformMapScrollSpeed++;
			_g_Macro_SAMPLE_ATLASABLE_TEX2D_LOD(in kw, ref _DeformMap, ref _DeformMap_Atlas);
			_DeformMapIntensity++;
			_DeformMapMaskByVertColorRAmount++;
			_RotateOnYAxisBySinTime++;
		}
		if (kw._VERTEX_ANIM_FLAP)
		{
			_VertexFlapSpeed++;
			_VertexFlapPhaseOffset++;
			_VertexFlapDegreesMinMax++;
			_VertexFlapAxis++;
		}
		if (kw._VERTEX_ANIM_WAVE)
		{
			_VertexWavePhaseOffset++;
			_VertexWaveParams++;
			_VertexWaveParams++;
			_VertexWaveParams++;
			_VertexWaveParams++;
			_VertexWaveEnd += 2;
			_VertexWaveFalloff += 2;
			_VertexWaveSphereMask++;
			_VertexWaveAxes++;
			_VertexWaveAxes++;
			_VertexWaveAxes++;
			_VertexWaveAxes++;
		}
		if (kw._LIQUID_VOLUME)
		{
			_LiquidFill++;
			_LiquidFillNormal++;
			_LiquidSwayX++;
			_LiquidSwayY++;
			_LiquidFill++;
		}
		if (kw._USE_TEXTURE || kw.USE_TEXTURE__AS_MASK || kw._EMISSION)
		{
			_ = kw._UV_SOURCE__WORLD_PLANAR_Y;
			if (kw._MAINTEX_ROTATE)
			{
				_RotateAngle++;
				_RotateAnim++;
			}
			if (kw._UV_WAVE_WARP)
			{
				_WaveAmplitude++;
				_WaveFrequency++;
				_WaveScale++;
			}
			if (kw._UV_SHIFT)
			{
				_UvShiftRate++;
				_UvShiftSteps++;
				_UvShiftOffset++;
			}
			_g_Macro_TRANSFORM_TEX(in kw, ref _BaseMap, ref _BaseMap_ST);
			_ = kw._GT_BASE_MAP_ATLAS_SLICE_SOURCE__UV1_Z;
			if (kw._EYECOMP)
			{
				_BaseMap_ST++;
				_EyeOverrideUVTransform++;
				_EyeOverrideUV += 2;
			}
			if (kw._EMISSION)
			{
				_EmissionUVScrollSpeed += 2;
				_BaseMap_ST += 2;
				if (kw._EMISSION_USE_UV_WAVE_WARP)
				{
					_WaveAmplitude++;
					_WaveFrequency++;
					_WaveScale++;
				}
			}
		}
		if (!kw._USE_VERTEX_COLOR && !kw._VERTEX_ANIM_FLAP)
		{
			_ = kw._VERTEX_ANIM_WAVE;
		}
		_ = kw.LIGHTMAP_ON;
		if (kw._WATER_EFFECT)
		{
			_ = kw._WATER_CAUSTICS;
		}
		if (kw._REFLECTIONS && kw._REFLECTIONS_MATCAP)
		{
			_ = kw._REFLECTIONS_MATCAP_PERSP_AWARE;
		}
		if (kw._MOUTHCOMP)
		{
			_g_Macro_TRANSFORM_TEX(in kw, ref _MouthMap, ref _MouthMap_ST);
		}
		if (!kw._PARALLAX)
		{
			_ = kw._PARALLAX_PLANAR;
		}
		if (kw._INNER_GLOW)
		{
			_InnerGlowParams += 2;
			_InnerGlowSinePeriod++;
			_InnerGlowSinePhaseShift++;
			_InnerGlowSinePeriod++;
			_InnerGlowTap++;
		}
		if (kw._ZONE_DYNAMIC_LIGHTS__CUSTOMVERTEX)
		{
			_ = kw._ZONE_DYNAMIC_LIGHTS__CUSTOMVERTEX;
		}
		_BaseColor++;
		if (kw._USE_TEXTURE || kw.USE_TEXTURE__AS_MASK)
		{
			if (kw._TEXEL_SNAP_UVS)
			{
				_BaseMap_WH++;
				_TexelSnap_Factor++;
				_TexelSnap_Factor++;
			}
			if (!kw._PARALLAX)
			{
				_ = kw._PARALLAX_PLANAR;
			}
			if (kw._PARALLAX)
			{
				_ParallaxSamplesMinMax += 2;
				_DepthMap++;
				_ParallaxAmplitude++;
				if (kw._PARALLAX_AA)
				{
					_BaseMap_WH++;
					_ParallaxAABias++;
				}
			}
			else if (kw._PARALLAX_PLANAR)
			{
				_ParallaxAmplitude++;
			}
			if (kw._USE_TEX_ARRAY_ATLAS && kw._GT_BASE_MAP_ATLAS_SLICE_SOURCE__UV1_Z)
			{
				_BaseMap_AtlasSlice++;
			}
			_g_Macro_SAMPLE_ATLASABLE_TEX2D(in kw, ref _BaseMap, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _TexMipBias);
			if (kw.USE_TEXTURE__AS_MASK)
			{
				_BaseColor++;
				_GChannelColor++;
				_BChannelColor++;
				_AChannelColor++;
			}
			if (kw._ALPHA_DETAIL_MAP)
			{
				_AlphaDetail_ST += 2;
				_BaseMap_WH++;
				_g_Macro_SAMPLE_ATLASABLE_TEX2D(in kw, ref _BaseMap, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _TexMipBias);
				_AlphaDetail_Opacity++;
			}
		}
		if (kw._USE_WEATHER_MAP)
		{
			_g_Macro_SAMPLE_ATLASABLE_TEX2D(in kw, ref _WeatherMap, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _TexMipBias);
			_WeatherMapDissolveEdgeSize++;
		}
		if (kw._EYECOMP)
		{
			_g_Macro_SAMPLE_ATLASABLE_TEX2D(in kw, ref _BaseMap, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _TexMipBias);
			_EyeTileOffsetUV++;
			_EyeTileOffsetUV++;
			_EyeTileOffsetUV++;
			_EyeTileOffsetUV++;
			_g_Macro_SAMPLE_ATLASABLE_TEX2D(in kw, ref _BaseMap, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _TexMipBias);
		}
		if (kw._MOUTHCOMP)
		{
			_MouthMap++;
		}
		_ = kw._USE_VERTEX_COLOR;
		_ = kw._DAY_CYCLE_BRIGHTNESS__OPTION_1;
		_ = kw._DAY_CYCLE_BRIGHTNESS__OPTION_2;
		if (kw.LIGHTMAP_ON && kw._USE_DAY_NIGHT_LIGHTMAP && kw.DIRLIGHTMAP_COMBINED)
		{
			_ = kw._UNITY_EDIT_MODE;
		}
		if (kw._CRYSTAL_EFFECT)
		{
			_CrystalPower++;
			_CrystalRimColor += 2;
		}
		if (kw._USE_TEXTURE && kw._MASK_MAP_ON && kw._FX_LAVA_LAMP && kw._GRADIENT_MAP_ON)
		{
			_MaskMap_ST += 2;
			_MaskMap++;
			_GradientMap++;
		}
		if (kw._USE_TEXTURE && kw._GRID_EFFECT)
		{
			_BaseColor++;
			_BaseMap_WH++;
		}
		if (kw._REFLECTIONS)
		{
			if (!kw._REFLECTIONS_MATCAP)
			{
				if (kw._REFLECTIONS_BOX_PROJECT)
				{
					_ReflectBoxSize++;
					_ReflectBoxCubePos++;
					_ReflectBoxCubePos++;
					_ReflectBoxRotation++;
					_ReflectBoxCubePos++;
				}
				_ReflectRotate++;
				_ReflectOffset++;
				_ReflectScale++;
			}
			if (kw._REFLECTIONS_USE_NORMAL_TEX)
			{
				_ReflectNormalTex++;
			}
			_ReflectTex++;
			if (kw._REFLECTIONS_ALBEDO_TINT)
			{
				_ReflectTint++;
			}
			else
			{
				_ReflectTint++;
			}
			_ReflectOpacity++;
			_ReflectExposure++;
		}
		_ = kw._HALF_LAMBERT_TERM;
		if (kw._GT_RIM_LIGHT)
		{
			_Smoothness++;
			if (kw._USE_TEXTURE)
			{
				_ = kw._GT_RIM_LIGHT_USE_ALPHA;
			}
		}
		if (kw._SPECULAR_HIGHLIGHT)
		{
			_SpecularPowerIntensity++;
			_SpecularPowerIntensity++;
			_SpecularDir++;
			_SpecularColor++;
			_SpecularColor++;
			if (kw._USE_TEXTURE)
			{
				_SpecularUseDiffuseColor++;
				mat.GetInt("_SpecularUseDiffuseColor");
			}
		}
		if (kw._EMISSION || kw._CRYSTAL_EFFECT)
		{
			_EmissionColor += 2;
			if (kw._ALPHA_DETAIL_MAP)
			{
				_AlphaDetail_Opacity++;
			}
			if (kw._PARALLAX)
			{
				_DepthMap++;
				_ParallaxAmplitude++;
			}
			else if (kw._PARALLAX_PLANAR)
			{
				_ParallaxAmplitude++;
			}
			_g_Macro_SAMPLE_ATLASABLE_TEX2D(in kw, ref _EmissionMap, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _notAProp, ref _TexMipBias);
			_EmissionDissolveProgress++;
			_EmissionDissolveEdgeSize++;
			_EmissionDissolveAnimation += 2;
			_EmissionMaskByBaseMapAlpha++;
			_ = kw._ZONE_DYNAMIC_LIGHTS__CUSTOMVERTEX;
		}
		if (kw._INNER_GLOW)
		{
			_InnerGlowColor++;
		}
		if (kw._WATER_EFFECT)
		{
			_ = kw._GLOBAL_ZONE_LIQUID_TYPE__LAVA;
			_ = kw._HEIGHT_BASED_WATER_EFFECT;
			if (kw._WATER_CAUSTICS)
			{
				_ = kw._GLOBAL_ZONE_LIQUID_TYPE__LAVA;
			}
			_ = kw._USE_TEXTURE;
			if (kw._HEIGHT_BASED_WATER_EFFECT)
			{
				_ = kw._ZONE_LIQUID_SHAPE__CYLINDER;
			}
		}
		flag = !kw._LIQUID_CONTAINER;
		if (kw._LIQUID_VOLUME && flag)
		{
			_LiquidSwayX++;
			_LiquidSwayY++;
			if (kw._USE_TEXTURE)
			{
				_LiquidSurfaceColor++;
			}
			else
			{
				_LiquidSurfaceColor++;
			}
		}
		if (kw._VERTEX_ANIM_WAVE_DEBUG)
		{
			_VertexWaveEnd += 2;
			_VertexWaveFalloff += 2;
			_VertexWaveSphereMask++;
		}
		_ = kw._DEBUG_PAWN_DATA;
		if (!kw._COLOR_GRADE_PROTANOMALY && !kw._COLOR_GRADE_PROTANOPIA && !kw._COLOR_GRADE_DEUTERANOMALY && !kw._COLOR_GRADE_DEUTERANOPIA && !kw._COLOR_GRADE_TRITANOMALY && !kw._COLOR_GRADE_TRITANOPIA && !kw._COLOR_GRADE_ACHROMATOMALY)
		{
			_ = kw._COLOR_GRADE_ACHROMATOPSIA;
		}
		if (kw._ALPHATEST_ON)
		{
			_Cutoff++;
		}
		else if (kw._ALPHA_BLUE_LIVE_ON)
		{
			_Cutoff++;
		}
		if (kw._LIQUID_CONTAINER)
		{
			_LiquidPlanePosition++;
			_LiquidPlaneNormal++;
		}
		else
		{
			_ = kw._LIQUID_VOLUME;
		}
		if (!kw._ALPHATEST_ON && !kw._ALPHA_BLUE_LIVE_ON && !kw._LIQUID_CONTAINER)
		{
			_ = kw._LIQUID_VOLUME;
		}
		if (kw._ZONE_DYNAMIC_LIGHTS__CUSTOMVERTEX && (kw._EMISSION || kw._CRYSTAL_EFFECT))
		{
			_EmissionIntensityInDynamic++;
		}
		_ = kw._ZONE_DYNAMIC_LIGHTS__CUSTOMVERTEX;
		IsValid = true;
		fingerprint = default(MaterialFingerprint);
		fingerprint = new MaterialFingerprint(this);
	}

	public override string ToString()
	{
		string[] array = new string[179];
		array[0] = "---- MaterialFingerprint of ";
		array[1] = material?.name;
		array[2] = " ----\n";
		array[3] = ((_TransparencyMode > 0) ? ("_TransparencyMode = " + fingerprint._TransparencyMode.ToString() + "\n") : "");
		array[4] = ((_Cutoff > 0) ? ("_Cutoff = " + fingerprint._Cutoff + "\n") : "");
		array[5] = ((_ColorSource > 0) ? ("_ColorSource = " + fingerprint._ColorSource + "\n") : "");
		object obj;
		if (_BaseColor <= 0)
		{
			obj = "";
		}
		else
		{
			int4 baseColor = fingerprint._BaseColor;
			obj = "_BaseColor = " + baseColor.ToString() + "\n";
		}
		array[6] = (string)obj;
		object obj2;
		if (_GChannelColor <= 0)
		{
			obj2 = "";
		}
		else
		{
			int4 baseColor = fingerprint._GChannelColor;
			obj2 = "_GChannelColor = " + baseColor.ToString() + "\n";
		}
		array[7] = (string)obj2;
		object obj3;
		if (_BChannelColor <= 0)
		{
			obj3 = "";
		}
		else
		{
			int4 baseColor = fingerprint._BChannelColor;
			obj3 = "_BChannelColor = " + baseColor.ToString() + "\n";
		}
		array[8] = (string)obj3;
		object obj4;
		if (_AChannelColor <= 0)
		{
			obj4 = "";
		}
		else
		{
			int4 baseColor = fingerprint._AChannelColor;
			obj4 = "_AChannelColor = " + baseColor.ToString() + "\n";
		}
		array[9] = (string)obj4;
		array[10] = ((_BaseMap > 0) ? ("_BaseMap = " + fingerprint._BaseMap + "\n") : "");
		object obj5;
		if (_BaseMap_ST <= 0)
		{
			obj5 = "";
		}
		else
		{
			int4 baseColor = fingerprint._BaseMap_ST;
			obj5 = "_BaseMap_ST = " + baseColor.ToString() + "\n";
		}
		array[11] = (string)obj5;
		array[12] = ((_SettingsPreset > 0) ? ("_SettingsPreset = " + fingerprint._SettingsPreset + "\n") : "");
		array[13] = ((_AdvancedOptions > 0) ? ("_AdvancedOptions = " + fingerprint._AdvancedOptions + "\n") : "");
		array[14] = ((_TexMipBias > 0) ? ("_TexMipBias = " + fingerprint._TexMipBias + "\n") : "");
		object obj6;
		if (_BaseMap_WH <= 0)
		{
			obj6 = "";
		}
		else
		{
			int4 baseColor = fingerprint._BaseMap_WH;
			obj6 = "_BaseMap_WH = " + baseColor.ToString() + "\n";
		}
		array[15] = (string)obj6;
		array[16] = ((_TexelSnapToggle > 0) ? ("_TexelSnapToggle = " + fingerprint._TexelSnapToggle + "\n") : "");
		array[17] = ((_TexelSnap_Factor > 0) ? ("_TexelSnap_Factor = " + fingerprint._TexelSnap_Factor + "\n") : "");
		array[18] = ((_UVSource > 0) ? ("_UVSource = " + fingerprint._UVSource + "\n") : "");
		array[19] = ((_AlphaDetailToggle > 0) ? ("_AlphaDetailToggle = " + fingerprint._AlphaDetailToggle + "\n") : "");
		object obj7;
		if (_AlphaDetail_ST <= 0)
		{
			obj7 = "";
		}
		else
		{
			int4 baseColor = fingerprint._AlphaDetail_ST;
			obj7 = "_AlphaDetail_ST = " + baseColor.ToString() + "\n";
		}
		array[20] = (string)obj7;
		array[21] = ((_AlphaDetail_Opacity > 0) ? ("_AlphaDetail_Opacity = " + fingerprint._AlphaDetail_Opacity + "\n") : "");
		array[22] = ((_AlphaDetail_WorldSpace > 0) ? ("_AlphaDetail_WorldSpace = " + fingerprint._AlphaDetail_WorldSpace + "\n") : "");
		array[23] = ((_MaskMapToggle > 0) ? ("_MaskMapToggle = " + fingerprint._MaskMapToggle + "\n") : "");
		array[24] = ((_MaskMap > 0) ? ("_MaskMap = " + fingerprint._MaskMap + "\n") : "");
		object obj8;
		if (_MaskMap_ST <= 0)
		{
			obj8 = "";
		}
		else
		{
			int4 baseColor = fingerprint._MaskMap_ST;
			obj8 = "_MaskMap_ST = " + baseColor.ToString() + "\n";
		}
		array[25] = (string)obj8;
		object obj9;
		if (_MaskMap_WH <= 0)
		{
			obj9 = "";
		}
		else
		{
			int4 baseColor = fingerprint._MaskMap_WH;
			obj9 = "_MaskMap_WH = " + baseColor.ToString() + "\n";
		}
		array[26] = (string)obj9;
		array[27] = ((_LavaLampToggle > 0) ? ("_LavaLampToggle = " + fingerprint._LavaLampToggle + "\n") : "");
		array[28] = ((_GradientMapToggle > 0) ? ("_GradientMapToggle = " + fingerprint._GradientMapToggle + "\n") : "");
		array[29] = ((_GradientMap > 0) ? ("_GradientMap = " + fingerprint._GradientMap + "\n") : "");
		array[30] = ((_DoTextureRotation > 0) ? ("_DoTextureRotation = " + fingerprint._DoTextureRotation + "\n") : "");
		array[31] = ((_RotateAngle > 0) ? ("_RotateAngle = " + fingerprint._RotateAngle + "\n") : "");
		array[32] = ((_RotateAnim > 0) ? ("_RotateAnim = " + fingerprint._RotateAnim + "\n") : "");
		array[33] = ((_UseWaveWarp > 0) ? ("_UseWaveWarp = " + fingerprint._UseWaveWarp + "\n") : "");
		array[34] = ((_WaveAmplitude > 0) ? ("_WaveAmplitude = " + fingerprint._WaveAmplitude + "\n") : "");
		array[35] = ((_WaveFrequency > 0) ? ("_WaveFrequency = " + fingerprint._WaveFrequency + "\n") : "");
		array[36] = ((_WaveScale > 0) ? ("_WaveScale = " + fingerprint._WaveScale + "\n") : "");
		array[37] = ((_WaveTimeScale > 0) ? ("_WaveTimeScale = " + fingerprint._WaveTimeScale + "\n") : "");
		array[38] = ((_ReflectToggle > 0) ? ("_ReflectToggle = " + fingerprint._ReflectToggle + "\n") : "");
		array[39] = ((_ReflectBoxProjectToggle > 0) ? ("_ReflectBoxProjectToggle = " + fingerprint._ReflectBoxProjectToggle + "\n") : "");
		object obj10;
		if (_ReflectBoxCubePos <= 0)
		{
			obj10 = "";
		}
		else
		{
			int4 baseColor = fingerprint._ReflectBoxCubePos;
			obj10 = "_ReflectBoxCubePos = " + baseColor.ToString() + "\n";
		}
		array[40] = (string)obj10;
		object obj11;
		if (_ReflectBoxSize <= 0)
		{
			obj11 = "";
		}
		else
		{
			int4 baseColor = fingerprint._ReflectBoxSize;
			obj11 = "_ReflectBoxSize = " + baseColor.ToString() + "\n";
		}
		array[41] = (string)obj11;
		object obj12;
		if (_ReflectBoxRotation <= 0)
		{
			obj12 = "";
		}
		else
		{
			int4 baseColor = fingerprint._ReflectBoxRotation;
			obj12 = "_ReflectBoxRotation = " + baseColor.ToString() + "\n";
		}
		array[42] = (string)obj12;
		array[43] = ((_ReflectMatcapToggle > 0) ? ("_ReflectMatcapToggle = " + fingerprint._ReflectMatcapToggle + "\n") : "");
		array[44] = ((_ReflectMatcapPerspToggle > 0) ? ("_ReflectMatcapPerspToggle = " + fingerprint._ReflectMatcapPerspToggle + "\n") : "");
		array[45] = ((_ReflectNormalToggle > 0) ? ("_ReflectNormalToggle = " + fingerprint._ReflectNormalToggle + "\n") : "");
		array[46] = ((_ReflectTex > 0) ? ("_ReflectTex = " + fingerprint._ReflectTex + "\n") : "");
		array[47] = ((_ReflectNormalTex > 0) ? ("_ReflectNormalTex = " + fingerprint._ReflectNormalTex + "\n") : "");
		array[48] = ((_ReflectAlbedoTint > 0) ? ("_ReflectAlbedoTint = " + fingerprint._ReflectAlbedoTint + "\n") : "");
		object obj13;
		if (_ReflectTint <= 0)
		{
			obj13 = "";
		}
		else
		{
			int4 baseColor = fingerprint._ReflectTint;
			obj13 = "_ReflectTint = " + baseColor.ToString() + "\n";
		}
		array[49] = (string)obj13;
		array[50] = ((_ReflectOpacity > 0) ? ("_ReflectOpacity = " + fingerprint._ReflectOpacity + "\n") : "");
		array[51] = ((_ReflectExposure > 0) ? ("_ReflectExposure = " + fingerprint._ReflectExposure + "\n") : "");
		object obj14;
		if (_ReflectOffset <= 0)
		{
			obj14 = "";
		}
		else
		{
			int4 baseColor = fingerprint._ReflectOffset;
			obj14 = "_ReflectOffset = " + baseColor.ToString() + "\n";
		}
		array[52] = (string)obj14;
		object obj15;
		if (_ReflectScale <= 0)
		{
			obj15 = "";
		}
		else
		{
			int4 baseColor = fingerprint._ReflectScale;
			obj15 = "_ReflectScale = " + baseColor.ToString() + "\n";
		}
		array[53] = (string)obj15;
		array[54] = ((_ReflectRotate > 0) ? ("_ReflectRotate = " + fingerprint._ReflectRotate + "\n") : "");
		array[55] = ((_HalfLambertToggle > 0) ? ("_HalfLambertToggle = " + fingerprint._HalfLambertToggle + "\n") : "");
		array[56] = ((_ParallaxPlanarToggle > 0) ? ("_ParallaxPlanarToggle = " + fingerprint._ParallaxPlanarToggle + "\n") : "");
		array[57] = ((_ParallaxToggle > 0) ? ("_ParallaxToggle = " + fingerprint._ParallaxToggle + "\n") : "");
		array[58] = ((_ParallaxAAToggle > 0) ? ("_ParallaxAAToggle = " + fingerprint._ParallaxAAToggle + "\n") : "");
		array[59] = ((_ParallaxAABias > 0) ? ("_ParallaxAABias = " + fingerprint._ParallaxAABias + "\n") : "");
		array[60] = ((_DepthMap > 0) ? ("_DepthMap = " + fingerprint._DepthMap + "\n") : "");
		array[61] = ((_ParallaxAmplitude > 0) ? ("_ParallaxAmplitude = " + fingerprint._ParallaxAmplitude + "\n") : "");
		object obj16;
		if (_ParallaxSamplesMinMax <= 0)
		{
			obj16 = "";
		}
		else
		{
			int4 baseColor = fingerprint._ParallaxSamplesMinMax;
			obj16 = "_ParallaxSamplesMinMax = " + baseColor.ToString() + "\n";
		}
		array[62] = (string)obj16;
		array[63] = ((_UvShiftToggle > 0) ? ("_UvShiftToggle = " + fingerprint._UvShiftToggle + "\n") : "");
		object obj17;
		if (_UvShiftSteps <= 0)
		{
			obj17 = "";
		}
		else
		{
			int4 baseColor = fingerprint._UvShiftSteps;
			obj17 = "_UvShiftSteps = " + baseColor.ToString() + "\n";
		}
		array[64] = (string)obj17;
		object obj18;
		if (_UvShiftRate <= 0)
		{
			obj18 = "";
		}
		else
		{
			int4 baseColor = fingerprint._UvShiftRate;
			obj18 = "_UvShiftRate = " + baseColor.ToString() + "\n";
		}
		array[65] = (string)obj18;
		object obj19;
		if (_UvShiftOffset <= 0)
		{
			obj19 = "";
		}
		else
		{
			int4 baseColor = fingerprint._UvShiftOffset;
			obj19 = "_UvShiftOffset = " + baseColor.ToString() + "\n";
		}
		array[66] = (string)obj19;
		array[67] = ((_UseGridEffect > 0) ? ("_UseGridEffect = " + fingerprint._UseGridEffect + "\n") : "");
		array[68] = ((_UseCrystalEffect > 0) ? ("_UseCrystalEffect = " + fingerprint._UseCrystalEffect + "\n") : "");
		array[69] = ((_CrystalPower > 0) ? ("_CrystalPower = " + fingerprint._CrystalPower + "\n") : "");
		object obj20;
		if (_CrystalRimColor <= 0)
		{
			obj20 = "";
		}
		else
		{
			int4 baseColor = fingerprint._CrystalRimColor;
			obj20 = "_CrystalRimColor = " + baseColor.ToString() + "\n";
		}
		array[70] = (string)obj20;
		array[71] = ((_LiquidVolume > 0) ? ("_LiquidVolume = " + fingerprint._LiquidVolume + "\n") : "");
		array[72] = ((_LiquidFill > 0) ? ("_LiquidFill = " + fingerprint._LiquidFill + "\n") : "");
		object obj21;
		if (_LiquidFillNormal <= 0)
		{
			obj21 = "";
		}
		else
		{
			int4 baseColor = fingerprint._LiquidFillNormal;
			obj21 = "_LiquidFillNormal = " + baseColor.ToString() + "\n";
		}
		array[73] = (string)obj21;
		object obj22;
		if (_LiquidSurfaceColor <= 0)
		{
			obj22 = "";
		}
		else
		{
			int4 baseColor = fingerprint._LiquidSurfaceColor;
			obj22 = "_LiquidSurfaceColor = " + baseColor.ToString() + "\n";
		}
		array[74] = (string)obj22;
		array[75] = ((_LiquidSwayX > 0) ? ("_LiquidSwayX = " + fingerprint._LiquidSwayX + "\n") : "");
		array[76] = ((_LiquidSwayY > 0) ? ("_LiquidSwayY = " + fingerprint._LiquidSwayY + "\n") : "");
		array[77] = ((_LiquidContainer > 0) ? ("_LiquidContainer = " + fingerprint._LiquidContainer + "\n") : "");
		object obj23;
		if (_LiquidPlanePosition <= 0)
		{
			obj23 = "";
		}
		else
		{
			int4 baseColor = fingerprint._LiquidPlanePosition;
			obj23 = "_LiquidPlanePosition = " + baseColor.ToString() + "\n";
		}
		array[78] = (string)obj23;
		object obj24;
		if (_LiquidPlaneNormal <= 0)
		{
			obj24 = "";
		}
		else
		{
			int4 baseColor = fingerprint._LiquidPlaneNormal;
			obj24 = "_LiquidPlaneNormal = " + baseColor.ToString() + "\n";
		}
		array[79] = (string)obj24;
		array[80] = ((_VertexFlapToggle > 0) ? ("_VertexFlapToggle = " + fingerprint._VertexFlapToggle + "\n") : "");
		object obj25;
		if (_VertexFlapAxis <= 0)
		{
			obj25 = "";
		}
		else
		{
			int4 baseColor = fingerprint._VertexFlapAxis;
			obj25 = "_VertexFlapAxis = " + baseColor.ToString() + "\n";
		}
		array[81] = (string)obj25;
		object obj26;
		if (_VertexFlapDegreesMinMax <= 0)
		{
			obj26 = "";
		}
		else
		{
			int4 baseColor = fingerprint._VertexFlapDegreesMinMax;
			obj26 = "_VertexFlapDegreesMinMax = " + baseColor.ToString() + "\n";
		}
		array[82] = (string)obj26;
		array[83] = ((_VertexFlapSpeed > 0) ? ("_VertexFlapSpeed = " + fingerprint._VertexFlapSpeed + "\n") : "");
		array[84] = ((_VertexFlapPhaseOffset > 0) ? ("_VertexFlapPhaseOffset = " + fingerprint._VertexFlapPhaseOffset + "\n") : "");
		array[85] = ((_VertexWaveToggle > 0) ? ("_VertexWaveToggle = " + fingerprint._VertexWaveToggle + "\n") : "");
		array[86] = ((_VertexWaveDebug > 0) ? ("_VertexWaveDebug = " + fingerprint._VertexWaveDebug + "\n") : "");
		object obj27;
		if (_VertexWaveEnd <= 0)
		{
			obj27 = "";
		}
		else
		{
			int4 baseColor = fingerprint._VertexWaveEnd;
			obj27 = "_VertexWaveEnd = " + baseColor.ToString() + "\n";
		}
		array[87] = (string)obj27;
		object obj28;
		if (_VertexWaveParams <= 0)
		{
			obj28 = "";
		}
		else
		{
			int4 baseColor = fingerprint._VertexWaveParams;
			obj28 = "_VertexWaveParams = " + baseColor.ToString() + "\n";
		}
		array[88] = (string)obj28;
		object obj29;
		if (_VertexWaveFalloff <= 0)
		{
			obj29 = "";
		}
		else
		{
			int4 baseColor = fingerprint._VertexWaveFalloff;
			obj29 = "_VertexWaveFalloff = " + baseColor.ToString() + "\n";
		}
		array[89] = (string)obj29;
		object obj30;
		if (_VertexWaveSphereMask <= 0)
		{
			obj30 = "";
		}
		else
		{
			int4 baseColor = fingerprint._VertexWaveSphereMask;
			obj30 = "_VertexWaveSphereMask = " + baseColor.ToString() + "\n";
		}
		array[90] = (string)obj30;
		array[91] = ((_VertexWavePhaseOffset > 0) ? ("_VertexWavePhaseOffset = " + fingerprint._VertexWavePhaseOffset + "\n") : "");
		object obj31;
		if (_VertexWaveAxes <= 0)
		{
			obj31 = "";
		}
		else
		{
			int4 baseColor = fingerprint._VertexWaveAxes;
			obj31 = "_VertexWaveAxes = " + baseColor.ToString() + "\n";
		}
		array[92] = (string)obj31;
		array[93] = ((_VertexRotateToggle > 0) ? ("_VertexRotateToggle = " + fingerprint._VertexRotateToggle + "\n") : "");
		object obj32;
		if (_VertexRotateAngles <= 0)
		{
			obj32 = "";
		}
		else
		{
			int4 baseColor = fingerprint._VertexRotateAngles;
			obj32 = "_VertexRotateAngles = " + baseColor.ToString() + "\n";
		}
		array[94] = (string)obj32;
		array[95] = ((_VertexRotateAnim > 0) ? ("_VertexRotateAnim = " + fingerprint._VertexRotateAnim + "\n") : "");
		array[96] = ((_VertexLightToggle > 0) ? ("_VertexLightToggle = " + fingerprint._VertexLightToggle + "\n") : "");
		array[97] = ((_InnerGlowOn > 0) ? ("_InnerGlowOn = " + fingerprint._InnerGlowOn + "\n") : "");
		object obj33;
		if (_InnerGlowColor <= 0)
		{
			obj33 = "";
		}
		else
		{
			int4 baseColor = fingerprint._InnerGlowColor;
			obj33 = "_InnerGlowColor = " + baseColor.ToString() + "\n";
		}
		array[98] = (string)obj33;
		object obj34;
		if (_InnerGlowParams <= 0)
		{
			obj34 = "";
		}
		else
		{
			int4 baseColor = fingerprint._InnerGlowParams;
			obj34 = "_InnerGlowParams = " + baseColor.ToString() + "\n";
		}
		array[99] = (string)obj34;
		array[100] = ((_InnerGlowTap > 0) ? ("_InnerGlowTap = " + fingerprint._InnerGlowTap + "\n") : "");
		array[101] = ((_InnerGlowSine > 0) ? ("_InnerGlowSine = " + fingerprint._InnerGlowSine + "\n") : "");
		array[102] = ((_InnerGlowSinePeriod > 0) ? ("_InnerGlowSinePeriod = " + fingerprint._InnerGlowSinePeriod + "\n") : "");
		array[103] = ((_InnerGlowSinePhaseShift > 0) ? ("_InnerGlowSinePhaseShift = " + fingerprint._InnerGlowSinePhaseShift + "\n") : "");
		array[104] = ((_StealthEffectOn > 0) ? ("_StealthEffectOn = " + fingerprint._StealthEffectOn + "\n") : "");
		array[105] = ((_UseEyeTracking > 0) ? ("_UseEyeTracking = " + fingerprint._UseEyeTracking + "\n") : "");
		object obj35;
		if (_EyeTileOffsetUV <= 0)
		{
			obj35 = "";
		}
		else
		{
			int4 baseColor = fingerprint._EyeTileOffsetUV;
			obj35 = "_EyeTileOffsetUV = " + baseColor.ToString() + "\n";
		}
		array[106] = (string)obj35;
		array[107] = ((_EyeOverrideUV > 0) ? ("_EyeOverrideUV = " + fingerprint._EyeOverrideUV + "\n") : "");
		object obj36;
		if (_EyeOverrideUVTransform <= 0)
		{
			obj36 = "";
		}
		else
		{
			int4 baseColor = fingerprint._EyeOverrideUVTransform;
			obj36 = "_EyeOverrideUVTransform = " + baseColor.ToString() + "\n";
		}
		array[108] = (string)obj36;
		array[109] = ((_UseMouthFlap > 0) ? ("_UseMouthFlap = " + fingerprint._UseMouthFlap + "\n") : "");
		array[110] = ((_MouthMap > 0) ? ("_MouthMap = " + fingerprint._MouthMap + "\n") : "");
		object obj37;
		if (_MouthMap_ST <= 0)
		{
			obj37 = "";
		}
		else
		{
			int4 baseColor = fingerprint._MouthMap_ST;
			obj37 = "_MouthMap_ST = " + baseColor.ToString() + "\n";
		}
		array[111] = (string)obj37;
		array[112] = ((_UseVertexColor > 0) ? ("_UseVertexColor = " + fingerprint._UseVertexColor + "\n") : "");
		array[113] = ((_WaterEffect > 0) ? ("_WaterEffect = " + fingerprint._WaterEffect + "\n") : "");
		array[114] = ((_HeightBasedWaterEffect > 0) ? ("_HeightBasedWaterEffect = " + fingerprint._HeightBasedWaterEffect + "\n") : "");
		array[115] = ((_WaterCaustics > 0) ? ("_WaterCaustics = " + fingerprint._WaterCaustics + "\n") : "");
		array[116] = ((_UseDayNightLightmap > 0) ? ("_UseDayNightLightmap = " + fingerprint._UseDayNightLightmap + "\n") : "");
		array[117] = ((_DAY_CYCLE_BRIGHTNESS_ > 0) ? ("_DAY_CYCLE_BRIGHTNESS_ = " + fingerprint._DAY_CYCLE_BRIGHTNESS_ + "\n") : "");
		array[118] = ((_UseWeatherMap > 0) ? ("_UseWeatherMap = " + fingerprint._UseWeatherMap + "\n") : "");
		array[119] = ((_WeatherMap > 0) ? ("_WeatherMap = " + fingerprint._WeatherMap + "\n") : "");
		array[120] = ((_WeatherMapDissolveEdgeSize > 0) ? ("_WeatherMapDissolveEdgeSize = " + fingerprint._WeatherMapDissolveEdgeSize + "\n") : "");
		array[121] = ((_UseSpecular > 0) ? ("_UseSpecular = " + fingerprint._UseSpecular + "\n") : "");
		array[122] = ((_UseSpecularAlphaChannel > 0) ? ("_UseSpecularAlphaChannel = " + fingerprint._UseSpecularAlphaChannel + "\n") : "");
		array[123] = ((_Smoothness > 0) ? ("_Smoothness = " + fingerprint._Smoothness + "\n") : "");
		array[124] = ((_UseSpecHighlight > 0) ? ("_UseSpecHighlight = " + fingerprint._UseSpecHighlight + "\n") : "");
		object obj38;
		if (_SpecularDir <= 0)
		{
			obj38 = "";
		}
		else
		{
			int4 baseColor = fingerprint._SpecularDir;
			obj38 = "_SpecularDir = " + baseColor.ToString() + "\n";
		}
		array[125] = (string)obj38;
		object obj39;
		if (_SpecularPowerIntensity <= 0)
		{
			obj39 = "";
		}
		else
		{
			int4 baseColor = fingerprint._SpecularPowerIntensity;
			obj39 = "_SpecularPowerIntensity = " + baseColor.ToString() + "\n";
		}
		array[126] = (string)obj39;
		object obj40;
		if (_SpecularColor <= 0)
		{
			obj40 = "";
		}
		else
		{
			int4 baseColor = fingerprint._SpecularColor;
			obj40 = "_SpecularColor = " + baseColor.ToString() + "\n";
		}
		array[127] = (string)obj40;
		array[128] = ((_SpecularUseDiffuseColor > 0) ? ("_SpecularUseDiffuseColor = " + fingerprint._SpecularUseDiffuseColor + "\n") : "");
		array[129] = ((_EmissionToggle > 0) ? ("_EmissionToggle = " + fingerprint._EmissionToggle + "\n") : "");
		object obj41;
		if (_EmissionColor <= 0)
		{
			obj41 = "";
		}
		else
		{
			int4 baseColor = fingerprint._EmissionColor;
			obj41 = "_EmissionColor = " + baseColor.ToString() + "\n";
		}
		array[130] = (string)obj41;
		array[131] = ((_EmissionMap > 0) ? ("_EmissionMap = " + fingerprint._EmissionMap + "\n") : "");
		array[132] = ((_EmissionMaskByBaseMapAlpha > 0) ? ("_EmissionMaskByBaseMapAlpha = " + fingerprint._EmissionMaskByBaseMapAlpha + "\n") : "");
		object obj42;
		if (_EmissionUVScrollSpeed <= 0)
		{
			obj42 = "";
		}
		else
		{
			int4 baseColor = fingerprint._EmissionUVScrollSpeed;
			obj42 = "_EmissionUVScrollSpeed = " + baseColor.ToString() + "\n";
		}
		array[133] = (string)obj42;
		array[134] = ((_EmissionDissolveProgress > 0) ? ("_EmissionDissolveProgress = " + fingerprint._EmissionDissolveProgress + "\n") : "");
		object obj43;
		if (_EmissionDissolveAnimation <= 0)
		{
			obj43 = "";
		}
		else
		{
			int4 baseColor = fingerprint._EmissionDissolveAnimation;
			obj43 = "_EmissionDissolveAnimation = " + baseColor.ToString() + "\n";
		}
		array[135] = (string)obj43;
		array[136] = ((_EmissionDissolveEdgeSize > 0) ? ("_EmissionDissolveEdgeSize = " + fingerprint._EmissionDissolveEdgeSize + "\n") : "");
		array[137] = ((_EmissionIntensityInDynamic > 0) ? ("_EmissionIntensityInDynamic = " + fingerprint._EmissionIntensityInDynamic + "\n") : "");
		array[138] = ((_EmissionUseUVWaveWarp > 0) ? ("_EmissionUseUVWaveWarp = " + fingerprint._EmissionUseUVWaveWarp + "\n") : "");
		array[139] = ((_GreyZoneException > 0) ? ("_GreyZoneException = " + fingerprint._GreyZoneException + "\n") : "");
		array[140] = ((_Cull > 0) ? ("_Cull = " + fingerprint._Cull + "\n") : "");
		array[141] = ((_StencilReference > 0) ? ("_StencilReference = " + fingerprint._StencilReference + "\n") : "");
		array[142] = ((_StencilComparison > 0) ? ("_StencilComparison = " + fingerprint._StencilComparison + "\n") : "");
		array[143] = ((_StencilPassFront > 0) ? ("_StencilPassFront = " + fingerprint._StencilPassFront + "\n") : "");
		array[144] = ((_USE_DEFORM_MAP > 0) ? ("_USE_DEFORM_MAP = " + fingerprint._USE_DEFORM_MAP + "\n") : "");
		array[145] = ((_DeformMap > 0) ? ("_DeformMap = " + fingerprint._DeformMap + "\n") : "");
		array[146] = ((_DeformMapIntensity > 0) ? ("_DeformMapIntensity = " + fingerprint._DeformMapIntensity + "\n") : "");
		array[147] = ((_DeformMapMaskByVertColorRAmount > 0) ? ("_DeformMapMaskByVertColorRAmount = " + fingerprint._DeformMapMaskByVertColorRAmount + "\n") : "");
		object obj44;
		if (_DeformMapScrollSpeed <= 0)
		{
			obj44 = "";
		}
		else
		{
			int4 baseColor = fingerprint._DeformMapScrollSpeed;
			obj44 = "_DeformMapScrollSpeed = " + baseColor.ToString() + "\n";
		}
		array[148] = (string)obj44;
		object obj45;
		if (_DeformMapUV0Influence <= 0)
		{
			obj45 = "";
		}
		else
		{
			int4 baseColor = fingerprint._DeformMapUV0Influence;
			obj45 = "_DeformMapUV0Influence = " + baseColor.ToString() + "\n";
		}
		array[149] = (string)obj45;
		object obj46;
		if (_DeformMapObjectSpaceOffsetsU <= 0)
		{
			obj46 = "";
		}
		else
		{
			int4 baseColor = fingerprint._DeformMapObjectSpaceOffsetsU;
			obj46 = "_DeformMapObjectSpaceOffsetsU = " + baseColor.ToString() + "\n";
		}
		array[150] = (string)obj46;
		object obj47;
		if (_DeformMapObjectSpaceOffsetsV <= 0)
		{
			obj47 = "";
		}
		else
		{
			int4 baseColor = fingerprint._DeformMapObjectSpaceOffsetsV;
			obj47 = "_DeformMapObjectSpaceOffsetsV = " + baseColor.ToString() + "\n";
		}
		array[151] = (string)obj47;
		object obj48;
		if (_DeformMapWorldSpaceOffsetsU <= 0)
		{
			obj48 = "";
		}
		else
		{
			int4 baseColor = fingerprint._DeformMapWorldSpaceOffsetsU;
			obj48 = "_DeformMapWorldSpaceOffsetsU = " + baseColor.ToString() + "\n";
		}
		array[152] = (string)obj48;
		object obj49;
		if (_DeformMapWorldSpaceOffsetsV <= 0)
		{
			obj49 = "";
		}
		else
		{
			int4 baseColor = fingerprint._DeformMapWorldSpaceOffsetsV;
			obj49 = "_DeformMapWorldSpaceOffsetsV = " + baseColor.ToString() + "\n";
		}
		array[153] = (string)obj49;
		object obj50;
		if (_RotateOnYAxisBySinTime <= 0)
		{
			obj50 = "";
		}
		else
		{
			int4 baseColor = fingerprint._RotateOnYAxisBySinTime;
			obj50 = "_RotateOnYAxisBySinTime = " + baseColor.ToString() + "\n";
		}
		array[154] = (string)obj50;
		array[155] = ((_USE_TEX_ARRAY_ATLAS > 0) ? ("_USE_TEX_ARRAY_ATLAS = " + fingerprint._USE_TEX_ARRAY_ATLAS + "\n") : "");
		array[156] = ((_BaseMap_Atlas > 0) ? ("_BaseMap_Atlas = " + fingerprint._BaseMap_Atlas + "\n") : "");
		array[157] = ((_BaseMap_AtlasSlice > 0) ? ("_BaseMap_AtlasSlice = " + fingerprint._BaseMap_AtlasSlice + "\n") : "");
		array[158] = ((_BaseMap_AtlasSliceSource > 0) ? ("_BaseMap_AtlasSliceSource = " + fingerprint._BaseMap_AtlasSliceSource + "\n") : "");
		array[159] = ((_EmissionMap_Atlas > 0) ? ("_EmissionMap_Atlas = " + fingerprint._EmissionMap_Atlas + "\n") : "");
		array[160] = ((_EmissionMap_AtlasSlice > 0) ? ("_EmissionMap_AtlasSlice = " + fingerprint._EmissionMap_AtlasSlice + "\n") : "");
		array[161] = ((_DeformMap_Atlas > 0) ? ("_DeformMap_Atlas = " + fingerprint._DeformMap_Atlas + "\n") : "");
		array[162] = ((_DeformMap_AtlasSlice > 0) ? ("_DeformMap_AtlasSlice = " + fingerprint._DeformMap_AtlasSlice + "\n") : "");
		array[163] = ((_WeatherMap_Atlas > 0) ? ("_WeatherMap_Atlas = " + fingerprint._WeatherMap_Atlas + "\n") : "");
		array[164] = ((_WeatherMap_AtlasSlice > 0) ? ("_WeatherMap_AtlasSlice = " + fingerprint._WeatherMap_AtlasSlice + "\n") : "");
		array[165] = ((_DEBUG_PAWN_DATA > 0) ? ("_DEBUG_PAWN_DATA = " + fingerprint._DEBUG_PAWN_DATA + "\n") : "");
		array[166] = ((_SrcBlend > 0) ? ("_SrcBlend = " + fingerprint._SrcBlend + "\n") : "");
		array[167] = ((_DstBlend > 0) ? ("_DstBlend = " + fingerprint._DstBlend + "\n") : "");
		array[168] = ((_SrcBlendAlpha > 0) ? ("_SrcBlendAlpha = " + fingerprint._SrcBlendAlpha + "\n") : "");
		array[169] = ((_DstBlendAlpha > 0) ? ("_DstBlendAlpha = " + fingerprint._DstBlendAlpha + "\n") : "");
		array[170] = ((_ZWrite > 0) ? ("_ZWrite = " + fingerprint._ZWrite + "\n") : "");
		array[171] = ((_AlphaToMask > 0) ? ("_AlphaToMask = " + fingerprint._AlphaToMask + "\n") : "");
		object obj51;
		if (_Color <= 0)
		{
			obj51 = "";
		}
		else
		{
			int4 baseColor = fingerprint._Color;
			obj51 = "_Color = " + baseColor.ToString() + "\n";
		}
		array[172] = (string)obj51;
		array[173] = ((_Surface > 0) ? ("_Surface = " + fingerprint._Surface + "\n") : "");
		array[174] = ((_Metallic > 0) ? ("_Metallic = " + fingerprint._Metallic + "\n") : "");
		object obj52;
		if (_SpecColor <= 0)
		{
			obj52 = "";
		}
		else
		{
			int4 baseColor = fingerprint._SpecColor;
			obj52 = "_SpecColor = " + baseColor.ToString() + "\n";
		}
		array[175] = (string)obj52;
		array[176] = ((_DayNightLightmapArray > 0) ? ("_DayNightLightmapArray = " + fingerprint._DayNightLightmapArray + "\n") : "");
		object obj53;
		if (_DayNightLightmapArray_ST <= 0)
		{
			obj53 = "";
		}
		else
		{
			int4 baseColor = fingerprint._DayNightLightmapArray_ST;
			obj53 = "_DayNightLightmapArray_ST = " + baseColor.ToString() + "\n";
		}
		array[177] = (string)obj53;
		array[178] = ((_DayNightLightmapArray_AtlasSlice > 0) ? ("_DayNightLightmapArray_AtlasSlice = " + fingerprint._DayNightLightmapArray_AtlasSlice + "\n") : "");
		return string.Concat(array);
	}

	public string ToStringTSV()
	{
		string[] array = new string[707];
		array[0] = "---- MaterialFingerprint of ";
		array[1] = material?.name;
		array[2] = " ----\nName,\tUsed?,\tRounded Value_TransparencyMode,\t";
		array[3] = (_TransparencyMode > 0).ToString();
		array[4] = ",\t";
		array[5] = fingerprint._TransparencyMode.ToString();
		array[6] = "\n_Cutoff,\t";
		array[7] = (_Cutoff > 0).ToString();
		array[8] = ",\t";
		array[9] = fingerprint._Cutoff.ToString();
		array[10] = "\n_ColorSource,\t";
		array[11] = (_ColorSource > 0).ToString();
		array[12] = ",\t";
		array[13] = fingerprint._ColorSource.ToString();
		array[14] = "\n_BaseColor,\t";
		array[15] = (_BaseColor > 0).ToString();
		array[16] = ",\t";
		int4 baseColor = fingerprint._BaseColor;
		array[17] = baseColor.ToString();
		array[18] = "\n_GChannelColor,\t";
		array[19] = (_GChannelColor > 0).ToString();
		array[20] = ",\t";
		baseColor = fingerprint._GChannelColor;
		array[21] = baseColor.ToString();
		array[22] = "\n_BChannelColor,\t";
		array[23] = (_BChannelColor > 0).ToString();
		array[24] = ",\t";
		baseColor = fingerprint._BChannelColor;
		array[25] = baseColor.ToString();
		array[26] = "\n_AChannelColor,\t";
		array[27] = (_AChannelColor > 0).ToString();
		array[28] = ",\t";
		baseColor = fingerprint._AChannelColor;
		array[29] = baseColor.ToString();
		array[30] = "\n_BaseMap,\t";
		array[31] = (_BaseMap > 0).ToString();
		array[32] = ",\t";
		array[33] = fingerprint._BaseMap;
		array[34] = "\n_BaseMap_ST,\t";
		array[35] = (_BaseMap_ST > 0).ToString();
		array[36] = ",\t";
		baseColor = fingerprint._BaseMap_ST;
		array[37] = baseColor.ToString();
		array[38] = "\n_SettingsPreset,\t";
		array[39] = (_SettingsPreset > 0).ToString();
		array[40] = ",\t";
		array[41] = fingerprint._SettingsPreset.ToString();
		array[42] = "\n_AdvancedOptions,\t";
		array[43] = (_AdvancedOptions > 0).ToString();
		array[44] = ",\t";
		array[45] = fingerprint._AdvancedOptions.ToString();
		array[46] = "\n_TexMipBias,\t";
		array[47] = (_TexMipBias > 0).ToString();
		array[48] = ",\t";
		array[49] = fingerprint._TexMipBias.ToString();
		array[50] = "\n_BaseMap_WH,\t";
		array[51] = (_BaseMap_WH > 0).ToString();
		array[52] = ",\t";
		baseColor = fingerprint._BaseMap_WH;
		array[53] = baseColor.ToString();
		array[54] = "\n_TexelSnapToggle,\t";
		array[55] = (_TexelSnapToggle > 0).ToString();
		array[56] = ",\t";
		array[57] = fingerprint._TexelSnapToggle.ToString();
		array[58] = "\n_TexelSnap_Factor,\t";
		array[59] = (_TexelSnap_Factor > 0).ToString();
		array[60] = ",\t";
		array[61] = fingerprint._TexelSnap_Factor.ToString();
		array[62] = "\n_UVSource,\t";
		array[63] = (_UVSource > 0).ToString();
		array[64] = ",\t";
		array[65] = fingerprint._UVSource.ToString();
		array[66] = "\n_AlphaDetailToggle,\t";
		array[67] = (_AlphaDetailToggle > 0).ToString();
		array[68] = ",\t";
		array[69] = fingerprint._AlphaDetailToggle.ToString();
		array[70] = "\n_AlphaDetail_ST,\t";
		array[71] = (_AlphaDetail_ST > 0).ToString();
		array[72] = ",\t";
		baseColor = fingerprint._AlphaDetail_ST;
		array[73] = baseColor.ToString();
		array[74] = "\n_AlphaDetail_Opacity,\t";
		array[75] = (_AlphaDetail_Opacity > 0).ToString();
		array[76] = ",\t";
		array[77] = fingerprint._AlphaDetail_Opacity.ToString();
		array[78] = "\n_AlphaDetail_WorldSpace,\t";
		array[79] = (_AlphaDetail_WorldSpace > 0).ToString();
		array[80] = ",\t";
		array[81] = fingerprint._AlphaDetail_WorldSpace.ToString();
		array[82] = "\n_MaskMapToggle,\t";
		array[83] = (_MaskMapToggle > 0).ToString();
		array[84] = ",\t";
		array[85] = fingerprint._MaskMapToggle.ToString();
		array[86] = "\n_MaskMap,\t";
		array[87] = (_MaskMap > 0).ToString();
		array[88] = ",\t";
		array[89] = fingerprint._MaskMap;
		array[90] = "\n_MaskMap_ST,\t";
		array[91] = (_MaskMap_ST > 0).ToString();
		array[92] = ",\t";
		baseColor = fingerprint._MaskMap_ST;
		array[93] = baseColor.ToString();
		array[94] = "\n_MaskMap_WH,\t";
		array[95] = (_MaskMap_WH > 0).ToString();
		array[96] = ",\t";
		baseColor = fingerprint._MaskMap_WH;
		array[97] = baseColor.ToString();
		array[98] = "\n_LavaLampToggle,\t";
		array[99] = (_LavaLampToggle > 0).ToString();
		array[100] = ",\t";
		array[101] = fingerprint._LavaLampToggle.ToString();
		array[102] = "\n_GradientMapToggle,\t";
		array[103] = (_GradientMapToggle > 0).ToString();
		array[104] = ",\t";
		array[105] = fingerprint._GradientMapToggle.ToString();
		array[106] = "\n_GradientMap,\t";
		array[107] = (_GradientMap > 0).ToString();
		array[108] = ",\t";
		array[109] = fingerprint._GradientMap;
		array[110] = "\n_DoTextureRotation,\t";
		array[111] = (_DoTextureRotation > 0).ToString();
		array[112] = ",\t";
		array[113] = fingerprint._DoTextureRotation.ToString();
		array[114] = "\n_RotateAngle,\t";
		array[115] = (_RotateAngle > 0).ToString();
		array[116] = ",\t";
		array[117] = fingerprint._RotateAngle.ToString();
		array[118] = "\n_RotateAnim,\t";
		array[119] = (_RotateAnim > 0).ToString();
		array[120] = ",\t";
		array[121] = fingerprint._RotateAnim.ToString();
		array[122] = "\n_UseWaveWarp,\t";
		array[123] = (_UseWaveWarp > 0).ToString();
		array[124] = ",\t";
		array[125] = fingerprint._UseWaveWarp.ToString();
		array[126] = "\n_WaveAmplitude,\t";
		array[127] = (_WaveAmplitude > 0).ToString();
		array[128] = ",\t";
		array[129] = fingerprint._WaveAmplitude.ToString();
		array[130] = "\n_WaveFrequency,\t";
		array[131] = (_WaveFrequency > 0).ToString();
		array[132] = ",\t";
		array[133] = fingerprint._WaveFrequency.ToString();
		array[134] = "\n_WaveScale,\t";
		array[135] = (_WaveScale > 0).ToString();
		array[136] = ",\t";
		array[137] = fingerprint._WaveScale.ToString();
		array[138] = "\n_WaveTimeScale,\t";
		array[139] = (_WaveTimeScale > 0).ToString();
		array[140] = ",\t";
		array[141] = fingerprint._WaveTimeScale.ToString();
		array[142] = "\n_ReflectToggle,\t";
		array[143] = (_ReflectToggle > 0).ToString();
		array[144] = ",\t";
		array[145] = fingerprint._ReflectToggle.ToString();
		array[146] = "\n_ReflectBoxProjectToggle,\t";
		array[147] = (_ReflectBoxProjectToggle > 0).ToString();
		array[148] = ",\t";
		array[149] = fingerprint._ReflectBoxProjectToggle.ToString();
		array[150] = "\n_ReflectBoxCubePos,\t";
		array[151] = (_ReflectBoxCubePos > 0).ToString();
		array[152] = ",\t";
		baseColor = fingerprint._ReflectBoxCubePos;
		array[153] = baseColor.ToString();
		array[154] = "\n_ReflectBoxSize,\t";
		array[155] = (_ReflectBoxSize > 0).ToString();
		array[156] = ",\t";
		baseColor = fingerprint._ReflectBoxSize;
		array[157] = baseColor.ToString();
		array[158] = "\n_ReflectBoxRotation,\t";
		array[159] = (_ReflectBoxRotation > 0).ToString();
		array[160] = ",\t";
		baseColor = fingerprint._ReflectBoxRotation;
		array[161] = baseColor.ToString();
		array[162] = "\n_ReflectMatcapToggle,\t";
		array[163] = (_ReflectMatcapToggle > 0).ToString();
		array[164] = ",\t";
		array[165] = fingerprint._ReflectMatcapToggle.ToString();
		array[166] = "\n_ReflectMatcapPerspToggle,\t";
		array[167] = (_ReflectMatcapPerspToggle > 0).ToString();
		array[168] = ",\t";
		array[169] = fingerprint._ReflectMatcapPerspToggle.ToString();
		array[170] = "\n_ReflectNormalToggle,\t";
		array[171] = (_ReflectNormalToggle > 0).ToString();
		array[172] = ",\t";
		array[173] = fingerprint._ReflectNormalToggle.ToString();
		array[174] = "\n_ReflectTex,\t";
		array[175] = (_ReflectTex > 0).ToString();
		array[176] = ",\t";
		array[177] = fingerprint._ReflectTex;
		array[178] = "\n_ReflectNormalTex,\t";
		array[179] = (_ReflectNormalTex > 0).ToString();
		array[180] = ",\t";
		array[181] = fingerprint._ReflectNormalTex;
		array[182] = "\n_ReflectAlbedoTint,\t";
		array[183] = (_ReflectAlbedoTint > 0).ToString();
		array[184] = ",\t";
		array[185] = fingerprint._ReflectAlbedoTint.ToString();
		array[186] = "\n_ReflectTint,\t";
		array[187] = (_ReflectTint > 0).ToString();
		array[188] = ",\t";
		baseColor = fingerprint._ReflectTint;
		array[189] = baseColor.ToString();
		array[190] = "\n_ReflectOpacity,\t";
		array[191] = (_ReflectOpacity > 0).ToString();
		array[192] = ",\t";
		array[193] = fingerprint._ReflectOpacity.ToString();
		array[194] = "\n_ReflectExposure,\t";
		array[195] = (_ReflectExposure > 0).ToString();
		array[196] = ",\t";
		array[197] = fingerprint._ReflectExposure.ToString();
		array[198] = "\n_ReflectOffset,\t";
		array[199] = (_ReflectOffset > 0).ToString();
		array[200] = ",\t";
		baseColor = fingerprint._ReflectOffset;
		array[201] = baseColor.ToString();
		array[202] = "\n_ReflectScale,\t";
		array[203] = (_ReflectScale > 0).ToString();
		array[204] = ",\t";
		baseColor = fingerprint._ReflectScale;
		array[205] = baseColor.ToString();
		array[206] = "\n_ReflectRotate,\t";
		array[207] = (_ReflectRotate > 0).ToString();
		array[208] = ",\t";
		array[209] = fingerprint._ReflectRotate.ToString();
		array[210] = "\n_HalfLambertToggle,\t";
		array[211] = (_HalfLambertToggle > 0).ToString();
		array[212] = ",\t";
		array[213] = fingerprint._HalfLambertToggle.ToString();
		array[214] = "\n_ParallaxPlanarToggle,\t";
		array[215] = (_ParallaxPlanarToggle > 0).ToString();
		array[216] = ",\t";
		array[217] = fingerprint._ParallaxPlanarToggle.ToString();
		array[218] = "\n_ParallaxToggle,\t";
		array[219] = (_ParallaxToggle > 0).ToString();
		array[220] = ",\t";
		array[221] = fingerprint._ParallaxToggle.ToString();
		array[222] = "\n_ParallaxAAToggle,\t";
		array[223] = (_ParallaxAAToggle > 0).ToString();
		array[224] = ",\t";
		array[225] = fingerprint._ParallaxAAToggle.ToString();
		array[226] = "\n_ParallaxAABias,\t";
		array[227] = (_ParallaxAABias > 0).ToString();
		array[228] = ",\t";
		array[229] = fingerprint._ParallaxAABias.ToString();
		array[230] = "\n_DepthMap,\t";
		array[231] = (_DepthMap > 0).ToString();
		array[232] = ",\t";
		array[233] = fingerprint._DepthMap;
		array[234] = "\n_ParallaxAmplitude,\t";
		array[235] = (_ParallaxAmplitude > 0).ToString();
		array[236] = ",\t";
		array[237] = fingerprint._ParallaxAmplitude.ToString();
		array[238] = "\n_ParallaxSamplesMinMax,\t";
		array[239] = (_ParallaxSamplesMinMax > 0).ToString();
		array[240] = ",\t";
		baseColor = fingerprint._ParallaxSamplesMinMax;
		array[241] = baseColor.ToString();
		array[242] = "\n_UvShiftToggle,\t";
		array[243] = (_UvShiftToggle > 0).ToString();
		array[244] = ",\t";
		array[245] = fingerprint._UvShiftToggle.ToString();
		array[246] = "\n_UvShiftSteps,\t";
		array[247] = (_UvShiftSteps > 0).ToString();
		array[248] = ",\t";
		baseColor = fingerprint._UvShiftSteps;
		array[249] = baseColor.ToString();
		array[250] = "\n_UvShiftRate,\t";
		array[251] = (_UvShiftRate > 0).ToString();
		array[252] = ",\t";
		baseColor = fingerprint._UvShiftRate;
		array[253] = baseColor.ToString();
		array[254] = "\n_UvShiftOffset,\t";
		array[255] = (_UvShiftOffset > 0).ToString();
		array[256] = ",\t";
		baseColor = fingerprint._UvShiftOffset;
		array[257] = baseColor.ToString();
		array[258] = "\n_UseGridEffect,\t";
		array[259] = (_UseGridEffect > 0).ToString();
		array[260] = ",\t";
		array[261] = fingerprint._UseGridEffect.ToString();
		array[262] = "\n_UseCrystalEffect,\t";
		array[263] = (_UseCrystalEffect > 0).ToString();
		array[264] = ",\t";
		array[265] = fingerprint._UseCrystalEffect.ToString();
		array[266] = "\n_CrystalPower,\t";
		array[267] = (_CrystalPower > 0).ToString();
		array[268] = ",\t";
		array[269] = fingerprint._CrystalPower.ToString();
		array[270] = "\n_CrystalRimColor,\t";
		array[271] = (_CrystalRimColor > 0).ToString();
		array[272] = ",\t";
		baseColor = fingerprint._CrystalRimColor;
		array[273] = baseColor.ToString();
		array[274] = "\n_LiquidVolume,\t";
		array[275] = (_LiquidVolume > 0).ToString();
		array[276] = ",\t";
		array[277] = fingerprint._LiquidVolume.ToString();
		array[278] = "\n_LiquidFill,\t";
		array[279] = (_LiquidFill > 0).ToString();
		array[280] = ",\t";
		array[281] = fingerprint._LiquidFill.ToString();
		array[282] = "\n_LiquidFillNormal,\t";
		array[283] = (_LiquidFillNormal > 0).ToString();
		array[284] = ",\t";
		baseColor = fingerprint._LiquidFillNormal;
		array[285] = baseColor.ToString();
		array[286] = "\n_LiquidSurfaceColor,\t";
		array[287] = (_LiquidSurfaceColor > 0).ToString();
		array[288] = ",\t";
		baseColor = fingerprint._LiquidSurfaceColor;
		array[289] = baseColor.ToString();
		array[290] = "\n_LiquidSwayX,\t";
		array[291] = (_LiquidSwayX > 0).ToString();
		array[292] = ",\t";
		array[293] = fingerprint._LiquidSwayX.ToString();
		array[294] = "\n_LiquidSwayY,\t";
		array[295] = (_LiquidSwayY > 0).ToString();
		array[296] = ",\t";
		array[297] = fingerprint._LiquidSwayY.ToString();
		array[298] = "\n_LiquidContainer,\t";
		array[299] = (_LiquidContainer > 0).ToString();
		array[300] = ",\t";
		array[301] = fingerprint._LiquidContainer.ToString();
		array[302] = "\n_LiquidPlanePosition,\t";
		array[303] = (_LiquidPlanePosition > 0).ToString();
		array[304] = ",\t";
		baseColor = fingerprint._LiquidPlanePosition;
		array[305] = baseColor.ToString();
		array[306] = "\n_LiquidPlaneNormal,\t";
		array[307] = (_LiquidPlaneNormal > 0).ToString();
		array[308] = ",\t";
		baseColor = fingerprint._LiquidPlaneNormal;
		array[309] = baseColor.ToString();
		array[310] = "\n_VertexFlapToggle,\t";
		array[311] = (_VertexFlapToggle > 0).ToString();
		array[312] = ",\t";
		array[313] = fingerprint._VertexFlapToggle.ToString();
		array[314] = "\n_VertexFlapAxis,\t";
		array[315] = (_VertexFlapAxis > 0).ToString();
		array[316] = ",\t";
		baseColor = fingerprint._VertexFlapAxis;
		array[317] = baseColor.ToString();
		array[318] = "\n_VertexFlapDegreesMinMax,\t";
		array[319] = (_VertexFlapDegreesMinMax > 0).ToString();
		array[320] = ",\t";
		baseColor = fingerprint._VertexFlapDegreesMinMax;
		array[321] = baseColor.ToString();
		array[322] = "\n_VertexFlapSpeed,\t";
		array[323] = (_VertexFlapSpeed > 0).ToString();
		array[324] = ",\t";
		array[325] = fingerprint._VertexFlapSpeed.ToString();
		array[326] = "\n_VertexFlapPhaseOffset,\t";
		array[327] = (_VertexFlapPhaseOffset > 0).ToString();
		array[328] = ",\t";
		array[329] = fingerprint._VertexFlapPhaseOffset.ToString();
		array[330] = "\n_VertexWaveToggle,\t";
		array[331] = (_VertexWaveToggle > 0).ToString();
		array[332] = ",\t";
		array[333] = fingerprint._VertexWaveToggle.ToString();
		array[334] = "\n_VertexWaveDebug,\t";
		array[335] = (_VertexWaveDebug > 0).ToString();
		array[336] = ",\t";
		array[337] = fingerprint._VertexWaveDebug.ToString();
		array[338] = "\n_VertexWaveEnd,\t";
		array[339] = (_VertexWaveEnd > 0).ToString();
		array[340] = ",\t";
		baseColor = fingerprint._VertexWaveEnd;
		array[341] = baseColor.ToString();
		array[342] = "\n_VertexWaveParams,\t";
		array[343] = (_VertexWaveParams > 0).ToString();
		array[344] = ",\t";
		baseColor = fingerprint._VertexWaveParams;
		array[345] = baseColor.ToString();
		array[346] = "\n_VertexWaveFalloff,\t";
		array[347] = (_VertexWaveFalloff > 0).ToString();
		array[348] = ",\t";
		baseColor = fingerprint._VertexWaveFalloff;
		array[349] = baseColor.ToString();
		array[350] = "\n_VertexWaveSphereMask,\t";
		array[351] = (_VertexWaveSphereMask > 0).ToString();
		array[352] = ",\t";
		baseColor = fingerprint._VertexWaveSphereMask;
		array[353] = baseColor.ToString();
		array[354] = "\n_VertexWavePhaseOffset,\t";
		array[355] = (_VertexWavePhaseOffset > 0).ToString();
		array[356] = ",\t";
		array[357] = fingerprint._VertexWavePhaseOffset.ToString();
		array[358] = "\n_VertexWaveAxes,\t";
		array[359] = (_VertexWaveAxes > 0).ToString();
		array[360] = ",\t";
		baseColor = fingerprint._VertexWaveAxes;
		array[361] = baseColor.ToString();
		array[362] = "\n_VertexRotateToggle,\t";
		array[363] = (_VertexRotateToggle > 0).ToString();
		array[364] = ",\t";
		array[365] = fingerprint._VertexRotateToggle.ToString();
		array[366] = "\n_VertexRotateAngles,\t";
		array[367] = (_VertexRotateAngles > 0).ToString();
		array[368] = ",\t";
		baseColor = fingerprint._VertexRotateAngles;
		array[369] = baseColor.ToString();
		array[370] = "\n_VertexRotateAnim,\t";
		array[371] = (_VertexRotateAnim > 0).ToString();
		array[372] = ",\t";
		array[373] = fingerprint._VertexRotateAnim.ToString();
		array[374] = "\n_VertexLightToggle,\t";
		array[375] = (_VertexLightToggle > 0).ToString();
		array[376] = ",\t";
		array[377] = fingerprint._VertexLightToggle.ToString();
		array[378] = "\n_InnerGlowOn,\t";
		array[379] = (_InnerGlowOn > 0).ToString();
		array[380] = ",\t";
		array[381] = fingerprint._InnerGlowOn.ToString();
		array[382] = "\n_InnerGlowColor,\t";
		array[383] = (_InnerGlowColor > 0).ToString();
		array[384] = ",\t";
		baseColor = fingerprint._InnerGlowColor;
		array[385] = baseColor.ToString();
		array[386] = "\n_InnerGlowParams,\t";
		array[387] = (_InnerGlowParams > 0).ToString();
		array[388] = ",\t";
		baseColor = fingerprint._InnerGlowParams;
		array[389] = baseColor.ToString();
		array[390] = "\n_InnerGlowTap,\t";
		array[391] = (_InnerGlowTap > 0).ToString();
		array[392] = ",\t";
		array[393] = fingerprint._InnerGlowTap.ToString();
		array[394] = "\n_InnerGlowSine,\t";
		array[395] = (_InnerGlowSine > 0).ToString();
		array[396] = ",\t";
		array[397] = fingerprint._InnerGlowSine.ToString();
		array[398] = "\n_InnerGlowSinePeriod,\t";
		array[399] = (_InnerGlowSinePeriod > 0).ToString();
		array[400] = ",\t";
		array[401] = fingerprint._InnerGlowSinePeriod.ToString();
		array[402] = "\n_InnerGlowSinePhaseShift,\t";
		array[403] = (_InnerGlowSinePhaseShift > 0).ToString();
		array[404] = ",\t";
		array[405] = fingerprint._InnerGlowSinePhaseShift.ToString();
		array[406] = "\n_StealthEffectOn,\t";
		array[407] = (_StealthEffectOn > 0).ToString();
		array[408] = ",\t";
		array[409] = fingerprint._StealthEffectOn.ToString();
		array[410] = "\n_UseEyeTracking,\t";
		array[411] = (_UseEyeTracking > 0).ToString();
		array[412] = ",\t";
		array[413] = fingerprint._UseEyeTracking.ToString();
		array[414] = "\n_EyeTileOffsetUV,\t";
		array[415] = (_EyeTileOffsetUV > 0).ToString();
		array[416] = ",\t";
		baseColor = fingerprint._EyeTileOffsetUV;
		array[417] = baseColor.ToString();
		array[418] = "\n_EyeOverrideUV,\t";
		array[419] = (_EyeOverrideUV > 0).ToString();
		array[420] = ",\t";
		array[421] = fingerprint._EyeOverrideUV.ToString();
		array[422] = "\n_EyeOverrideUVTransform,\t";
		array[423] = (_EyeOverrideUVTransform > 0).ToString();
		array[424] = ",\t";
		baseColor = fingerprint._EyeOverrideUVTransform;
		array[425] = baseColor.ToString();
		array[426] = "\n_UseMouthFlap,\t";
		array[427] = (_UseMouthFlap > 0).ToString();
		array[428] = ",\t";
		array[429] = fingerprint._UseMouthFlap.ToString();
		array[430] = "\n_MouthMap,\t";
		array[431] = (_MouthMap > 0).ToString();
		array[432] = ",\t";
		array[433] = fingerprint._MouthMap;
		array[434] = "\n_MouthMap_ST,\t";
		array[435] = (_MouthMap_ST > 0).ToString();
		array[436] = ",\t";
		baseColor = fingerprint._MouthMap_ST;
		array[437] = baseColor.ToString();
		array[438] = "\n_UseVertexColor,\t";
		array[439] = (_UseVertexColor > 0).ToString();
		array[440] = ",\t";
		array[441] = fingerprint._UseVertexColor.ToString();
		array[442] = "\n_WaterEffect,\t";
		array[443] = (_WaterEffect > 0).ToString();
		array[444] = ",\t";
		array[445] = fingerprint._WaterEffect.ToString();
		array[446] = "\n_HeightBasedWaterEffect,\t";
		array[447] = (_HeightBasedWaterEffect > 0).ToString();
		array[448] = ",\t";
		array[449] = fingerprint._HeightBasedWaterEffect.ToString();
		array[450] = "\n_WaterCaustics,\t";
		array[451] = (_WaterCaustics > 0).ToString();
		array[452] = ",\t";
		array[453] = fingerprint._WaterCaustics.ToString();
		array[454] = "\n_UseDayNightLightmap,\t";
		array[455] = (_UseDayNightLightmap > 0).ToString();
		array[456] = ",\t";
		array[457] = fingerprint._UseDayNightLightmap.ToString();
		array[458] = "\n_DAY_CYCLE_BRIGHTNESS_,\t";
		array[459] = (_DAY_CYCLE_BRIGHTNESS_ > 0).ToString();
		array[460] = ",\t";
		array[461] = fingerprint._DAY_CYCLE_BRIGHTNESS_.ToString();
		array[462] = "\n_UseWeatherMap,\t";
		array[463] = (_UseWeatherMap > 0).ToString();
		array[464] = ",\t";
		array[465] = fingerprint._UseWeatherMap.ToString();
		array[466] = "\n_WeatherMap,\t";
		array[467] = (_WeatherMap > 0).ToString();
		array[468] = ",\t";
		array[469] = fingerprint._WeatherMap;
		array[470] = "\n_WeatherMapDissolveEdgeSize,\t";
		array[471] = (_WeatherMapDissolveEdgeSize > 0).ToString();
		array[472] = ",\t";
		array[473] = fingerprint._WeatherMapDissolveEdgeSize.ToString();
		array[474] = "\n_UseSpecular,\t";
		array[475] = (_UseSpecular > 0).ToString();
		array[476] = ",\t";
		array[477] = fingerprint._UseSpecular.ToString();
		array[478] = "\n_UseSpecularAlphaChannel,\t";
		array[479] = (_UseSpecularAlphaChannel > 0).ToString();
		array[480] = ",\t";
		array[481] = fingerprint._UseSpecularAlphaChannel.ToString();
		array[482] = "\n_Smoothness,\t";
		array[483] = (_Smoothness > 0).ToString();
		array[484] = ",\t";
		array[485] = fingerprint._Smoothness.ToString();
		array[486] = "\n_UseSpecHighlight,\t";
		array[487] = (_UseSpecHighlight > 0).ToString();
		array[488] = ",\t";
		array[489] = fingerprint._UseSpecHighlight.ToString();
		array[490] = "\n_SpecularDir,\t";
		array[491] = (_SpecularDir > 0).ToString();
		array[492] = ",\t";
		baseColor = fingerprint._SpecularDir;
		array[493] = baseColor.ToString();
		array[494] = "\n_SpecularPowerIntensity,\t";
		array[495] = (_SpecularPowerIntensity > 0).ToString();
		array[496] = ",\t";
		baseColor = fingerprint._SpecularPowerIntensity;
		array[497] = baseColor.ToString();
		array[498] = "\n_SpecularColor,\t";
		array[499] = (_SpecularColor > 0).ToString();
		array[500] = ",\t";
		baseColor = fingerprint._SpecularColor;
		array[501] = baseColor.ToString();
		array[502] = "\n_SpecularUseDiffuseColor,\t";
		array[503] = (_SpecularUseDiffuseColor > 0).ToString();
		array[504] = ",\t";
		array[505] = fingerprint._SpecularUseDiffuseColor.ToString();
		array[506] = "\n_EmissionToggle,\t";
		array[507] = (_EmissionToggle > 0).ToString();
		array[508] = ",\t";
		array[509] = fingerprint._EmissionToggle.ToString();
		array[510] = "\n_EmissionColor,\t";
		array[511] = (_EmissionColor > 0).ToString();
		array[512] = ",\t";
		baseColor = fingerprint._EmissionColor;
		array[513] = baseColor.ToString();
		array[514] = "\n_EmissionMap,\t";
		array[515] = (_EmissionMap > 0).ToString();
		array[516] = ",\t";
		array[517] = fingerprint._EmissionMap;
		array[518] = "\n_EmissionMaskByBaseMapAlpha,\t";
		array[519] = (_EmissionMaskByBaseMapAlpha > 0).ToString();
		array[520] = ",\t";
		array[521] = fingerprint._EmissionMaskByBaseMapAlpha.ToString();
		array[522] = "\n_EmissionUVScrollSpeed,\t";
		array[523] = (_EmissionUVScrollSpeed > 0).ToString();
		array[524] = ",\t";
		baseColor = fingerprint._EmissionUVScrollSpeed;
		array[525] = baseColor.ToString();
		array[526] = "\n_EmissionDissolveProgress,\t";
		array[527] = (_EmissionDissolveProgress > 0).ToString();
		array[528] = ",\t";
		array[529] = fingerprint._EmissionDissolveProgress.ToString();
		array[530] = "\n_EmissionDissolveAnimation,\t";
		array[531] = (_EmissionDissolveAnimation > 0).ToString();
		array[532] = ",\t";
		baseColor = fingerprint._EmissionDissolveAnimation;
		array[533] = baseColor.ToString();
		array[534] = "\n_EmissionDissolveEdgeSize,\t";
		array[535] = (_EmissionDissolveEdgeSize > 0).ToString();
		array[536] = ",\t";
		array[537] = fingerprint._EmissionDissolveEdgeSize.ToString();
		array[538] = "\n_EmissionIntensityInDynamic,\t";
		array[539] = (_EmissionIntensityInDynamic > 0).ToString();
		array[540] = ",\t";
		array[541] = fingerprint._EmissionIntensityInDynamic.ToString();
		array[542] = "\n_EmissionUseUVWaveWarp,\t";
		array[543] = (_EmissionUseUVWaveWarp > 0).ToString();
		array[544] = ",\t";
		array[545] = fingerprint._EmissionUseUVWaveWarp.ToString();
		array[546] = "\n_GreyZoneException,\t";
		array[547] = (_GreyZoneException > 0).ToString();
		array[548] = ",\t";
		array[549] = fingerprint._GreyZoneException.ToString();
		array[550] = "\n_Cull,\t";
		array[551] = (_Cull > 0).ToString();
		array[552] = ",\t";
		array[553] = fingerprint._Cull.ToString();
		array[554] = "\n_StencilReference,\t";
		array[555] = (_StencilReference > 0).ToString();
		array[556] = ",\t";
		array[557] = fingerprint._StencilReference.ToString();
		array[558] = "\n_StencilComparison,\t";
		array[559] = (_StencilComparison > 0).ToString();
		array[560] = ",\t";
		array[561] = fingerprint._StencilComparison.ToString();
		array[562] = "\n_StencilPassFront,\t";
		array[563] = (_StencilPassFront > 0).ToString();
		array[564] = ",\t";
		array[565] = fingerprint._StencilPassFront.ToString();
		array[566] = "\n_USE_DEFORM_MAP,\t";
		array[567] = (_USE_DEFORM_MAP > 0).ToString();
		array[568] = ",\t";
		array[569] = fingerprint._USE_DEFORM_MAP.ToString();
		array[570] = "\n_DeformMap,\t";
		array[571] = (_DeformMap > 0).ToString();
		array[572] = ",\t";
		array[573] = fingerprint._DeformMap;
		array[574] = "\n_DeformMapIntensity,\t";
		array[575] = (_DeformMapIntensity > 0).ToString();
		array[576] = ",\t";
		array[577] = fingerprint._DeformMapIntensity.ToString();
		array[578] = "\n_DeformMapMaskByVertColorRAmount,\t";
		array[579] = (_DeformMapMaskByVertColorRAmount > 0).ToString();
		array[580] = ",\t";
		array[581] = fingerprint._DeformMapMaskByVertColorRAmount.ToString();
		array[582] = "\n_DeformMapScrollSpeed,\t";
		array[583] = (_DeformMapScrollSpeed > 0).ToString();
		array[584] = ",\t";
		baseColor = fingerprint._DeformMapScrollSpeed;
		array[585] = baseColor.ToString();
		array[586] = "\n_DeformMapUV0Influence,\t";
		array[587] = (_DeformMapUV0Influence > 0).ToString();
		array[588] = ",\t";
		baseColor = fingerprint._DeformMapUV0Influence;
		array[589] = baseColor.ToString();
		array[590] = "\n_DeformMapObjectSpaceOffsetsU,\t";
		array[591] = (_DeformMapObjectSpaceOffsetsU > 0).ToString();
		array[592] = ",\t";
		baseColor = fingerprint._DeformMapObjectSpaceOffsetsU;
		array[593] = baseColor.ToString();
		array[594] = "\n_DeformMapObjectSpaceOffsetsV,\t";
		array[595] = (_DeformMapObjectSpaceOffsetsV > 0).ToString();
		array[596] = ",\t";
		baseColor = fingerprint._DeformMapObjectSpaceOffsetsV;
		array[597] = baseColor.ToString();
		array[598] = "\n_DeformMapWorldSpaceOffsetsU,\t";
		array[599] = (_DeformMapWorldSpaceOffsetsU > 0).ToString();
		array[600] = ",\t";
		baseColor = fingerprint._DeformMapWorldSpaceOffsetsU;
		array[601] = baseColor.ToString();
		array[602] = "\n_DeformMapWorldSpaceOffsetsV,\t";
		array[603] = (_DeformMapWorldSpaceOffsetsV > 0).ToString();
		array[604] = ",\t";
		baseColor = fingerprint._DeformMapWorldSpaceOffsetsV;
		array[605] = baseColor.ToString();
		array[606] = "\n_RotateOnYAxisBySinTime,\t";
		array[607] = (_RotateOnYAxisBySinTime > 0).ToString();
		array[608] = ",\t";
		baseColor = fingerprint._RotateOnYAxisBySinTime;
		array[609] = baseColor.ToString();
		array[610] = "\n_USE_TEX_ARRAY_ATLAS,\t";
		array[611] = (_USE_TEX_ARRAY_ATLAS > 0).ToString();
		array[612] = ",\t";
		array[613] = fingerprint._USE_TEX_ARRAY_ATLAS.ToString();
		array[614] = "\n_BaseMap_Atlas,\t";
		array[615] = (_BaseMap_Atlas > 0).ToString();
		array[616] = ",\t";
		array[617] = fingerprint._BaseMap_Atlas;
		array[618] = "\n_BaseMap_AtlasSlice,\t";
		array[619] = (_BaseMap_AtlasSlice > 0).ToString();
		array[620] = ",\t";
		array[621] = fingerprint._BaseMap_AtlasSlice.ToString();
		array[622] = "\n_BaseMap_AtlasSliceSource,\t";
		array[623] = (_BaseMap_AtlasSliceSource > 0).ToString();
		array[624] = ",\t";
		array[625] = fingerprint._BaseMap_AtlasSliceSource.ToString();
		array[626] = "\n_EmissionMap_Atlas,\t";
		array[627] = (_EmissionMap_Atlas > 0).ToString();
		array[628] = ",\t";
		array[629] = fingerprint._EmissionMap_Atlas;
		array[630] = "\n_EmissionMap_AtlasSlice,\t";
		array[631] = (_EmissionMap_AtlasSlice > 0).ToString();
		array[632] = ",\t";
		array[633] = fingerprint._EmissionMap_AtlasSlice.ToString();
		array[634] = "\n_DeformMap_Atlas,\t";
		array[635] = (_DeformMap_Atlas > 0).ToString();
		array[636] = ",\t";
		array[637] = fingerprint._DeformMap_Atlas;
		array[638] = "\n_DeformMap_AtlasSlice,\t";
		array[639] = (_DeformMap_AtlasSlice > 0).ToString();
		array[640] = ",\t";
		array[641] = fingerprint._DeformMap_AtlasSlice.ToString();
		array[642] = "\n_WeatherMap_Atlas,\t";
		array[643] = (_WeatherMap_Atlas > 0).ToString();
		array[644] = ",\t";
		array[645] = fingerprint._WeatherMap_Atlas;
		array[646] = "\n_WeatherMap_AtlasSlice,\t";
		array[647] = (_WeatherMap_AtlasSlice > 0).ToString();
		array[648] = ",\t";
		array[649] = fingerprint._WeatherMap_AtlasSlice.ToString();
		array[650] = "\n_DEBUG_PAWN_DATA,\t";
		array[651] = (_DEBUG_PAWN_DATA > 0).ToString();
		array[652] = ",\t";
		array[653] = fingerprint._DEBUG_PAWN_DATA.ToString();
		array[654] = "\n_SrcBlend,\t";
		array[655] = (_SrcBlend > 0).ToString();
		array[656] = ",\t";
		array[657] = fingerprint._SrcBlend.ToString();
		array[658] = "\n_DstBlend,\t";
		array[659] = (_DstBlend > 0).ToString();
		array[660] = ",\t";
		array[661] = fingerprint._DstBlend.ToString();
		array[662] = "\n_SrcBlendAlpha,\t";
		array[663] = (_SrcBlendAlpha > 0).ToString();
		array[664] = ",\t";
		array[665] = fingerprint._SrcBlendAlpha.ToString();
		array[666] = "\n_DstBlendAlpha,\t";
		array[667] = (_DstBlendAlpha > 0).ToString();
		array[668] = ",\t";
		array[669] = fingerprint._DstBlendAlpha.ToString();
		array[670] = "\n_ZWrite,\t";
		array[671] = (_ZWrite > 0).ToString();
		array[672] = ",\t";
		array[673] = fingerprint._ZWrite.ToString();
		array[674] = "\n_AlphaToMask,\t";
		array[675] = (_AlphaToMask > 0).ToString();
		array[676] = ",\t";
		array[677] = fingerprint._AlphaToMask.ToString();
		array[678] = "\n_Color,\t";
		array[679] = (_Color > 0).ToString();
		array[680] = ",\t";
		baseColor = fingerprint._Color;
		array[681] = baseColor.ToString();
		array[682] = "\n_Surface,\t";
		array[683] = (_Surface > 0).ToString();
		array[684] = ",\t";
		array[685] = fingerprint._Surface.ToString();
		array[686] = "\n_Metallic,\t";
		array[687] = (_Metallic > 0).ToString();
		array[688] = ",\t";
		array[689] = fingerprint._Metallic.ToString();
		array[690] = "\n_SpecColor,\t";
		array[691] = (_SpecColor > 0).ToString();
		array[692] = ",\t";
		baseColor = fingerprint._SpecColor;
		array[693] = baseColor.ToString();
		array[694] = "\n_DayNightLightmapArray,\t";
		array[695] = (_DayNightLightmapArray > 0).ToString();
		array[696] = ",\t";
		array[697] = fingerprint._DayNightLightmapArray;
		array[698] = "\n_DayNightLightmapArray_ST,\t";
		array[699] = (_DayNightLightmapArray_ST > 0).ToString();
		array[700] = ",\t";
		baseColor = fingerprint._DayNightLightmapArray_ST;
		array[701] = baseColor.ToString();
		array[702] = "\n_DayNightLightmapArray_AtlasSlice,\t";
		array[703] = (_DayNightLightmapArray_AtlasSlice > 0).ToString();
		array[704] = ",\t";
		array[705] = fingerprint._DayNightLightmapArray_AtlasSlice.ToString();
		array[706] = "\n";
		return string.Concat(array);
	}

	public static void _g_Macro_TRANSFORM_TEX(in GTUberShader_MaterialKeywordStates kw, ref int tex, ref int tex_ST)
	{
		tex++;
		tex_ST++;
	}

	private static void _g_Macro_DECLARE_ATLASABLE_TEX2D(in GTUberShader_MaterialKeywordStates kw, ref int tex, ref int tex_Atlas)
	{
		tex += ((!kw._USE_TEX_ARRAY_ATLAS) ? 1 : 0);
		tex_Atlas += (kw._USE_TEX_ARRAY_ATLAS ? 1 : 0);
	}

	private static void _g_Macro_DECLARE_ATLASABLE_SAMPLER(in GTUberShader_MaterialKeywordStates kw, ref int sampler, ref int sampler_Atlas)
	{
		sampler += ((!kw._USE_TEX_ARRAY_ATLAS) ? 1 : 0);
		sampler_Atlas += (kw._USE_TEX_ARRAY_ATLAS ? 1 : 0);
	}

	private static void _g_Macro_SAMPLE_ATLASABLE_TEX2D(in GTUberShader_MaterialKeywordStates kw, ref int tex, ref int tex_Atlas, ref int tex_AtlasSlice, ref int sampler, ref int sampler_Atlas, ref int coord2, ref int mipBias)
	{
		tex += ((!kw._USE_TEX_ARRAY_ATLAS) ? 1 : 0);
		tex_Atlas += (kw._USE_TEX_ARRAY_ATLAS ? 1 : 0);
		tex_AtlasSlice += (kw._USE_TEX_ARRAY_ATLAS ? 1 : 0);
		sampler += ((!kw._USE_TEX_ARRAY_ATLAS) ? 1 : 0);
		sampler_Atlas += (kw._USE_TEX_ARRAY_ATLAS ? 1 : 0);
		mipBias++;
		coord2++;
	}

	private static void _g_Macro_SAMPLE_ATLASABLE_TEX2D_LOD(in GTUberShader_MaterialKeywordStates kw, ref int texName, ref int texName_Atlas)
	{
		texName += ((!kw._USE_TEX_ARRAY_ATLAS) ? 1 : 0);
		texName_Atlas += (kw._USE_TEX_ARRAY_ATLAS ? 1 : 0);
	}

	private static void _g_Macro_SAMPLE_ATLASABLE_TEX2D_LOD(in GTUberShader_MaterialKeywordStates kw, ref int texName, ref int texName_Atlas, ref int sampler, ref int coord2, ref int lod)
	{
		texName += ((!kw._USE_TEX_ARRAY_ATLAS) ? 1 : 0);
		texName_Atlas += (kw._USE_TEX_ARRAY_ATLAS ? 1 : 0);
		sampler++;
		coord2++;
		lod++;
	}
}
