using System;
using System.Reflection;

namespace Viveport.Core;

public class Logger
{
	private const string LoggerTypeNameUnity = "UnityEngine.Debug";

	private static bool _hasDetected;

	private static bool _usingUnityLog = true;

	private static Type _unityLogType;

	public static void Log(string message)
	{
		if (!_hasDetected || _usingUnityLog)
		{
			UnityLog(message);
		}
		else
		{
			ConsoleLog(message);
		}
	}

	private static void ConsoleLog(string message)
	{
		Console.WriteLine(message);
		_hasDetected = true;
	}

	private static void UnityLog(string message)
	{
		try
		{
			if (_unityLogType == null)
			{
				_unityLogType = GetType("UnityEngine.Debug");
			}
			_unityLogType.GetMethod("Log", new Type[1] { typeof(string) }).Invoke(null, new object[1] { message });
			_usingUnityLog = true;
		}
		catch (Exception)
		{
			ConsoleLog(message);
			_usingUnityLog = false;
		}
		_hasDetected = true;
	}

	private static Type GetType(string typeName)
	{
		Type type = Type.GetType(typeName);
		if (type != null)
		{
			return type;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			type = assemblies[i].GetType(typeName);
			if (type != null)
			{
				return type;
			}
		}
		return null;
	}
}
