using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SubSplineGrabPoint : SubLineGrabPoint
{
	public CatmullRomSpline spline;

	public List<Vector3> controlPointsRelativeToGrabOrigin = new List<Vector3>();

	public List<Matrix4x4> controlPointsTransformsRelativeToGrabOrigin = new List<Matrix4x4>();

	public override Matrix4x4 GetTransformation_GripPointLocalToAdvOriginLocal(AdvancedItemState.PreData advancedItemState, SlotTransformOverride slotTransformOverride)
	{
		return CatmullRomSpline.Evaluate(controlPointsTransformsRelativeToGrabOrigin, advancedItemState.distAlongLine);
	}

	public override void InitializePoints(Transform anchor, Transform grabPointAnchor, Transform advancedGrabPointOrigin)
	{
		base.InitializePoints(anchor, grabPointAnchor, advancedGrabPointOrigin);
		controlPointsRelativeToGrabOrigin = new List<Vector3>();
		Transform[] controlPointTransforms = spline.controlPointTransforms;
		foreach (Transform transform in controlPointTransforms)
		{
			controlPointsRelativeToGrabOrigin.Add(advancedGrabPointOrigin.InverseTransformPoint(transform.position));
			controlPointsTransformsRelativeToGrabOrigin.Add(advancedGrabPointOrigin.worldToLocalMatrix * transform.localToWorldMatrix);
		}
	}

	public override AdvancedItemState.PreData GetPreData(Transform objectTransform, Transform handTransform, Transform targetDock, SlotTransformOverride slotTransformOverride)
	{
		Vector3 worldPoint = objectTransform.InverseTransformPoint(handTransform.position);
		Vector3 linePoint;
		return new AdvancedItemState.PreData
		{
			distAlongLine = CatmullRomSpline.GetClosestEvaluationOnSpline(controlPointsRelativeToGrabOrigin, worldPoint, out linePoint),
			pointType = AdvancedItemState.PointType.DistanceBased
		};
	}

	public override float EvaluateScore(Transform objectTransform, Transform handTransform, Transform targetDock)
	{
		Vector3 vector = objectTransform.InverseTransformPoint(handTransform.position);
		CatmullRomSpline.GetClosestEvaluationOnSpline(controlPointsRelativeToGrabOrigin, vector, out var linePoint);
		return Vector3.SqrMagnitude(linePoint - vector);
	}
}
