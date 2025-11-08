using System;
using CjLib;
using GorillaTag;
using UnityEngine;

namespace GorillaLocomotion.Swimming;

[ExecuteAlways]
public class UnderwaterCameraEffect : MonoBehaviour
{
	private enum CameraOverlapWaterState
	{
		Uninitialized,
		OutOfWater,
		PartiallySubmerged,
		FullySubmerged
	}

	private const float edgeBuffer = 0.04f;

	[SerializeField]
	private Camera targetCamera;

	[SerializeField]
	private MeshRenderer planeRenderer;

	[SerializeField]
	private UnderwaterParticleEffects underwaterParticleEffect;

	[SerializeField]
	private float distanceFromCamera = 0.02f;

	[SerializeField]
	[DebugOption]
	private bool debugDraw;

	private float cachedAspectRatio = 1f;

	private float cachedFov = 90f;

	private readonly Vector3[] frustumPlaneCornersLocal = new Vector3[4];

	private Vector2 frustumPlaneExtents;

	private GTPlayer player;

	private WaterVolume.SurfaceQuery waterSurface;

	private const string kShaderKeyword_GlobalCameraTouchingWater = "_GLOBAL_CAMERA_TOUCHING_WATER";

	private const string kShaderKeyword_GlobalCameraFullyUnderwater = "_GLOBAL_CAMERA_FULLY_UNDERWATER";

	private int shaderParam_GlobalCameraOverlapWaterSurfacePlane = Shader.PropertyToID("_GlobalCameraOverlapWaterSurfacePlane");

	private bool hasTargetCamera;

	[DebugReadout]
	private CameraOverlapWaterState cameraOverlapWaterState;

	private void SetOffScreenPosition()
	{
		base.transform.localScale = new Vector3(2f * (frustumPlaneExtents.x + 0.04f), 0f, 1f);
		base.transform.localPosition = new Vector3(0f, 0f - (frustumPlaneExtents.y + 0.04f), distanceFromCamera);
	}

	private void SetFullScreenPosition()
	{
		base.transform.localScale = new Vector3(2f * (frustumPlaneExtents.x + 0.04f), 2f * (frustumPlaneExtents.y + 0.04f), 1f);
		base.transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
	}

