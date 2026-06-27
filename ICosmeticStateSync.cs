public interface ICosmeticStateSync
{
	int StateValue { get; }

	void OnStateUpdate(int state);
}
