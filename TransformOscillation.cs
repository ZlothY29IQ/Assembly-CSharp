using System;
using GorillaNetworking;
using UnityEngine;

public class TransformOscillation : MonoBehaviour
{
	[SerializeField]
	private Vector3 PosAmp;

	[SerializeField]
	private Vector3 PosFreq;

	[SerializeField]
	private Vector3 RotAmp;

	[SerializeField]
	private Vector3 RotFreq;

	[SerializeField]
	private bool useServerTime;

	[Header("Rigidbody Motion (optional)")]
	[Tooltip("If true and a Rigidbody is present, applies motion using Rigidbody.MovePosition/MoveRotation in FixedUpdate.")]
	[SerializeField]
	private bool useRigidbodyMotion;

	[SerializeField]
	private Rigidbody targetRigidbody;

	[Header("Activation Timer (optional)")]
	[Tooltip("If true, oscillation only runs for 'activeDurationSeconds' after OnEnable; otherwise it runs indefinitely.")]
	[SerializeField]
	private bool useTimeLimit;

	[SerializeField]
	private float timer = 2f;

	[Header("Start Behavior (optional)")]
	[Tooltip("If true, oscillation starts automatically on OnEnable(). If false, call StartOscillation() manually.")]
	[SerializeField]
	private bool startOnEnable = true;

	private Vector3 lastPosOffs = Vector3.zero;

	private Quaternion lastRotOffs = Quaternion.identity;

	private Vector3 offsPos;

	private Vector3 offsRot;

	private DateTime dt;

	private float startTime;

	private bool isRunning;

	private void Awake()
	{
		if (useRigidbodyMotion && !targetRigidbody)
		{
			targetRigidbody = GetComponent<Rigidbody>();
		}
		lastRotOffs = Quaternion.identity;
		startTime = Time.time;
		isRunning = false;
	}

	private void OnEnable()
	{
		lastPosOffs = Vector3.zero;
		lastRotOffs = Quaternion.identity;
		if (startOnEnable)
		{
			StartOscillation();
		}
		else
		{
			isRunning = false;
		}
	}

	public void StartOscillation()
	{
		startTime = Time.time;
		isRunning = true;
	}

	private float GetTimeSeconds()
	{
		if (!useServerTime)
		{
			return Time.timeSinceLevelLoad;
		}
		if (GorillaComputer.instance == null)
		{
			return Time.timeSinceLevelLoad;
		}
		dt = GorillaComputer.instance.GetServerTime();
		return (float)dt.Minute * 60f + (float)dt.Second + (float)dt.Millisecond / 1000f;
	}

	private void ComputeOffsets(float t)
	{
		offsPos.x = PosAmp.x * Mathf.Sin(t * PosFreq.x);
		offsPos.y = PosAmp.y * Mathf.Sin(t * PosFreq.y);
		offsPos.z = PosAmp.z * Mathf.Sin(t * PosFreq.z);
		offsRot.x = RotAmp.x * Mathf.Sin(t * RotFreq.x);
		offsRot.y = RotAmp.y * Mathf.Sin(t * RotFreq.y);
		offsRot.z = RotAmp.z * Mathf.Sin(t * RotFreq.z);
	}

	private void LateUpdate()
	{
		if (isRunning && (!useTimeLimit || !(Time.time - startTime >= timer)) && (!useRigidbodyMotion || !targetRigidbody))
		{
			float timeSeconds = GetTimeSeconds();
			ComputeOffsets(timeSeconds);
			Transform obj = base.transform;
			Quaternion quaternion = Quaternion.Euler(offsRot);
			Vector3 vector = obj.localPosition - lastPosOffs;
			Quaternion quaternion2 = obj.localRotation * Quaternion.Inverse(lastRotOffs);
			obj.localPosition = vector + offsPos;
			obj.localRotation = quaternion2 * quaternion;
			lastPosOffs = offsPos;
			lastRotOffs = quaternion;
		}
	}

	private void FixedUpdate()
	{
		if (isRunning && (!useTimeLimit || !(Time.time - startTime >= timer)) && useRigidbodyMotion && (bool)targetRigidbody)
		{
			float timeSeconds = GetTimeSeconds();
			ComputeOffsets(timeSeconds);
			Transform obj = base.transform;
			Quaternion quaternion = Quaternion.Euler(offsRot);
			Transform parent = obj.parent;
			Vector3 vector = (parent ? parent.TransformVector(lastPosOffs) : lastPosOffs);
			Quaternion rotation = (parent ? (parent.rotation * lastRotOffs * Quaternion.Inverse(parent.rotation)) : lastRotOffs);
			Vector3 vector2 = obj.position - vector;
			Quaternion quaternion2 = obj.rotation * Quaternion.Inverse(rotation);
			Vector3 vector3 = (parent ? parent.TransformVector(offsPos) : offsPos);
			Quaternion quaternion3 = (parent ? (parent.rotation * quaternion * Quaternion.Inverse(parent.rotation)) : quaternion);
			targetRigidbody.MovePosition(vector2 + vector3);
			targetRigidbody.MoveRotation(quaternion2 * quaternion3);
			lastPosOffs = offsPos;
			lastRotOffs = quaternion;
		}
	}
}
