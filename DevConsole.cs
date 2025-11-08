using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class DevConsole : MonoBehaviour, IDebugObject
{
	[Serializable]
	public class LogEntry
	{
		private static int TotalIndex;

		[SerializeReference]
		[SerializeField]
		public readonly string _Message;

		[SerializeField]
		[SerializeReference]
		public readonly LogType Type;

		public readonly string Trace;

		public bool forwarded;

		public int repeatCount = 1;

		public bool filtered;

		public int index;

		public string Message
		{
			get
			{
				if (repeatCount > 1)
				{
					return $"({repeatCount}) {_Message}";
				}
				return _Message;
			}
		}

		public LogEntry(string message, LogType type, string trace)
		{
			_Message = message;
			Type = type;
			Trace = trace;
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = trace.Split("\n".ToCharArray(), StringSplitOptions.None);
			foreach (string line in array)
			{
				if (!tracebackScrubbing.Any((string scrubString) => line.Contains(scrubString)))
				{
					stringBuilder.AppendLine(line);
				}
			}
			Trace = stringBuilder.ToString();
			TotalIndex++;
			index = TotalIndex;
		}
	}

	[Serializable]
	public class DisplayedLogLine
	{
		public GorillaDevButton[] buttons;

		public Text lineText;

		public RectTransform transform;

		public int targetMessage;

		public GorillaDevButton maximizeButton;

		public GorillaDevButton forwardButton;

		public SpriteRenderer backdrop;

		private bool expanded;

		public DevInspector inspector;

		public Type data { get; set; }

		public DisplayedLogLine(GameObject obj)
		{
			lineText = obj.GetComponentInChildren<Text>();
			buttons = obj.GetComponentsInChildren<GorillaDevButton>();
			transform = obj.GetComponent<RectTransform>();
			backdrop = obj.GetComponentInChildren<SpriteRenderer>();
			GorillaDevButton[] array = buttons;
			foreach (GorillaDevButton gorillaDevButton in array)
			{
				if (gorillaDevButton.Type == DevButtonType.LineExpand)
				{
					maximizeButton = gorillaDevButton;
				}
				if (gorillaDevButton.Type == DevButtonType.LineForward)
				{
					forwardButton = gorillaDevButton;
				}
			}
		}
	}

	[Serializable]
	public class MessagePayload
	{
		[Serializable]
		public class Block
		{
			public string type;

			public TextBlock text;

			public Block(string markdownText)
			{
				text = new TextBlock
				{
					text = markdownText,
					type = "mrkdwn"
				};
				type = "section";
			}
		}

		[Serializable]
		public class TextBlock
		{
			public string type;

			public string text;
		}

		public Block[] blocks;

		public static List<MessagePayload> GeneratePayloads(string username, List<LogEntry> entries)
		{
			List<MessagePayload> list = new List<MessagePayload>();
			List<Block> list2 = new List<Block>();
			entries.Sort((LogEntry e1, LogEntry e2) => e1.index.CompareTo(e2.index));
			string text = "";
			text += "```";
			list2.Add(new Block("User `" + username + "` Forwarded some errors"));
			foreach (LogEntry entry in entries)
			{
				string[] array = entry.Trace.Split("\n".ToCharArray());
				string text2 = "";
				string[] array2 = array;
				foreach (string text3 in array2)
				{
					text2 = text2 + "    " + text3 + "\n";
				}
				string text4 = $"({entry.Type}) {entry.Message}\n{text2}\n";
				if (text.Length + text4.Length > 3000)
				{
					text += "```";
					list2.Add(new Block(text));
					list.Add(new MessagePayload
					{
						blocks = list2.ToArray()
					});
					list2 = new List<Block>();
					text = "```";
				}
				text += $"({entry.Type}) {entry.Message}\n{text2}\n";
			}
			text += "```";
			list2.Add(new Block(text));
			list.Add(new MessagePayload
			{
				blocks = list2.ToArray()
			});
			return list;
		}
	}

	private static DevConsole _instance;

	[SerializeField]
	private AudioClip errorSound;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private float maxHeight;

	public static readonly string[] tracebackScrubbing = new string[3] { "ExitGames.Client.Photon", "Photon.Realtime.LoadBalancingClient", "Photon.Pun.PhotonHandler" };

	private const int kLogEntriesCapacityIncrementAmount = 1024;

	[SerializeReference]
	[SerializeField]
	private readonly List<LogEntry> _logEntries = new List<LogEntry>(1024);

	public int targetLogIndex = -1;

	public int currentLogIndex;

	public bool isMuted;

	public float currentZoomLevel = 1f;

	public List<GameObject> disableWhileActive;

	public List<GameObject> enableWhileActive;

	public int expandAmount = 20;

	public int expandedMessageIndex = -1;

	public bool canExpand = true;

	public List<DisplayedLogLine> logLines = new List<DisplayedLogLine>();

	public float lineStartHeight;

	public float textStartHeight;

	public float lineStartTextWidth;

	public double textScale = 0.5;

	public List<DevConsoleInstance> instances;

	public static DevConsole instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindAnyObjectByType<DevConsole>();
			}
			return _instance;
		}
	}

	public static List<LogEntry> logEntries => instance._logEntries;

	public void OnDestroyDebugObject()
	{
		Debug.Log("Destroying debug instances now");
		foreach (DevConsoleInstance instance in instances)
		{
			UnityEngine.Object.DestroyImmediate(instance.gameObject);
		}
	}

	private void OnEnable()
	{
		base.gameObject.SetActive(value: false);
	}
}
