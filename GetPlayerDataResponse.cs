using System;
using KID.Model;

[Serializable]
public class GetPlayerDataResponse
{
	public SessionStatus? Status;

	public Session Session;

	public KIDDefaultSession DefaultSession;

	public string[] Permissions;

	public bool HasConfirmedSetup;
}
