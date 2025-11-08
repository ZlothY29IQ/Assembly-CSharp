using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

[RequireComponent(typeof(Collider))]
public class CosmeticSwipeReactor : MonoBehaviour, ITickSystemTick
{
	public enum Axis
	{
		X,
		Y,
		Z
	}

	[SerializeField]
	private Axis localSwipeAxis = Axis.Y;

	private Vector3 swipeDir = Vector3.up;

	[Tooltip("Distance hand can move perpindicular to the swipe without cancelling the gesture")]
	[SerializeField]
	private float lateralMovementTolerance = 0.1f;

	[Tooltip("How far the hand has to move along the axis to count as a swipe\nThis distance must be contained within the trigger area")]
	[SerializeField]
	private float swipeDistance = 0.3f;

	[SerializeField]
	private float minimumVelocity = 0.1f;

	[SerializeField]
	private float maximumVelocity = 3f;

	[Tooltip("Delay after completing a swipe before starting the next")]
	[SerializeField]
	private float swipeCooldown = 0.25f;

	[SerializeField]
	private bool resetCooldownOnTriggerExit = true;

	[Tooltip("Amplitude of haptics from normalized swiped distance")]
	[SerializeField]
	private AnimationCurve swipeHaptics = AnimationCurve.EaseInOut(0f, 0.02f, 1f, 0.5f);

	public UnityEvent<bool> OnSwipe;

	public UnityEvent<bool> OnReverseSwipe;

	private VRRig _rig;

	private Collider col;

	private bool isLocal;

	private bool handInTriggerR;

	private bool handInTriggerL;

	private GorillaTriggerColliderHandIndicator handIndicatorR;

	private GorillaTriggerColliderHandIndicator handIndicatorL;

	private Vector3 startPosR;

	private Vector3 startPosL;

	private Vector3 lastFramePosR;

	private Vector3 lastFramePosL;

	private float distanceR;

	private float distanceL;

	private bool swipingUpL;

	private bool swipingUpR;

	private double cooldownEndL = double.MinValue;

	private double cooldownEndR = double.MinValue;

	private bool isCoolingDownL;

