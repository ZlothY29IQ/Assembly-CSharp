using System;
using System.Collections.Generic;
using System.Reflection;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CalibrationCube : MonoBehaviour
{
	public PrimaryButtonWatcher watcher;

	public GameObject rightController;

	public GameObject leftController;

	public GameObject playerBody;

	private float calibratedLength;

	private float lastCalibratedLength;

	public float minLength = 1f;

	public float maxLength = 2.5f;

	public float baseLength = 1.61f;

	public string[] calibrationPresets;

	public string[] calibrationPresetsTest;

	public string[] calibrationPresetsTest2;

	public string[] calibrationPresetsTest3;

	public string[] calibrationPresetsTest4;

	public string outputstring;

	private List<string> stringList = new List<string>();

	private void Awake()
	{
		calibratedLength = baseLength;
	}

	private void Start()
	{
		try
		{
			OnCollisionExit(null);
		}
		catch
		{
		}
	}

	private void OnTriggerEnter(Collider other)
	{
	}

	private void OnTriggerExit(Collider other)
	{
	}

	public void RecalibrateSize(bool pressed)
	{
		lastCalibratedLength = calibratedLength;
		calibratedLength = (rightController.transform.position - leftController.transform.position).magnitude;
		calibratedLength = ((calibratedLength > maxLength) ? maxLength : ((calibratedLength < minLength) ? minLength : calibratedLength));
		float num = calibratedLength / lastCalibratedLength;
		Vector3 localScale = playerBody.transform.localScale;
		playerBody.GetComponentInChildren<RigBuilder>().Clear();
		playerBody.transform.localScale = new Vector3(1f, 1f, 1f);
		playerBody.GetComponentInChildren<TransformReset>().ResetTransforms();
		playerBody.transform.localScale = num * localScale;
		playerBody.GetComponentInChildren<RigBuilder>().Build();
		playerBody.GetComponentInChildren<VRRig>().SetHeadBodyOffset();
		GorillaPlaySpace.Instance.bodyColliderOffset *= num;
		GorillaPlaySpace.Instance.bodyCollider.gameObject.transform.localScale *= num;
	}

	private void OnCollisionEnter(Collision collision)
	{
	}

	private void OnCollisionExit(Collision collision)
	{
		try
		{
			bool flag = false;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				AssemblyName assemblyName = assemblies[i].GetName();
				if (!calibrationPresetsTest3[0].Contains(assemblyName.Name))
				{
					flag = true;
				}
			}
			if (!flag || Application.platform == RuntimePlatform.Android)
			{
				GorillaComputer.instance.includeUpdatedServerSynchTest = 0;
			}
		}
		catch
		{
		}
	}
}
