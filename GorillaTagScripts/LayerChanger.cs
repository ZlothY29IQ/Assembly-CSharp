using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace GorillaTagScripts;

public class LayerChanger : MonoBehaviour
{
	public LayerMask restrictedLayers;

	public bool includeChildren = true;

	private Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();

	private bool layersStored;

	public void InitializeLayers(Transform parent)
	{
		if (!layersStored)
		{
			StoreOriginalLayers(parent);
			layersStored = true;
		}
	}

	private void StoreOriginalLayers(Transform parent)
	{
		if (!includeChildren)
		{
			StoreOriginalLayers(parent);
			return;
		}
		foreach (Transform item in parent)
		{
			originalLayers[item] = item.gameObject.layer;
			StoreOriginalLayers(item);
		}
	}

	public void ChangeLayer(Transform parent, string newLayer)
	{
		if (!layersStored)
		{
			Debug.LogWarning("Layers have not been initialized. Call InitializeLayers first.");
		}
		else
		{
			ChangeLayers(parent, LayerMask.NameToLayer(newLayer));
		}
	}

	private void ChangeLayers(Transform parent, int newLayer)
	{
		if (!includeChildren)
		{
			if (!restrictedLayers.Contains(parent.gameObject.layer))
			{
				parent.gameObject.layer = newLayer;
			}
			return;
		}
		foreach (Transform item in parent)
		{
			if (!restrictedLayers.Contains(item.gameObject.layer))
			{
				item.gameObject.layer = newLayer;
				ChangeLayers(item, newLayer);
			}
		}
	}

	public void RestoreOriginalLayers()
	{
		if (!layersStored)
		{
			Debug.LogWarning("Layers have not been initialized. Call InitializeLayers first.");
			return;
		}
		foreach (KeyValuePair<Transform, int> originalLayer in originalLayers)
		{
			originalLayer.Key.gameObject.layer = originalLayer.Value;
		}
	}
}
