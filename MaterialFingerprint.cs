using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

[StructLayout(LayoutKind.Auto)]
public struct MaterialFingerprint
{
	public GTShaderTransparencyMode _TransparencyMode;

	public int _Cutoff;

	public int _ColorSource;

	public int4 _BaseColor;

	public int4 _GChannelColor;

	public int4 _BChannelColor;

	public int4 _AChannelColor;

	public string _BaseMap;

	public int4 _BaseMap_ST;

	public int _SettingsPreset;

	public int _AdvancedOptions;

	public int _TexMipBias;

	public int4 _BaseMap_WH;

	public int _TexelSnapToggle;

	public int _TexelSnap_Factor;

	public int _UVSource;

	public int _AlphaDetailToggle;

	public int4 _AlphaDetail_ST;

	public int _AlphaDetail_Opacity;

	public int _AlphaDetail_WorldSpace;

	public int _MaskMapToggle;

	public string _MaskMap;

	public int4 _MaskMap_ST;

	public int4 _MaskMap_WH;

	public int _LavaLampToggle;

	public int _GradientMapToggle;

	public string _GradientMap;

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

	public int4 _ReflectBoxCubePos;

	public int4 _ReflectBoxSize;

	public int4 _ReflectBoxRotation;

	public int _ReflectMatcapToggle;

	public int _ReflectMatcapPerspToggle;

	public int _ReflectNormalToggle;

	public string _ReflectTex;

	public string _ReflectNormalTex;

	public int _ReflectAlbedoTint;

	public int4 _ReflectTint;

	public int _ReflectOpacity;

	public int _ReflectExposure;

	public int4 _ReflectOffset;

	public int4 _ReflectScale;

	public int _ReflectRotate;

	public int _HalfLambertToggle;

	public int _ParallaxPlanarToggle;

	public int _ParallaxToggle;

	public int _ParallaxAAToggle;

	public int _ParallaxAABias;

	public string _DepthMap;

	public int _ParallaxAmplitude;

	public int4 _ParallaxSamplesMinMax;

	public int _UvShiftToggle;

	public int4 _UvShiftSteps;

	public int4 _UvShiftRate;

	public int4 _UvShiftOffset;

	public int _UseGridEffect;

	public int _UseCrystalEffect;

	public int _CrystalPower;

	public int4 _CrystalRimColor;

	public int _LiquidVolume;

	public int _LiquidFill;

	public int4 _LiquidFillNormal;

	public int4 _LiquidSurfaceColor;

	public int _LiquidSwayX;

	public int _LiquidSwayY;

	public int _LiquidContainer;

	public int4 _LiquidPlanePosition;

	public int4 _LiquidPlaneNormal;

	public int _VertexFlapToggle;

	public int4 _VertexFlapAxis;

	public int4 _VertexFlapDegreesMinMax;

	public int _VertexFlapSpeed;

	public int _VertexFlapPhaseOffset;

	public int _VertexWaveToggle;

	public int _VertexWaveDebug;

	public int4 _VertexWaveEnd;

	public int4 _VertexWaveParams;

	public int4 _VertexWaveFalloff;

	public int4 _VertexWaveSphereMask;

	public int _VertexWavePhaseOffset;

	public int4 _VertexWaveAxes;

	public int _VertexRotateToggle;

	public int4 _VertexRotateAngles;

	public int _VertexRotateAnim;

	public int _VertexLightToggle;

	public int _InnerGlowOn;

	public int4 _InnerGlowColor;

	public int4 _InnerGlowParams;

	public int _InnerGlowTap;

	public int _InnerGlowSine;

	public int _InnerGlowSinePeriod;

	public int _InnerGlowSinePhaseShift;

	public int _StealthEffectOn;

	public int _UseEyeTracking;

	public int4 _EyeTileOffsetUV;

	public int _EyeOverrideUV;

	public int4 _EyeOverrideUVTransform;

	public int _UseMouthFlap;

	public string _MouthMap;

	public int4 _MouthMap_ST;

	public int _UseVertexColor;

	public int _WaterEffect;

	public int _HeightBasedWaterEffect;

	public int _WaterCaustics;

	public int _UseDayNightLightmap;

	public int _DAY_CYCLE_BRIGHTNESS_;

	public int _UseWeatherMap;

	public string _WeatherMap;

