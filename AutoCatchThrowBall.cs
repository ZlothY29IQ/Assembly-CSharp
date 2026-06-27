using System.Collections.Generic;
using CjLib;
using UnityEngine;
using UnityEngine.InputSystem;

public class AutoCatchThrowBall : MonoBehaviour
{
	private struct HeldBall
	{
		public bool held;

		public float catchTime;

		public float throwTime;

		public TransferrableObject transferrable;
	}

	public GameObject ballPrefab;

	public float throwPitch = 20f;

	public float throwSpeed = 5f;

	public float throwWaitTime = 1f;

	public float catchWaitTime = 0.2f;

	public LayerMask ballLayer;

	private VRRig vrRig;

	private Collider[] overlapResults = new Collider[32];

	private List<HeldBall> heldBalls = new List<HeldBall>();

	private void Start()
	{
		vrRig = GetComponent<VRRig>();
	}

	private void Update()
	{
		float time = Time.time;
		Vector3 vector = vrRig.transform.position + vrRig.transform.forward * 0.5f;
		Quaternion quaternion = vrRig.transform.rotation * Quaternion.AngleAxis(0f - throwPitch, Vector3.right);
		Vector3 center = vector - quaternion * Vector3.forward * 0.5f;
		int num = Physics.OverlapBoxNonAlloc(center, Vector3.one * 0.5f, overlapResults, quaternion);
		DebugUtil.DrawBox(center, quaternion, Vector3.one, Color.green);
		for (int i = 0; i < num; i++)
		{
			Collider collider = overlapResults[i];
			TransferrableObject componentInParent = collider.gameObject.GetComponentInParent<TransferrableObject>();
			if (!(componentInParent != null))
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < heldBalls.Count; j++)
			{
				if (componentInParent == heldBalls[j].transferrable)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Debug.Log($"Catching {componentInParent.gameObject.name} in from collider {collider.gameObject.name} at position {componentInParent.transform.position}");
				for (int k = 0; k < heldBalls.Count; k++)
				{
				}
				heldBalls.Add(new HeldBall
				{
					held = true,
					catchTime = time,
					transferrable = componentInParent
				});
				componentInParent.OnGrab(null, null);
				componentInParent.currentState = TransferrableObject.PositionState.InRightHand;
			}
		}
		for (int num2 = heldBalls.Count - 1; num2 >= 0; num2--)
		{
			HeldBall value = heldBalls[num2];
			if (value.held)
			{
				value.transferrable.transform.position = vector;
				if (time > value.catchTime + throwWaitTime)
				{
					Throw(value.transferrable, quaternion * Vector3.forward);
					value.held = false;
					value.throwTime = time;
					heldBalls[num2] = value;
				}
			}
			else if (time > value.throwTime + catchWaitTime)
			{
				Debug.Log("Removing " + value.transferrable.gameObject.name);
				heldBalls.RemoveAt(num2);
				for (int l = 0; l < heldBalls.Count; l++)
				{
				}
			}
		}
		if (!TestScript.IsUIOpen && Keyboard.current.tKey.wasPressedThisFrame && ballPrefab != null)
		{
			TransferrableObject componentInChildren = Object.Instantiate(ballPrefab, vector, Quaternion.identity, null).GetComponentInChildren<TransferrableObject>();
			componentInChildren.OnGrab(null, null);
			componentInChildren.currentState = TransferrableObject.PositionState.InRightHand;
			Throw(componentInChildren, quaternion * Vector3.forward);
		}
		DebugUtil.DrawRect(vector, quaternion * Quaternion.AngleAxis(-90f, Vector3.right), Vector2.one, Color.green);
	}

	private void Throw(TransferrableObject transferrable, Vector3 throwDir)
	{
		Rigidbody componentInChildren = transferrable.GetComponentInChildren<Rigidbody>();
		transferrable.OnRelease(null, null);
		transferrable.currentState = TransferrableObject.PositionState.Dropped;
		componentInChildren.isKinematic = false;
		componentInChildren.linearVelocity = throwDir * throwSpeed;
		Debug.Log($"Throwing {transferrable.gameObject.name} in direction {throwDir} at position {transferrable.transform.position}");
	}
}
