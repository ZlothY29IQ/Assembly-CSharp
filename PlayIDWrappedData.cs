internal struct PlayIDWrappedData<T>(T initialValue)
{
	private T currentValue = initialValue;

	private T initialValue = initialValue;

	private EnterPlayID id = EnterPlayID.GetCurrent();

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
}
