using System;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class FingerFlexEvent2 : MonoBehaviour, ITickSystemTick
{
	[Serializable]
	public class FlexEvent
	{
		public enum TriggerType
		{
			OnFlex = 0,
			OnRelease = 2
		}

		public enum FingerType
		{
			Thumb,
			Index,
			Middle,
			IndexAndMiddle,
			IndexOrMiddle
		}

		public enum HandType
		{
			TransferrableHeldHand,
			TransferrableEquippedSide,
			LeftHand,
			RightHand
		}

		private enum RangeState
		{
			Below,
			Within,
			Above
		}

		public TriggerType triggerType;

		public bool tryLink = true;

		[HideInInspector]
		public int linkIndex = -1;

		[Space]
		public FingerType fingerType = FingerType.Index;

		[Space]
		public HandType handType;

		private const string ADVANCED = "Advanced Properties";

		[Tooltip("When this is checked, all players in the room will fire the event. Otherwise, only the local player will fire it. You should usually leave this on, unless you're using it for something local like controller haptics.")]
		public bool networked = true;

		[Range(0.01f, 0.75f)]
		public float flexThreshold = 0.75f;

		[Range(0.01f, 1f)]
		public float releaseThreshold = 0.01f;

		public ContinuousPropertyArray continuousProperties;

		public UnityEvent<bool, float> unityEvent;

		[NonSerialized]
		public bool wasHeld;

		[NonSerialized]
		public bool marginError;

		private RangeState currentState;

		private RangeState lastState;

		private float lastThresholdTime = -100000f;

		public bool IsFlexTrigger => triggerType == TriggerType.OnFlex;

		public bool IsReleaseTrigger => triggerType == TriggerType.OnRelease;

		public bool UsesTransferrable
		{
			get
			{
				HandType handType = this.handType;
				return handType == HandType.TransferrableHeldHand || handType == HandType.TransferrableEquippedSide;
			}
		}

		public bool HasValidLink => linkIndex >= 0;

		public bool IsLinked
		{
			get
			{
				if (tryLink)
				{
					return linkIndex >= 0;
				}
				return false;
			}
		}

		private bool ShowMainProperties
		{
			get
			{
				if (IsLinked)
				{
					return IsFlexTrigger;
				}
				return true;
			}
		}

		private bool ShowFlexThreshold => ShowMainProperties;

		private bool ShowReleaseThreshold
		{
			get
			{
				if (!IsLinked || IsReleaseTrigger)
				{
					return !IsFlexTrigger;
				}
				return false;
			}
		}

		public void ProcessState(bool leftHand, float flexValue)
		{
			currentState = ((!(flexValue < releaseThreshold)) ? ((!(flexValue >= flexThreshold)) ? RangeState.Within : RangeState.Above) : RangeState.Below);
			if (ShowMainProperties && currentState != lastState && continuousProperties != null && continuousProperties.Count > 0)
			{
				float f = Mathf.InverseLerp(releaseThreshold, flexThreshold, flexValue);
				continuousProperties.ApplyAll(f);
			}
			if (currentState == RangeState.Above && lastState == RangeState.Below)
			{
				lastThresholdTime = Time.time;
				lastState = RangeState.Above;
				if (IsFlexTrigger)
				{
					unityEvent?.Invoke(leftHand, flexValue);
				}
			}
			else if (currentState == RangeState.Below && lastState == RangeState.Above)
			{
				lastThresholdTime = Time.time;
				lastState = RangeState.Below;
				if (IsReleaseTrigger)
				{
					unityEvent?.Invoke(leftHand, flexValue);
				}
			}
		}
	}

	public FlexEvent[] list;

	private VRRig myRig;

	private TransferrableObject myTransferrable;

	public bool TickRunning { get; set; }

	private bool TryLinkToNextEvent(int index)
	{
		if (index < list.Length - 1)
		{
			if (list[index].IsFlexTrigger && list[index + 1].IsReleaseTrigger)
			{
				list[index].linkIndex = index + 1;
				list[index + 1].linkIndex = index;
				return true;
			}
			list[index + 1].linkIndex = -1;
		}
		list[index].linkIndex = -1;
		return false;
	}

	private void Awake()
	{
		myRig = GetComponentInParent<VRRig>();
		myTransferrable = GetComponentInParent<TransferrableObject>();
		for (int i = 0; i < list.Length; i++)
		{
			FlexEvent flexEvent = list[i];
			if (myTransferrable.IsNull() && flexEvent.UsesTransferrable)
			{
				myTransferrable = GetComponentInParent<TransferrableObject>();
			}
			if (flexEvent.tryLink && TryLinkToNextEvent(i))
			{
				FlexEvent flexEvent2 = list[i + 1];
				flexEvent.releaseThreshold = flexEvent2.releaseThreshold;
				flexEvent2.flexThreshold = flexEvent.flexThreshold;
				flexEvent2.fingerType = flexEvent.fingerType;
				flexEvent2.handType = flexEvent.handType;
				flexEvent2.networked = flexEvent.networked;
				i++;
			}
		}
	}

	private void CalcFlex(bool disable)
	{
		float num2 = default(float);
		for (int i = 0; i < list.Length; i++)
		{
			FlexEvent flexEvent = list[i];
			if ((!flexEvent.networked && !myRig.isOfflineVRRig) || (flexEvent.UsesTransferrable && myTransferrable.IsNull()))
			{
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			switch (flexEvent.handType)
			{
			case FlexEvent.HandType.LeftHand:
				flag = true;
				break;
			case FlexEvent.HandType.RightHand:
				flag2 = true;
				break;
			case FlexEvent.HandType.TransferrableHeldHand:
				flag = myTransferrable.currentState == TransferrableObject.PositionState.InLeftHand;
				flag2 = myTransferrable.currentState == TransferrableObject.PositionState.InRightHand;
				flag3 = flag || flag2;
				break;
			case FlexEvent.HandType.TransferrableEquippedSide:
				flag = (myTransferrable.storedZone & (BodyDockPositions.DropPositions.LeftArm | BodyDockPositions.DropPositions.LeftBack)) != 0;
				flag2 = (myTransferrable.storedZone & (BodyDockPositions.DropPositions.RightArm | BodyDockPositions.DropPositions.RightBack)) != 0;
				break;
			}
			if ((flag && flag2) || (!flag && !flag2 && !flexEvent.wasHeld))
			{
				continue;
			}
			float num;
			if (disable || (flexEvent.wasHeld && !flag3))
			{
				num = 0f;
			}
			else
			{
				FlexEvent.FingerType fingerType = flexEvent.fingerType;
				switch (fingerType)
				{
				case FlexEvent.FingerType.Thumb:
					num2 = (flag ? myRig.leftThumb.calcT : myRig.rightThumb.calcT);
					break;
				case FlexEvent.FingerType.Index:
					num2 = (flag ? myRig.leftIndex.calcT : myRig.rightIndex.calcT);
					break;
				case FlexEvent.FingerType.Middle:
					num2 = (flag ? myRig.leftMiddle.calcT : myRig.rightMiddle.calcT);
					break;
				case FlexEvent.FingerType.IndexAndMiddle:
					num2 = (flag ? Mathf.Min(myRig.leftIndex.calcT, myRig.leftMiddle.calcT) : Mathf.Min(myRig.rightIndex.calcT, myRig.rightMiddle.calcT));
					break;
				case FlexEvent.FingerType.IndexOrMiddle:
					num2 = (flag ? Mathf.Max(myRig.leftIndex.calcT, myRig.leftMiddle.calcT) : Mathf.Max(myRig.rightIndex.calcT, myRig.rightMiddle.calcT));
					break;
				default:
					global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(fingerType);
					break;
				}
				num = num2;
			}
			float flexValue = num;
			flexEvent.ProcessState(flag, flexValue);
			flexEvent.wasHeld = flag3 && !disable;
			if (flexEvent.IsLinked)
			{
				FlexEvent obj = list[i + 1];
				obj.ProcessState(flag, flexValue);
				obj.wasHeld = flag3;
				i++;
			}
		}
	}

	public void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	public void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
		CalcFlex(disable: true);
	}

	public void Tick()
	{
		CalcFlex(disable: false);
	}
}
