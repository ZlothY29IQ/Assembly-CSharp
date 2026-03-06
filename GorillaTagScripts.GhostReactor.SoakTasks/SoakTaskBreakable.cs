using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor.SoakTasks;

public sealed class SoakTaskBreakable : IGhostReactorSoakTask
{
	public const float TIME_BETWEEN_HITS = 0.1f;

	private readonly GRPlayer _grPlayer;

	private GameEntity _breakable;

	private float? _nextHitTime;

	public bool Complete { get; private set; }

	public SoakTaskBreakable(GRPlayer grPlayer)
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
		if (_breakable != null && _breakable.GetComponent<GRBreakable>().BrokenLocal)
		{
			Debug.Log($"soak breakable {_breakable.id.index} is broken");
			_breakable = null;
			_nextHitTime = null;
			Complete = true;
		}
		else if (_breakable == null)
		{
			foreach (GameEntity item in managerForZone.GetGameEntities().ShuffleIntoCollection<List<GameEntity>, GameEntity>())
			{
				if (!(item == null) && !item.IsHeld())
				{
					GRBreakable component = item.gameObject.GetComponent<GRBreakable>();
					if ((object)component != null && !component.BrokenLocal)
					{
						_breakable = item;
						_nextHitTime = Time.time + 0.1f;
						break;
					}
				}
			}
		}
		else if (_breakable != null)
		{
			float? nextHitTime = _nextHitTime;
			if (nextHitTime.HasValue)
			{
				float valueOrDefault = nextHitTime.GetValueOrDefault();
				if (Time.time >= valueOrDefault)
				{
					Debug.Log($"soak hit breakable {_breakable.id.index}");
					GameHitData hit = new GameHitData
					{
						hitEntityId = _breakable.id,
						hitByEntityId = _breakable.id,
						hitTypeId = 0,
						hitEntityPosition = Vector3.zero,
						hitPosition = Vector3.zero,
						hitImpulse = Vector3.zero,
						hitAmount = 1
					};
					managerForZone.RequestHit(hit);
				}
			}
		}
		return true;
	}

	public void Reset()
	{
		_breakable = null;
		_nextHitTime = null;
		Complete = false;
	}
}
