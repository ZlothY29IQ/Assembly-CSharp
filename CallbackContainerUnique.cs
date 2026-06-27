internal class CallbackContainerUnique<T> : CallbackContainer<T> where T : class, ICallbackUnique
{
	public CallbackContainerUnique()
		: base(10)
	{
	}

	public CallbackContainerUnique(int capacity)
		: base(capacity)
	{
	}

	public override void Add(in T item)
	{
		T val = item;
		if (!val.Registered)
		{
			base.Add(in item);
			val = item;
			val.Registered = true;
		}
	}

	public override bool Remove(in T item)
	{
		T val = item;
		if (!val.Registered)
		{
			return false;
		}
		base.Remove(in item);
		val = item;
		val.Registered = false;
		return true;
	}
}
