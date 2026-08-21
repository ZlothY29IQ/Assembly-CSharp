using GorillaTag;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using Utilities;

public class GTPlayerStats : MonoBehaviourPostTick
{
	private FloatAverages m_fps = new FloatAverages(30);

	private IntAverages m_ping = new IntAverages(10);

	private TickSystemTimer m_periodicUpdate = new TickSystemTimer(0.1f);

	public static short Ping { get; private set; }

	public static short FPS { get; private set; }

	public static short TargetFPS { get; private set; }

	public static long GetPackedValues()
	{
		return (long)(0uL | (ulong)Ping | (ulong)((long)FPS << 16)) | ((long)TargetFPS << 32);
	}

	public static PlayerStatsReadonly UnPackValues(long values)
	{
		short ping = (short)values;
		short fps = (short)(values >> 16);
		short targetFps = (short)(values >> 32);
		return new PlayerStatsReadonly(ping, fps, targetFps);
	}

	private void Awake()
	{
		m_periodicUpdate.callback = DelayedUpdate;
	}

	public override void OnEnable()
	{
		m_periodicUpdate.Start();
		DelayedUpdate();
	}

	public override void OnDisable()
	{
		m_periodicUpdate.Stop();
	}

	public override void PostTick()
	{
	}

	private void DelayedUpdate()
	{
		float smoothDeltaTime = Time.smoothDeltaTime;
		if (smoothDeltaTime > 0f)
		{
			float sample = 1f / smoothDeltaTime;
			m_fps.AddSample(sample);
		}
		FPS = (short)Mathf.RoundToInt(m_fps.Average);
		_ = FPS;
		_ = DebugHudStats.FPS_THRESHOLD;
		int sample2 = 0;
		if (PhotonNetwork.IsConnectedAndReady)
		{
			sample2 = PhotonNetwork.GetPing();
		}
		m_ping.AddSample(sample2);
		Ping = (short)m_ping.Average;
		TargetFPS = (short)Screen.currentResolution.refreshRateRatio.value;
		if (!XRSettings.enabled)
		{
			int vSyncCount = QualitySettings.vSyncCount;
			if (vSyncCount > 0)
			{
				TargetFPS /= (short)vSyncCount;
			}
			else if (Application.targetFrameRate < 0)
			{
				TargetFPS = -1;
			}
		}
	}
}