	private bool isCoolingDownR;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		_rig = GetComponentInParent<VRRig>();
		if (_rig == null && base.gameObject.GetComponentInParent<GTPlayer>() != null)
		{
			_rig = GorillaTagger.Instance.offlineVRRig;
		}
		isLocal = _rig != null && _rig.isLocal;
		col = GetComponent<Collider>();
		switch (localSwipeAxis)
		{
		case Axis.X:
			swipeDir = Vector3.right;
			break;
		case Axis.Y:
			swipeDir = Vector3.up;
			break;
		case Axis.Z:
			swipeDir = Vector3.forward;
			break;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!isLocal || !base.enabled)
		{
			return;
		}
		GorillaTriggerColliderHandIndicator component = other.GetComponent<GorillaTriggerColliderHandIndicator>();
		if (component != null)
		{
			if (component.isLeftHand)
			{
				handIndicatorL = component;
				Vector3 pos = base.transform.InverseTransformPoint(component.transform.position);
				ResetProgress(left: true, pos);
				handInTriggerL = true;
			}
			else
			{
				handIndicatorR = component;
				Vector3 pos2 = base.transform.InverseTransformPoint(component.transform.position);
				ResetProgress(left: false, pos2);
				handInTriggerR = true;
			}
		}
		if ((handInTriggerL || handInTriggerR) && !TickRunning)
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!isLocal || !base.enabled)
		{
			return;
		}
		GorillaTriggerColliderHandIndicator component = other.GetComponent<GorillaTriggerColliderHandIndicator>();
		if (component != null)
		{
			if (component.isLeftHand)
			{
				handInTriggerL = false;
				if (resetCooldownOnTriggerExit)
				{
					isCoolingDownL = false;
					cooldownEndL = double.MinValue;
				}
			}
			else
			{
				handInTriggerR = false;
				if (resetCooldownOnTriggerExit)
				{
					isCoolingDownR = false;
					cooldownEndR = double.MinValue;
				}
			}
		}
		if (!handInTriggerL && !handInTriggerR && TickRunning)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
	}

	public void Tick()
	{
		if (handInTriggerL)
		{
			ProcessHandMovement(handIndicatorL, startPosL, ref lastFramePosL, ref swipingUpL, ref distanceL, ref isCoolingDownL, ref cooldownEndL);
		}
		if (handInTriggerR)
		{
			ProcessHandMovement(handIndicatorR, startPosR, ref lastFramePosR, ref swipingUpR, ref distanceR, ref isCoolingDownR, ref cooldownEndR);
		}
		if (!handInTriggerL && !handInTriggerR && TickRunning)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
	}

	private void ResetProgress(bool left, Vector3 pos)
	{
		if (left)
		{
			startPosL = pos;
			lastFramePosL = startPosL;
			distanceL = 0f;
		}
		else
		{
			startPosR = pos;
			lastFramePosR = startPosR;
			distanceR = 0f;
		}
	}

	private void ProcessHandMovement(GorillaTriggerColliderHandIndicator hand, Vector3 start, ref Vector3 last, ref bool swipingUp, ref float dist, ref bool isCoolingDown, ref double cooldownEndTime)
	{
		if (isCoolingDown)
		{
			if (!(Time.timeAsDouble < cooldownEndTime))
			{
				isCoolingDown = false;
				cooldownEndTime = double.MinValue;
				ResetProgress(hand.isLeftHand, base.transform.InverseTransformPoint(hand.transform.position));
			}
			return;
		}
		Vector3 vector = base.transform.InverseTransformPoint(hand.transform.position);
		float num = Mathf.Abs(GetAxisComponent(hand.currentVelocity));
		if (num < minimumVelocity * _rig.scaleFactor || num > maximumVelocity * _rig.scaleFactor)
		{
			ResetProgress(hand.isLeftHand, vector);
			return;
		}
		float num2 = GetAxisComponent(vector) - GetAxisComponent(last);
		if (num2 >= 0f && !swipingUp)
		{
			swipingUp = true;
			ResetProgress(hand.isLeftHand, vector);
			return;
		}
		if ((num2 < 0f) & swipingUp)
		{
			swipingUp = false;
			ResetProgress(hand.isLeftHand, vector);
			return;
		}
		if ((GetLateralMovement(start) - GetLateralMovement(vector)).sqrMagnitude > lateralMovementTolerance * lateralMovementTolerance)
		{
			ResetProgress(hand.isLeftHand, vector);
			return;
		}
		last = vector;
		dist += Mathf.Abs(num2);
		GorillaTagger.Instance.StartVibration(hand.isLeftHand, swipeHaptics.Evaluate(dist / swipeDistance), Time.deltaTime);
		if (dist >= swipeDistance)
		{
			if (swipingUp)
			{
				OnSwipe?.Invoke(hand.isLeftHand);
				cooldownEndTime = Time.timeAsDouble + (double)swipeCooldown;
				isCoolingDown = true;
			}
			else
			{
				OnReverseSwipe?.Invoke(hand.isLeftHand);
				cooldownEndTime = Time.timeAsDouble + (double)swipeCooldown;
				isCoolingDown = true;
			}
			ResetProgress(hand.isLeftHand, vector);
		}
	}

	private float GetAxisComponent(Vector3 vec)
	{
		return localSwipeAxis switch
		{
			Axis.X => vec.x, 
			Axis.Y => vec.y, 
			_ => vec.z, 
		};
	}

	private Vector2 GetLateralMovement(Vector3 vec)
	{
		return localSwipeAxis switch
		{
			Axis.X => new Vector2(vec.y, vec.z), 
			Axis.Y => new Vector2(vec.x, vec.z), 
			_ => new Vector2(vec.x, vec.y), 
		};
	}
}
