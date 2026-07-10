using System;
using UnityEngine;

[Serializable]
public struct NetworkSystemConfig
{
	[HideInInspector]
	public int MaxPlayerCount;

	private static string gameVersionType = "live1";

	public static string prependCode = "gorillaconprepend47838011";

	public static int majorVersion = 1;

	public static int minorVersion = 1;

	public static int minorVersion2 = 141;

	public static string AppVersion => prependCode + "." + AppVersionStripped;

	public static string AppVersionStripped => gameVersionType + "." + majorVersion + "." + minorVersion + "." + minorVersion2;

	public static string BundleVersion => majorVersion + "." + minorVersion + "." + minorVersion2;

	public static string GameVersionType => gameVersionType;

	public static int GameMajorVersion => majorVersion;

	public static int GameMinorVersion => minorVersion;

	public static int GameMinorVersion2 => minorVersion2;
}
