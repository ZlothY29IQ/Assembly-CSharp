using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

public class CosmeticTiltReactor : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	public class TiltEvent
	{
		public enum ComparisonMethod
		{
			DotProduct,
			Angle
		}

		public enum TiltEventType
		{
			LessThanThreshold,
			GreaterThanThreshold,
			LessThanThresholdForDuration,
			GreaterThanThresholdForDuration
		}

		public ComparisonMethod comparisonMethod;

		public TiltEventType tiltEventType;

		[Range(0f, 180f)]
		[Tooltip("Angle in degrees from the reference direction")]
		public float angleThreshold;

		[Range(-1f, 1f)]
		[Tooltip("Dot product compared to the reference direction")]
		public float dotThreshold;

		[Tooltip("Minimum time between events firing")]
		public float retriggerDelay;

		[Tooltip("Amount of time the angle or dot product should be less/greater than the threshold before firing an event")]
		public float duration;

		public UnityEvent OnTiltEvent;

		[NonSerialized]
		public bool wasGreater;

		[NonSerialized]
		public bool hasFired;

		[NonSerialized]
		public double thresholdCrossTime = double.MinValue;

		public TiltEvent()
		{
			tiltEventType = TiltEventType.LessThanThreshold;
			comparisonMethod = ComparisonMethod.DotProduct;
			angleThreshold = 15f;
			retriggerDelay = 0f;
			duration = 0.5f;
		}
	}

	[SerializeField]
	private bool useTransform;

	[Tooltip("Direction to which this transform's y is compared in world space")]
	[SerializeField]
	private Vector3 referenceDirection = Vector3.up;

	[Tooltip("compare referenceTransform's y to this transform's y")]
	[SerializeField]
	private Transform referenceTransform;

	[SerializeField]
	private List<TiltEvent> events;

	[Tooltip("input for continuous properties is the dot product of this transform's y and the reference direction")]
	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	[Tooltip("Should this script be run for all clients or just the owner")]
	[SerializeField]
	private bool syncForAllPlayers = true;

	[Tooltip("option to run only if this transferrable object is in the hand")]
	[SerializeField]
	private bool onlyWhileHeld;

	private VRRig _rig;

	private TransferrableObject parentTransferable;

	private bool isLocallyOwned;

	private bool hasContinuousProperties;

	private float angle;

	private float dotProduct;

	private bool calculateAngle;

	private bool calculateDot;

	private bool wasInHand;

	private void Awake()
	{
		referenceDirection.Normalize();
		if (!useTransform && referenceDirection == Vector3.zero)
		{
			GTDev.LogError("CosmeticTiltReactor " + base.gameObject.name + " referenceDirection cannot be 0 vector");
		}
		if (useTransform && referenceTransform == null)
		{
			GTDev.LogError("CosmeticTiltReactor " + base.gameObject.name + " referenceTransform cannot be null");
		}
		hasContinuousProperties = continuousProperties != null && continuousProperties.Count > 0;
		calculateDot = hasContinuousProperties;
		foreach (TiltEvent @event in events)
		{
			if (@event.comparisonMethod == TiltEvent.ComparisonMethod.DotProduct)
			{
				calculateDot = true;
			}
			else
			{
				calculateAngle = true;
			}
			if (calculateDot && calculateAngle)
			{
				break;
			}
		}
		_rig = GetComponentInParent<VRRig>();
		parentTransferable = GetComponentInParent<TransferrableObject>();
		if (_rig == null && base.gameObject.GetComponentInParent<GTPlayer>() != null)
		{
			_rig = GorillaTagger.Instance.offlineVRRig;
		}
		if (_rig == null && !syncForAllPlayers)
		{
			GTDev.LogError("CosmeticTiltReactor on " + base.gameObject.name + " set to not syncForAllPlayers and has no VR Rig parent. Events will not fire");
		}
		else if (_rig != null)
		{
			isLocallyOwned = _rig.isLocal;
		}
		if (parentTransferable == null && onlyWhileHeld)
		{
			GTDev.LogError("CosmeticTiltReactor on " + base.gameObject.name + " set to OnlyWhileHeld but has no TransferrableObject parent. Events will not fire");
		}
	}

	public void OnEnable()
	{
		if ((syncForAllPlayers || isLocallyOwned) && (!useTransform || !(referenceTransform == null)))
		{
			Vector3 vector = (useTransform ? referenceTransform.up : referenceDirection);
			if (calculateAngle)
			{
				angle = Vector3.Angle(base.transform.up, vector);
			}
			if (calculateDot)
			{
				dotProduct = Vector3.Dot(base.transform.up, vector);
			}
			ResetEvents();
			GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		}
	}

	public void OnDisable()
	{
		if ((syncForAllPlayers || isLocallyOwned) && (!useTransform || !(referenceTransform == null)))
		{
			GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		}
	}

	public void SliceUpdate()
	{
		if (onlyWhileHeld)
		{
			bool flag = parentTransferable != null && parentTransferable.InHand();
			if (!flag && wasInHand)
			{
				ResetEvents();
			}
			wasInHand = flag;
			if (!flag)
			{
				return;
			}
		}
		Vector3 vector = (useTransform ? referenceTransform.up : referenceDirection);
		if (calculateAngle)
		{
			angle = Vector3.Angle(base.transform.up, vector);
		}
		if (calculateDot)
		{
			dotProduct = Vector3.Dot(base.transform.up, vector);
		}
		FireEvents();
		if (hasContinuousProperties)
		{
			continuousProperties.ApplyAll(dotProduct);
		}
	}

	private void ResetEvents()
	{
		if (events == null || events.Count <= 0)
		{
			return;
		}
		foreach (TiltEvent @event in events)
		{
			switch (@event.tiltEventType)
			{
			case TiltEvent.TiltEventType.LessThanThreshold:
				@event.wasGreater = true;
				break;
			case TiltEvent.TiltEventType.GreaterThanThreshold:
				@event.wasGreater = false;
				break;
			case TiltEvent.TiltEventType.LessThanThresholdForDuration:
				@event.wasGreater = true;
				@event.hasFired = false;
				break;
			case TiltEvent.TiltEventType.GreaterThanThresholdForDuration:
				@event.wasGreater = false;
				@event.hasFired = false;
				break;
			}
			@event.thresholdCrossTime = double.MinValue;
		}
	}

	private void FireEvents()
	{
		if (events == null || events.Count <= 0)
		{
			return;
		}
		foreach (TiltEvent @event in events)
		{
			bool flag = ((@event.comparisonMethod == TiltEvent.ComparisonMethod.Angle) ? (angle > @event.angleThreshold) : (dotProduct > @event.dotThreshold));
			TiltEvent.TiltEventType tiltEventType = @event.tiltEventType;
			if (tiltEventType == TiltEvent.TiltEventType.LessThanThreshold || tiltEventType == TiltEvent.TiltEventType.GreaterThanThreshold)
			{
				if (flag == @event.wasGreater)
				{
					continue;
				}
				if (@event.tiltEventType == TiltEvent.TiltEventType.GreaterThanThreshold && flag)
				{
					if (@event.thresholdCrossTime + (double)@event.retriggerDelay <= Time.timeAsDouble)
					{
						@event.thresholdCrossTime = Time.timeAsDouble;
						@event.wasGreater = true;
						@event.OnTiltEvent?.Invoke();
					}
				}
				else if (@event.tiltEventType == TiltEvent.TiltEventType.LessThanThreshold && !flag)
				{
					if (@event.thresholdCrossTime + (double)@event.retriggerDelay <= Time.timeAsDouble)
					{
						@event.thresholdCrossTime = Time.timeAsDouble;
						@event.wasGreater = false;
						@event.OnTiltEvent?.Invoke();
					}
				}
				else
				{
					@event.wasGreater = flag;
				}
				continue;
			}
			if (@event.tiltEventType == TiltEvent.TiltEventType.GreaterThanThresholdForDuration)
			{
				if (flag)
				{
					if (!@event.wasGreater)
					{
						@event.thresholdCrossTime = Time.timeAsDouble;
					}
					else if (!@event.hasFired && @event.thresholdCrossTime + (double)@event.duration <= Time.timeAsDouble)
					{
						@event.OnTiltEvent?.Invoke();
						@event.hasFired = true;
					}
				}
				else
				{
					@event.hasFired = false;
				}
			}
			if (@event.tiltEventType == TiltEvent.TiltEventType.LessThanThresholdForDuration)
			{
				if (!flag)
				{
					if (@event.wasGreater)
					{
						@event.thresholdCrossTime = Time.timeAsDouble;
					}
					else if (!@event.hasFired && @event.thresholdCrossTime + (double)@event.duration <= Time.timeAsDouble)
					{
						@event.OnTiltEvent?.Invoke();
						@event.hasFired = true;
					}
				}
				else
				{
					@event.hasFired = false;
				}
			}
			@event.wasGreater = flag;
		}
	}
}
