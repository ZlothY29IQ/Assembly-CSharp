using UnityEngine;

public class GRElevatorButton : MonoBehaviour
{
	public GRElevator.ButtonType buttonType;

	public GameObject buttonLit;

	public float litUpTime;

	public DisableGameObjectDelayed disableDelayed;

	public bool tempLight;

	private void Awake()
	{
		if (disableDelayed == null)
		{
			disableDelayed = buttonLit.GetComponent<DisableGameObjectDelayed>();
		}
		if (tempLight)
		{
			disableDelayed.enabled = false;
		}
		else
		{
			disableDelayed.delayTime = litUpTime;
		}
	}

	public void Pressed()
	{
		buttonLit.SetActive(value: true);
	}

	public void Depressed()
	{
		buttonLit.SetActive(value: false);
	}
}
