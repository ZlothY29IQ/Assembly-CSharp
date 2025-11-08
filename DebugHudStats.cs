using System.Collections.Generic;
using System.Text;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR;

public class DebugHudStats : MonoBehaviour
{
	private enum State
	{
		Inactive,
		Active,
		ShowLog,
		ShowStats
	}

	private const int FPS_THRESHOLD = 89;

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

	private List<string> logMessages = new List<string>();

	private bool buttonDown;

	private bool showLog;

	private int lowFps;

	private string zones;

	private GroupJoinZoneAB lastGroupJoinZone;

	private State currentState = State.Active;

	private ProfilerRecorder drawCallsRecorder;

	private ProfilerRecorder trisRecorder;

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

	private void Update()
	{
		bool flag = ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
		if (flag != buttonDown)
		{
			buttonDown = flag;
			if (!buttonDown)
			{
				switch (currentState)
				{
				case State.ShowStats:
					PlayerGameEvents.OnPlayerMoved += OnPlayerMoved;
					PlayerGameEvents.OnPlayerSwam += OnPlayerSwam;
					break;
				}
				switch (currentState)
				{
				case State.Inactive:
					currentState = State.Active;
					text.gameObject.SetActive(value: true);
					break;
				case State.Active:
					currentState = State.ShowLog;
					break;
				case State.ShowLog:
					currentState = State.ShowStats;
					distanceMoved = (distanceSwam = 0f);
					PlayerGameEvents.OnPlayerMoved += OnPlayerMoved;
					PlayerGameEvents.OnPlayerSwam += OnPlayerSwam;
					break;
				case State.ShowStats:
					currentState = State.Inactive;
					text.gameObject.SetActive(value: false);
					break;
				}
				if (RigidbodyHighlighter.Instance != null)
				{
					RigidbodyHighlighter.Instance.Active = currentState != State.Inactive;
				}
			}
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
		if (num < 89)
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
			builder.Append("v: ");
			builder.Append(GorillaComputer.instance.version);
			builder.Append(":");
			builder.Append(GorillaComputer.instance.buildCode);
			num = Mathf.Min(num, 90);
			builder.Append((num < 89) ? " - <color=\"red\">" : " - <color=\"white\">");
			builder.Append(num);
			builder.AppendLine(" fps</color>");
			builder.AppendLine($"draw calls: {drawCallsRecorder.LastValue} tris: {trisRecorder.LastValue}");
			if (GorillaComputer.instance != null)
			{
				builder.AppendLine(GorillaComputer.instance.GetServerTime().ToString());
			}
			else
			{
				builder.AppendLine("Server Time Unavailable");
			}
			zones = GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone.ToString();
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
			builder.Append("Z: <color=\"orange\">");
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
				builder.AppendLine($"v: {magnitude:F1} m/s");
				builder.AppendLine($"ground: {groundVelocity.magnitude:F1} m/s");
				builder.AppendLine($"head: {headCenterPosition:F2}\n");
				builder.AppendLine($"odo: {distanceMoved:F2}m");
				builder.AppendLine($"swam: {distanceSwam:F2}m");
			}
			else if (currentState == State.ShowLog)
			{
				builder.AppendLine();
				for (int i = 0; i < logMessages.Count; i++)
				{
					builder.AppendLine(logMessages[i]);
				}
			}
			text.text = builder.ToString();
		}
		updateTimer = 0f;
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
	}

	private void OnDisable()
	{
		Application.logMessageReceived -= LogMessageReceived;
	}

	private void LogMessageReceived(string condition, string stackTrace, LogType type)
	{
		logMessages.Add(getColorStringFromLogType(type) + condition + "</color>");
		if (logMessages.Count > 6)
		{
			logMessages.RemoveAt(0);
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