	private void OnEnable()
	{
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}
		hasTargetCamera = targetCamera != null;
		InitializeShaderProperties();
	}

	private void Start()
	{
		player = GTPlayer.Instance;
		cachedAspectRatio = targetCamera.aspect;
		cachedFov = targetCamera.fieldOfView;
		CalculateFrustumPlaneBounds(cachedFov, cachedAspectRatio);
		SetOffScreenPosition();
	}

	private void LateUpdate()
	{
		if (!hasTargetCamera || !player)
		{
			return;
		}
		if (player.HeadOverlappingWaterVolumes.Count < 1)
		{
			SetCameraOverlapState(CameraOverlapWaterState.OutOfWater);
			if (planeRenderer.enabled)
			{
				planeRenderer.enabled = false;
				SetOffScreenPosition();
			}
			if (underwaterParticleEffect != null && underwaterParticleEffect.gameObject.activeInHierarchy)
			{
				underwaterParticleEffect.UpdateParticleEffect(waterSurfaceDetected: false, ref waterSurface);
			}
			return;
		}
		if (targetCamera.aspect != cachedAspectRatio || targetCamera.fieldOfView != cachedFov)
		{
			cachedAspectRatio = targetCamera.aspect;
			cachedFov = targetCamera.fieldOfView;
			CalculateFrustumPlaneBounds(cachedFov, cachedAspectRatio);
		}
		bool flag = false;
		float num = float.MinValue;
		Vector3 position = targetCamera.transform.position;
		for (int i = 0; i < player.HeadOverlappingWaterVolumes.Count; i++)
		{
			if (player.HeadOverlappingWaterVolumes[i].GetSurfaceQueryForPoint(position, out var result))
			{
				float num2 = Vector3.Dot(result.surfacePoint - position, result.surfaceNormal);
				if (num2 > num)
				{
					flag = true;
					num = num2;
					waterSurface = result;
				}
			}
		}
		if (flag)
		{
			Vector3 inPoint = targetCamera.transform.InverseTransformPoint(waterSurface.surfacePoint);
			Vector3 inNormal = targetCamera.transform.InverseTransformDirection(waterSurface.surfaceNormal);
			Plane p = new Plane(inNormal, inPoint);
			Plane p2 = new Plane(Vector3.forward, 0f - distanceFromCamera);
			if (IntersectPlanes(p2, p, out var point, out var direction))
			{
				Vector3 normalized = Vector3.Cross(direction, Vector3.forward).normalized;
				float num3 = Vector3.Dot(new Vector3(point.x, point.y, 0f), normalized);
				if (num3 > frustumPlaneExtents.y + 0.04f)
				{
					SetFullScreenPosition();
					SetCameraOverlapState(CameraOverlapWaterState.FullySubmerged);
				}
				else if (num3 < 0f - (frustumPlaneExtents.y + 0.04f))
				{
					SetOffScreenPosition();
					SetCameraOverlapState(CameraOverlapWaterState.OutOfWater);
				}
				else
				{
					float num4 = num3;
					num4 += GetFrustumCoverageDistance(-normalized) + 0.04f;
					float num5 = GetFrustumCoverageDistance(direction) + 0.04f;
					num5 += GetFrustumCoverageDistance(-direction) + 0.04f;
					base.transform.localScale = new Vector3(num5, num4, 1f);
					base.transform.localPosition = normalized * (num3 - num4 * 0.5f) + new Vector3(0f, 0f, distanceFromCamera);
					float angle = Vector3.SignedAngle(Vector3.up, normalized, Vector3.forward);
					base.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
					SetCameraOverlapState(CameraOverlapWaterState.PartiallySubmerged);
				}
				if (debugDraw)
				{
					Vector3 vector = targetCamera.transform.TransformPoint(point);
					Vector3 vector2 = targetCamera.transform.TransformDirection(direction);
					DebugUtil.DrawLine(vector - 2f * frustumPlaneExtents.x * vector2, vector + 2f * frustumPlaneExtents.x * vector2, Color.white, depthTest: false);
				}
			}
			else if (new Plane(waterSurface.surfaceNormal, waterSurface.surfacePoint).GetSide(targetCamera.transform.position))
			{
				SetFullScreenPosition();
				SetCameraOverlapState(CameraOverlapWaterState.FullySubmerged);
			}
			else
			{
				SetOffScreenPosition();
				SetCameraOverlapState(CameraOverlapWaterState.OutOfWater);
			}
		}
		else
		{
			SetOffScreenPosition();
			SetCameraOverlapState(CameraOverlapWaterState.OutOfWater);
		}
		if (underwaterParticleEffect != null && underwaterParticleEffect.gameObject.activeInHierarchy)
		{
			underwaterParticleEffect.UpdateParticleEffect(flag, ref waterSurface);
		}
	}

	[DebugOption]
	private void InitializeShaderProperties()
	{
		Shader.DisableKeyword("_GLOBAL_CAMERA_TOUCHING_WATER");
		Shader.DisableKeyword("_GLOBAL_CAMERA_FULLY_UNDERWATER");
		float w = 0f - Vector3.Dot(waterSurface.surfaceNormal, waterSurface.surfacePoint);
		Shader.SetGlobalVector(shaderParam_GlobalCameraOverlapWaterSurfacePlane, new Vector4(waterSurface.surfaceNormal.x, waterSurface.surfaceNormal.y, waterSurface.surfaceNormal.z, w));
	}

	private void SetCameraOverlapState(CameraOverlapWaterState state)
	{
		if (state != cameraOverlapWaterState || state == CameraOverlapWaterState.Uninitialized)
		{
			cameraOverlapWaterState = state;
			switch (cameraOverlapWaterState)
			{
			case CameraOverlapWaterState.Uninitialized:
			case CameraOverlapWaterState.OutOfWater:
				Shader.DisableKeyword("_GLOBAL_CAMERA_TOUCHING_WATER");
				Shader.DisableKeyword("_GLOBAL_CAMERA_FULLY_UNDERWATER");
				break;
			case CameraOverlapWaterState.PartiallySubmerged:
				Shader.EnableKeyword("_GLOBAL_CAMERA_TOUCHING_WATER");
				Shader.DisableKeyword("_GLOBAL_CAMERA_FULLY_UNDERWATER");
				break;
			case CameraOverlapWaterState.FullySubmerged:
				Shader.EnableKeyword("_GLOBAL_CAMERA_TOUCHING_WATER");
				Shader.EnableKeyword("_GLOBAL_CAMERA_FULLY_UNDERWATER");
				break;
			}
		}
		if (cameraOverlapWaterState == CameraOverlapWaterState.PartiallySubmerged)
		{
			Plane plane = new Plane(waterSurface.surfaceNormal, waterSurface.surfacePoint);
			Shader.SetGlobalVector(shaderParam_GlobalCameraOverlapWaterSurfacePlane, new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance));
		}
	}

	private void CalculateFrustumPlaneBounds(float fieldOfView, float aspectRatio)
	{
		float num = Mathf.Tan(MathF.PI / 180f * fieldOfView * 0.5f) * distanceFromCamera;
		float num2 = aspectRatio * num + 0.04f;
		float num3 = 1f / aspectRatio * num + 0.04f;
		frustumPlaneExtents = new Vector2(num2, num3);
		frustumPlaneCornersLocal[0] = new Vector3(0f - num2, 0f - num3, distanceFromCamera);
		frustumPlaneCornersLocal[1] = new Vector3(0f - num2, num3, distanceFromCamera);
		frustumPlaneCornersLocal[2] = new Vector3(num2, num3, distanceFromCamera);
		frustumPlaneCornersLocal[3] = new Vector3(num2, 0f - num3, distanceFromCamera);
	}

	private bool IntersectPlanes(Plane p1, Plane p2, out Vector3 point, out Vector3 direction)
	{
		direction = Vector3.Cross(p1.normal, p2.normal);
		float num = Vector3.Dot(direction, direction);
		if (num < Mathf.Epsilon)
		{
			point = Vector3.zero;
			return false;
		}
		point = Vector3.Cross(direction, p1.distance * p2.normal - p2.distance * p1.normal) / num;
		return true;
	}

	private float GetFrustumCoverageDistance(Vector3 localDirection)
	{
		float num = float.MinValue;
		for (int i = 0; i < frustumPlaneCornersLocal.Length; i++)
		{
			float num2 = Vector3.Dot(frustumPlaneCornersLocal[i], localDirection);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}
}
