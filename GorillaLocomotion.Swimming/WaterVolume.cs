using System;
using System.Collections.Generic;
using CjLib;
using GorillaLocomotion.Climbing;
using GorillaTag.GuidedRefs;
using GorillaTagScripts;
using GT_CustomMapSupportRuntime;
using UnityEngine;

namespace GorillaLocomotion.Swimming;

[RequireComponent(typeof(Collider))]
public class WaterVolume : BaseGuidedRefTargetMono, ITickSystemTick
{
	public struct SurfaceQuery
	{
		public Vector3 surfacePoint;

		public Vector3 surfaceNormal;

		public float maxDepth;

		public Plane surfacePlane => new Plane(surfaceNormal, surfacePoint);
	}

	public delegate void WaterVolumeEvent(WaterVolume volume, Collider collider);

	[SerializeField]
	public Transform surfacePlane;

	[SerializeField]
	private List<MeshCollider> surfaceColliders = new List<MeshCollider>();

	[SerializeField]
	public List<Collider> volumeColliders = new List<Collider>();

	[SerializeField]
	private GTPlayer.LiquidType liquidType;

	[SerializeField]
	private WaterCurrent waterCurrent;

	[SerializeField]
	private WaterParameters waterParams;

	[SerializeField]
	[Tooltip("The water volume be placed in the scene (not spawned) and not moved for this to be true")]
	public bool isStationary = true;

	[SerializeField]
	[Tooltip("Check scale of monke entering")]
	public bool isMonkeblock;

	public const string WaterSplashRPC = "RPC_PlaySplashEffect";

	public static float[] splashRPCSendTimes = new float[4];

	private static Dictionary<Collider, WaterVolume> sharedColliderRegistry = new Dictionary<Collider, WaterVolume>(16);

	private static Dictionary<Mesh, int[]> meshTrianglesDict = new Dictionary<Mesh, int[]>(16);

	private static Dictionary<Mesh, Vector3[]> meshVertsDict = new Dictionary<Mesh, Vector3[]>(16);

	private int[] sharedMeshTris;

	private Vector3[] sharedMeshVerts;

	private VRRig playerVRRig;

	private float volumeMaxHeight;

	private float volumeMinHeight;

	private bool debugDrawSurfaceCast;

	private Collider triggerCollider;

	private List<WaterOverlappingCollider> persistentColliders = new List<WaterOverlappingCollider>(16);

	private GuidedRefTargetIdSO _guidedRefTargetId;

	private UnityEngine.Object _guidedRefTargetObject;

	public bool TickRunning { get; set; }

	public GTPlayer.LiquidType LiquidType => liquidType;

	public WaterCurrent Current => waterCurrent;

	public WaterParameters Parameters => waterParams;

	private VRRig PlayerVRRig
	{
		get
		{
			if (playerVRRig == null)
			{
				GorillaTagger instance = GorillaTagger.Instance;
				if (instance != null)
				{
					playerVRRig = instance.offlineVRRig;
				}
			}
			return playerVRRig;
		}
	}

	public event WaterVolumeEvent ColliderEnteredVolume;

	public event WaterVolumeEvent ColliderExitedVolume;

	public event WaterVolumeEvent ColliderEnteredWater;

	public event WaterVolumeEvent ColliderExitedWater;

