using KID.Model;

public class VerifyAgeResponse
{
	public SessionStatus Status { get; set; }

	public Session? Session { get; set; }

	public KIDDefaultSession DefaultSession { get; set; }
}
