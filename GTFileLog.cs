using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class GTFileLog
{
	public sealed class FLogInstance
	{
		private StreamWriter _writer;

		private bool _failed;

		private readonly object _lock = new object();

		private readonly string _prefix;

		private const string FilePrefix = "flog_";

		private const int MaxFlogFiles = 10;

		internal bool IsActive
		{
			get
			{
				lock (_lock)
				{
					return _writer != null;
				}
			}
		}

		internal FLogInstance(string prefix)
		{
			_prefix = prefix;
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void Log(string msg)
		{
			WriteEntry("LOG", msg, StackTraceUtility.ExtractStackTrace());
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void LogWarning(string msg)
		{
			WriteEntry("WARN", msg, StackTraceUtility.ExtractStackTrace());
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void LogError(string msg)
		{
			WriteEntry("ERR", msg, StackTraceUtility.ExtractStackTrace());
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void LogNoTrace(string msg)
		{
			WriteEntryNoTrace("LOG", msg);
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void LogWarningNoTrace(string msg)
		{
			WriteEntryNoTrace("WARN", msg);
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void LogErrorNoTrace(string msg)
		{
			WriteEntryNoTrace("ERR", msg);
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void CLog(string msg)
		{
			WriteEntryNoTrace("LOG", msg);
			UnityEngine.Debug.Log("[GT/FLog:" + _prefix + "] " + msg);
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void CLogWarning(string msg)
		{
			WriteEntryNoTrace("WARN", msg);
			UnityEngine.Debug.LogWarning("[GT/FLog:" + _prefix + "] " + msg);
		}

		[Conditional("BETA")]
		[Conditional("UNITY_EDITOR")]
		public void CLogError(string msg)
		{
			WriteEntryNoTrace("ERR", msg);
			UnityEngine.Debug.LogError("[GT/FLog:" + _prefix + "] " + msg);
		}

		internal void WriteEntryNoTrace(string level, string msg)
		{
			if (ApplicationQuittingState.IsQuitting)
			{
				return;
			}
			lock (_lock)
			{
				EnsureWriter(null);
				if (_writer == null)
				{
					return;
				}
				try
				{
					string timestamp = GetTimestamp();
					_writer.WriteLine("[" + timestamp + "] [" + level + "] " + msg);
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogError("[GT/GTFileLog:" + _prefix + "] Write failed: " + ex.Message);
					CloseWriter();
				}
			}
		}

		internal void WriteEntry(string level, string msg, string trace)
		{
			if (ApplicationQuittingState.IsQuitting)
			{
				return;
			}
			lock (_lock)
			{
				EnsureWriter(trace);
				if (_writer == null)
				{
					return;
				}
				try
				{
					string timestamp = GetTimestamp();
					_writer.WriteLine("[" + timestamp + "] [" + level + "] " + msg + "\n- - - -");
					_writer.WriteLine(trace);
					_writer.WriteLine("");
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogError("[GT/GTFileLog:" + _prefix + "] Write failed: " + ex.Message);
					CloseWriter();
				}
			}
		}

		private void EnsureWriter(string callerTrace)
		{
			if (_writer != null || _failed)
			{
				return;
			}
			if (ApplicationQuittingState.IsQuitting)
			{
				_failed = true;
				return;
			}
			try
			{
				string persistentDataPath = Application.persistentDataPath;
				Directory.CreateDirectory(persistentDataPath);
				string text = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
				string text2 = "flog_" + _prefix + "_" + text;
				string text3 = Path.Combine(persistentDataPath, text2 + ".log");
				for (int i = 1; i <= 10; i++)
				{
					try
					{
						_writer = new StreamWriter(text3, append: true)
						{
							AutoFlush = true
						};
					}
					catch (IOException) when (i < 10)
					{
						text3 = Path.Combine(persistentDataPath, text2 + "_" + (i + 1) + ".log");
						continue;
					}
					break;
				}
				if (_writer == null)
				{
					throw new IOException("All 10 log file attempts failed due to sharing violations.");
				}
				_writer.WriteLine($"--- {_prefix} log started {DateTime.UtcNow:u} ---");
				_writer.WriteLine("--- playerName: " + PlayerPrefs.GetString("playerName", "(unset)") + " ---");
				string text4 = ((callerTrace != null) ? ExtractFirstExternalCaller(callerTrace) : "(no-trace)");
				UnityEngine.Debug.Log("<color=orange><b>[GT/GTFileLog:" + _prefix + "]</b> Writing to \"" + text3 + "\". First caller: " + text4 + "</color>");
				PruneOldFlogFiles(persistentDataPath);
			}
			catch (Exception ex2)
			{
				_failed = true;
				UnityEngine.Debug.LogError("[GT/GTFileLog:" + _prefix + "] Failed to create log file: " + ex2.Message);
			}
		}

		private static void PruneOldFlogFiles(string dir)
		{
			try
			{
				string[] files = Directory.GetFiles(dir, "flog_*.log");
				if (files.Length <= 10)
				{
					return;
				}
				Array.Sort(files, (string a, string b) => File.GetLastWriteTimeUtc(a).CompareTo(File.GetLastWriteTimeUtc(b)));
				int num = files.Length - 10;
				for (int num2 = 0; num2 < num; num2++)
				{
					try
					{
						File.Delete(files[num2]);
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}

		private void CloseWriter()
		{
			try
			{
				_writer?.Flush();
				_writer?.Dispose();
			}
			catch
			{
			}
			_writer = null;
		}

		internal void Close()
		{
			lock (_lock)
			{
				CloseWriter();
				_failed = false;
			}
		}
	}

	private static readonly object _registryLock = new object();

	private static Dictionary<string, FLogInstance> _instances = new Dictionary<string, FLogInstance>();

	private static FLogInstance _default;

	[ThreadStatic]
	private static bool _inCallback;

	private static FLogInstance Default
	{
		get
		{
			if (_default != null)
			{
				return _default;
			}
			lock (_registryLock)
			{
				if (_default == null)
				{
					_default = new FLogInstance("main");
				}
				return _default;
			}
		}
	}

	public static FLogInstance GetLog(string name)
	{
		lock (_registryLock)
		{
			if (_instances.TryGetValue(name, out var value))
			{
				return value;
			}
			FLogInstance fLogInstance = new FLogInstance(name);
			_instances[name] = fLogInstance;
			return fLogInstance;
		}
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void Log(string msg)
	{
		Default.WriteEntry("LOG", msg, StackTraceUtility.ExtractStackTrace());
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogWarning(string msg)
	{
		Default.WriteEntry("WARN", msg, StackTraceUtility.ExtractStackTrace());
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogError(string msg)
	{
		Default.WriteEntry("ERR", msg, StackTraceUtility.ExtractStackTrace());
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogNoTrace(string msg)
	{
		Default.WriteEntryNoTrace("LOG", msg);
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogWarningNoTrace(string msg)
	{
		Default.WriteEntryNoTrace("WARN", msg);
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogErrorNoTrace(string msg)
	{
		Default.WriteEntryNoTrace("ERR", msg);
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void CLog(string msg)
	{
		lock (_registryLock)
		{
			if (_default != null && _default.IsActive)
			{
				_default.WriteEntryNoTrace("LOG", msg);
			}
			foreach (FLogInstance value in _instances.Values)
			{
				if (value.IsActive)
				{
					value.WriteEntryNoTrace("LOG", msg);
				}
			}
		}
		UnityEngine.Debug.Log("[GT/FLog] " + msg);
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void CLogWarning(string msg)
	{
		lock (_registryLock)
		{
			if (_default != null && _default.IsActive)
			{
				_default.WriteEntryNoTrace("WARN", msg);
			}
			foreach (FLogInstance value in _instances.Values)
			{
				if (value.IsActive)
				{
					value.WriteEntryNoTrace("WARN", msg);
				}
			}
		}
		UnityEngine.Debug.LogWarning("[GT/FLog] " + msg);
	}

	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void CLogError(string msg)
	{
		lock (_registryLock)
		{
			if (_default != null && _default.IsActive)
			{
				_default.WriteEntryNoTrace("ERR", msg);
			}
			foreach (FLogInstance value in _instances.Values)
			{
				if (value.IsActive)
				{
					value.WriteEntryNoTrace("ERR", msg);
				}
			}
		}
		UnityEngine.Debug.LogError("[GT/FLog] " + msg);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Reset()
	{
		lock (_registryLock)
		{
			if (_default != null)
			{
				_default.Close();
			}
			foreach (FLogInstance value in _instances.Values)
			{
				value.Close();
			}
		}
	}

	private static void OnUnityLogMessage(string condition, string stackTrace, LogType type)
	{
		if ((type != LogType.Error && type != LogType.Exception && type != LogType.Assert) || _inCallback)
		{
			return;
		}
		_inCallback = true;
		try
		{
			Default.WriteEntry(type switch
			{
				LogType.Assert => "ASSERT", 
				LogType.Exception => "EXCEPTION", 
				_ => "UNITY_ERR", 
			}, condition, stackTrace);
		}
		finally
		{
			_inCallback = false;
		}
	}

	internal static string GetTimestamp()
	{
		if (!(NetworkSystem.Instance != null))
		{
			return Mathf.FloorToInt(Time.realtimeSinceStartup * 1000f) + "u";
		}
		return NetworkSystem.Instance.ServerTimestamp.ToString();
	}

	internal static string ExtractFirstExternalCaller(string stackTrace)
	{
		if (string.IsNullOrEmpty(stackTrace))
		{
			return "(unknown)";
		}
		int num = 0;
		while (num < stackTrace.Length)
		{
			int num2 = stackTrace.IndexOf('\n', num);
			if (num2 < 0)
			{
				num2 = stackTrace.Length;
			}
			int num3 = num2 - num;
			if (num3 > 0 && stackTrace.IndexOf("GTFileLog", num, Math.Min(num3, 60), StringComparison.Ordinal) < 0)
			{
				return stackTrace.Substring(num, num3).Trim();
			}
			num = num2 + 1;
		}
		return "(unknown)";
	}
}