	public int _WeatherMapDissolveEdgeSize;

	public int _UseSpecular;

	public int _UseSpecularAlphaChannel;

	public int _Smoothness;

	public int _UseSpecHighlight;

	public int4 _SpecularDir;

	public int4 _SpecularPowerIntensity;

	public int4 _SpecularColor;

	public int _SpecularUseDiffuseColor;

	public int _EmissionToggle;

	public int4 _EmissionColor;

	public string _EmissionMap;

	public int _EmissionMaskByBaseMapAlpha;

	public int4 _EmissionUVScrollSpeed;

	public int _EmissionDissolveProgress;

	public int4 _EmissionDissolveAnimation;

	public int _EmissionDissolveEdgeSize;

	public int _EmissionIntensityInDynamic;

	public int _EmissionUseUVWaveWarp;

	public int _GreyZoneException;

	public int _Cull;

	public int _StencilReference;

	public int _StencilComparison;

	public int _StencilPassFront;

	public int _USE_DEFORM_MAP;

	public string _DeformMap;

	public int _DeformMapIntensity;

	public int _DeformMapMaskByVertColorRAmount;

	public int4 _DeformMapScrollSpeed;

	public int4 _DeformMapUV0Influence;

	public int4 _DeformMapObjectSpaceOffsetsU;

	public int4 _DeformMapObjectSpaceOffsetsV;

	public int4 _DeformMapWorldSpaceOffsetsU;

	public int4 _DeformMapWorldSpaceOffsetsV;

	public int4 _RotateOnYAxisBySinTime;

	public int _USE_TEX_ARRAY_ATLAS;

	public string _BaseMap_Atlas;

	public int _BaseMap_AtlasSlice;

	public int _BaseMap_AtlasSliceSource;

	public string _EmissionMap_Atlas;

	public int _EmissionMap_AtlasSlice;

	public string _DeformMap_Atlas;

	public int _DeformMap_AtlasSlice;

	public string _WeatherMap_Atlas;

	public int _WeatherMap_AtlasSlice;

	public int _DEBUG_PAWN_DATA;

	public int _SrcBlend;

	public int _DstBlend;

	public int _SrcBlendAlpha;

	public int _DstBlendAlpha;

	public int _ZWrite;

	public int _AlphaToMask;

	public int4 _Color;

	public int _Surface;

	public int _Metallic;

	public int4 _SpecColor;

	public string _DayNightLightmapArray;

	public int4 _DayNightLightmapArray_ST;

	public int _DayNightLightmapArray_AtlasSlice;

	private const bool _k_UNITY_2023_1_OR_NEWER = true;

	public bool isValid;

