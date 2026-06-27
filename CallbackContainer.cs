using GorillaTag;

internal class CallbackContainer<T> : ListProcessorAbstract<T> where T : ICallBack
{
	public CallbackContainer()
		: base(100)
	{
	}

	public CallbackContainer(int capacity)
		: base(capacity)
	{
	}

	public void TryRunCallbacks()
	{
		ProcessListSafe();
	}

	public void RunCallbacks()
	{
		ProcessList();
	}

	protected override void ProcessItem(in T item)
	{
		item.CallBack();
	}
}
