using System;
using UnityEngine;
using UnityEngine.Events;

public class GenericTriggerReactor : MonoBehaviour, IBuildValidation
{
	[SerializeField]
	private string ComponentName = string.Empty;

	[Space]
	[SerializeField]
	private Vector2 speedRangeEnter;

	[SerializeField]
	private Vector2 speedRangeExit;

	[Space]
	[SerializeField]
	private Transform idealMotion;

	[SerializeField]
	private Vector2 idealMotionPlayRangeEnter;

	[SerializeField]
	private Vector2 idealMotionPlayRangeExit;

	[Space]
	[SerializeField]
	private UnityEvent GTOnTriggerEnter;

	[SerializeField]
	private UnityEvent GTOnTriggerExit;

	private Type componentType;

	private GorillaVelocityEstimator gorillaVelocityEstimator;

	bool IBuildValidation.BuildValidationCheck()
	{
		if (ComponentName.Length == 0)
		{
			return true;
		}
		if (Type.GetType(ComponentName) == null)
		{
			Debug.LogError("GenericTriggerReactor :: ComponentName must specify a valid Component or be empty.");
			return false;
		}
		return true;
	}

	private void Awake()
	{
		componentType = Type.GetType(ComponentName);
		TryGetComponent<GorillaVelocityEstimator>(out gorillaVelocityEstimator);
	}

	private void OnTriggerEnter(Collider other)
	{
		OnTriggerTest(other, speedRangeEnter, GTOnTriggerEnter, idealMotionPlayRangeEnter);
	}

	private void OnTriggerExit(Collider other)
	{
		OnTriggerTest(other, speedRangeExit, GTOnTriggerExit, idealMotionPlayRangeExit);
	}

	private void OnTriggerTest(Collider other, Vector2 speedRange, UnityEvent unityEvent, Vector2 idealMotionPlay)
	{
		if (unityEvent == null || (!(componentType == null) && !other.TryGetComponent(componentType, out var _)))
		{
			return;
		}
		if (gorillaVelocityEstimator != null)
		{
			float magnitude = gorillaVelocityEstimator.linearVelocity.magnitude;
			if (magnitude < speedRange.x || magnitude > speedRange.y)
			{
				return;
			}
			if (idealMotion != null)
			{
				float num = Vector3.Dot(gorillaVelocityEstimator.linearVelocity.normalized, idealMotion.forward);
				if (num < idealMotionPlay.x || num > idealMotionPlay.y)
				{
					return;
				}
			}
		}
		unityEvent.Invoke();
	}
}
