using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class MirrorCameraScript : MonoBehaviour
{
	public Camera mainCamera;

	public Camera mirrorCamera;

	private void Start()
	{
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
		}
		GorillaTagger.Instance.MirrorCameraCullingMask.AddCallback(MirrorCullingMaskChanged, shouldCallbackNow: true);
	}

	private void OnDestroy()
	{
		if (GorillaTagger.Instance != null)
		{
			GorillaTagger.Instance.MirrorCameraCullingMask.RemoveCallback(MirrorCullingMaskChanged);
		}
	}

	private void MirrorCullingMaskChanged(int newMask)
	{
		mirrorCamera.cullingMask = newMask;
	}

	private void LateUpdate()
	{
		Vector3 position = base.transform.position;
		Vector3 right = base.transform.right;
		Vector4 plane = new Vector4(right.x, right.y, right.z, 0f - Vector3.Dot(right, position));
		Matrix4x4 reflectionMatrix = Matrix4x4.zero;
		CalculateReflectionMatrix(ref reflectionMatrix, plane);
		mirrorCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * reflectionMatrix;
		Vector4 clipPlane = CameraSpacePlane(mirrorCamera, position, right, 1f);
		mirrorCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(clipPlane);
		Debug.Log($"Main Camera position {mainCamera.transform.position}");
		mirrorCamera.transform.position = reflectionMatrix.MultiplyPoint(mainCamera.transform.position);
		Debug.Log($"Reflected Camera position {mirrorCamera.transform.position}");
		mirrorCamera.transform.rotation = mainCamera.transform.rotation * Quaternion.Inverse(base.transform.rotation);
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			List<Material> value;
			using (CollectionPool<List<Material>, Material>.Get(out value))
			{
				renderer.GetSharedMaterials(value);
				foreach (Material item in value)
				{
					if (item.shader == Shader.Find("Reflection"))
					{
						item.SetTexture("_ReflectionTex", mirrorCamera.targetTexture);
					}
				}
			}
		}
	}

	private void CalculateReflectionMatrix(ref Matrix4x4 reflectionMatrix, Vector4 plane)
	{
		reflectionMatrix.m00 = 1f - 2f * plane[0] * plane[0];
		reflectionMatrix.m01 = -2f * plane[0] * plane[1];
		reflectionMatrix.m02 = -2f * plane[0] * plane[2];
		reflectionMatrix.m03 = -2f * plane[3] * plane[0];
		reflectionMatrix.m10 = -2f * plane[1] * plane[0];
		reflectionMatrix.m11 = 1f - 2f * plane[1] * plane[1];
		reflectionMatrix.m12 = -2f * plane[1] * plane[2];
		reflectionMatrix.m13 = -2f * plane[3] * plane[1];
		reflectionMatrix.m20 = -2f * plane[2] * plane[0];
		reflectionMatrix.m21 = -2f * plane[2] * plane[1];
		reflectionMatrix.m22 = 1f - 2f * plane[2] * plane[2];
		reflectionMatrix.m23 = -2f * plane[3] * plane[2];
		reflectionMatrix.m30 = 0f;
		reflectionMatrix.m31 = 0f;
		reflectionMatrix.m32 = 0f;
		reflectionMatrix.m33 = 1f;
	}

	private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
	{
		Vector3 point = pos + normal * 0.07f;
		Matrix4x4 worldToCameraMatrix = cam.worldToCameraMatrix;
		Vector3 lhs = worldToCameraMatrix.MultiplyPoint(point);
		Vector3 rhs = worldToCameraMatrix.MultiplyVector(normal).normalized * sideSign;
		return new Vector4(rhs.x, rhs.y, rhs.z, 0f - Vector3.Dot(lhs, rhs));
	}
}
