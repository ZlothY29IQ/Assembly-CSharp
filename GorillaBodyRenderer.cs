using System;
using System.Collections.Generic;
using UnityEngine;

public class GorillaBodyRenderer : MonoBehaviour
{
	[SerializeField]
	private GorillaBodyType _bodyType;

	[SerializeField]
	private bool _renderFace = true;

	public MeshRenderer faceRenderer;

	[SerializeField]
	private SkinnedMeshRenderer bodyDefault;

	[SerializeField]
	private SkinnedMeshRenderer bodyNoHead;

	[SerializeField]
	private SkinnedMeshRenderer bodySkeleton;

	private int _lastMatIndex;

	private Mesh defaultBodyMesh;

	private static bool oopsAllSkeletons;

	private GorillaBodyType cosmeticBodyType;

	[SerializeField]
	private Material[] _cachedSkinMaterials = new Material[0];

	[SerializeField]
	private Material[] _defaultSkinMaterials = new Material[0];

	private bool _applySkinToHeadlessMesh;

	[NonSerialized]
	[Space]
	private SkinnedMeshRenderer[] _renderersCache = new SkinnedMeshRenderer[0];

	private static readonly List<Material> gEmptyDefaultMats = new List<Material>();

	[Space]
	public VRRig rig;

	public GorillaBodyType bodyType
	{
		get
		{
			return _bodyType;
		}
		set
		{
			SetBodyType(value);
		}
	}

	public bool renderFace => _renderFace;

	public static bool ForceSkeleton => oopsAllSkeletons;

	public GorillaBodyType gameModeBodyType { get; private set; }

	public Material myDefaultSkinMaterialInstance { get; private set; }

	public SkinnedMeshRenderer ActiveBody => GetBody(_bodyType);

	public SkinnedMeshRenderer GetBody(GorillaBodyType type)
	{
		if (type < GorillaBodyType.Default || (int)type >= _renderersCache.Length)
		{
			return null;
		}
		return _renderersCache[(int)type];
	}

