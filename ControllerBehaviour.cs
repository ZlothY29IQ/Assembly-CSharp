using System;
using UnityEngine;

[Obsolete("Use ControllerInputPoller instead", false)]
public class ControllerBehaviour : MonoBehaviour, IBuildValidation
{
	public delegate void OnActionEvent();

	private float actionTime;

	private float repeatAction = 1f;

	[SerializeField]
	private UXSettings uxSettings;

	[SerializeField]
	private float actionDelay = 0.5f;

	[SerializeField]
	private float actionRepeatDelayReduction = 0.5f;

	[Tooltip("Should the triggers modify the x axis like the sticks do?")]
	[SerializeField]
	private bool useTriggersAsSticks;

	private ControllerInputPoller poller;

	private bool wasLeftStick;

	private bool wasRightStick;

	private bool wasUpStick;

	private bool wasDownStick;

	private bool wasHeld;

	[field: OnEnterPlay_SetNull]
	public static ControllerBehaviour Instance { get; private set; }

	private ControllerInputPoller Poller
	{
		get
		{
			if (poller != null)
			{
				return poller;
			}
			if (ControllerInputPoller.instance != null)
			{
				poller = ControllerInputPoller.instance;
				return poller;
			}
			return null;
		}
	}

	public bool ButtonDown
	{
		get
		{
			if (!(Poller == null))
			{
				if (!Poller.leftControllerPrimaryButton && !Poller.leftControllerSecondaryButton && !Poller.rightControllerPrimaryButton)
				{
					return Poller.rightControllerSecondaryButton;
				}
				return true;
			}
			return false;
		}
	}

	public bool LeftButtonDown
	{
		get
		{
			if (!(Poller == null))
			{
				if (!Poller.leftControllerPrimaryButton && !Poller.leftControllerSecondaryButton)
				{
					return Poller.leftControllerTriggerButton;
				}
				return true;
			}
			return false;
		}
	}

	public bool RightButtonDown
	{
		get
		{
			if (!(Poller == null))
			{
				if (!Poller.rightControllerPrimaryButton && !Poller.rightControllerSecondaryButton)
				{
					return Poller.rightControllerTriggerButton;
				}
				return true;
			}
			return false;
		}
	}

	public bool IsLeftStick
	{
		get
		{
			if (!(Poller == null))
			{
				return Mathf.Min(Poller.leftControllerPrimary2DAxis.x, Poller.rightControllerPrimary2DAxis.x) < 0f - uxSettings.StickSensitvity;
			}
			return false;
		}
	}

	public bool IsRightStick
	{
		get
		{
			if (!(Poller == null))
			{
				return Mathf.Max(Poller.leftControllerPrimary2DAxis.x, Poller.rightControllerPrimary2DAxis.x) > uxSettings.StickSensitvity;
			}
			return false;
		}
	}

	public bool IsUpStick
	{
		get
		{
			if (!(Poller == null))
			{
				return Mathf.Max(Poller.leftControllerPrimary2DAxis.y, Poller.rightControllerPrimary2DAxis.y) > uxSettings.StickSensitvity;
			}
			return false;
		}
	}

	public bool IsDownStick
	{
		get
		{
			if (!(Poller == null))
			{
				return Mathf.Min(Poller.leftControllerPrimary2DAxis.y, Poller.rightControllerPrimary2DAxis.y) < 0f - uxSettings.StickSensitvity;
			}
			return false;
		}
	}

	public float StickXValue
	{
		get
		{
			if (!(Poller == null))
			{
				return Mathf.Max(Mathf.Abs(Poller.leftControllerPrimary2DAxis.x), Mathf.Abs(Poller.rightControllerPrimary2DAxis.x));
			}
			return 0f;
		}
	}

	public float StickYValue
	{
		get
		{
			if (!(Poller == null))
			{
				return Mathf.Max(Mathf.Abs(Poller.leftControllerPrimary2DAxis.y), Mathf.Abs(Poller.rightControllerPrimary2DAxis.y));
			}
			return 0f;
		}
	}

	public bool TriggerDown
	{
		get
		{
			if (!(Poller == null))
			{
				if (!Poller.leftControllerTriggerButton)
				{
					return Poller.rightControllerTriggerButton;
				}
				return true;
			}
			return false;
		}
	}

	public event OnActionEvent OnAction;

	private void Awake()
	{
		if (Instance != null)
		{
			Debug.LogError("[CONTROLLER_BEHAVIOUR] Trying to create new singleton but one already exists", base.gameObject);
			UnityEngine.Object.DestroyImmediate(this);
		}
		else
		{
			Instance = this;
		}
	}

	private void Update()
	{
		bool flag = (IsLeftStick && wasLeftStick) || (IsRightStick && wasRightStick) || (IsUpStick && wasUpStick) || (IsDownStick && wasDownStick);
		if (!(Time.time - actionTime < actionDelay / repeatAction))
		{
			if (wasHeld && flag)
			{
				repeatAction += actionRepeatDelayReduction;
			}
			else
			{
				repeatAction = 1f;
			}
			if (IsLeftStick || IsRightStick || IsUpStick || IsDownStick || ButtonDown)
			{
				actionTime = Time.time;
			}
			if (this.OnAction != null)
			{
				this.OnAction();
			}
			wasHeld = flag;
			wasDownStick = IsDownStick;
			wasUpStick = IsUpStick;
			wasLeftStick = IsLeftStick;
			wasRightStick = IsRightStick;
		}
	}

	public bool BuildValidationCheck()
	{
		if (uxSettings == null)
		{
			Debug.LogError("ControllerBehaviour must set UXSettings");
			return false;
		}
		return true;
	}

	public static ControllerBehaviour CreateNewControllerBehaviour(GameObject gameObject, UXSettings settings)
	{
		ControllerBehaviour controllerBehaviour = gameObject.AddComponent<ControllerBehaviour>();
		controllerBehaviour.uxSettings = settings;
		return controllerBehaviour;
	}
}
