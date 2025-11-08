using System;
using GorillaLocomotion;
using UnityEngine;

public class GorillaVelocityEstimator : MonoBehaviour
{
	public struct VelocityHistorySample
	{
		public Vector3 linear;

		public Vector3 angular;
	}

	[Min(1f)]
	[SerializeField]
	private int numFrames = 8;

	private VelocityHistorySample[] history;

	private int currentFrame;

	private Vector3 lastPos;

	private Quaternion lastRotation;

	private Vector3 lastRotationVec;

	public bool useGlobalSpace;

	public Vector3 linearVelocity { get; private set; }

	public Vector3 angularVelocity { get; private set; }

	public Vector3 handPos { get; private set; }

	private void Awake()
	{
		history = new VelocityHistorySample[numFrames];
	}

	private void OnEnable()
	{
		currentFrame = 0;
		for (int i = 0; i < history.Length; i++)
		{
			history[i] = default(VelocityHistorySample);
		}
		lastPos = base.transform.position;
		lastRotation = base.transform.rotation;
		GorillaVelocityEstimatorManager.Register(this);
	}

	private void OnDisable()
	{
		GorillaVelocityEstimatorManager.Unregister(this);
	}

	private void OnDestroy()
	{
		GorillaVelocityEstimatorManager.Unregister(this);
	}

	public void TriggeredLateUpdate()
	{
		base.transform.GetPositionAndRotation(out var position, out var rotation);
		Vector3 vector = Vector3.zero;
		if (!useGlobalSpace)
		{
			vector = GTPlayer.Instance.InstantaneousVelocity;
		}
		Vector3 vector2 = (position - lastPos) / Time.deltaTime - vector;
		Vector3 eulerAngles = (rotation * Quaternion.Inverse(lastRotation)).eulerAngles;
		if (eulerAngles.x > 180f)
		{
			eulerAngles.x -= 360f;
		}
		if (eulerAngles.y > 180f)
		{
			eulerAngles.y -= 360f;
		}
		if (eulerAngles.z > 180f)
		{
			eulerAngles.z -= 360f;
		}
		eulerAngles *= MathF.PI / 180f / Time.fixedDeltaTime;
		linearVelocity += (vector2 - history[currentFrame].linear) / numFrames;
		angularVelocity += (eulerAngles - history[currentFrame].angular) / numFrames;
		history[currentFrame] = new VelocityHistorySample
		{
			linear = vector2,
			angular = eulerAngles
		};
		handPos = position;
		currentFrame = (currentFrame + 1) % numFrames;
		lastPos = position;
		lastRotation = rotation;
	}
}
