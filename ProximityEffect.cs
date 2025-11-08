using System;
using System.Collections.Generic;
using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

public class ProximityEffect : MonoBehaviour, ITickSystemTick
{
	[Serializable]
	private class ProximityEvent
	{
		[SerializeField]
		[Range(0f, 1f)]
		[Tooltip("High-threshold events will only fire if the alignment score is above this value.")]
		private float highThreshold = 0.5f;

		[SerializeField]
		[Tooltip("Wait this many seconds before activating the high-threshold events.")]
		private float highThresholdBufferTime;

		[SerializeField]
		[Range(0f, 1f)]
		[Tooltip("Low-threshold events will only fire if the alignment score is below this value.")]
		private float lowThreshold = 0.3f;

		[SerializeField]
		[Tooltip("Wait this many seconds before activating the low-threshold events.")]
		private float lowThresholdBufferTime;

		public UnityEvent onThresholdHigh;

		public UnityEvent onThresholdLow;

		private bool wasAboveThreshold;

		private bool wasBelowThreshold = true;

		private float lastThresholdTime = -100f;

		public bool Evaluate(float score)
		{
			if (score >= highThreshold)
			{
				if (!wasAboveThreshold && Time.time - lastThresholdTime >= highThresholdBufferTime)
				{
					onThresholdHigh?.Invoke();
					wasAboveThreshold = true;
					wasBelowThreshold = false;
				}
				if (wasAboveThreshold)
				{
					lastThresholdTime = Time.time;
				}
				return true;
			}
			if (score < lowThreshold)
			{
				if (!wasBelowThreshold && Time.time - lastThresholdTime >= lowThresholdBufferTime)
				{
					onThresholdLow?.Invoke();
					wasAboveThreshold = false;
					wasBelowThreshold = true;
				}
				if (wasBelowThreshold)
				{
					lastThresholdTime = Time.time;
				}
			}
			return false;
		}

		public void ResetAllEvents()
		{
			wasAboveThreshold = false;
			wasBelowThreshold = true;
		}
	}

	[SerializeField]
	private Transform leftTransform;

	[SerializeField]
	private Transform rightTransform;

	[SerializeField]
	[Tooltip("How many times AddTrigger() needs to be called before the events are allowed to be invoked. Used for pausing events until certain actions are performed (like squeezing the triggers of both controllers).")]
	private int triggersToActivate;

	[Space]
	[SerializeField]
	[Tooltip("The transform that moves to follow the midpoint of the left and right transforms.")]
	private Transform centerTransform;

	private const string SHOW_CONDITION = "@centerTransform != null";

	[SerializeField]
	private float positionCTLerpSpeed = 10f;

	[SerializeField]
	private bool rotateCT;

	private const string SHOW_ROTATE_CONDITION = "@centerTransform != null && rotateCT";

	[SerializeField]
	private float rotationCTLerpSpeed = 10f;

	[SerializeField]
	private bool scaleCT;

	private const string SHOW_SCALE_CONDITION = "@centerTransform != null && scaleCT";

	[SerializeField]
	private float scaleCTLerpSpeed = 10f;

	[SerializeField]
	private float scaleCTMult = 1f;

	[Space]
	[SerializeField]
	[Tooltip("The curves that get evaluated to determine the alignment score. They get multiplied together, so their Y values should all range from 0-1. The result is compared against the thresholds of the ProximityEvents.")]
	private ProximityEffectScoreCurvesSO scoreCurves;

	[Space]
	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	[SerializeField]
	private UnityEvent<float> onScoreCalculated;

	[SerializeField]
	private ProximityEvent[] events;

	[Header("Editor Only")]
	[SerializeField]
	private Vector3 defaultLeftHandLocalPosition = new Vector3(-0.0568f, 0.04311f, 0.00249f);

	[SerializeField]
	private Vector3 defaultLeftHandLocalEuler = new Vector3(173.176f, 80.201f, 3.615f);

	[Header("Visualization is currently NOT WORKING IN PLAY MODE due to tick optimization")]
	[SerializeField]
	private bool enableVisualization = true;

	[SerializeField]
	private Material visualizationMaterial;

	[SerializeField]
	[Range(0f, 1f)]
	private float visualizationLineThickness = 0.01f;

	[SerializeField]
	[HideInInspector]
	private LineRenderer visualizer;

	private List<IProximityEffectReceiver> receivers;

	private VRRig rig;

	private bool anyAboveThreshold;

