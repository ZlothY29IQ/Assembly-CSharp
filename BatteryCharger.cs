using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class BatteryCharger : MonoBehaviour
{
	[Serializable]
	private class EventPhaseObjects
	{
		public string friendlyName;

		public GameObject[] objects;
	}

	[Serializable]
	private class BatteryChargerEvent
	{
		public enum VDirection
		{
			Up,
			Down
		}

		[SerializeField]
		private VDirection direction;

		[SerializeField]
		private float value;

		[SerializeField]
		private UnityEvent action;

		public VDirection Direction => direction;

		public float Value => value;

		public UnityEvent Action => action;
	}

	[Header("Network State")]
	[SerializeField]
	private XSceneRef stateRef;

	[Header("Charge Visuals")]
	[Tooltip("Transform rotated on its local Z axis to show charge level")]
	[SerializeField]
	private Transform chargeFillTransform;

	[Tooltip("Local Z rotation in degrees when fully charged")]
	[SerializeField]
	private float chargeFullRollAngle = -180f;

	[Tooltip("Renderer whose material color lerps with charge")]
	[SerializeField]
	private Renderer chargeFillRenderer;

	[SerializeField]
	private Color emptyColor = Color.red;

	[SerializeField]
	private Color fullColor = Color.green;

	[Header("Audio")]
	[SerializeField]
	private AudioSource chargingLoopSound;

	[SerializeField]
	private AudioSource fullyChargedSound;

	[Header("Event Phases")]
	[SerializeField]
	private EventPhaseObjects[] eventPhases;

	private BatteryChargerState state;

	private BatteryChargerCrank[] cranks = new BatteryChargerCrank[20];

	private int crankCount;

	[SerializeField]
	private BatteryChargerEvent[] actions;

	private float previousCharge;

	public int CurrentEventPhase
	{
		get
		{
			if (!(state != null))
			{
				return -1;
			}
			return state.EventPhase;
		}
	}

	private int LocalActorNr
	{
		get
		{
			if (PhotonNetwork.LocalPlayer == null)
			{
				return -1;
			}
			return PhotonNetwork.LocalPlayer.ActorNumber;
		}
	}

	internal int RegisterCrank(BatteryChargerCrank crank)
	{
		if (crankCount >= 20)
		{
			Debug.LogError($"BatteryCharger: too many cranks (max {20})", this);
			return -1;
		}
		int num = crankCount;
		cranks[num] = crank;
		crankCount++;
		return num;
	}

	private void OnEnable()
	{
		if (stateRef.TryResolve(out BatteryChargerState result))
		{
			Bind(result);
		}
		else
		{
			stateRef.AddCallbackOnLoad(OnStateSceneLoaded);
		}
	}

	private void OnDisable()
	{
		stateRef.RemoveCallbackOnLoad(OnStateSceneLoaded);
		Unbind();
	}

	private void OnStateSceneLoaded()
	{
		if (stateRef.TryResolve(out BatteryChargerState result))
		{
			Bind(result);
		}
	}

	private void Bind(BatteryChargerState newState)
	{
		if (!(state == newState))
		{
			Unbind();
			state = newState;
			if (!(state == null))
			{
				state.onChargeChanged += OnChargeChanged;
				state.onFullyCharged += OnFullyCharged;
				state.onEventPhaseChanged += OnEventPhaseChanged;
				previousCharge = state.CurrentCharge;
				ApplyChargeVisuals();
				OnEventPhaseChanged(state.EventPhase);
			}
		}
	}

	private void Unbind()
	{
		if (!(state == null))
		{
			state.onChargeChanged -= OnChargeChanged;
			state.onFullyCharged -= OnFullyCharged;
			state.onEventPhaseChanged -= OnEventPhaseChanged;
			state = null;
		}
	}

	private void LateUpdate()
	{
		if (state == null)
		{
			return;
		}
		int localActorNr = LocalActorNr;
		for (int i = 0; i < crankCount; i++)
		{
			if (!(cranks[i] == null))
			{
				if (state.crankSyncs[i].holderActorNr == localActorNr)
				{
					state.UpdateLocalCrankState(i, cranks[i].IsHeldLeftHand, cranks[i].CurrentAngle);
				}
				UpdateRemoteCrankVisual(cranks[i], state.crankSyncs[i], localActorNr);
			}
		}
		if (!(chargingLoopSound != null))
		{
			return;
		}
		bool flag = false;
		for (int j = 0; j < 20; j++)
		{
			if (state.crankSyncs[j].holderActorNr != -1)
			{
				flag = true;
				break;
			}
		}
		if (flag && !chargingLoopSound.isPlaying)
		{
			chargingLoopSound.Play();
		}
		else if (!flag && chargingLoopSound.isPlaying)
		{
			chargingLoopSound.Stop();
		}
	}

	private void UpdateRemoteCrankVisual(BatteryChargerCrank crank, BatteryChargerState.CrankSyncState syncState, int localActor)
	{
		if (crank == null || syncState.holderActorNr == localActor)
		{
			return;
		}
		if (syncState.holderActorNr != -1)
		{
			VRRig vRRig = BatteryChargerState.FindRigForActor(syncState.holderActorNr);
			if (vRRig != null)
			{
				crank.UpdateFromRemoteHand(vRRig, syncState.isLeftHand);
				return;
			}
		}
		crank.SetVisualAngle(syncState.angle);
	}

	internal bool IsCrankHeldLocally(int crankIndex)
	{
		if (state == null || crankIndex < 0 || crankIndex >= 20)
		{
			return false;
		}
		return state.crankSyncs[crankIndex].holderActorNr == LocalActorNr;
	}

	public void SetEventPhase(int phase)
	{
		state.SetEventPhase(phase);
	}

	public void SetChargePerCrankDegree(float chargeRate)
	{
		state.SetChargePerCrankDegree(chargeRate);
	}

	internal bool OnCrankGrabbed(int crankIndex, bool isLeftHand)
	{
		return state.NotifyCrankGrabbed(crankIndex, isLeftHand);
	}

	internal void OnCrankReleased(int crankIndex, float finalAngle)
	{
		state.NotifyCrankReleased(crankIndex, finalAngle);
	}

	internal void OnCrankInput(int crankIndex, float degrees)
	{
		state.NotifyCrankInput(crankIndex, degrees);
		ApplyChargeVisuals();
	}

	private void OnChargeChanged()
	{
		for (int i = 0; i < actions.Length; i++)
		{
			if ((actions[i].Direction == BatteryChargerEvent.VDirection.Up && previousCharge < state.CurrentCharge && previousCharge < actions[i].Value && state.CurrentCharge >= actions[i].Value) || (actions[i].Direction == BatteryChargerEvent.VDirection.Down && previousCharge > state.CurrentCharge && previousCharge > actions[i].Value && state.CurrentCharge <= actions[i].Value))
			{
				actions[i].Action?.Invoke();
			}
		}
		previousCharge = state.CurrentCharge;
		ApplyChargeVisuals();
	}

	private void OnFullyCharged()
	{
		if (fullyChargedSound != null)
		{
			fullyChargedSound.GTPlay();
		}
	}

	private void OnEventPhaseChanged(int phase)
	{
		for (int i = 0; i < eventPhases.Length; i++)
		{
			if (eventPhases[i]?.objects == null)
			{
				continue;
			}
			bool active = i == phase;
			for (int j = 0; j < eventPhases[i].objects.Length; j++)
			{
				if (eventPhases[i].objects[j] != null)
				{
					eventPhases[i].objects[j].SetActive(active);
				}
			}
		}
	}

	private void ApplyChargeVisuals()
	{
		if (!(state == null))
		{
			float chargePercent = state.ChargePercent;
			if (chargeFillTransform != null)
			{
				chargeFillTransform.localRotation = Quaternion.Euler(0f, 0f, chargePercent * chargeFullRollAngle);
			}
			if (chargeFillRenderer != null)
			{
				chargeFillRenderer.material.color = Color.Lerp(emptyColor, fullColor, chargePercent);
			}
		}
	}
}
