using System;
using System.Collections.Generic;
using CjLib;
using GorillaNetworking;
using UnityEngine;

public class MoonController : MonoBehaviour
{
	public enum Scenes
	{
		Forest,
		Bayou,
		Beach,
		Canyon,
		Clouds,
		City,
		Metropolis,
		Mountain
	}

	[Serializable]
	public struct SceneData
	{
		public Scenes scene;

		public Transform referencePoint;

		public bool overridePlacement;

		public Placement PlacementOverride;
	}

	[Serializable]
	public struct Placement
	{
		public Vector2 radiusRange;

		public Vector2 heightRange;

		public Vector2 scaleRange;

		public float restAngle;
	}

	[SerializeField]
	private List<SceneData> scenes = new List<SceneData>();

	[SerializeField]
	private Scenes activeScene;

	[SerializeField]
	private Placement defaultPlacement;

	[SerializeField]
	[Range(0f, 1f)]
	private float distance;

	[SerializeField]
	private bool alwaysInTheSky;

	[Header("Model Swap")]
	[SerializeField]
	private Transform defaultMoon;

	[SerializeField]
	private Transform openMoon;

	[Header("Animation")]
	[SerializeField]
	private Animator openMoonAnimator;

	[SerializeField]
	private float eyeOpenDistThreshold = 0.9f;

	[SerializeField]
	private float eyeCloseDistThreshold = 0.05f;

	[Header("Debug")]
	[SerializeField]
	private bool debugOverrideTimeOfDay;

	[SerializeField]
	[Range(0f, 4f)]
	private float timeOfDayOverride;

	[SerializeField]
	private bool debugOverrideCrackProgress;

	[SerializeField]
	[Range(0f, 1f)]
	private float crackProgress;

	[SerializeField]
	private bool debugOverrideCrackDayInOctober;

	[SerializeField]
	[Range(1f, 31f)]
	private int crackDayInOctoberOverride = 4;

	[SerializeField]
	private MeshRenderer crackRenderer;

	private int crackStartDayOfYear;

	private int crackEndDayOfYear;

	private float orbitAngle;

	private int eyeOpenHash;

	private bool openEyeModelEnabled;

	private float currentlySetCrackProgress;

	private MaterialPropertyBlock crackMaterialPropertyBlock;

	private bool debugDrawOrbit;

	private Dictionary<GTZone, Scenes> zoneToSceneMapping = new Dictionary<GTZone, Scenes>();

	private const float moonFallStart = 13f / 150f;

	private const float moonFallEnd = 0.22f;

	private const float moonRiseStart = 0.53999996f;

	private const float moonRiseEnd = 0.6733333f;

	public float Distance => distance;

	private float TimeOfDay
	{
		get
		{
			if (debugOverrideTimeOfDay)
			{
				return Mathf.Repeat(timeOfDayOverride, 1f);
			}
			if (!(BetterDayNightManager.instance != null))
			{
				return 1f;
			}
			return BetterDayNightManager.instance.NormalizedTimeOfDay;
		}
	}

	public void SetEyeOpenAnimation()
	{
		openMoonAnimator.SetBool(eyeOpenHash, value: true);
	}

	public void StartEyeCloseAnimation()
	{
		openMoonAnimator.SetBool(eyeOpenHash, value: false);
	}

