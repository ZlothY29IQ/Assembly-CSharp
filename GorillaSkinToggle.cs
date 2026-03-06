using System;
using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class GorillaSkinToggle : MonoBehaviour, ISpawnable
{
	[Serializable]
	private struct ColoringRule
	{
		public GorillaSkinMaterials colorMaterials;

		public string shaderColorProperty;

		private ShaderHashId shaderHashId;

		public void Init()
		{
			if (string.IsNullOrEmpty(shaderColorProperty))
			{
				shaderColorProperty = "_BaseColor";
			}
			shaderHashId = new ShaderHashId(shaderColorProperty);
		}

		public void Apply(GorillaSkin skin, Color color)
		{
			if (colorMaterials.HasFlag(GorillaSkinMaterials.Body))
			{
				skin.bodyMaterial.SetColor(shaderHashId, color);
			}
			if (colorMaterials.HasFlag(GorillaSkinMaterials.Chest))
			{
				skin.chestMaterial.SetColor(shaderHashId, color);
			}
			if (colorMaterials.HasFlag(GorillaSkinMaterials.Scoreboard))
			{
				skin.scoreboardMaterial.SetColor(shaderHashId, color);
			}
		}
	}

	private VRRig _rig;

	[SerializeField]
	private GorillaSkin _skin;

	private GorillaSkin _activeSkin;

	[SerializeField]
	private ColoringRule[] coloringRules;

	[Space]
	[SerializeField]
	private bool _applied;

	public bool applied => _applied;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnSpawn(VRRig rig)
	{
		_rig = GetComponentInParent<VRRig>(includeInactive: true);
		if (coloringRules.Length != 0)
		{
			_activeSkin = GorillaSkin.CopyWithInstancedMaterials(_skin);
			for (int i = 0; i < coloringRules.Length; i++)
			{
				coloringRules[i].Init();
			}
		}
		else
		{
			_activeSkin = _skin;
		}
	}

	void ISpawnable.OnDespawn()
	{
	}

	private void OnPlayerColorChanged(Color playerColor)
	{
		ColoringRule[] array = coloringRules;
		foreach (ColoringRule coloringRule in array)
		{
			coloringRule.Apply(_activeSkin, playerColor);
		}
	}

	private void OnEnable()
	{
		if (coloringRules.Length != 0)
		{
			_rig.OnColorChanged += OnPlayerColorChanged;
			OnPlayerColorChanged(_rig.playerColor);
		}
		Apply();
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			Remove();
			if (coloringRules.Length != 0)
			{
				_rig.OnColorChanged -= OnPlayerColorChanged;
			}
		}
	}

	public void Apply()
	{
		GorillaSkin.ApplyToRig(_rig, _activeSkin, GorillaSkin.SkinType.cosmetic);
		_applied = true;
	}

	public void ApplyToMannequin(GameObject mannequin, bool swapMesh = false)
	{
		if (_skin.IsNull())
		{
			Debug.LogError("No skin set on GorillaSkinToggle");
		}
		else if (mannequin.IsNull())
		{
			Debug.LogError("No mannequin set on GorillaSkinToggle");
		}
		else
		{
			_skin.ApplySkinToMannequin(mannequin, swapMesh);
		}
	}

	public void Remove()
	{
		GorillaSkin.ApplyToRig(_rig, null, GorillaSkin.SkinType.cosmetic);
		float red = PlayerPrefs.GetFloat("redValue", 0f);
		float green = PlayerPrefs.GetFloat("greenValue", 0f);
		float blue = PlayerPrefs.GetFloat("blueValue", 0f);
		GorillaTagger.Instance.UpdateColor(red, green, blue);
		_applied = false;
	}
}
