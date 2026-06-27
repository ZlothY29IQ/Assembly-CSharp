using System;
using System.Threading.Tasks;
using GorillaNetworking;
using UnityEngine;

public class TransformRotator : MonoBehaviour
{
	[SerializeField]
	private Vector3 localRotation;

	[SerializeField]
	private Vector4 wobble;

	[SerializeField]
	private Vector3 spin;

	private DateTime anchor = new DateTime(2026, 4, 1);

	private async void Start()
	{
		base.gameObject.SetActive(value: false);
		while (((bool)base.gameObject && GorillaComputer.instance == null) || GorillaComputer.instance.GetServerTime().Year < 2000)
		{
			await Task.Yield();
		}
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
		double num = t % 360.0;
		double num2 = (double)localRotation.x + (double)spin.x * num + (double)wobble.x * Math.Sin(t * (double)wobble.w);
		double num3 = (double)localRotation.y + (double)spin.y * num + (double)wobble.y * Math.Cos(t * (double)wobble.w);
		double num4 = (double)localRotation.z + (double)spin.z * num + (double)wobble.z * Math.Cos(t * (double)wobble.w);
		base.transform.localRotation = Quaternion.Euler((float)num2, (float)num3, (float)num4);
	}
}