	private int numTriggers;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		rig = GetComponentInParent<VRRig>();
		enableVisualization = false;
		if ((bool)visualizer)
		{
			UnityEngine.Object.Destroy(visualizer);
		}
	}

	public void AddReceiver(IProximityEffectReceiver receiver)
	{
		if (receivers == null)
		{
			receivers = new List<IProximityEffectReceiver> { receiver };
		}
		else if (!receivers.Contains(receiver))
		{
			receivers.Add(receiver);
		}
	}

	public void RemoveReceiver(IProximityEffectReceiver receiver)
	{
		receivers.Remove(receiver);
	}

	private void StartCalculating()
	{
		centerTransform.position = (leftTransform.position + rightTransform.position) / 2f;
		TickSystem<object>.AddTickCallback(this);
	}

	private void StopCalculating()
	{
		ProximityEvent[] array = events;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ResetAllEvents();
		}
		continuousProperties?.ApplyAll(0f);
		onScoreCalculated?.Invoke(0f);
		TickSystem<object>.RemoveTickCallback(this);
	}

	private void OnEnable()
	{
		if (triggersToActivate == 0)
		{
			StartCalculating();
		}
	}

	private void OnDisable()
	{
		if (triggersToActivate == 0)
		{
			StopCalculating();
		}
	}

	public void AddTrigger()
	{
		if (numTriggers < triggersToActivate)
		{
			numTriggers++;
			if (numTriggers == triggersToActivate)
			{
				StartCalculating();
			}
		}
	}

	public void RemoveTrigger()
	{
		if (numTriggers > 0)
		{
			if (numTriggers == triggersToActivate)
			{
				StopCalculating();
			}
			numTriggers--;
		}
	}

	private void CalculateProximityScores()
	{
		CalculateProximityScores(drawGizmos: true, out var _, out var _, out var _, out var _);
	}

	private void CalculateProximityScores(out float distance, out float alignment, out float parallel, out Vector3 midpoint)
	{
		CalculateProximityScores(drawGizmos: false, out distance, out alignment, out parallel, out midpoint);
	}

	private void CalculateProximityScores(bool drawGizmos, out float distance, out float alignment, out float parallel, out Vector3 midpoint)
	{
		float num = ((rig != null) ? rig.scaleFactor : 1f);
		Vector3 position = leftTransform.position;
		Vector3 position2 = rightTransform.position;
		Vector3 forward = leftTransform.forward;
		Vector3 forward2 = rightTransform.forward;
		Vector3 vector = (position2 - position) / num;
		float magnitude = vector.magnitude;
		Vector3 vector2 = vector / magnitude;
		distance = scoreCurves.distanceModifierCurve.Evaluate(magnitude);
		alignment = scoreCurves.alignmentModifierCurve.Evaluate(0f - Vector3.Dot(forward, forward2));
		parallel = scoreCurves.parallelModifierCurve.Evaluate((Vector3.Dot(forward, vector2) + Vector3.Dot(forward2, -vector2)) / 2f);
		midpoint = position + 0.5f * vector;
	}

	private void MoveTransform(Transform target, float score, Vector3 midpoint)
	{
		target.GetPositionAndRotation(out var position, out var rotation);
		Vector3 vector = Vector3.Lerp(position, midpoint, ExpT(positionCTLerpSpeed));
		if (rotateCT)
		{
			Vector3 vector2 = (vector - position) / Time.deltaTime;
			if (vector2 != Vector3.zero)
			{
				Quaternion b = Quaternion.LookRotation(vector2);
				Quaternion a = Quaternion.LookRotation(vector - rig.syncPos);
				Quaternion rotation2 = Quaternion.Slerp(rotation, Quaternion.Slerp(a, b, vector2.magnitude), ExpT(rotationCTLerpSpeed));
				target.SetPositionAndRotation(vector, rotation2);
			}
		}
		else
		{
			target.position = vector;
		}
		if (scaleCT)
		{
			target.localScale = Vector3.Lerp(target.localScale, score * scaleCTMult * Vector3.one, ExpT(scaleCTLerpSpeed));
		}
		static float ExpT(float speed)
		{
			return 1f - Mathf.Exp((0f - speed) * Time.deltaTime);
		}
	}

	public void Tick()
	{
		CalculateProximityScores(out var distance, out var alignment, out var parallel, out var midpoint);
		if (receivers != null)
		{
			for (int i = 0; i < receivers.Count; i++)
			{
				receivers[i].OnProximityCalculated(distance, alignment, parallel);
			}
		}
		float num = distance * alignment * parallel;
		continuousProperties?.ApplyAll(num);
		onScoreCalculated?.Invoke(num);
		if (centerTransform != null)
		{
			MoveTransform(centerTransform, num, midpoint);
		}
		anyAboveThreshold = false;
		ProximityEvent[] array = events;
		foreach (ProximityEvent proximityEvent in array)
		{
			anyAboveThreshold = proximityEvent.Evaluate(num) || anyAboveThreshold;
		}
	}
}
