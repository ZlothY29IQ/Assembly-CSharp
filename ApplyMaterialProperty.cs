using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ApplyMaterialProperty : MonoBehaviour
{
	public enum ApplyMode
	{
		MaterialInstance,
		MaterialPropertyBlock
	}

	public enum SuportedTypes
	{
		Color,
		Float,
		Vector2,
		Vector3,
		Vector4,
		Texture2D
	}

	[Serializable]
	public class CustomMaterialData
	{
		public string name;

		public int id;

		public SuportedTypes dataType;

		public Color color;

		public float @float;

		public Vector2 vector2;

		public Vector3 vector3;

		public Vector4 vector4;

		public Texture2D texture2D;

		public CustomMaterialData(string propertyName)
		{
			name = propertyName;
			id = Shader.PropertyToID(propertyName);
			dataType = SuportedTypes.Color;
			color = default(Color);
			@float = 0f;
			vector2 = default(Vector2);
			vector3 = default(Vector3);
			vector4 = default(Vector4);
			texture2D = null;
		}

		public CustomMaterialData(int propertyId, string propertyName)
		{
			name = propertyName;
			id = propertyId;
			dataType = SuportedTypes.Color;
			color = default(Color);
			@float = 0f;
			vector2 = default(Vector2);
			vector3 = default(Vector3);
			vector4 = default(Vector4);
			texture2D = null;
		}

		public override int GetHashCode()
		{
			return (id, dataType, color, @float, vector2, vector3, vector4, texture2D).GetHashCode();
		}
	}

	public ApplyMode mode = ApplyMode.MaterialPropertyBlock;

	[FormerlySerializedAs("materialToApplyBlock")]
	public Material targetMaterial;

	[SerializeField]
	private MaterialInstance _instance;

	[SerializeField]
	private Renderer _renderer;

	public List<CustomMaterialData> customData;

	[SerializeField]
	private bool applyOnStart;

	[NonSerialized]
	private MaterialPropertyBlock _block;

	private void Start()
	{
		UpdateShaderPropertyIds();
		if (applyOnStart)
		{
			Apply();
		}
	}

	public void Apply()
	{
		if (!_renderer)
		{
			_renderer = GetComponent<Renderer>();
		}
		switch (mode)
		{
		case ApplyMode.MaterialInstance:
			ApplyMaterialInstance();
			break;
		case ApplyMode.MaterialPropertyBlock:
			ApplyMaterialPropertyBlock();
			break;
		}
	}

	public void SetColor(string propertyName, Color color)
	{
		SetColor(Shader.PropertyToID(propertyName), color);
	}

	public void SetColor(int propertyId, Color color)
	{
		CustomMaterialData orCreateData = GetOrCreateData(propertyId, null);
		orCreateData.dataType = SuportedTypes.Color;
		orCreateData.color = color;
	}

	public void SetFloat(string propertyName, float value)
	{
		SetFloat(Shader.PropertyToID(propertyName), value);
	}

	public void SetFloat(int propertyId, float value)
	{
		CustomMaterialData orCreateData = GetOrCreateData(propertyId, null);
		orCreateData.dataType = SuportedTypes.Float;
		orCreateData.@float = value;
	}

	private CustomMaterialData GetOrCreateData(int id, string propertyName)
	{
		for (int i = 0; i < customData.Count; i++)
		{
			if (customData[i].id == id)
			{
				return customData[i];
			}
		}
		CustomMaterialData customMaterialData = new CustomMaterialData(id, propertyName);
		customData.Add(customMaterialData);
		return customMaterialData;
	}

	private void ApplyMaterialInstance()
	{
		if (!_instance)
		{
			_instance = GetComponent<MaterialInstance>();
			if (_instance == null)
			{
				_instance = base.gameObject.AddComponent<MaterialInstance>();
			}
		}
		Material material = (targetMaterial = _instance.Material);
		for (int i = 0; i < customData.Count; i++)
		{
			switch (customData[i].dataType)
			{
			case SuportedTypes.Color:
				material.SetColor(customData[i].id, customData[i].color);
				break;
			case SuportedTypes.Float:
				material.SetFloat(customData[i].id, customData[i].@float);
				break;
			case SuportedTypes.Vector2:
				material.SetVector(customData[i].id, customData[i].vector2);
				break;
			case SuportedTypes.Vector3:
				material.SetVector(customData[i].id, customData[i].vector3);
				break;
			case SuportedTypes.Vector4:
				material.SetVector(customData[i].id, customData[i].vector4);
				break;
			case SuportedTypes.Texture2D:
				material.SetTexture(customData[i].id, customData[i].texture2D);
				break;
			}
		}
		_renderer.SetPropertyBlock(_block);
	}

	private void ApplyMaterialPropertyBlock()
	{
		if (_block == null)
		{
			_block = new MaterialPropertyBlock();
		}
		_renderer.GetPropertyBlock(_block);
		for (int i = 0; i < customData.Count; i++)
		{
			switch (customData[i].dataType)
			{
			case SuportedTypes.Color:
				_block.SetColor(customData[i].id, customData[i].color);
				break;
			case SuportedTypes.Float:
				_block.SetFloat(customData[i].id, customData[i].@float);
				break;
			case SuportedTypes.Vector2:
				_block.SetVector(customData[i].id, customData[i].vector2);
				break;
			case SuportedTypes.Vector3:
				_block.SetVector(customData[i].id, customData[i].vector3);
				break;
			case SuportedTypes.Vector4:
				_block.SetVector(customData[i].id, customData[i].vector4);
				break;
			case SuportedTypes.Texture2D:
				_block.SetTexture(customData[i].id, customData[i].texture2D);
				break;
			}
		}
		_renderer.SetPropertyBlock(_block);
	}

	private void UpdateShaderPropertyIds()
	{
		for (int i = 0; i < customData.Count; i++)
		{
			if (customData[i] != null && !string.IsNullOrEmpty(customData[i].name))
			{
				customData[i].id = Shader.PropertyToID(customData[i].name);
			}
		}
	}
}
