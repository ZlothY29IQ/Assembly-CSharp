using GorillaLocomotion;
using GorillaTagScripts.VirtualStumpCustomMaps;
using UnityEngine;

public class VStumpActivateTrigger : MonoBehaviour
{
	[Tooltip("Which hallway this is: FeatureA -> featured map 0, FeatureB -> featured map 1, Custom -> open the stump with no auto-load.")]
	[SerializeField]
	private VirtualStumpActivateMode mode = VirtualStumpActivateMode.FeatureA;

	private bool armed = true;

	public void OnTriggerEnter(Collider other)
	{
		if (armed && !(other != GTPlayer.Instance.headCollider))
		{
			armed = false;
			CustomMapManager.Activate(mode);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			armed = true;
		}
	}
}
