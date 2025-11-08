using System;
using UnityEngine;
using UnityEngine.Rendering;

public static class UberShader
{
	private static Shader kReferenceShader;

	private static Material kReferenceMaterial;

	private static Shader kReferenceShaderNonSRP;

	private static Material kReferenceMaterialNonSRP;

	private static UberShaderProperty[] kProperties;

	private static bool gInitialized = false;

	public static UberShaderProperty TransparencyMode = GetProperty(0);

	public static UberShaderProperty Cutoff = GetProperty(1);

	public static UberShaderProperty ColorSource = GetProperty(2);

	public static UberShaderProperty BaseColor = GetProperty(3);

	public static UberShaderProperty GChannelColor = GetProperty(4);

	public static UberShaderProperty BChannelColor = GetProperty(5);

	public static UberShaderProperty AChannelColor = GetProperty(6);

	public static UberShaderProperty BaseMap = GetProperty(7);

	public static UberShaderProperty BaseMap_WH = GetProperty(8);

	public static UberShaderProperty TexelSnapToggle = GetProperty(9);

	public static UberShaderProperty TexelSnap_Factor = GetProperty(10);

	public static UberShaderProperty UVSource = GetProperty(11);

	public static UberShaderProperty AlphaDetailToggle = GetProperty(12);

	public static UberShaderProperty AlphaDetail_ST = GetProperty(13);

	public static UberShaderProperty AlphaDetail_Opacity = GetProperty(14);

	public static UberShaderProperty AlphaDetail_WorldSpace = GetProperty(15);

	public static UberShaderProperty MaskMapToggle = GetProperty(16);

	public static UberShaderProperty MaskMap = GetProperty(17);

	public static UberShaderProperty MaskMap_WH = GetProperty(18);

	public static UberShaderProperty LavaLampToggle = GetProperty(19);

	public static UberShaderProperty GradientMapToggle = GetProperty(20);

	public static UberShaderProperty GradientMap = GetProperty(21);

	public static UberShaderProperty DoTextureRotation = GetProperty(22);

	public static UberShaderProperty RotateAngle = GetProperty(23);

	public static UberShaderProperty RotateAnim = GetProperty(24);

	public static UberShaderProperty UseWaveWarp = GetProperty(25);

	public static UberShaderProperty WaveAmplitude = GetProperty(26);

	public static UberShaderProperty WaveFrequency = GetProperty(27);

	public static UberShaderProperty WaveScale = GetProperty(28);

	public static UberShaderProperty WaveTimeScale = GetProperty(29);

	public static UberShaderProperty UseWeatherMap = GetProperty(30);

	public static UberShaderProperty WeatherMap = GetProperty(31);

	public static UberShaderProperty WeatherMapDissolveEdgeSize = GetProperty(32);

	public static UberShaderProperty ReflectToggle = GetProperty(33);

	public static UberShaderProperty ReflectBoxProjectToggle = GetProperty(34);

	public static UberShaderProperty ReflectBoxCubePos = GetProperty(35);

	public static UberShaderProperty ReflectBoxSize = GetProperty(36);

	public static UberShaderProperty ReflectBoxRotation = GetProperty(37);

	public static UberShaderProperty ReflectMatcapToggle = GetProperty(38);

	public static UberShaderProperty ReflectMatcapPerspToggle = GetProperty(39);

	public static UberShaderProperty ReflectNormalToggle = GetProperty(40);

	public static UberShaderProperty ReflectTex = GetProperty(41);

	public static UberShaderProperty ReflectNormalTex = GetProperty(42);

	public static UberShaderProperty ReflectAlbedoTint = GetProperty(43);

	public static UberShaderProperty ReflectTint = GetProperty(44);

	public static UberShaderProperty ReflectOpacity = GetProperty(45);

	public static UberShaderProperty ReflectExposure = GetProperty(46);

	public static UberShaderProperty ReflectOffset = GetProperty(47);

	public static UberShaderProperty ReflectScale = GetProperty(48);

	public static UberShaderProperty ReflectRotate = GetProperty(49);

	public static UberShaderProperty HalfLambertToggle = GetProperty(50);

