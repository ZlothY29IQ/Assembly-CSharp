using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GorillaNetworking;
using UnityEngine;

public class PersistLog : MonoBehaviour
{
	private static StreamWriter sr;

	private string plog;

	private bool dup;

	private List<(double time, string msg, string strace)> earlyQ;

	private async void OnEnable()
	{
		earlyQ = new List<(double, string, string)>();
		Application.logMessageReceived += LogMessageEnqueue;
		while (GorillaComputer.instance == null)
		{
			await Task.Yield();
		}
		string text = Application.persistentDataPath + Path.DirectorySeparatorChar;
		string text2 = text + "gt.log";
		string text3 = text + "gt-old.log";
		string destFileName = text + "gt-older.log";
		try
		{
			if (File.Exists(text3))
			{
				File.Copy(text3, destFileName, overwrite: true);
			}
		}
		catch (IOException)
		{
		}
		try
		{
			if (File.Exists(text2))
			{
				File.Copy(text2, text3, overwrite: true);
			}
		}
		catch (IOException)
		{
		}
		string path = text2;
		for (int i = 1; i <= 10; i++)
		{
			try
			{
				sr = File.CreateText(path);
			}
			catch (IOException) when (i < 10)
			{
				path = text + "gt_" + (i + 1) + ".log";
				continue;
			}
			break;
		}
		if (sr == null)
		{
			Debug.LogError("[PersistLog] Failed to create log file after 10 attempts.");
			Application.logMessageReceived -= LogMessageEnqueue;
			return;
		}
		sr.Write($"{DateTime.Now:U}\r\n\r\n                           MONKE WUZ HERE!\r\n               _______    /\r\n              /       \\\r\n             /  _____  \\\r\n            / / _   _ \\ \\\r\n           [ | (O) (O) | ]\r\n            | \\  . .  / |\r\n     _______|  | _._ |  |_______\r\n    /        \\  \\___/  /        \\\r\n\r\nApp Id:        {Application.identifier}\r\nApp Ver:       {Application.version}\r\nPlatform:      {Application.platform}\r\nSys Lang:      {Application.systemLanguage}\r\nGC Version:    {GorillaComputer.instance.version}\r\nGC Build Code: {GorillaComputer.instance.buildCode}\r\nGC Build Date: {GorillaComputer.instance.buildDate}\r\n\r\n");
		Application.logMessageReceived -= LogMessageEnqueue;
		foreach (var (num, arg, arg2) in earlyQ)
		{
			sr.Write($"T+{num} >> {arg}\n==========================\n{arg2}\n\n");
		}
		sr.Flush();
		Application.logMessageReceived += LogMessageReceived;
	}

	private void OnDisable()
	{
		OnDestroy();
	}

	private void OnDestroy()
	{
		Application.logMessageReceived -= LogMessageEnqueue;
		Application.logMessageReceived -= LogMessageReceived;
		if (sr != null)
		{
			sr.Close();
			sr = null;
		}
	}

	private void LogMessageEnqueue(string msg, string strace, LogType type)
	{
		if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
		{
			earlyQ.Add((Time.realtimeSinceStartupAsDouble, msg, strace));
		}
	}

	private void LogMessageReceived(string msg, string strace, LogType type)
	{
		if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
		{
			if (plog == msg + strace)
			{
				if (!dup)
				{
					sr.Write($"T+{Time.realtimeSinceStartupAsDouble} >> Duplicate log entry... Supressing further\n\n");
					sr.Flush();
					dup = true;
				}
			}
			else
			{
				sr.Write($"T+{Time.realtimeSinceStartupAsDouble} >> {msg}\n==========================\n{strace}\n\n");
				sr.Flush();
				dup = false;
			}
		}
		plog = msg + strace;
	}

	public static void Log(string msg)
	{
		Log(LogType.Log, msg);
	}

	public static void Log(LogType type, string msg)
	{
		msg = $"T+{Time.realtimeSinceStartupAsDouble} >[DEV MSG]> {msg}\n\n";
		Debug.unityLogger.Log(type, msg);
		if (sr != null)
		{
			sr.Write(msg);
			sr.Flush();
		}
	}
}
