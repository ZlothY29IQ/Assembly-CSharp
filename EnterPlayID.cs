public struct EnterPlayID
{
	private static int currentID = 1;

	private int id;

	public bool IsCurrent => id == currentID;

	[OnEnterPlay_Run]
	private static void NextID()
	{
		currentID++;
	}

	public static EnterPlayID GetCurrent()
	{
		return new EnterPlayID
		{
			id = currentID
		};
	}
}
