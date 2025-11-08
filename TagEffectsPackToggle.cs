using GorillaTag;
using GorillaTag.CosmeticSystem;
using TagEffects;
using UnityEngine;

public class TagEffectsPackToggle : MonoBehaviour, ISpawnable
{
	private VRRig _rig;

	[SerializeField]
	private TagEffectPack tagEffectPack;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnSpawn(VRRig rig)
	{
		_rig = rig;
	}

	void ISpawnable.OnDespawn()
	{
	}

	private void OnEnable()
	{
		Apply();
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			Remove();
		}
	}

	public void Apply()
	{
		_rig.CosmeticEffectPack = tagEffectPack;
	}

	public void Remove()
	{
		_rig.CosmeticEffectPack = null;
	}
}