	public bool GetSurfaceQueryForPoint(Vector3 point, out SurfaceQuery result, bool debugDraw = false)
	{
		result = default(SurfaceQuery);
		if (!isStationary)
		{
			float num = float.MinValue;
			float num2 = float.MaxValue;
			for (int i = 0; i < volumeColliders.Count; i++)
			{
				float y = volumeColliders[i].bounds.max.y;
				float y2 = volumeColliders[i].bounds.min.y;
				if (y > num)
				{
					num = y;
				}
				if (y2 < num2)
				{
					num2 = y2;
				}
			}
			volumeMaxHeight = num;
			volumeMinHeight = num2;
		}
		Ray ray = new Ray(new Vector3(point.x, volumeMaxHeight, point.z), Vector3.down);
		Ray ray2 = new Ray(new Vector3(point.x, volumeMinHeight, point.z), Vector3.up);
		float num3 = volumeMaxHeight - volumeMinHeight;
		float num4 = float.MinValue;
		float num5 = float.MaxValue;
		bool flag = false;
		bool flag2 = false;
		float num6 = 0f;
		for (int j = 0; j < surfaceColliders.Count; j++)
		{
			bool flag3 = surfaceColliders[j].enabled;
			surfaceColliders[j].enabled = true;
			if (surfaceColliders[j].Raycast(ray, out var hitInfo, num3) && hitInfo.point.y > num4 && HitOutsideSurfaceOfMesh(ray.direction, surfaceColliders[j], hitInfo))
			{
				num4 = hitInfo.point.y;
				flag = true;
				result.surfacePoint = hitInfo.point;
				result.surfaceNormal = hitInfo.normal;
			}
			if (surfaceColliders[j].Raycast(ray2, out var hitInfo2, num3) && hitInfo2.point.y < num5 && HitOutsideSurfaceOfMesh(ray2.direction, surfaceColliders[j], hitInfo2))
			{
				num5 = hitInfo2.point.y;
				flag2 = true;
				num6 = hitInfo2.point.y;
			}
			surfaceColliders[j].enabled = flag3;
		}
		if (!flag && surfacePlane != null)
		{
			flag = true;
			result.surfacePoint = point - Vector3.Dot(point - surfacePlane.position, surfacePlane.up) * surfacePlane.up;
			result.surfaceNormal = surfacePlane.up;
		}
		if (flag && flag2)
		{
			result.maxDepth = result.surfacePoint.y - num6;
		}
		else if (flag)
		{
			result.maxDepth = result.surfacePoint.y - volumeMinHeight;
		}
		else
		{
			result.maxDepth = volumeMaxHeight - volumeMinHeight;
		}
		if (debugDraw)
		{
			if (flag)
			{
				DebugUtil.DrawLine(ray.origin, ray.origin + ray.direction * num3, Color.green, depthTest: false);
				DebugUtil.DrawSphere(result.surfacePoint, 0.001f, 12, 12, Color.green, depthTest: false, DebugUtil.Style.SolidColor);
			}
			else
			{
				DebugUtil.DrawLine(ray.origin, ray.origin + ray.direction * num3, Color.red, depthTest: false);
			}
			if (flag2)
			{
				DebugUtil.DrawLine(ray2.origin, ray2.origin + ray2.direction * num3, Color.yellow, depthTest: false);
				DebugUtil.DrawSphere(new Vector3(result.surfacePoint.x, num6, result.surfacePoint.z), 0.001f, 12, 12, Color.yellow, depthTest: false, DebugUtil.Style.SolidColor);
			}
		}
		return flag;
	}

	private bool HitOutsideSurfaceOfMesh(Vector3 castDir, MeshCollider meshCollider, RaycastHit hit)
	{
		if (!meshTrianglesDict.TryGetValue(meshCollider.sharedMesh, out sharedMeshTris))
		{
			sharedMeshTris = (int[])meshCollider.sharedMesh.triangles.Clone();
			meshTrianglesDict.Add(meshCollider.sharedMesh, sharedMeshTris);
		}
		if (!meshVertsDict.TryGetValue(meshCollider.sharedMesh, out sharedMeshVerts))
		{
			sharedMeshVerts = (Vector3[])meshCollider.sharedMesh.vertices.Clone();
			meshVertsDict.Add(meshCollider.sharedMesh, sharedMeshVerts);
		}
		Vector3 vector = sharedMeshVerts[sharedMeshTris[hit.triangleIndex * 3]];
		Vector3 vector2 = sharedMeshVerts[sharedMeshTris[hit.triangleIndex * 3 + 1]];
		Vector3 vector3 = sharedMeshVerts[sharedMeshTris[hit.triangleIndex * 3 + 2]];
		Vector3 vector4 = meshCollider.transform.TransformDirection(Vector3.Cross(vector2 - vector, vector3 - vector).normalized);
		bool flag = Vector3.Dot(castDir, vector4) < 0f;
		if (debugDrawSurfaceCast)
		{
			Color color = (flag ? Color.blue : Color.red);
			DebugUtil.DrawLine(hit.point, hit.point + vector4 * 0.3f, color, depthTest: false);
		}
		return flag;
	}

