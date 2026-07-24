using UnityEngine;

public class GreyZoneAreaEnable : MonoBehaviour
{
	private void OnEnable()
	{
		GreyZoneManager.Instance?.RegisterArea(this);
	}

	private void OnDisable()
	{
		GreyZoneManager.Instance.UnRegisterArea(this);
	}
}
