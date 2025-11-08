namespace BuildSafe;

public class SessionState
{
	public static readonly SessionState Shared = new SessionState();

	public string this[string key]
	{
		get
		{
			return null;
		}
		set
		{
		}
	}
}