	private void DebugDrawMeshColliderHitTriangle(RaycastHit hit)
	{
		MeshCollider meshCollider = hit.collider as MeshCollider;
		if (meshCollider != null)
		{
			Mesh sharedMesh = meshCollider.sharedMesh;
			int[] triangles = sharedMesh.triangles;
			Vector3[] vertices = sharedMesh.vertices;
			Vector3 vector = meshCollider.gameObject.transform.TransformPoint(vertices[triangles[hit.triangleIndex * 3]]);
			Vector3 vector2 = meshCollider.gameObject.transform.TransformPoint(vertices[triangles[hit.triangleIndex * 3 + 1]]);
			Vector3 vector3 = meshCollider.gameObject.transform.TransformPoint(vertices[triangles[hit.triangleIndex * 3 + 2]]);
			Vector3 normalized = Vector3.Cross(vector2 - vector, vector3 - vector).normalized;
			float num = 0.2f;
			DebugUtil.DrawLine(vector, vector + normalized * num, Color.blue, depthTest: false);
			DebugUtil.DrawLine(vector2, vector2 + normalized * num, Color.blue, depthTest: false);
			DebugUtil.DrawLine(vector3, vector3 + normalized * num, Color.blue, depthTest: false);
			DebugUtil.DrawLine(vector, vector2, Color.blue, depthTest: false);
			DebugUtil.DrawLine(vector, vector3, Color.blue, depthTest: false);
			DebugUtil.DrawLine(vector2, vector3, Color.blue, depthTest: false);
		}
	}

	public bool RaycastWater(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance, int layerMask)
	{
		if (triggerCollider != null)
		{
			return Physics.Raycast(new Ray(origin, direction), out hit, distance, layerMask, QueryTriggerInteraction.Collide);
		}
		hit = default(RaycastHit);
		return false;
	}

	public bool CheckColliderInVolume(Collider collider, out bool inWater, out bool surfaceDetected)
	{
		for (int i = 0; i < persistentColliders.Count; i++)
		{
			if (persistentColliders[i].collider == collider)
			{
				inWater = persistentColliders[i].inWater;
				surfaceDetected = persistentColliders[i].surfaceDetected;
				return true;
			}
		}
		inWater = false;
		surfaceDetected = false;
		return false;
	}

	protected override void Awake()
	{
		base.Awake();
		RefreshColliders();
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	public void RefreshColliders()
	{
		triggerCollider = GetComponent<Collider>();
		if (volumeColliders == null || volumeColliders.Count < 1)
		{
			volumeColliders = new List<Collider>();
			volumeColliders.Add(base.gameObject.GetComponent<Collider>());
		}
		float num = float.MinValue;
		float num2 = float.MaxValue;
		for (int i = 0; i < volumeColliders.Count; i++)
		{
			float y = volumeColliders[i].bounds.max.y;
			float y2 = volumeColliders[i].bounds.min.y;
			if (y > num)
			{
				num = y;
			}
			if (y2 < num2)
			{
				num2 = y2;
			}
		}
		volumeMaxHeight = num;
		volumeMinHeight = num2;
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			for (int i = 0; i < persistentColliders.Count; i++)
			{
				WaterOverlappingCollider value = persistentColliders[i];
				value.inVolume = false;
				value.playDripEffect = false;
				this.ColliderExitedVolume?.Invoke(this, value.collider);
				persistentColliders[i] = value;
			}
			RemoveCollidersOutsideVolume(Time.time);
			TickSystem<object>.RemoveTickCallback(this);
		}
	}

