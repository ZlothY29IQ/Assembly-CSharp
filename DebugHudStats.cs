using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag;
using GorillaUtil;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
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
		timeAdjust,
		RecordingMode,
		TitleDataMonitor
	}

	public static int FPS_THRESHOLD = 89;

	private static DebugHudStats _instance;

	[SerializeField]
	public TMP_Text text;

	[SerializeField]
	public TMP_Text logging;

	[SerializeField]
	public TMP_Text logPage;

	[SerializeField]
	private TMP_Text fpsWarning;

	[SerializeField]
	private TMP_Text dismiss;

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

	private List<string> logTD = new List<string>();

	private bool buttonDown;

	private bool buttonDownBack;

	private bool spoofIds;

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

	private bool button5Down;

	private bool button6Down;

	private bool button7Down;

	private bool button8Down;

	[SerializeField]
	private StringTable betaTitleDataOveride;

	private Array fixedWeathers;

	private int fixedWeatherIndex;

	private float btnDownTime;

	public static DebugHudStats Instance => _instance;

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
			fixedWeathers = Enum.GetValues(typeof(BetterDayNightManager.WeatherType));
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
		if (GTPlayerTransform.Instance != null)
		{
			base.transform.LookAt(Camera.main.transform.position, GTPlayerTransform.Instance.GravityUp);
		}
		else
		{
			base.transform.LookAt(Camera.main.transform.position, Vector3.up);
		}
		if (!ControllerInputPoller.HandTrackingActive())
		{
			if (currentState == State.timeAdjust)
			{
				bool flag = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand);
				bool flag2 = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
				bool flag3 = ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.5f;
				bool flag4 = ControllerInputPoller.GripFloat(XRNode.RightHand) > 0.5f;
				bool flag5 = ControllerInputPoller.Primary2DAxis(XRNode.LeftHand).x > 0.5f;
				bool flag6 = ControllerInputPoller.Primary2DAxis(XRNode.LeftHand).x < -0.5f;
				bool flag7 = ControllerInputPoller.Primary2DAxis(XRNode.LeftHand).y > 0.5f;
				bool flag8 = ControllerInputPoller.Primary2DAxis(XRNode.LeftHand).y < -0.5f;
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
				if (!button5Down && flag5)
				{
					ChangeTOD(1);
				}
				if (!button6Down && flag6)
				{
					ChangeTOD(-1);
				}
				if (!button7Down && flag7)
				{
					ChangeWeather(1);
				}
				if (!button8Down && flag8)
				{
					ChangeWeather(-1);
				}
				button1Down = flag;
				button2Down = flag2;
				button3Down = flag3;
				button5Down = flag5;
				button6Down = flag6;
				button7Down = flag7;
				button8Down = flag8;
			}
			if (currentState == State.TitleDataMonitor || currentState == State.ShowLog || currentState == State.ShowError)
			{
				bool flag9 = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand);
				bool flag10 = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
				if (button1Down && !flag9)
				{
					logging.pageToDisplay = ((logging.pageToDisplay >= logging.textInfo.pageCount) ? 1 : (logging.pageToDisplay + 1));
					updateLogTitle();
				}
				if (button2Down && !flag10)
				{
					logging.pageToDisplay = ((logging.pageToDisplay > 1) ? (logging.pageToDisplay - 1) : logging.textInfo.pageCount);
					updateLogTitle();
				}
				button1Down = flag9;
				button2Down = flag10;
			}
			bool flag11 = ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
			bool flag12 = ControllerInputPoller.PrimaryButtonPress(XRNode.LeftHand);
			if ((buttonDown && !flag11) || (buttonDownBack && !flag12))
			{
				NextState(buttonDown);
				if (currentState == State.ShowStats)
				{
					distanceMoved = (distanceSwam = 0f);
					PlayerGameEvents.OnPlayerMoved += OnPlayerMoved;
					PlayerGameEvents.OnPlayerSwam += OnPlayerSwam;
				}
				text.gameObject.SetActive(currentState != State.Inactive);
				if (RigidbodyHighlighter.Instance != null)
				{
					RigidbodyHighlighter.Instance.Active = currentState == State.ShowRBs;
				}
				btnDownTime = 0f;
			}
			buttonDown = flag11;
			buttonDownBack = flag12;
		}
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
		if (currentState != State.Inactive)
		{
			builder.Clear();
			builder.Append("gt: ");
			builder.Append(GorillaComputer.instance.version);
			builder.Append(":");
			builder.Append(GorillaComputer.instance.buildCode);
			builder.AppendLine(spoofIds ? " <color=\"red\">*Spoofing IDs*</color>" : string.Empty);
			num = Mathf.Min(num, 90);
			builder.Append((num < FPS_THRESHOLD) ? "<color=\"red\">" : "<color=\"white\">");
			builder.Append(num);
			builder.Append($" fps / {FPS_THRESHOLD + 1} fps</color> ");
			builder.AppendLine($"sfps: {GorillaTagger.Instance.SmoothedFramerate} (Health: {GorillaTagger.Instance.FramerateHealth})");
			float eyeTextureResolutionScale = XRSettings.eyeTextureResolutionScale;
			float renderViewportScale = XRSettings.renderViewportScale;
			float renderScale = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).renderScale;
			builder.AppendLine($"draw calls: {drawCallsRecorder.LastValue} tris: {trisRecorder.LastValue} " + $"rs: {eyeTextureResolutionScale}/{renderViewportScale}/{renderScale} ");
			builder.AppendLine($"Memory: {Profiler.GetMonoUsedSizeLong() / 1048576}M");
			if (GorillaComputer.instance != null)
			{
				DateTime serverTime = GorillaComputer.instance.GetServerTime();
				builder.AppendLine(string.Format("<color={0}>{1}</color>", (serverTime.Year > 2020) ? "#00FFAA" : "#FF3333", serverTime));
			}
			else
			{
				builder.AppendLine("<color=#FF3333>Server Time Unavailable</color>");
			}
			ZoneDef currentNode = GorillaTagger.Instance.offlineVRRig.zoneEntity.currentNode;
			if (currentNode != null)
			{
				zones = $"{currentNode.gameObject.name.ToUpperInvariant()}/{currentNode.zoneId}/{currentNode.subZoneId}";
			}
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
			switch (currentState)
			{
			case State.ShowStats:
			{
				builder.AppendLine("\nStats:\n");
				Vector3 vector = GTPlayer.Instance.AveragedVelocity;
				Vector3 headCenterPosition = GTPlayer.Instance.HeadCenterPosition;
				float magnitude = vector.magnitude;
				groundVelocity = vector;
				groundVelocity.y = 0f;
				builder.AppendLine($"v: {magnitude:F1} m/s\t\todo: {distanceMoved:F2}m\tswam: {distanceSwam:F2}m");
				builder.AppendLine($"ground: {groundVelocity.magnitude:F1} m/s\thead: {headCenterPosition:F2}");
				break;
			}
			case State.timeAdjust:
				builder.AppendLine("\nAdjust Time\n");
				builder.AppendLine("Press [A] to advance one hour [+ R Grip to go back one hour]");
				builder.AppendLine("Press [B] to advance five minutes [+ R Grip to go back one minute]");
				builder.AppendLine("Press [R] Trigger to advance one day [+ R Grip to go back one day]");
				builder.AppendLine($"\nAdjust Environment {BetterDayNightManager.instance.currentTimeIndex + 1}/{BetterDayNightManager.instance.timeOfDayRange.Length} : {BetterDayNightManager.instance.CurrentWeather()} \n");
				builder.AppendLine("[L STICK L/R] to change Time Of Day. [L STICK U/D] to change Weather.");
				break;
			case State.RecordingMode:
				builder.AppendLine("\nMo-Cap Recording:\n");
				break;
			case State.ShowRBs:
				builder.AppendLine("\nRigid Body Locator\n");
				break;
			}
			text.text = builder.ToString();
		}
		updateTimer = 0f;
		if (buttonDown && currentState != State.RecordingMode)
		{
			btnDownTime += Time.deltaTime / delayUpdateRate;
			if (btnDownTime >= 15f)
			{
				base.gameObject.SetActive(value: false);
			}
			dismiss.text = $"let go of that button in the next {15f - btnDownTime:0.0} seconds or the debug hud will vanish forever";
		}
		dismiss.gameObject.SetActive(buttonDown && currentState != State.RecordingMode && btnDownTime > 5f);
	}

	private void ChangeTOD(int v)
	{
		int num = (BetterDayNightManager.instance.currentTimeIndex + BetterDayNightManager.instance.timeOfDayRange.Length + v) % BetterDayNightManager.instance.timeOfDayRange.Length;
		BetterDayNightManager.instance.SetTimeOfDay(num);
		BetterDayNightManager.instance.SetOverrideIndex(num);
		BetterDayNightManager.instance.SetFixedWeather((BetterDayNightManager.WeatherType)fixedWeathers.GetValue(fixedWeatherIndex));
	}

	private void ChangeWeather(int v)
	{
		fixedWeatherIndex = (fixedWeatherIndex + fixedWeathers.Length + v) % fixedWeathers.Length;
		BetterDayNightManager.instance.SetFixedWeather((BetterDayNightManager.WeatherType)fixedWeathers.GetValue(fixedWeatherIndex));
	}

	private void NextState(bool fwd)
	{
		PlayerGameEvents.OnPlayerMoved -= OnPlayerMoved;
		PlayerGameEvents.OnPlayerSwam -= OnPlayerSwam;
		logging.gameObject.SetActive(value: false);
		logging.pageToDisplay = 1;
		if (currentState == State.timeAdjust)
		{
			BetterDayNightManager.instance.ClearFixedWeather();
		}
		switch (currentState)
		{
		case State.Inactive:
			currentState = (fwd ? State.Active : State.timeAdjust);
			break;
		case State.Active:
			currentState = (fwd ? State.ShowLog : State.Inactive);
			break;
		case State.ShowLog:
			currentState = ((!fwd) ? State.Active : State.ShowError);
			break;
		case State.ShowError:
			currentState = (fwd ? State.ShowStats : State.ShowLog);
			break;
		case State.ShowStats:
			currentState = (fwd ? State.ShowRBs : State.ShowError);
			break;
		case State.ShowRBs:
			currentState = (fwd ? State.TitleDataMonitor : State.ShowStats);
			break;
		case State.TitleDataMonitor:
			currentState = (fwd ? State.timeAdjust : State.ShowRBs);
			break;
		case State.timeAdjust:
			currentState = ((!fwd) ? State.TitleDataMonitor : State.Inactive);
			break;
		case State.RecordingMode:
			currentState = ((!fwd) ? State.timeAdjust : State.Inactive);
			break;
		}
		if (currentState == State.timeAdjust)
		{
			BetterDayNightManager.instance.SetFixedWeather((BetterDayNightManager.WeatherType)fixedWeathers.GetValue(fixedWeatherIndex));
		}
		UpdateLog();
	}

	private void DisplayLog(List<string> log)
	{
		logging.gameObject.SetActive(value: true);
		logging.text = string.Empty;
		for (int num = log.Count - 1; num >= 0; num--)
		{
			TMP_Text tMP_Text = logging;
			tMP_Text.text = tMP_Text.text + log[num] + "\n";
		}
		updateLogTitle();
	}

	private async void updateLogTitle()
	{
		await Task.Yield();
		logPage.text = $"{logTitleFromState(currentState)} <<[B] turn page [A]>> ({logging.pageToDisplay}/{logging.textInfo.pageCount})";
	}

	private string logTitleFromState(State s)
	{
		return s switch
		{
			State.ShowLog => "Debug Log", 
			State.ShowError => "Error Log", 
			State.TitleDataMonitor => "Title Data Log", 
			_ => string.Empty, 
		};
	}

	private string colorFromState(State s)
	{
		return s switch
		{
			State.ShowStats => "\"green\"", 
			State.ShowLog => "\"yellow\"", 
			State.ShowError => "\"orange\"", 
			State.ShowRBs => "\"red\"", 
			State.RecordingMode => "\"purple\"", 
			State.TitleDataMonitor => "#00ffff", 
			_ => "#ffffff", 
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

	private void OnEnable()
	{
		Application.logMessageReceived += LogMessageReceived;
		PlayFabTitleDataCache.OnValueRetieved = (Action<string, string>)Delegate.Combine(PlayFabTitleDataCache.OnValueRetieved, new Action<string, string>(TDValueRetrieved));
		PlayFabTitleDataCache.OnCachedValueRetieved = (Action<string, string>)Delegate.Combine(PlayFabTitleDataCache.OnCachedValueRetieved, new Action<string, string>(TDCachedValueRetrieved));
	}

	private void TDValueRetrieved(string arg1, string arg2)
	{
		logTD.Add($" >{Time.realtimeSinceStartup:F2}> TitleData[ <color=#ffaaff>{arg1}</color> ] = {arg2}");
		if (logTD.Count > 1000)
		{
			logTD.RemoveAt(0);
		}
		UpdateLog();
	}

	private void TDCachedValueRetrieved(string arg1, string arg2)
	{
		logTD.Add($" >{Time.realtimeSinceStartup:F2}> TitleData[ <color=#00ffff>{arg1}</color> ] = {arg2}");
		if (logTD.Count > 1000)
		{
			logTD.RemoveAt(0);
		}
		UpdateLog();
	}

	private void OnDisable()
	{
		PlayFabTitleDataCache.OnValueRetieved = (Action<string, string>)Delegate.Remove(PlayFabTitleDataCache.OnValueRetieved, new Action<string, string>(TDValueRetrieved));
		PlayFabTitleDataCache.OnCachedValueRetieved = (Action<string, string>)Delegate.Remove(PlayFabTitleDataCache.OnCachedValueRetieved, new Action<string, string>(TDCachedValueRetrieved));
		Application.logMessageReceived -= LogMessageReceived;
	}

	private void LogMessageReceived(string condition, string stackTrace, LogType type)
	{
		string text = $" >{Time.realtimeSinceStartup:F2}> {getColorStringFromLogType(type)}{condition}</color>";
		if (pLog != condition)
		{
			logMessage.Add(text);
		}
		else
		{
			logMessage[logMessage.Count - 1] = text;
		}
		pLog = condition;
		if (logMessage.Count > 100)
		{
			logMessage.RemoveAt(0);
		}
		if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
		{
			logError.Add(text + "\n" + stackTrace);
			if (logError.Count > 100)
			{
				logError.RemoveAt(0);
			}
		}
		UpdateLog();
	}

	private void UpdateLog()
	{
		switch (currentState)
		{
		case State.ShowLog:
			DisplayLog(logMessage);
			break;
		case State.ShowError:
			DisplayLog(logError);
			break;
		case State.TitleDataMonitor:
			DisplayLog(logTD);
			break;
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
