using System;
using UnityEngine;

[Serializable]
public struct ShaderGroup
{
	public Material material;

	public Shader originalShader;

	public Shader gameplayShader;

	public Shader bakingShader;

	public ShaderGroup(Material material, Shader original, Shader gameplay, Shader baking)
	{
		this.material = material;
		originalShader = original;
		gameplayShader = gameplay;
		bakingShader = baking;
	}
}
