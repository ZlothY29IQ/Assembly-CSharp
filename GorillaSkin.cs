using System;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Serialization;

public class GorillaSkin : ScriptableObject
{
	public enum SkinType
	{
		cosmetic,
		gameMode,
		temporaryEffect
	}

	[FormerlySerializedAs("chestMaterial")]
	[FormerlySerializedAs("chestEarsMaterial")]
	[SerializeField]
	private Material _chestMaterial;

	[FormerlySerializedAs("bodyMaterial")]
	[SerializeField]
	private Material _bodyMaterial;

	[SerializeField]
	private Material _scoreboardMaterial;

	[Tooltip("Check this if skin materials are incompatible with HeadlessMonkeRig mesh")]
	[SerializeField]
	private bool _disableHeadless;

	[Space]
	[SerializeField]
	private Mesh _bodyMesh;

	[NonSerialized]
	[Space]
	private Material _bodyRuntime;

	[NonSerialized]
	private Material _chestRuntime;

	[NonSerialized]
	private Material _scoreRuntime;

	public Mesh bodyMesh => _bodyMesh;

	public bool allowHeadless => !_disableHeadless;

	public Material bodyMaterial => _bodyMaterial;

	public Material chestMaterial => _chestMaterial;

	public Material scoreboardMaterial => _scoreboardMaterial;

	public static GorillaSkin CopyWithInstancedMaterials(GorillaSkin basis)
	{
		GorillaSkin gorillaSkin = ScriptableObject.CreateInstance<GorillaSkin>();
		gorillaSkin._chestMaterial = ((basis._chestMaterial != null) ? new Material(basis._chestMaterial) : null);
		gorillaSkin._bodyMaterial = ((basis._bodyMaterial != null) ? new Material(basis._bodyMaterial) : null);
		gorillaSkin._scoreboardMaterial = ((basis._scoreboardMaterial != null) ? new Material(basis._scoreboardMaterial) : null);
		gorillaSkin._bodyMesh = basis.bodyMesh;
		return gorillaSkin;
	}

	public static void ShowActiveSkin(VRRig rig)
	{
		bool useDefaultBodySkin;
		GorillaSkin activeSkin = GetActiveSkin(rig, out useDefaultBodySkin);
		ShowSkin(rig, activeSkin, useDefaultBodySkin);
	}

	public void ApplySkinToMannequin(GameObject mannequin, bool swapMesh = false)
	{
		MeshRenderer component2;
		if (mannequin.TryGetComponent<SkinnedMeshRenderer>(out var component))
		{
			int subMeshCount = component.sharedMesh.subMeshCount;
			if (swapMesh && bodyMesh != null)
			{
				component.sharedMesh = bodyMesh;
			}
			int subMeshCount2 = component.sharedMesh.subMeshCount;
			if (subMeshCount == subMeshCount2)
			{
				Material[] sharedMaterials = component.sharedMaterials;
				sharedMaterials[0] = bodyMaterial;
				if (subMeshCount > 2)
				{
					sharedMaterials[1] = chestMaterial;
				}
				component.sharedMaterials = sharedMaterials;
				return;
			}
			if (component.sharedMaterials.Length == subMeshCount)
			{
				if (subMeshCount2 == 2 && subMeshCount > subMeshCount2)
				{
					Material[] sharedMaterials2 = new Material[2]
					{
						bodyMaterial,
						component.sharedMaterials[2]
					};
					component.sharedMaterials = sharedMaterials2;
				}
				else if (subMeshCount2 == 3 && subMeshCount < subMeshCount2 && component.sharedMaterials.Length > 1)
				{
					Material[] sharedMaterials3 = new Material[3]
					{
						bodyMaterial,
						chestMaterial,
						component.sharedMaterials[1]
					};
					component.sharedMaterials = sharedMaterials3;
				}
				else
				{
					Debug.LogError($"Unexpected Submesh count {subMeshCount} {subMeshCount2}");
				}
				return;
			}
			switch (subMeshCount2)
			{
			case 2:
			{
				Material[] sharedMaterials5 = new Material[1] { bodyMaterial };
				component.sharedMaterials = sharedMaterials5;
				break;
			}
			case 3:
			{
				Material[] sharedMaterials4 = new Material[2] { bodyMaterial, chestMaterial };
				component.sharedMaterials = sharedMaterials4;
				break;
			}
			default:
				Debug.LogError($"Unexpected Submesh count {subMeshCount2}");
				break;
			}
		}
		else if (mannequin.TryGetComponent<MeshRenderer>(out component2))
		{
			Material[] sharedMaterials6 = component2.sharedMaterials;
			sharedMaterials6[0] = bodyMaterial;
			sharedMaterials6[1] = chestMaterial;
			component2.sharedMaterials = sharedMaterials6;
		}
	}

	public static GorillaSkin GetActiveSkin(VRRig rig, out bool useDefaultBodySkin)
	{
		if (rig.CurrentModeSkin.IsNotNull())
		{
			useDefaultBodySkin = false;
			return rig.CurrentModeSkin;
		}
		if (rig.TemporaryEffectSkin.IsNotNull())
		{
			useDefaultBodySkin = false;
			return rig.TemporaryEffectSkin;
		}
		if (rig.CurrentCosmeticSkin.IsNotNull())
		{
			useDefaultBodySkin = false;
			return rig.CurrentCosmeticSkin;
		}
		useDefaultBodySkin = true;
		return rig.defaultSkin;
	}

	public static void ShowSkin(VRRig rig, GorillaSkin skin, bool useDefaultBodySkin = false)
	{
		if (skin.bodyMesh != null)
		{
			rig.bodyRenderer.SetCosmeticBodyMesh(skin.bodyMesh);
		}
		else
		{
			rig.bodyRenderer.ClearCosmeticBodyMesh();
		}
		if (useDefaultBodySkin)
		{
			rig.materialsToChangeTo[0] = rig.myDefaultSkinMaterialInstance;
		}
		else
		{
			rig.materialsToChangeTo[0] = skin.bodyMaterial;
		}
		rig.bodyRenderer.SetSkinMaterials(rig.materialsToChangeTo[rig.setMatIndex], skin.chestMaterial, skin.allowHeadless);
		rig.scoreboardMaterial = skin.scoreboardMaterial;
	}

	public static void ApplyToRig(VRRig rig, GorillaSkin skin, SkinType type)
	{
		bool useDefaultBodySkin;
		GorillaSkin activeSkin = GetActiveSkin(rig, out useDefaultBodySkin);
		switch (type)
		{
		case SkinType.cosmetic:
			rig.CurrentCosmeticSkin = skin;
			break;
		case SkinType.gameMode:
			rig.CurrentModeSkin = skin;
			break;
		case SkinType.temporaryEffect:
			rig.TemporaryEffectSkin = skin;
			break;
		default:
			Debug.LogError("Unknown skin slot");
			break;
		}
		bool useDefaultBodySkin2;
		GorillaSkin activeSkin2 = GetActiveSkin(rig, out useDefaultBodySkin2);
		if (activeSkin != activeSkin2)
		{
			ShowSkin(rig, activeSkin2, useDefaultBodySkin2);
		}
	}
}
