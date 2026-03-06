using System.Collections.Generic;
using GorillaLocomotion;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace UnityEngine.XR.Interaction.Toolkit;

public class GorillaSnapTurn : LocomotionProvider, ITickSystemTick
{
	public enum InputAxes
	{
		Primary2DAxis,
		Secondary2DAxis
	}

	[Header("References")]
	[SerializeField]
	private XROrigin xrOrigin;

	private static readonly InputFeatureUsage<Vector2>[] m_Vec2UsageList = new InputFeatureUsage<Vector2>[2]
	{
		CommonUsages.primary2DAxis,
		CommonUsages.secondary2DAxis
	};

	[SerializeField]
	[Tooltip("The 2D Input Axis on the primary devices that will be used to trigger a snap turn.")]
	private InputAxes m_TurnUsage;

	[SerializeField]
	[Tooltip("A list of controllers that allow Snap Turn.  If an XRController is not enabled, or does not have input actions enabled.  Snap Turn will not work.")]
	private List<XRController> m_Controllers = new List<XRController>();

	[SerializeField]
	[Tooltip("The number of degrees clockwise to rotate when snap turning clockwise.")]
	private float m_TurnAmount = 45f;

	[SerializeField]
	[Tooltip("The amount of time that the system will wait before starting another snap turn.")]
	private float m_DebounceTime = 0.5f;

	[SerializeField]
	[Tooltip("The deadzone that the controller movement will have to be above to trigger a snap turn.")]
	private float m_DeadZone = 0.75f;

	private float m_CurrentTurnAmount;

	private float m_TimeStarted;

	private bool m_AxisReset;

	public float turnSpeed = 1f;

	private HashSet<ISnapTurnOverride> turningOverriders = new HashSet<ISnapTurnOverride>();

	private List<bool> m_ControllersWereActive = new List<bool>();

	private static int _cachedTurnFactor;

	private static string _cachedTurnType;

	private string m_TurnType = "";

	private int m_TurnFactor = 1;

	[OnEnterPlay_SetNull]
	private static GorillaSnapTurn _cachedReference;

	public bool TickRunning { get; set; }

	public InputAxes turnUsage
	{
		get
		{
			return m_TurnUsage;
		}
		set
		{
			m_TurnUsage = value;
		}
	}

	public List<XRController> controllers
	{
		get
		{
			return m_Controllers;
		}
		set
		{
			m_Controllers = value;
		}
	}

	public float turnAmount
	{
		get
		{
			return m_TurnAmount;
		}
		set
		{
			m_TurnAmount = value;
		}
	}

	public float debounceTime
	{
		get
		{
			return m_DebounceTime;
		}
		set
		{
			m_DebounceTime = value;
		}
	}

	public float deadZone
	{
		get
		{
			return m_DeadZone;
		}
		set
		{
			m_DeadZone = value;
		}
	}

	public string turnType
	{
		get
		{
			return m_TurnType;
		}
		private set
		{
			m_TurnType = value;
		}
	}

	public int turnFactor
	{
		get
		{
			return m_TurnFactor;
		}
		private set
		{
			m_TurnFactor = value;
		}
	}

