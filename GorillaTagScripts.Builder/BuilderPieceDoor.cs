using System;
using BoingKit;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderPieceDoor : MonoBehaviour, IBuilderPieceComponent, IBuilderPieceFunctional
{
	public enum DoorState
	{
		Closed,
		Closing,
		Open,
		Opening,
		HeldOpen
	}

	[SerializeField]
	private BuilderPiece myPiece;

	[SerializeField]
	private Vector3 rotateAxis = Vector3.up;

	[Tooltip("True if the door stays open until the button is triggered again")]
	[SerializeField]
	private bool IsToggled;

	[Tooltip("True if the door opens when players enter the Keep Open Trigger")]
	[SerializeField]
	private bool isAutomatic;

	[SerializeField]
	private Transform doorTransform;

	[SerializeField]
	private Collider[] triggerVolumes;

	[SerializeField]
	private BuilderSmallHandTrigger[] doorButtonTriggers;

	[SerializeField]
	private BuilderSmallMonkeTrigger[] doorHoldTriggers;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private SoundBankPlayer openSound;

	[SerializeField]
	private SoundBankPlayer closeSound;

	[SerializeField]
	private float doorOpenSpeed = 1f;

	[SerializeField]
	private float doorCloseSpeed = 1f;

	[SerializeField]
	[Range(1.5f, 10f)]
	private float timeUntilDoorCloses = 3f;

	[Header("Double Door Settings")]
	[SerializeField]
	private bool isDoubleDoor;

	[SerializeField]
	private Vector3 rotateAxisB = Vector3.down;

	[SerializeField]
	private Transform doorTransformB;

	[SerializeField]
	private LineRenderer[] lineRenderers;

	private DoorState currentState;

	private float tLastOpened;

	private FloatSpring doorSpring;

	private bool peopleInHoldOpenVolume;

	private double CheckHoldTriggersTime;

	private float checkHoldTriggersDelay = 3f;

	private void Awake()
	{
		BuilderSmallMonkeTrigger[] array = doorHoldTriggers;
		foreach (BuilderSmallMonkeTrigger obj in array)
		{
			obj.onTriggerFirstEntered += OnHoldTriggerEntered;
			obj.onTriggerLastExited += OnHoldTriggerExited;
		}
		BuilderSmallHandTrigger[] array2 = doorButtonTriggers;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].TriggeredEvent.AddListener(OnDoorButtonTriggered);
		}
	}

	private void OnDestroy()
	{
		BuilderSmallMonkeTrigger[] array = doorHoldTriggers;
		foreach (BuilderSmallMonkeTrigger obj in array)
		{
			obj.onTriggerFirstEntered -= OnHoldTriggerEntered;
			obj.onTriggerLastExited -= OnHoldTriggerExited;
		}
		BuilderSmallHandTrigger[] array2 = doorButtonTriggers;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].TriggeredEvent.RemoveListener(OnDoorButtonTriggered);
		}
	}

	private void SetDoorState(DoorState value)
	{
		bool num = currentState == DoorState.Closed || (currentState == DoorState.Open && IsToggled);
		bool flag = value switch
		{
			DoorState.Open => IsToggled, 
			DoorState.Closed => true, 
			_ => false, 
		};
		currentState = value;
		if (num != flag)
		{
			if (flag)
			{
				myPiece.GetTable().UnregisterFunctionalPiece(this);
			}
			else
			{
				myPiece.GetTable().RegisterFunctionalPiece(this);
			}
		}
	}

	private void UpdateDoorStateMaster()
	{
		switch (currentState)
		{
		case DoorState.Closing:
			if (doorSpring.Value < 1f)
			{
				doorSpring.Reset();
				doorTransform.localRotation = Quaternion.identity;
				if (isDoubleDoor && doorTransformB != null)
				{
					doorTransformB.localRotation = Quaternion.identity;
				}
				SetDoorState(DoorState.Closed);
			}
			break;
		case DoorState.Open:
		{
			if (IsToggled || !(Time.time - tLastOpened > timeUntilDoorCloses))
			{
				break;
			}
			peopleInHoldOpenVolume = false;
			BuilderSmallMonkeTrigger[] array = doorHoldTriggers;
			foreach (BuilderSmallMonkeTrigger obj2 in array)
			{
				obj2.ValidateOverlappingColliders();
				if (obj2.overlapCount > 0)
				{
					peopleInHoldOpenVolume = true;
					break;
				}
			}
			if (peopleInHoldOpenVolume)
			{
				CheckHoldTriggersTime = Time.time + checkHoldTriggersDelay;
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 4, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			else
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		}
		case DoorState.Opening:
			if (doorSpring.Value > 89f)
			{
				SetDoorState(DoorState.Open);
			}
			break;
		case DoorState.HeldOpen:
		{
			if (IsToggled || !((double)Time.time > CheckHoldTriggersTime))
			{
				break;
			}
			BuilderSmallMonkeTrigger[] array = doorHoldTriggers;
			foreach (BuilderSmallMonkeTrigger obj in array)
			{
				obj.ValidateOverlappingColliders();
				if (obj.overlapCount > 0)
				{
					peopleInHoldOpenVolume = true;
					break;
				}
			}
			if (peopleInHoldOpenVolume)
			{
				CheckHoldTriggersTime = Time.time + checkHoldTriggersDelay;
			}
			else
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		}
		}
	}

	private void UpdateDoorState()
	{
		switch (currentState)
		{
		case DoorState.Opening:
			if (doorSpring.Value > 89f)
			{
				SetDoorState(DoorState.Open);
			}
			break;
		case DoorState.Closing:
			if (doorSpring.Value < 1f)
			{
				doorSpring.Reset();
				doorTransform.localRotation = Quaternion.identity;
				if (isDoubleDoor && doorTransformB != null)
				{
					doorTransformB.localRotation = Quaternion.identity;
				}
				SetDoorState(DoorState.Closed);
			}
			break;
		}
	}

	private void CloseDoor()
	{
		switch (currentState)
		{
		case DoorState.Open:
		case DoorState.HeldOpen:
			closeSound.Play();
			SetDoorState(DoorState.Closing);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case DoorState.Closed:
		case DoorState.Closing:
		case DoorState.Opening:
			break;
		}
	}

	private void OpenDoor()
	{
		switch (currentState)
		{
		case DoorState.Closed:
			tLastOpened = Time.time;
			openSound.Play();
			SetDoorState(DoorState.Opening);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case DoorState.Closing:
		case DoorState.Open:
		case DoorState.Opening:
		case DoorState.HeldOpen:
			break;
		}
	}

	private void UpdateDoorAnimation()
	{
		DoorState doorState = currentState;
		if ((uint)doorState > 1u && (uint)(doorState - 2) <= 2u)
		{
			doorSpring.TrackDampingRatio(90f, MathF.PI * doorOpenSpeed, 1f, Time.deltaTime);
			doorTransform.localRotation = Quaternion.Euler(rotateAxis * doorSpring.Value);
			if (isDoubleDoor && doorTransformB != null)
			{
				doorTransformB.localRotation = Quaternion.Euler(rotateAxisB * doorSpring.Value);
			}
		}
		else
		{
			doorSpring.TrackDampingRatio(0f, MathF.PI * doorCloseSpeed, 1f, Time.deltaTime);
			doorTransform.localRotation = Quaternion.Euler(rotateAxis * doorSpring.Value);
			if (isDoubleDoor && doorTransformB != null)
			{
				doorTransformB.localRotation = Quaternion.Euler(rotateAxisB * doorSpring.Value);
			}
		}
	}

	private void OnDoorButtonTriggered()
	{
		switch (currentState)
		{
		case DoorState.Closed:
			if (NetworkSystem.Instance.IsMasterClient)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 3, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			else
			{
				myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 3);
			}
			break;
		case DoorState.Open:
			if (IsToggled)
			{
				if (NetworkSystem.Instance.IsMasterClient)
				{
					myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
				}
				else
				{
					myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 1);
				}
			}
			break;
		}
	}

	private void OnHoldTriggerEntered()
	{
		peopleInHoldOpenVolume = true;
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		switch (currentState)
		{
		case DoorState.Closed:
			if (isAutomatic)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 3, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		case DoorState.Closing:
			if (!IsToggled)
			{
				openSound.Play();
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 4, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		}
	}

	private void OnHoldTriggerExited()
	{
		peopleInHoldOpenVolume = false;
		BuilderSmallMonkeTrigger[] array = doorHoldTriggers;
		foreach (BuilderSmallMonkeTrigger obj in array)
		{
			obj.ValidateOverlappingColliders();
			if (obj.overlapCount > 0)
			{
				peopleInHoldOpenVolume = true;
				break;
			}
		}
		if (NetworkSystem.Instance.IsMasterClient && currentState == DoorState.HeldOpen && !peopleInHoldOpenVolume)
		{
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		tLastOpened = 0f;
		SetDoorState(DoorState.Closed);
		doorSpring.Reset();
		doorTransform.localRotation = Quaternion.identity;
		if (isDoubleDoor && doorTransformB != null)
		{
			doorTransformB.localRotation = Quaternion.identity;
		}
		Collider[] array = triggerVolumes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		if (lineRenderers != null)
		{
			LineRenderer[] array2 = lineRenderers;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].widthMultiplier = myPiece.GetScale();
			}
		}
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
		Collider[] array = triggerVolumes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = true;
		}
	}

	public void OnPieceDeactivate()
	{
		Collider[] array = triggerVolumes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		myPiece.functionalPieceState = 0;
		SetDoorState(DoorState.Closed);
		doorSpring.Reset();
		doorTransform.localRotation = Quaternion.identity;
		if (isDoubleDoor && doorTransformB != null)
		{
			doorTransformB.localRotation = Quaternion.identity;
		}
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (NetworkSystem.Instance.IsMasterClient && IsStateValid(newState) && instigator != null && currentState != (DoorState)newState)
		{
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, newState, instigator.GetPlayerRef(), timeStamp);
		}
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (!IsStateValid(newState))
		{
			return;
		}
		switch (newState)
		{
		case 3:
			if (currentState == DoorState.Closed)
			{
				OpenDoor();
			}
			break;
		case 4:
			if (currentState == DoorState.Closing)
			{
				openSound.Play();
			}
			break;
		case 1:
			if (currentState == DoorState.Open || currentState == DoorState.HeldOpen)
			{
				CloseDoor();
			}
			break;
		}
		SetDoorState((DoorState)newState);
	}

	public bool IsStateValid(byte state)
	{
		return state < 5;
	}

	public void FunctionalPieceUpdate()
	{
		if (myPiece != null && myPiece.state == BuilderPiece.State.AttachedAndPlaced)
		{
			if (!NetworkSystem.Instance.InRoom && currentState != DoorState.Closed)
			{
				CloseDoor();
			}
			else if (NetworkSystem.Instance.IsMasterClient)
			{
				UpdateDoorStateMaster();
			}
			else
			{
				UpdateDoorState();
			}
			UpdateDoorAnimation();
		}
	}
}
