using UnityEngine;

public class GhostLab : MonoBehaviourTick, IBuildValidation
{
	public enum EntranceDoorsState
	{
		BothClosed,
		InnerDoorOpen,
		OuterDoorOpen
	}

	public Transform outerDoor;

	public Transform innerDoor;

	public Vector3 doorTravelDistance;

	public float doorMoveSpeed;

	public float singleDoorMoveSpeed;

	public EntranceDoorsState doorState;

	public GhostLabReliableState relState;

	public Transform[] slidingDoor;

	public Vector3 singleDoorTravelDistance;

	private bool[] doorOpen;

	private void Awake()
	{
		relState = Object.FindFirstObjectByType<GhostLabReliableState>();
		doorState = EntranceDoorsState.BothClosed;
		doorOpen = new bool[relState.singleDoorCount];
	}

	public bool BuildValidationCheck()
	{
		return true;
	}

	public void DoorButtonPress(int buttonIndex, bool forSingleDoor)
	{
		if (!forSingleDoor)
		{
			UpdateEntranceDoorsState(buttonIndex);
			return;
		}
		UpdateDoorState(buttonIndex);
		relState.UpdateSingleDoorState(buttonIndex);
	}

	public void UpdateDoorState(int buttonIndex)
	{
		if ((doorOpen[buttonIndex] && slidingDoor[buttonIndex].localPosition == singleDoorTravelDistance) || (!doorOpen[buttonIndex] && slidingDoor[buttonIndex].localPosition == Vector3.zero))
		{
			doorOpen[buttonIndex] = !doorOpen[buttonIndex];
		}
	}

	public void UpdateEntranceDoorsState(int buttonIndex)
	{
		if (outerDoor == null || innerDoor == null)
		{
			return;
		}
		if (doorState == EntranceDoorsState.BothClosed)
		{
			if (!(innerDoor.localPosition != Vector3.zero) && !(outerDoor.localPosition != Vector3.zero))
			{
				if (buttonIndex == 0 || buttonIndex == 1)
				{
					doorState = EntranceDoorsState.OuterDoorOpen;
				}
				if (buttonIndex == 2 || buttonIndex == 3)
				{
					doorState = EntranceDoorsState.InnerDoorOpen;
				}
			}
		}
		else if (innerDoor.localPosition == doorTravelDistance || outerDoor.localPosition == doorTravelDistance)
		{
			doorState = EntranceDoorsState.BothClosed;
		}
		relState.UpdateEntranceDoorsState(doorState);
	}

	public override void Tick()
	{
		SynchStates();
		if (innerDoor != null && outerDoor != null)
		{
			Vector3 zero = Vector3.zero;
			Vector3 zero2 = Vector3.zero;
			switch (doorState)
			{
			case EntranceDoorsState.InnerDoorOpen:
				zero2 = doorTravelDistance;
				break;
			case EntranceDoorsState.OuterDoorOpen:
				zero = doorTravelDistance;
				break;
			}
			outerDoor.localPosition = Vector3.MoveTowards(outerDoor.localPosition, zero, doorMoveSpeed * Time.deltaTime);
			innerDoor.localPosition = Vector3.MoveTowards(innerDoor.localPosition, zero2, doorMoveSpeed * Time.deltaTime);
		}
		Vector3 zero3 = Vector3.zero;
		for (int i = 0; i < slidingDoor.Length; i++)
		{
			zero3 = ((!doorOpen[i]) ? Vector3.zero : singleDoorTravelDistance);
			slidingDoor[i].localPosition = Vector3.MoveTowards(slidingDoor[i].localPosition, zero3, singleDoorMoveSpeed * Time.deltaTime);
		}
	}

	private void SynchStates()
	{
		doorState = relState.doorState;
		for (int i = 0; i < doorOpen.Length; i++)
		{
			doorOpen[i] = relState.singleDoorOpen[i];
		}
	}

	public bool IsDoorMoving(bool singleDoor, int index)
	{
		if (singleDoor)
		{
			if (!doorOpen[index] || !(slidingDoor[index].localPosition != singleDoorTravelDistance))
			{
				if (!doorOpen[index])
				{
					return slidingDoor[index].localPosition != Vector3.zero;
				}
				return false;
			}
			return true;
		}
		if (index == 0 || index == 1)
		{
			if (doorState != EntranceDoorsState.OuterDoorOpen || !(outerDoor.localPosition != doorTravelDistance))
			{
				if (doorState != EntranceDoorsState.OuterDoorOpen)
				{
					return outerDoor.localPosition != Vector3.zero;
				}
				return false;
			}
			return true;
		}
		if (doorState != EntranceDoorsState.InnerDoorOpen || !(innerDoor.localPosition != doorTravelDistance))
		{
			if (doorState != EntranceDoorsState.InnerDoorOpen)
			{
				return innerDoor.localPosition != Vector3.zero;
			}
			return false;
		}
		return true;
	}
}
