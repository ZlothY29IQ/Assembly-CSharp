using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor.SoakTasks;

public sealed class SoakTaskGrabThrow : IGhostReactorSoakTask
{
	public const float TIME_TO_HOLD_ENTITY = 0.1f;

	private readonly GRPlayer _grPlayer;

	private GameEntityId? _heldEntityId;

	private float? _dropEntityTime;

	public bool Complete { get; private set; }

	public SoakTaskGrabThrow(GRPlayer grPlayer)
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
		if (!_dropEntityTime.HasValue || !_heldEntityId.HasValue)
		{
			List<GameEntity> list = managerForZone.GetGameEntities().ShuffleIntoCollection<List<GameEntity>, GameEntity>();
			GameEntity gameEntity = null;
			foreach (GameEntity item in list)
			{
				if (!(item == null) && !item.IsHeld() && item.pickupable && !(item.gameObject.GetComponent<GameAgent>() != null))
				{
					gameEntity = item;
					break;
				}
			}
			if (gameEntity != null)
			{
				Debug.Log($"Soak grabbing entity {gameEntity.id.index}");
				managerForZone.RequestGrabEntity(gameEntity.id, isLeftHand: true, Vector3.zero, Quaternion.identity);
				_heldEntityId = gameEntity.id;
				_dropEntityTime = Time.time + 0.1f;
			}
		}
		else if (_heldEntityId.HasValue)
		{
			float? dropEntityTime = _dropEntityTime;
			if (dropEntityTime.HasValue)
			{
				float valueOrDefault = dropEntityTime.GetValueOrDefault();
				if (Time.time >= valueOrDefault)
				{
					Debug.Log($"Soak dropping entity {_heldEntityId.Value.index}");
					managerForZone.RequestThrowEntity(_heldEntityId.Value, isLeftHand: true, Vector3.zero, Vector3.zero, Vector3.zero);
					_heldEntityId = null;
					_dropEntityTime = null;
					Complete = true;
				}
			}
		}
		return true;
	}

	public void Reset()
	{
		_heldEntityId = null;
		_dropEntityTime = null;
		Complete = false;
	}
}
