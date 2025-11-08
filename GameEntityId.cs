public struct GameEntityId
{
	public static GameEntityId Invalid = new GameEntityId
	{
		index = -1
	};

	public int index;

	public bool IsValid()
	{
		return index != -1;
	}

	public static bool operator ==(GameEntityId obj1, GameEntityId obj2)
	{
		return obj1.index == obj2.index;
	}

	public static bool operator !=(GameEntityId obj1, GameEntityId obj2)
	{
		return obj1.index != obj2.index;
	}

	public override bool Equals(object obj)
	{
		GameEntityId gameEntityId = (GameEntityId)obj;
		return index == gameEntityId.index;
	}

	public override int GetHashCode()
	{
		return index.GetHashCode();
	}
}
