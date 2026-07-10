using System;
using UnityEngine;

public class ServerTimeRevolution : MonoBehaviour
{
	[SerializeField]
	private Vector3 orbit;

	[SerializeField]
	private Transform pivot;

	[SerializeField]
	private Vector3 pivotOffset;

	[SerializeField]
	private double speed = 1.0;

	private DateTime anchor = new DateTime(2026, 4, 1);

	private void LateUpdate()
	{
		double totalSeconds = (DateTime.UtcNow - anchor).TotalSeconds;
		double num = (double)orbit.x * Math.Sin(totalSeconds * speed);
		double num2 = (double)orbit.y * Math.Cos(totalSeconds * speed);
		double num3 = (double)orbit.z * Math.Cos(totalSeconds * speed);
		base.transform.position = pivot.position + pivotOffset + new Vector3((float)num, (float)num2, (float)num3);
	}
}
