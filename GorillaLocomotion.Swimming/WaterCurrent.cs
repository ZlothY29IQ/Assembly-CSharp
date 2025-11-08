using System;
using System.Collections.Generic;
using AA;
using CjLib;
using UnityEngine;

namespace GorillaLocomotion.Swimming;

public class WaterCurrent : MonoBehaviour
{
	[SerializeField]
	private List<CatmullRomSpline> splines = new List<CatmullRomSpline>();

	[SerializeField]
	private float fullEffectDistance = 1f;

	[SerializeField]
	private float fadeDistance = 0.5f;

	[SerializeField]
	private float currentSpeed = 1f;

	[SerializeField]
	private float currentAccel = 10f;

	[SerializeField]
	private float velocityAnticipationAdjustment = 0.05f;

	[SerializeField]
	private float inwardCurrentFullEffectRadius = 1f;

	[SerializeField]
	private float inwardCurrentNoEffectRadius = 0.25f;

	[SerializeField]
	private float inwardCurrentSpeed = 1f;

	[SerializeField]
	private float inwardCurrentAccel = 10f;

	[SerializeField]
	private float dampingHalfLife = 0.25f;

	[SerializeField]
	private bool debugDrawCurrentQueries;

	private Vector3 debugCurrentVelocity = Vector3.zero;

	private Vector3 debugSplinePoint = Vector3.zero;

	public float Speed => currentSpeed;

	public float Accel => currentAccel;

	public float InwardSpeed => inwardCurrentSpeed;

	public float InwardAccel => inwardCurrentAccel;

	public bool GetCurrentAtPoint(Vector3 worldPoint, Vector3 startingVelocity, float dt, out Vector3 currentVelocity, out Vector3 velocityChange)
	{
		float num = (fullEffectDistance + fadeDistance) * (fullEffectDistance + fadeDistance);
		bool result = false;
		velocityChange = Vector3.zero;
		currentVelocity = Vector3.zero;
		float num2 = 0.0001f;
		float magnitude = startingVelocity.magnitude;
		if (magnitude > num2)
		{
			Vector3 vector = startingVelocity / magnitude;
			float num3 = Spring.DamperDecayExact(magnitude, dampingHalfLife, dt);
			Vector3 vector2 = vector * num3;
			velocityChange += vector2 - startingVelocity;
		}
		for (int i = 0; i < splines.Count; i++)
		{
			CatmullRomSpline catmullRomSpline = splines[i];
			Vector3 linePoint;
			float closestEvaluationOnSpline = catmullRomSpline.GetClosestEvaluationOnSpline(worldPoint, out linePoint);
			Vector3 vector3 = catmullRomSpline.Evaluate(closestEvaluationOnSpline);
			Vector3 vector4 = vector3 - worldPoint;
			if (!(vector4.sqrMagnitude < num))
			{
				continue;
			}
			result = true;
			float magnitude2 = vector4.magnitude;
			float num4 = ((magnitude2 > fullEffectDistance) ? (1f - Mathf.Clamp01((magnitude2 - fullEffectDistance) / fadeDistance)) : 1f);
			float t = Mathf.Clamp01(closestEvaluationOnSpline + velocityAnticipationAdjustment);
			Vector3 forwardTangent = catmullRomSpline.GetForwardTangent(t);
			if (currentSpeed > num2 && Vector3.Dot(startingVelocity, forwardTangent) < num4 * currentSpeed)
			{
				velocityChange += forwardTangent * (currentAccel * dt);
			}
			else if (currentSpeed < num2 && Vector3.Dot(startingVelocity, forwardTangent) > num4 * currentSpeed)
			{
				velocityChange -= forwardTangent * (currentAccel * dt);
			}
			currentVelocity += forwardTangent * num4 * currentSpeed;
			float num5 = Mathf.InverseLerp(inwardCurrentNoEffectRadius, inwardCurrentFullEffectRadius, magnitude2);
			if (num5 > num2)
			{
				linePoint = Vector3.ProjectOnPlane(vector4, forwardTangent);
				Vector3 normalized = linePoint.normalized;
				if (inwardCurrentSpeed > num2 && Vector3.Dot(startingVelocity, normalized) < num5 * inwardCurrentSpeed)
				{
					velocityChange += normalized * (InwardAccel * dt);
				}
				else if (inwardCurrentSpeed < num2 && Vector3.Dot(startingVelocity, normalized) > num5 * inwardCurrentSpeed)
				{
					velocityChange -= normalized * (InwardAccel * dt);
				}
			}
			debugSplinePoint = vector3;
		}
		debugCurrentVelocity = velocityChange.normalized;
		return result;
	}

	private void Update()
	{
		if (debugDrawCurrentQueries)
		{
			DebugUtil.DrawSphere(debugSplinePoint, 0.15f, 12, 12, Color.green, depthTest: false);
			DebugUtil.DrawArrow(debugSplinePoint, debugSplinePoint + debugCurrentVelocity, 0.1f, 0.1f, 12, 0.1f, Color.yellow, depthTest: false);
		}
	}

	private void OnDrawGizmosSelected()
	{
		int num = 16;
		for (int i = 0; i < splines.Count; i++)
		{
			CatmullRomSpline catmullRomSpline = splines[i];
			Vector3 vector = catmullRomSpline.Evaluate(0f);
			for (int j = 1; j <= num; j++)
			{
				float t = (float)j / (float)num;
				Vector3 vector2 = catmullRomSpline.Evaluate(t);
				_ = vector2 - vector;
				Quaternion rotation = Quaternion.LookRotation(catmullRomSpline.GetForwardTangent(t), Vector3.up);
				Gizmos.color = new Color(0f, 0.5f, 0.75f);
				DrawGizmoCircle(vector2, rotation, fullEffectDistance);
				Gizmos.color = new Color(0f, 0.25f, 0.5f);
				DrawGizmoCircle(vector2, rotation, fullEffectDistance + fadeDistance);
			}
		}
	}

	private void DrawGizmoCircle(Vector3 center, Quaternion rotation, float radius)
	{
		Vector3 vector = Vector3.right * radius;
		int num = 16;
		for (int i = 1; i <= num; i++)
		{
			float f = (float)i / (float)num * 2f * MathF.PI;
			Vector3 vector2 = new Vector3(Mathf.Cos(f), Mathf.Sin(f), 0f) * radius;
			Gizmos.DrawLine(center + rotation * vector, center + rotation * vector2);
			vector = vector2;
		}
	}
}
