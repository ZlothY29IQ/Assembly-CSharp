using GorillaExtensions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class StickyCosmetic : MonoBehaviour
{
	private enum ObjectState
	{
		Extending,
		Retracting,
		Stuck,
		JustRetracted,
		Idle,
		AutoUnstuck,
		AutoRetract
	}

	[Tooltip("Optional reference to an UpdateBlendShapeCosmetic component. Used to drive extension length based on blend shape weight (e.g. finger flex input).")]
	[SerializeField]
	private UpdateBlendShapeCosmetic blendShapeCosmetic;

	[Tooltip("Defines which physics layers this sticky object can attach to when extending (checked via raycast).")]
	[SerializeField]
	private LayerMask collisionLayers;

	[Tooltip("Transform origin from which the raycast will be fired forward to detect stickable surfaces.")]
	[SerializeField]
	private Transform rayOrigin;

	[Tooltip("Transform representing the start or base position of the sticky object (where extension originates).")]
	[SerializeField]
	private Transform startPosition;

	[Tooltip("Rigidbody controlling the physical end of the sticky object (the part that extends and can attach).")]
	[SerializeField]
	private Rigidbody endRigidbody;

	[Tooltip("Parent transform the end object will reattach to when fully retracted. This keeps local transform resets consistent.")]
	[SerializeField]
	private Transform endPositionParent;

	[Tooltip("Maximum distance the object can extend from its start position (in meters).")]
	[SerializeField]
	private float maxObjectLength = 0.7f;

	[Tooltip("If the sticky object remains stuck but the distance from start exceeds this threshold, it will automatically unstuck and begin retracting.")]
	[SerializeField]
	private float autoRetractThreshold = 1f;

	[Tooltip("Speed (units per second) at which the end rigidbody retracts toward its start position when returning.")]
	[SerializeField]
	private float retractSpeed = 5f;

	[Tooltip("If the sticky end remains extended but doesn’t stick to anything, it will automatically start retracting after this many seconds.")]
	[SerializeField]
	private float retractAfterSecond = 2f;

	[Tooltip("Invoked when the sticky object successfully attaches to a surface.")]
	public UnityEvent onStick;

	[Tooltip("Invoked when the sticky object becomes unstuck — either manually or automatically.")]
	public UnityEvent onUnstick;

	private ObjectState currentState;

	private float rayLength;

	private bool stick;

	private ObjectState lastState;

	private float extendingStartedTime;

	private void Start()
	{
		endRigidbody.isKinematic = false;
		endRigidbody.useGravity = false;
		UpdateState(ObjectState.Idle);
	}

	public void Extend()
	{
		if (currentState == ObjectState.Idle || currentState == ObjectState.Extending)
		{
			UpdateState(ObjectState.Extending);
		}
	}

	public void Retract()
	{
		UpdateState(ObjectState.Retracting);
	}

	private void Extend_Internal()
	{
		if (!endRigidbody.isKinematic)
		{
			rayLength = Mathf.Lerp(0f, maxObjectLength, blendShapeCosmetic.GetBlendValue() / blendShapeCosmetic.maxBlendShapeWeight);
			endRigidbody.MovePosition(startPosition.position + startPosition.forward * rayLength);
		}
	}

	private void Retract_Internal()
	{
		endRigidbody.isKinematic = false;
		Vector3 position = Vector3.MoveTowards(endRigidbody.position, startPosition.position, retractSpeed * Time.fixedDeltaTime);
		endRigidbody.MovePosition(position);
	}

	private void FixedUpdate()
	{
		switch (currentState)
		{
		case ObjectState.Retracting:
			if (Vector3.Distance(endRigidbody.position, startPosition.position) <= 0.01f)
			{
				endRigidbody.position = startPosition.position;
				Transform obj = endRigidbody.transform;
				obj.parent = endPositionParent;
				obj.localRotation = quaternion.identity;
				obj.localScale = Vector3.one;
				if (lastState == ObjectState.AutoUnstuck || lastState == ObjectState.AutoRetract)
				{
					UpdateState(ObjectState.JustRetracted);
				}
				else
				{
					UpdateState(ObjectState.Idle);
				}
			}
			else
			{
				Retract_Internal();
			}
			break;
		case ObjectState.Extending:
		{
			if (Time.time - extendingStartedTime > retractAfterSecond)
			{
				UpdateState(ObjectState.AutoRetract);
			}
			Extend_Internal();
			if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var _, rayLength, collisionLayers))
			{
				endRigidbody.isKinematic = true;
				endRigidbody.transform.parent = null;
				onStick?.Invoke();
				UpdateState(ObjectState.Stuck);
			}
			break;
		}
		case ObjectState.Stuck:
			if (endRigidbody.isKinematic && (endRigidbody.position - startPosition.position).IsLongerThan(autoRetractThreshold))
			{
				UpdateState(ObjectState.AutoUnstuck);
			}
			break;
		case ObjectState.AutoUnstuck:
			UpdateState(ObjectState.Retracting);
			break;
		case ObjectState.AutoRetract:
			UpdateState(ObjectState.Retracting);
			break;
		}
		Debug.DrawRay(rayOrigin.position, rayOrigin.forward * rayLength, Color.red);
	}

	private void UpdateState(ObjectState newState)
	{
		lastState = currentState;
		if (lastState == ObjectState.Stuck && newState != currentState)
		{
			onUnstick.Invoke();
		}
		if (lastState != ObjectState.Extending && newState == ObjectState.Extending)
		{
			extendingStartedTime = Time.time;
		}
		currentState = newState;
	}
}
