using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SynthesisvrArcade;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using WebSocketSharp;

public class SynthesisArcadeObject : MonoBehaviour
{
	private enum DRM_VERIFICATION_MODE
	{
		DEFAULT,
		WEB
	}

	internal string TAG = "SynthesisVRArcadeObject";

	internal static SynthesisArcadeObject _instance;

	private Coroutine co;

	public string synthesisGameId;

	public bool synthesisCdnBuild = true;

	public bool disableSynthesisDRMInUnityEditor = true;

	private bool quitGame;

	private bool manualPpmTracking;

	private string webSocketClientAddress = "";

	private double webSocketLastConnectionAttemptEpoch;

	private Action<string> synchronizationAction;

	private List<string> synthesisFlags = new List<string>();

	private int successfulDrmChecks;

	public bool enableLiveInteractions;

	[Range(1000f, 65535f)]
	public int udpPort = 23232;

	internal static volatile Queue<SynthesisUdpCommand> udpQueue = new Queue<SynthesisUdpCommand>();

	public List<SynthesisUdpCommand> commandDefinitions;

	public UnityEvent<string, string> fallbackCommandProcessor = new UnityEvent<string, string>();

	internal static WebSocket wsclient;

	private ArcadeInterface svrInterfaceWin;

	private UdpClient synthesisUdpListener;

	private IPEndPoint localUdpEndpoint;

	internal bool allowEnteringTheLoop;

	public static SynthesisArcadeObject Instance => _instance;

