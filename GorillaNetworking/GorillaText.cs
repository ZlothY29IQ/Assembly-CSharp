using System;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaNetworking;

[Serializable]
public class GorillaText
{
	private string failureText;

	public string currentText;

	private string originalText = string.Empty;

	private StringBuilder stringBuilder = new StringBuilder();

	private bool modified;

	private bool failedState;

	private Material[] originalMaterials;

	private Material failureMaterial;

	internal Material[] currentMaterials;

	private UnityEvent<string> updateTextCallback;

	private UnityEvent<Material[]> updateMaterialCallback;

	public void Initialize(Material[] originalMaterials, Material failureMaterial, UnityEvent<string> callback = null, UnityEvent<Material[]> materialCallback = null)
	{
		this.failureMaterial = failureMaterial;
		this.originalMaterials = originalMaterials;
		currentMaterials = originalMaterials;
		Debug.Log("Original text = " + originalText);
		updateTextCallback = callback;
		updateMaterialCallback = materialCallback;
		GorillaTextManager.RegisterText(this);
	}

	public void InvokeIfUpdated()
	{
		if (modified)
		{
			modified = false;
			string text = stringBuilder.ToString();
			if (currentText != text)
			{
				currentText = text;
				updateTextCallback?.Invoke(currentText);
			}
		}
	}

	public void EnableFailedState(string failText)
	{
		failedState = true;
		failureText = failText;
		updateTextCallback?.Invoke(failText);
		originalText = currentText;
		currentText = failText;
		currentMaterials = (Material[])originalMaterials.Clone();
		currentMaterials[0] = failureMaterial;
		updateMaterialCallback?.Invoke(currentMaterials);
	}

	public void DisableFailedState()
	{
		failedState = false;
		updateTextCallback?.Invoke(originalText);
		failureText = "";
		currentText = originalText;
		currentMaterials = originalMaterials;
		updateMaterialCallback?.Invoke(currentMaterials);
	}

	public void Append(string str)
	{
		modified = true;
		stringBuilder.Append(str);
	}

	public void Set(string str)
	{
		modified = true;
		stringBuilder.Clear();
		stringBuilder.Append(str);
	}
}
