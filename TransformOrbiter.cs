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
	[Range(0.01f, 5f)]
	private double speed = 1.0;

	private DateTime anchor = new DateTime(2026, 4, 1);

	private async void Start()
	{
		base.gameObject.SetActive(value: false);
		if (!(barycenter == null))
		{
			while (((bool)base.gameObject && GorillaComputer.instance == null) || GorillaComputer.instance.GetServerTime().Year < 2000)
			{
				await Task.Yield();
			}
			if ((bool)base.gameObject)
			{
				base.gameObject.SetActive(value: true);
			}
		}
	}

	private void LateUpdate()
	{
		double totalSeconds = (GorillaComputer.instance.GetServerTime() - anchor).TotalSeconds;
		double num = (double)orbit.x * Math.Sin(totalSeconds * speed);
		double num2 = (double)orbit.y * Math.Cos(totalSeconds * speed);
		double num3 = (double)orbit.z * Math.Cos(totalSeconds * speed);
		base.transform.position = barycenter.position + translation + new Vector3((float)num, (float)num2, (float)num3);
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
