using UnityEngine;

public class GRMeterEnergy : MonoBehaviour
{
	public enum MeterType
	{
		Linear,
		Radial
	}

	public GRTool tool;

	public Transform meter;

	public Transform chargePoint;

	public MeterType meterType;

	public Vector2 angularRange = new Vector2(-45f, 45f);

	[Range(0f, 2f)]
	public int rotationAxis;

	public void Awake()
	{
	}

	public void Refresh()
	{
		float value = 0f;
		if (tool != null && tool.GetEnergyMax() > 0)
		{
			value = (float)tool.energy / (float)tool.GetEnergyMax();
		}
		value = Mathf.Clamp(value, 0f, 1f);
		MeterType meterType = this.meterType;
		if (meterType == MeterType.Linear || meterType != MeterType.Radial)
		{
			meter.localScale = new Vector3(1f, value, 1f);
			return;
		}
		float value2 = Mathf.Lerp(angularRange.x, angularRange.y, value);
		Vector3 zero = Vector3.zero;
		zero[rotationAxis] = value2;
		meter.localRotation = Quaternion.Euler(zero);
	}
}
