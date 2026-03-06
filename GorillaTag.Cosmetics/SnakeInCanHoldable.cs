using System;
using System.Collections;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class SnakeInCanHoldable : TransferrableObject
{
	[SerializeField]
	private float jumpSpeed;

	[SerializeField]
	private Transform stretchedPoint;

	[SerializeField]
	private Transform compressedPoint;

	[SerializeField]
	private GameObject topRigObject;

	[SerializeField]
	private GameObject disableObjectBeforeTrigger;

	private CallLimiter snakeInCanCallLimiter = new CallLimiter(10, 2f);

	private Vector3 topRigPosition;

	private Vector3 originalTopRigPosition;

	private RubberDuckEvents _events;

	protected override void Awake()
	{
		base.Awake();
		topRigPosition = topRigObject.transform.position;
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		disableObjectBeforeTrigger.SetActive(value: false);
		if (compressedPoint != null)
		{
			topRigObject.transform.position = compressedPoint.position;
		}
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((base.myOnlineRig != null) ? base.myOnlineRig.creator : ((!(base.myRig != null)) ? null : ((base.myRig.creator != null) ? base.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnEnableObject);
		}
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnEnableObject);
			_events.Dispose();
			_events = null;
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (VRRigCache.Instance.localRig.Rig != ownerRig)
		{
			return false;
		}
		if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
		{
			_events.Activate.RaiseOthers(false);
		}
		EnableObjectLocal(enable: false);
		return true;
	}

	private void OnEnableObject(int sender, int target, object[] arg, PhotonMessageInfoWrapped info)
	{
		if (info.senderID == ownerRig.creator.ActorNumber && arg.Length == 1 && arg[0] is bool && sender == target)
		{
			MonkeAgent.IncrementRPCCall(info, "OnEnableObject");
			if (snakeInCanCallLimiter.CheckCallTime(Time.time))
			{
				bool enable = (bool)arg[0];
				EnableObjectLocal(enable);
			}
		}
	}

	private void EnableObjectLocal(bool enable)
	{
		disableObjectBeforeTrigger.SetActive(enable);
		if (enable)
		{
			if (stretchedPoint != null)
			{
				StartCoroutine(SmoothTransition());
			}
			else
			{
				topRigObject.transform.position = topRigPosition;
			}
		}
		else if (compressedPoint != null)
		{
			topRigObject.transform.position = compressedPoint.position;
		}
	}

	private IEnumerator SmoothTransition()
	{
		while (Vector3.Distance(topRigObject.transform.position, stretchedPoint.position) > 0.01f)
		{
			topRigObject.transform.position = Vector3.MoveTowards(topRigObject.transform.position, stretchedPoint.position, jumpSpeed * Time.deltaTime);
			yield return null;
		}
		topRigObject.transform.position = stretchedPoint.position;
	}

	public void OnButtonPressed()
	{
		EnableObjectLocal(enable: true);
	}
}
