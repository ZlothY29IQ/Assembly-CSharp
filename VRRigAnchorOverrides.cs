using System;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class VRRigAnchorOverrides : MonoBehaviour
{
	[SerializeField]
	public Transform nameDefaultAnchor;

	[SerializeField]
	public Transform nameTransform;

	[SerializeField]
	public Transform chestDefaultTransform;

	[SerializeField]
	public Transform huntComputer;

	[SerializeField]
	public Transform huntComputerDefaultAnchor;

	public Transform huntDefaultTransform;

	[SerializeField]
	protected Transform builderResizeButton;

	[SerializeField]
	protected Transform builderResizeButtonDefaultAnchor;

	private Transform builderResizeButtonDefaultTransform;

	private readonly Transform[] overrideAnchors = new Transform[8];

	private CosmeticAnchorAntiIntersectOffsets activeAntiClippingOffsets;

	private Transform[] clippingOffsetTransforms = new Transform[8];

	private GameObject nameLastObjectToAttach;

	private Transform currentBadgeTransform;

	private Vector3 badgeDefaultPos;

	private Quaternion badgeDefaultRot;

	private GameObject[] badgeAnchors = new GameObject[4];

	private GameObject[] nameAnchors = new GameObject[6];

	private CosmeticAnchorAntiClipEntry[] badgeOffsets = new CosmeticAnchorAntiClipEntry[6];

	private CosmeticAnchorAntiClipEntry[] nameOffsets = new CosmeticAnchorAntiClipEntry[7];

	[SerializeField]
	public Transform friendshipBraceletLeftDefaultAnchor;

	public Transform friendshipBraceletLeftAnchor;

	[SerializeField]
	public Transform friendshipBraceletRightDefaultAnchor;

	public Transform friendshipBraceletRightAnchor;

	[DebugOption]
	public Transform CurrentBadgeTransform
	{
		get
		{
			return currentBadgeTransform;
		}
		set
		{
			if (value != currentBadgeTransform)
			{
				ResetBadge();
				currentBadgeTransform = value;
				badgeDefaultRot = currentBadgeTransform.localRotation;
				badgeDefaultPos = currentBadgeTransform.localPosition;
				UpdateBadge();
			}
		}
	}

	public Transform HuntDefaultAnchor => huntComputerDefaultAnchor;

	public Transform HuntComputer => huntComputer;

	public Transform BuilderWatchAnchor => builderResizeButtonDefaultAnchor;

	public Transform BuilderWatch => builderResizeButton;

	private void Awake()
	{
		for (int i = 0; i < 8; i++)
		{
			overrideAnchors[i] = null;
		}
		int num = MapPositionToIndex(TransferrableObject.PositionState.OnChest);
		overrideAnchors[num] = chestDefaultTransform;
		huntDefaultTransform = huntComputer;
		builderResizeButtonDefaultTransform = builderResizeButton;
		activeAntiClippingOffsets = default(CosmeticAnchorAntiIntersectOffsets);
	}

	private void OnEnable()
	{
		if ((bool)nameDefaultAnchor && (bool)nameDefaultAnchor.parent)
		{
			nameTransform.parent = nameDefaultAnchor.parent;
		}
		else
		{
			Debug.LogError("VRRigAnchorOverrides: could not set parent `nameTransform` because `nameDefaultAnchor` or its parent was null!" + base.transform.GetPathQ(), this);
		}
		huntComputer = huntDefaultTransform;
		if ((bool)huntComputerDefaultAnchor && (bool)huntComputerDefaultAnchor.parent)
		{
			huntComputer.parent = huntComputerDefaultAnchor.parent;
		}
		else
		{
			Debug.LogError("VRRigAnchorOverrides: could not set parent `huntComputer` because `huntComputerDefaultAnchor` or its parent was null!" + base.transform.GetPathQ(), this);
		}
		builderResizeButton = builderResizeButtonDefaultTransform;
		if ((bool)builderResizeButtonDefaultAnchor && (bool)builderResizeButtonDefaultAnchor.parent)
		{
			builderResizeButton.parent = builderResizeButtonDefaultAnchor.parent;
		}
		else
		{
			Debug.LogError("VRRigAnchorOverrides: could not set parent `builderResizeButton` because `builderResizeButtonDefaultAnchor` or its parent was null! Path: " + base.transform.GetPathQ(), this);
		}
	}

	private int MapPositionToIndex(TransferrableObject.PositionState pos)
	{
		int num = (int)pos;
		int num2 = 0;
		while ((num >>= 1) != 0)
		{
			num2++;
		}
		return num2;
	}

	public void ApplyAntiClippingOffsets(TransferrableObject.PositionState pos, XformOffset offset, bool enable, Transform defaultAnchor)
	{
		int num = MapPositionToIndex(pos);
		switch (pos)
		{
		case TransferrableObject.PositionState.OnLeftArm:
			activeAntiClippingOffsets.leftArm.enabled = enable;
			activeAntiClippingOffsets.leftArm.offset = (enable ? offset : XformOffset.Identity);
			break;
		case TransferrableObject.PositionState.OnRightArm:
			activeAntiClippingOffsets.rightArm.enabled = enable;
			activeAntiClippingOffsets.rightArm.offset = (enable ? offset : XformOffset.Identity);
			break;
		case TransferrableObject.PositionState.OnChest:
			activeAntiClippingOffsets.chest.enabled = enable;
			activeAntiClippingOffsets.chest.offset = (enable ? offset : XformOffset.Identity);
			break;
		default:
			GTDev.LogWarning($"Anti Clipping offset for position {pos} is not implemented");
			return;
		}
		if (enable && (overrideAnchors[num] == null || (pos == TransferrableObject.PositionState.OnChest && overrideAnchors[num] == chestDefaultTransform)))
		{
			if (clippingOffsetTransforms[num] == null)
			{
				GameObject gameObject = new GameObject("Anti Clipping Offset");
				gameObject.transform.SetParent(defaultAnchor);
				clippingOffsetTransforms[num] = gameObject.transform;
			}
			Transform transform = clippingOffsetTransforms[num];
			transform.SetParent(defaultAnchor);
			transform.localPosition = offset.pos;
			transform.localRotation = offset.rot;
			transform.localScale = Vector3.one;
			OverrideAnchor(pos, transform);
		}
		else if (!enable && overrideAnchors[num] == clippingOffsetTransforms[num])
		{
			if (pos == TransferrableObject.PositionState.OnChest)
			{
				OverrideAnchor(pos, chestDefaultTransform);
			}
			else
			{
				OverrideAnchor(pos, null);
			}
		}
	}

	public void OverrideAnchor(TransferrableObject.PositionState pos, Transform anchor)
	{
		int num = MapPositionToIndex(pos);
		if (overrideAnchors[num] == chestDefaultTransform)
		{
			foreach (Transform item in overrideAnchors[num])
			{
				if (!item.name.Equals("DropZoneChest") && item != anchor)
				{
					item.parent = null;
				}
			}
			overrideAnchors[num] = anchor;
			return;
		}
		if ((bool)overrideAnchors[num])
		{
			foreach (Transform item2 in overrideAnchors[num])
			{
				if (item2 != anchor)
				{
					item2.parent = null;
				}
			}
		}
		overrideAnchors[num] = anchor;
	}

	public Transform AnchorOverride(TransferrableObject.PositionState pos, Transform fallback)
	{
		int num = MapPositionToIndex(pos);
		Transform transform = overrideAnchors[num];
		if ((object)transform != null)
		{
			return transform;
		}
		return fallback;
	}

	public void UpdateHuntWatchOffset(XformOffset offset, bool enable)
	{
		activeAntiClippingOffsets.huntComputer.enabled = enable;
		activeAntiClippingOffsets.huntComputer.offset = (enable ? offset : XformOffset.Identity);
		huntComputer.parent = HuntDefaultAnchor;
		huntComputer.localPosition = activeAntiClippingOffsets.huntComputer.offset.pos;
		huntComputer.localRotation = activeAntiClippingOffsets.huntComputer.offset.rot;
	}

	public void UpdateBuilderWatchOffset(XformOffset offset, bool enable)
	{
		activeAntiClippingOffsets.builderWatch.enabled = enable;
		activeAntiClippingOffsets.builderWatch.offset = (enable ? offset : XformOffset.Identity);
		BuilderWatch.parent = BuilderWatchAnchor;
		BuilderWatch.localPosition = activeAntiClippingOffsets.builderWatch.offset.pos;
		BuilderWatch.localRotation = activeAntiClippingOffsets.builderWatch.offset.rot;
	}

	public void UpdateFriendshipBraceletOffset(XformOffset offset, bool left, bool enable)
	{
		if (left)
		{
			activeAntiClippingOffsets.friendshipBraceletLeft.enabled = enable;
			activeAntiClippingOffsets.friendshipBraceletLeft.offset = (enable ? offset : XformOffset.Identity);
			friendshipBraceletLeftAnchor.parent = friendshipBraceletLeftDefaultAnchor;
			friendshipBraceletLeftAnchor.localPosition = activeAntiClippingOffsets.friendshipBraceletLeft.offset.pos;
			friendshipBraceletLeftAnchor.localRotation = activeAntiClippingOffsets.friendshipBraceletLeft.offset.rot;
			friendshipBraceletLeftAnchor.localScale = activeAntiClippingOffsets.friendshipBraceletLeft.offset.scale;
		}
		else
		{
			activeAntiClippingOffsets.friendshipBraceletRight.enabled = enable;
			activeAntiClippingOffsets.friendshipBraceletRight.offset = (enable ? offset : XformOffset.Identity);
			friendshipBraceletRightAnchor.parent = friendshipBraceletRightDefaultAnchor;
			friendshipBraceletRightAnchor.localPosition = activeAntiClippingOffsets.friendshipBraceletRight.offset.pos;
			friendshipBraceletRightAnchor.localRotation = activeAntiClippingOffsets.friendshipBraceletRight.offset.rot;
			friendshipBraceletRightAnchor.localScale = activeAntiClippingOffsets.friendshipBraceletRight.offset.scale;
		}
	}

	public void UpdateNameTagOffset(XformOffset offset, bool enable, CosmeticsController.CosmeticSlots slot)
	{
		switch (slot)
		{
		case CosmeticsController.CosmeticSlots.Shirt:
			nameOffsets[0].enabled = enable;
			nameOffsets[0].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Fur:
			nameOffsets[1].enabled = enable;
			nameOffsets[1].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Pants:
			nameOffsets[2].enabled = enable;
			nameOffsets[2].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Back:
			nameOffsets[3].enabled = enable;
			nameOffsets[3].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Face:
			nameOffsets[4].enabled = enable;
			nameOffsets[4].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Hat:
			nameOffsets[5].enabled = enable;
			nameOffsets[5].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Badge:
			nameOffsets[6].enabled = enable;
			nameOffsets[6].offset = offset;
			break;
		}
		UpdateName();
	}

	[Obsolete("Use UpdateNameOffset", true)]
	public void UpdateNameAnchor(GameObject nameAnchor, CosmeticsController.CosmeticSlots slot)
	{
		switch (slot)
		{
		case CosmeticsController.CosmeticSlots.Shirt:
			nameAnchors[0] = nameAnchor;
			break;
		case CosmeticsController.CosmeticSlots.Fur:
			nameAnchors[1] = nameAnchor;
			break;
		case CosmeticsController.CosmeticSlots.Pants:
			nameAnchors[2] = nameAnchor;
			break;
		case CosmeticsController.CosmeticSlots.Back:
			nameAnchors[3] = nameAnchor;
			break;
		case CosmeticsController.CosmeticSlots.Face:
			nameAnchors[4] = nameAnchor;
			break;
		case CosmeticsController.CosmeticSlots.Badge:
			nameAnchors[5] = nameAnchor;
			break;
		}
		UpdateName();
	}

	private void UpdateName()
	{
		for (int i = 0; i < nameOffsets.Length; i++)
		{
			if (nameOffsets[i].enabled)
			{
				nameTransform.parent = nameDefaultAnchor;
				nameTransform.localRotation = nameOffsets[i].offset.rot;
				nameTransform.localPosition = nameOffsets[i].offset.pos;
				return;
			}
		}
		if ((bool)nameDefaultAnchor)
		{
			nameTransform.parent = nameDefaultAnchor;
			nameTransform.localRotation = Quaternion.identity;
			nameTransform.localPosition = Vector3.zero;
		}
		else
		{
			Debug.LogError("VRRigAnchorOverrides: could not set parent for `nameTransform` because `nameDefaultAnchor` or its parent was null! Path: " + base.transform.GetPathQ(), this);
		}
	}

	public void UpdateBadgeOffset(XformOffset offset, bool enable, CosmeticsController.CosmeticSlots slot)
	{
		switch (slot)
		{
		case CosmeticsController.CosmeticSlots.Shirt:
			badgeOffsets[0].enabled = enable;
			badgeOffsets[0].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Fur:
			badgeOffsets[1].enabled = enable;
			badgeOffsets[1].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Pants:
			badgeOffsets[2].enabled = enable;
			badgeOffsets[2].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Back:
			badgeOffsets[3].enabled = enable;
			badgeOffsets[3].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Face:
			badgeOffsets[4].enabled = enable;
			badgeOffsets[4].offset = offset;
			break;
		case CosmeticsController.CosmeticSlots.Hat:
			badgeOffsets[5].enabled = enable;
			badgeOffsets[5].offset = offset;
			break;
		}
		UpdateBadge();
	}

	[Obsolete("Use UpdateBadgeOffset", true)]
	public void UpdateBadgeAnchor(GameObject badgeAnchor, CosmeticsController.CosmeticSlots slot)
	{
		switch (slot)
		{
		case CosmeticsController.CosmeticSlots.Shirt:
			badgeAnchors[0] = badgeAnchor;
			break;
		case CosmeticsController.CosmeticSlots.Fur:
			badgeAnchors[1] = badgeAnchor;
			break;
		case CosmeticsController.CosmeticSlots.Pants:
			badgeAnchors[2] = badgeAnchor;
			break;
		case CosmeticsController.CosmeticSlots.Back:
			badgeAnchors[3] = badgeAnchor;
			break;
		}
		UpdateBadge();
	}

	private void UpdateBadge()
	{
		if (!currentBadgeTransform)
		{
			return;
		}
		for (int i = 0; i < badgeOffsets.Length; i++)
		{
			if (badgeOffsets[i].enabled)
			{
				Matrix4x4 matrix4x = Matrix4x4.TRS(badgeDefaultPos, badgeDefaultRot, currentBadgeTransform.localScale);
				Matrix4x4 matrix = Matrix4x4.TRS(badgeOffsets[i].offset.pos, badgeOffsets[i].offset.rot, Vector3.one) * matrix4x;
				currentBadgeTransform.localRotation = matrix.rotation;
				currentBadgeTransform.localPosition = matrix.Position();
				return;
			}
		}
		GameObject[] array = badgeAnchors;
		foreach (GameObject gameObject in array)
		{
			if ((bool)gameObject)
			{
				currentBadgeTransform.localRotation = gameObject.transform.localRotation;
				currentBadgeTransform.localPosition = gameObject.transform.localPosition;
				return;
			}
		}
		ResetBadge();
	}

	private void ResetBadge()
	{
		if ((bool)currentBadgeTransform)
		{
			currentBadgeTransform.localRotation = badgeDefaultRot;
			currentBadgeTransform.localPosition = badgeDefaultPos;
		}
	}

	private void OnDestroy()
	{
		for (int i = 0; i < clippingOffsetTransforms.Length; i++)
		{
			if (!(clippingOffsetTransforms[i] != null))
			{
				continue;
			}
			foreach (Transform item in clippingOffsetTransforms[i])
			{
				item.parent = null;
			}
			UnityEngine.Object.Destroy(clippingOffsetTransforms[i].gameObject);
		}
	}
}
