using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor.SoakTasks;

public sealed class SoakTaskDepositCollectibles : IGhostReactorSoakTask
{
	public const float TIME_TO_HOLD_COLLECTIBLE = 0.1f;

	private readonly GRPlayer _grPlayer;

	private GRCurrencyDepositor _coreDepositor;

	private Vector3? _seedExtractorTriggerLocation;

	private GameEntity _heldEntity;

	private float? _depositCollectibleTime;

	public bool Complete { get; private set; }

	public SoakTaskDepositCollectibles(GRPlayer grPlayer)
	{
		_grPlayer = grPlayer;
	}

	public bool Update()
	{
		if (_coreDepositor == null)
		{
			global::GhostReactor instance = global::GhostReactor.instance;
			if ((object)instance != null)
			{
				_coreDepositor = instance.currencyDepositor;
			}
			if (_coreDepositor == null)
			{
				return false;
			}
		}
		if (!_seedExtractorTriggerLocation.HasValue)
		{
			global::GhostReactor instance2 = global::GhostReactor.instance;
			if ((object)instance2 != null)
			{
				_seedExtractorTriggerLocation = instance2.seedExtractor.transform.Find("DepositorTrigger").position;
			}
			if (!_seedExtractorTriggerLocation.HasValue)
			{
				return false;
			}
		}
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(_grPlayer.gamePlayer.rig.zoneEntity.currentZone);
		if (managerForZone == null)
		{
			return false;
		}
		if (_heldEntity == null || !_depositCollectibleTime.HasValue)
		{
			List<GameEntity> list = managerForZone.GetGameEntities().ShuffleIntoCollection<List<GameEntity>, GameEntity>();
			GameEntity gameEntity = null;
			foreach (GameEntity item in list)
			{
				if (!(item == null) && !item.IsHeld())
				{
					GRCollectible component = item.gameObject.GetComponent<GRCollectible>();
					if ((object)component != null && (component.type == ProgressionManager.CoreType.Core || component.type == ProgressionManager.CoreType.SuperCore || component.type == ProgressionManager.CoreType.ChaosSeed))
					{
						gameEntity = item;
						break;
					}
				}
			}
			if (gameEntity != null)
			{
				Debug.Log($"Soak grabbing core {gameEntity.id.index}");
				managerForZone.RequestGrabEntity(gameEntity.id, isLeftHand: true, Vector3.zero, Quaternion.identity);
				_heldEntity = gameEntity;
				_depositCollectibleTime = Time.time + 0.1f;
			}
		}
		else if (_heldEntity != null)
		{
			float? depositCollectibleTime = _depositCollectibleTime;
			if (depositCollectibleTime.HasValue)
			{
				float valueOrDefault = depositCollectibleTime.GetValueOrDefault();
				if (Time.time >= valueOrDefault)
				{
					GRCollectible component2 = _heldEntity.GetComponent<GRCollectible>();
					if ((object)component2 == null)
					{
						return false;
					}
					switch (component2.type)
					{
					case ProgressionManager.CoreType.Core:
					case ProgressionManager.CoreType.SuperCore:
						Debug.Log($"Soak depositing core {_heldEntity.id.index}");
						_heldEntity.gameObject.transform.position = _coreDepositor.gameObject.transform.position;
						break;
					case ProgressionManager.CoreType.ChaosSeed:
						Debug.Log($"Soak depositing chaos seed {_heldEntity.id.index}");
						_heldEntity.gameObject.transform.position = _seedExtractorTriggerLocation.Value;
						break;
					}
					_heldEntity = null;
					_depositCollectibleTime = null;
					Complete = true;
				}
			}
		}
		return true;
	}

	public void Reset()
	{
		_heldEntity = null;
		_depositCollectibleTime = null;
		Complete = false;
	}
}
