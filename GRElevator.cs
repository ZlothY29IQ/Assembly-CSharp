using System;
using System.Collections.Generic;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class GRElevator : MonoBehaviour
{
	public enum ElevatorState
	{
		DoorBeginClosing,
		DoorMovingClosing,
		DoorEndClosing,
		DoorClosed,
		DoorBeginOpening,
		DoorMovingOpening,
		DoorEndOpening,
		DoorOpen,
		None
	}

	[Serializable]
	public enum ButtonType
	{
		Stump = 1,
		City,
		GhostReactor,
		Open,
		Close,
		Summon,
		MonkeBlocks,
		VIMExperience1,
		VIMExperience2,
		VIMExperience3,
		VIMExperience4,
		Count
	}

	public GRElevatorManager.ElevatorLocation location;

	public Transform upperDoor;

	public Transform lowerDoor;

	public Transform closedTargetTop;

	public Transform closedTargetBottom;

	public Transform openTargetTop;

	public Transform openTargetBottom;

	public TextMeshPro outerText;

	public TextMeshPro innerText;

	public List<GRElevatorButton> elevatorButtons;

	private Dictionary<ButtonType, GRElevatorButton> typeButtonDict;

	public GorillaFriendCollider friendCollider;

	public GorillaNetworkJoinTrigger joinTrigger;

	public SoundBankPlayer buttonBank;

	public AudioSource doorAudio;

	public AudioSource ambientAudio;

	public AudioSource musicAudio;

	public AudioClip travellingLoopClip;

	public AudioClip ambientLoopClip;

	public AudioClip dingClip;

	public AudioClip doorOpenClip;

	public AudioClip doorCloseClip;

	public float adjustedOffsetTime;

	public float doorMoveBeginTime;

	public float doorOpenSpeed = 0.5f;

	public float doorCloseSpeed = 0.5f;

	public float closeBeginDuration;

	public float closeTravelDuration;

	public float closeEndDuration;

	public float openBeginDuration;

	public float openTravelDuration;

	public float openEndDuration;

	public float travelDistance;

	public ElevatorState state;

	public GameObject collidersAndVisuals;

	public GameObject videoDisplay;

	public AudioSource videoAudio;

	private void OnEnable()
	{
		GRElevatorManager.RegisterElevator(this);
		ambientAudio.clip = ambientLoopClip;
		ambientAudio.Play();
	}

	private void OnDisable()
	{
		GRElevatorManager.DeregisterElevator(this);
	}

	private void Awake()
	{
		typeButtonDict = new Dictionary<ButtonType, GRElevatorButton>();
		for (int i = 0; i < elevatorButtons.Count; i++)
		{
			typeButtonDict.TryAdd(elevatorButtons[i].buttonType, elevatorButtons[i]);
		}
		travelDistance = (openTargetTop.position - closedTargetTop.position).magnitude;
		doorOpenSpeed = travelDistance / openTravelDuration;
		doorCloseSpeed = travelDistance / closeTravelDuration;
		state = ElevatorState.DoorClosed;
		UpdateLocalState(state);
	}

	public void PressButton(int type)
	{
		GRElevatorManager.ElevatorButtonPressed((ButtonType)type, location);
	}

	public void PressButtonVisuals(ButtonType type)
	{
		if (typeButtonDict.TryGetValue(type, out var value))
		{
			value.Pressed();
		}
	}

	public void PlayDing()
	{
		ambientAudio.PlayOneShot(dingClip);
	}

	public void PlayButtonPress()
	{
		buttonBank.Play();
	}

	public void PlayElevatorMoving()
	{
		if (!ambientAudio.isPlaying || !(ambientAudio.clip == travellingLoopClip))
		{
			ambientAudio.clip = travellingLoopClip;
			ambientAudio.loop = true;
			ambientAudio.time = 0f;
			ambientAudio.Play();
		}
	}

	public void PlayElevatorStopped()
	{
		if (!ambientAudio.isPlaying || !(ambientAudio.clip == ambientLoopClip))
		{
			ambientAudio.clip = ambientLoopClip;
			ambientAudio.loop = true;
			ambientAudio.time = 0f;
			ambientAudio.Play();
		}
	}

	public void PlayElevatorMusic(float time = 0f)
	{
		if (!musicAudio.isPlaying)
		{
			musicAudio.time = time;
			musicAudio.Play();
		}
	}

	public void PlayDoorOpenBegin()
	{
		doorAudio.clip = doorOpenClip;
		doorAudio.time = 0f;
		doorAudio.Play();
	}

	public void PlayDoorCloseBegin()
	{
		doorAudio.clip = doorCloseClip;
		doorAudio.time = 0f;
		doorAudio.Play();
	}

	public void PlayDoorOpenTravel()
	{
		doorAudio.time = adjustedOffsetTime + openBeginDuration;
	}

	public void PlayDoorCloseTravel()
	{
		doorAudio.time = adjustedOffsetTime + closeBeginDuration;
	}

	public bool DoorsFullyClosed()
	{
		return (upperDoor.position - closedTargetTop.position).sqrMagnitude < 0.0001f;
	}

	public bool DoorsFullyOpen()
	{
		return (upperDoor.position - openTargetTop.position).sqrMagnitude < 0.0001f;
	}

	public void UpdateLocalState(ElevatorState newState)
	{
		if (newState == state)
		{
			return;
		}
		state = newState;
		switch (newState)
		{
		case ElevatorState.DoorBeginClosing:
			if (DoorsFullyClosed())
			{
				UpdateLocalState(ElevatorState.DoorClosed);
				break;
			}
			doorMoveBeginTime = Time.time;
			SetDoorClosedBeginTime();
			PlayDoorCloseBegin();
			break;
		case ElevatorState.DoorMovingClosing:
			PlayDoorCloseTravel();
			break;
		case ElevatorState.DoorClosed:
			upperDoor.position = closedTargetTop.position;
			lowerDoor.position = closedTargetBottom.position;
			break;
		case ElevatorState.DoorBeginOpening:
			if (DoorsFullyOpen())
			{
				UpdateLocalState(ElevatorState.DoorOpen);
				break;
			}
			doorMoveBeginTime = Time.time;
			SetDoorOpenBeginTime();
			PlayDoorOpenBegin();
			break;
		case ElevatorState.DoorMovingOpening:
			PlayDoorOpenTravel();
			break;
		case ElevatorState.DoorOpen:
			upperDoor.position = openTargetTop.position;
			lowerDoor.position = openTargetBottom.position;
			break;
		case ElevatorState.DoorEndClosing:
		case ElevatorState.DoorEndOpening:
			break;
		}
	}

	public void UpdateRemoteState(ElevatorState remoteNewState)
	{
		if (StateIsOpeningState(remoteNewState) && StateIsClosingState(state))
		{
			UpdateLocalState(ElevatorState.DoorBeginOpening);
		}
		else if (StateIsClosingState(remoteNewState) && StateIsOpeningState(state))
		{
			UpdateLocalState(ElevatorState.DoorBeginClosing);
		}
	}

	public void SetDoorOpenBeginTime()
	{
		float num = (travelDistance - (upperDoor.position - openTargetTop.position).magnitude) / travelDistance;
		adjustedOffsetTime = num * openTravelDuration;
	}

	public void SetDoorClosedBeginTime()
	{
		float num = (travelDistance - (upperDoor.position - closedTargetTop.position).magnitude) / travelDistance;
		adjustedOffsetTime = num * closeTravelDuration;
	}

	public static bool StateIsOpeningState(ElevatorState checkState)
	{
		if (checkState != ElevatorState.DoorMovingOpening && checkState != ElevatorState.DoorBeginOpening && checkState != ElevatorState.DoorEndOpening)
		{
			return checkState == ElevatorState.DoorOpen;
		}
		return true;
	}

	public static bool StateIsClosingState(ElevatorState checkState)
	{
		if (checkState != ElevatorState.DoorMovingClosing && checkState != ElevatorState.DoorBeginClosing && checkState != ElevatorState.DoorEndClosing)
		{
			return checkState == ElevatorState.DoorClosed;
		}
		return true;
	}

	public bool DoorIsOpening()
	{
		return StateIsOpeningState(state);
	}

	public bool DoorIsClosing()
	{
		return StateIsClosingState(state);
	}

	public void PhysicalElevatorUpdate()
	{
		switch (state)
		{
		case ElevatorState.DoorBeginClosing:
			if (Time.time > doorMoveBeginTime + closeBeginDuration)
			{
				UpdateLocalState(ElevatorState.DoorMovingClosing);
			}
			break;
		case ElevatorState.DoorMovingClosing:
			if (Time.time > doorMoveBeginTime - adjustedOffsetTime + closeBeginDuration + closeTravelDuration)
			{
				UpdateLocalState(ElevatorState.DoorEndClosing);
			}
			break;
		case ElevatorState.DoorEndClosing:
			if (Time.time > doorMoveBeginTime - adjustedOffsetTime + closeBeginDuration + closeTravelDuration + closeEndDuration)
			{
				UpdateLocalState(ElevatorState.DoorClosed);
			}
			break;
		case ElevatorState.DoorBeginOpening:
			if (Time.time > doorMoveBeginTime + openBeginDuration)
			{
				UpdateLocalState(ElevatorState.DoorMovingOpening);
			}
			break;
		case ElevatorState.DoorMovingOpening:
			if (Time.time > doorMoveBeginTime - adjustedOffsetTime + openBeginDuration + openTravelDuration)
			{
				UpdateLocalState(ElevatorState.DoorEndOpening);
			}
			break;
		case ElevatorState.DoorEndOpening:
			if (Time.time > doorMoveBeginTime - adjustedOffsetTime + openBeginDuration + openTravelDuration + openEndDuration)
			{
				UpdateLocalState(ElevatorState.DoorOpen);
			}
			break;
		}
		Transform transform;
		Transform transform2;
		float num;
		switch (state)
		{
		case ElevatorState.DoorMovingOpening:
			transform = openTargetTop;
			transform2 = openTargetBottom;
			num = doorOpenSpeed;
			break;
		case ElevatorState.DoorMovingClosing:
			transform = closedTargetTop;
			transform2 = closedTargetBottom;
			num = doorCloseSpeed;
			break;
		default:
			transform = upperDoor;
			transform2 = lowerDoor;
			num = 1f;
			break;
		}
		upperDoor.position = Vector3.MoveTowards(upperDoor.position, transform.position, Time.deltaTime * num);
		lowerDoor.position = Vector3.MoveTowards(lowerDoor.position, transform2.position, Time.deltaTime * num);
	}
}
