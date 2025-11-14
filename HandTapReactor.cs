using System;
using TagEffects;
using UnityEngine;

public class HandTapReactor : MonoBehaviour
{
	[Flags]
	private enum TapType
	{
		None = 0,
		LeftDown = 1,
		LeftUp = 2,
		LeftHighFive = 4,
		LeftFistBump = 8,
		LeftTagFirstPerson = 0x10,
		LeftTagThirdPerson = 0x20,
		AllLeft = 0x3F,
		RightDown = 0x40,
		RightUp = 0x80,
		RightHighFive = 0x100,
		RightFistBump = 0x200,
		RightTagFirstPerson = 0x400,
		RightTagThirdPerson = 0x800,
		AllRight = 0xFC0,
		All = -1
	}

	[SerializeField]
	private FlagEvents<TapType> handTapEvents;

	private VRRig myRig;

	private IHandEffectsTrigger leftHandTrigger;

	private IHandEffectsTrigger rightHandTrigger;

	private void LeftDown(HandEffectContext ctx)
	{
		handTapEvents.InvokeAll(TapType.LeftDown);
	}

	private void LeftUp(HandEffectContext ctx)
	{
		handTapEvents.InvokeAll(TapType.LeftUp);
	}

	private void LeftGesture(IHandEffectsTrigger.Mode mode)
	{
		FlagEvents<TapType> flagEvents = handTapEvents;
		TapType test = default(TapType);
		switch (mode)
		{
		case IHandEffectsTrigger.Mode.HighFive:
			test = TapType.LeftHighFive;
			break;
		case IHandEffectsTrigger.Mode.FistBump:
			test = TapType.LeftFistBump;
			break;
		case IHandEffectsTrigger.Mode.Tag1P:
			test = TapType.LeftTagFirstPerson;
			break;
		case IHandEffectsTrigger.Mode.Tag3P:
			test = TapType.LeftTagThirdPerson;
			break;
		default:
			global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(mode);
			break;
		}
		flagEvents.InvokeAll(test);
	}

	private void RightDown(HandEffectContext ctx)
	{
		handTapEvents.InvokeAll(TapType.RightDown);
	}

	private void RightUp(HandEffectContext ctx)
	{
		handTapEvents.InvokeAll(TapType.RightUp);
	}

	private void RightGesture(IHandEffectsTrigger.Mode mode)
	{
		FlagEvents<TapType> flagEvents = handTapEvents;
		TapType test = default(TapType);
		switch (mode)
		{
		case IHandEffectsTrigger.Mode.HighFive:
			test = TapType.RightHighFive;
			break;
		case IHandEffectsTrigger.Mode.FistBump:
			test = TapType.RightFistBump;
			break;
		case IHandEffectsTrigger.Mode.Tag1P:
			test = TapType.RightTagFirstPerson;
			break;
		case IHandEffectsTrigger.Mode.Tag3P:
			test = TapType.RightTagThirdPerson;
			break;
		default:
			global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(mode);
			break;
		}
		flagEvents.InvokeAll(test);
	}

	private void OnEnable()
	{
		if (myRig == null)
		{
			myRig = GetComponentInParent<VRRig>();
			IHandEffectsTrigger[] componentsInChildren = myRig.GetComponentsInChildren<IHandEffectsTrigger>();
			if (componentsInChildren[0].RightHand)
			{
				rightHandTrigger = componentsInChildren[0];
				leftHandTrigger = componentsInChildren[1];
			}
			else
			{
				rightHandTrigger = componentsInChildren[1];
				leftHandTrigger = componentsInChildren[0];
			}
		}
		if (myRig != null)
		{
			myRig.LeftHandEffect.handTapDown += LeftDown;
			myRig.LeftHandEffect.handTapUp += LeftUp;
			IHandEffectsTrigger handEffectsTrigger = leftHandTrigger;
			handEffectsTrigger.OnTrigger = (Action<IHandEffectsTrigger.Mode>)Delegate.Combine(handEffectsTrigger.OnTrigger, new Action<IHandEffectsTrigger.Mode>(LeftGesture));
			myRig.RightHandEffect.handTapDown += RightDown;
			myRig.RightHandEffect.handTapUp += RightUp;
			IHandEffectsTrigger handEffectsTrigger2 = rightHandTrigger;
			handEffectsTrigger2.OnTrigger = (Action<IHandEffectsTrigger.Mode>)Delegate.Combine(handEffectsTrigger2.OnTrigger, new Action<IHandEffectsTrigger.Mode>(RightGesture));
		}
	}

	private void OnDisable()
	{
		if (myRig != null)
		{
			myRig.LeftHandEffect.handTapDown -= LeftDown;
			myRig.LeftHandEffect.handTapUp -= LeftUp;
			IHandEffectsTrigger handEffectsTrigger = leftHandTrigger;
			handEffectsTrigger.OnTrigger = (Action<IHandEffectsTrigger.Mode>)Delegate.Remove(handEffectsTrigger.OnTrigger, new Action<IHandEffectsTrigger.Mode>(LeftGesture));
			myRig.RightHandEffect.handTapDown -= RightDown;
			myRig.RightHandEffect.handTapUp -= RightUp;
			IHandEffectsTrigger handEffectsTrigger2 = rightHandTrigger;
			handEffectsTrigger2.OnTrigger = (Action<IHandEffectsTrigger.Mode>)Delegate.Remove(handEffectsTrigger2.OnTrigger, new Action<IHandEffectsTrigger.Mode>(RightGesture));
		}
	}
}
