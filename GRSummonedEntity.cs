using UnityEngine;

public class GRSummonedEntity : MonoBehaviour, IGameEntityComponent
{
	private int summonerNetID;

	private GameEntity entity;

	private IGRSummoningEntity summoner;

	private void Awake()
	{
		entity = GetComponent<GameEntity>();
	}

	public void OnEntityInit()
	{
		summonerNetID = (int)entity.createData;
		if (summonerNetID != 0)
		{
			summoner = FindSummoner();
			if (summoner != null)
			{
				summoner.OnSummonedEntityInit(entity);
			}
		}
	}

	public int GetSummonerNetID()
	{
		return summonerNetID;
	}

	public void OnEntityDestroy()
	{
		if (summoner != null)
		{
			summoner.OnSummonedEntityDestroy(entity);
		}
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	private IGRSummoningEntity FindSummoner()
	{
		if (summonerNetID != 0)
		{
			GameEntityManager gameEntityManager = GhostReactorManager.Get(entity).gameEntityManager;
			GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(summonerNetID);
			GameEntity gameEntity = gameEntityManager.GetGameEntity(entityIdFromNetId);
			if (gameEntity != null)
			{
				return gameEntity.GetComponent<IGRSummoningEntity>();
			}
		}
		return null;
	}
}
