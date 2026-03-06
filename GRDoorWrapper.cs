using UnityEngine;

public class GRDoorWrapper : MonoBehaviour
{
	[SerializeField]
	private GRDoor grDoor;

	public void ToggleDoor(bool value)
	{
		grDoor.SetDoorState(value ? GRDoor.DoorState.Open : GRDoor.DoorState.Closed);
	}
}
