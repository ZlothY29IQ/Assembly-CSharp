using System;
using System.Collections.Generic;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

namespace GorillaNetworking;

public class CosmeticItemInstance
{
	public List<GameObject> leftObjects = new List<GameObject>();

	public List<GameObject> rightObjects = new List<GameObject>();

	public List<GameObject> objects = new List<GameObject>();

	public List<GameObject> holdableObjects = new List<GameObject>();

	public List<Renderer> allRenderers = new List<Renderer>();

	public List<ParticleSystem> allParticles = new List<ParticleSystem>();

	public CosmeticAnchorAntiIntersectOffsets clippingOffsets;

	public bool isHoldableItem;

	public string dbgname;

	private BodyDockPositions _bodyDockPositions;

	private VRRigAnchorOverrides _anchorOverrides;

	private CosmeticsController.CosmeticSlots _activeSlot;

	public CosmeticsController.CosmeticSlots ActiveSlot => _activeSlot;

	private void EnableItem(GameObject obj, bool enable)
	{
		try
		{
			obj.SetActive(enable);
		}
		catch (Exception arg)
		{
			Debug.LogError($"Exception while enabling cosmetic: {arg}");
		}
	}

	private void ApplyClippingOffsets(bool itemEnabled)
	{
		if (!(_bodyDockPositions == null) && _anchorOverrides != null)
		{
			if (clippingOffsets.nameTag.enabled)
			{
				_anchorOverrides.UpdateNameTagOffset(itemEnabled ? clippingOffsets.nameTag.offset : XformOffset.Identity, itemEnabled, _activeSlot);
			}
			if (clippingOffsets.leftArm.enabled)
			{
				_anchorOverrides.ApplyAntiClippingOffsets(TransferrableObject.PositionState.OnLeftArm, clippingOffsets.leftArm.offset, itemEnabled, _bodyDockPositions.leftArmTransform);
			}
			if (clippingOffsets.rightArm.enabled)
			{
				_anchorOverrides.ApplyAntiClippingOffsets(TransferrableObject.PositionState.OnRightArm, clippingOffsets.rightArm.offset, itemEnabled, _bodyDockPositions.rightArmTransform);
			}
			if (clippingOffsets.chest.enabled)
			{
				_anchorOverrides.ApplyAntiClippingOffsets(TransferrableObject.PositionState.OnChest, clippingOffsets.chest.offset, itemEnabled, _anchorOverrides.chestDefaultTransform);
			}
			if (clippingOffsets.huntComputer.enabled)
			{
				_anchorOverrides.UpdateHuntWatchOffset(clippingOffsets.huntComputer.offset, itemEnabled);
			}
			if (clippingOffsets.badge.enabled)
			{
				_anchorOverrides.UpdateBadgeOffset(itemEnabled ? clippingOffsets.badge.offset : XformOffset.Identity, itemEnabled, _activeSlot);
			}
			if (clippingOffsets.builderWatch.enabled)
			{
				_anchorOverrides.UpdateBuilderWatchOffset(clippingOffsets.builderWatch.offset, itemEnabled);
			}
			if (clippingOffsets.friendshipBraceletLeft.enabled)
			{
				_anchorOverrides.UpdateFriendshipBraceletOffset(clippingOffsets.friendshipBraceletLeft.offset, left: true, itemEnabled);
			}
			if (clippingOffsets.friendshipBraceletRight.enabled)
			{
				_anchorOverrides.UpdateFriendshipBraceletOffset(clippingOffsets.friendshipBraceletRight.offset, left: false, itemEnabled);
			}
		}
	}

	public void DisableItem(CosmeticsController.CosmeticSlots cosmeticSlot)
	{
		bool flag = CosmeticsController.CosmeticSet.IsSlotLeftHanded(cosmeticSlot);
		bool flag2 = CosmeticsController.CosmeticSet.IsSlotRightHanded(cosmeticSlot);
		foreach (GameObject @object in objects)
		{
			EnableItem(@object, enable: false);
		}
		if (flag)
		{
			foreach (GameObject leftObject in leftObjects)
			{
				EnableItem(leftObject, enable: false);
			}
		}
		if (flag2)
		{
			foreach (GameObject rightObject in rightObjects)
			{
				EnableItem(rightObject, enable: false);
			}
		}
		ApplyClippingOffsets(itemEnabled: false);
	}

	public void EnableItem(CosmeticsController.CosmeticSlots cosmeticSlot, VRRig rig)
	{
		bool flag = CosmeticsController.CosmeticSet.IsSlotLeftHanded(cosmeticSlot);
		bool flag2 = CosmeticsController.CosmeticSet.IsSlotRightHanded(cosmeticSlot);
		_activeSlot = cosmeticSlot;
		if (rig != null && _anchorOverrides == null)
		{
			_anchorOverrides = rig.gameObject.GetComponent<VRRigAnchorOverrides>();
			_bodyDockPositions = rig.GetComponent<BodyDockPositions>();
		}
		foreach (GameObject @object in objects)
		{
			EnableItem(@object, enable: true);
			if (cosmeticSlot != CosmeticsController.CosmeticSlots.Badge)
			{
				continue;
			}
			if (objects.Count > 1)
			{
				if (GTHardCodedBones.TryGetFirstBoneInParents(@object.transform, out var eBone, out var _) && eBone == GTHardCodedBones.EBone.body)
				{
					_anchorOverrides.CurrentBadgeTransform = @object.transform;
				}
			}
			else
			{
				_anchorOverrides.CurrentBadgeTransform = @object.transform;
			}
		}
		if (flag)
		{
			foreach (GameObject leftObject in leftObjects)
			{
				EnableItem(leftObject, enable: true);
			}
		}
		if (flag2)
		{
			foreach (GameObject rightObject in rightObjects)
			{
				EnableItem(rightObject, enable: true);
			}
		}
		ApplyClippingOffsets(itemEnabled: true);
	}

	public void ToggleRenderers(bool enabled)
	{
		for (int i = 0; i < allRenderers.Count; i++)
		{
			allRenderers[i].enabled = enabled;
		}
	}

	public void ToggleParticles(bool enabled)
	{
		for (int i = 0; i < allParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = allParticles[i].emission;
			emission.enabled = enabled;
		}
	}
}
