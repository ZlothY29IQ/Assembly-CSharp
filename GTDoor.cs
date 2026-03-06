using System;
using BoingKit;
using Fusion;
using GorillaTag;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Scripting;

public class GTDoor : NetworkSceneObject
{
	public enum DoorState
	{
		Closed,
		ClosingWaitingOnRPC,
		Closing,
		Open,
		OpeningWaitingOnRPC,
		Opening,
		HeldOpen,
		HeldOpenLocally
	}

	[SerializeField]
	private Transform doorTransform;

	[SerializeField]
	private Collider[] doorColliders;

	[SerializeField]
	private GTDoorTrigger[] doorButtonTriggers;

	[SerializeField]
	private GTDoorTrigger[] doorHoldOpenTriggers;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip openSound;

	[SerializeField]
	private AudioClip closeSound;

	[SerializeField]
	private float doorOpenSpeed = 1f;

	[SerializeField]
	private float doorCloseSpeed = 1f;

	[SerializeField]
	[Range(1.5f, 10f)]
	private float timeUntilDoorCloses = 3f;

	private int GTDoorID;

	[DebugOption]
	private DoorState currentState;

	private float tLastOpened;

	private FloatSpring doorSpring;

	[DebugOption]
	private bool peopleInHoldOpenVolume;

	[DebugOption]
	private bool buttonTriggeredThisFrame;

	private float lastChecked;

	private float secondsCheck = 1f;

