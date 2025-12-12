using System.Collections.Generic;
using UnityEngine;

public class ThermalSourceVolume : MonoBehaviour
{
	[Tooltip("Temperature in celsius. Default is 20 which is room temperature.")]
	public float celsius = 20f;

	public float innerRadius = 0.1f;

	public float outerRadius = 1f;

	[Tooltip("Exclude these thermal receivers from being impacted by this source")]
	public List<ThermalReceiver> exclusionReceivers = new List<ThermalReceiver>();

	protected void OnEnable()
	{
		ThermalManager.Register(this);
	}

	protected void OnDisable()
	{
		ThermalManager.Unregister(this);
	}
}
