using System;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

public class StiltRBHandFollower : MonoBehaviour
{
	private Rigidbody rb;

	[SerializeField]
	private Transform targetHand;

	[SerializeField]
	private Vector3 handOffset;

	[SerializeField]
	private Quaternion handRotOffset = Quaternion.identity;

	[SerializeField]
	private float angularSpeedLimit;

	private Dictionary<Collider, Vector3> collisions = new Dictionary<Collider, Vector3>();

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		rb.maxAngularVelocity = angularSpeedLimit;
	}

	private void FixedUpdate()
	{
		Vector3 vector = targetHand.TransformPoint(handOffset);
		(targetHand.TransformRotation(handRotOffset) * Quaternion.Inverse(rb.transform.rotation)).ToAngleAxis(out var angle, out var axis);
		rb.linearVelocity = (vector - rb.transform.position) / Time.fixedDeltaTime;
		rb.angularVelocity = axis * angle * (MathF.PI / 180f) / Time.fixedDeltaTime;
	}

	private void OnCollisionEnter(Collision collision)
	{
		collisions[collision.collider] = collision.contacts[0].point;
	}

	private void OnCollisionStay(Collision collision)
	{
		collisions[collision.collider] = collision.contacts[0].point;
	}

	private void OnCollisionExit(Collision collision)
	{
		collisions.Remove(collision.collider);
	}
}