	public static UberShaderProperty ZFightOffset = GetProperty(51);

	public static UberShaderProperty ParallaxPlanarToggle = GetProperty(52);

	public static UberShaderProperty ParallaxToggle = GetProperty(53);

	public static UberShaderProperty ParallaxAAToggle = GetProperty(54);

	public static UberShaderProperty ParallaxAABias = GetProperty(55);

	public static UberShaderProperty DepthMap = GetProperty(56);

	public static UberShaderProperty ParallaxAmplitude = GetProperty(57);

	public static UberShaderProperty ParallaxSamplesMinMax = GetProperty(58);

	public static UberShaderProperty UvShiftToggle = GetProperty(59);

	public static UberShaderProperty UvShiftSteps = GetProperty(60);

	public static UberShaderProperty UvShiftRate = GetProperty(61);

	public static UberShaderProperty UvShiftOffset = GetProperty(62);

	public static UberShaderProperty UseGridEffect = GetProperty(63);

	public static UberShaderProperty UseCrystalEffect = GetProperty(64);

	public static UberShaderProperty CrystalPower = GetProperty(65);

	public static UberShaderProperty CrystalRimColor = GetProperty(66);

	public static UberShaderProperty LiquidVolume = GetProperty(67);

	public static UberShaderProperty LiquidFill = GetProperty(68);

	public static UberShaderProperty LiquidFillNormal = GetProperty(69);

	public static UberShaderProperty LiquidSurfaceColor = GetProperty(70);

	public static UberShaderProperty LiquidSwayX = GetProperty(71);

	public static UberShaderProperty LiquidSwayY = GetProperty(72);

	public static UberShaderProperty LiquidContainer = GetProperty(73);

	public static UberShaderProperty LiquidPlanePosition = GetProperty(74);

	public static UberShaderProperty LiquidPlaneNormal = GetProperty(75);

	public static UberShaderProperty VertexFlapToggle = GetProperty(76);

	public static UberShaderProperty VertexFlapAxis = GetProperty(77);

	public static UberShaderProperty VertexFlapDegreesMinMax = GetProperty(78);

	public static UberShaderProperty VertexFlapSpeed = GetProperty(79);

	public static UberShaderProperty VertexFlapPhaseOffset = GetProperty(80);

	public static UberShaderProperty VertexWaveToggle = GetProperty(81);

	public static UberShaderProperty VertexWaveDebug = GetProperty(82);

	public static UberShaderProperty VertexWaveEnd = GetProperty(83);

	public static UberShaderProperty VertexWaveParams = GetProperty(84);

	public static UberShaderProperty VertexWaveFalloff = GetProperty(85);

	public static UberShaderProperty VertexWaveSphereMask = GetProperty(86);

	public static UberShaderProperty VertexWavePhaseOffset = GetProperty(87);

	public static UberShaderProperty VertexWaveAxes = GetProperty(88);

	public static UberShaderProperty VertexRotateToggle = GetProperty(89);

	public static UberShaderProperty VertexRotateAngles = GetProperty(90);

	public static UberShaderProperty VertexRotateAnim = GetProperty(91);

	public static UberShaderProperty VertexLightToggle = GetProperty(92);

	public static UberShaderProperty InnerGlowOn = GetProperty(93);

	public static UberShaderProperty InnerGlowColor = GetProperty(94);

	public static UberShaderProperty InnerGlowParams = GetProperty(95);

	public static UberShaderProperty InnerGlowTap = GetProperty(96);

	public static UberShaderProperty InnerGlowSine = GetProperty(97);

	public static UberShaderProperty InnerGlowSinePeriod = GetProperty(98);

	public static UberShaderProperty InnerGlowSinePhaseShift = GetProperty(99);

	public static UberShaderProperty StealthEffectOn = GetProperty(100);

	public static UberShaderProperty UseEyeTracking = GetProperty(101);

	public static UberShaderProperty EyeTileOffsetUV = GetProperty(102);

	public static UberShaderProperty EyeOverrideUV = GetProperty(103);

	public static UberShaderProperty EyeOverrideUVTransform = GetProperty(104);

