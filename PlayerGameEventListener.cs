using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerGameEventListener : MonoBehaviour
{
	[SerializeField]
	private PlayerGameEvents.EventType eventType;

	[Tooltip("Cooldown in seconds")]
	[SerializeField]
	private string filter;

	[SerializeField]
	private float cooldown = 1f;

	[SerializeField]
	private UnityEvent onGameEvent;

	[SerializeField]
	private UnityEvent<int> onGameEventCounted;

	private float _cooldownEnd;

	private void OnEnable()
	{
		SubscribeToEvents();
	}

	private void OnDisable()
	{
		UnsubscribeFromEvents();
	}

	private void SubscribeToEvents()
	{
		switch (eventType)
		{
		case PlayerGameEvents.EventType.GameModeObjective:
			PlayerGameEvents.OnGameModeObjectiveTrigger += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.GameModeCompleteRound:
			PlayerGameEvents.OnGameModeCompleteRound += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.GrabbedObject:
			PlayerGameEvents.OnGrabbedObject += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.DroppedObject:
			PlayerGameEvents.OnDroppedObject += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.EatObject:
			PlayerGameEvents.OnEatObject += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.TapObject:
			PlayerGameEvents.OnTapObject += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.LaunchedProjectile:
			PlayerGameEvents.OnLaunchedProjectile += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.PlayerMoved:
			PlayerGameEvents.OnPlayerMoved += OnGameMoveEventTriggered;
			break;
		case PlayerGameEvents.EventType.PlayerSwam:
			PlayerGameEvents.OnPlayerSwam += OnGameMoveEventTriggered;
			break;
		case PlayerGameEvents.EventType.TriggerHandEfffect:
			PlayerGameEvents.OnTriggerHandEffect += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.EnterLocation:
			PlayerGameEvents.OnEnterLocation += OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.MiscEvent:
			PlayerGameEvents.OnMiscEvent += OnGameEventTriggered;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case PlayerGameEvents.EventType.NONE:
			break;
		}
	}

	private void UnsubscribeFromEvents()
	{
		switch (eventType)
		{
		case PlayerGameEvents.EventType.GameModeObjective:
			PlayerGameEvents.OnGameModeObjectiveTrigger -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.GameModeCompleteRound:
			PlayerGameEvents.OnGameModeCompleteRound -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.GrabbedObject:
			PlayerGameEvents.OnGrabbedObject -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.DroppedObject:
			PlayerGameEvents.OnDroppedObject -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.EatObject:
			PlayerGameEvents.OnEatObject -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.TapObject:
			PlayerGameEvents.OnTapObject -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.LaunchedProjectile:
			PlayerGameEvents.OnLaunchedProjectile -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.PlayerMoved:
			PlayerGameEvents.OnPlayerMoved -= OnGameMoveEventTriggered;
			break;
		case PlayerGameEvents.EventType.PlayerSwam:
			PlayerGameEvents.OnPlayerSwam -= OnGameMoveEventTriggered;
			break;
		case PlayerGameEvents.EventType.TriggerHandEfffect:
			PlayerGameEvents.OnTriggerHandEffect -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.EnterLocation:
			PlayerGameEvents.OnEnterLocation -= OnGameEventTriggered;
			break;
		case PlayerGameEvents.EventType.MiscEvent:
			PlayerGameEvents.OnMiscEvent -= OnGameEventTriggered;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case PlayerGameEvents.EventType.NONE:
			break;
		}
	}

	private void OnGameMoveEventTriggered(float distance, float speed)
	{
		Debug.LogError("Movement events not supported - please implement");
	}

	public void OnGameEventTriggered(string eventName)
	{
		OnGameEventTriggered(eventName, 1);
	}

	public void OnGameEventTriggered(string eventName, int count)
	{
		if ((string.IsNullOrEmpty(filter) || eventName.StartsWith(filter)) && !(_cooldownEnd > Time.time))
		{
			_cooldownEnd = Time.time + cooldown;
			onGameEvent?.Invoke();
			onGameEventCounted?.Invoke(count);
		}
	}
}
