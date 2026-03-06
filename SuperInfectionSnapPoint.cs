using System;
using GorillaExtensions;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class SuperInfectionSnapPoint : MonoBehaviour
{
	private const string preLog = "[SuperInfectionSnapPoint]  ";

	private const string preErr = "[SuperInfectionSnapPoint]  ERROR!!!  ";

	public GamePlayer playerForPoint;

	public SnapJointType jointType;

	public GTHardCodedBones.SturdyEBone parentBone;

	public Transform overrideParentTransform;

	private Transform parentTransform;

	public bool canSnapOverride;

	public float snapPointRadius;

	private GameEntity snappedEntity;

	public void Initialize()
	{
		VRRig componentInParent = GetComponentInParent<VRRig>(includeInactive: true);
		if (componentInParent == null)
		{
			throw new NullReferenceException("[SuperInfectionSnapPoint]  ERROR!!!  Expected a VRRig to be in parent hierarchy. Path=\"" + base.transform.GetPathQ() + "\"");
		}
		if (!GTHardCodedBones.TryGetBoneXforms(componentInParent, out var outBoneXforms, out var outErrorMsg))
		{
			throw new NullReferenceException("[SuperInfectionSnapPoint]  ERROR!!!  Could not get bone transforms: " + outErrorMsg);
		}
		if (overrideParentTransform != null)
		{
			parentTransform = overrideParentTransform;
		}
		else if (!GTHardCodedBones.TryGetBoneXform(outBoneXforms, parentBone.Bone, out parentTransform))
		{
			throw new NullReferenceException("[SuperInfectionSnapPoint]  ERROR!!!  " + $"Could not find bone Transform `{parentBone}`.");
		}
		Vector3 localPosition = base.transform.localPosition;
		Vector3 localEulerAngles = base.transform.localEulerAngles;
		if (parentTransform != null)
		{
			base.transform.SetParent(parentTransform, worldPositionStays: false);
		}
		base.transform.localPosition = localPosition;
		base.transform.localEulerAngles = localEulerAngles;
	}

	public void Clear()
	{
		Unsnapped();
	}

	public void Snapped(GameEntity entity)
	{
		snappedEntity = entity;
		if (snappedEntity.TryGetComponent<GameSnappable>(out var component))
		{
			component.snappedToJoint = this;
		}
		else
		{
			Debug.LogError($"Snapped: entity {snappedEntity} has no GameSnappable!?");
		}
	}

	public void Unsnapped()
	{
		if ((bool)snappedEntity && snappedEntity.TryGetComponent<GameSnappable>(out var component))
		{
			component.snappedToJoint = null;
		}
		else
		{
			Debug.LogError($"Unsnapped: entity {snappedEntity} has no GameSnappable!?");
		}
		snappedEntity = null;
	}

	public bool HasSnapped()
	{
		return snappedEntity != null;
	}

	public GameEntity GetSnappedEntity()
	{
		return snappedEntity;
	}
}
