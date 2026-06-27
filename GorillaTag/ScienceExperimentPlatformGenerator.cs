using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaTag.GuidedRefs;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag;

public class ScienceExperimentPlatformGenerator : MonoBehaviourPun, ITickSystemPost, IGuidedRefReceiverMono, IGuidedRefMonoBehaviour, IGuidedRefObject
{
	private struct BubbleData
	{
		public Vector3 position;

		public Vector3 direction;

		public float spawnSize;

		public float lifetime;

		public double spawnTime;

		public bool isTrail;

		public SodaBubble bubble;
	}

	private struct BubbleSpawnDebug
	{
		public Vector3 initialPosition;

		public Vector3 initialDirection;

		public Vector3 spawnPosition;

		public float minAngle;

		public float maxAngle;

		public float edgeCorrectionAngle;

		public double spawnTime;
	}

	[SerializeField]
	private GameObject spawnedPrefab;

	[SerializeField]
	private float scaleFactor = 0.03f;

	[Header("Random Bubbles")]
	[SerializeField]
	private Vector2 surfaceRadiusSpawnRange = new Vector2(0.1f, 0.7f);

	[SerializeField]
	private Vector2 lifetimeRange = new Vector2(5f, 10f);

	[SerializeField]
	private Vector2 sizeRange = new Vector2(0.5f, 2f);

	[SerializeField]
	private AnimationCurve rockCountVsLavaProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	[FormerlySerializedAs("rockCountMultiplier")]
	private float bubbleCountMultiplier = 80f;

	[SerializeField]
	private int maxBubbleCount = 100;

