using System.Collections;
using Photon.Voice.Unity;
using UnityEngine;

namespace GorillaTag.Audio;

public class GTRecorder : Recorder, ITickSystemPost
{
	public bool AllowPitchAdjustment;

	public float PitchAdjustment = 1f;

	public bool AllowVolumeAdjustment;

	public float VolumeAdjustment = 1f;

	public float DebugEchoLength = 5f;

	private GTMicWrapper _micWrapper;

	private Coroutine _testEchoCoroutine;

	public bool PostTickRunning { get; set; }

	private void OnEnable()
	{
		TickSystem<object>.AddPostTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemovePostTickCallback(this);
	}

	protected override MicWrapper CreateMicWrapper(string micDev, int samplingRateInt, VoiceLogger logger)
	{
		_micWrapper = new GTMicWrapper(micDev, samplingRateInt, AllowPitchAdjustment, PitchAdjustment, AllowVolumeAdjustment, VolumeAdjustment, logger);
		return _micWrapper;
	}

	private IEnumerator DoTestEcho()
	{
		base.DebugEchoMode = true;
		yield return new WaitForSeconds(DebugEchoLength);
		base.DebugEchoMode = false;
		yield return null;
		_testEchoCoroutine = null;
	}

	public void PostTick()
	{
		if (_micWrapper != null)
		{
			_micWrapper.UpdateWrapper(AllowPitchAdjustment, PitchAdjustment, AllowVolumeAdjustment, VolumeAdjustment);
		}
	}
}
