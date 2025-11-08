using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class UberShaderProperty
{
	public int index;

	public int nameID;

	public string name;

	public ShaderPropertyType type;

	public ShaderPropertyFlags flags;

	public Vector2 rangeLimits;

	public string[] attributes;

	public bool isKeywordToggle;

	public string keyword;

	public T GetValue<T>(Material target)
	{
		switch (type)
		{
		case ShaderPropertyType.Color:
			return ValueAs<Color, T>(target.GetColor(nameID));
		case ShaderPropertyType.Vector:
			return ValueAs<Vector4, T>(target.GetVector(nameID));
		case ShaderPropertyType.Float:
		case ShaderPropertyType.Range:
			return ValueAs<float, T>(target.GetFloat(nameID));
		case ShaderPropertyType.Texture:
			return ValueAs<Texture, T>(target.GetTexture(nameID));
		case ShaderPropertyType.Int:
			return ValueAs<int, T>(target.GetInt(nameID));
		default:
			return default(T);
		}
	}

	public void SetValue<T>(Material target, T value)
	{
		switch (type)
		{
		case ShaderPropertyType.Color:
			target.SetColor(nameID, ValueAs<T, Color>(value));
			break;
		case ShaderPropertyType.Vector:
			target.SetVector(nameID, ValueAs<T, Vector4>(value));
			break;
		case ShaderPropertyType.Float:
		case ShaderPropertyType.Range:
			target.SetFloat(nameID, ValueAs<T, float>(value));
			break;
		case ShaderPropertyType.Texture:
			target.SetTexture(nameID, ValueAs<T, Texture>(value));
			break;
		case ShaderPropertyType.Int:
			target.SetInt(nameID, ValueAs<T, int>(value));
			break;
		}
		if (isKeywordToggle)
		{
			bool flag = false;
			switch (type)
			{
			case ShaderPropertyType.Int:
				flag = ValueAs<T, int>(value) >= 1;
				break;
			case ShaderPropertyType.Float:
				flag = ValueAs<T, float>(value) >= 0.5f;
				break;
			}
			if (flag)
			{
				target.EnableKeyword(keyword);
			}
			else
			{
				target.DisableKeyword(keyword);
			}
		}
	}

	public void Enable(Material target)
	{
		switch (type)
		{
		case ShaderPropertyType.Int:
			target.SetInt(nameID, 1);
			break;
		case ShaderPropertyType.Float:
			target.SetFloat(nameID, 1f);
			break;
		}
		if (isKeywordToggle)
		{
			target.EnableKeyword(keyword);
		}
	}

	public void Disable(Material target)
	{
		switch (type)
		{
		case ShaderPropertyType.Int:
			target.SetInt(nameID, 0);
			break;
		case ShaderPropertyType.Float:
			target.SetFloat(nameID, 0f);
			break;
		}
		if (isKeywordToggle)
		{
			target.DisableKeyword(keyword);
		}
	}

	public bool TryGetKeywordState(Material target, out bool enabled)
	{
		enabled = false;
		if (!isKeywordToggle)
		{
			return false;
		}
		enabled = target.IsKeywordEnabled(keyword);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static TOut ValueAs<TIn, TOut>(TIn value)
	{
		return Unsafe.As<TIn, TOut>(ref value);
	}
}
