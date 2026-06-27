public class VerifyAgeData
{
	public readonly SessionStatus Status;

	public readonly TMPSession Session;

	public VerifyAgeData(VerifyAgeResponse response)
	{
		if (response != null)
		{
			Status = response.Status;
			if (response.Session != null || response.DefaultSession != null)
			{
				Session = new TMPSession(response.Session, response.DefaultSession, Status);
			}
		}
	}
}
