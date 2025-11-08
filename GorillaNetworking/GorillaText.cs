using System;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaNetworking;

[Serializable]
public class GorillaText
{
	private string failureText;

	private string originalText = string.Empty;

	private bool failedState;

	private Material[] originalMaterials;

	private Material failureMaterial;

	internal Material[] currentMaterials;

	private UnityEvent<string> updateTextCallback;

	private UnityEvent<Material[]> updateMaterialCallback;

	public string Text
	{
		get
		{
			return originalText;
		}
		set
		{
			if (!(originalText == value))
			{
				originalText = value;
				if (!failedState)
				{
					updateTextCallback?.Invoke(value);
				}
			}
		}
	}

	public void Initialize(Material[] originalMaterials, Material failureMaterial, UnityEvent<string> callback = null, UnityEvent<Material[]> materialCallback = null)
	{
		this.failureMaterial = failureMaterial;
		this.originalMaterials = originalMaterials;
		currentMaterials = originalMaterials;
		Debug.Log("Original text = " + originalText);
		updateTextCallback = callback;
		updateMaterialCallback = materialCallback;
	}

	public void EnableFailedState(string failText)
	{
		failedState = true;
		failureText = failText;
		updateTextCallback?.Invoke(failText);
		currentMaterials = (Material[])originalMaterials.Clone();
		currentMaterials[0] = failureMaterial;
		updateMaterialCallback?.Invoke(currentMaterials);
	}

	public void DisableFailedState()
	{
		failedState = false;
		updateTextCallback?.Invoke(originalText);
		failureText = "";
		currentMaterials = originalMaterials;
		updateMaterialCallback?.Invoke(currentMaterials);
	}
}
