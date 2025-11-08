using System;
using GorillaExtensions;
using UnityEngine;

[Serializable]
public class SubLineGrabPoint : SubGrabPoint
{
	public Transform startPoint;

	public Transform endPoint;

	public Vector3 startPointRelativeToGrabPointOrigin;

	public Vector3 endPointRelativeToGrabPointOrigin;

	public Matrix4x4 startPointRelativeTransformToGrabPointOrigin;

	public Matrix4x4 endPointRelativeTransformToGrabPointOrigin;

	public override Matrix4x4 GetTransformation_GripPointLocalToAdvOriginLocal(AdvancedItemState.PreData advancedItemState, SlotTransformOverride slotTransformOverride)
	{
		float distAlongLine = advancedItemState.distAlongLine;
		Vector3 pos = Vector3.Lerp(startPointRelativeTransformToGrabPointOrigin.Position(), endPointRelativeTransformToGrabPointOrigin.Position(), distAlongLine);
		Quaternion q = Quaternion.Slerp(startPointRelativeTransformToGrabPointOrigin.rotation, endPointRelativeTransformToGrabPointOrigin.rotation, distAlongLine);
		return Matrix4x4.TRS(pos, q, Vector3.one);
	}

	public override void InitializePoints(Transform anchor, Transform grabPointAnchor, Transform advancedGrabPointOrigin)
	{
		base.InitializePoints(anchor, grabPointAnchor, advancedGrabPointOrigin);
		if (!(startPoint == null) && !(endPoint == null))
		{
			startPointRelativeToGrabPointOrigin = advancedGrabPointOrigin.InverseTransformPoint(startPoint.position);
			endPointRelativeToGrabPointOrigin = advancedGrabPointOrigin.InverseTransformPoint(endPoint.position);
			endPointRelativeTransformToGrabPointOrigin = advancedGrabPointOrigin.worldToLocalMatrix * endPoint.localToWorldMatrix;
			startPointRelativeTransformToGrabPointOrigin = advancedGrabPointOrigin.worldToLocalMatrix * startPoint.localToWorldMatrix;
		}
	}

	public override AdvancedItemState.PreData GetPreData(Transform objectTransform, Transform handTransform, Transform targetDock, SlotTransformOverride slotTransformOverride)
	{
		return new AdvancedItemState.PreData
		{
			distAlongLine = FindNearestFractionOnLine(objectTransform.TransformPoint(startPointRelativeToGrabPointOrigin), objectTransform.TransformPoint(endPointRelativeToGrabPointOrigin), handTransform.position),
			pointType = AdvancedItemState.PointType.DistanceBased
		};
		static float FindNearestFractionOnLine(Vector3 origin, Vector3 end, Vector3 point)
		{
			Vector3 rhs = end - origin;
			float magnitude = rhs.magnitude;
			rhs /= magnitude;
			return Mathf.Clamp01(Vector3.Dot(point - origin, rhs) / magnitude);
		}
	}

	public override float EvaluateScore(Transform objectTransform, Transform handTransform, Transform targetDock)
	{
		float t = FindNearestFractionOnLine(objectTransform.TransformPoint(startPointRelativeToGrabPointOrigin), objectTransform.TransformPoint(endPointRelativeToGrabPointOrigin), handTransform.position);
		Vector3 vector = Vector3.Lerp(startPointRelativeTransformToGrabPointOrigin.Position(), endPointRelativeTransformToGrabPointOrigin.Position(), t);
		Vector3 vector2 = objectTransform.InverseTransformPoint(handTransform.position);
		return Vector3.SqrMagnitude(vector - vector2);
		static float FindNearestFractionOnLine(Vector3 origin, Vector3 end, Vector3 point)
		{
			Vector3 rhs = end - origin;
			float magnitude = rhs.magnitude;
			rhs /= magnitude;
			return Mathf.Clamp01(Vector3.Dot(point - origin, rhs) / magnitude);
		}
	}
}
