internal class PropHuntGameModeRPCs : RPCNetworkBase
{
	private GameModeSerializer serializer;

	private GorillaPropHuntGameManager propHuntManager;

	public override void SetClassTarget(IWrappedSerializable target, GorillaWrappedSerializer netHandler)
	{
		propHuntManager = (GorillaPropHuntGameManager)target;
		serializer = (GameModeSerializer)netHandler;
	}
}
