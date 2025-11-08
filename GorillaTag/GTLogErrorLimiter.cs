using System.Runtime.CompilerServices;
using System.Text;
using Cysharp.Text;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTag;

public class GTLogErrorLimiter
{
	private const string __NULL__ = "__NULL__";

	public int countdown;

	public int occurrenceCount;

	public string occurrencesJoinString;

	private string _baseMessage;

	public Utf16ValueStringBuilder sb;

	private const string k_lastMsgHeader = "!!!! THIS MESSAGE HAS REACHED MAX SPAM COUNT AND WILL NO LONGER BE LOGGED !!!!\n";

	public string baseMessage
	{
		get
		{
			return _baseMessage;
		}
		set
		{
			_baseMessage = value ?? "__NULL__";
		}
	}

	public GTLogErrorLimiter(string baseMessage, int countdown = 10, string occurrencesJoinString = "\n- ")
	{
		this.baseMessage = baseMessage;
		this.countdown = countdown;
		sb = ZString.CreateStringBuilder();
		sb.Append(this.baseMessage);
		this.occurrencesJoinString = occurrencesJoinString;
	}

	public void Log(string subMessage = "", Object context = null, [CallerMemberName] string caller = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int line = 0)
	{
		if (countdown >= 0)
		{
			if (countdown == 0)
			{
				sb.Insert(0, "!!!! THIS MESSAGE HAS REACHED MAX SPAM COUNT AND WILL NO LONGER BE LOGGED !!!!\n");
			}
			sb.Append(subMessage ?? "__NULL__");
			sb.Append("\n\nError origin - Caller: ");
			sb.Append(caller ?? "__NULL__");
			sb.Append(", Line: ");
			sb.Append(line);
			sb.Append("File: ");
			sb.Append(sourceFilePath ?? "__NULL__");
			Debug.LogError(sb.ToString(), context);
			sb.Clear();
			sb.Append(baseMessage);
			countdown--;
			occurrenceCount = 0;
		}
	}

	public void Log(Object obj, Object context = null, [CallerMemberName] string caller = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int line = 0)
	{
		if (!obj)
		{
			Log("__NULL__", context, caller, sourceFilePath, line);
		}
		else
		{
			Log(obj.ToString(), null, "Log", "C:\\Users\\root\\GT\\Assets\\GorillaTag\\Shared\\Scripts\\MonkeFX\\GTLogErrorLimiter.cs", 137);
		}
	}

	public void AddOccurrence(string s)
	{
		occurrenceCount++;
		sb.Append(occurrencesJoinString ?? "\n- ");
		sb.Append(s);
	}

	public void AddOccurrence(StringBuilder stringBuilder)
	{
		occurrenceCount++;
		sb.Append(occurrencesJoinString ?? "\n- ");
		sb.Append(stringBuilder);
	}

	public void AddOccurence(GameObject gObj)
	{
		occurrenceCount++;
		if (gObj == null)
		{
			AddOccurrence("__NULL__");
			return;
		}
		sb.Append(occurrencesJoinString ?? "\n- ");
		sb.Q(gObj.GetPath());
	}

	public void LogOccurrences(Component component = null, Object obj = null, [CallerMemberName] string caller = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int line = 0)
	{
		if (occurrenceCount > 0)
		{
			sb.Insert(0, $"Occurred {occurrenceCount} times: ");
			Log("\"" + component.GetComponentPath() + "\"", obj, caller, sourceFilePath, line);
		}
	}

	public void LogOccurrences(Utf16ValueStringBuilder subMessage, Object obj = null, [CallerMemberName] string caller = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int line = 0)
	{
		if (occurrenceCount > 0)
		{
			sb.Insert(0, $"Occurred {occurrenceCount} times: ");
			sb.Append(subMessage);
			Log("", obj, caller, sourceFilePath, line);
		}
	}
}
