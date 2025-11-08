using CjLib;
using UnityEngine;

namespace GorillaLocomotion.Swimming;

public class UnderwaterParticleEffects : MonoBehaviour
{
	public ParticleSystem underwaterFloaterParticles;

	public ParticleSystem underwaterBubbleParticles;

	public Camera playerCamera;

	public Vector3 floaterParticleBoxExtents = Vector3.one;

	public Vector3 floaterParticleBaseOffset = Vector3.forward;

	public AnimationCurve floaterSpeedVsOffsetDist = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public Vector2 floaterSpeedVsOffsetDistMinMax = new Vector2(0f, 1f);

	private bool debugDraw;

	public void UpdateParticleEffect(bool waterSurfaceDetected, ref WaterVolume.SurfaceQuery waterSurface)
	{
		GTPlayer instance = GTPlayer.Instance;
		Plane plane = new Plane(waterSurface.surfaceNormal, waterSurface.surfacePoint);
		if (waterSurfaceDetected && plane.GetDistanceToPoint(instance.headCollider.transform.position) < instance.headCollider.radius)
		{
			underwaterFloaterParticles.gameObject.SetActive(value: true);
			Vector3 averagedVelocity = instance.AveragedVelocity;
			float magnitude = averagedVelocity.magnitude;
			Vector3 vector = ((magnitude > 0.001f) ? (averagedVelocity / magnitude) : playerCamera.transform.forward);
			float num = floaterSpeedVsOffsetDist.Evaluate(Mathf.Clamp(magnitude, floaterSpeedVsOffsetDistMinMax.x, floaterSpeedVsOffsetDistMinMax.y));
			Quaternion rotation = playerCamera.transform.rotation;
			Vector3 vector2 = playerCamera.transform.position + playerCamera.transform.rotation * floaterParticleBaseOffset + vector * num;
			Vector3 vector3 = vector2 + rotation * new Vector3(0f, floaterParticleBoxExtents.y, 0f - floaterParticleBoxExtents.z);
			Vector3 vector4 = vector2 + rotation * new Vector3(0f, floaterParticleBoxExtents.y, floaterParticleBoxExtents.z);
			float num2 = floaterParticleBoxExtents.z * 2f;
			float num3 = plane.GetDistanceToPoint(vector3);
			float num4 = plane.GetDistanceToPoint(vector4);
			Quaternion quaternion = rotation;
			Vector3 vector5 = vector2;
			if (num3 > 0f || num4 > 0f)
			{
				if (vector3.y < vector4.y)
				{
					if (num3 > 0f)
					{
						vector3 -= plane.normal * num3;
						num3 = 0f;
					}
					Vector3 rhs = (new Vector3(vector4.x, vector3.y, vector4.z) - vector3).normalized * num2;
					Vector3 axis = Vector3.Cross(vector4 - vector3, rhs);
					quaternion = Quaternion.AngleAxis((Mathf.Asin((vector4.y - vector3.y) / num2) - Mathf.Asin((0f - num3) / num2)) * 57.29578f, axis) * playerCamera.transform.rotation;
					vector5 = vector3 + quaternion * new Vector3(0f, 0f - floaterParticleBoxExtents.y, floaterParticleBoxExtents.z);
				}
				else
				{
					if (num4 > 0f)
					{
						vector4 -= plane.normal * num4;
						num4 = 0f;
					}
					Vector3 rhs2 = (new Vector3(vector3.x, vector4.y, vector3.z) - vector4).normalized * num2;
					Vector3 axis2 = Vector3.Cross(vector3 - vector4, rhs2);
					quaternion = Quaternion.AngleAxis((Mathf.Asin((vector3.y - vector4.y) / num2) - Mathf.Asin((0f - num4) / num2)) * 57.29578f, axis2) * playerCamera.transform.rotation;
					vector5 = vector4 + quaternion * new Vector3(0f, 0f - floaterParticleBoxExtents.y, 0f - floaterParticleBoxExtents.z);
				}
			}
			if (IsValid(vector5))
			{
				underwaterFloaterParticles.transform.rotation = quaternion;
				underwaterFloaterParticles.transform.position = vector5;
			}
			else
			{
				underwaterFloaterParticles.gameObject.SetActive(value: false);
			}
			if (debugDraw)
			{
				vector3 = vector2 + rotation * new Vector3(0f, floaterParticleBoxExtents.y, 0f - floaterParticleBoxExtents.z);
				vector4 = vector2 + rotation * new Vector3(0f, floaterParticleBoxExtents.y, floaterParticleBoxExtents.z);
				DebugUtil.DrawSphere(vector3, 0.1f, 12, 12, Color.red, depthTest: false, DebugUtil.Style.SolidColor);
				DebugUtil.DrawSphere(vector4, 0.1f, 12, 12, Color.red, depthTest: false, DebugUtil.Style.SolidColor);
				DebugUtil.DrawLine(vector3, vector4, Color.red, depthTest: false);
				vector3 = vector5 + quaternion * new Vector3(0f, floaterParticleBoxExtents.y, 0f - floaterParticleBoxExtents.z);
				vector4 = vector5 + quaternion * new Vector3(0f, floaterParticleBoxExtents.y, floaterParticleBoxExtents.z);
				DebugUtil.DrawSphere(vector3, 0.1f, 12, 12, Color.green, depthTest: false, DebugUtil.Style.SolidColor);
				DebugUtil.DrawSphere(vector4, 0.1f, 12, 12, Color.green, depthTest: false, DebugUtil.Style.SolidColor);
				DebugUtil.DrawLine(vector3, vector4, Color.green, depthTest: false);
			}
		}
		else
		{
			underwaterFloaterParticles.gameObject.SetActive(value: false);
		}
	}

	private bool IsValid(Vector3 vector)
	{
		if (!float.IsNaN(vector.x) && !float.IsNaN(vector.y))
		{
			return !float.IsNaN(vector.z);
		}
		return false;
	}
}