	public void Tick()
	{
		if (persistentColliders.Count < 1)
		{
			return;
		}
		float time = Time.time;
		RemoveCollidersOutsideVolume(time);
		if (!CanPlayerSwim())
		{
			return;
		}
		for (int i = 0; i < persistentColliders.Count; i++)
		{
			WaterOverlappingCollider persistentCollider = persistentColliders[i];
			bool inWater = persistentCollider.inWater;
			if (persistentCollider.inVolume)
			{
				CheckColliderAgainstWater(ref persistentCollider, time);
			}
			else
			{
				persistentCollider.inWater = false;
			}
			TryRegisterOwnershipOfCollider(persistentCollider.collider, persistentCollider.inWater, persistentCollider.surfaceDetected);
			if (persistentCollider.inWater && !inWater)
			{
				OnWaterSurfaceEnter(ref persistentCollider);
			}
			else if (!persistentCollider.inWater && inWater)
			{
				OnWaterSurfaceExit(ref persistentCollider, time);
			}
			if (HasOwnershipOfCollider(persistentCollider.collider) && persistentCollider.surfaceDetected)
			{
				if (!persistentCollider.inWater)
				{
					ColliderOutOfWaterUpdate(ref persistentCollider, time);
				}
				else
				{
					ColliderInWaterUpdate(ref persistentCollider, time);
				}
			}
			persistentColliders[i] = persistentCollider;
		}
	}

