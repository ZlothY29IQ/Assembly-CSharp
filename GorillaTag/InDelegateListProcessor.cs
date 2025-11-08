namespace GorillaTag;

public class InDelegateListProcessor<T> : DelegateListProcessorPlusMinus<InDelegateListProcessor<T>, InAction<T>>
{
	private T m_data;

	public InDelegateListProcessor()
	{
	}

	public InDelegateListProcessor(int capacity)
		: base(capacity)
	{
	}

	public void InvokeSafe(in T data)
	{
		m_data = data;
		ProcessListSafe();
		m_data = default(T);
	}

	public void Invoke(in T data)
	{
		m_data = data;
		ProcessList();
		m_data = default(T);
	}

	protected override void ProcessItem(in InAction<T> item)
	{
		item(in m_data);
	}
}
public class InDelegateListProcessor<T1, T2> : DelegateListProcessorPlusMinus<InDelegateListProcessor<T1, T2>, InAction<T1, T2>>
{
	private T1 m_data1;

	private T2 m_data2;

	public InDelegateListProcessor()
	{
	}

	public InDelegateListProcessor(int capacity)
		: base(capacity)
	{
	}

	public void InvokeSafe(in T1 data1, in T2 data2)
	{
		SetData(in data1, in data2);
		ProcessListSafe();
		ResetData();
	}

	public void Invoke(in T1 data1, in T2 data2)
	{
		SetData(in data1, in data2);
		ProcessList();
		ResetData();
	}

	protected override void ProcessItem(in InAction<T1, T2> item)
	{
		item(in m_data1, in m_data2);
	}

	private void SetData(in T1 data1, in T2 data2)
	{
		m_data1 = data1;
		m_data2 = data2;
	}

	private void ResetData()
	{
		m_data1 = default(T1);
		m_data2 = default(T2);
	}
}
