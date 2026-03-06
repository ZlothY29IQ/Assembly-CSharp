using System;
using GorillaExtensions;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class DreidelHoldable : TransferrableObject
{
	[SerializeField]
	private Dreidel dreidelAnimation;

	private RubberDuckEvents _events;

	internal override void OnEnable()
	{
		base.OnEnable();
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((base.myOnlineRig != null) ? base.myOnlineRig.creator : ((base.myRig != null) ? (base.myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : null));
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
			_events.Activate.reliable = true;
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnDreidelSpin);
		}
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnDreidelSpin);
			_events.Dispose();
			_events = null;
		}
	}

	private void OnDreidelSpin(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "OnDreidelSpin");
		if (sender == target && info.senderID == ownerRig.creator.ActorNumber)
		{
			Vector3 v = (Vector3)args[0];
			Vector3 v2 = (Vector3)args[1];
			float num = (float)args[2];
			double num2 = (double)args[6];
			if (v.IsValid(10000f) && v2.IsValid(10000f) && float.IsFinite(num) && double.IsFinite(num2))
			{
				bool counterClockwise = (bool)args[3];
				Dreidel.Side side = (Dreidel.Side)args[4];
				Dreidel.Variation variation = (Dreidel.Variation)args[5];
				StartSpinLocal(v, v2, num, counterClockwise, side, variation, num2);
			}
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		base.OnGrab(pointGrabbed, grabbingHand);
		if (dreidelAnimation != null)
		{
			dreidelAnimation.TryCheckForSurfaces();
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (dreidelAnimation != null)
		{
			dreidelAnimation.TrySetIdle();
		}
		return true;
	}

	public override void OnActivate()
	{
		base.OnActivate();
		if (dreidelAnimation != null && dreidelAnimation.TryGetSpinStartData(out var surfacePoint, out var surfaceNormal, out var randomDuration, out var randomSide, out var randomVariation, out var startTime))
		{
			bool flag = currentState == PositionState.InLeftHand;
			if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				object[] args = new object[7]
				{
					surfacePoint,
					surfaceNormal,
					randomDuration,
					flag,
					(int)randomSide,
					(int)randomVariation,
					startTime
				};
				_events.Activate.RaiseAll(args);
			}
			else
			{
				StartSpinLocal(surfacePoint, surfaceNormal, randomDuration, flag, randomSide, randomVariation, startTime);
			}
		}
	}

	private void StartSpinLocal(Vector3 surfacePoint, Vector3 surfaceNormal, float duration, bool counterClockwise, Dreidel.Side side, Dreidel.Variation variation, double startTime)
	{
		if (dreidelAnimation != null)
		{
			dreidelAnimation.SetSpinStartData(surfacePoint, surfaceNormal, duration, counterClockwise, side, variation, startTime);
			dreidelAnimation.Spin();
		}
	}

	public void DebugSpinDreidel()
	{
		Transform transform = GTPlayer.Instance.headCollider.transform;
		Vector3 origin = transform.position + transform.forward * 0.5f;
		float maxDistance = 2f;
		if (Physics.Raycast(origin, Vector3.down, out var hitInfo, maxDistance, GTPlayer.Instance.locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
		{
			Vector3 point = hitInfo.point;
			Vector3 normal = hitInfo.normal;
			float num = UnityEngine.Random.Range(7f, 10f);
			Dreidel.Side side = (Dreidel.Side)UnityEngine.Random.Range(0, 4);
			Dreidel.Variation variation = (Dreidel.Variation)UnityEngine.Random.Range(0, 5);
			bool flag = currentState == PositionState.InLeftHand;
			double num2 = (PhotonNetwork.InRoom ? PhotonNetwork.Time : (-1.0));
			if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				object[] args = new object[7]
				{
					point,
					normal,
					num,
					flag,
					(int)side,
					(int)variation,
					num2
				};
				_events.Activate.RaiseAll(args);
			}
			else
			{
				StartSpinLocal(point, normal, num, flag, side, variation, num2);
			}
		}
	}
}