	public static GorillaSnapTurn CachedSnapTurnRef
	{
		get
		{
			if (_cachedReference == null)
			{
				Debug.LogError("[SNAP_TURN] Tried accessing static cached reference, but was still null. Trying to find component in scene");
				_cachedReference = Object.FindAnyObjectByType<GorillaSnapTurn>();
			}
			return _cachedReference;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (_cachedReference != null)
		{
			Debug.LogError("[SNAP_TURN] A [GorillaSnapTurn] component already exists in the scene");
			return;
		}
		_cachedReference = this;
		TickSystem<object>.AddTickCallback(this);
	}

	public void Tick()
	{
		ValidateTurningOverriders();
		if (m_Controllers.Count > 0)
		{
			EnsureControllerDataListSize();
			for (int i = 0; i < m_Controllers.Count; i++)
			{
				XRController xRController = m_Controllers[i];
				if (!(xRController == null) && xRController.enableInputActions)
				{
					float num = 0f;
					if (xRController.controllerNode == XRNode.RightHand)
					{
						num = ControllerInputPoller.instance.rightControllerPrimary2DAxis.x;
					}
					else if (xRController.controllerNode == XRNode.LeftHand)
					{
						num = ControllerInputPoller.instance.leftControllerPrimary2DAxis.x;
					}
					if (num > deadZone)
					{
						StartTurn(m_TurnAmount);
					}
					else if (num < 0f - deadZone)
					{
						StartTurn(0f - m_TurnAmount);
					}
					else
					{
						m_AxisReset = true;
					}
				}
			}
		}
		if (Mathf.Abs(m_CurrentTurnAmount) > 0f && TryPrepareLocomotion())
		{
			if (xrOrigin != null)
			{
				GTPlayer.Instance.Turn(m_CurrentTurnAmount);
			}
			m_CurrentTurnAmount = 0f;
			TryEndLocomotion();
		}
	}

	private void EnsureControllerDataListSize()
	{
		if (m_Controllers.Count != m_ControllersWereActive.Count)
		{
			while (m_ControllersWereActive.Count < m_Controllers.Count)
			{
				m_ControllersWereActive.Add(item: false);
			}
			while (m_ControllersWereActive.Count < m_Controllers.Count)
			{
				m_ControllersWereActive.RemoveAt(m_ControllersWereActive.Count - 1);
			}
		}
	}

	internal void FakeStartTurn(bool isLeft)
	{
		StartTurn(isLeft ? (0f - m_TurnAmount) : m_TurnAmount);
	}

	private void StartTurn(float amount)
	{
		if ((!(m_TimeStarted + m_DebounceTime > Time.time) || m_AxisReset) && !base.isLocomotionActive && turningOverriders.Count <= 0)
		{
			m_TimeStarted = Time.time;
			m_CurrentTurnAmount = amount;
			m_AxisReset = false;
		}
	}

	public void ChangeTurnMode(string turnMode, int turnSpeedFactor)
	{
		turnType = turnMode;
		turnFactor = turnSpeedFactor;
		if (!(turnMode == "SNAP"))
		{
			if (turnMode == "SMOOTH")
			{
				m_DebounceTime = 0f;
				m_TurnAmount = 360f * Time.fixedDeltaTime * ConvertedTurnFactor(turnSpeedFactor);
			}
			else
			{
				m_DebounceTime = 0f;
				m_TurnAmount = 0f;
			}
		}
		else
		{
			m_DebounceTime = 0.5f;
			m_TurnAmount = 60f * ConvertedTurnFactor(turnSpeedFactor);
		}
	}

	public float ConvertedTurnFactor(float newTurnSpeed)
	{
		return Mathf.Max(0.75f, 0.5f + newTurnSpeed / 10f * 1.5f);
	}

	public void SetTurningOverride(ISnapTurnOverride caller)
	{
		if (!turningOverriders.Contains(caller))
		{
			turningOverriders.Add(caller);
		}
	}

	public void UnsetTurningOverride(ISnapTurnOverride caller)
	{
		if (turningOverriders.Contains(caller))
		{
			turningOverriders.Remove(caller);
		}
	}

	public void ValidateTurningOverriders()
	{
		foreach (ISnapTurnOverride turningOverrider in turningOverriders)
		{
			if (turningOverrider == null || !turningOverrider.TurnOverrideActive())
			{
				turningOverriders.Remove(turningOverrider);
			}
		}
	}

	public static void DisableSnapTurn()
	{
		Debug.Log("[SNAP_TURN] Disabling Snap Turn");
		if (!(CachedSnapTurnRef == null))
		{
			_cachedTurnFactor = PlayerPrefs.GetInt("turnFactor");
			_cachedTurnType = PlayerPrefs.GetString("stickTurning");
			CachedSnapTurnRef.ChangeTurnMode("NONE", 0);
		}
	}

	public static void UpdateAndSaveTurnType(string mode)
	{
		if (CachedSnapTurnRef == null)
		{
			Debug.LogError("[SNAP_TURN] Failed to Update, [CachedSnapTurnRef] is NULL");
			return;
		}
		PlayerPrefs.SetString("stickTurning", mode);
		PlayerPrefs.Save();
		CachedSnapTurnRef.ChangeTurnMode(mode, CachedSnapTurnRef.turnFactor);
	}

	public static void UpdateAndSaveTurnFactor(int factor)
	{
		if (CachedSnapTurnRef == null)
		{
			Debug.LogError("[SNAP_TURN] Failed to Update, [CachedSnapTurnRef] is NULL");
			return;
		}
		PlayerPrefs.SetInt("turnFactor", factor);
		PlayerPrefs.Save();
		CachedSnapTurnRef.ChangeTurnMode(CachedSnapTurnRef.turnType, factor);
	}

	public static void LoadSettingsFromPlayerPrefs()
	{
		if (!(CachedSnapTurnRef == null))
		{
			string defaultValue = ((Application.platform == RuntimePlatform.Android) ? "NONE" : "SNAP");
			string turnMode = PlayerPrefs.GetString("stickTurning", defaultValue);
			int turnSpeedFactor = PlayerPrefs.GetInt("turnFactor", 4);
			CachedSnapTurnRef.ChangeTurnMode(turnMode, turnSpeedFactor);
		}
	}

	public static void LoadSettingsFromCache()
	{
		if (!(CachedSnapTurnRef == null))
		{
			if (string.IsNullOrEmpty(_cachedTurnType))
			{
				_cachedTurnType = ((Application.platform == RuntimePlatform.Android) ? "NONE" : "SNAP");
			}
			string cachedTurnType = _cachedTurnType;
			int cachedTurnFactor = _cachedTurnFactor;
			CachedSnapTurnRef.ChangeTurnMode(cachedTurnType, cachedTurnFactor);
		}
	}
}