	public static UberShaderProperty UseMouthFlap = GetProperty(105);

	public static UberShaderProperty MouthMap = GetProperty(106);

	public static UberShaderProperty MouthMap_Atlas = GetProperty(107);

	public static UberShaderProperty MouthMap_AtlasSlice = GetProperty(108);

	public static UberShaderProperty UseVertexColor = GetProperty(109);

	public static UberShaderProperty WaterEffect = GetProperty(110);

	public static UberShaderProperty HeightBasedWaterEffect = GetProperty(111);

	public static UberShaderProperty UseDayNightLightmap = GetProperty(112);

	public static UberShaderProperty UseSpecular = GetProperty(113);

	public static UberShaderProperty UseSpecularAlphaChannel = GetProperty(114);

	public static UberShaderProperty Smoothness = GetProperty(115);

	public static UberShaderProperty UseSpecHighlight = GetProperty(116);

	public static UberShaderProperty SpecularDir = GetProperty(117);

	public static UberShaderProperty SpecularPowerIntensity = GetProperty(118);

	public static UberShaderProperty SpecularColor = GetProperty(119);

	public static UberShaderProperty SpecularUseDiffuseColor = GetProperty(120);

	public static UberShaderProperty EmissionToggle = GetProperty(121);

	public static UberShaderProperty EmissionColor = GetProperty(122);

	public static UberShaderProperty EmissionMap = GetProperty(123);

	public static UberShaderProperty EmissionMaskByBaseMapAlpha = GetProperty(124);

	public static UberShaderProperty EmissionUVScrollSpeed = GetProperty(125);

	public static UberShaderProperty EmissionDissolveProgress = GetProperty(126);

	public static UberShaderProperty EmissionDissolveAnimation = GetProperty(127);

	public static UberShaderProperty EmissionDissolveEdgeSize = GetProperty(128);

	public static UberShaderProperty EmissionUseUVWaveWarp = GetProperty(129);

	public static UberShaderProperty GreyZoneException = GetProperty(130);

	public static UberShaderProperty Cull = GetProperty(131);

	public static UberShaderProperty StencilReference = GetProperty(132);

	public static UberShaderProperty StencilComparison = GetProperty(133);

	public static UberShaderProperty StencilPassFront = GetProperty(134);

	public static UberShaderProperty USE_DEFORM_MAP = GetProperty(135);

	public static UberShaderProperty DeformMap = GetProperty(136);

	public static UberShaderProperty DeformMapIntensity = GetProperty(137);

	public static UberShaderProperty DeformMapMaskByVertColorRAmount = GetProperty(138);

	public static UberShaderProperty DeformMapScrollSpeed = GetProperty(139);

	public static UberShaderProperty DeformMapUV0Influence = GetProperty(140);

	public static UberShaderProperty DeformMapObjectSpaceOffsetsU = GetProperty(141);

	public static UberShaderProperty DeformMapObjectSpaceOffsetsV = GetProperty(142);

	public static UberShaderProperty DeformMapWorldSpaceOffsetsU = GetProperty(143);

	public static UberShaderProperty DeformMapWorldSpaceOffsetsV = GetProperty(144);

	public static UberShaderProperty RotateOnYAxisBySinTime = GetProperty(145);

	public static UberShaderProperty USE_TEX_ARRAY_ATLAS = GetProperty(146);

	public static UberShaderProperty BaseMap_Atlas = GetProperty(147);

	public static UberShaderProperty BaseMap_AtlasSlice = GetProperty(148);

	public static UberShaderProperty EmissionMap_Atlas = GetProperty(149);

	public static UberShaderProperty EmissionMap_AtlasSlice = GetProperty(150);

	public static UberShaderProperty DeformMap_Atlas = GetProperty(151);

	public static UberShaderProperty DeformMap_AtlasSlice = GetProperty(152);

	public static UberShaderProperty DEBUG_PAWN_DATA = GetProperty(153);

	public static UberShaderProperty SrcBlend = GetProperty(154);

	public static UberShaderProperty DstBlend = GetProperty(155);

	public static UberShaderProperty SrcBlendAlpha = GetProperty(156);