	private void Start()
	{
		eyeOpenHash = Animator.StringToHash("EyeOpen");
		zoneToSceneMapping.Add(GTZone.forest, Scenes.Forest);
		zoneToSceneMapping.Add(GTZone.city, Scenes.City);
		zoneToSceneMapping.Add(GTZone.basement, Scenes.City);
		zoneToSceneMapping.Add(GTZone.canyon, Scenes.Canyon);
		zoneToSceneMapping.Add(GTZone.beach, Scenes.Beach);
		zoneToSceneMapping.Add(GTZone.mountain, Scenes.Mountain);
		zoneToSceneMapping.Add(GTZone.skyJungle, Scenes.Clouds);
		zoneToSceneMapping.Add(GTZone.cave, Scenes.Forest);
		zoneToSceneMapping.Add(GTZone.cityWithSkyJungle, Scenes.City);
		zoneToSceneMapping.Add(GTZone.tutorial, Scenes.Forest);
		zoneToSceneMapping.Add(GTZone.rotating, Scenes.Forest);
		zoneToSceneMapping.Add(GTZone.none, Scenes.Forest);
		zoneToSceneMapping.Add(GTZone.Metropolis, Scenes.Metropolis);
		zoneToSceneMapping.Add(GTZone.cityNoBuildings, Scenes.City);
		zoneToSceneMapping.Add(GTZone.attic, Scenes.Forest);
		zoneToSceneMapping.Add(GTZone.arcade, Scenes.City);
		zoneToSceneMapping.Add(GTZone.bayou, Scenes.Bayou);
		if (ZoneManagement.instance != null)
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
		}
		if (GreyZoneManager.Instance != null)
		{
			GreyZoneManager.Instance.RegisterMoon(this);
		}
		crackStartDayOfYear = new DateTime(2024, 10, 4).DayOfYear;
		crackEndDayOfYear = new DateTime(2024, 10, 25).DayOfYear;
		if (crackRenderer != null)
		{
			currentlySetCrackProgress = 1f;
			crackMaterialPropertyBlock = new MaterialPropertyBlock();
			crackRenderer.GetPropertyBlock(crackMaterialPropertyBlock);
			crackMaterialPropertyBlock.SetFloat(ShaderProps._Progress, currentlySetCrackProgress);
			crackRenderer.SetPropertyBlock(crackMaterialPropertyBlock);
		}
		orbitAngle = 0f;
		UpdateCrack();
		UpdatePlacement();
	}

	private void OnDestroy()
	{
		if (GreyZoneManager.Instance != null)
		{
			GreyZoneManager.Instance.UnregisterMoon(this);
		}
	}

	private void OnZoneChanged()
	{
		ZoneManagement instance = ZoneManagement.instance;
		Scenes scenes = Scenes.Forest;
		for (int i = 0; i < instance.activeZones.Count; i++)
		{
			if (zoneToSceneMapping.TryGetValue(instance.activeZones[i], out var value) && value > scenes)
			{
				scenes = value;
			}
		}
		UpdateActiveScene(scenes);
	}

	private void UpdateActiveScene(Scenes nextScene)
	{
		activeScene = nextScene;
		UpdateCrack();
		UpdatePlacement();
	}

	private void Update()
	{
		UpdateCrack();
		if (!alwaysInTheSky)
		{
			float timeOfDay = TimeOfDay;
			bool num = timeOfDay > 0.53999996f && timeOfDay < 0.6733333f;
			bool flag = timeOfDay > 13f / 150f && timeOfDay < 0.22f;
			bool flag2 = timeOfDay <= 13f / 150f || timeOfDay >= 0.6733333f;
			float num2 = orbitAngle;
			num2 = (num ? Mathf.Lerp(MathF.PI, 0f, (timeOfDay - 0.53999996f) / 0.13333333f) : (flag ? Mathf.Lerp(0f, -MathF.PI, (timeOfDay - 13f / 150f) / 0.13333333f) : ((!flag2) ? MathF.PI : 0f)));
			if (orbitAngle != num2)
			{
				orbitAngle = num2;
				UpdateCrack();
				UpdatePlacement();
			}
		}
	}

	public void UpdateDistance(float nextDistance)
	{
		distance = nextDistance;
		UpdateVisualState();
		UpdatePlacement();
	}

	public void UpdateVisualState()
	{
		bool flag = false;
		if (GreyZoneManager.Instance != null)
		{
			flag = GreyZoneManager.Instance.GreyZoneActive;
		}
		if (flag && openEyeModelEnabled && distance < eyeOpenDistThreshold && !openMoonAnimator.GetBool(eyeOpenHash))
		{
			openMoonAnimator.SetBool(eyeOpenHash, value: true);
		}
		else if (!flag && distance > eyeCloseDistThreshold && openMoonAnimator.GetBool(eyeOpenHash))
		{
			openMoonAnimator.SetBool(eyeOpenHash, value: false);
		}
	}

	public void UpdatePlacement()
	{
		if (alwaysInTheSky)
		{
			UpdatePlacementSimple();
		}
		else
		{
			UpdatePlacementOrbit();
		}
	}

	private void UpdatePlacementSimple()
	{
		SceneData sceneData = scenes[(int)activeScene];
		Transform referencePoint = sceneData.referencePoint;
		Placement placement = (sceneData.overridePlacement ? sceneData.PlacementOverride : defaultPlacement);
		float num = Mathf.Lerp(placement.heightRange.x, placement.heightRange.y, distance);
		float num2 = Mathf.Lerp(placement.radiusRange.x, placement.radiusRange.y, distance);
		float num3 = Mathf.Lerp(placement.scaleRange.x, placement.scaleRange.y, distance);
		float restAngle = placement.restAngle;
		Vector3 position = referencePoint.position;
		position.y += num;
		position.x += num2 * Mathf.Cos(restAngle * (MathF.PI / 180f));
		position.z += num2 * Mathf.Sin(restAngle * (MathF.PI / 180f));
		base.transform.position = position;
		base.transform.rotation = Quaternion.LookRotation(referencePoint.position - base.transform.position);
		base.transform.localScale = Vector3.one * num3;
	}

	public void UpdatePlacementOrbit()
	{
		SceneData sceneData = scenes[(int)activeScene];
		Transform referencePoint = sceneData.referencePoint;
		Placement placement = (sceneData.overridePlacement ? sceneData.PlacementOverride : defaultPlacement);
		float y = placement.heightRange.y;
		float y2 = placement.radiusRange.y;
		Vector3 position = referencePoint.position;
		position.y += y;
		position.x += y2 * Mathf.Cos(placement.restAngle * (MathF.PI / 180f));
		position.z += y2 * Mathf.Sin(placement.restAngle * (MathF.PI / 180f));
		float num = Mathf.Sqrt(y * y + y2 * y2);
		float num2 = Mathf.Atan2(y, y2);
		Quaternion quaternion = Quaternion.AngleAxis(57.29578f * num2, Vector3.Cross(position - referencePoint.position, Vector3.up));
		float f = placement.restAngle * (MathF.PI / 180f) + orbitAngle;
		Vector3 vector = referencePoint.position + quaternion * new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f)) * num;
		if (distance < 1f)
		{
			Vector3 position2 = referencePoint.position;
			position2.y += placement.heightRange.x;
			position2.x += placement.radiusRange.x * Mathf.Cos(placement.restAngle * (MathF.PI / 180f));
			position2.z += placement.radiusRange.x * Mathf.Sin(placement.restAngle * (MathF.PI / 180f));
			vector = ((!(Mathf.Abs(orbitAngle) < 0.9424779f)) ? Vector3.Lerp(position2, position, distance) : Vector3.Lerp(position2, vector, distance));
		}
		base.transform.position = vector;
		base.transform.rotation = Quaternion.LookRotation(referencePoint.position - base.transform.position);
		base.transform.localScale = Vector3.one * Mathf.Lerp(placement.scaleRange.x, placement.scaleRange.y, distance);
		if (debugDrawOrbit)
		{
			int num3 = 32;
			float timeOfDay = TimeOfDay;
			float num4 = 13f / 150f;
			float num5 = 37f / 150f;
			float num6 = 19f / 30f;
			float num7 = 0.76f;
			bool flag = timeOfDay > num4 && timeOfDay < num5;
			bool num8 = timeOfDay > num6 && timeOfDay < num7;
			bool flag2 = timeOfDay <= num4 || timeOfDay >= num7;
			Color color = (num8 ? Color.red : (flag2 ? Color.green : (flag ? Color.yellow : Color.blue)));
			Vector3 v = referencePoint.position + quaternion * new Vector3(Mathf.Cos(0f), 0f, Mathf.Sin(0f)) * num;
			for (int i = 1; i <= num3; i++)
			{
				float num9 = (float)i / (float)num3;
				Vector3 vector2 = referencePoint.position + quaternion * new Vector3(Mathf.Cos(MathF.PI * 2f * num9), 0f, Mathf.Sin(MathF.PI * 2f * num9)) * num;
				DebugUtil.DrawLine(v, vector2, color, depthTest: false);
				v = vector2;
			}
		}
	}

	private void UpdateCrack()
	{
		bool flag = GreyZoneManager.Instance != null && GreyZoneManager.Instance.GreyZoneAvailable;
		if (flag && !openEyeModelEnabled)
		{
			openEyeModelEnabled = true;
			defaultMoon.gameObject.SetActive(value: false);
			openMoon.gameObject.SetActive(value: true);
		}
		else if (!flag && openEyeModelEnabled)
		{
			openEyeModelEnabled = false;
			defaultMoon.gameObject.SetActive(value: true);
			openMoon.gameObject.SetActive(value: false);
		}
		if (!flag && GorillaComputer.instance != null)
		{
			DateTime dateTime = GorillaComputer.instance.GetServerTime();
			if (debugOverrideCrackDayInOctober)
			{
				dateTime = new DateTime(2024, 10, Mathf.Clamp(crackDayInOctoberOverride, 1, 31));
			}
			float value = Mathf.InverseLerp(crackStartDayOfYear, crackEndDayOfYear, dateTime.DayOfYear);
			if (debugOverrideCrackProgress)
			{
				value = crackProgress;
			}
			float num = 1f - Mathf.Clamp01(value);
			if (crackRenderer != null && Mathf.Abs(num - currentlySetCrackProgress) > Mathf.Epsilon)
			{
				currentlySetCrackProgress = num;
				crackMaterialPropertyBlock.SetFloat(ShaderProps._Progress, currentlySetCrackProgress);
				crackRenderer.SetPropertyBlock(crackMaterialPropertyBlock);
			}
		}
	}
}
