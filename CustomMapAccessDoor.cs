using UnityEngine;

public class CustomMapAccessDoor : MonoBehaviour
{
	public GameObject openDoorObject;

	public GameObject closedDoorObject;

	public void OpenDoor()
	{
		if (openDoorObject != null)
		{
			openDoorObject.SetActive(value: true);
		}
		if (closedDoorObject != null)
		{
			closedDoorObject.SetActive(value: false);
		}
	}

	public void CloseDoor()
	{
		if (openDoorObject != null)
		{
			openDoorObject.SetActive(value: false);
		}
		if (closedDoorObject != null)
		{
			closedDoorObject.SetActive(value: true);
		}
	}
}
