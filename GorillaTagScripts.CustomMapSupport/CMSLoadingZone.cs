using System.Collections.Generic;
using GorillaLocomotion;
using GorillaTagScripts.VirtualStumpCustomMaps;
using GT_CustomMapSupportRuntime;
using UnityEngine;

namespace GorillaTagScripts.CustomMapSupport;

public class CMSLoadingZone : MonoBehaviour
{
	private int[] scenesToLoad;

	private int[] scenesToUnload;

	private bool useDynamicLighting;

	private Color dynamicLightingAmbientColor;

	private void Start()
	{
		base.gameObject.layer = UnityLayer.GorillaTrigger.ToLayerIndex();
	}

	public void SetupLoadingZone(LoadZoneSettings settings, in string[] assetBundleSceneFilePaths)
	{
		scenesToLoad = GetSceneIndexes(settings.scenesToLoad, in assetBundleSceneFilePaths);
		scenesToUnload = CleanSceneUnloadArray(settings.scenesToUnload, settings.scenesToLoad, in assetBundleSceneFilePaths);
		useDynamicLighting = settings.useDynamicLighting;
		dynamicLightingAmbientColor = settings.UberShaderAmbientDynamicLight;
		base.gameObject.layer = UnityLayer.GorillaBoundary.ToLayerIndex();
		Collider[] components = base.gameObject.GetComponents<Collider>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].isTrigger = true;
		}
	}

	private int[] GetSceneIndexes(List<string> sceneNames, in string[] assetBundleSceneFilePaths)
	{
		int[] array = new int[sceneNames.Count];
		for (int i = 0; i < sceneNames.Count; i++)
		{
			for (int j = 0; j < assetBundleSceneFilePaths.Length; j++)
			{
				if (string.Equals(sceneNames[i], GetSceneNameFromFilePath(assetBundleSceneFilePaths[j])))
				{
					array[i] = j;
					break;
				}
			}
		}
		return array;
	}

	private int[] CleanSceneUnloadArray(List<string> unload, List<string> load, in string[] assetBundleSceneFilePaths)
	{
		for (int i = 0; i < load.Count; i++)
		{
			if (unload.Contains(load[i]))
			{
				unload.Remove(load[i]);
			}
		}
		return GetSceneIndexes(unload, in assetBundleSceneFilePaths);
	}

	public void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.bodyCollider)
		{
			if (useDynamicLighting)
			{
				GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: true);
				GameLightingManager.instance.SetAmbientLightDynamic(dynamicLightingAmbientColor);
			}
			else
			{
				GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: false);
				GameLightingManager.instance.SetAmbientLightDynamic(Color.black);
			}
			CustomMapManager.LoadZoneTriggered(scenesToLoad, scenesToUnload);
		}
	}

	private string GetSceneNameFromFilePath(string filePath)
	{
		return filePath.Split("/")[^1].Split(".")[0];
	}
}
