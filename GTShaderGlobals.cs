using System;
using UnityEngine;

public class GTShaderGlobals : MonoBehaviour, IGorillaSliceableSimple
{
	private static Camera gMainCamera;

	private static Transform gMainCameraXform;

	private static Vector3 gMainCameraWorldPos;

	[Space]
	private static int gIFrame;

	private static float gTime;

	[Space]
	private static Texture2D gBlueNoiseTex;

	private static Vector4 gBlueNoiseTexWH;

	[Space]
	private static int gActivePawns;

	[Space]
	private static DateTime gStartTime = DateTime.Today.AddDays(-1.0).ToUniversalTime();

	private static Matrix4x4[] gPawnData = GorillaPawn.ShaderData;

	private static ShaderHashId _GT_WorldSpaceCameraPos = "_GT_WorldSpaceCameraPos";

	private static ShaderHashId _GT_BlueNoiseTex = "_GT_BlueNoiseTex";

	private static ShaderHashId _GT_BlueNoiseTex_WH = "_GT_BlueNoiseTex_WH";

	private static ShaderHashId _GT_iFrame = "_GT_iFrame";

	private static ShaderHashId _GT_Time = "_GT_Time";

	private static ShaderHashId _GT_PawnData = "_GT_PawnData";

	private static ShaderHashId _GT_PawnActiveCount = "_GT_PawnActiveCount";

	public static Vector3 WorldSpaceCameraPos => gMainCameraWorldPos;

	public static float Time => gTime;

	public static int Frame => gIFrame;

	private void Awake()
	{
		gMainCamera = Camera.main;
		if ((bool)gMainCamera)
		{
			gMainCameraXform = gMainCamera.transform;
			gMainCameraWorldPos = gMainCameraXform.position;
		}
		SliceUpdate();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize()
	{
		InitBlueNoiseTex();
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		UpdateTime();
		UpdateFrame();
		UpdateCamera();
	}

	private static void UpdateFrame()
	{
		gIFrame = UnityEngine.Time.frameCount;
		Shader.SetGlobalInteger(_GT_iFrame, gIFrame);
	}

	private static void UpdateCamera()
	{
		if ((bool)gMainCameraXform)
		{
			gMainCameraWorldPos = gMainCameraXform.position;
			Shader.SetGlobalVector(_GT_WorldSpaceCameraPos, gMainCameraWorldPos);
		}
	}

	private static void UpdateTime()
	{
		gTime = (float)(DateTime.UtcNow - gStartTime).TotalSeconds;
		Shader.SetGlobalFloat(_GT_Time, gTime);
	}

	private static void UpdatePawns()
	{
		gActivePawns = GorillaPawn.ActiveCount;
		GorillaPawn.SyncPawnData();
		Shader.SetGlobalMatrixArray(_GT_PawnData, gPawnData);
		Shader.SetGlobalInteger(_GT_PawnActiveCount, gActivePawns);
	}

	private static void InitBlueNoiseTex()
	{
		gBlueNoiseTex = Resources.Load<Texture2D>("Graphics/Textures/noise_blue_rgba_128");
		gBlueNoiseTexWH = gBlueNoiseTex.GetTexelSize();
		Shader.SetGlobalTexture(_GT_BlueNoiseTex, gBlueNoiseTex);
		Shader.SetGlobalVector(_GT_BlueNoiseTex_WH, gBlueNoiseTexWH);
	}
}
