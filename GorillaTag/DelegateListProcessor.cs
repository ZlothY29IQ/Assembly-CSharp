using System;

namespace GorillaTag;

public class DelegateListProcessor : DelegateListProcessorPlusMinus<DelegateListProcessor, Action>
{
	public DelegateListProcessor()
	{
	}

	public DelegateListProcessor(int capacity)
		: base(capacity)
	{
	}

	public void Invoke()
	{
		ProcessList();
	}

	public void InvokeSafe()
	{
		ProcessListSafe();
	}

	protected override void ProcessItem(in Action del)
	{
		del();
	}
}
public class DelegateListProcessor<T> : DelegateListProcessorPlusMinus<DelegateListProcessor<T>, Action<T>>
{
	private T m_data;

	public DelegateListProcessor()
	{
	}

	public DelegateListProcessor(int capacity)
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

	protected override void ProcessItem(in Action<T> item)
	{
		item(m_data);
	}
}
public class DelegateListProcessor<T1, T2> : DelegateListProcessorPlusMinus<DelegateListProcessor<T1, T2>, Action<T1, T2>>
{
	private T1 m_data1;

	private T2 m_data2;

	public DelegateListProcessor()
	{
	}

	public DelegateListProcessor(int capacity)
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

	protected override void ProcessItem(in Action<T1, T2> item)
	{
		item(m_data1, m_data2);
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
