using UnityEngine;

namespace GorillaTag.Audio;

public class LoudSpeakerActivator : MonoBehaviour
{
	public float PitchAdjustment = 1f;

	public float VolumeAdjustment = 2.5f;

	public bool IsBroadcasting;

	[SerializeField]
	private LoudSpeakerNetwork _network;

	[SerializeField]
	private GTRecorder _recorder;

	private bool _isLocal;

	private VRRig _nonlocalRig;

	private void Awake()
	{
		_isLocal = IsParentedToLocalRig();
		if (!_isLocal)
		{
			_nonlocalRig = base.transform.root.GetComponent<VRRig>();
		}
	}

	private bool IsParentedToLocalRig()
	{
		if (VRRigCache.Instance.localRig == null)
		{
			return false;
		}
		Transform parent = base.transform.parent;
		while (parent != null)
		{
			if (parent == VRRigCache.Instance.localRig.transform)
			{
				return true;
			}
			parent = parent.parent;
		}
		return false;
	}

	public void SetRecorder(GTRecorder recorder)
	{
		_recorder = recorder;
	}

	public void StartLocalBroadcast()
	{
		if (!_isLocal)
		{
			if (_network != null && _nonlocalRig != null)
			{
				_network.StartBroadcastSpeakerOutput(_nonlocalRig);
			}
		}
		else if (!IsBroadcasting)
		{
			if (_recorder == null && NetworkSystem.Instance.LocalRecorder != null)
			{
				SetRecorder((GTRecorder)NetworkSystem.Instance.LocalRecorder);
			}
			if (_recorder != null && _network != null)
			{
				IsBroadcasting = true;
				_recorder.AllowPitchAdjustment = true;
				_recorder.PitchAdjustment = PitchAdjustment;
				_recorder.AllowVolumeAdjustment = true;
				_recorder.VolumeAdjustment = VolumeAdjustment;
				_network.StartBroadcastSpeakerOutput(VRRigCache.Instance.localRig.Rig);
			}
		}
	}

	public void StopLocalBroadcast()
	{
		if (!_isLocal)
		{
			if (_network != null && _nonlocalRig != null)
			{
				_network.StopBroadcastSpeakerOutput(_nonlocalRig);
			}
		}
		else if (IsBroadcasting)
		{
			if (_recorder == null && NetworkSystem.Instance.LocalRecorder != null)
			{
				SetRecorder((GTRecorder)NetworkSystem.Instance.LocalRecorder);
			}
			if (_recorder != null && _network != null)
			{
				IsBroadcasting = false;
				_recorder.AllowPitchAdjustment = false;
				_recorder.PitchAdjustment = 1f;
				_recorder.AllowVolumeAdjustment = false;
				_recorder.VolumeAdjustment = 1f;
				_network.StopBroadcastSpeakerOutput(VRRigCache.Instance.localRig.Rig);
			}
		}
	}
}
