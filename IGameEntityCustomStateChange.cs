public interface IGameEntityCustomStateChange
{
	bool CanChangeState(long newState, int playerId);
}
