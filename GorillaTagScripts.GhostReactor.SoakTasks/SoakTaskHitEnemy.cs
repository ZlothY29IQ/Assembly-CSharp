using System;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor.SoakTasks;

public sealed class SoakTaskHitEnemy : IGhostReactorSoakTask
{
	public const float TIME_BETWEEN_HITS = 0.1f;

	private readonly GRPlayer _grPlayer;

	private GameEntity _enemy;

	private float? _nextHitTime;

	public bool Complete { get; private set; }

	public SoakTaskHitEnemy(GRPlayer grPlayer)
	{
		_grPlayer = grPlayer;
	}

	public bool Update()
	{
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(_grPlayer.gamePlayer.rig.zoneEntity.currentZone);
		if (managerForZone == null)
		{
			return false;
		}
		if (_enemy != null && !IsLivingEnemy(_enemy))
		{
			Debug.Log($"soak enemy {_enemy.id.index} is dead");
			Complete = true;
			return true;
		}
		if (_enemy == null)
		{
			foreach (GameEntity item in managerForZone.GetGameEntities().ShuffleIntoCollection<List<GameEntity>, GameEntity>())
			{
				if (!(item == null) && !item.IsHeld() && !(item.GetComponent<GameAgent>() == null) && !(item.GetComponent<GameHittable>() == null) && IsEnemy(item))
				{
					_enemy = item;
					_nextHitTime = Time.time + 0.1f;
					break;
				}
			}
			return _enemy != null;
		}
		if (!_nextHitTime.HasValue)
		{
			throw new Exception("Invalid state in HitEnemySoakTask.");
		}
		if (Time.time < _nextHitTime.Value)
		{
			return true;
		}
		Debug.Log($"soak hitting enemy {_enemy.id.index}");
		GameEntity randomTool = GetRandomTool();
		if ((object)randomTool == null)
		{
			Debug.LogError("No club found for soak task hit enemy.");
			return false;
		}
		GameHitData hit = new GameHitData
		{
			hitEntityId = _enemy.id,
			hitByEntityId = randomTool.id,
			hitTypeId = 0,
			hitEntityPosition = Vector3.zero,
			hitPosition = Vector3.zero,
			hitImpulse = Vector3.zero,
			hitAmount = 1
		};
		managerForZone.RequestHit(hit);
		_nextHitTime = Time.time + 0.1f;
		return true;
	}

	public void Reset()
	{
		_enemy = null;
		_nextHitTime = null;
		Complete = false;
	}

	private GameEntity GetRandomTool()
	{
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(_grPlayer.gamePlayer.rig.zoneEntity.currentZone);
		if (managerForZone == null)
		{
			return null;
		}
		foreach (GameEntity item in managerForZone.GetGameEntities().ShuffleIntoCollection<List<GameEntity>, GameEntity>())
		{
			if (item == null)
			{
				continue;
			}
			GRTool component = item.GetComponent<GRTool>();
			if ((object)component != null)
			{
				GRTool.GRToolType toolType = component.toolType;
				if (toolType == GRTool.GRToolType.Club || toolType == GRTool.GRToolType.HockeyStick)
				{
					return item;
				}
			}
		}
		return null;
	}

	private static bool IsEnemy(GameEntity entity)
	{
		if (!(entity.GetComponent<GREnemyChaser>() != null) && !(entity.GetComponent<GREnemyPest>() != null) && !(entity.GetComponent<GREnemyRanged>() != null) && !(entity.GetComponent<GREnemySummoner>() != null))
		{
			return entity.GetComponent<GREnemyMonkeye>() != null;
		}
		return true;
	}

	private static bool IsLivingEnemy(GameEntity entity)
	{
		if (IsEnemy(entity))
		{
			GREnemyChaser component = entity.GetComponent<GREnemyChaser>();
			if ((object)component == null || component.hp <= 0)
			{
				GREnemyPest component2 = entity.GetComponent<GREnemyPest>();
				if ((object)component2 == null || component2.hp <= 0)
				{
					GREnemyRanged component3 = entity.GetComponent<GREnemyRanged>();
					if ((object)component3 == null || component3.hp <= 0)
					{
						GREnemySummoner component4 = entity.GetComponent<GREnemySummoner>();
						if ((object)component4 == null || component4.hp <= 0)
						{
							GREnemyMonkeye component5 = entity.GetComponent<GREnemyMonkeye>();
							if ((object)component5 != null)
							{
								return component5.hp > 0;
							}
							return false;
						}
					}
				}
			}
			return true;
		}
		return false;
	}
}
