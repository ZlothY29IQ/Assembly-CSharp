using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag;

public class ListProcessor<T>
{
	protected readonly List<T> m_list;

	protected int m_currentIndex;

	protected int m_listCount;

	protected InAction<T> m_itemProcessorDelegate;

	public int Count => m_list.Count;

	public InAction<T> ItemProcessor
	{
		get
		{
			return m_itemProcessorDelegate;
		}
		set
		{
			m_itemProcessorDelegate = value;
		}
	}

	public ListProcessor()
		: this(10, (InAction<T>)null)
	{
	}

	public ListProcessor(int capacity, InAction<T> itemProcessorDelegate = null)
	{
		m_list = new List<T>(capacity);
		m_currentIndex = -1;
		m_listCount = -1;
		m_itemProcessorDelegate = itemProcessorDelegate;
	}

	public void Add(in T item)
	{
		m_listCount++;
		m_list.Add(item);
	}

	public void Remove(in T item)
	{
		int num = m_list.IndexOf(item);
		if (num >= 0)
		{
			if (num < m_currentIndex)
			{
				m_currentIndex--;
			}
			m_listCount--;
			m_list.RemoveAt(num);
		}
	}

	public void Clear()
	{
		m_list.Clear();
		m_currentIndex = -1;
		m_listCount = -1;
	}

	public bool Contains(in T item)
	{
		return m_list.Contains(item);
	}

	public virtual void ProcessListSafe()
	{
		if (m_itemProcessorDelegate == null)
		{
			Debug.LogError("ListProcessor: ItemProcessor is null");
			return;
		}
		m_listCount = m_list.Count;
		for (m_currentIndex = 0; m_currentIndex < m_listCount; m_currentIndex++)
		{
			try
			{
				m_itemProcessorDelegate(m_list[m_currentIndex]);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.ToString());
			}
		}
	}

	public virtual void ProcessList()
	{
		if (m_itemProcessorDelegate == null)
		{
			Debug.LogError("ListProcessor: ItemProcessor is null");
			return;
		}
		m_listCount = m_list.Count;
		for (m_currentIndex = 0; m_currentIndex < m_listCount; m_currentIndex++)
		{
			m_itemProcessorDelegate(m_list[m_currentIndex]);
		}
	}
}
