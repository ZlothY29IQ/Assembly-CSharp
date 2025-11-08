public abstract class RankedMultiplayerStatistic
{
	public enum SerializationType
	{
		None,
		Mothership,
		PlayerPrefs
	}

	protected SerializationType serializationType = SerializationType.Mothership;

	public string name;

	public bool IsValid { get; protected set; }

	public override string ToString()
	{
		return string.Empty;
	}

	public abstract void Load();

	protected abstract void Save();

	public abstract bool TrySetValue(string valAsString);

	public virtual string WriteToJson()
	{
		return $"{{{name}:\"{ToString()}\"}}";
	}

	public RankedMultiplayerStatistic(string n, SerializationType sType = SerializationType.Mothership)
	{
		serializationType = sType;
		name = n;
		IsValid = serializationType != SerializationType.Mothership;
		_ = serializationType;
		_ = 1;
	}

	protected virtual void HandleUserDataSetSuccess(string keyName)
	{
		if (keyName == name)
		{
			IsValid = true;
		}
	}

	protected virtual void HandleUserDataGetSuccess(string keyName, string value)
	{
		if (keyName == name)
		{
			if (TrySetValue(value))
			{
				IsValid = true;
			}
			else
			{
				Save();
			}
		}
	}

	protected void HandleUserDataGetFailure(string keyName)
	{
		if (keyName == name)
		{
			Save();
			IsValid = true;
		}
	}

	protected void HandleUserDataSetFailure(string keyName)
	{
		if (keyName == name)
		{
			Save();
		}
	}
}
