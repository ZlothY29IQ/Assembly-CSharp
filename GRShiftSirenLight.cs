using UnityEngine;

public class GRShiftSirenLight : MonoBehaviourTick
{
	public float rotationRate = 1.25f;

	public Transform greenLightParent;

	public Transform redLightParent;

	public GameObject redLight;

	public GameObject greenLight;

	public GhostReactorShiftManager shiftManager;

	public float dimLight;

	public float brightLight;

	public Light readyRoomLight;

	public override void Tick()
	{
		if (shiftManager == null)
		{
			shiftManager = GhostReactor.instance.shiftManager;
			return;
		}
		if (redLight.activeSelf != shiftManager.ShiftActive)
		{
			redLight.SetActive(shiftManager.ShiftActive);
		}
		if (greenLight.activeSelf == shiftManager.ShiftActive)
		{
			greenLight.SetActive(!shiftManager.ShiftActive);
		}
		if (readyRoomLight != null)
		{
			readyRoomLight.intensity = (shiftManager.ShiftActive ? dimLight : brightLight);
		}
		if (shiftManager.ShiftActive)
		{
			redLightParent.localEulerAngles = new Vector3(0f, Time.time * rotationRate, 0f);
		}
		else
		{
			greenLightParent.localEulerAngles = new Vector3(0f, Time.time * rotationRate, 0f);
		}
	}
}
