using System.Collections.Generic;
using GorillaTag;
using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

public class ThermalReceiver : MonoBehaviour, IDynamicFloat, IResettableItem
{
	public float radius = 0.2f;

	[Tooltip("How fast the temperature should change overtime. 1.0 would be instantly.")]
	public float conductivity = 0.3f;

	public ContinuousPropertyArray continuousProperties;

	[Tooltip("Optional: Fire events if temperature goes below or above this threshold - Celsius")]
	public float temperatureThreshold;

	[Tooltip("Exclude these thermal sources from impacting this receiver")]
	public List<ThermalSourceVolume> exclusionSources = new List<ThermalSourceVolume>();

	[Space]
	public UnityEvent OnAboveThreshold;

	public UnityEvent OnBelowThreshold;

	[DebugOption]
	public float celsius;

	public bool wasAboveThreshold;

	private float defaultCelsius;

	public float Farenheit => celsius * 1.8f + 32f;

	public float floatValue => celsius;

	protected void Awake()
	{
		defaultCelsius = celsius;
		wasAboveThreshold = false;
	}

	protected void OnEnable()
	{
		ThermalManager.Register(this);
	}

	protected void OnDisable()
	{
		wasAboveThreshold = false;
		ThermalManager.Unregister(this);
	}

	public void ResetToDefaultState()
	{
		celsius = defaultCelsius;
	}
}
