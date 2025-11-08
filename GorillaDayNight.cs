using System.Collections;
using System.Threading;
using UnityEngine;

public class GorillaDayNight : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	public static volatile GorillaDayNight instance;

	public GorillaLightmapData[] lightmapDatas;

	private LightmapData[] workingLightMapDatas;

	private LightmapData workingLightMapData;

	public float lerpValue;

	public bool done;

	public bool finishedStep;

	private Color[] fromPixels;

	private Color[] toPixels;

	private Color[] mixedPixels;

	public int firstData;

	public int secondData;

	public int i;

	public int j;

	public int k;

	public int l;

	private Thread lightsThread;

	private Thread dirsThread;

	public bool test;

	public bool working;

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		test = false;
		working = false;
		lerpValue = 0.5f;
		workingLightMapDatas = new LightmapData[3];
		workingLightMapData = new LightmapData();
		workingLightMapData.lightmapColor = lightmapDatas[0].lightTextures[0];
		workingLightMapData.lightmapDir = lightmapDatas[0].dirTextures[0];
	}

	public void Update()
	{
		if (test)
		{
			test = false;
			StartCoroutine(LightMapSet(firstData, secondData, lerpValue));
		}
	}

	public void DoWork()
	{
		for (k = 0; k < lightmapDatas[firstData].lights.Length; k++)
		{
			fromPixels = lightmapDatas[firstData].lights[k];
			toPixels = lightmapDatas[secondData].lights[k];
			mixedPixels = fromPixels;
			for (j = 0; j < mixedPixels.Length; j++)
			{
				mixedPixels[j] = Color.Lerp(fromPixels[j], toPixels[j], lerpValue);
			}
			workingLightMapData.lightmapColor.SetPixels(mixedPixels);
			workingLightMapData.lightmapDir.Apply(updateMipmaps: false);
			fromPixels = lightmapDatas[firstData].dirs[k];
			toPixels = lightmapDatas[secondData].dirs[k];
			mixedPixels = fromPixels;
			for (j = 0; j < mixedPixels.Length; j++)
			{
				mixedPixels[j] = Color.Lerp(fromPixels[j], toPixels[j], lerpValue);
			}
			workingLightMapData.lightmapDir.SetPixels(mixedPixels);
			workingLightMapData.lightmapDir.Apply(updateMipmaps: false);
			workingLightMapDatas[k] = workingLightMapData;
		}
		done = true;
	}

	public void DoLightsStep()
	{
		fromPixels = lightmapDatas[firstData].lights[k];
		toPixels = lightmapDatas[secondData].lights[k];
		mixedPixels = fromPixels;
		for (j = 0; j < mixedPixels.Length; j++)
		{
			mixedPixels[j] = Color.Lerp(fromPixels[j], toPixels[j], lerpValue);
		}
		finishedStep = true;
	}

	public void DoDirsStep()
	{
		fromPixels = lightmapDatas[firstData].dirs[k];
		toPixels = lightmapDatas[secondData].dirs[k];
		mixedPixels = fromPixels;
		for (j = 0; j < mixedPixels.Length; j++)
		{
			mixedPixels[j] = Color.Lerp(fromPixels[j], toPixels[j], lerpValue);
		}
		finishedStep = true;
	}

	private IEnumerator LightMapSet(int setFirstData, int setSecondData, float setLerp)
	{
		working = true;
		firstData = setFirstData;
		secondData = setSecondData;
		lerpValue = setLerp;
		for (k = 0; k < lightmapDatas[firstData].lights.Length; k++)
		{
			lightsThread = new Thread(DoLightsStep);
			lightsThread.Start();
			yield return new WaitUntil(() => finishedStep);
			finishedStep = false;
			workingLightMapData.lightmapColor.SetPixels(mixedPixels);
			workingLightMapData.lightmapColor.Apply(updateMipmaps: false);
			dirsThread = new Thread(DoDirsStep);
			dirsThread.Start();
			yield return new WaitUntil(() => finishedStep);
			finishedStep = false;
			workingLightMapData.lightmapDir.SetPixels(mixedPixels);
			workingLightMapData.lightmapDir.Apply(updateMipmaps: false);
			workingLightMapDatas[k] = workingLightMapData;
		}
		LightmapSettings.lightmaps = workingLightMapDatas;
		working = false;
		done = true;
	}
}
