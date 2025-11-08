using UnityEngine;

namespace GorillaTag.Audio;

public class LoudSpeakerVolume : MonoBehaviour
{
	[SerializeField]
	private LoudSpeakerTrigger _trigger;

	public void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag("GorillaPlayer"))
		{
			return;
		}
		VRRig component = other.attachedRigidbody.GetComponent<VRRig>();
		if (component != null && component.creator != null)
		{
			if (component.creator.UserId == NetworkSystem.Instance.LocalPlayer.UserId)
			{
				_trigger.OnPlayerEnter(component);
			}
		}
		else
		{
			Debug.LogWarning("LoudSpeakerNetworkVolume :: OnTriggerEnter no colliding rig found!");
		}
	}

	public void OnTriggerExit(Collider other)
	{
		VRRig component = other.attachedRigidbody.GetComponent<VRRig>();
		if (component != null && component.creator != null)
		{
			if (component.creator.UserId == NetworkSystem.Instance.LocalPlayer.UserId)
			{
				_trigger.OnPlayerExit(component);
			}
		}
		else
		{
			Debug.LogWarning("LoudSpeakerNetworkVolume :: OnTriggerExit no colliding rig found!");
		}
	}
}
