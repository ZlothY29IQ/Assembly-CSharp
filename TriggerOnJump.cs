using System;
using GorillaExtensions;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class TriggerOnJump : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private float minJumpStrength = 1f;

	[SerializeField]
	private float minJumpVertical = 1f;

	[SerializeField]
	private float cooldownTime = 1f;

	[SerializeField]
	private UnityEvent onJumping;

	private RubberDuckEvents _events;

	private bool playerOnGround;

	private float minJumpTime = 0.05f;

	private bool waitingForGrounding;

	private float jumpStartTime;

	private float lastActivationTime;

	private VRRig myRig;

	public bool TickRunning { get; set; }

	private void OnEnable()
	{
		if (myRig.IsNull())
		{
			myRig = GetComponentInParent<VRRig>();
		}
		if (_events == null && myRig != null && myRig.Creator != null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			_events.Init(myRig.creator);
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnActivate);
		}
		bool num = !PhotonNetwork.InRoom && myRig != null && myRig.isOfflineVRRig;
		RigContainer playerRig;
		bool flag = PhotonNetwork.InRoom && myRig != null && VRRigCache.Instance.TryGetVrrig(PhotonNetwork.LocalPlayer, out playerRig) && playerRig != null && playerRig.Rig != null && playerRig.Rig == myRig;
		if (num || flag)
		{
			TickSystem<object>.AddCallbackTarget(this);
		}
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveCallbackTarget(this);
		playerOnGround = false;
		jumpStartTime = 0f;
		lastActivationTime = 0f;
		waitingForGrounding = false;
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnActivate);
			UnityEngine.Object.Destroy(_events);
			_events = null;
		}
	}

	private void OnActivate(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "OnJumpActivate");
		if (info.senderID == myRig.creator.ActorNumber && sender == target)
		{
			onJumping.Invoke();
		}
	}

	public void Tick()
	{
		GTPlayer instance = GTPlayer.Instance;
		if (!(instance != null))
		{
			return;
		}
		bool flag = playerOnGround;
		playerOnGround = instance.BodyOnGround || instance.IsHandTouching(isLeftHand: true) || instance.IsHandTouching(isLeftHand: false);
		float time = Time.time;
		if (playerOnGround)
		{
			waitingForGrounding = false;
		}
		if (!playerOnGround && flag)
		{
			jumpStartTime = time;
		}
		if (playerOnGround || waitingForGrounding || !(instance.RigidbodyVelocity.sqrMagnitude > minJumpStrength * minJumpStrength) || !(instance.RigidbodyVelocity.y > minJumpVertical) || !(time > jumpStartTime + minJumpTime))
		{
			return;
		}
		waitingForGrounding = true;
		if (time > lastActivationTime + cooldownTime)
		{
			lastActivationTime = time;
			if (PhotonNetwork.InRoom)
			{
				_events.Activate.RaiseAll();
			}
			else
			{
				onJumping.Invoke();
			}
		}
	}
}