	public static void SetAllSkeletons(bool allSkeletons)
	{
		oopsAllSkeletons = allSkeletons;
		GorillaTagger.Instance.offlineVRRig.bodyRenderer.Refresh();
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			activeRigContainer.Rig.bodyRenderer.Refresh();
		}
	}

	public void SetSkeletonBodyActive(bool active)
	{
		bodySkeleton.gameObject.SetActive(active);
	}

	public void SetGameModeBodyType(GorillaBodyType bodyType)
	{
		if (gameModeBodyType != bodyType)
		{
			gameModeBodyType = bodyType;
			Refresh();
		}
	}

	public void SetCosmeticBodyType(GorillaBodyType bodyType)
	{
		if (cosmeticBodyType != bodyType)
		{
			cosmeticBodyType = bodyType;
			Refresh();
		}
	}

	public void SetDefaults()
	{
		gameModeBodyType = GorillaBodyType.Default;
		cosmeticBodyType = GorillaBodyType.Default;
		Refresh();
	}

	private void Refresh()
	{
		SetBodyType(GetActiveBodyType());
	}

	public void SetMaterialIndex(int materialIndex)
	{
		_lastMatIndex = materialIndex;
		switch (bodyType)
		{
		case GorillaBodyType.Default:
			bodyDefault.sharedMaterial = rig.materialsToChangeTo[materialIndex];
			break;
		case GorillaBodyType.NoHead:
			if (materialIndex == 0 && !_applySkinToHeadlessMesh)
			{
				bodyNoHead.sharedMaterial = myDefaultSkinMaterialInstance;
			}
			else
			{
				bodyNoHead.sharedMaterial = rig.materialsToChangeTo[materialIndex];
			}
			break;
		case GorillaBodyType.Skeleton:
			rig.skeleton.SetMaterialIndex(materialIndex);
			break;
		}
	}

	public void SetSkinMaterials(Material bodyMat, Material chestMat, bool allowHeadless)
	{
		EnsureInstantiatedMaterial();
		if (chestMat == null)
		{
			if (_cachedSkinMaterials.Length != 1)
			{
				_cachedSkinMaterials = new Material[1];
			}
			_cachedSkinMaterials[0] = bodyMat;
		}
		else
		{
			if (_cachedSkinMaterials.Length < 2)
			{
				_cachedSkinMaterials = new Material[2];
			}
			_cachedSkinMaterials[0] = bodyMat;
			_cachedSkinMaterials[1] = chestMat;
		}
		_applySkinToHeadlessMesh = allowHeadless;
		switch (bodyType)
		{
		case GorillaBodyType.Default:
			bodyDefault.sharedMaterials = _cachedSkinMaterials;
			bodyDefault.sharedMaterial = rig.materialsToChangeTo[_lastMatIndex];
			break;
		case GorillaBodyType.NoHead:
			if (_applySkinToHeadlessMesh)
			{
				bodyNoHead.sharedMaterials = _cachedSkinMaterials;
				bodyNoHead.sharedMaterial = rig.materialsToChangeTo[_lastMatIndex];
				break;
			}
			bodyNoHead.sharedMaterials = _defaultSkinMaterials;
			if (_lastMatIndex != 0)
			{
				bodyNoHead.sharedMaterial = rig.materialsToChangeTo[_lastMatIndex];
			}
			break;
		}
	}

	public void SetupAsLocalPlayerBody()
	{
		faceRenderer.gameObject.layer = 22;
	}

	public GorillaBodyType GetActiveBodyType()
	{
		if (!oopsAllSkeletons)
		{
			if (gameModeBodyType == GorillaBodyType.Default)
			{
				return cosmeticBodyType;
			}
			return gameModeBodyType;
		}
		return GorillaBodyType.Skeleton;
	}

	private void SetBodyType(GorillaBodyType type)
	{
		if (_bodyType == type)
		{
			return;
		}
		SetBodyEnabled(_bodyType, enabled: false);
		_bodyType = type;
		SetBodyEnabled(type, enabled: true);
		_renderFace = _bodyType != GorillaBodyType.NoHead && _bodyType != GorillaBodyType.Skeleton && _bodyType != GorillaBodyType.Invisible;
		if (faceRenderer != null)
		{
			faceRenderer.enabled = _renderFace;
		}
		switch (type)
		{
		case GorillaBodyType.Default:
			bodyDefault.sharedMaterials = _cachedSkinMaterials;
			bodyDefault.sharedMaterial = rig.materialsToChangeTo[_lastMatIndex];
			UpdateBodyMaterialColor(rig.playerColor);
			break;
		case GorillaBodyType.NoHead:
			if (_applySkinToHeadlessMesh)
			{
				bodyNoHead.sharedMaterials = _cachedSkinMaterials;
				bodyNoHead.sharedMaterial = rig.materialsToChangeTo[_lastMatIndex];
			}
			else
			{
				bodyNoHead.sharedMaterials = _defaultSkinMaterials;
				if (_lastMatIndex != 0)
				{
					bodyNoHead.sharedMaterial = rig.materialsToChangeTo[_lastMatIndex];
				}
			}
			UpdateBodyMaterialColor(rig.playerColor);
			break;
		case GorillaBodyType.Skeleton:
			rig.skeleton.SetMaterialIndex(_lastMatIndex);
			rig.skeleton.UpdateColor(rig.playerColor);
			break;
		}
	}

	public void SetCosmeticBodyMesh(Mesh mesh)
	{
		if (defaultBodyMesh == null)
		{
			defaultBodyMesh = bodyDefault.sharedMesh;
		}
		bodyDefault.sharedMesh = mesh;
	}

	public void ClearCosmeticBodyMesh()
	{
		if (defaultBodyMesh != null)
		{
			bodyDefault.sharedMesh = defaultBodyMesh;
		}
	}

	private void SetBodyEnabled(GorillaBodyType bodyType, bool enabled)
	{
		SkinnedMeshRenderer body = GetBody(bodyType);
		if (!(body == null))
		{
			body.enabled = enabled;
			Transform[] bones = body.bones;
			for (int i = 0; i < bones.Length; i++)
			{
				bones[i].gameObject.SetActive(enabled);
			}
		}
	}

	private void Awake()
	{
		Setup();
	}

	public void SharedStart()
	{
		if (rig == null)
		{
			rig = GetComponentInParent<VRRig>();
		}
		EnsureInstantiatedMaterial();
	}

	private void Setup()
	{
		if (rig == null)
		{
			rig = GetComponentInParent<VRRig>();
		}
		_renderersCache = new SkinnedMeshRenderer[EnumData<GorillaBodyType>.Shared.Values.Length];
		_renderersCache[0] = bodyDefault;
		_renderersCache[1] = bodyNoHead;
		_renderersCache[2] = bodySkeleton;
		SetBodyEnabled(GorillaBodyType.Default, enabled: true);
		SetBodyEnabled(GorillaBodyType.NoHead, enabled: false);
		SetBodyEnabled(GorillaBodyType.Skeleton, enabled: false);
		_cachedSkinMaterials = bodyDefault.sharedMaterials;
		_bodyType = GorillaBodyType.Default;
		_bodyType = GorillaBodyType.Default;
		defaultBodyMesh = bodyDefault.sharedMesh;
		EnsureInstantiatedMaterial();
		UpdateColor(rig.playerColor);
		Refresh();
	}

	public void EnsureInstantiatedMaterial()
	{
		if (myDefaultSkinMaterialInstance == null)
		{
			myDefaultSkinMaterialInstance = UnityEngine.Object.Instantiate(rig.materialsToChangeTo[0]);
			rig.materialsToChangeTo[0] = myDefaultSkinMaterialInstance;
		}
		if (_defaultSkinMaterials.Length == 0)
		{
			_defaultSkinMaterials = new Material[2];
			_defaultSkinMaterials[0] = myDefaultSkinMaterialInstance;
			_defaultSkinMaterials[1] = rig.defaultSkin.chestMaterial;
		}
	}

	public void ResetBodyMaterial()
	{
		bodyDefault.sharedMaterial = rig.materialsToChangeTo[0];
		bodyNoHead.sharedMaterial = (_applySkinToHeadlessMesh ? rig.materialsToChangeTo[0] : myDefaultSkinMaterialInstance);
	}

	public void UpdateColor(Color color)
	{
		UpdateBodyMaterialColor(color);
		if (bodyType == GorillaBodyType.Skeleton)
		{
			rig.skeleton.UpdateColor(color);
		}
	}

	private void UpdateBodyMaterialColor(Color color)
	{
		EnsureInstantiatedMaterial();
		if (myDefaultSkinMaterialInstance != null)
		{
			myDefaultSkinMaterialInstance.color = color;
		}
	}
}
