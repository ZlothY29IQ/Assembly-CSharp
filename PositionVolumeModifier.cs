using UnityEngine;

public class PositionVolumeModifier : MonoBehaviour
{
	public TimeOfDayDependentAudio audioToMod;

	public void OnTriggerStay(Collider other)
	{
		audioToMod.isModified = true;
	}
}
