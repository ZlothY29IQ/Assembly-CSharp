using Photon.Realtime;

public class NetEventOptions
{
	public enum RecieverTarget
	{
		others,
		all,
		master
	}

	public RecieverTarget Reciever;

	public int[] TargetActors;

	public WebFlags Flags = WebFlags.Default;

	public bool HasWebHooks => Flags != WebFlags.Default;

	public NetEventOptions()
	{
	}

	public NetEventOptions(int reciever, int[] actors, byte flags)
	{
		Reciever = (RecieverTarget)reciever;
		TargetActors = actors;
		Flags = new WebFlags(flags);
	}
}
