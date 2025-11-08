using UnityEngine;

namespace GorillaTag.Cosmetics;

public class CloserCosmetic : MonoBehaviour, ITickSystemTick
{
	private enum State
	{
		Closing,
		Opening,
		None
	}

	[SerializeField]
	private GameObject sideA;

	[SerializeField]
	private GameObject sideB;

	[SerializeField]
	private Vector3 maxRotationA;

	[SerializeField]
	private Vector3 maxRotationB;

	[SerializeField]
	private bool useFingerFlexValueAsStrength;

	private Quaternion localRotA;

	private Quaternion localRotB;

	private State currentState;

	private float fingerValue;

	public bool TickRunning { get; set; }

	private void OnEnable()
	{
		TickSystem<object>.AddCallbackTarget(this);
		localRotA = sideA.transform.localRotation;
		localRotB = sideB.transform.localRotation;
		fingerValue = 0f;
		UpdateState(State.Opening);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveCallbackTarget(this);
	}

	public void Tick()
	{
		switch (currentState)
		{
		case State.Closing:
			Closing();
			break;
		case State.Opening:
			Opening();
			break;
		case State.None:
			break;
		}
	}

	public void Close(bool leftHand, float fingerFlexValue)
	{
		UpdateState(State.Closing);
		fingerValue = fingerFlexValue;
	}

	public void Open(bool leftHand, float fingerFlexValue)
	{
		UpdateState(State.Opening);
		fingerValue = fingerFlexValue;
	}

	private void Closing()
	{
		float t = (useFingerFlexValueAsStrength ? Mathf.Clamp01(fingerValue) : 1f);
		Quaternion b = Quaternion.Euler(maxRotationB);
		Quaternion quaternion = Quaternion.Slerp(localRotB, b, t);
		sideB.transform.localRotation = quaternion;
		Quaternion b2 = Quaternion.Euler(maxRotationA);
		Quaternion quaternion2 = Quaternion.Slerp(localRotA, b2, t);
		sideA.transform.localRotation = quaternion2;
		if (Quaternion.Angle(sideB.transform.localRotation, quaternion) < 0.1f && Quaternion.Angle(sideA.transform.localRotation, quaternion2) < 0.1f)
		{
			UpdateState(State.None);
		}
	}

	private void Opening()
	{
		float t = (useFingerFlexValueAsStrength ? Mathf.Clamp01(fingerValue) : 1f);
		Quaternion quaternion = Quaternion.Slerp(sideB.transform.localRotation, localRotB, t);
		sideB.transform.localRotation = quaternion;
		Quaternion quaternion2 = Quaternion.Slerp(sideA.transform.localRotation, localRotA, t);
		sideA.transform.localRotation = quaternion2;
		if (Quaternion.Angle(sideB.transform.localRotation, quaternion) < 0.1f && Quaternion.Angle(sideA.transform.localRotation, quaternion2) < 0.1f)
		{
			UpdateState(State.None);
		}
	}

	private void UpdateState(State newState)
	{
		currentState = newState;
	}
}
