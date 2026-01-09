using System.Collections.Generic;
using System.Text;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

public class DebugHudStats : MonoBehaviour
{
	private enum State
	{
		Inactive,
		Active,
		ShowLog,
		ShowError,
		ShowStats,
		ShowRBs,
		timeAdjust
	}

	public static int FPS_THRESHOLD = 89;

	private static DebugHudStats _instance;

	[SerializeField]
	public TMP_Text text;

	[SerializeField]
	private TMP_Text fpsWarning;

	[SerializeField]
	private float delayUpdateRate = 0.25f;

	private float updateTimer;

	public float sessionAnytrackingLost;

	public float last30SecondsTrackingLost;

	private float firstAwake;

	private bool leftHandTracked;

	private bool rightHandTracked;

	private StringBuilder builder;

	private Vector3 averagedVelocity;

	private Vector3 groundVelocity;

	private Vector3 centerHeadPos;

	private float distanceMoved;

	private float distanceSwam;

	private List<string> logMessage = new List<string>();

	private List<string> logError = new List<string>();

	private bool buttonDown;

	private bool showLog;

	private int lowFps;

	private string zones;

	private GroupJoinZoneAB lastGroupJoinZone;

	private State currentState = State.Active;

	private ProfilerRecorder drawCallsRecorder;

	private ProfilerRecorder trisRecorder;

	private string pLog;

	private bool button1Down;

	private bool button2Down;

	private bool button3Down;

