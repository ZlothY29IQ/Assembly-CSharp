using System.Collections.Generic;

public class StaticRPCLookup
{
	public List<StaticRPCEntry> entries = new List<StaticRPCEntry>();

	private Dictionary<byte, int> eventCodeEntryLookup = new Dictionary<byte, int>();

	private Dictionary<NetworkSystem.StaticRPCPlaceholder, int> placeholderEntryLookup = new Dictionary<NetworkSystem.StaticRPCPlaceholder, int>();

	public void Add(NetworkSystem.StaticRPCPlaceholder placeholder, byte code, NetworkSystem.StaticRPC lookupMethod)
	{
		int count = entries.Count;
		entries.Add(new StaticRPCEntry(placeholder, code, lookupMethod));
		eventCodeEntryLookup.Add(code, count);
		placeholderEntryLookup.Add(placeholder, count);
	}

	public NetworkSystem.StaticRPC CodeToMethod(byte code)
	{
		return entries[eventCodeEntryLookup[code]].lookupMethod;
	}

	public byte PlaceholderToCode(NetworkSystem.StaticRPCPlaceholder placeholder)
	{
		return entries[placeholderEntryLookup[placeholder]].code;
	}
}
