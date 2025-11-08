using System;

public class PlayerGameEvents
{
	public enum EventType
	{
		NONE,
		GameModeObjective,
		GameModeCompleteRound,
		GrabbedObject,
		DroppedObject,
		EatObject,
		TapObject,
		LaunchedProjectile,
		PlayerMoved,
		PlayerSwam,
		TriggerHandEfffect,
		EnterLocation,
		MiscEvent
	}

	public static event Action<string> OnGameModeObjectiveTrigger;

	public static event Action<string> OnGameModeCompleteRound;

	public static event Action<string> OnGrabbedObject;

	public static event Action<string> OnDroppedObject;

	public static event Action<string> OnEatObject;

	public static event Action<string> OnTapObject;

	public static event Action<string> OnLaunchedProjectile;

	public static event Action<float, float> OnPlayerMoved;

	public static event Action<float, float> OnPlayerSwam;

	public static event Action<string> OnTriggerHandEffect;

	public static event Action<string> OnEnterLocation;

	public static event Action<string, int> OnMiscEvent;

	public static event Action<string> OnCritterEvent;

	public static void GameModeObjectiveTriggered()
	{
		string obj = GorillaGameManager.instance.GameModeName();
		PlayerGameEvents.OnGameModeObjectiveTrigger?.Invoke(obj);
	}

	public static void GameModeCompleteRound()
	{
		string obj = GorillaGameManager.instance.GameModeName();
		PlayerGameEvents.OnGameModeCompleteRound?.Invoke(obj);
	}

	public static void GrabbedObject(string objectName)
	{
		PlayerGameEvents.OnGrabbedObject?.Invoke(objectName);
	}

	public static void DroppedObject(string objectName)
	{
		PlayerGameEvents.OnDroppedObject?.Invoke(objectName);
	}

	public static void EatObject(string objectName)
	{
		PlayerGameEvents.OnEatObject?.Invoke(objectName);
	}

	public static void TapObject(string objectName)
	{
		PlayerGameEvents.OnTapObject?.Invoke(objectName);
	}

	public static void LaunchedProjectile(string objectName)
	{
		PlayerGameEvents.OnLaunchedProjectile?.Invoke(objectName);
	}

	public static void PlayerMoved(float distance, float speed)
	{
		PlayerGameEvents.OnPlayerMoved?.Invoke(distance, speed);
	}

	public static void PlayerSwam(float distance, float speed)
	{
		PlayerGameEvents.OnPlayerSwam?.Invoke(distance, speed);
	}

	public static void TriggerHandEffect(string effectName)
	{
		PlayerGameEvents.OnTriggerHandEffect?.Invoke(effectName);
	}

	public static void TriggerEnterLocation(string locationName)
	{
		PlayerGameEvents.OnEnterLocation?.Invoke(locationName);
	}

	public static void MiscEvent(string eventName, int count = 1)
	{
		PlayerGameEvents.OnMiscEvent?.Invoke(eventName, count);
	}

	public static void CritterEvent(string eventName)
	{
		PlayerGameEvents.OnCritterEvent?.Invoke(eventName);
	}
}
