using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VoiceLoudnessReactorRendererColorTarget
{
	[SerializeField]
	private string colorProperty = "_BaseColor";

	public Renderer renderer;

	public int materialIndex;

	public Gradient gradient;

	public bool useSmoothedLoudness;

	public float scale = 1f;

	private List<Material> _materials;

	private Color _lastColor = Color.white;

	public void Inititialize()
	{
		if (_materials == null)
		{
			_materials = new List<Material>(renderer.materials);
			_materials[materialIndex].EnableKeyword(colorProperty);
			renderer.SetMaterials(_materials);
			UpdateMaterialColor(0f);
		}
	}

	public void UpdateMaterialColor(float level)
	{
		Color color = gradient.Evaluate(level);
		if (!(_lastColor == color))
		{
			_materials[materialIndex].SetColor(colorProperty, color);
			_lastColor = color;
		}
	}
}
