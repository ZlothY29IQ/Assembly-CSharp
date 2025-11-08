using System;
using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

[Serializable]
public class SlotTransformOverride
{
	[Obsolete("(2024-08-20 MattO) Cosmetics use xformOffsets now which fills in the appropriate data for this component. If you are doing something weird then `overrideTransformMatrix` must be used instead. This will probably be removed after 2024-09-15.")]
	public Transform overrideTransform;

	[Obsolete("(2024-08-20 MattO) Cosmetics use xformOffsets now which fills in the appropriate data for this component. If you are doing something weird then `overrideTransformMatrix` must be used instead. This will probably be removed after 2024-09-15.")]
	[Delayed]
	public string overrideTransform_path;

	public TransferrableObject.PositionState positionState;

	public bool useAdvancedGrab;

	public Matrix4x4 overrideTransformMatrix = Matrix4x4.identity;

	public Transform advancedGrabPointAnchor;

	public Transform advancedGrabPointOrigin;

	[SerializeReference]
	public List<SubGrabPoint> multiPoints = new List<SubGrabPoint>();

	public Matrix4x4 AdvOriginLocalToParentAnchorLocal;

	public Matrix4x4 AdvAnchorLocalToAdvOriginLocal;

	private XformOffset _EdXformOffsetRepresenationOf_overrideTransformMatrix
	{
		get
		{
			return new XformOffset(overrideTransformMatrix);
		}
		set
		{
			overrideTransformMatrix = Matrix4x4.TRS(value.pos, value.rot, value.scale);
		}
	}

	public void Initialize(Component component, Transform anchor)
	{
		if (!useAdvancedGrab)
		{
			return;
		}
		AdvOriginLocalToParentAnchorLocal = anchor.worldToLocalMatrix * advancedGrabPointOrigin.localToWorldMatrix;
		AdvAnchorLocalToAdvOriginLocal = advancedGrabPointOrigin.worldToLocalMatrix * advancedGrabPointAnchor.localToWorldMatrix;
		foreach (SubGrabPoint multiPoint in multiPoints)
		{
			if (multiPoint == null)
			{
				break;
			}
			multiPoint.InitializePoints(anchor, advancedGrabPointAnchor, advancedGrabPointOrigin);
		}
	}

	public void AddLineButton()
	{
		multiPoints.Add(new SubLineGrabPoint());
	}

	public void AddSubGrabPoint(TransferrableObjectGripPosition togp)
	{
		SubGrabPoint item = togp.CreateSubGrabPoint(this);
		multiPoints.Add(item);
	}
}
