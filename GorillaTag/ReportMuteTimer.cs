using Photon.Realtime;

namespace GorillaTag;

internal class ReportMuteTimer : TickSystemTimerAbstract, ObjectPoolEvents
{
	private static readonly NetEventOptions netEventOptions = new NetEventOptions
	{
		Flags = new WebFlags(3),
		TargetActors = new int[1] { -1 }
	};

	private static readonly object[] content = new object[6];

	private const byte evCode = 51;

	private string m_playerID;

	private string m_nickName;

	public int Muted { get; set; }

	public override void OnTimedEvent()
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			Stop();
			return;
		}
		content[0] = m_playerID;
		content[1] = Muted;
		content[2] = ((m_nickName.Length > 12) ? m_nickName.Remove(12) : m_nickName);
		content[3] = NetworkSystem.Instance.LocalPlayer.NickName;
		content[4] = !NetworkSystem.Instance.SessionIsPrivate;
		content[5] = NetworkSystem.Instance.RoomStringStripped();
		NetworkSystemRaiseEvent.RaiseEvent(51, content, netEventOptions, reliable: true);
		Stop();
	}

	public void SetReportData(string id, string name, int muted)
	{
		Muted = muted;
		m_playerID = id;
		m_nickName = name;
	}

	void ObjectPoolEvents.OnTaken()
	{
	}

	void ObjectPoolEvents.OnReturned()
	{
		if (base.Running)
		{
			OnTimedEvent();
		}
		m_playerID = string.Empty;
		m_nickName = string.Empty;
		Muted = 0;
	}
}