	public static UberShaderProperty DstBlendAlpha = GetProperty(157);

	public static UberShaderProperty ZWrite = GetProperty(158);

	public static UberShaderProperty AlphaToMask = GetProperty(159);

	public static UberShaderProperty Color = GetProperty(160);

	public static UberShaderProperty Surface = GetProperty(161);

	public static UberShaderProperty Metallic = GetProperty(162);

	public static UberShaderProperty SpecColor = GetProperty(163);

	public static UberShaderProperty DayNightLightmapArray = GetProperty(164);

	public static UberShaderProperty DayNightLightmapArray_AtlasSlice = GetProperty(165);

	public static UberShaderProperty SingleLightmap = GetProperty(166);

	public static Material ReferenceMaterial
	{
		get
		{
			InitDependencies();
			return kReferenceMaterial;
		}
	}

	public static Shader ReferenceShader
	{
		get
		{
			InitDependencies();
			return kReferenceShader;
		}
	}

	public static Material ReferenceMaterialNonSRP
	{
		get
		{
			InitDependencies();
			return kReferenceMaterialNonSRP;
		}
	}

	public static Shader ReferenceShaderNonSRP
	{
		get
		{
			InitDependencies();
			return kReferenceShaderNonSRP;
		}
	}

	public static UberShaderProperty[] AllProperties
	{
		get
		{
			InitDependencies();
			return kProperties;
		}
	}

	public static bool IsAnimated(Material m)
	{
		if (m == null)
		{
			return false;
		}
		if (!((double)UvShiftToggle.GetValue<float>(m) > 0.5))
		{
			return false;
		}
		Vector2 value = UvShiftRate.GetValue<Vector2>(m);
		if (!(value.x > 0f))
		{
			return value.y > 0f;
		}
		return true;
	}

	private static UberShaderProperty GetProperty(int i)
	{
		InitDependencies();
		return kProperties[i];
	}

	private static UberShaderProperty GetProperty(int i, string expectedName)
	{
		InitDependencies();
		return kProperties[i];
	}

	private static void InitDependencies()
	{
		if (!gInitialized)
		{
			kReferenceShader = Shader.Find("GorillaTag/UberShader");
			kReferenceMaterial = new Material(kReferenceShader);
			kReferenceShaderNonSRP = Shader.Find("GorillaTag/UberShaderNonSRP");
			kReferenceMaterialNonSRP = new Material(kReferenceShaderNonSRP);
			kProperties = EnumerateAllProperties(kReferenceShader);
			gInitialized = true;
		}
	}

	public static Shader GetShader()
	{
		InitDependencies();
		return kReferenceShader;
	}

	private static UberShaderProperty[] EnumerateAllProperties(Shader uberShader)
	{
		int propertyCount = uberShader.GetPropertyCount();
		UberShaderProperty[] array = new UberShaderProperty[propertyCount];
		for (int i = 0; i < propertyCount; i++)
		{
			UberShaderProperty uberShaderProperty = new UberShaderProperty
			{
				index = i,
				flags = uberShader.GetPropertyFlags(i),
				type = uberShader.GetPropertyType(i),
				nameID = uberShader.GetPropertyNameId(i),
				name = uberShader.GetPropertyName(i),
				attributes = uberShader.GetPropertyAttributes(i)
			};
			if (uberShaderProperty.type == ShaderPropertyType.Range)
			{
				uberShaderProperty.rangeLimits = uberShader.GetPropertyRangeLimits(uberShaderProperty.index);
			}
			string[] attributes = uberShaderProperty.attributes;
			if (attributes != null && attributes.Length != 0)
			{
				foreach (string text in attributes)
				{
					if (!string.IsNullOrWhiteSpace(text) && (uberShaderProperty.isKeywordToggle = text.StartsWith("Toggle(")))
					{
						string keyword = text.Split('(', StringSplitOptions.RemoveEmptyEntries)[1].RemoveEnd(")", StringComparison.InvariantCulture);
						uberShaderProperty.keyword = keyword;
					}
				}
			}
			array[i] = uberShaderProperty;
		}
		return array;
	}
}
