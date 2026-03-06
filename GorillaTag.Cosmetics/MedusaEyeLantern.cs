using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaLocomotion.Climbing;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class MedusaEyeLantern : MonoBehaviour
{
	[Serializable]
	public class EyeState
	{
		public State eyeState;

		public AnimationCurve hapticStrength;

		public UnityEvent onEnterState;

		public UnityEvent onExitState;
	}

	public enum State
	{
		SLOSHING,
		DORMANT,
		TRACKING,
		WARMUP,
		PRIMING,
		PETRIFICATION,
		COOLDOWN,
		RESET
	}

	[SerializeField]
	private DistanceCheckerCosmetic distanceChecker;

	[SerializeField]
	private TransferrableObject transferableParent;

	[SerializeField]
	private GorillaVelocityTracker velocityTracker;

	[SerializeField]
	private Transform rotatingObjectTransform;

	[Space]
	[Header("Rotation Settings")]
	[SerializeField]
	private float maxRotationAngle = 50f;

	[SerializeField]
	private float sloshVelocityThreshold = 1f;

	[SerializeField]
	private float rotationSmoothing = 10f;

	[SerializeField]
	private float rotationSpeedMultiplier = 5f;

	[Space]
	[Header("Target Tracking Settings")]
	[SerializeField]
	private float lookAtEyeAngleThreshold = 90f;

	[SerializeField]
	private float targetHeadAngleThreshold = 5f;

	[SerializeField]
	private float lookAtTargetSpeed = 5f;

	[SerializeField]
	private float warmUpProgressTime = 3f;

	[SerializeField]
	private float resetCooldown = 5f;

	[SerializeField]
	private float faceDistanceOffset = 0.2f;

	[SerializeField]
	private float petrificationDuration = 0.2f;

	[Space]
	[Header("Eye State Settings")]
	public EyeState[] allStates = new EyeState[0];

	public UnityEvent<VRRig> OnPetrification;

	private Quaternion initialRotation;

	private Quaternion targetRotation;

	private State currentState;

	private State lastState;

	private float petrificationStarted = float.PositiveInfinity;

	private float warmupCounter;

	private Dictionary<State, EyeState> allStatesDict = new Dictionary<State, EyeState>();

	private VRRig targetRig;

	private float resetTargetTimer = 1f;

	private float resetTargetTime = float.PositiveInfinity;

	private void Awake()
	{
		EyeState[] array = allStates;
		foreach (EyeState eyeState in array)
		{
			allStatesDict.Add(eyeState.eyeState, eyeState);
		}
	}

	private void OnDestroy()
	{
		allStatesDict.Clear();
	}

	private void Start()
	{
		if (rotatingObjectTransform == null)
		{
			rotatingObjectTransform = base.transform;
		}
		initialRotation = rotatingObjectTransform.localRotation;
		SwitchState(State.DORMANT);
	}

	private void Update()
	{
		if (!transferableParent.InHand() && currentState != State.DORMANT)
		{
			SwitchState(State.DORMANT);
		}
		if (!transferableParent.InHand())
		{
			return;
		}
		UpdateState();
		if (!(velocityTracker == null) && !(rotatingObjectTransform == null))
		{
			Vector3 averageVelocity = velocityTracker.GetAverageVelocity(worldSpace: true);
			Vector3 vector = new Vector3(averageVelocity.x, 0f, averageVelocity.z);
			float magnitude = vector.magnitude;
			Vector3 normalized = vector.normalized;
			float x = Mathf.Clamp(0f - normalized.z, -1f, 1f) * maxRotationAngle * (magnitude * rotationSpeedMultiplier);
			float z = Mathf.Clamp(normalized.x, -1f, 1f) * maxRotationAngle * (magnitude * rotationSpeedMultiplier);
			targetRotation = initialRotation * Quaternion.Euler(x, 0f, z);
			if (magnitude > sloshVelocityThreshold)
			{
				SwitchState(State.SLOSHING);
			}
			if ((double)magnitude < 0.01)
			{
				targetRotation = initialRotation;
			}
			if (!EyeIsLockedOn())
			{
				rotatingObjectTransform.localRotation = Quaternion.Slerp(rotatingObjectTransform.localRotation, targetRotation, Time.deltaTime * rotationSmoothing);
			}
		}
	}

	public void HandleOnNoOneInRange()
	{
		SwitchState(State.RESET);
		resetTargetTime = Time.time;
		rotatingObjectTransform.localRotation = initialRotation;
	}

	public void HandleOnNewPlayerDetected(VRRig target, float distance)
	{
		targetRig = target;
		if (currentState != State.SLOSHING)
		{
			SwitchState(State.TRACKING);
		}
	}

	private void Sloshing()
	{
		Vector3 averageVelocity = velocityTracker.GetAverageVelocity(worldSpace: true);
		if ((double)new Vector3(averageVelocity.x, 0f, averageVelocity.z).magnitude < 0.01)
		{
			SwitchState(State.DORMANT);
		}
	}

	private void FaceTarget()
	{
		if (targetRig == null || rotatingObjectTransform == null)
		{
			return;
		}
		Vector3 normalized = (targetRig.tagSound.transform.position - rotatingObjectTransform.position).normalized;
		Vector3 normalized2 = new Vector3(normalized.x, 0f, normalized.z).normalized;
		Debug.DrawRay(rotatingObjectTransform.position, rotatingObjectTransform.forward * 0.3f, Color.blue);
		Debug.DrawRay(rotatingObjectTransform.position, normalized2 * 0.3f, Color.green);
		if (normalized2.sqrMagnitude > 0.001f)
		{
			float num = Mathf.Acos(Mathf.Clamp(Vector3.Dot(rotatingObjectTransform.forward.normalized, normalized2), -1f, 1f)) * 57.29578f;
			if (180f - num < targetHeadAngleThreshold && currentState == State.TRACKING)
			{
				SwitchState(State.WARMUP);
				return;
			}
			Quaternion to = Quaternion.LookRotation(-normalized2, Vector3.up);
			rotatingObjectTransform.rotation = Quaternion.RotateTowards(rotatingObjectTransform.rotation, to, lookAtTargetSpeed * Time.deltaTime);
		}
	}

	private bool IsTargetLookingAtEye()
	{
		if (targetRig == null || rotatingObjectTransform == null)
		{
			return false;
		}
		Transform transform = targetRig.tagSound.transform;
		Vector3 normalized = (rotatingObjectTransform.position - rotatingObjectTransform.forward * faceDistanceOffset - transform.position).normalized;
		float num = Mathf.Acos(Mathf.Clamp(Vector3.Dot(transform.up.normalized, normalized), -1f, 1f)) * 57.29578f;
		Debug.DrawRay(transform.position, transform.up * 0.3f, Color.magenta);
		Debug.DrawRay(transform.position, normalized * 0.3f, Color.yellow);
		return num < lookAtEyeAngleThreshold;
	}

	private void UpdateState()
	{
		switch (currentState)
		{
		case State.RESET:
			if (Time.time - resetTargetTime > resetTargetTimer)
			{
				resetTargetTime = float.PositiveInfinity;
				SwitchState(State.DORMANT);
			}
			break;
		case State.SLOSHING:
			Sloshing();
			break;
		case State.DORMANT:
			warmupCounter = 0f;
			petrificationStarted = float.PositiveInfinity;
			if (targetRig != null && (targetRig.transform.position - base.transform.position).IsShorterThan(distanceChecker.distanceThreshold))
			{
				SwitchState(State.TRACKING);
			}
			break;
		case State.TRACKING:
			FaceTarget();
			break;
		case State.WARMUP:
			warmupCounter += Time.deltaTime;
			FaceTarget();
			if (warmupCounter > warmUpProgressTime)
			{
				SwitchState(State.PRIMING);
				warmupCounter = 0f;
			}
			break;
		case State.PRIMING:
			FaceTarget();
			if (IsTargetLookingAtEye())
			{
				OnPetrification?.Invoke(targetRig);
				SwitchState(State.PETRIFICATION);
				petrificationStarted = Time.time;
			}
			break;
		case State.PETRIFICATION:
			if (Time.time - petrificationStarted > petrificationDuration)
			{
				SwitchState(State.COOLDOWN);
			}
			break;
		case State.COOLDOWN:
			if (Time.time - petrificationStarted > resetCooldown)
			{
				SwitchState(State.DORMANT);
				petrificationStarted = float.PositiveInfinity;
			}
			break;
		}
		PlayHaptic(currentState);
	}

	private void SwitchState(State newState)
	{
		lastState = currentState;
		currentState = newState;
		if (lastState != currentState && allStatesDict.TryGetValue(newState, out var value))
		{
			value.onEnterState?.Invoke();
		}
		if (lastState != currentState && allStatesDict.TryGetValue(lastState, out var value2))
		{
			value2.onExitState?.Invoke();
		}
	}

	private void PlayHaptic(State state)
	{
		if (!transferableParent.IsMyItem())
		{
			return;
		}
		allStatesDict.TryGetValue(state, out var value);
		if (currentState == State.WARMUP)
		{
			float time = Mathf.Clamp01(warmupCounter / warmUpProgressTime);
			if (value != null && value.hapticStrength != null)
			{
				float amplitude = value.hapticStrength.Evaluate(time);
				bool forLeftController = transferableParent.InLeftHand();
				GorillaTagger.Instance.StartVibration(forLeftController, amplitude, Time.deltaTime);
			}
		}
		else if (value != null && value.hapticStrength != null)
		{
			float amplitude2 = value.hapticStrength.Evaluate(0.5f);
			bool forLeftController2 = transferableParent.InLeftHand();
			GorillaTagger.Instance.StartVibration(forLeftController2, amplitude2, Time.deltaTime);
		}
	}

	private bool EyeIsLockedOn()
	{
		if (currentState == State.TRACKING || currentState == State.WARMUP || currentState == State.PRIMING)
		{
			return true;
		}
		return false;
	}
}
