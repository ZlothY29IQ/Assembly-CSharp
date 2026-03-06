using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class ShakeReactorCosmetic : MonoBehaviour, ISpawnable
{
	[Header("Speed Source")]
	[Tooltip("Speed component provider")]
	[SerializeField]
	private SimpleSpeedTracker speedTracker;

	[Header("Settings")]
	[Tooltip("Minimum reversals-per-second required to consider motion a shake - Hz.")]
	[SerializeField]
	private float shakeRateThreshold = 1f;

	[Tooltip("Minimum distance traveled between direction reversals to count as a valid half-cycle.")]
	[SerializeField]
	private float shakeAmplitudeThreshold = 0.1f;

	[Tooltip("Minimum angle change (degrees) between consecutive lobes to register a reversal. Higher = stricter.")]
	[SerializeField]
	[Range(10f, 170f)]
	private float angleToleranceDeg = 120f;

	[Tooltip("Minimum speed required to accept a direction reversal, ignores tiny jitter near stop.")]
	[SerializeField]
	private float minSpeedForReversal = 0.2f;

	[Tooltip("After a shake ends, how long to wait before ShakeStartLocal can fire again")]
	[SerializeField]
	private float startCooldownSeconds = 0.2f;

	[SerializeField]
	private bool useMaxes;

	[Tooltip("If enabled, exceeding this rate is considered a max shake.")]
	[SerializeField]
	private float maxShakeRate = 6f;

	[Tooltip("If enabled, exceeding this amplitude per half cycle is considered a max shake.")]
	[SerializeField]
	private float maxShakeAmplitude = 0.3f;

	[Header("Continuous Output")]
	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	[Header("Advanced")]
	[Tooltip("When no hard max amplitude is defined, strength is mapped to Threshold × this multiplier.")]
	[SerializeField]
	private float softMaxMultiplier = 3f;

	[FormerlySerializedAs("ShakeStart")]
	[Header("Events")]
	public UnityEvent ShakeStartLocal;

	public UnityEvent ShakeStartShared;

	[FormerlySerializedAs("ShakeEnd")]
	public UnityEvent ShakeEndLocal;

	public UnityEvent ShakeEndShared;

	public UnityEvent MaxShake;

	[Header("Debug")]
	public bool isShaking;

	public float lastAmplitudeMeters;

	public float debugCurrentHalfCycleDistance;

	public float debugCurrentRateHz;

	private const int kFrequencyHistoryCount = 1;

	private const float kNoReversalGraceMultiplier = 1f;

	private readonly Queue<float> recentHalfCycleDurations = new Queue<float>();

	private Vector3 lastVelocityDir;

	private bool hasLastDir;

	private float lastReversalTime;

	private Vector3 lastPosition;

	private float pathSinceLastReversal;

	private float nextAllowedShakeStartTime;

	private const float kEpsilon = 1E-05f;

	private const float kTinyVelocitySqr = 1E-06f;

	private const float kMinHalfCycleDuration = 0.0005f;

	private const float kHalfPerCycle = 0.5f;

	private RubberDuckEvents _events;

	private CallLimiter callLimiter = new CallLimiter(10, 1f);

	private VRRig myRig;

	private bool subscribed;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	private void OnEnable()
	{
		lastReversalTime = Time.time;
		pathSinceLastReversal = 0f;
		recentHalfCycleDurations.Clear();
		hasLastDir = false;
		lastPosition = ((speedTracker != null) ? speedTracker.transform.position : base.transform.position);
		isShaking = false;
		debugCurrentHalfCycleDistance = 0f;
		debugCurrentRateHz = 0f;
		lastAmplitudeMeters = 0f;
		nextAllowedShakeStartTime = Time.time;
		if (myRig == null)
		{
			myRig = GetComponentInParent<VRRig>();
		}
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
		}
		NetPlayer netPlayer = ((myRig != null) ? (myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : NetworkSystem.Instance.LocalPlayer);
		if (netPlayer != null)
		{
			_events.Init(netPlayer);
		}
		if (!subscribed && _events.Activate != null)
		{
			_events.Activate.reliable = true;
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnShake);
			subscribed = true;
		}
	}

	private void OnDisable()
	{
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnShake);
			subscribed = false;
			_events.Dispose();
			_events = null;
		}
	}

	private void Update()
	{
		if (myRig != null && !myRig.isLocal)
		{
			return;
		}
		if (speedTracker == null)
		{
			if (isShaking)
			{
				isShaking = false;
				if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
				{
					_events.Activate.RaiseOthers(isShaking);
				}
				ShakeEndShared?.Invoke();
				ShakeEndLocal?.Invoke();
				nextAllowedShakeStartTime = Time.time + Mathf.Max(0f, startCooldownSeconds);
			}
			return;
		}
		Vector3 position = speedTracker.transform.position;
		float magnitude = (position - lastPosition).magnitude;
		if (magnitude > 0f)
		{
			pathSinceLastReversal += magnitude;
			debugCurrentHalfCycleDistance = pathSinceLastReversal;
		}
		Vector3 worldVelocity = speedTracker.GetWorldVelocity();
		float magnitude2 = worldVelocity.magnitude;
		Vector3 to = ((worldVelocity.sqrMagnitude > 1E-06f) ? worldVelocity.normalized : lastVelocityDir);
		bool flag = false;
		if (hasLastDir)
		{
			if (Vector3.Angle(lastVelocityDir, to) >= angleToleranceDeg && magnitude2 >= minSpeedForReversal)
			{
				float num = Time.time - lastReversalTime;
				if (num > 0.0005f)
				{
					EnqueueHalfCycle(num);
					lastAmplitudeMeters = pathSinceLastReversal;
					lastReversalTime = Time.time;
					pathSinceLastReversal = 0f;
					flag = true;
				}
			}
		}
		else
		{
			hasLastDir = true;
			lastVelocityDir = to;
			lastReversalTime = Time.time;
		}
		lastVelocityDir = to;
		lastPosition = position;
		float averageHalfCycleDuration = GetAverageHalfCycleDuration();
		float b = Time.time - lastReversalTime;
		float num2 = Mathf.Max((averageHalfCycleDuration > 1E-05f) ? averageHalfCycleDuration : float.PositiveInfinity, b);
		float num3 = (debugCurrentRateHz = ((num2 < float.PositiveInfinity) ? (0.5f / num2) : 0f));
		bool flag2 = num3 >= shakeRateThreshold;
		bool flag3 = lastAmplitudeMeters >= shakeAmplitudeThreshold;
		if (!isShaking)
		{
			if (Time.time >= nextAllowedShakeStartTime && flag2 && flag3)
			{
				isShaking = true;
				if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
				{
					_events.Activate.RaiseOthers(isShaking);
				}
				ShakeStartLocal?.Invoke();
				ShakeStartShared?.Invoke();
			}
		}
		else
		{
			float num4 = ((shakeRateThreshold > 1E-05f) ? (0.5f / shakeRateThreshold) : float.PositiveInfinity);
			float num5 = 1f * num4;
			bool flag4 = Time.time - lastReversalTime > num5;
			if ((!flag2 && !flag) || flag4)
			{
				isShaking = false;
				if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
				{
					_events.Activate.RaiseOthers(isShaking);
				}
				ShakeEndLocal?.Invoke();
				ShakeEndShared?.Invoke();
				nextAllowedShakeStartTime = Time.time + Mathf.Max(0f, startCooldownSeconds);
			}
		}
		if (useMaxes && isShaking)
		{
			bool num6 = num3 >= maxShakeRate;
			bool flag5 = lastAmplitudeMeters >= maxShakeAmplitude;
			if (num6 || flag5)
			{
				MaxShake?.Invoke();
			}
		}
		float strength = 0f;
		if (isShaking)
		{
			float num7 = Mathf.Max(1E-05f, shakeAmplitudeThreshold);
			if (useMaxes && maxShakeAmplitude > num7)
			{
				strength = Mathf.InverseLerp(num7, maxShakeAmplitude, lastAmplitudeMeters);
			}
			else
			{
				float b2 = Mathf.Max(num7, shakeAmplitudeThreshold * Mathf.Max(1f, softMaxMultiplier));
				strength = Mathf.InverseLerp(num7, b2, lastAmplitudeMeters);
			}
		}
		ApplyStrength(strength);
	}

	private void EnqueueHalfCycle(float duration)
	{
		recentHalfCycleDurations.Enqueue(duration);
		while (recentHalfCycleDurations.Count > Mathf.Max(1, 1))
		{
			recentHalfCycleDurations.Dequeue();
		}
	}

	private float GetAverageHalfCycleDuration()
	{
		if (recentHalfCycleDurations.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		foreach (float recentHalfCycleDuration in recentHalfCycleDurations)
		{
			num += recentHalfCycleDuration;
		}
		return num / (float)recentHalfCycleDurations.Count;
	}

	private void ApplyStrength(float strength01)
	{
		if (continuousProperties != null)
		{
			continuousProperties.ApplyAll(strength01);
		}
	}

	private void OnShake(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target || info.senderID != myRig.creator.ActorNumber)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnShake");
		if (!callLimiter.CheckCallTime(Time.time) || args.Length != 1)
		{
			return;
		}
		object obj = args[0];
		if (obj is bool)
		{
			if ((bool)obj)
			{
				ShakeStartShared?.Invoke();
			}
			else
			{
				ShakeEndShared?.Invoke();
			}
		}
	}

	public void OnSpawn(VRRig rig)
	{
		myRig = rig;
	}

	public void OnDespawn()
	{
	}
}
