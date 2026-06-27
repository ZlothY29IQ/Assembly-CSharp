using UnityEngine;

public class GetPlayerData_Data
{
	public readonly GetSessionResponseType responseType;

	public readonly SessionStatus? status;

	public readonly TMPSession session;

	public readonly string[]? OptInPermissions;

	public readonly bool HasConfirmedSetup;

	public GetPlayerData_Data(GetSessionResponseType type, GetPlayerDataResponse response)
	{
		responseType = type;
		if (response == null)
		{
			if (responseType == GetSessionResponseType.OK)
			{
				responseType = GetSessionResponseType.ERROR;
				Debug.LogError("[KID::GET_PLAYER_DATA_DATA] Incoming [GetPlayerDataResponse] is NULL");
			}
			return;
		}
		status = response.Status;
		if (status.HasValue)
		{
			session = new TMPSession(response.Session, response.DefaultSession, status.Value);
			session.SetOptInPermissions(response.Permissions);
			Debug.Log("[KID::GET_PLAYER_DATA_DATA::OptInRefactor] Setting Opt-in Permissions: " + string.Join(", ", session.GetOptedInPermissions()));
		}
		HasConfirmedSetup = response.HasConfirmedSetup;
	}
}
