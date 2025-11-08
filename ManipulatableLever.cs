using System;
using UnityEngine;

public class ManipulatableLever : ManipulatableObject
{
	[Serializable]
	public class LeverNotch
	{
		public float minAngleValue;

		public float maxAngleValue;

		public int value;
	}

	[SerializeField]
	private float breakDistance = 0.2f;

	[SerializeField]
	private Transform leverGrip;

	[SerializeField]
	private float maxAngle = 22.5f;

	[SerializeField]
	private float minAngle = -22.5f;

	[SerializeField]
	private LeverNotch[] notches;

	private Matrix4x4 localSpace;

	private void Awake()
	{
		localSpace = base.transform.worldToLocalMatrix;
	}

	protected override bool ShouldHandDetach(GameObject hand)
	{
		Vector3 position = leverGrip.position;
		Vector3 position2 = hand.transform.position;
		return Vector3.SqrMagnitude(position - position2) > breakDistance * breakDistance;
	}

	protected override void OnHeldUpdate(GameObject hand)
	{
		Vector3 position = hand.transform.position;
		Vector3 upwards = Vector3.Normalize(localSpace.MultiplyPoint3x4(position) - base.transform.localPosition);
		Vector3 eulerAngles = Quaternion.LookRotation(Vector3.forward, upwards).eulerAngles;
		if (eulerAngles.z > 180f)
		{
			eulerAngles.z -= 360f;
		}
		else if (eulerAngles.z < -180f)
		{
			eulerAngles.z += 360f;
		}
		eulerAngles.z = Mathf.Clamp(eulerAngles.z, minAngle, maxAngle);
		base.transform.localEulerAngles = eulerAngles;
	}

	public void SetValue(float value)
	{
		float z = Mathf.Lerp(minAngle, maxAngle, value);
		Vector3 localEulerAngles = base.transform.localEulerAngles;
		localEulerAngles.z = z;
		base.transform.localEulerAngles = localEulerAngles;
	}

	public void SetNotch(int notchValue)
	{
		if (notches == null)
		{
			return;
		}
		LeverNotch[] array = notches;
		foreach (LeverNotch leverNotch in array)
		{
			if (leverNotch.value == notchValue)
			{
				SetValue(Mathf.Lerp(leverNotch.minAngleValue, leverNotch.maxAngleValue, 0.5f));
				break;
			}
		}
	}

	public float GetValue()
	{
		Vector3 localEulerAngles = base.transform.localEulerAngles;
		if (localEulerAngles.z > 180f)
		{
			localEulerAngles.z -= 360f;
		}
		else if (localEulerAngles.z < -180f)
		{
			localEulerAngles.z += 360f;
		}
		return Mathf.InverseLerp(minAngle, maxAngle, localEulerAngles.z);
	}

	public int GetNotch()
	{
		if (notches == null)
		{
			return 0;
		}
		float value = GetValue();
		LeverNotch[] array = notches;
		foreach (LeverNotch leverNotch in array)
		{
			if (value >= leverNotch.minAngleValue && value <= leverNotch.maxAngleValue)
			{
				return leverNotch.value;
			}
		}
		return 0;
	}
}