	public static DebugHudStats Instance => _instance;

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
		}
		base.gameObject.SetActive(value: false);
	}

	private void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
			if (drawCallsRecorder.Valid)
			{
				drawCallsRecorder.Dispose();
			}
			if (trisRecorder.Valid)
			{
				trisRecorder.Dispose();
			}
		}
	}

	private void LateUpdate()
	{
		base.transform.LookAt(Camera.main.transform.position, Vector3.up);
		if (currentState == State.timeAdjust)
		{
			bool flag = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand);
			bool flag2 = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
			bool flag3 = ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.5f;
			bool flag4 = ControllerInputPoller.GripFloat(XRNode.RightHand) > 0.5f;
			if (button1Down && !flag)
			{
				GorillaComputer.instance.AddSeverTime(flag4 ? (-60) : 60);
			}
			if (button2Down && !flag2)
			{
				GorillaComputer.instance.AddSeverTime(flag4 ? (-1) : 5);
			}
			if (button3Down && !flag3)
			{
				GorillaComputer.instance.AddSeverTime(flag4 ? (-1440) : 1440);
			}
			button1Down = flag;
			button2Down = flag2;
			button3Down = flag3;
		}
		bool flag5 = ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
		if (buttonDown && !flag5)
		{
			Application.logMessageReceived -= LogMessageReceived;
			PlayerGameEvents.OnPlayerMoved -= OnPlayerMoved;
			PlayerGameEvents.OnPlayerSwam -= OnPlayerSwam;
			switch (currentState)
			{
			case State.Inactive:
				currentState = State.Active;
				break;
			case State.Active:
				currentState = State.ShowLog;
				break;
			case State.ShowLog:
				currentState = State.ShowError;
				break;
			case State.ShowError:
				currentState = State.ShowStats;
				break;
			case State.ShowStats:
				currentState = State.ShowRBs;
				break;
			case State.ShowRBs:
				currentState = State.timeAdjust;
				break;
			case State.timeAdjust:
				currentState = State.Inactive;
				break;
			}
			Application.logMessageReceived -= LogMessageReceived;
			PlayerGameEvents.OnPlayerMoved -= OnPlayerMoved;
			PlayerGameEvents.OnPlayerSwam -= OnPlayerSwam;
			switch (currentState)
			{
			case State.ShowLog:
			case State.ShowError:
				Application.logMessageReceived += LogMessageReceived;
				break;
			case State.ShowStats:
				distanceMoved = (distanceSwam = 0f);
				PlayerGameEvents.OnPlayerMoved += OnPlayerMoved;
				PlayerGameEvents.OnPlayerSwam += OnPlayerSwam;
				break;
			}
			text.gameObject.SetActive(currentState != State.Inactive);
			if (RigidbodyHighlighter.Instance != null)
			{
				RigidbodyHighlighter.Instance.Active = currentState == State.ShowRBs;
			}
		}
		buttonDown = flag5;
		if (firstAwake == 0f)
		{
			firstAwake = Time.time;
		}
		if (updateTimer < delayUpdateRate)
		{
			updateTimer += Time.deltaTime;
			return;
		}
		int num = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
		if (num < FPS_THRESHOLD)
		{
			lowFps++;
		}
		else
		{
			lowFps = 0;
		}
		fpsWarning.gameObject.SetActive(lowFps > 5 && currentState == State.Inactive);
		if (currentState != 0)
		{
			builder.Clear();
			builder.Append("<color=\"" + colorFromState(currentState) + "\">");
			builder.Append("gt: ");
			builder.Append(GorillaComputer.instance.version);
			builder.Append(":");
			builder.Append(GorillaComputer.instance.buildCode);
			builder.Append("</color>");
			num = Mathf.Min(num, 90);
			builder.Append((num < FPS_THRESHOLD) ? " - <color=\"red\">" : " - <color=\"white\">");
			builder.Append(num);
			builder.AppendLine(" fps</color>");
			float eyeTextureResolutionScale = XRSettings.eyeTextureResolutionScale;
			float renderViewportScale = XRSettings.renderViewportScale;
			float renderScale = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).renderScale;
			builder.AppendLine($"draw calls: {drawCallsRecorder.LastValue} tris: {trisRecorder.LastValue} " + $"rs: {eyeTextureResolutionScale}/{renderViewportScale}/{renderScale} ");
			if (GorillaComputer.instance != null)
			{
				builder.AppendLine(GorillaComputer.instance.GetServerTime().ToString());
			}
			else
			{
				builder.AppendLine("Server Time Unavailable");
			}
			zones = GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone.ToString().ToUpperInvariant();
			if (NetworkSystem.Instance.IsMasterClient)
			{
				builder.Append("H");
			}
			if (NetworkSystem.Instance.InRoom)
			{
				if (NetworkSystem.Instance.SessionIsPrivate)
				{
					builder.Append("Pri ");
				}
				else
				{
					builder.Append("Pub ");
				}
			}
			else
			{
				builder.Append("DC ");
			}
			builder.Append("z: <color=\"green\">");
			builder.Append(zones);
			builder.AppendLine("</color>");
			if (NetworkSystem.Instance.InRoom)
			{
				GorillaGameManager instance = GorillaGameManager.instance;
				if (instance != null)
				{
					GorillaTagCompetitiveManager gorillaTagCompetitiveManager = instance as GorillaTagCompetitiveManager;
					if (gorillaTagCompetitiveManager != null)
					{
						builder.Append("Ranked Mode ELO: ");
						builder.Append(gorillaTagCompetitiveManager.GetScoring().Progression.GetEloScore().ToString());
						builder.Append("  Tier: ");
						builder.AppendLine(gorillaTagCompetitiveManager.GetScoring().Progression.GetRankedProgressionTierName());
						RankedMultiplayerScore.PlayerScoreInRound inGameScoreForSelf = gorillaTagCompetitiveManager.GetScoring().GetInGameScoreForSelf();
						builder.Append("Tags: ");
						builder.Append(inGameScoreForSelf.NumTags.ToString());
						builder.Append("  Defense: ");
						builder.Append(Mathf.RoundToInt(inGameScoreForSelf.PointsOnDefense).ToString());
						builder.Append("  Score: ");
						builder.AppendLine(Mathf.RoundToInt(gorillaTagCompetitiveManager.GetScoring().ComputeGameScore(inGameScoreForSelf.NumTags, inGameScoreForSelf.PointsOnDefense)).ToString());
						if (gorillaTagCompetitiveManager.ShowDebugPing)
						{
							builder.AppendLine("Server MatchID Ping!");
						}
					}
				}
			}
			if (currentState == State.ShowStats)
			{
				builder.AppendLine();
				Vector3 vector = GTPlayer.Instance.AveragedVelocity;
				Vector3 headCenterPosition = GTPlayer.Instance.HeadCenterPosition;
				float magnitude = vector.magnitude;
				groundVelocity = vector;
				groundVelocity.y = 0f;
				builder.AppendLine($"v: {magnitude:F1} m/s\t\todo: {distanceMoved:F2}m\tswam: {distanceSwam:F2}m");
				builder.AppendLine($"ground: {groundVelocity.magnitude:F1} m/s\thead: {headCenterPosition:F2}");
			}
			else if (currentState == State.ShowLog)
			{
				builder.AppendLine();
				for (int num2 = logMessage.Count - 1; num2 >= 0; num2--)
				{
					builder.AppendLine(logMessage[num2]);
				}
			}
			else if (currentState == State.ShowError)
			{
				builder.AppendLine();
				for (int num3 = logError.Count - 1; num3 >= 0; num3--)
				{
					builder.AppendLine(logError[num3]);
				}
			}
			else if (currentState == State.timeAdjust)
			{
				builder.AppendLine();
				builder.AppendLine("Press A to advance one hour [+ R Grip to go back one hour]");
				builder.AppendLine("Press B to advance five minutes [+ R Grip to go back one minute]");
				builder.AppendLine("Press R Trigger to advance one day [+ R Grip to go back one day]");
			}
			text.text = builder.ToString();
		}
		updateTimer = 0f;
	}

	private string colorFromState(State s)
	{
		return s switch
		{
			State.ShowStats => "green", 
			State.ShowLog => "yellow", 
			State.ShowError => "orange", 
			State.ShowRBs => "red", 
			_ => "white", 
		};
	}

	private void OnPlayerSwam(float distance, float speed)
	{
		if (distance > 0.005f)
		{
			distanceSwam += distance;
		}
	}

	private void OnPlayerMoved(float distance, float speed)
	{
		if (distance > 0.005f)
		{
			distanceMoved += distance;
		}
	}

	private void OnDisable()
	{
		Application.logMessageReceived -= LogMessageReceived;
	}

	private void LogMessageReceived(string condition, string stackTrace, LogType type)
	{
		string text = $"{Time.realtimeSinceStartup:F2}> {getColorStringFromLogType(type)}{condition}</color>";
		if (pLog != condition)
		{
			logMessage.Add(text);
		}
		else
		{
			logMessage[logMessage.Count - 1] = text;
		}
		pLog = condition;
		if (logMessage.Count > 10)
		{
			logMessage.RemoveAt(0);
		}
		if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
		{
			logError.Add(text + "\n" + stackTrace);
			if (logError.Count > 10)
			{
				logError.RemoveAt(0);
			}
		}
	}

	private string getColorStringFromLogType(LogType type)
	{
		switch (type)
		{
		case LogType.Error:
		case LogType.Assert:
		case LogType.Exception:
			return "<color=\"red\">";
		case LogType.Warning:
			return "<color=\"yellow\">";
		default:
			return "<color=\"white\">";
		}
	}

	private void OnZoneChanged(ZoneData[] zoneData)
	{
		zones = string.Empty;
		for (int i = 0; i < zoneData.Length; i++)
		{
			if (zoneData[i].active)
			{
				zones = zones + zoneData[i].zone.ToString().ToUpper() + "; ";
			}
		}
	}
}
