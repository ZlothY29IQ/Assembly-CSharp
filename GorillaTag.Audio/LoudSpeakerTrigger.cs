using UnityEngine;

namespace GorillaTag.Audio;

public class LoudSpeakerTrigger : MonoBehaviour
{
	public float PitchAdjustment = 1f;

	[SerializeField]
	private LoudSpeakerNetwork _network;

	[SerializeField]
	private GTRecorder _recorder;

	public void SetRecorder(GTRecorder recorder)
	{
		_recorder = recorder;
	}

	public void OnPlayerEnter(VRRig player)
	{
		if (_recorder != null && _network != null)
		{
			_recorder.AllowPitchAdjustment = true;
			_recorder.PitchAdjustment = PitchAdjustment;
			_network.StartBroadcastSpeakerOutput(player);
		}
	}

	public void OnPlayerExit(VRRig player)
	{
		if (_recorder != null && _network != null)
		{
			_recorder.AllowPitchAdjustment = false;
			_recorder.PitchAdjustment = 1f;
			_network.StopBroadcastSpeakerOutput(player);
		}
	}
}
