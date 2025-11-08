namespace GorillaTag;

public abstract class ListProcessorAbstract<T> : ListProcessor<T>
{
	protected ListProcessorAbstract()
	{
		m_itemProcessorDelegate = ProcessItem;
	}

	protected ListProcessorAbstract(int capacity)
		: base(capacity, (InAction<T>)null)
	{
		m_itemProcessorDelegate = ProcessItem;
	}

	protected abstract void ProcessItem(in T item);
}