	protected override void Start()
	{
		base.Start();
		Collider[] array = doorColliders;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = true;
		}
		tLastOpened = 0f;
		GTDoorTrigger[] array2 = doorButtonTriggers;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].TriggeredEvent.AddListener(DoorButtonTriggered);
		}
	}

	private void Update()
	{
		if (currentState == DoorState.Open || currentState == DoorState.Closed)
		{
			if (Time.time < lastChecked + secondsCheck)
			{
				return;
			}
			lastChecked = Time.time;
		}
		UpdateDoorState();
		UpdateDoorAnimation();
		if (currentState == DoorState.Closed)
		{
			Collider[] array = doorColliders;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
		}
		else
		{
			Collider[] array = doorColliders;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
		}
	}

	private void UpdateDoorState()
	{
		peopleInHoldOpenVolume = false;
		GTDoorTrigger[] array = doorHoldOpenTriggers;
		foreach (GTDoorTrigger obj in array)
		{
			obj.ValidateOverlappingColliders();
			if (obj.overlapCount > 0)
			{
				peopleInHoldOpenVolume = true;
				break;
			}
		}
		switch (currentState)
		{
		case DoorState.Open:
			if (!(Time.time - tLastOpened > timeUntilDoorCloses))
			{
				break;
			}
			if (peopleInHoldOpenVolume)
			{
				currentState = DoorState.HeldOpenLocally;
				if (NetworkSystem.Instance.InRoom && base.IsMine)
				{
					photonView.RPC("ChangeDoorState", RpcTarget.AllViaServer, DoorState.HeldOpen);
				}
			}
			else if (!NetworkSystem.Instance.InRoom)
			{
				CloseDoor();
			}
			else if (base.IsMine)
			{
				currentState = DoorState.ClosingWaitingOnRPC;
				photonView.RPC("ChangeDoorState", RpcTarget.AllViaServer, DoorState.Closing);
			}
			break;
		case DoorState.Closing:
			if (doorSpring.Value < 1f)
			{
				currentState = DoorState.Closed;
			}
			if (peopleInHoldOpenVolume)
			{
				currentState = DoorState.HeldOpenLocally;
				if (NetworkSystem.Instance.InRoom && base.IsMine)
				{
					photonView.RPC("ChangeDoorState", RpcTarget.AllViaServer, DoorState.HeldOpen);
				}
				audioSource.GTPlayOneShot(openSound);
			}
			break;
		case DoorState.Opening:
			if (doorSpring.Value > 89f)
			{
				currentState = DoorState.Open;
			}
			break;
		case DoorState.HeldOpen:
			if (!peopleInHoldOpenVolume)
			{
				if (!NetworkSystem.Instance.InRoom)
				{
					CloseDoor();
				}
				else if (base.IsMine)
				{
					currentState = DoorState.ClosingWaitingOnRPC;
					photonView.RPC("ChangeDoorState", RpcTarget.AllViaServer, DoorState.Closing);
				}
			}
			break;
		case DoorState.HeldOpenLocally:
			if (!peopleInHoldOpenVolume)
			{
				CloseDoor();
			}
			break;
		case DoorState.Closed:
			if (buttonTriggeredThisFrame)
			{
				buttonTriggeredThisFrame = false;
				if (!NetworkSystem.Instance.InRoom)
				{
					OpenDoor();
					break;
				}
				currentState = DoorState.OpeningWaitingOnRPC;
				photonView.RPC("ChangeDoorState", RpcTarget.AllViaServer, DoorState.Opening);
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case DoorState.ClosingWaitingOnRPC:
		case DoorState.OpeningWaitingOnRPC:
			break;
		}
		if (!NetworkSystem.Instance.InRoom)
		{
			switch (currentState)
			{
			case DoorState.ClosingWaitingOnRPC:
				CloseDoor();
				break;
			case DoorState.OpeningWaitingOnRPC:
				OpenDoor();
				break;
			}
		}
	}

	private void DoorButtonTriggered()
	{
		DoorState doorState = currentState;
		if ((uint)(doorState - 3) > 4u)
		{
			buttonTriggeredThisFrame = true;
		}
	}

	private void OpenDoor()
	{
		switch (currentState)
		{
		case DoorState.Closed:
		case DoorState.OpeningWaitingOnRPC:
			ResetDoorOpenedTime();
			audioSource.GTPlayOneShot(openSound);
			currentState = DoorState.Opening;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case DoorState.ClosingWaitingOnRPC:
		case DoorState.Closing:
		case DoorState.Open:
		case DoorState.Opening:
		case DoorState.HeldOpen:
		case DoorState.HeldOpenLocally:
			break;
		}
	}

	private void CloseDoor()
	{
		switch (currentState)
		{
		case DoorState.ClosingWaitingOnRPC:
		case DoorState.Open:
		case DoorState.HeldOpen:
		case DoorState.HeldOpenLocally:
			audioSource.GTPlayOneShot(closeSound);
			currentState = DoorState.Closing;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case DoorState.Closed:
		case DoorState.Closing:
		case DoorState.OpeningWaitingOnRPC:
		case DoorState.Opening:
			break;
		}
	}

	private void UpdateDoorAnimation()
	{
		switch (currentState)
		{
		case DoorState.ClosingWaitingOnRPC:
		case DoorState.Open:
		case DoorState.Opening:
		case DoorState.HeldOpen:
		case DoorState.HeldOpenLocally:
			doorSpring.TrackDampingRatio(90f, MathF.PI * doorOpenSpeed, 1f, Time.deltaTime);
			doorTransform.localRotation = Quaternion.Euler(new Vector3(0f, doorSpring.Value, 0f));
			break;
		default:
			doorSpring.TrackDampingRatio(0f, MathF.PI * doorCloseSpeed, 1f, Time.deltaTime);
			doorTransform.localRotation = Quaternion.Euler(new Vector3(0f, doorSpring.Value, 0f));
			break;
		}
	}

	public void ResetDoorOpenedTime()
	{
		tLastOpened = Time.time;
	}

	[PunRPC]
	public void ChangeDoorState(DoorState shouldOpenState, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ChangeDoorState");
		ChangeDoorStateShared(shouldOpenState);
	}

	[Rpc]
	public unsafe static void RPC_ChangeDoorState(NetworkRunner runner, DoorState shouldOpenState, int doorId)
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int num = 8;
			num += 4;
			num += 4;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GTDoor::RPC_ChangeDoorState(Fusion.NetworkRunner,GTDoor/DoorState,System.Int32)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GTDoor::RPC_ChangeDoorState(Fusion.NetworkRunner,GTDoor/DoorState,System.Int32)"));
				int num2 = 8;
				*(DoorState*)(ptr2 + num2) = shouldOpenState;
				num2 += 4;
				*(int*)(ptr2 + num2) = doorId;
				num2 += 4;
				ptr->Offset = num2 * 8;
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
		}
		GTDoor[] array = UnityEngine.Object.FindObjectsByType<GTDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		if (array == null || array.Length == 0)
		{
			return;
		}
		GTDoor[] array2 = array;
		foreach (GTDoor gTDoor in array2)
		{
			if (gTDoor.GTDoorID == doorId)
			{
				gTDoor.ChangeDoorStateShared(shouldOpenState);
			}
		}
	}

	private void ChangeDoorStateShared(DoorState shouldOpenState)
	{
		switch (shouldOpenState)
		{
		case DoorState.HeldOpen:
			switch (currentState)
			{
			case DoorState.Open:
			case DoorState.HeldOpenLocally:
				currentState = DoorState.HeldOpen;
				break;
			case DoorState.Closing:
				audioSource.GTPlayOneShot(openSound);
				currentState = DoorState.HeldOpen;
				break;
			case DoorState.Closed:
			case DoorState.ClosingWaitingOnRPC:
			case DoorState.OpeningWaitingOnRPC:
			case DoorState.Opening:
			case DoorState.HeldOpen:
				break;
			}
			break;
		case DoorState.Closing:
			switch (currentState)
			{
			case DoorState.ClosingWaitingOnRPC:
			case DoorState.Open:
			case DoorState.HeldOpen:
				CloseDoor();
				break;
			case DoorState.Closed:
			case DoorState.Closing:
			case DoorState.OpeningWaitingOnRPC:
			case DoorState.Opening:
			case DoorState.HeldOpenLocally:
				break;
			}
			break;
		case DoorState.Opening:
			switch (currentState)
			{
			case DoorState.Closed:
			case DoorState.OpeningWaitingOnRPC:
				OpenDoor();
				break;
			case DoorState.ClosingWaitingOnRPC:
			case DoorState.Closing:
			case DoorState.Open:
			case DoorState.Opening:
			case DoorState.HeldOpen:
			case DoorState.HeldOpenLocally:
				break;
			}
			break;
		case DoorState.Closed:
		case DoorState.ClosingWaitingOnRPC:
		case DoorState.Open:
		case DoorState.OpeningWaitingOnRPC:
		case DoorState.HeldOpenLocally:
			break;
		}
	}

	public void SetupDoorIDs()
	{
		GTDoor[] array = UnityEngine.Object.FindObjectsByType<GTDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].GTDoorID = i + 1;
		}
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GTDoor::RPC_ChangeDoorState(Fusion.NetworkRunner,GTDoor/DoorState,System.Int32)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_ChangeDoorState_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		DoorState shouldOpenState = (DoorState)num2;
		int num3 = *(int*)(ptr + num);
		num += 4;
		int doorId = num3;
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_ChangeDoorState(runner, shouldOpenState, doorId);
	}
}