	[SerializeField]
	private AnimationCurve rockLifetimeMultiplierVsLavaProgress = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[SerializeField]
	private AnimationCurve rockMaxSizeMultiplierVsLavaProgress = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[SerializeField]
	private AnimationCurve spawnRadiusMultiplierVsLavaProgress = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[SerializeField]
	private AnimationCurve rockSizeVsLifetime = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Header("Bubble Trails")]
	[SerializeField]
	private AnimationCurve trailSpawnRateVsProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float trailSpawnRateMultiplier = 1f;

	[SerializeField]
	private AnimationCurve trailBubbleLifetimeVsProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve trailBubbleBoundaryRadiusVsProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float trailBubbleLifetimeMultiplier = 6f;

	[SerializeField]
	private float trailDistanceBetweenSpawns = 3f;

	[SerializeField]
	private float trailMaxTurnAngle = 55f;

	[SerializeField]
	private float trailBubbleSize = 1.5f;

	[SerializeField]
	private AnimationCurve trailCountVsProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float trailCountMultiplier = 12f;

	[SerializeField]
	private Vector2 trailEdgeAvoidanceSpawnsMinMax = new Vector2(3f, 1f);

	[Header("Feedback Effects")]
	[SerializeField]
	private float bubblePopAnticipationTime = 2f;

	[SerializeField]
	private float bubblePopWobbleFrequency = 25f;

	[SerializeField]
	private float bubblePopWobbleAmplitude = 0.01f;

	[SerializeField]
	private Transform liquidSurfacePlane;

	[SerializeField]
	private GuidedRefReceiverFieldInfo liquidSurfacePlane_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: true);

	private List<BubbleData> activeBubbles = new List<BubbleData>();

	private List<BubbleData> trailHeads = new List<BubbleData>();

	private List<BubbleSpawnDebug> bubbleSpawnDebug = new List<BubbleSpawnDebug>();

	private ScienceExperimentManager scienceExperimentManager;

	bool ITickSystemPost.PostTickRunning { get; set; }

	int IGuidedRefReceiverMono.GuidedRefsWaitingToResolveCount { get; set; }

	private void Awake()
	{
		((IGuidedRefObject)this).GuidedRefInitialize();
		scienceExperimentManager = GetComponent<ScienceExperimentManager>();
	}

	private void OnEnable()
	{
		if (((IGuidedRefReceiverMono)this).GuidedRefsWaitingToResolveCount <= 0)
		{
			TickSystem<object>.AddPostTickCallback(this);
		}
	}

	protected void OnDisable()
	{
		TickSystem<object>.RemovePostTickCallback(this);
	}

	void ITickSystemPost.PostTick()
	{
		double currentTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.unscaledTimeAsDouble);
		UpdateTrails(currentTime);
		RemoveExpiredBubbles(currentTime);
		SpawnNewBubbles(currentTime);
		UpdateActiveBubbles(currentTime);
	}

	private void RemoveExpiredBubbles(double currentTime)
	{
		for (int num = activeBubbles.Count - 1; num >= 0; num--)
		{
			if (Mathf.Clamp01((float)(currentTime - activeBubbles[num].spawnTime) / activeBubbles[num].lifetime) >= 1f)
			{
				activeBubbles[num].bubble.Pop();
				activeBubbles.RemoveAt(num);
			}
		}
	}

	private void SpawnNewBubbles(double currentTime)
	{
		if (!base.photonView.IsMine || scienceExperimentManager.GameState != ScienceExperimentManager.RisingLiquidState.Rising)
		{
			return;
		}
		int num = Mathf.Min((int)(rockCountVsLavaProgress.Evaluate(scienceExperimentManager.RiseProgressLinear) * bubbleCountMultiplier), maxBubbleCount) - activeBubbles.Count;
		if (activeBubbles.Count < maxBubbleCount)
		{
			for (int i = 0; i < num; i++)
			{
				SpawnRockAuthority(currentTime, scienceExperimentManager.RiseProgressLinear);
			}
		}
	}

	private void UpdateActiveBubbles(double currentTime)
	{
		if (liquidSurfacePlane == null)
		{
			return;
		}
		float y = liquidSurfacePlane.transform.position.y;
		float num = bubblePopWobbleAmplitude * Mathf.Sin(bubblePopWobbleFrequency * 0.5f * MathF.PI * Time.time);
		for (int i = 0; i < activeBubbles.Count; i++)
		{
			BubbleData value = activeBubbles[i];
			float time = Mathf.Clamp01((float)(currentTime - value.spawnTime) / value.lifetime);
			float num2 = value.spawnSize * rockSizeVsLifetime.Evaluate(time) * scaleFactor;
			value.position.y = y;
			value.bubble.body.gameObject.transform.localScale = Vector3.one * num2;
			value.bubble.body.MovePosition(value.position);
			float num3 = (float)((double)value.lifetime + value.spawnTime - currentTime);
			if (num3 < bubblePopAnticipationTime)
			{
				float num4 = Mathf.Clamp01(1f - num3 / bubblePopAnticipationTime);
				value.bubble.bubbleMesh.transform.localScale = Vector3.one * (1f + num4 * num);
			}
			activeBubbles[i] = value;
		}
	}

	private void UpdateTrails(double currentTime)
	{
		if (!base.photonView.IsMine)
		{
			return;
		}
		int num = (int)(trailCountVsProgress.Evaluate(scienceExperimentManager.RiseProgressLinear) * trailCountMultiplier) - trailHeads.Count;
		if (num > 0 && scienceExperimentManager.GameState == ScienceExperimentManager.RisingLiquidState.Rising)
		{
			for (int i = 0; i < num; i++)
			{
				SpawnTrailAuthority(currentTime, scienceExperimentManager.RiseProgressLinear);
			}
		}
		else if (num < 0)
		{
			for (int num2 = 0; num2 > num; num2--)
			{
				trailHeads.RemoveAt(0);
			}
		}
		float num3 = trailSpawnRateVsProgress.Evaluate(scienceExperimentManager.RiseProgressLinear) * trailSpawnRateMultiplier;
		float num4 = trailBubbleBoundaryRadiusVsProgress.Evaluate(scienceExperimentManager.RiseProgressLinear) * surfaceRadiusSpawnRange.y;
		for (int num5 = trailHeads.Count - 1; num5 >= 0; num5--)
		{
			if ((float)(currentTime - trailHeads[num5].spawnTime) > num3)
			{
				float num6 = 0f - trailMaxTurnAngle;
				float num7 = trailMaxTurnAngle;
				float num8 = Vector3.SignedAngle(trailHeads[num5].direction, trailHeads[num5].position - liquidSurfacePlane.transform.position, Vector3.up);
				float num9 = num4 - Vector3.Distance(trailHeads[num5].position, liquidSurfacePlane.transform.position);
				if (num9 < trailEdgeAvoidanceSpawnsMinMax.x * trailDistanceBetweenSpawns * scaleFactor)
				{
					float num10 = Mathf.InverseLerp(trailEdgeAvoidanceSpawnsMinMax.x * trailDistanceBetweenSpawns * scaleFactor, trailEdgeAvoidanceSpawnsMinMax.y * trailDistanceBetweenSpawns * scaleFactor, num9);
					if (num8 > 0f)
					{
						float b = num8 - 90f * num10;
						num7 = Mathf.Min(num7, b);
						num6 = Mathf.Min(num6, num7 - trailMaxTurnAngle);
					}
					else
					{
						float b2 = num8 + 90f * num10;
						num6 = Mathf.Max(num6, b2);
						num7 = Mathf.Max(num7, num6 + trailMaxTurnAngle);
					}
				}
				Vector3 vector = Quaternion.AngleAxis(UnityEngine.Random.Range(num6, num7), Vector3.up) * trailHeads[num5].direction;
				Vector3 vector2 = trailHeads[num5].position + vector * trailDistanceBetweenSpawns * scaleFactor - liquidSurfacePlane.transform.position;
				if (vector2.sqrMagnitude > surfaceRadiusSpawnRange.y * surfaceRadiusSpawnRange.y)
				{
					vector2 = vector2.normalized * surfaceRadiusSpawnRange.y;
				}
				Vector2 vector3 = new Vector2(vector2.x, vector2.z);
				float num11 = trailBubbleSize;
				float num12 = trailBubbleLifetimeVsProgress.Evaluate(scienceExperimentManager.RiseProgressLinear) * trailBubbleLifetimeMultiplier;
				trailHeads.RemoveAt(num5);
				base.photonView.RPC("SpawnSodaBubbleRPC", RpcTarget.Others, vector3, num11, num12, currentTime);
				SpawnSodaBubbleLocal(vector3, num11, num12, currentTime, addAsTrail: true, vector);
			}
		}
	}

	private void SpawnRockAuthority(double currentTime, float lavaProgress)
	{
		if (base.photonView.IsMine)
		{
			float num = rockLifetimeMultiplierVsLavaProgress.Evaluate(lavaProgress);
			float num2 = rockMaxSizeMultiplierVsLavaProgress.Evaluate(lavaProgress);
			float num3 = UnityEngine.Random.Range(lifetimeRange.x, lifetimeRange.y) * num;
			float num4 = UnityEngine.Random.Range(sizeRange.x, sizeRange.y * num2);
			float num5 = spawnRadiusMultiplierVsLavaProgress.Evaluate(lavaProgress);
			Vector2 inputPosition = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(surfaceRadiusSpawnRange.x, surfaceRadiusSpawnRange.y) * num5;
			inputPosition = GetSpawnPositionWithClearance(inputPosition, num4 * scaleFactor, surfaceRadiusSpawnRange.y, liquidSurfacePlane.transform.position);
			base.photonView.RPC("SpawnSodaBubbleRPC", RpcTarget.Others, inputPosition, num4, num3, currentTime);
			SpawnSodaBubbleLocal(inputPosition, num4, num3, currentTime);
		}
	}

	private void SpawnTrailAuthority(double currentTime, float lavaProgress)
	{
		if (base.photonView.IsMine)
		{
			float num = trailBubbleLifetimeVsProgress.Evaluate(scienceExperimentManager.RiseProgressLinear) * trailBubbleLifetimeMultiplier;
			float num2 = trailBubbleSize;
			Vector2 inputPosition = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(surfaceRadiusSpawnRange.x, surfaceRadiusSpawnRange.y);
			inputPosition = GetSpawnPositionWithClearance(inputPosition, num2 * scaleFactor, surfaceRadiusSpawnRange.y, liquidSurfacePlane.transform.position);
			Vector3 direction = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up) * Vector3.forward;
			base.photonView.RPC("SpawnSodaBubbleRPC", RpcTarget.Others, inputPosition, num2, num, currentTime);
			SpawnSodaBubbleLocal(inputPosition, num2, num, currentTime, addAsTrail: true, direction);
		}
	}

	private void SpawnSodaBubbleLocal(Vector2 surfacePosLocal, float spawnSize, float lifetime, double spawnTime, bool addAsTrail = false, Vector3 direction = default(Vector3))
	{
		if (activeBubbles.Count < maxBubbleCount)
		{
			Vector3 position = liquidSurfacePlane.transform.position + new Vector3(surfacePosLocal.x, 0f, surfacePosLocal.y);
			BubbleData item = new BubbleData
			{
				position = position,
				spawnSize = spawnSize,
				lifetime = lifetime,
				spawnTime = spawnTime,
				isTrail = false
			};
			item.bubble = ObjectPools.instance.Instantiate(spawnedPrefab, item.position, Quaternion.identity, 0f).GetComponent<SodaBubble>();
			if (base.photonView.IsMine && addAsTrail)
			{
				item.direction = direction;
				item.isTrail = true;
				trailHeads.Add(item);
			}
			activeBubbles.Add(item);
		}
	}

	[PunRPC]
	public void SpawnSodaBubbleRPC(Vector2 surfacePosLocal, float spawnSize, float lifetime, double spawnTime, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SpawnSodaBubbleRPC");
		if (info.Sender == PhotonNetwork.MasterClient && float.IsFinite(spawnSize) && float.IsFinite(lifetime) && double.IsFinite(spawnTime))
		{
			float time = Mathf.Clamp01(scienceExperimentManager.RiseProgressLinear);
			surfacePosLocal.ClampThisMagnitudeSafe(surfaceRadiusSpawnRange.y);
			spawnSize = Mathf.Clamp(spawnSize, sizeRange.x, sizeRange.y * rockMaxSizeMultiplierVsLavaProgress.Evaluate(time));
			lifetime = Mathf.Clamp(lifetime, lifetimeRange.x, lifetimeRange.y * rockLifetimeMultiplierVsLavaProgress.Evaluate(time));
			double num = (PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.unscaledTimeAsDouble);
			spawnTime = ((Mathf.Abs((float)(spawnTime - num)) < 10f) ? spawnTime : num);
			SpawnSodaBubbleLocal(surfacePosLocal, spawnSize, lifetime, spawnTime);
		}
	}

	private Vector2 GetSpawnPositionWithClearance(Vector2 inputPosition, float inputSize, float maxDistance, Vector3 lavaSurfaceOrigin)
	{
		Vector2 vector = inputPosition;
		for (int i = 0; i < activeBubbles.Count; i++)
		{
			Vector3 vector2 = activeBubbles[i].position - lavaSurfaceOrigin;
			Vector2 vector3 = new Vector2(vector2.x, vector2.z);
			Vector2 vector4 = vector - vector3;
			float num = (inputSize + activeBubbles[i].spawnSize * scaleFactor) * 0.5f;
			if (!(vector4.sqrMagnitude < num * num))
			{
				continue;
			}
			float magnitude = vector4.magnitude;
			if (magnitude > 0.001f)
			{
				Vector2 vector5 = vector4 / magnitude;
				vector += vector5 * (num - magnitude);
				if (vector.sqrMagnitude > maxDistance * maxDistance)
				{
					vector = vector.normalized * maxDistance;
				}
			}
		}
		if (vector.sqrMagnitude > surfaceRadiusSpawnRange.y * surfaceRadiusSpawnRange.y)
		{
			vector = vector.normalized * surfaceRadiusSpawnRange.y;
		}
		return vector;
	}

	void IGuidedRefObject.GuidedRefInitialize()
	{
		GuidedRefHub.RegisterReceiverField(this, "liquidSurfacePlane", ref liquidSurfacePlane_gRef);
		GuidedRefHub.ReceiverFullyRegistered(this);
	}

	bool IGuidedRefReceiverMono.GuidedRefTryResolveReference(GuidedRefTryResolveInfo target)
	{
		return GuidedRefHub.TryResolveField(this, ref liquidSurfacePlane, liquidSurfacePlane_gRef, target);
	}

	void IGuidedRefReceiverMono.OnAllGuidedRefsResolved()
	{
		if (base.enabled)
		{
			TickSystem<object>.AddPostTickCallback(this);
		}
	}

	void IGuidedRefReceiverMono.OnGuidedRefTargetDestroyed(int fieldId)
	{
		TickSystem<object>.RemovePostTickCallback(this);
	}

	int IGuidedRefObject.GetInstanceID()
	{
		return GetInstanceID();
	}
}
