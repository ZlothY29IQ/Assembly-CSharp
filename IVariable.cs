using System;

public interface IVariable<T> : IVariable
{
	T Value
	{
		get
		{
			return Get();
		}
		set
		{
			Set(value);
		}
	}

	Type IVariable.ValueType => typeof(T);

	T Get();

	void Set(T value);
}
public interface IVariable
{
	Type ValueType { get; }
}
