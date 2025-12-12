using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ThermalManager : MonoBehaviour, IGorillaSliceableSimple
{
	public static readonly List<ThermalSourceVolume> sources = new List<ThermalSourceVolume>(256);

	public static readonly List<ThermalReceiver> receivers = new List<ThermalReceiver>(256);

	[NonSerialized]
	public static ThermalManager instance;

	private float lastTime;

	public void OnEnable()
	{
		if (instance != null)
		{
			Debug.LogError("ThermalManager already exists!");
			return;
		}
		instance = this;
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		lastTime = Time.time;
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		float num = Time.time - lastTime;
		lastTime = Time.time;
		for (int i = 0; i < receivers.Count; i++)
		{
			ThermalReceiver thermalReceiver = receivers[i];
			Transform obj = thermalReceiver.transform;
			Vector3 position = obj.position;
			float x = obj.lossyScale.x;
			float num2 = 20f;
			for (int j = 0; j < sources.Count; j++)
			{
				ThermalSourceVolume thermalSourceVolume = sources[j];
				if ((thermalSourceVolume.exclusionReceivers.Count <= 0 || !thermalSourceVolume.exclusionReceivers.Contains(thermalReceiver)) && (thermalReceiver.exclusionSources.Count <= 0 || !thermalReceiver.exclusionSources.Contains(thermalSourceVolume)))
				{
					Transform obj2 = thermalSourceVolume.transform;
					float x2 = obj2.lossyScale.x;
					float num3 = Vector3.Distance(obj2.position, position);
					float num4 = 1f - Mathf.InverseLerp(thermalSourceVolume.innerRadius * x2, thermalSourceVolume.outerRadius * x2, num3 - thermalReceiver.radius * x);
					num2 += thermalSourceVolume.celsius * num4;
				}
			}
			thermalReceiver.celsius = Mathf.Lerp(thermalReceiver.celsius, num2, num * thermalReceiver.conductivity);
			thermalReceiver.continuousProperties?.ApplyAll(thermalReceiver.celsius);
			if (!thermalReceiver.wasAboveThreshold && thermalReceiver.celsius > thermalReceiver.temperatureThreshold)
			{
				thermalReceiver.wasAboveThreshold = true;
				thermalReceiver.OnAboveThreshold?.Invoke();
			}
			else if (thermalReceiver.wasAboveThreshold && thermalReceiver.celsius < thermalReceiver.temperatureThreshold)
			{
				thermalReceiver.wasAboveThreshold = false;
				thermalReceiver.OnBelowThreshold?.Invoke();
			}
		}
	}

	public static void Register(ThermalSourceVolume source)
	{
		sources.Add(source);
	}

	public static void Unregister(ThermalSourceVolume source)
	{
		sources.Remove(source);
	}

	public static void Register(ThermalReceiver receiver)
	{
		receivers.Add(receiver);
	}

	public static void Unregister(ThermalReceiver receiver)
	{
		receivers.Remove(receiver);
	}
}
