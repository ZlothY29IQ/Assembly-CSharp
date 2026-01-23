using UnityEngine;

public class GRSummonedEntity : MonoBehaviour, IGameEntityComponent
{
	private GameEntityId summonerEntityId = GameEntityId.Invalid;

	private GameEntity entity;

	private IGRSummoningEntity summoner;

	private void Awake()
	{
		entity = GetComponent<GameEntity>();
	}

	public void OnEntityInit()
	{
		summonerEntityId = entity.createdByEntityId;
		if (summonerEntityId.IsValid())
		{
			summoner = FindSummoner();
			if (summoner != null)
			{
				summoner.OnSummonedEntityInit(entity);
			}
		}
	}

	public GameEntityId GetSummonerID()
	{
		return summonerEntityId;
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
		if (summonerEntityId.IsValid())
		{
			GameEntity gameEntity = GhostReactorManager.Get(entity).gameEntityManager.GetGameEntity(summonerEntityId);
			if (gameEntity != null)
			{
				return gameEntity.GetComponent<IGRSummoningEntity>();
			}
		}
		return null;
	}
}
