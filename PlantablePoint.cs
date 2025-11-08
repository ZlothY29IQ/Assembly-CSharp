using UnityEngine;

public class PlantablePoint : MonoBehaviour
{
	public bool shouldBeSet;

	public LayerMask floorMask;

	public PlantableObject plantableObject;

	private void OnTriggerEnter(Collider other)
	{
		if (((int)floorMask & (1 << other.gameObject.layer)) != 0)
		{
			plantableObject.SetPlanted(newPlanted: true);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (((int)floorMask & (1 << other.gameObject.layer)) != 0)
		{
			plantableObject.SetPlanted(newPlanted: false);
		}
	}
}
