using System;
using GorillaExtensions;
using GorillaLocomotion.Climbing;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class ChickenSword : MonoBehaviour
{
	private enum SwordState
	{
		Ready,
		Deflated
	}

	[SerializeField]
	private float rechargeCooldown;

	[SerializeField]
	private GorillaVelocityTracker velocityTracker;

	[SerializeField]
	private float hitVelocityThreshold;

	[SerializeField]
	private TransferrableObject transferrableObject;

	[SerializeField]
	private CosmeticSwapper cosmeticSwapper;

	[Space]
	[Space]
	public UnityEvent OnDeflatedShared;

	public UnityEvent<bool> OnDeflatedLocal;

	public UnityEvent OnRechargedShared;

	public UnityEvent<bool> OnRechargedLocal;

	public UnityEvent<VRRig> OnHitTargetShared;

	public UnityEvent<bool> OnHitTargetLocal;

	public UnityEvent<VRRig> OnReachedLastTransformationStepShared;

	private float lastHitTime;

	private SwordState currentState;

	private bool hitReceievd;

	private RubberDuckEvents _events;

	private CallLimiter callLimiter = new CallLimiter(10, 2f);

	private void Awake()
	{
		lastHitTime = float.PositiveInfinity;
		SwitchState(SwordState.Ready);
	}

	internal void OnEnable()
	{
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((transferrableObject.myOnlineRig != null) ? transferrableObject.myOnlineRig.creator : ((!(transferrableObject.myRig != null)) ? null : ((transferrableObject.myRig.creator != null) ? transferrableObject.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
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
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnReachedLastTransformationStep);
		}
	}

	private void OnDisable()
	{
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnReachedLastTransformationStep);
			_events.Dispose();
			_events = null;
		}
	}

	private void Update()
	{
		switch (currentState)
		{
		case SwordState.Ready:
			if (hitReceievd)
			{
				hitReceievd = false;
				lastHitTime = Time.time;
				SwitchState(SwordState.Deflated);
				OnDeflatedShared?.Invoke();
				if ((bool)transferrableObject && transferrableObject.IsMyItem())
				{
					OnDeflatedLocal?.Invoke(transferrableObject.InLeftHand());
				}
			}
			break;
		case SwordState.Deflated:
			if (Time.time - lastHitTime > rechargeCooldown)
			{
				lastHitTime = float.PositiveInfinity;
				SwitchState(SwordState.Ready);
				OnRechargedShared?.Invoke();
				if ((bool)transferrableObject && transferrableObject.IsMyItem())
				{
					OnRechargedLocal?.Invoke(transferrableObject.InLeftHand());
				}
			}
			break;
		}
	}

	public void OnHitTargetSync(VRRig playerRig)
	{
		if (velocityTracker == null)
		{
			return;
		}
		Vector3 averageVelocity = velocityTracker.GetAverageVelocity(worldSpace: true);
		if (currentState == SwordState.Ready && averageVelocity.magnitude > hitVelocityThreshold)
		{
			hitReceievd = true;
			OnHitTargetShared?.Invoke(playerRig);
			if ((bool)transferrableObject && transferrableObject.IsMyItem())
			{
				bool arg = transferrableObject.InLeftHand();
				OnHitTargetLocal?.Invoke(arg);
			}
			if (cosmeticSwapper != null && playerRig == GorillaTagger.Instance.offlineVRRig && cosmeticSwapper.GetCurrentStepIndex(playerRig) >= cosmeticSwapper.GetNumberOfSteps() && PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				_events.Activate.RaiseAll();
			}
		}
	}

	private void OnReachedLastTransformationStep(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender == target)
		{
			MonkeAgent.IncrementRPCCall(info, "OnReachedLastTransformationStep");
			if (callLimiter.CheckCallTime(Time.time) && VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender.ActorNumber), out var playerRig) && playerRig.Rig.IsPositionInRange(base.transform.position, 6f))
			{
				OnReachedLastTransformationStepShared?.Invoke(playerRig.Rig);
			}
		}
	}

	private void SwitchState(SwordState newState)
	{
		currentState = newState;
	}
}