	public MaterialFingerprint(UberShaderMatUsedProps used)
	{
		Material material = used.material;
		_TransparencyMode = GetMatTransparencyMode(material);
		_Cutoff = _Round(material.GetFloat(ShaderProps._Cutoff), 100, used._Cutoff);
		_ColorSource = ((used._ColorSource > 0) ? material.GetInt(ShaderProps._ColorSource) : 0);
		_BaseColor = _Round(material.GetColor(ShaderProps._BaseColor), 100, used._BaseColor);
		_GChannelColor = _Round(material.GetColor(ShaderProps._GChannelColor), 100, used._GChannelColor);
		_BChannelColor = _Round(material.GetColor(ShaderProps._BChannelColor), 100, used._BChannelColor);
		_AChannelColor = _Round(material.GetColor(ShaderProps._AChannelColor), 100, used._AChannelColor);
		_BaseMap = _GetTexPropGuid(material, ShaderProps._BaseMap, used._BaseMap);
		_BaseMap_ST = _Round(material.GetVector(ShaderProps._BaseMap_ST), 100, used._BaseMap_ST);
		_SettingsPreset = ((used._SettingsPreset > 0) ? material.GetInt(ShaderProps._SettingsPreset) : 0);
		_AdvancedOptions = _Round(material.GetFloat(ShaderProps._AdvancedOptions), 100, used._AdvancedOptions);
		_TexMipBias = _Round(material.GetFloat(ShaderProps._TexMipBias), 100, used._TexMipBias);
		_BaseMap_WH = _Round(material.GetVector(ShaderProps._BaseMap_WH), 100, used._BaseMap_WH);
		_TexelSnapToggle = _Round(material.GetFloat(ShaderProps._TexelSnapToggle), 100, used._TexelSnapToggle);
		_TexelSnap_Factor = _Round(material.GetFloat(ShaderProps._TexelSnap_Factor), 100, used._TexelSnap_Factor);
		_UVSource = ((used._UVSource > 0) ? material.GetInt(ShaderProps._UVSource) : 0);
		_AlphaDetailToggle = _Round(material.GetFloat(ShaderProps._AlphaDetailToggle), 100, used._AlphaDetailToggle);
		_AlphaDetail_ST = _Round(material.GetVector(ShaderProps._AlphaDetail_ST), 100, used._AlphaDetail_ST);
		_AlphaDetail_Opacity = _Round(material.GetFloat(ShaderProps._AlphaDetail_Opacity), 100, used._AlphaDetail_Opacity);
		_AlphaDetail_WorldSpace = _Round(material.GetFloat(ShaderProps._AlphaDetail_WorldSpace), 100, used._AlphaDetail_WorldSpace);
		_MaskMapToggle = _Round(material.GetFloat(ShaderProps._MaskMapToggle), 100, used._MaskMapToggle);
		_MaskMap = _GetTexPropGuid(material, ShaderProps._MaskMap, used._MaskMap);
		_MaskMap_ST = _Round(material.GetVector(ShaderProps._MaskMap_ST), 100, used._MaskMap_ST);
		_MaskMap_WH = _Round(material.GetVector(ShaderProps._MaskMap_WH), 100, used._MaskMap_WH);
		_LavaLampToggle = _Round(material.GetFloat(ShaderProps._LavaLampToggle), 100, used._LavaLampToggle);
		_GradientMapToggle = _Round(material.GetFloat(ShaderProps._GradientMapToggle), 100, used._GradientMapToggle);
		_GradientMap = _GetTexPropGuid(material, ShaderProps._GradientMap, used._GradientMap);
		_DoTextureRotation = _Round(material.GetFloat(ShaderProps._DoTextureRotation), 100, used._DoTextureRotation);
		_RotateAngle = _Round(material.GetFloat(ShaderProps._RotateAngle), 100, used._RotateAngle);
		_RotateAnim = _Round(material.GetFloat(ShaderProps._RotateAnim), 100, used._RotateAnim);
		_UseWaveWarp = _Round(material.GetFloat(ShaderProps._UseWaveWarp), 100, used._UseWaveWarp);
		_WaveAmplitude = _Round(material.GetFloat(ShaderProps._WaveAmplitude), 100, used._WaveAmplitude);
		_WaveFrequency = _Round(material.GetFloat(ShaderProps._WaveFrequency), 100, used._WaveFrequency);
		_WaveScale = _Round(material.GetFloat(ShaderProps._WaveScale), 100, used._WaveScale);
		_WaveTimeScale = _Round(material.GetFloat(ShaderProps._WaveTimeScale), 100, used._WaveTimeScale);
		_ReflectToggle = _Round(material.GetFloat(ShaderProps._ReflectToggle), 100, used._ReflectToggle);
		_ReflectBoxProjectToggle = _Round(material.GetFloat(ShaderProps._ReflectBoxProjectToggle), 100, used._ReflectBoxProjectToggle);
		_ReflectBoxCubePos = _Round(material.GetVector(ShaderProps._ReflectBoxCubePos), 100, used._ReflectBoxCubePos);
		_ReflectBoxSize = _Round(material.GetVector(ShaderProps._ReflectBoxSize), 100, used._ReflectBoxSize);
		_ReflectBoxRotation = _Round(material.GetVector(ShaderProps._ReflectBoxRotation), 100, used._ReflectBoxRotation);
		_ReflectMatcapToggle = _Round(material.GetFloat(ShaderProps._ReflectMatcapToggle), 100, used._ReflectMatcapToggle);
		_ReflectMatcapPerspToggle = _Round(material.GetFloat(ShaderProps._ReflectMatcapPerspToggle), 100, used._ReflectMatcapPerspToggle);
		_ReflectNormalToggle = _Round(material.GetFloat(ShaderProps._ReflectNormalToggle), 100, used._ReflectNormalToggle);
		_ReflectTex = _GetTexPropGuid(material, ShaderProps._ReflectTex, used._ReflectTex);
		_ReflectNormalTex = _GetTexPropGuid(material, ShaderProps._ReflectNormalTex, used._ReflectNormalTex);
		_ReflectAlbedoTint = _Round(material.GetFloat(ShaderProps._ReflectAlbedoTint), 100, used._ReflectAlbedoTint);
		_ReflectTint = _Round(material.GetColor(ShaderProps._ReflectTint), 100, used._ReflectTint);
		_ReflectOpacity = _Round(material.GetFloat(ShaderProps._ReflectOpacity), 100, used._ReflectOpacity);
		_ReflectExposure = _Round(material.GetFloat(ShaderProps._ReflectExposure), 100, used._ReflectExposure);
		_ReflectOffset = _Round(material.GetVector(ShaderProps._ReflectOffset), 100, used._ReflectOffset);
		_ReflectScale = _Round(material.GetVector(ShaderProps._ReflectScale), 100, used._ReflectScale);
		_ReflectRotate = _Round(material.GetFloat(ShaderProps._ReflectRotate), 100, used._ReflectRotate);
		_HalfLambertToggle = _Round(material.GetFloat(ShaderProps._HalfLambertToggle), 100, used._HalfLambertToggle);
		_ParallaxPlanarToggle = _Round(material.GetFloat(ShaderProps._ParallaxPlanarToggle), 100, used._ParallaxPlanarToggle);
		_ParallaxToggle = _Round(material.GetFloat(ShaderProps._ParallaxToggle), 100, used._ParallaxToggle);
		_ParallaxAAToggle = _Round(material.GetFloat(ShaderProps._ParallaxAAToggle), 100, used._ParallaxAAToggle);
		_ParallaxAABias = _Round(material.GetFloat(ShaderProps._ParallaxAABias), 100, used._ParallaxAABias);
		_DepthMap = _GetTexPropGuid(material, ShaderProps._DepthMap, used._DepthMap);
		_ParallaxAmplitude = _Round(material.GetFloat(ShaderProps._ParallaxAmplitude), 100, used._ParallaxAmplitude);
		_ParallaxSamplesMinMax = _Round(material.GetVector(ShaderProps._ParallaxSamplesMinMax), 100, used._ParallaxSamplesMinMax);
		_UvShiftToggle = _Round(material.GetFloat(ShaderProps._UvShiftToggle), 100, used._UvShiftToggle);
		_UvShiftSteps = _Round(material.GetVector(ShaderProps._UvShiftSteps), 100, used._UvShiftSteps);
		_UvShiftRate = _Round(material.GetVector(ShaderProps._UvShiftRate), 100, used._UvShiftRate);
		_UvShiftOffset = _Round(material.GetVector(ShaderProps._UvShiftOffset), 100, used._UvShiftOffset);
		_UseGridEffect = _Round(material.GetFloat(ShaderProps._UseGridEffect), 100, used._UseGridEffect);
		_UseCrystalEffect = _Round(material.GetFloat(ShaderProps._UseCrystalEffect), 100, used._UseCrystalEffect);
		_CrystalPower = _Round(material.GetFloat(ShaderProps._CrystalPower), 100, used._CrystalPower);
		_CrystalRimColor = _Round(material.GetColor(ShaderProps._CrystalRimColor), 100, used._CrystalRimColor);
		_LiquidVolume = _Round(material.GetFloat(ShaderProps._LiquidVolume), 100, used._LiquidVolume);
		_LiquidFill = _Round(material.GetFloat(ShaderProps._LiquidFill), 100, used._LiquidFill);
		_LiquidFillNormal = _Round(material.GetVector(ShaderProps._LiquidFillNormal), 100, used._LiquidFillNormal);
		_LiquidSurfaceColor = _Round(material.GetColor(ShaderProps._LiquidSurfaceColor), 100, used._LiquidSurfaceColor);
		_LiquidSwayX = _Round(material.GetFloat(ShaderProps._LiquidSwayX), 100, used._LiquidSwayX);
		_LiquidSwayY = _Round(material.GetFloat(ShaderProps._LiquidSwayY), 100, used._LiquidSwayY);
		_LiquidContainer = _Round(material.GetFloat(ShaderProps._LiquidContainer), 100, used._LiquidContainer);
		_LiquidPlanePosition = _Round(material.GetVector(ShaderProps._LiquidPlanePosition), 100, used._LiquidPlanePosition);
		_LiquidPlaneNormal = _Round(material.GetVector(ShaderProps._LiquidPlaneNormal), 100, used._LiquidPlaneNormal);
		_VertexFlapToggle = _Round(material.GetFloat(ShaderProps._VertexFlapToggle), 100, used._VertexFlapToggle);
		_VertexFlapAxis = _Round(material.GetVector(ShaderProps._VertexFlapAxis), 100, used._VertexFlapAxis);
		_VertexFlapDegreesMinMax = _Round(material.GetVector(ShaderProps._VertexFlapDegreesMinMax), 100, used._VertexFlapDegreesMinMax);
		_VertexFlapSpeed = _Round(material.GetFloat(ShaderProps._VertexFlapSpeed), 100, used._VertexFlapSpeed);
		_VertexFlapPhaseOffset = _Round(material.GetFloat(ShaderProps._VertexFlapPhaseOffset), 100, used._VertexFlapPhaseOffset);
		_VertexWaveToggle = _Round(material.GetFloat(ShaderProps._VertexWaveToggle), 100, used._VertexWaveToggle);
		_VertexWaveDebug = _Round(material.GetFloat(ShaderProps._VertexWaveDebug), 100, used._VertexWaveDebug);
		_VertexWaveEnd = _Round(material.GetVector(ShaderProps._VertexWaveEnd), 100, used._VertexWaveEnd);
		_VertexWaveParams = _Round(material.GetVector(ShaderProps._VertexWaveParams), 100, used._VertexWaveParams);
		_VertexWaveFalloff = _Round(material.GetVector(ShaderProps._VertexWaveFalloff), 100, used._VertexWaveFalloff);
		_VertexWaveSphereMask = _Round(material.GetVector(ShaderProps._VertexWaveSphereMask), 100, used._VertexWaveSphereMask);
		_VertexWavePhaseOffset = _Round(material.GetFloat(ShaderProps._VertexWavePhaseOffset), 100, used._VertexWavePhaseOffset);
		_VertexWaveAxes = _Round(material.GetVector(ShaderProps._VertexWaveAxes), 100, used._VertexWaveAxes);
		_VertexRotateToggle = _Round(material.GetFloat(ShaderProps._VertexRotateToggle), 100, used._VertexRotateToggle);
		_VertexRotateAngles = _Round(material.GetVector(ShaderProps._VertexRotateAngles), 100, used._VertexRotateAngles);
		_VertexRotateAnim = _Round(material.GetFloat(ShaderProps._VertexRotateAnim), 100, used._VertexRotateAnim);
		_VertexLightToggle = _Round(material.GetFloat(ShaderProps._VertexLightToggle), 100, used._VertexLightToggle);
		_InnerGlowOn = _Round(material.GetFloat(ShaderProps._InnerGlowOn), 100, used._InnerGlowOn);
		_InnerGlowColor = _Round(material.GetColor(ShaderProps._InnerGlowColor), 100, used._InnerGlowColor);
		_InnerGlowParams = _Round(material.GetVector(ShaderProps._InnerGlowParams), 100, used._InnerGlowParams);
		_InnerGlowTap = _Round(material.GetFloat(ShaderProps._InnerGlowTap), 100, used._InnerGlowTap);
		_InnerGlowSine = _Round(material.GetFloat(ShaderProps._InnerGlowSine), 100, used._InnerGlowSine);
		_InnerGlowSinePeriod = _Round(material.GetFloat(ShaderProps._InnerGlowSinePeriod), 100, used._InnerGlowSinePeriod);
		_InnerGlowSinePhaseShift = _Round(material.GetFloat(ShaderProps._InnerGlowSinePhaseShift), 100, used._InnerGlowSinePhaseShift);
		_StealthEffectOn = _Round(material.GetFloat(ShaderProps._StealthEffectOn), 100, used._StealthEffectOn);
		_UseEyeTracking = _Round(material.GetFloat(ShaderProps._UseEyeTracking), 100, used._UseEyeTracking);
		_EyeTileOffsetUV = _Round(material.GetVector(ShaderProps._EyeTileOffsetUV), 100, used._EyeTileOffsetUV);
		_EyeOverrideUV = _Round(material.GetFloat(ShaderProps._EyeOverrideUV), 100, used._EyeOverrideUV);
		_EyeOverrideUVTransform = _Round(material.GetVector(ShaderProps._EyeOverrideUVTransform), 100, used._EyeOverrideUVTransform);
		_UseMouthFlap = _Round(material.GetFloat(ShaderProps._UseMouthFlap), 100, used._UseMouthFlap);
		_MouthMap = _GetTexPropGuid(material, ShaderProps._MouthMap, used._MouthMap);
		_MouthMap_ST = _Round(material.GetVector(ShaderProps._MouthMap_ST), 100, used._MouthMap_ST);
		_UseVertexColor = _Round(material.GetFloat(ShaderProps._UseVertexColor), 100, used._UseVertexColor);
		_WaterEffect = _Round(material.GetFloat(ShaderProps._WaterEffect), 100, used._WaterEffect);
		_HeightBasedWaterEffect = _Round(material.GetFloat(ShaderProps._HeightBasedWaterEffect), 100, used._HeightBasedWaterEffect);
		_WaterCaustics = _Round(material.GetFloat(ShaderProps._WaterCaustics), 100, used._WaterCaustics);
		_UseDayNightLightmap = _Round(material.GetFloat(ShaderProps._UseDayNightLightmap), 100, used._UseDayNightLightmap);
		_DAY_CYCLE_BRIGHTNESS_ = _Round(material.GetFloat(ShaderProps._DAY_CYCLE_BRIGHTNESS_), 100, used._DAY_CYCLE_BRIGHTNESS_);
		_UseWeatherMap = _Round(material.GetFloat(ShaderProps._UseWeatherMap), 100, used._UseWeatherMap);
		_WeatherMap = _GetTexPropGuid(material, ShaderProps._WeatherMap, used._WeatherMap);
		_WeatherMapDissolveEdgeSize = _Round(material.GetFloat(ShaderProps._WeatherMapDissolveEdgeSize), 100, used._WeatherMapDissolveEdgeSize);
		_UseSpecular = _Round(material.GetFloat(ShaderProps._UseSpecular), 100, used._UseSpecular);
		_UseSpecularAlphaChannel = _Round(material.GetFloat(ShaderProps._UseSpecularAlphaChannel), 100, used._UseSpecularAlphaChannel);
		_Smoothness = _Round(material.GetFloat(ShaderProps._Smoothness), 100, used._Smoothness);
		_UseSpecHighlight = _Round(material.GetFloat(ShaderProps._UseSpecHighlight), 100, used._UseSpecHighlight);
		_SpecularDir = _Round(material.GetVector(ShaderProps._SpecularDir), 100, used._SpecularDir);
		_SpecularPowerIntensity = _Round(material.GetVector(ShaderProps._SpecularPowerIntensity), 100, used._SpecularPowerIntensity);
		_SpecularColor = _Round(material.GetColor(ShaderProps._SpecularColor), 100, used._SpecularColor);
		_SpecularUseDiffuseColor = _Round(material.GetFloat(ShaderProps._SpecularUseDiffuseColor), 100, used._SpecularUseDiffuseColor);
		_EmissionToggle = _Round(material.GetFloat(ShaderProps._EmissionToggle), 100, used._EmissionToggle);
		_EmissionColor = _Round(material.GetColor(ShaderProps._EmissionColor), 100, used._EmissionColor);
		_EmissionMap = _GetTexPropGuid(material, ShaderProps._EmissionMap, used._EmissionMap);
		_EmissionMaskByBaseMapAlpha = _Round(material.GetFloat(ShaderProps._EmissionMaskByBaseMapAlpha), 100, used._EmissionMaskByBaseMapAlpha);
		_EmissionUVScrollSpeed = _Round(material.GetVector(ShaderProps._EmissionUVScrollSpeed), 100, used._EmissionUVScrollSpeed);
		_EmissionDissolveProgress = _Round(material.GetFloat(ShaderProps._EmissionDissolveProgress), 100, used._EmissionDissolveProgress);
		_EmissionDissolveAnimation = _Round(material.GetVector(ShaderProps._EmissionDissolveAnimation), 100, used._EmissionDissolveAnimation);
		_EmissionDissolveEdgeSize = _Round(material.GetFloat(ShaderProps._EmissionDissolveEdgeSize), 100, used._EmissionDissolveEdgeSize);
		_EmissionIntensityInDynamic = _Round(material.GetFloat(ShaderProps._EmissionIntensityInDynamic), 100, used._EmissionIntensityInDynamic);
		_EmissionUseUVWaveWarp = _Round(material.GetFloat(ShaderProps._EmissionUseUVWaveWarp), 100, used._EmissionUseUVWaveWarp);
		_GreyZoneException = _Round(material.GetFloat(ShaderProps._GreyZoneException), 100, used._GreyZoneException);
		_Cull = _Round(material.GetFloat(ShaderProps._Cull), 100, used._Cull);
		_StencilReference = _Round(material.GetFloat(ShaderProps._StencilReference), 100, used._StencilReference);
		_StencilComparison = _Round(material.GetFloat(ShaderProps._StencilComparison), 100, used._StencilComparison);
		_StencilPassFront = _Round(material.GetFloat(ShaderProps._StencilPassFront), 100, used._StencilPassFront);
		_USE_DEFORM_MAP = _Round(material.GetFloat(ShaderProps._USE_DEFORM_MAP), 100, used._USE_DEFORM_MAP);
		_DeformMap = _GetTexPropGuid(material, ShaderProps._DeformMap, used._DeformMap);
		_DeformMapIntensity = _Round(material.GetFloat(ShaderProps._DeformMapIntensity), 100, used._DeformMapIntensity);
		_DeformMapMaskByVertColorRAmount = _Round(material.GetFloat(ShaderProps._DeformMapMaskByVertColorRAmount), 100, used._DeformMapMaskByVertColorRAmount);
		_DeformMapScrollSpeed = _Round(material.GetVector(ShaderProps._DeformMapScrollSpeed), 100, used._DeformMapScrollSpeed);
		_DeformMapUV0Influence = _Round(material.GetVector(ShaderProps._DeformMapUV0Influence), 100, used._DeformMapUV0Influence);
		_DeformMapObjectSpaceOffsetsU = _Round(material.GetVector(ShaderProps._DeformMapObjectSpaceOffsetsU), 100, used._DeformMapObjectSpaceOffsetsU);
		_DeformMapObjectSpaceOffsetsV = _Round(material.GetVector(ShaderProps._DeformMapObjectSpaceOffsetsV), 100, used._DeformMapObjectSpaceOffsetsV);
		_DeformMapWorldSpaceOffsetsU = _Round(material.GetVector(ShaderProps._DeformMapWorldSpaceOffsetsU), 100, used._DeformMapWorldSpaceOffsetsU);
		_DeformMapWorldSpaceOffsetsV = _Round(material.GetVector(ShaderProps._DeformMapWorldSpaceOffsetsV), 100, used._DeformMapWorldSpaceOffsetsV);
		_RotateOnYAxisBySinTime = _Round(material.GetVector(ShaderProps._RotateOnYAxisBySinTime), 100, used._RotateOnYAxisBySinTime);
		_USE_TEX_ARRAY_ATLAS = _Round(material.GetFloat(ShaderProps._USE_TEX_ARRAY_ATLAS), 100, used._USE_TEX_ARRAY_ATLAS);
		_BaseMap_Atlas = _GetTexPropGuid(material, ShaderProps._BaseMap_Atlas, used._BaseMap_Atlas);
		_BaseMap_AtlasSlice = _Round(material.GetFloat(ShaderProps._BaseMap_AtlasSlice), 100, used._BaseMap_AtlasSlice);
		_BaseMap_AtlasSliceSource = _Round(material.GetFloat(ShaderProps._BaseMap_AtlasSliceSource), 100, used._BaseMap_AtlasSliceSource);
		_EmissionMap_Atlas = _GetTexPropGuid(material, ShaderProps._EmissionMap_Atlas, used._EmissionMap_Atlas);
		_EmissionMap_AtlasSlice = _Round(material.GetFloat(ShaderProps._EmissionMap_AtlasSlice), 100, used._EmissionMap_AtlasSlice);
		_DeformMap_Atlas = _GetTexPropGuid(material, ShaderProps._DeformMap_Atlas, used._DeformMap_Atlas);
		_DeformMap_AtlasSlice = _Round(material.GetFloat(ShaderProps._DeformMap_AtlasSlice), 100, used._DeformMap_AtlasSlice);
		_WeatherMap_Atlas = _GetTexPropGuid(material, ShaderProps._WeatherMap_Atlas, used._WeatherMap_Atlas);
		_WeatherMap_AtlasSlice = _Round(material.GetFloat(ShaderProps._WeatherMap_AtlasSlice), 100, used._WeatherMap_AtlasSlice);
		_DEBUG_PAWN_DATA = _Round(material.GetFloat(ShaderProps._DEBUG_PAWN_DATA), 100, used._DEBUG_PAWN_DATA);
		_SrcBlend = _Round(material.GetFloat(ShaderProps._SrcBlend), 100, used._SrcBlend);
		_DstBlend = _Round(material.GetFloat(ShaderProps._DstBlend), 100, used._DstBlend);
		_SrcBlendAlpha = _Round(material.GetFloat(ShaderProps._SrcBlendAlpha), 100, used._SrcBlendAlpha);
		_DstBlendAlpha = _Round(material.GetFloat(ShaderProps._DstBlendAlpha), 100, used._DstBlendAlpha);
		_ZWrite = _Round(material.GetFloat(ShaderProps._ZWrite), 100, used._ZWrite);
		_AlphaToMask = _Round(material.GetFloat(ShaderProps._AlphaToMask), 100, used._AlphaToMask);
		_Color = _Round(material.GetColor(ShaderProps._Color), 100, used._Color);
		_Surface = _Round(material.GetFloat(ShaderProps._Surface), 100, used._Surface);
		_Metallic = _Round(material.GetFloat(ShaderProps._Metallic), 100, used._Metallic);
		_SpecColor = _Round(material.GetColor(ShaderProps._SpecColor), 100, used._SpecColor);
		_DayNightLightmapArray = _GetTexPropGuid(material, ShaderProps._DayNightLightmapArray, used._DayNightLightmapArray);
		_DayNightLightmapArray_ST = _Round(material.GetVector(ShaderProps._DayNightLightmapArray_ST), 100, used._DayNightLightmapArray_ST);
		_DayNightLightmapArray_AtlasSlice = _Round(material.GetFloat(ShaderProps._DayNightLightmapArray_AtlasSlice), 100, used._DayNightLightmapArray_AtlasSlice);
		isValid = true;
	}

