using System;
using System.Collections;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

[Obsolete]
public class EvolvingCosmetic : MonoBehaviour, ITickSystemTick
{
	[Serializable]
	private class EvolutionStage
	{
		[Flags]
		public enum ProgressionFlags
		{
			None = 0,
			Time = 1,
			Temperature = 2
		}

		[Serializable]
		public class EventAtTime : IComparable<EventAtTime>
		{
			public enum Type
			{
				SecondsFromBeginning,
				SecondsBeforeEnd,
				DurationFraction
			}

			public string debugName;

			public float time;

			public Type type;

			public float absoluteTime;

			public UnityEvent onTimeReached;

			private string DynamicTimeLabel
			{
				get
				{
					if (type != Type.DurationFraction)
					{
						return "Time";
					}
					return "Fraction";
				}
			}

			public int CompareTo(EventAtTime other)
			{
				return absoluteTime.CompareTo(other.absoluteTime);
			}
		}

		private const float MIN_STAGE_TIME = 0.01f;

		public string debugName;

		public ProgressionFlags progressionFlags = ProgressionFlags.Time;

		[SerializeField]
		private float durationSeconds = float.NaN;

		public ThermalReceiver thermalReceiver;

		public AnimationCurve celsiusSpeedupMult = AnimationCurve.Linear(0f, 0f, 100f, 2f);

		public ContinuousPropertyArray continuousProperties;

		[SerializeField]
		private EventAtTime[] events;

		public bool HasDuration => HasAnyFlag(ProgressionFlags.Time | ProgressionFlags.Temperature);

		public bool HasTime => HasAnyFlag(ProgressionFlags.Time);

		public bool HasTemperature => HasAnyFlag(ProgressionFlags.Temperature);

		public float Duration
		{
			get
			{
				if (!HasDuration)
				{
					return 1f;
				}
				return durationSeconds;
			}
		}

		private bool HasAnyFlag(ProgressionFlags flag)
		{
			return (progressionFlags & flag) != 0;
		}

		public float DeltaTime(float deltaTime)
		{
			return (HasTime ? deltaTime : 0f) + (HasTemperature ? (deltaTime * celsiusSpeedupMult.Evaluate(thermalReceiver.celsius)) : 0f);
		}

		public EventAtTime GetEventOrNull(int index)
		{
			if (events == null || index < 0 || index >= events.Length)
			{
				return null;
			}
			return events[index];
		}
	}

	[SerializeField]
	private bool enableLooping;

	[SerializeField]
	private int loopToStageOnComplete = 1;

	[SerializeField]
	private EvolutionStage[] stages;

	private RubberDuckEvents networkEvents;

	private VRRig myRig;

	private CallLimiter callLimiter = new CallLimiter(5, 10f);

	private int activeStageIndex;

	private EvolutionStage activeStage;

	private int nextEventIndex;

	private EvolutionStage.EventAtTime nextEvent;

	private float totalElapsedTime;

	private float totalTimeOfPreviousStages;

	private float totalDuration;

	private float timeAtLoopStart;

	private float loopDuration;

	private Coroutine sendProgressDelayCoroutine;

