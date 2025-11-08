using System;
using Steamworks;
using UnityEngine;

public class SteamAuthTicket : IDisposable
{
	private HAuthTicket m_hAuthTicket;

	private SteamAuthTicket(HAuthTicket hAuthTicket)
	{
		m_hAuthTicket = hAuthTicket;
	}

	public static implicit operator SteamAuthTicket(HAuthTicket hAuthTicket)
	{
		return new SteamAuthTicket(hAuthTicket);
	}

	~SteamAuthTicket()
	{
		Dispose();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		if (m_hAuthTicket != HAuthTicket.Invalid)
		{
			try
			{
				SteamUser.CancelAuthTicket(m_hAuthTicket);
			}
			catch (InvalidOperationException)
			{
				Debug.LogWarning("Failed to invalidate a Steam auth ticket because the Steam API was shut down. Was it supposed to be disposed of sooner?");
			}
			m_hAuthTicket = HAuthTicket.Invalid;
		}
	}
}