	private static int4 _Round(Color c, int mul, int usedCount)
	{
		if (usedCount <= 0)
		{
			return int4.zero;
		}
		return new int4(Mathf.RoundToInt(c.r * (float)mul), Mathf.RoundToInt(c.g * (float)mul), Mathf.RoundToInt(c.b * (float)mul), Mathf.RoundToInt(c.a * (float)mul));
	}

	private static int4 _Round(Vector4 v, int mul, int usedCount)
	{
		if (usedCount <= 0)
		{
			return int4.zero;
		}
		return new int4(Mathf.RoundToInt(v.x * (float)mul), Mathf.RoundToInt(v.y * (float)mul), Mathf.RoundToInt(v.z * (float)mul), Mathf.RoundToInt(v.w * (float)mul));
	}

	private static int _Round(float f, int mul, int usedCount)
	{
		return Mathf.RoundToInt(f * (float)mul);
	}

	private static TexFormatInfo _GetTexFormatInfo(Material mat, string texPropName, int usedCount)
	{
		if (usedCount > 0)
		{
			Texture2D texture2D = mat.GetTexture(texPropName) as Texture2D;
			if (texture2D != null)
			{
				return new TexFormatInfo(texture2D);
			}
		}
		return default(TexFormatInfo);
	}

	private static string _GetTexPropGuid(Material mat, int texPropId, int usedCount)
	{
		_ = 0;
		return string.Empty;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GTShaderTransparencyMode GetMatTransparencyMode(Material mat)
	{
		return (GTShaderTransparencyMode)mat.GetInteger(ShaderProps._TransparencyMode);
	}

	public override string ToString()
	{
		string text = "";
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		FieldInfo[] fields = typeof(MaterialFingerprint).GetFields(bindingAttr);
		foreach (FieldInfo fieldInfo in fields)
		{
			text = text + "|" + fieldInfo.ToString() + ":" + fieldInfo.GetValue(this).ToString();
		}
		return text;
	}
}
