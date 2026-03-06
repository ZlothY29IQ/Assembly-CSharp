using System;
using GorillaExtensions;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class DiceHoldable : TransferrableObject
{
	[SerializeField]
	private DicePhysics dicePhysics;

	private RubberDuckEvents _events;

	internal override void OnEnable()
	{
		base.OnEnable();
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((base.myOnlineRig != null) ? base.myOnlineRig.creator : ((!(base.myRig != null)) ? null : ((base.myRig.creator != null) ? base.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
			else
			{
				Debug.LogError("Failed to get a reference to the Photon Player needed to hook up the cosmetic event");
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnDiceEvent);
		}
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnDiceEvent);
			UnityEngine.Object.Destroy(_events);
			_events = null;
		}
	}

	private void OnDiceEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "OnDiceEvent");
		if (sender == target && info.senderID == ownerRig.creator.ActorNumber)
		{
			if ((bool)args[0])
			{
				Vector3 v = base.transform.position;
				Vector3 v2 = base.transform.forward;
				v.SetValueSafe((Vector3)args[1]);
				v2.SetValueSafe((Vector3)args[2]);
				float playerScale = ((float)args[3]).ClampSafe(0.01f, 1f);
				int landingSide = Mathf.Clamp((int)args[4], 1, 20);
				double finite = ((double)args[5]).GetFinite();
				ThrowDiceLocal(v, v2, playerScale, landingSide, finite);
			}
			else
			{
				dicePhysics.EndThrow();
			}
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (dicePhysics.enabled)
		{
			if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				object[] args = new object[1] { false };
				_events.Activate.RaiseOthers(args);
			}
			base.transform.position = dicePhysics.transform.position;
			base.transform.rotation = dicePhysics.transform.rotation;
			dicePhysics.EndThrow();
			if (grabbingHand == EquipmentInteractor.instance.leftHand && currentState == PositionState.OnLeftArm)
			{
				canAutoGrabLeft = false;
				interpState = InterpolateState.None;
				currentState = PositionState.InLeftHand;
			}
			else if (grabbingHand == EquipmentInteractor.instance.rightHand && currentState == PositionState.OnRightArm)
			{
				canAutoGrabRight = false;
				interpState = InterpolateState.None;
				currentState = PositionState.InLeftHand;
			}
		}
		base.OnGrab(pointGrabbed, grabbingHand);
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (zoneReleased == null)
		{
			Vector3 position = base.transform.position;
			bool isLeftHand = releasingHand == EquipmentInteractor.instance.leftHand;
			Vector3 averageVelocity = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeftHand).GetAverageVelocity(worldSpace: true);
			int randomSide = dicePhysics.GetRandomSide();
			double num = (PhotonNetwork.InRoom ? PhotonNetwork.Time : (-1.0));
			float scale = GTPlayer.Instance.scale;
			if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				object[] args = new object[6] { true, position, averageVelocity, scale, randomSide, num };
				_events.Activate.RaiseOthers(args);
			}
			ThrowDiceLocal(position, averageVelocity, scale, randomSide, num);
		}
		return true;
	}

	private void ThrowDiceLocal(Vector3 startPosition, Vector3 throwVelocity, float playerScale, int landingSide, double startTime)
	{
		dicePhysics.StartThrow(this, startPosition, throwVelocity, playerScale, landingSide, startTime);
	}
}
