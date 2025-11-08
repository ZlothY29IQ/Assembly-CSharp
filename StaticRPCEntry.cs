public class StaticRPCEntry
{
	public NetworkSystem.StaticRPCPlaceholder placeholder;

	public byte code;

	public NetworkSystem.StaticRPC lookupMethod;

	public StaticRPCEntry(NetworkSystem.StaticRPCPlaceholder placeholder, byte code, NetworkSystem.StaticRPC lookupMethod)
	{
		this.placeholder = placeholder;
		this.code = code;
		this.lookupMethod = lookupMethod;
	}
}
