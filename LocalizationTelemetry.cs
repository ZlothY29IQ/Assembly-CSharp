using UnityEngine;

public static class LocalizationTelemetry
{
	public const string LANGUAGE_CHANGED_EVENT_NAME = "language_changed";

	private const string GAME_VERSION_CUSTOM_TAG_PREFIX = "game_version_";

	public const string STARTING_LANGUAGE_BODY_DATA = "starting_language";

	public const string NEW_LANGUAGE_BODY_DATA = "new_language";

	public static string GameVersionCustomTag => "game_version_" + Application.version;
}
