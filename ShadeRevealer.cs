using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ShadeRevealer : TransferrableObject
{
	public enum State
	{
		OFF,
		SCANNING,
		TRACKING,
		LOCKED,
		PRIMED
	}

	[SerializeField]
	private AudioSource initialActivationSFX;

	[SerializeField]
	private AudioSource beamSFX;

	[SerializeField]
	private AudioSource catchSFX;

	[SerializeField]
	private ParticleSystem catchFX;

	[Space]
	[SerializeField]
	private CosmeticCritterCatcherShade shadeCatcher;

	[Space]
	[Tooltip("The transform that represents the origin of the revealer beam.")]
	[SerializeField]
	private Transform beamForward;

	[Tooltip("The maximum length of the beam.")]
	[SerializeField]
	private float beamLength;

	[Tooltip("If the Shade is this close to the beam, set it to flee and have all Revealers enter Tracking mode.")]
	[SerializeField]
	private float trackThreshold;

	[Tooltip("If the Shade is this close to the beam, slow it down.")]
	[SerializeField]
	private float lockThreshold;

	[Tooltip("Editor-only object to help test the thresholds.")]
	[SerializeField]
	private Transform thresholdTester;

	[Tooltip("Whether to draw the tester or not.")]
	[SerializeField]
	private bool drawThresholdTesterInEditor = true;

	[Space]
	[Tooltip("Enable these objects while the beam is in Scanning mode.")]
	[SerializeField]
	private GameObject[] enableWhenScanning;

	[Tooltip("Enable these objects while the beam is in Tracking mode.")]
	[SerializeField]
	private GameObject[] enableWhenTracking;

	[Tooltip("Enable these objects while the beam is in Locked mode.")]
	[SerializeField]
	private GameObject[] enableWhenLocked;

	[Tooltip("Enable these objects while ready to fire.")]
	[SerializeField]
	private GameObject[] enableWhenPrimed;

	[Space]
	[SerializeField]
	private UnityEvent onShadeLaunched;

	private bool isScanning;

	private State currentBeamState;

	private State pendingBeamState;

	private GameObject[] objectsToDisableWhenOff;

	protected override void Awake()
	{
		base.Awake();
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		for (int i = 0; i < enableWhenScanning.Length; i++)
		{
			hashSet.Add(enableWhenScanning[i]);
		}
		for (int j = 0; j < enableWhenTracking.Length; j++)
		{
			hashSet.Add(enableWhenTracking[j]);
		}
		for (int k = 0; k < enableWhenLocked.Length; k++)
		{
			hashSet.Add(enableWhenLocked[k]);
		}
		for (int l = 0; l < enableWhenPrimed.Length; l++)
		{
			hashSet.Add(enableWhenPrimed[l]);
		}
		objectsToDisableWhenOff = new GameObject[hashSet.Count];
		hashSet.CopyTo(objectsToDisableWhenOff);
	}

	private float GetDistanceToBeamRay(Vector3 toPosition)
	{
		return Vector3.Cross(beamForward.forward, toPosition).magnitude;
	}

	public State GetBeamStateForPosition(Vector3 toPosition, float tolerance)
	{
		if (toPosition.magnitude <= beamLength + tolerance && Vector3.Dot(toPosition.normalized, beamForward.forward) > 0f)
		{
			float num = GetDistanceToBeamRay(toPosition) - tolerance;
			if (num <= lockThreshold)
			{
				return State.LOCKED;
			}
			if (num <= trackThreshold)
			{
				return State.TRACKING;
			}
		}
		return State.SCANNING;
	}

	public State GetBeamStateForCritter(CosmeticCritter critter, float tolerance)
	{
		return GetBeamStateForPosition(critter.transform.position - beamForward.position, tolerance);
	}

	public bool CritterWithinBeamThreshold(CosmeticCritter critter, State criteria, float tolerance)
	{
		return GetBeamStateForCritter(critter, tolerance) >= criteria;
	}

	public void SetBestBeamState(State state)
	{
		if (state > pendingBeamState)
		{
			pendingBeamState = state;
		}
	}

	private void SetObjectsEnabledFromState(State state)
	{
		for (int i = 0; i < objectsToDisableWhenOff.Length; i++)
		{
			objectsToDisableWhenOff[i].SetActive(value: false);
		}
		GameObject[] array;
		switch (state)
		{
		default:
			return;
		case State.SCANNING:
			array = enableWhenScanning;
			break;
		case State.TRACKING:
			array = enableWhenTracking;
			break;
		case State.LOCKED:
			array = enableWhenLocked;
			break;
		case State.PRIMED:
			array = enableWhenPrimed;
			break;
		}
		for (int j = 0; j < array.Length; j++)
		{
			array[j].SetActive(value: true);
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (currentBeamState != pendingBeamState)
		{
			currentBeamState = pendingBeamState;
			SetObjectsEnabledFromState(currentBeamState);
		}
		beamSFX.pitch = 1f + shadeCatcher.GetActionTimeFrac() * 2f;
		if (isScanning)
		{
			pendingBeamState = State.SCANNING;
		}
	}

	public void StartScanning()
	{
		shadeCatcher.enabled = true;
		initialActivationSFX.GTPlay();
		beamSFX.GTPlay();
		isScanning = true;
		currentBeamState = State.OFF;
		pendingBeamState = State.SCANNING;
	}

	public void StopScanning()
	{
		if (currentBeamState == State.PRIMED)
		{
			onShadeLaunched?.Invoke();
		}
		shadeCatcher.enabled = false;
		initialActivationSFX.GTStop();
		beamSFX.GTStop();
		isScanning = false;
		currentBeamState = State.OFF;
		pendingBeamState = State.OFF;
		SetObjectsEnabledFromState(State.OFF);
	}

	public void ShadeCaught()
	{
		shadeCatcher.enabled = false;
		beamSFX.GTStop();
		catchSFX.GTPlay();
		catchFX.Play();
		isScanning = false;
		currentBeamState = State.OFF;
		pendingBeamState = State.PRIMED;
	}
}
