public interface IGameStateProvider
{
	void GameStateReceiverRegister(IGameStateReceiver receiver);

	void GameStateReceiverUnregister(IGameStateReceiver receiver);
}
