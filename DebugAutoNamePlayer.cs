using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

public class DebugAutoNamePlayer : MonoBehaviour
{
	private float m_authorityPollTimer;

	private float m_joinDelayTimer;

	private bool m_lastIsZoneAuthority;

	private GTZone m_lastZone;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
	}

	private void Update()
	{
	}

	private void OnRoomJoined()
	{
		m_lastZone = GetPrimaryZone();
		m_lastIsZoneAuthority = GetIsZoneAuthority(m_lastZone);
		m_joinDelayTimer = 2f;
		ApplyAutoName();
	}

	private void OnPlayersChanged()
	{
		if (RoomSystem.JoinedRoom)
		{
			ApplyAutoName();
		}
	}

	private void OnZoneChange(ZoneData[] zones)
	{
		if (RoomSystem.JoinedRoom)
		{
			m_lastZone = GetPrimaryZone();
			m_lastIsZoneAuthority = GetIsZoneAuthority(m_lastZone);
			ApplyAutoName();
		}
	}

	private void ApplyAutoName()
	{
		string platformCode = GetPlatformCode();
		int localPlayerID = NetworkSystem.Instance.LocalPlayerID;
		string text = (NetworkSystem.Instance.IsMasterClient ? "MC" : "C");
		GTZone primaryZone = GetPrimaryZone();
		string text2 = (GetIsZoneAuthority(primaryZone) ? "ZA" : "Z");
		string text3 = primaryZone.ToString().ToUpper();
		string text4 = $"{platformCode}_{localPlayerID}_{text}_{text2}_{text3}";
		if (text4.Length > 20)
		{
			text4 = text4.Substring(0, 20);
		}
		NetworkSystem.Instance.SetMyNickName(text4);
		if (GorillaComputer.instance != null)
		{
			GorillaComputer.instance.currentName = text4;
			GorillaComputer.instance.savedName = text4;
			GorillaComputer.instance.SetLocalNameTagText(text4);
		}
		if (NetworkSystem.Instance.InRoom)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, PlayerPrefs.GetFloat("redValue", 0f), PlayerPrefs.GetFloat("greenValue", 0f), PlayerPrefs.GetFloat("blueValue", 0f));
		}
	}

	private static GTZone GetPrimaryZone()
	{
		ZoneManagement instance = ZoneManagement.instance;
		if (instance != null && instance.activeZones.Count > 0)
		{
			return instance.activeZones[0];
		}
		return GTZone.forest;
	}

	private static bool GetIsZoneAuthority(GTZone zone)
	{
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(zone);
		if (managerForZone == null)
		{
			return false;
		}
		NetPlayer localPlayer = NetworkSystem.Instance.LocalPlayer;
		if (localPlayer == null)
		{
			return false;
		}
		return managerForZone.IsAuthorityPlayer(localPlayer);
	}

	private static string GetPlatformCode()
	{
		return "ST";
	}
}
