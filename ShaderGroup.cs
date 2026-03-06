using System;
using UnityEngine;

[Serializable]
public struct ShaderGroup(Material material, Shader original, Shader gameplay, Shader baking)
{
	public Material material = material;

	public Shader originalShader = original;

	public Shader gameplayShader = gameplay;

	public Shader bakingShader = baking;
}
