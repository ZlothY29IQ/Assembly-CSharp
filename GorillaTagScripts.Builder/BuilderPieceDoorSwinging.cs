using System;
using BoingKit;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderPieceDoorSwinging : MonoBehaviour, IBuilderPieceComponent, IBuilderPieceFunctional
{
	private enum SwingingDoorState
	{
		Closed,
		ClosingOut,
		OpenOut,
		OpeningOut,
		HeldOpenOut,
		ClosingIn,
		OpenIn,
		OpeningIn,
		HeldOpenIn
	}

	[SerializeField]
	private BuilderPiece myPiece;

	[SerializeField]
	private Vector3 rotateAxis = Vector3.up;

	[SerializeField]
	private Transform doorTransform;

	[SerializeField]
	private Collider[] triggerVolumes;

	[SerializeField]
	private BuilderSmallMonkeTrigger[] doorHoldTriggers;

	[SerializeField]
	private BuilderSmallHandTrigger frontTrigger;

	[SerializeField]
	private BuilderSmallHandTrigger backTrigger;

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

	[SerializeField]
	private float doorClosedVelocityMag = 30f;

	[SerializeField]
	private float dampingRatio = 0.5f;

	[Header("Double Door Settings")]
	[SerializeField]
	private bool isDoubleDoor;

	[SerializeField]
	private Vector3 rotateAxisB = Vector3.down;

	[SerializeField]
	private Transform doorTransformB;

	private SwingingDoorState currentState;

	private float tLastOpened;

	private FloatSpring doorSpring;

	private bool peopleInHoldOpenVolume;

	private double checkHoldTriggersTime;

	private float checkHoldTriggersDelay = 3f;

	private int pushDirection = 1;

	private void Awake()
	{
		BuilderSmallMonkeTrigger[] array = doorHoldTriggers;
		foreach (BuilderSmallMonkeTrigger obj in array)
		{
			obj.onTriggerFirstEntered += OnHoldTriggerEntered;
			obj.onTriggerLastExited += OnHoldTriggerExited;
		}
		frontTrigger.TriggeredEvent.AddListener(OnFrontTriggerEntered);
		backTrigger.TriggeredEvent.AddListener(OnBackTriggerEntered);
	}

	private void OnDestroy()
	{
		BuilderSmallMonkeTrigger[] array = doorHoldTriggers;
		foreach (BuilderSmallMonkeTrigger obj in array)
		{
			obj.onTriggerFirstEntered -= OnHoldTriggerEntered;
			obj.onTriggerLastExited -= OnHoldTriggerExited;
		}
		frontTrigger.TriggeredEvent.RemoveListener(OnFrontTriggerEntered);
		backTrigger.TriggeredEvent.RemoveListener(OnBackTriggerEntered);
	}

	private void OnFrontTriggerEntered()
	{
		if (currentState == SwingingDoorState.Closed)
		{
			if (NetworkSystem.Instance.IsMasterClient)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 7, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			else
			{
				myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 7);
			}
		}
	}

	private void OnBackTriggerEntered()
	{
		if (currentState == SwingingDoorState.Closed)
		{
			if (NetworkSystem.Instance.IsMasterClient)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 3, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			else
			{
				myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 3);
			}
		}
	}

	private void OnHoldTriggerEntered()
	{
		peopleInHoldOpenVolume = true;
		if (NetworkSystem.Instance.IsMasterClient)
		{
			switch (currentState)
			{
			case SwingingDoorState.ClosingOut:
				openSound.Play();
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 4, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
				break;
			case SwingingDoorState.ClosingIn:
				openSound.Play();
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 8, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
				break;
			}
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
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (currentState == SwingingDoorState.HeldOpenIn && !peopleInHoldOpenVolume)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 5, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			else if (currentState == SwingingDoorState.HeldOpenOut && !peopleInHoldOpenVolume)
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
		}
	}

	private void SetDoorState(SwingingDoorState value)
	{
		bool num = currentState == SwingingDoorState.Closed;
		bool flag = value == SwingingDoorState.Closed;
		currentState = value;
		if (currentState == SwingingDoorState.Closed)
		{
			frontTrigger.enabled = true;
			backTrigger.enabled = true;
		}
		else
		{
			frontTrigger.enabled = false;
			backTrigger.enabled = false;
		}
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
		case SwingingDoorState.ClosingOut:
		case SwingingDoorState.ClosingIn:
			if (Mathf.Abs(doorSpring.Value) < 1f && Mathf.Abs(doorSpring.Velocity) < doorClosedVelocityMag)
			{
				SetDoorState(SwingingDoorState.Closed);
			}
			break;
		case SwingingDoorState.OpenOut:
		case SwingingDoorState.OpenIn:
		{
			if (!(Time.time - tLastOpened > timeUntilDoorCloses))
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
				SwingingDoorState swingingDoorState2 = ((currentState == SwingingDoorState.OpenIn) ? SwingingDoorState.HeldOpenIn : SwingingDoorState.HeldOpenOut);
				checkHoldTriggersTime = Time.time + checkHoldTriggersDelay;
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, (byte)swingingDoorState2, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			else
			{
				SwingingDoorState swingingDoorState3 = ((currentState != SwingingDoorState.OpenIn) ? SwingingDoorState.ClosingOut : SwingingDoorState.ClosingIn);
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, (byte)swingingDoorState3, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
			break;
		}
		case SwingingDoorState.OpeningIn:
			if (Mathf.Abs(doorSpring.Value) > 89f)
			{
				SetDoorState(SwingingDoorState.OpenIn);
			}
			break;
		case SwingingDoorState.OpeningOut:
			if (Mathf.Abs(doorSpring.Value) > 89f)
			{
				SetDoorState(SwingingDoorState.OpenOut);
			}
			break;
		case SwingingDoorState.HeldOpenOut:
		case SwingingDoorState.HeldOpenIn:
		{
			if (!((double)Time.time > checkHoldTriggersTime))
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
				checkHoldTriggersTime = Time.time + checkHoldTriggersDelay;
				break;
			}
			SwingingDoorState swingingDoorState = ((currentState != SwingingDoorState.HeldOpenIn) ? SwingingDoorState.ClosingOut : SwingingDoorState.ClosingIn);
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, (byte)swingingDoorState, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			break;
		}
		}
	}

	private void UpdateDoorState()
	{
		switch (currentState)
		{
		case SwingingDoorState.OpeningIn:
			if (Mathf.Abs(doorSpring.Value) > 89f)
			{
				SetDoorState(SwingingDoorState.OpenIn);
			}
			break;
		case SwingingDoorState.OpeningOut:
			if (Mathf.Abs(doorSpring.Value) > 89f)
			{
				SetDoorState(SwingingDoorState.OpenOut);
			}
			break;
		case SwingingDoorState.ClosingOut:
		case SwingingDoorState.ClosingIn:
			if (Mathf.Abs(doorSpring.Value) < 1f && Mathf.Abs(doorSpring.Velocity) < doorClosedVelocityMag)
			{
				SetDoorState(SwingingDoorState.Closed);
			}
			break;
		case SwingingDoorState.OpenOut:
		case SwingingDoorState.HeldOpenOut:
		case SwingingDoorState.OpenIn:
			break;
		}
	}

	private void CloseDoor()
	{
		switch (currentState)
		{
		case SwingingDoorState.OpenIn:
		case SwingingDoorState.HeldOpenIn:
			closeSound.Play();
			SetDoorState(SwingingDoorState.ClosingIn);
			break;
		case SwingingDoorState.OpenOut:
		case SwingingDoorState.HeldOpenOut:
			closeSound.Play();
			SetDoorState(SwingingDoorState.ClosingOut);
			break;
		case SwingingDoorState.OpeningOut:
		case SwingingDoorState.ClosingIn:
		case SwingingDoorState.OpeningIn:
			break;
		}
	}

	private void OpenDoor(bool openIn)
	{
		if (currentState == SwingingDoorState.Closed)
		{
			tLastOpened = Time.time;
			openSound.Play();
			SetDoorState(openIn ? SwingingDoorState.OpeningIn : SwingingDoorState.OpeningOut);
		}
	}

	private void UpdateDoorAnimation()
	{
		switch (currentState)
		{
		case SwingingDoorState.OpenIn:
		case SwingingDoorState.OpeningIn:
		case SwingingDoorState.HeldOpenIn:
			doorSpring.TrackDampingRatio(90f, MathF.PI * doorOpenSpeed, 1f, Time.deltaTime);
			doorTransform.localRotation = Quaternion.Euler(rotateAxis * doorSpring.Value);
			if (isDoubleDoor && doorTransformB != null)
			{
				doorTransformB.localRotation = Quaternion.Euler(rotateAxisB * doorSpring.Value);
			}
			break;
		case SwingingDoorState.OpenOut:
		case SwingingDoorState.OpeningOut:
		case SwingingDoorState.HeldOpenOut:
			doorSpring.TrackDampingRatio(-90f, MathF.PI * doorOpenSpeed, 1f, Time.deltaTime);
			doorTransform.localRotation = Quaternion.Euler(rotateAxis * doorSpring.Value);
			if (isDoubleDoor && doorTransformB != null)
			{
				doorTransformB.localRotation = Quaternion.Euler(rotateAxisB * doorSpring.Value);
			}
			break;
		default:
			doorSpring.TrackDampingRatio(0f, MathF.PI * doorCloseSpeed, dampingRatio, Time.deltaTime);
			doorTransform.localRotation = Quaternion.Euler(rotateAxis * doorSpring.Value);
			if (isDoubleDoor && doorTransformB != null)
			{
				doorTransformB.localRotation = Quaternion.Euler(rotateAxisB * doorSpring.Value);
			}
			break;
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		tLastOpened = 0f;
		SetDoorState(SwingingDoorState.Closed);
		doorSpring.Reset();
		Collider[] array = triggerVolumes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
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
		SetDoorState(SwingingDoorState.Closed);
		doorSpring.Reset();
		doorTransform.localRotation = Quaternion.Euler(rotateAxis * doorSpring.Value);
		if (isDoubleDoor && doorTransformB != null)
		{
			doorTransformB.localRotation = Quaternion.Euler(rotateAxisB * doorSpring.Value);
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
		case 7:
			if (currentState == SwingingDoorState.Closed)
			{
				OpenDoor(openIn: true);
			}
			break;
		case 3:
			if (currentState == SwingingDoorState.Closed)
			{
				OpenDoor(openIn: false);
			}
			break;
		case 8:
			if (currentState == SwingingDoorState.ClosingIn)
			{
				openSound.Play();
			}
			break;
		case 4:
			if (currentState == SwingingDoorState.ClosingOut)
			{
				openSound.Play();
			}
			break;
		case 5:
			if (currentState == SwingingDoorState.OpenIn || currentState == SwingingDoorState.HeldOpenIn)
			{
				CloseDoor();
			}
			break;
		case 1:
			if (currentState == SwingingDoorState.OpenOut || currentState == SwingingDoorState.HeldOpenOut)
			{
				CloseDoor();
			}
			break;
		}
		SetDoorState((SwingingDoorState)newState);
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (NetworkSystem.Instance.IsMasterClient && IsStateValid(newState) && instigator != null && (newState == 7 || newState == 3) && currentState == SwingingDoorState.Closed)
		{
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, newState, instigator.GetPlayerRef(), timeStamp);
		}
	}

	public bool IsStateValid(byte state)
	{
		return state <= 8;
	}

	public void FunctionalPieceUpdate()
	{
		if (myPiece != null && myPiece.state == BuilderPiece.State.AttachedAndPlaced)
		{
			if (!NetworkSystem.Instance.InRoom && currentState != SwingingDoorState.Closed)
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
