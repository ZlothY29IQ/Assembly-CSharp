using System;
using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class GTPosRotConstraints : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private bool _shouldCallOnSpawnDuringAwake;

	[Tooltip("Used for actors that get disabled and re-enabled")]
	[SerializeField]
	private bool _registerOnEnable;

	public GorillaPosRotConstraint[] constraints;

	public bool IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	public void Awake()
	{
		if (_shouldCallOnSpawnDuringAwake)
		{
			VRRig componentInParent = GetComponentInParent<VRRig>(includeInactive: true);
			if (!(componentInParent == null))
			{
				((ISpawnable)this).OnSpawn(componentInParent);
			}
		}
	}

	void ISpawnable.OnSpawn(VRRig rig)
	{
		Transform[] outBoneXforms = Array.Empty<Transform>();
		if ((object)rig != null && !GTHardCodedBones.TryGetBoneXforms(rig, out outBoneXforms, out var outErrorMsg))
		{
			Debug.LogError("GTPosRotConstraints: Error getting bone Transforms: " + outErrorMsg, this);
			return;
		}
		for (int i = 0; i < constraints.Length; i++)
		{
			GorillaPosRotConstraint gorillaPosRotConstraint = constraints[i];
			if (Mathf.Approximately(gorillaPosRotConstraint.rotationOffset.x, 0f) && Mathf.Approximately(gorillaPosRotConstraint.rotationOffset.y, 0f) && Mathf.Approximately(gorillaPosRotConstraint.rotationOffset.z, 0f) && Mathf.Approximately(gorillaPosRotConstraint.rotationOffset.w, 0f))
			{
				gorillaPosRotConstraint.rotationOffset = Quaternion.identity;
			}
			if (!gorillaPosRotConstraint.follower)
			{
				Debug.LogError(string.Format("{0}: Disabling component! At index {1}, Transform `follower` is ", "GTPosRotConstraints", i) + "null. Affected component path: " + base.transform.GetPathQ() + "\n- Affected component path: " + base.transform.GetPathQ(), this);
				base.enabled = false;
				return;
			}
			if ((GTHardCodedBones.EBone)gorillaPosRotConstraint.sourceGorillaBone == GTHardCodedBones.EBone.None)
			{
				if (!gorillaPosRotConstraint.source)
				{
					if (string.IsNullOrEmpty(gorillaPosRotConstraint.sourceRelativePath))
					{
						Debug.LogError(string.Format("{0}: Disabling component! At index {1} Transform `source` is ", "GTPosRotConstraints", i) + "null, not EBone, and `sourceRelativePath` is null or empty.\n- Affected component path: " + base.transform.GetPathQ(), this);
						base.enabled = false;
						return;
					}
					if (!base.transform.TryFindByPath(gorillaPosRotConstraint.sourceRelativePath, out gorillaPosRotConstraint.source))
					{
						Debug.LogError(string.Format("{0}: Disabling component! At index {1} Transform `source` is ", "GTPosRotConstraints", i) + "null, not EBone, and could not find by path: \"" + gorillaPosRotConstraint.sourceRelativePath + "\"\n- Affected component path: " + base.transform.GetPathQ(), this);
						base.enabled = false;
						return;
					}
				}
				constraints[i] = gorillaPosRotConstraint;
				continue;
			}
			if ((object)rig == null)
			{
				Debug.LogError("GTPosRotConstraints: Disabling component! `VRRig` could not be found in parents, but " + $"bone at index {i} is set to use EBone `{gorillaPosRotConstraint.sourceGorillaBone}` but without `VRRig` it cannot " + "be resolved.\n- Affected component path: " + base.transform.GetPathQ(), this);
				base.enabled = false;
				return;
			}
			int boneIndex = GTHardCodedBones.GetBoneIndex(gorillaPosRotConstraint.sourceGorillaBone);
			if (boneIndex <= 0)
			{
				Debug.LogError(string.Format("{0}: (should never happen) Disabling component! At index {1}, could ", "GTPosRotConstraints", i) + $"not find EBone `{gorillaPosRotConstraint.sourceGorillaBone}`.\n" + "- Affected component path: " + base.transform.GetPathQ(), this);
				base.enabled = false;
				return;
			}
			gorillaPosRotConstraint.source = outBoneXforms[boneIndex];
			if (!gorillaPosRotConstraint.source)
			{
				Debug.LogError(string.Format("{0}: Disabling component! At index {1}, bone {2} was ", "GTPosRotConstraints", i, gorillaPosRotConstraint.sourceGorillaBone) + "not present in `VRRig` path: " + rig.transform.GetPathQ() + "\n- Affected component path: " + base.transform.GetPathQ(), this);
				base.enabled = false;
				return;
			}
			constraints[i] = gorillaPosRotConstraint;
		}
		if (base.isActiveAndEnabled && !_registerOnEnable)
		{
			GTPosRotConstraintManager.Register(this);
		}
	}

	void ISpawnable.OnDespawn()
	{
	}

	protected void OnEnable()
	{
		if (IsSpawned || _registerOnEnable)
		{
			GTPosRotConstraintManager.Register(this);
		}
	}

	protected void OnDisable()
	{
		GTPosRotConstraintManager.Unregister(this);
	}
}
