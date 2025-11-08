using System;
using System.Text;
using Steamworks;
using UnityEngine;

public class SteamAuthenticator : MonoBehaviour
{
	public HAuthTicket GetAuthTicket(Action<string> successCallback, Action<EResult> failureCallback)
	{
		HAuthTicket ticketHandle = HAuthTicket.Invalid;
		Callback<GetAuthSessionTicketResponse_t> ticketCallback = null;
		byte[] ticketBlob = new byte[1024];
		uint ticketSize = 0u;
		ticketCallback = Callback<GetAuthSessionTicketResponse_t>.Create(delegate(GetAuthSessionTicketResponse_t response)
		{
			if (!(response.m_hAuthTicket != ticketHandle))
			{
				ticketCallback.Dispose();
				ticketCallback = null;
				if (response.m_eResult != EResult.k_EResultOK)
				{
					failureCallback?.Invoke(response.m_eResult);
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					for (uint num = 0u; num < ticketSize; num++)
					{
						stringBuilder.AppendFormat("{0:x2}", ticketBlob[num]);
					}
					successCallback?.Invoke(stringBuilder.ToString());
				}
			}
		});
		SteamNetworkingIdentity pSteamNetworkingIdentity = default(SteamNetworkingIdentity);
		ticketHandle = SteamUser.GetAuthSessionTicket(ticketBlob, ticketBlob.Length, out ticketSize, ref pSteamNetworkingIdentity);
		if (ticketHandle == HAuthTicket.Invalid)
		{
			failureCallback?.Invoke(EResult.k_EResultFail);
		}
		return ticketHandle;
	}

	public HAuthTicket GetAuthTicketForWebApi(string authenticatorId, Action<string> successCallback, Action<EResult> failureCallback)
	{
		HAuthTicket ticketHandle = HAuthTicket.Invalid;
		Callback<GetTicketForWebApiResponse_t> ticketCallback = null;
		ticketCallback = Callback<GetTicketForWebApiResponse_t>.Create(delegate(GetTicketForWebApiResponse_t response)
		{
			if (!(response.m_hAuthTicket != ticketHandle))
			{
				ticketCallback.Dispose();
				ticketCallback = null;
				if (response.m_eResult != EResult.k_EResultOK)
				{
					failureCallback?.Invoke(response.m_eResult);
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					for (int i = 0; i < response.m_cubTicket; i++)
					{
						stringBuilder.AppendFormat("{0:x2}", response.m_rgubTicket[i]);
					}
					successCallback?.Invoke(stringBuilder.ToString());
				}
			}
		});
		ticketHandle = SteamUser.GetAuthTicketForWebApi(authenticatorId);
		if (ticketHandle == HAuthTicket.Invalid)
		{
			failureCallback?.Invoke(EResult.k_EResultFail);
		}
		return ticketHandle;
	}
}
