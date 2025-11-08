using Modio;

public struct ModIORequestResult
{
	public bool success;

	public string message;

	public static ModIORequestResult CreateFailureResult(string inMessage)
	{
		ModIORequestResult result = default(ModIORequestResult);
		result.success = false;
		result.message = inMessage;
		return result;
	}

	public static ModIORequestResult CreateSuccessResult()
	{
		ModIORequestResult result = default(ModIORequestResult);
		result.success = true;
		result.message = "";
		return result;
	}

	public static ModIORequestResult CreateFromError(Error error)
	{
		ModIORequestResult result = default(ModIORequestResult);
		if ((bool)error)
		{
			result.success = false;
			result.message = error.GetMessage();
		}
		else
		{
			result.success = true;
			result.message = "";
		}
		return result;
	}
}
