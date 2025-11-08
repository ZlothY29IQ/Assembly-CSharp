public struct ModIORequestResultAnd<T>
{
	public ModIORequestResult result;

	public T data;

	public static ModIORequestResultAnd<T> CreateFailureResult(string inMessage)
	{
		ModIORequestResultAnd<T> modIORequestResultAnd = default(ModIORequestResultAnd<T>);
		modIORequestResultAnd.result = ModIORequestResult.CreateFailureResult(inMessage);
		return modIORequestResultAnd;
	}

	public static ModIORequestResultAnd<T> CreateSuccessResult(T payload)
	{
		ModIORequestResultAnd<T> modIORequestResultAnd = default(ModIORequestResultAnd<T>);
		modIORequestResultAnd.result = ModIORequestResult.CreateSuccessResult();
		modIORequestResultAnd.data = payload;
		return modIORequestResultAnd;
	}
}
