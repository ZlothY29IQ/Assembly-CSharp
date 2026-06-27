using System;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class SprayCanCosmeticNetworked : MonoBehaviour
{
	[SerializeField]
	private TransferrableObject transferrableObject;

	private RubberDuckEvents _events;

	private CallLimiter callLimiter = new CallLimiter(10, 1f);

	public UnityEvent HandleOnShakeStart;

	public UnityEvent HandleOnShakeEnd;

	private void OnEnable()
	{
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((transferrableObject.myOnlineRig != null) ? transferrableObject.myOnlineRig.creator : ((transferrableObject.myRig != null) ? (transferrableObject.myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : null));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnShakeEvent);
		}
	}

	private void OnDisable()
	{
		if (_events != null)
		{
			if (_events.Activate != null)
			{
				_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnShakeEvent);
			}
			_events.Dispose();
			_events = null;
		}
	}

	private void OnShakeEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnShakeEvent");
		if (info.Sender != transferrableObject.myOnlineRig?.creator || !callLimiter.CheckCallTime(Time.time))
		{
			return;
		}
		object obj = args[0];
		if (obj is bool)
		{
			if ((bool)obj)
			{
				HandleOnShakeStart?.Invoke();
			}
			else
			{
				HandleOnShakeEnd?.Invoke();
			}
		}
	}

	public void OnShakeStart()
	{
		if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
		{
			_events.Activate.RaiseOthers(true);
		}
		HandleOnShakeStart?.Invoke();
	}

	public void OnShakeEnd()
	{
		if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
		{
			_events.Activate.RaiseOthers(false);
		}
		HandleOnShakeEnd?.Invoke();
	}
}