	public string ReadCommandLineArgument(string name)
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i] == name && commandLineArgs.Length > i + 1)
			{
				return commandLineArgs[i + 1];
			}
		}
		return null;
	}

	internal void processLiveCommand(string message)
	{
		string[] array = message.Split(new string[1] { " " }, 2, StringSplitOptions.None);
		processLiveCommand(array[0], array[1]);
	}

	internal void processLiveCommand(string interactionname, string args)
	{
		List<SynthesisUdpCommand> list = commandDefinitions.FindAll((SynthesisUdpCommand el) => el.commandName == interactionname);
		if (list.Count > 0)
		{
			list.ForEach(delegate(SynthesisUdpCommand cmd)
			{
				cmd.extraArgs = ((args != null) ? args : "");
				udpQueue.Enqueue(cmd);
			});
		}
		else
		{
			fallbackCommandProcessor.Invoke(interactionname, args);
		}
	}

	internal void processUnknownLiveCommand(string command, string args)
	{
		Debug.LogWarning("[" + TAG + "] Unknown Live Interactions Command: " + command);
	}

	internal void SynthesisUdpReceived(IAsyncResult ar)
	{
		IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
		byte[] array;
		try
		{
			array = synthesisUdpListener.EndReceive(ar, ref remoteEP);
			if (array.Length == 0)
			{
				return;
			}
			synthesisUdpListener.BeginReceive(SynthesisUdpReceived, null);
		}
		catch (ObjectDisposedException ex)
		{
			try
			{
				Debug.Log("[" + TAG + "] Port listening failed. Retry / " + ex.Message);
				synthesisUdpListener.BeginReceive(SynthesisUdpReceived, null);
				return;
			}
			catch
			{
				return;
			}
		}
		string message = Encoding.UTF8.GetString(array);
		_instance.processLiveCommand(message);
	}

	internal void StartSynthesisUdpListener()
	{
		synthesisUdpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
		synthesisUdpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, optionValue: true);
		synthesisUdpListener.ExclusiveAddressUse = false;
		synthesisUdpListener.Client.EnableBroadcast = true;
		synthesisUdpListener.EnableBroadcast = true;
		synthesisUdpListener.Client.Bind(localUdpEndpoint);
		synthesisUdpListener.BeginReceive(SynthesisUdpReceived, null);
		Debug.Log("[" + TAG + "] Step1/Listening on UDP port " + udpPort);
	}

	public void UdpHelloWorld(string args)
	{
		Debug.Log("[" + TAG + "] Hello World -> " + args);
	}

	private void OnApplicationPause(bool pauseStatus)
	{
	}

	private void Update()
	{
		if (quitGame)
		{
			Debug.Log("[" + TAG + "] SynthesisVR stopping the game");
			Application.Quit();
		}
		else if (webSocketClientAddress != null && webSocketClientAddress.Length > 5 && (wsclient == null || wsclient.ReadyState == WebSocketState.Closed || wsclient.ReadyState == WebSocketState.Closing))
		{
			ConnectToWebSocket();
		}
		while (udpQueue.Count > 0)
		{
			SynthesisUdpCommand synthesisUdpCommand = udpQueue.Dequeue();
			if (synthesisUdpCommand.EventReceiver.GetPersistentEventCount() == 0)
			{
				synthesisUdpCommand.EventReceiver?.Invoke(synthesisUdpCommand.extraArgs);
				continue;
			}
			for (int i = 0; i < synthesisUdpCommand.EventReceiver.GetPersistentEventCount(); i++)
			{
				try
				{
					if (synthesisUdpCommand.extraArgs.Trim().Length == 0)
					{
						synthesisUdpCommand.EventReceiver.Invoke("");
					}
					else if (synthesisUdpCommand.EventReceiver.GetPersistentEventCount() > 0)
					{
						string persistentMethodName = synthesisUdpCommand.EventReceiver.GetPersistentMethodName(i);
						if (persistentMethodName != null)
						{
							((MonoBehaviour)synthesisUdpCommand.EventReceiver.GetPersistentTarget(i)).SendMessage(persistentMethodName, synthesisUdpCommand.extraArgs);
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Log("[" + TAG + "] ERROR => " + ex.Message);
				}
			}
		}
	}

	private void OnDestroy()
	{
		try
		{
			if (_instance == this && _instance.TryGetComponent<SynthesisColumns>(out var component))
			{
				SceneManager.sceneLoaded -= component.HandleSceneLoaded;
			}
			if (wsclient != null && wsclient.ReadyState == WebSocketState.Open)
			{
				wsclient.Close();
			}
		}
		catch
		{
		}
		if (!enableLiveInteractions)
		{
			return;
		}
		try
		{
			if (synthesisUdpListener.Client != null)
			{
				synthesisUdpListener.Close();
			}
		}
		catch
		{
		}
	}

	private void Start()
	{
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
		}
		bool flag = false;
		string text = Application.dataPath + "\\" + (Application.isEditor ? "Plugins" : "Managed") + "\\arcade_success.dll";
		Type type = Type.GetType("SynthesisvrArcade.ArcadeFactory");
		if (Application.isEditor)
		{
			Debug.Log("[" + TAG + "] DLL FILE HASH - " + filehash(Application.dataPath + "/SynthesisVR/SDK/arcade_success.dll"));
		}
		else if (!File.Exists(text) && flag && type != null)
		{
			Debug.Log("[" + TAG + "] This is an IL2CPP build");
		}
		else if (!filehash(text).Equals("65958aba4e88155682dab8ea50244793"))
		{
			Debug.LogError("[" + TAG + "] BAD DLL FILE");
			quitGame = true;
		}
		if (quitGame)
		{
			return;
		}
		UnityEngine.Object.DontDestroyOnLoad(_instance);
		if (_instance.TryGetComponent<SynthesisColumns>(out var svrColumnsManager))
		{
			svrColumnsManager.AutoAssignFromResources();
			string text2 = ReadCommandLineArgument("-svrcolumns") ?? "{}";
			if (text2.StartsWith("file_"))
			{
				text2 = svrColumnsManager.readColumnsFromJsonFile(Instance);
			}
			try
			{
				svrColumnsManager.LoadLayoutFromJson(text2);
				if (!enableLiveInteractions)
				{
					enableLiveInteractions = true;
				}
				bool flag2 = false;
				foreach (SynthesisUdpCommand commandDefinition in commandDefinitions)
				{
					if (commandDefinition.commandName == "svrreloadcolumns")
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					SynthesisUdpCommand item = new SynthesisUdpCommand
					{
						commandName = "svrreloadcolumns",
						EventReceiver = new UnityEvent<string>(),
						extraArgs = ""
					};
					item.EventReceiver.AddListener(delegate(string json)
					{
						if (string.IsNullOrWhiteSpace(json))
						{
							json = "1";
						}
						try
						{
							if (json == "1")
							{
								json = svrColumnsManager.readColumnsFromJsonFile(Instance);
							}
							svrColumnsManager.LoadLayoutFromJson(json);
							Debug.Log("[Columns] Layout reloaded via svrreloadcolumns");
						}
						catch (Exception ex2)
						{
							Debug.LogError("[Columns] Failed to load layout: " + ex2.Message);
						}
					});
					if (commandDefinitions == null)
					{
						commandDefinitions = new List<SynthesisUdpCommand>();
					}
					commandDefinitions.Add(item);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("[" + TAG + "] [COLUMNS_JSON_ERROR/3] => " + ex.Message);
			}
			svrColumnsManager._mpb = new MaterialPropertyBlock();
			SceneManager.sceneLoaded += svrColumnsManager.HandleSceneLoaded;
		}
		co = StartCoroutine(EnableLicensingCheckTask());
		webSocketClientAddress = ReadCommandLineArgument("wsaddr");
		synthesisFlags = new List<string>((ReadCommandLineArgument("-svrflags") ?? "").ToLowerInvariant().Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries));
		Debug.Log("[" + TAG + "] webSocketClientAddress = " + webSocketClientAddress);
	}

	private void ConnectToWebSocket()
	{
		TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
		if (webSocketClientAddress != null && webSocketClientAddress.Length > 5 && (wsclient == null || wsclient.ReadyState == WebSocketState.Closed || wsclient.ReadyState == WebSocketState.Closing) && timeSpan.TotalMilliseconds - webSocketLastConnectionAttemptEpoch >= 1000.0)
		{
			string text = webSocketClientAddress.Replace("#^epoch^#", timeSpan.TotalMilliseconds.ToString());
			Debug.Log("[" + TAG + "] webSocketClientAddress_Connect = " + text);
			wsclient = new WebSocket(text);
			wsclient.OnMessage += Wsclient_OnMessage;
			wsclient.OnError += Wsclient_OnError;
			wsclient.OnClose += Wsclient_OnClose;
			wsclient.OnOpen += Wsclient_OnOpen;
			webSocketLastConnectionAttemptEpoch = timeSpan.TotalMilliseconds;
			wsclient.ConnectAsync();
		}
	}

	internal void WebsocketBroadcast(string sendmsg)
	{
		wsclient.SendAsync(sendmsg, onWsSend);
	}

	internal bool StartMultiplayerSynchronization(Action<string> callbackAction, string syncType)
	{
		try
		{
			if (wsclient != null && wsclient.ReadyState == WebSocketState.Open)
			{
				WsProxyMessage obj = new WsProxyMessage
				{
					action = WS_PROXY_ACTIONS.AWAIT_SYNCHRONIZATION,
					arguments = syncType
				};
				if (callbackAction != null)
				{
					synchronizationAction = callbackAction;
				}
				wsclient.SendAsync(JsonUtility.ToJson(obj), onWsSend);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[" + TAG + "] [StartMultiplayerSynchronizationException] => " + ex.Message);
		}
		return false;
	}

	internal bool CancelMultiplayerSynchronization(string syncType)
	{
		try
		{
			if (wsclient != null && wsclient.ReadyState == WebSocketState.Open)
			{
				WsProxyMessage obj = new WsProxyMessage
				{
					action = WS_PROXY_ACTIONS.CANCEL_AWAIT_SYNCHRONIZATION,
					arguments = syncType
				};
				synchronizationAction = null;
				wsclient.SendAsync(JsonUtility.ToJson(obj), onWsSend);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[" + TAG + "] [StartMultiplayerSynchronizationException] => " + ex.Message);
		}
		return false;
	}

	private void Wsclient_OnMessage(object sender, MessageEventArgs e)
	{
		string text = Encoding.UTF8.GetString(e.RawData).Trim();
		if (!text.StartsWith("{") || !text.Trim().EndsWith("}"))
		{
			return;
		}
		try
		{
			WsProxyMessage wsProxyMessage = JsonUtility.FromJson<WsProxyMessage>(text);
			if (wsProxyMessage.action == WS_PROXY_ACTIONS.SYNCHRONIZED)
			{
				if (synchronizationAction != null)
				{
					synchronizationAction(wsProxyMessage.arguments);
				}
			}
			else
			{
				processLiveCommand(wsProxyMessage.interactionName, wsProxyMessage.arguments);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[" + TAG + "] [WEBSOCKET_EXCEPTION->] ERROR Parsing JSON [" + text + "] => " + ex.Message);
		}
	}

	private void Wsclient_OnOpen(object sender, EventArgs e)
	{
	}

	private static void onWsSend(bool status)
	{
	}

	private void Wsclient_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
	{
		Debug.LogError("[" + TAG + "] [WEBSOCKET_ONERROR] " + e.Message);
	}

	private void Wsclient_OnClose(object sender, EventArgs e)
	{
		Debug.LogError("[" + TAG + "] [WEBSOCKET_ONCLOSE]");
	}

	private IEnumerator EnableLicensingCheckTask()
	{
		svrInterfaceWin = ArcadeFactory.Synthesis();
		if (enableLiveInteractions)
		{
			synthesisUdpListener = new UdpClient();
			localUdpEndpoint = new IPEndPoint(IPAddress.Loopback, udpPort);
			fallbackCommandProcessor.AddListener(processUnknownLiveCommand);
			StartSynthesisUdpListener();
		}
		bool flag = !Application.isEditor || !disableSynthesisDRMInUnityEditor;
		if (flag && !synthesisCdnBuild && svrInterfaceWin != null && !svrInterfaceWin.IsSynthesisVRStation())
		{
			flag = false;
		}
		if (!flag)
		{
			yield break;
		}
		if (svrInterfaceWin != null)
		{
			allowEnteringTheLoop = true;
			svrInterfaceWin.Init(synthesisGameId, 1);
		}
		while (allowEnteringTheLoop)
		{
			yield return new WaitForSecondsRealtime(1f);
			Task<int> checkTask = null;
			if (svrInterfaceWin == null || (synthesisFlags.Contains("relaxed") && successfulDrmChecks > 30) || (synthesisFlags.Contains("chill") && successfulDrmChecks > 5))
			{
				continue;
			}
			checkTask = Task.Run(() => svrInterfaceWin.CheckInMode());
			yield return new WaitUntil(() => checkTask.IsCompleted);
			if (checkTask.IsFaulted)
			{
				Debug.LogError($"[{TAG}] Task threw: {checkTask.Exception.Flatten().InnerException}");
				continue;
			}
			int result = checkTask.Result;
			if (result == 0)
			{
				successfulDrmChecks++;
			}
			else
			{
				successfulDrmChecks = 0;
			}
			if (result > 1)
			{
				Debug.Log("[" + TAG + "] Check=" + result + (synthesisFlags.Contains("debug") ? (" || Debug=" + svrInterfaceWin.DebugString()) : ""));
			}
			if (!Application.isEditor && result >= 30)
			{
				allowEnteringTheLoop = false;
			}
		}
		quitGame = true;
	}

	protected string filehash(string fileName)
	{
		if (!File.Exists(fileName))
		{
			return "";
		}
		try
		{
			using MD5 mD = MD5.Create();
			using FileStream inputStream = File.OpenRead(fileName);
			return BitConverter.ToString(mD.ComputeHash(inputStream)).Replace("-", string.Empty).ToLowerInvariant();
		}
		catch
		{
			Application.Quit();
		}
		return "";
	}

	public bool AddToLeaderboard(string data)
	{
		Debug.Log("[" + TAG + "] Leaderboard Data -> " + data);
		bool result = false;
		if (svrInterfaceWin != null)
		{
			result = svrInterfaceWin.AddToLeaderboard(data);
		}
		return result;
	}

	public int sessionSecondsLeft()
	{
		int result = 1;
		if (svrInterfaceWin != null)
		{
			result = svrInterfaceWin.getSessionSecondsLeft();
		}
		return result;
	}

	public bool resetBillingSession()
	{
		Debug.Log("[" + TAG + "] Reset PPM Session");
		bool result = false;
		if (svrInterfaceWin != null)
		{
			result = svrInterfaceWin.ResetBillingSession(synthesisGameId);
		}
		return result;
	}

	public void setEngineData(string _key, string _value)
	{
		Debug.Log("[" + TAG + "] Set[" + _key + "][" + _value + "]");
		svrInterfaceWin.setKeyValue(_key, _value);
	}

	public void excludeFromDefaultBilling()
	{
		Debug.Log("[" + TAG + "] Stop Default Billing Logic");
		if (svrInterfaceWin != null)
		{
			manualPpmTracking = true;
			svrInterfaceWin.excludeFromBilling(excludeFromBilling: true);
		}
	}

	public bool startManualPpmTracking()
	{
		Debug.Log("[" + TAG + "] Start PPM Tracking");
		excludeFromDefaultBilling();
		if (manualPpmTracking)
		{
			manualPpmTracking = svrInterfaceWin.timeTracking("start");
			if (!manualPpmTracking)
			{
				svrInterfaceWin.excludeFromBilling(excludeFromBilling: false);
			}
		}
		return manualPpmTracking;
	}

	public bool stopManualPpmTracking()
	{
		Debug.Log("[" + TAG + "] Stop PPM Tracking");
		if (manualPpmTracking)
		{
			if (svrInterfaceWin != null)
			{
				manualPpmTracking = svrInterfaceWin.timeTracking("end");
			}
			if (manualPpmTracking)
			{
				manualPpmTracking = false;
			}
		}
		return manualPpmTracking;
	}

	public string AndroidConfigFileRead(string filename)
	{
		return "";
	}

	public bool AndroidConfigFileWrite(string filename, string content)
	{
		return false;
	}
}
