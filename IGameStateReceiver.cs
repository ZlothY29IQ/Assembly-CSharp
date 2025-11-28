public interface IGameStateReceiver
{
	void GameStateReceiverOnStateChanged(long oldState, long newState);
}