	private int LoopMaxValue => stages.Length;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		base.gameObject.GetOrAddComponent(ref networkEvents);
		myRig = GetComponentInParent<VRRig>();
		for (int i = 0; i < stages.Length; i++)
		{
			totalDuration += stages[i].Duration;
			if (enableLooping)
			{
				if (i < loopToStageOnComplete - 1)
				{
					timeAtLoopStart += stages[i].Duration;
				}
				else
				{
					loopDuration += stages[i].Duration;
				}
			}
		}
	}

	private void OnEnable()
	{
		if (stages.Length != 0)
		{
			NetPlayer netPlayer = myRig.creator ?? NetworkSystem.Instance.LocalPlayer;
			if (netPlayer != null)
			{
				networkEvents.Init(netPlayer);
				TickSystem<object>.AddTickCallback(this);
				NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(SendElapsedTime);
				networkEvents.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(ReceiveElapsedTime);
				FirstStage();
			}
			else
			{
				Debug.LogError("Failed to get a reference to the Photon Player needed to hook up the cosmetic event");
			}
		}
	}

	private void OnDisable()
	{
		if (networkEvents != null)
		{
			TickSystem<object>.RemoveTickCallback(this);
			NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(SendElapsedTime);
			networkEvents.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(ReceiveElapsedTime);
			FirstStage();
		}
		callLimiter?.Reset();
	}

	private void Log(bool isComplete, bool isEvent)
	{
	}

	private void FirstStage()
	{
		activeStageIndex = 0;
		activeStage = stages[0];
		nextEventIndex = 0;
		nextEvent = activeStage.GetEventOrNull(0);
		totalElapsedTime = 0f;
		totalTimeOfPreviousStages = 0f;
		HandleStages();
	}

	private void HandleStages()
	{
		while (true)
		{
			float num = totalElapsedTime - totalTimeOfPreviousStages;
			float f = Mathf.Min(num / activeStage.Duration, 1f);
			activeStage.continuousProperties.ApplyAll(f);
			while (nextEvent != null && num >= nextEvent.absoluteTime)
			{
				nextEvent.onTimeReached?.Invoke();
				Log(isComplete: false, isEvent: true);
				nextEvent = activeStage.GetEventOrNull(++nextEventIndex);
			}
			if (num < activeStage.Duration)
			{
				return;
			}
			activeStageIndex++;
			if (activeStageIndex >= stages.Length && !enableLooping)
			{
				break;
			}
			if (activeStageIndex >= stages.Length)
			{
				activeStageIndex = loopToStageOnComplete - 1;
				totalTimeOfPreviousStages = timeAtLoopStart;
				totalElapsedTime -= loopDuration;
			}
			else
			{
				totalTimeOfPreviousStages += activeStage.Duration;
			}
			activeStage = stages[activeStageIndex];
			nextEventIndex = 0;
			nextEvent = activeStage.GetEventOrNull(0);
			if (!activeStage.HasDuration)
			{
				totalElapsedTime = totalTimeOfPreviousStages + activeStage.Duration * 0.5f;
				TickSystem<object>.RemoveTickCallback(this);
			}
			else
			{
				TickSystem<object>.AddTickCallback(this);
			}
			Log(isComplete: false, isEvent: false);
		}
		totalElapsedTime = totalDuration;
		TickSystem<object>.RemoveTickCallback(this);
		Log(isComplete: true, isEvent: false);
	}

	public void Tick()
	{
		totalElapsedTime = Mathf.Clamp(totalElapsedTime + Mathf.Max(activeStage.DeltaTime(Time.deltaTime), 0f), 0f, totalDuration * 1.01f);
		HandleStages();
	}

	public void CompleteManualStage()
	{
		if (!activeStage.HasDuration)
		{
			ForceNextStage();
		}
	}

	public void ForceNextStage()
	{
		totalElapsedTime = totalTimeOfPreviousStages + activeStage.Duration;
		HandleStages();
	}

	private void SendElapsedTime(NetPlayer player)
	{
		if (sendProgressDelayCoroutine != null)
		{
			StopCoroutine(sendProgressDelayCoroutine);
		}
		sendProgressDelayCoroutine = StartCoroutine(SendElapsedTimeDelayed());
	}

	private IEnumerator SendElapsedTimeDelayed()
	{
		yield return new WaitForSeconds(1f);
		sendProgressDelayCoroutine = null;
		networkEvents.Activate.RaiseOthers(totalElapsedTime);
	}

	private void ReceiveElapsedTime(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender == target)
		{
			MonkeAgent.IncrementRPCCall(info, "ReceiveElapsedTime");
			if (info.senderID == myRig.creator.ActorNumber && callLimiter.CheckCallServerTime(Time.unscaledTime) && args.Length == 1 && args[0] is float num && float.IsFinite(num) && !(num > totalDuration) && !(num < 0f))
			{
				totalElapsedTime = num;
				HandleStages();
			}
		}
	}

	private void SetStage(int targetIndex)
	{
		if (stages == null || stages.Length == 0)
		{
			return;
		}
		if (enableLooping)
		{
			if (targetIndex < 0)
			{
				targetIndex = stages.Length - 1;
			}
			else if (targetIndex >= stages.Length)
			{
				targetIndex = 0;
			}
		}
		else
		{
			targetIndex = Mathf.Clamp(targetIndex, 0, stages.Length - 1);
		}
		activeStageIndex = targetIndex;
		activeStage = stages[targetIndex];
		float num = 0f;
		for (int i = 0; i < targetIndex; i++)
		{
			num += stages[i].Duration;
		}
		totalTimeOfPreviousStages = num;
		totalElapsedTime = num + Mathf.Epsilon;
		nextEventIndex = 0;
		nextEvent = activeStage.GetEventOrNull(0);
		if (activeStage.HasDuration)
		{
			TickSystem<object>.AddTickCallback(this);
		}
		else
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
		int num2 = 0;
		for (EvolutionStage.EventAtTime eventOrNull = activeStage.GetEventOrNull(num2); eventOrNull != null; eventOrNull = activeStage.GetEventOrNull(num2))
		{
			eventOrNull.onTimeReached?.Invoke();
			num2++;
		}
		HandleStages();
	}

	private void RestartStageInternal()
	{
		SetStage(activeStageIndex);
	}

	public void IncrementStage()
	{
		SetStage(activeStageIndex + 1);
	}

	public void DecrementStage()
	{
		SetStage(activeStageIndex - 1);
	}

	public void JumpToFirstStage()
	{
		SetStage(0);
	}

	public void JumpToLastStage()
	{
		if (stages != null && stages.Length != 0)
		{
			SetStage(stages.Length - 1);
		}
	}

	public void RestartCurrentStage()
	{
		RestartStageInternal();
	}

	public void JumpToStageIndex(int index)
	{
		SetStage(index);
	}
}
