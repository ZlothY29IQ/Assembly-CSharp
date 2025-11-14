internal struct PlayIDWrappedData<T>
{
	private T currentValue;

	private T initialValue;

	private EnterPlayID id;

	public T Value
	{
		get
		{
			if (!id.IsCurrent)
			{
				return initialValue;
			}
			return currentValue;
		}
		set
		{
			currentValue = value;
			id = EnterPlayID.GetCurrent();
		}
	}

	public PlayIDWrappedData(T initialValue)
	{
		currentValue = initialValue;
		this.initialValue = initialValue;
		id = EnterPlayID.GetCurrent();
	}
}
