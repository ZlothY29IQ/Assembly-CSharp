using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GorillaTag;
using Unity.Mathematics;
using UnityEngine;

public class PlayableBoundaryManager : MonoBehaviour
{
	public List<PlayableBoundaryTracker> tracked = new List<PlayableBoundaryTracker>(10);

	[Space]
	[Range(0f, 128f)]
	public float m_bigCylinderRadius = 8f;

	public float m_smoothFactor = 1.5f;

	public float m_smallCylindersRadius = 3f;

	[SerializeField]
	private double m_smallCylindersMoveTimeScale = 0.25;

	[Space]
	private readonly Vector4[] _cylinders_centers = new Vector4[8];

	private readonly Vector4[] _cylinders_radiusHeights = new Vector4[8];

	private static ShaderHashId _GTGameModes_PlayableBoundary_Cylinders_Centers = "_GTGameModes_PlayableBoundary_Cylinders_Centers";

	private static ShaderHashId _GTGameModes_PlayableBoundary_Cylinders_RadiusHeights = "_GTGameModes_PlayableBoundary_Cylinders_RadiusHeights";

	private static ShaderHashId _GTGameModes_PlayableBoundary_NonZeroSmoothRadius = "_GTGameModes_PlayableBoundary_NonZeroSmoothRadius";

	private static ShaderHashId _GTGameModes_PlayableBoundary_IsEnabled = "_GTGameModes_PlayableBoundary_IsEnabled";

	private const int _k_cylinders_count = 8;

	[NonSerialized]
	public float radiusScale = 1f;

	private int _lastFrameUpdated = -1;

	private static Vector3 kHashVec = Vector3.zero;

	public static bool ShouldRender
	{
		get
		{
			return Shader.GetGlobalFloat(_GTGameModes_PlayableBoundary_IsEnabled) > 0f;
		}
		set
		{
			Shader.SetGlobalFloat(_GTGameModes_PlayableBoundary_IsEnabled, value ? 1 : 0);
		}
	}

	protected void Awake()
	{
		if (!Application.isPlaying)
		{
			base.enabled = false;
		}
	}

	public void Setup()
	{
		Shader.SetGlobalFloat(_GTGameModes_PlayableBoundary_NonZeroSmoothRadius, m_smoothFactor);
		Vector3 position = base.transform.position;
		SRand sRand = new SRand(StaticHash.Compute(position.x, position.y, position.z));
		_cylinders_centers[0] = new Vector3(position.x, position.y, position.z);
		_cylinders_radiusHeights[0] = new Vector2(m_bigCylinderRadius * radiusScale, 100f);
		for (int i = 1; i < 8; i++)
		{
			Vector3 vector = position + sRand.NextPointInsideSphere(m_bigCylinderRadius * radiusScale);
			_cylinders_centers[i] = new Vector4(vector.x, vector.y, vector.z, 0f);
			_cylinders_radiusHeights[i] = new Vector4(m_smallCylindersRadius * radiusScale, 100f, 0f, 0f);
		}
	}

	private void OnEnable()
	{
		ShouldRender = true;
		Setup();
	}

	private void OnDisable()
	{
		ShouldRender = false;
	}

	public void UpdateSim()
	{
		if (Time.frameCount == _lastFrameUpdated)
		{
			return;
		}
		_lastFrameUpdated = Time.frameCount;
		Vector4[] cylinders_centers = _cylinders_centers;
		if (cylinders_centers == null || cylinders_centers.Length != 8)
		{
			return;
		}
		cylinders_centers = _cylinders_radiusHeights;
		if (cylinders_centers == null || cylinders_centers.Length != 8)
		{
			return;
		}
		if (m_smallCylindersMoveTimeScale > 0.0)
		{
			Vector3 position = base.transform.position;
			float num = (float)((double)(GTTime.TimeAsMilliseconds() % 86400000) * m_smallCylindersMoveTimeScale / 1000.0);
			_cylinders_centers[0] = new Vector3(position.x, position.y, position.z);
			_cylinders_radiusHeights[0] = new Vector2(m_bigCylinderRadius * radiusScale, 100f);
			for (int i = 1; i < 8; i++)
			{
				float num2 = (float)i * 0.125f;
				Vector3 v = Hash3(num2 * 1.17f) + Hash3(num2 * 13.7f) * num;
				Vector3 vector = position + v.Sin() * m_bigCylinderRadius * radiusScale;
				_cylinders_centers[i] = new Vector4(vector.x, vector.y, vector.z, 0f);
				_cylinders_radiusHeights[i] = new Vector4(m_smallCylindersRadius * radiusScale, 100f, 0f, 0f);
			}
		}
		Shader.SetGlobalVectorArray(_GTGameModes_PlayableBoundary_Cylinders_Centers, _cylinders_centers);
		Shader.SetGlobalVectorArray(_GTGameModes_PlayableBoundary_Cylinders_RadiusHeights, _cylinders_radiusHeights);
		for (int j = 0; j < tracked.Count; j++)
		{
			PlayableBoundaryTracker playableBoundaryTracker = tracked[j];
			if ((bool)playableBoundaryTracker)
			{
				playableBoundaryTracker.UpdateSignedDistanceToBoundary(_GetSignedDistanceToBoundary(playableBoundaryTracker.transform.position, playableBoundaryTracker.radius), Time.deltaTime);
			}
		}
		Shader.SetGlobalFloat(_GTGameModes_PlayableBoundary_NonZeroSmoothRadius, m_smoothFactor);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float _GetSignedDistanceToBoundary(float3 tracked_center, float tracked_radius)
	{
		float num = float.MaxValue;
		float smoothFactor = GetSmoothFactor();
		for (int i = 0; i < 8; i++)
		{
			float3 float5 = ((float4)_cylinders_centers[i]).xyz - tracked_center;
			float x = _cylinders_radiusHeights[i].x;
			float signedDist = math.length(float5.xz) - x;
			num = SDFSmoothMerge(num, signedDist, smoothFactor);
		}
		return num - tracked_radius;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float SDFSmoothMerge(float signedDist1, float signedDist2, float smoothRadius)
	{
		float num = 0f - math.length(math.min(new float2(signedDist1 - smoothRadius, signedDist2 - smoothRadius), new float2(0f, 0f)));
		float num2 = math.max(math.min(signedDist1, signedDist2), smoothRadius);
		return num + num2;
	}

	private static ref Vector3 Hash3(float n)
	{
		kHashVec.x = Mathf.Sin(n) * 43758.547f % 1f;
		kHashVec.y = Mathf.Sin(n + 1f) * 22578.146f % 1f;
		kHashVec.z = Mathf.Sin(n + 2f) * 19642.35f % 1f;
		return ref kHashVec;
	}

	private float GetSmoothFactor()
	{
		float num = m_smoothFactor;
		if (m_bigCylinderRadius <= 1f)
		{
			num *= math.max(m_bigCylinderRadius, 0f);
		}
		return math.max(num, 1E-06f);
	}
}
