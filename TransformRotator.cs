using System;
using System.Threading.Tasks;
using GorillaNetworking;
using UnityEngine;

public class TransformRotator : MonoBehaviour
{
	[SerializeField]
	private Vector3 axis = Vector3.forward;

	[SerializeField]
	private float degreesPerSecond = 90f;

	[SerializeField]
	private float sinAmp;

	private Quaternion baseRotation;

	private DateTime anchor;

	private async void Start()
	{
		baseRotation = base.transform.localRotation;
		base.gameObject.SetActive(value: false);
		while (((bool)base.gameObject && GorillaComputer.instance == null) || GorillaComputer.instance.GetServerTime().Year < 2000)
		{
			await Task.Yield();
		}
		anchor = DateTime.Parse(GorillaComputer.instance.buildDate);
		while ((GorillaComputer.instance.GetServerTime() - anchor).TotalDays > 14.0)
		{
			anchor = anchor.AddDays(14.0);
			await Task.Yield();
		}
		Debug.Log($"TransformRotator :: anchor = {anchor}");
		if ((bool)base.gameObject)
		{
			base.gameObject.SetActive(value: true);
		}
	}

	private void LateUpdate()
	{
		UpdateRotation((GorillaComputer.instance.GetServerTime() - anchor).TotalSeconds);
	}

	private void UpdateRotation(double t)
	{
		float num = degreesPerSecond * (float)t;
		if (sinAmp != 0f)
		{
			num = sinAmp * Mathf.Sin(num / sinAmp);
		}
		Quaternion quaternion = Quaternion.AngleAxis(num, axis);
		base.transform.localRotation = baseRotation * quaternion;
	}
}
