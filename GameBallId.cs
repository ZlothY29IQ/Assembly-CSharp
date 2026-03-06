public struct GameBallId(int index)
{
	public static GameBallId Invalid = new GameBallId(-1);

	public int index = index;

	public bool IsValid()
	{
		return index != -1;
	}

	public static bool operator ==(GameBallId obj1, GameBallId obj2)
	{
		return obj1.index == obj2.index;
	}

	public static bool operator !=(GameBallId obj1, GameBallId obj2)
	{
		return obj1.index != obj2.index;
	}

	public override bool Equals(object obj)
	{
		GameBallId gameBallId = (GameBallId)obj;
		return index == gameBallId.index;
	}

	public override int GetHashCode()
	{
		return index.GetHashCode();
	}
}
