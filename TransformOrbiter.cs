using System;
using System.Threading.Tasks;
using GorillaNetworking;
using UnityEngine;

public class TransformOrbiter : MonoBehaviour
{
	[SerializeField]
	private Transform barycenter;

	[SerializeField]
	private Vector3 orbit;

	[SerializeField]
	private Vector3 translation;

	[SerializeField]
	[Range(0.01f, 10f)]
	private double speed = 1.0;

	private double orbitTime;

	[SerializeField]
	private bool faceBarycenter;

	[SerializeField]
	private bool absoluteOrbitX;

	[SerializeField]
	private bool absoluteOrbitY;

	[SerializeField]
	private bool absoluteOrbitZ;

	private DateTime anchor;

	private async void Start()
	{
		base.gameObject.SetActive(value: false);
		if (!(barycenter == null))
		{
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
			Debug.Log($"TransformOrbiter :: anchor = {anchor}");
			if ((bool)base.gameObject)
			{
				base.gameObject.SetActive(value: true);
			}
		}
	}

	private void LateUpdate()
	{
		UpdatePosRot((GorillaComputer.instance.GetServerTime() - anchor).TotalSeconds);
	}

	private void UpdatePosRot(double t)
	{
		base.transform.position = GetPositionAtTime(t);
		if (faceBarycenter)
		{
			base.transform.rotation = Quaternion.LookRotation(barycenter.position - base.transform.position);
		}
	}

	private Vector3 GetPositionAtTime(double t)
	{
		double num = Math.Sin(t * speed);
		double num2 = Math.Cos(t * speed);
		double num3 = Math.Cos(t * speed);
		if (absoluteOrbitX)
		{
			num = Math.Abs(num);
		}
		if (absoluteOrbitY)
		{
			num2 = Math.Abs(num2);
		}
		if (absoluteOrbitZ)
		{
			num3 = Math.Abs(num3);
		}
		double num4 = (double)orbit.x * num;
		double num5 = (double)orbit.y * num2;
		double num6 = (double)orbit.z * num3;
		return barycenter.position + translation + new Vector3((float)num4, (float)num5, (float)num6);
	}

	private bool validateBarycenter()
	{
		return validateBarycenter(base.transform);
	}

	private bool validateBarycenter(Transform t)
	{
		if (barycenter == null)
		{
			Debug.LogError("The Barycenter cannot be null!");
			return false;
		}
		if (barycenter == t)
		{
			Debug.LogError("You cannot use the TransformOrbiter's own transform, or one nested below it, as its Barycenter!");
			return false;
		}
		for (int i = 0; i < t.childCount; i++)
		{
			if (!validateBarycenter(t.GetChild(i)))
			{
				return false;
			}
		}
		return true;
	}
}
