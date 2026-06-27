public class UpgradeSessionData
{
	public readonly SessionStatus status;

	public readonly TMPSession session;

	public UpgradeSessionData(UpgradeSessionResponse response)
	{
		status = response.status;
		session = new TMPSession(response.session, null, status);
	}
}
