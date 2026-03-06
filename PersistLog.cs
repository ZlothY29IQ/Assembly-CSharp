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

	private List<Tuple<string, string>> earlyQ;

	private async void OnEnable()
	{
		earlyQ = new List<Tuple<string, string>>();
		Application.logMessageReceived += LogMessageEnqueue;
		while (GorillaComputer.instance == null)
		{
			await Task.Yield();
		}
		string text = Application.persistentDataPath + Path.DirectorySeparatorChar + "gt.log";
		string text2 = Application.persistentDataPath + Path.DirectorySeparatorChar + "gt-old.log";
		string destFileName = Application.persistentDataPath + Path.DirectorySeparatorChar + "gt-older.log";
		if (File.Exists(text2))
		{
			File.Copy(text2, destFileName, overwrite: true);
		}
		if (File.Exists(text))
		{
			File.Copy(text, text2, overwrite: true);
		}
		sr = File.CreateText(text);
		sr.Write($"{DateTime.Now:U}\r\n\r\n                           MONKE WUZ HERE!\r\n               _______    /\r\n              /       \\\r\n             /  _____  \\\r\n            / / _   _ \\ \\\r\n           [ | (O) (O) | ]\r\n            | \\  . .  / |\r\n     _______|  | _._ |  |_______\r\n    /        \\  \\___/  /        \\\r\n\r\nApp Id:        {Application.identifier}\r\nApp Ver:       {Application.version}\r\nPlatform:      {Application.platform}\r\nSys Lang:      {Application.systemLanguage}\r\nGC Version:    {GorillaComputer.instance.version}\r\nGC Build Code: {GorillaComputer.instance.buildCode}\r\nGC Build Date: {GorillaComputer.instance.buildDate}\r\n\r\n");
		Application.logMessageReceived -= LogMessageEnqueue;
		for (int i = 0; i < earlyQ.Count; i++)
		{
			sr.Write($"T+{Time.time} >> {earlyQ[i].Item1}\n==========================\n{earlyQ[i].Item2}\n\n");
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
			earlyQ.Add(Tuple.Create(msg, strace));
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
					sr.Write($"T+{Time.time} >> Duplicate log entry... Supressing further\n\n");
					dup = true;
				}
			}
			else
			{
				sr.Write($"T+{Time.time} >> {msg}\n==========================\n{strace}\n\n");
				dup = false;
			}
			sr.Flush();
		}
		plog = msg + strace;
	}

	public static void Log(string msg)
	{
		msg = $"T+{Time.time} >[DEV MSG]> {msg}\n\n";
		Debug.Log(msg);
		if (sr != null)
		{
			sr.Write(msg);
			sr.Flush();
		}
	}
}
