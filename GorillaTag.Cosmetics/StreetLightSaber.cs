using System;
using System.Collections.Generic;
using GorillaLocomotion.Climbing;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace GorillaTag.Cosmetics;

public class StreetLightSaber : MonoBehaviour
{
	[Serializable]
	public class StaffStates
	{
		public State state;

		public Color color;

		public UnityEvent onEnterState;

		public UnityEvent onExitState;

		public UnityEvent<Vector3> OnSuccessfulHit;
	}

	public enum State
	{
		Off,
		Green,
		Red
	}

	[SerializeField]
	private float autoSwitchTimer = 5f;

	[SerializeField]
	private TrailRenderer trailRenderer;

	[SerializeField]
	private Renderer meshRenderer;

	[SerializeField]
	private string shaderColorProperty;

	[SerializeField]
	private int materialIndex;

	[SerializeField]
	private GorillaVelocityTracker velocityTracker;

	[SerializeField]
	private float minHitVelocityThreshold;

	private static readonly State[] values = (State[])Enum.GetValues(typeof(State));

	[Space]
	[Header("Staff State Settings")]
	public StaffStates[] allStates = new StaffStates[0];

	private int currentIndex;

	private Dictionary<State, StaffStates> allStatesDict = new Dictionary<State, StaffStates>();

	private bool autoSwitch;

	private float autoSwitchEnabledTime;

	private int hashId;

	private Material instancedMaterial;

	private State CurrentState => values[currentIndex];

	private void Awake()
	{
		StaffStates[] array = allStates;
		foreach (StaffStates staffStates in array)
		{
			allStatesDict[staffStates.state] = staffStates;
		}
		currentIndex = 0;
		autoSwitchEnabledTime = 0f;
		hashId = Shader.PropertyToID(shaderColorProperty);
		List<Material> value;
		using (CollectionPool<List<Material>, Material>.Get(out value))
		{
			meshRenderer.GetSharedMaterials(value);
			instancedMaterial = new Material(value[materialIndex]);
			value[materialIndex] = instancedMaterial;
			meshRenderer.SetSharedMaterials(value);
		}
	}

	private void Update()
	{
		if (autoSwitch && Time.time - autoSwitchEnabledTime > autoSwitchTimer)
		{
			UpdateStateAuto();
		}
	}

	private void OnDestroy()
	{
		allStatesDict.Clear();
	}

	private void OnEnable()
	{
		ForceSwitchTo(State.Off);
	}

	public void UpdateStateManual()
	{
		int newIndex = (currentIndex + 1) % values.Length;
		SwitchState(newIndex);
	}

	private void UpdateStateAuto()
	{
		State value = ((CurrentState != State.Green) ? State.Green : State.Red);
		int newIndex = Array.IndexOf(values, value);
		SwitchState(newIndex);
		autoSwitchEnabledTime = Time.time;
	}

	public void EnableAutoSwitch(bool enable)
	{
		autoSwitch = enable;
	}

	public void ResetStaff()
	{
		ForceSwitchTo(State.Off);
	}

	public void HitReceived(Vector3 contact)
	{
		if (velocityTracker != null && velocityTracker.GetLatestVelocity(worldSpace: true).magnitude >= minHitVelocityThreshold)
		{
			allStatesDict[CurrentState]?.OnSuccessfulHit.Invoke(contact);
		}
	}

	private void SwitchState(int newIndex)
	{
		if (newIndex == currentIndex)
		{
			return;
		}
		State currentState = CurrentState;
		State key = values[newIndex];
		if (allStatesDict.TryGetValue(currentState, out var value))
		{
			value.onExitState?.Invoke();
		}
		currentIndex = newIndex;
		if (allStatesDict.TryGetValue(key, out var value2))
		{
			value2.onEnterState?.Invoke();
			if (trailRenderer != null)
			{
				trailRenderer.startColor = value2.color;
			}
			if (meshRenderer != null)
			{
				instancedMaterial.SetColor(hashId, value2.color);
			}
		}
	}

	private void ForceSwitchTo(State targetState)
	{
		int num = Array.IndexOf(values, targetState);
		if (num >= 0)
		{
			SwitchState(num);
		}
	}
}
