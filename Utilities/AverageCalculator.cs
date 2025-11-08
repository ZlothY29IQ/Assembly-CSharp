using System.Runtime.CompilerServices;

namespace Utilities;

public abstract class AverageCalculator<T> where T : struct
{
	private T[] m_samples;

	private T m_average;

	private T m_total;

	private int m_index;

	public T Average => m_average;

	public AverageCalculator(int sampleCount)
	{
		m_samples = new T[sampleCount];
	}

	public virtual void AddSample(T sample)
	{
		T sample2 = m_samples[m_index];
		m_total = MinusEquals(m_total, sample2);
		m_total = PlusEquals(m_total, sample);
		m_average = Divide(m_total, m_samples.Length);
		m_samples[m_index] = sample;
		m_index = ++m_index % m_samples.Length;
	}

	public virtual void Reset()
	{
		T val = DefaultTypeValue();
		for (int i = 0; i < m_samples.Length; i++)
		{
			m_samples[i] = val;
		}
		m_index = 0;
		m_average = val;
		m_total = Multiply(val, m_samples.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected virtual T DefaultTypeValue()
	{
		return default(T);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected abstract T PlusEquals(T value, T sample);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected abstract T MinusEquals(T value, T sample);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected abstract T Divide(T value, int sampleCount);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected abstract T Multiply(T value, int sampleCount);
}