	private void RemoveCollidersOutsideVolume(float currentTime)
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		for (int num = persistentColliders.Count - 1; num >= 0; num--)
		{
			WaterOverlappingCollider waterOverlappingCollider = persistentColliders[num];
			if (waterOverlappingCollider.collider == null || !waterOverlappingCollider.collider.gameObject.activeInHierarchy || (!waterOverlappingCollider.inVolume && (!waterOverlappingCollider.playDripEffect || currentTime - waterOverlappingCollider.lastInWaterTime > waterParams.postExitDripDuration)) || !CanPlayerSwim())
			{
				UnregisterOwnershipOfCollider(waterOverlappingCollider.collider);
				GTPlayer instance = GTPlayer.Instance;
				if (waterOverlappingCollider.collider == instance.headCollider || waterOverlappingCollider.collider == instance.bodyCollider)
				{
					instance.OnExitWaterVolume(waterOverlappingCollider.collider, this);
				}
				persistentColliders.RemoveAt(num);
			}
		}
	}

	private void CheckColliderAgainstWater(ref WaterOverlappingCollider persistentCollider, float currentTime)
	{
		Vector3 position = persistentCollider.collider.transform.position;
		bool flag = true;
		if (persistentCollider.surfaceDetected && persistentCollider.scaleMultiplier > 0.99f && isStationary)
		{
			flag = (position - Vector3.Dot(position - persistentCollider.lastSurfaceQuery.surfacePoint, persistentCollider.lastSurfaceQuery.surfaceNormal) * persistentCollider.lastSurfaceQuery.surfaceNormal - persistentCollider.lastSurfaceQuery.surfacePoint).sqrMagnitude > waterParams.recomputeSurfaceForColliderDist * waterParams.recomputeSurfaceForColliderDist;
		}
		if (flag)
		{
			if (GetSurfaceQueryForPoint(position, out var result, debugDrawSurfaceCast))
			{
				persistentCollider.surfaceDetected = true;
				persistentCollider.lastSurfaceQuery = result;
			}
			else
			{
				persistentCollider.surfaceDetected = false;
				persistentCollider.lastSurfaceQuery = default(SurfaceQuery);
			}
		}
		if (persistentCollider.surfaceDetected)
		{
			Vector3 obj = ((persistentCollider.collider is MeshCollider) ? persistentCollider.collider.ClosestPointOnBounds(position + Vector3.down * 10f) : persistentCollider.collider.ClosestPoint(position + Vector3.down * 10f));
			bool flag2 = obj.y < persistentCollider.lastSurfaceQuery.surfacePoint.y;
			Vector3 obj2 = ((persistentCollider.collider is MeshCollider) ? persistentCollider.collider.ClosestPointOnBounds(position + Vector3.up * 10f) : persistentCollider.collider.ClosestPoint(position + Vector3.up * 10f));
			bool flag3 = obj2.y > persistentCollider.lastSurfaceQuery.surfacePoint.y - persistentCollider.lastSurfaceQuery.maxDepth;
			persistentCollider.inWater = flag2 && flag3;
		}
		else
		{
			persistentCollider.inWater = false;
		}
		if (persistentCollider.inWater)
		{
			persistentCollider.lastInWaterTime = currentTime;
		}
	}

	private Vector3 GetColliderVelocity(ref WaterOverlappingCollider persistentCollider)
	{
		GTPlayer instance = GTPlayer.Instance;
		Vector3 result = Vector3.one * (waterParams.splashSpeedRequirement + 0.1f);
		if (persistentCollider.velocityTracker != null)
		{
			result = persistentCollider.velocityTracker.GetAverageVelocity(worldSpace: true, 0.1f);
		}
		else if (persistentCollider.collider == instance.headCollider || persistentCollider.collider == instance.bodyCollider)
		{
			result = instance.AveragedVelocity;
		}
		else if (persistentCollider.collider.attachedRigidbody != null && !persistentCollider.collider.attachedRigidbody.isKinematic)
		{
			result = persistentCollider.collider.attachedRigidbody.linearVelocity;
		}
		return result;
	}

	private void OnWaterSurfaceEnter(ref WaterOverlappingCollider persistentCollider)
	{
		this.ColliderEnteredWater?.Invoke(this, persistentCollider.collider);
		GTPlayer instance = GTPlayer.Instance;
		if (persistentCollider.collider == instance.headCollider || persistentCollider.collider == instance.bodyCollider)
		{
			instance.OnEnterWaterVolume(persistentCollider.collider, this);
		}
		if (HasOwnershipOfCollider(persistentCollider.collider))
		{
			Vector3 colliderVelocity = GetColliderVelocity(ref persistentCollider);
			bool flag = Vector3.Dot(colliderVelocity, -persistentCollider.lastSurfaceQuery.surfaceNormal) > waterParams.splashSpeedRequirement * persistentCollider.scaleMultiplier;
			bool flag2 = Vector3.Dot(colliderVelocity, -persistentCollider.lastSurfaceQuery.surfaceNormal) > waterParams.bigSplashSpeedRequirement * persistentCollider.scaleMultiplier;
			persistentCollider.PlayRippleEffect(waterParams.rippleEffect, persistentCollider.lastSurfaceQuery.surfacePoint, persistentCollider.lastSurfaceQuery.surfaceNormal, waterParams.rippleEffectScale, Time.time, this);
			if (waterParams.playSplashEffect && flag && (flag2 || !persistentCollider.playBigSplash))
			{
				persistentCollider.PlaySplashEffect(waterParams.splashEffect, persistentCollider.lastRipplePosition, waterParams.splashEffectScale, persistentCollider.playBigSplash && flag2, enteringWater: true, this);
			}
		}
	}

	private void OnWaterSurfaceExit(ref WaterOverlappingCollider persistentCollider, float currentTime)
	{
		this.ColliderExitedWater?.Invoke(this, persistentCollider.collider);
		persistentCollider.nextDripTime = currentTime + waterParams.perDripTimeDelay + UnityEngine.Random.Range((0f - waterParams.perDripTimeRandRange) * 0.5f, waterParams.perDripTimeRandRange * 0.5f);
		GTPlayer instance = GTPlayer.Instance;
		if (persistentCollider.collider == instance.headCollider || persistentCollider.collider == instance.bodyCollider)
		{
			instance.OnExitWaterVolume(persistentCollider.collider, this);
		}
		if (HasOwnershipOfCollider(persistentCollider.collider))
		{
			float num = Vector3.Dot(GetColliderVelocity(ref persistentCollider), persistentCollider.lastSurfaceQuery.surfaceNormal);
			bool flag = num > waterParams.splashSpeedRequirement * persistentCollider.scaleMultiplier;
			bool flag2 = num > waterParams.bigSplashSpeedRequirement * persistentCollider.scaleMultiplier;
			persistentCollider.PlayRippleEffect(waterParams.rippleEffect, persistentCollider.lastSurfaceQuery.surfacePoint, persistentCollider.lastSurfaceQuery.surfaceNormal, waterParams.rippleEffectScale, Time.time, this);
			if (waterParams.playSplashEffect && flag && (flag2 || !persistentCollider.playBigSplash))
			{
				persistentCollider.PlaySplashEffect(waterParams.splashEffect, persistentCollider.lastRipplePosition, waterParams.splashEffectScale, persistentCollider.playBigSplash && flag2, enteringWater: false, this);
			}
		}
	}

	private void ColliderOutOfWaterUpdate(ref WaterOverlappingCollider persistentCollider, float currentTime)
	{
		if (currentTime < persistentCollider.lastInWaterTime + waterParams.postExitDripDuration && currentTime > persistentCollider.nextDripTime && persistentCollider.playDripEffect)
		{
			persistentCollider.nextDripTime = currentTime + waterParams.perDripTimeDelay + UnityEngine.Random.Range((0f - waterParams.perDripTimeRandRange) * 0.5f, waterParams.perDripTimeRandRange * 0.5f);
			float dripScale = waterParams.rippleEffectScale * 2f * (waterParams.perDripDefaultRadius + UnityEngine.Random.Range((0f - waterParams.perDripRadiusRandRange) * 0.5f, waterParams.perDripRadiusRandRange * 0.5f));
			persistentCollider.PlayDripEffect(waterParams.rippleEffect, persistentCollider.lastSurfaceQuery.surfacePoint, persistentCollider.lastSurfaceQuery.surfaceNormal, dripScale);
		}
	}

	private void ColliderInWaterUpdate(ref WaterOverlappingCollider persistentCollider, float currentTime)
	{
		Vector3 vector = Vector3.ProjectOnPlane(persistentCollider.collider.transform.position - persistentCollider.lastSurfaceQuery.surfacePoint, persistentCollider.lastSurfaceQuery.surfaceNormal) + persistentCollider.lastSurfaceQuery.surfacePoint;
		bool flag = false;
		if ((!persistentCollider.overrideBoundingRadius) ? ((persistentCollider.collider.ClosestPointOnBounds(vector) - vector).sqrMagnitude < 0.001f) : ((persistentCollider.collider.transform.position - vector).sqrMagnitude < persistentCollider.boundingRadiusOverride * persistentCollider.boundingRadiusOverride))
		{
			float num = Mathf.Max(waterParams.minDistanceBetweenRipples, waterParams.defaultDistanceBetweenRipples * (persistentCollider.lastRippleScale / waterParams.rippleEffectScale));
			bool num2 = (persistentCollider.lastRipplePosition - vector).sqrMagnitude > num * num;
			bool flag2 = currentTime - persistentCollider.lastRippleTime > waterParams.minTimeBetweenRipples;
			if (num2 || flag2)
			{
				persistentCollider.PlayRippleEffect(waterParams.rippleEffect, persistentCollider.lastSurfaceQuery.surfacePoint, persistentCollider.lastSurfaceQuery.surfaceNormal, waterParams.rippleEffectScale, currentTime, this);
			}
		}
		else
		{
			persistentCollider.lastRippleTime = currentTime;
		}
	}

	private void TryRegisterOwnershipOfCollider(Collider collider, bool isInWater, bool isSurfaceDetected)
	{
		if (sharedColliderRegistry.TryGetValue(collider, out var value))
		{
			if (value != this)
			{
				value.CheckColliderInVolume(collider, out var inWater, out var surfaceDetected);
				if ((isSurfaceDetected && !surfaceDetected) || (isInWater && !inWater))
				{
					sharedColliderRegistry.Remove(collider);
					sharedColliderRegistry.Add(collider, this);
				}
			}
		}
		else
		{
			sharedColliderRegistry.Add(collider, this);
		}
	}

	private void UnregisterOwnershipOfCollider(Collider collider)
	{
		if (sharedColliderRegistry.ContainsKey(collider))
		{
			sharedColliderRegistry.Remove(collider);
		}
	}

	private bool HasOwnershipOfCollider(Collider collider)
	{
		if (sharedColliderRegistry.TryGetValue(collider, out var value))
		{
			return value == this;
		}
		return false;
	}

	protected virtual bool CanPlayerSwim()
	{
		if (isMonkeblock && PlayerVRRig != null)
		{
			if (PlayerVRRig.scaleFactor < 0.5f)
			{
				return true;
			}
			if (BuilderTable.TryGetBuilderTableForZone(PlayerVRRig.zoneEntity.currentZone, out var table))
			{
				return !table.isTableMutable;
			}
		}
		return true;
	}

	public void OnTriggerEnter(Collider other)
	{
		if (!CanPlayerSwim())
		{
			return;
		}
		GorillaTriggerColliderHandIndicator component = other.GetComponent<GorillaTriggerColliderHandIndicator>();
		if (other.isTrigger && component == null)
		{
			return;
		}
		this.ColliderEnteredVolume?.Invoke(this, other);
		for (int i = 0; i < persistentColliders.Count; i++)
		{
			if (persistentColliders[i].collider == other)
			{
				WaterOverlappingCollider value = persistentColliders[i];
				value.inVolume = true;
				persistentColliders[i] = value;
				return;
			}
		}
		WaterOverlappingCollider item = new WaterOverlappingCollider
		{
			collider = other
		};
		item.inVolume = true;
		item.lastInWaterTime = Time.time - waterParams.postExitDripDuration - 10f;
		WaterSplashOverride component2 = other.GetComponent<WaterSplashOverride>();
		if (component2 != null)
		{
			if (component2.suppressWaterEffects)
			{
				return;
			}
			item.playBigSplash = component2.playBigSplash;
			item.playDripEffect = component2.playDrippingEffect;
			item.overrideBoundingRadius = component2.overrideBoundingRadius;
			item.boundingRadiusOverride = component2.boundingRadiusOverride;
			item.scaleMultiplier = (component2.scaleByPlayersScale ? GTPlayer.Instance.scale : 1f);
		}
		else
		{
			if (other.GetComponent<BuilderPieceCollider>() != null)
			{
				return;
			}
			item.playDripEffect = true;
			item.overrideBoundingRadius = false;
			item.scaleMultiplier = 1f;
			item.playBigSplash = false;
		}
		GTPlayer instance = GTPlayer.Instance;
		if (component != null)
		{
			item.velocityTracker = instance.GetHandVelocityTracker(component.isLeftHand);
			item.scaleMultiplier = instance.scale;
		}
		else
		{
			item.velocityTracker = other.GetComponent<GorillaVelocityTracker>();
		}
		if (PlayerVRRig != null && waterParams.sendSplashEffectRPCs && (component != null || item.collider == instance.headCollider || item.collider == instance.bodyCollider))
		{
			item.photonViewForRPC = PlayerVRRig.netView;
		}
		persistentColliders.Add(item);
	}

	private void OnTriggerExit(Collider other)
	{
		if (!CanPlayerSwim())
		{
			return;
		}
		GorillaTriggerColliderHandIndicator component = other.GetComponent<GorillaTriggerColliderHandIndicator>();
		if (other.isTrigger && component == null)
		{
			return;
		}
		this.ColliderExitedVolume?.Invoke(this, other);
		for (int i = 0; i < persistentColliders.Count; i++)
		{
			if (persistentColliders[i].collider == other)
			{
				WaterOverlappingCollider value = persistentColliders[i];
				value.inVolume = false;
				persistentColliders[i] = value;
			}
		}
	}

	public void SetPropertiesFromPlaceholder(WaterVolumeProperties properties, List<Collider> waterVolumeColliders, WaterParameters parameters)
	{
		surfacePlane = properties.surfacePlane;
		surfaceColliders = properties.surfaceColliders;
		volumeColliders = waterVolumeColliders;
		liquidType = (GTPlayer.LiquidType)Math.Clamp((int)(properties.liquidType - 1), 0, 1);
		waterParams = parameters;
	}
}
