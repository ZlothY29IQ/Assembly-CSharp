using System;
using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PaperPlaneThrowable : TransferrableObject
{
	[Tooltip("Renderer on the body to disable when spawning the projectile")]
	[SerializeField]
	private Renderer _renderer;

	[Tooltip("Prefab in the Global object pool to spawn when throwing")]
	[SerializeField]
	private GameObject _projectilePrefab;

	[Tooltip("Minimum velocity of the hand required to launch the projectile")]
	[SerializeField]
	private float minThrowSpeed;

	private static Camera _playerView;

	private static PhotonEvent gLaunchRPC;

	private CallLimiterWithCooldown m_spamCheck = new CallLimiterWithCooldown(5f, 4, 1f);

	private static readonly int kProjectileEvent = StaticHash.Compute("PaperPlaneThrowable".GetStaticHash(), "LaunchProjectileLocal".GetStaticHash());

	private static object[] gEventArgs = new object[5];

	private static RaiseEventOptions gRaiseOpts;

	[SerializeField]
	private string _throwableID;

	private int? _throwableIdHash;

	[Space]
	private Vector3 _lastWorldPos;

	private Quaternion _lastWorldRot;

	[Space]
	private Vector3 _itemWorldVel;

	private Vector3 _itemWorldAngVel;

	private void OnLaunchRPC(int sender, int receiver, object[] args, PhotonMessageInfoWrapped info)
	{
		if (info.senderID != ownerRig.creator.ActorNumber)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnLaunchRPC");
		if (sender != receiver || !this)
		{
			return;
		}
		int num = FetchViewID(this);
		int num2 = (int)args[0];
		if (num == -1 || num2 == -1 || num != num2)
		{
			return;
		}
		int num3 = (int)args[1];
		int throwableId = GetThrowableId();
		if (num3 == throwableId)
		{
			Vector3 v = (Vector3)args[2];
			Quaternion q = (Quaternion)args[3];
			Vector3 v2 = (Vector3)args[4];
			if (v.IsValid(10000f) && q.IsValid() && v2.IsValid(10000f) && !_renderer.forceRenderingOff)
			{
				LaunchProjectileLocal(v, q, v2);
			}
		}
	}

	internal override void OnEnable()
	{
		PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
		_lastWorldPos = base.transform.position;
		_renderer.forceRenderingOff = false;
		base.OnEnable();
	}

	internal override void OnDisable()
	{
		PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
		base.OnDisable();
	}

	private void OnPhotonEvent(EventData evData)
	{
		if (evData.Code != 176)
		{
			return;
		}
		object[] array = (object[])evData.CustomData;
		if (!(array[0] is int num) || num != kProjectileEvent)
		{
			return;
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(evData.Sender);
		NetPlayer netPlayer = OwningPlayer();
		if (player != netPlayer)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(new PhotonMessageInfo(netPlayer.GetPlayerRef(), PhotonNetwork.ServerTimestamp, null), "OnPhotonEvent");
		if (!m_spamCheck.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		PositionState positionState = (PositionState)array[1];
		Vector3 v = (Vector3)array[2];
		Quaternion q = (Quaternion)array[3];
		Vector3 v2 = (Vector3)array[4];
		switch (positionState)
		{
		case PositionState.InLeftHand:
			if (InLeftHand())
			{
				break;
			}
			return;
		case PositionState.InRightHand:
			if (!InRightHand())
			{
				return;
			}
			break;
		}
		if (v.IsValid(10000f) && q.IsValid() && v2.IsValid(10000f) && !_renderer.forceRenderingOff && !base.myOnlineRig.IsNull() && base.myOnlineRig.IsPositionInRange(v, 4f))
		{
			LaunchProjectileLocal(v, q, v2);
		}
	}

	protected override void Start()
	{
		base.Start();
		if (_playerView == null)
		{
			_playerView = Camera.main;
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!_renderer.forceRenderingOff)
		{
			base.OnGrab(pointGrabbed, grabbingHand);
		}
	}

	private static int FetchViewID(PaperPlaneThrowable ppt)
	{
		NetPlayer netPlayer = ((ppt.myOnlineRig != null) ? ppt.myOnlineRig.creator : ((!(ppt.myRig != null)) ? null : ((ppt.myRig.creator != null) ? ppt.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
		if (netPlayer == null)
		{
			return -1;
		}
		if (VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig))
		{
			if (playerRig.Rig.netView == null)
			{
				return -1;
			}
			return playerRig.Rig.netView.ViewID;
		}
		return -1;
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		PositionState positionState = currentState;
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (VRRigCache.Instance.localRig.Rig != ownerRig)
		{
			return false;
		}
		if (_renderer.forceRenderingOff)
		{
			return false;
		}
		bool isLeftHand = releasingHand == EquipmentInteractor.instance.leftHand;
		GorillaVelocityTracker interactPointVelocityTracker = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeftHand);
		Vector3 vector = base.transform.TransformPoint(Vector3.zero);
		Quaternion rotation = base.transform.rotation;
		Vector3 averageVelocity = interactPointVelocityTracker.GetAverageVelocity(worldSpace: true);
		FetchViewID(this);
		GetThrowableId();
		LaunchProjectileLocal(vector, rotation, averageVelocity);
		if (gRaiseOpts == null)
		{
			gRaiseOpts = RaiseEventOptions.Default;
			gRaiseOpts.Receivers = ReceiverGroup.Others;
		}
		gEventArgs[0] = kProjectileEvent;
		gEventArgs[1] = positionState;
		gEventArgs[2] = vector;
		gEventArgs[3] = rotation;
		gEventArgs[4] = averageVelocity;
		PhotonNetwork.RaiseEvent(176, gEventArgs, gRaiseOpts, SendOptions.SendReliable);
		return true;
	}

	private int GetThrowableId()
	{
		int valueOrDefault = _throwableIdHash.GetValueOrDefault();
		if (!_throwableIdHash.HasValue)
		{
			valueOrDefault = StaticHash.Compute(_throwableID);
			_throwableIdHash = valueOrDefault;
			return valueOrDefault;
		}
		return valueOrDefault;
	}

	private void LaunchProjectileLocal(Vector3 launchPos, Quaternion launchRot, Vector3 releaseVel)
	{
		if (!(releaseVel.sqrMagnitude <= minThrowSpeed * base.transform.lossyScale.z * base.transform.lossyScale.z))
		{
			GameObject obj = ObjectPools.instance.Instantiate(_projectilePrefab.gameObject, launchPos);
			obj.transform.localScale = base.transform.lossyScale;
			PaperPlaneProjectile component = obj.GetComponent<PaperPlaneProjectile>();
			component.OnHit += OnProjectileHit;
			if (networkedStateEvents != SyncOptions.None)
			{
				int state = (int)(itemState & (ItemStates)(-65));
				component.SetTransferrableState(networkedStateEvents, state);
			}
			component.ResetProjectile();
			component.SetVRRig(base.myRig);
			component.Launch(launchPos, launchRot, releaseVel);
			_renderer.forceRenderingOff = true;
		}
	}

	private void OnProjectileHit(Vector3 endPoint)
	{
		_renderer.forceRenderingOff = false;
		if (IsLocalObject() && networkedStateEvents != SyncOptions.None && resetOnDocked)
		{
			switch (networkedStateEvents)
			{
			case SyncOptions.Bool:
				ResetStateBools();
				break;
			case SyncOptions.Int:
				SetItemStateInt(0);
				break;
			}
		}
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		Transform obj = base.transform;
		Vector3 position = obj.position;
		_itemWorldVel = (position - _lastWorldPos) / Time.deltaTime;
		Quaternion localRotation = obj.localRotation;
		_itemWorldAngVel = CalcAngularVelocity(_lastWorldRot, localRotation, Time.deltaTime);
		_lastWorldRot = localRotation;
		_lastWorldPos = position;
	}

	private static Vector3 CalcAngularVelocity(Quaternion from, Quaternion to, float dt)
	{
		Vector3 eulerAngles = (to * Quaternion.Inverse(from)).eulerAngles;
		if (eulerAngles.x > 180f)
		{
			eulerAngles.x -= 360f;
		}
		if (eulerAngles.y > 180f)
		{
			eulerAngles.y -= 360f;
		}
		if (eulerAngles.z > 180f)
		{
			eulerAngles.z -= 360f;
		}
		return eulerAngles * (MathF.PI / 180f / dt);
	}

	public override void DropItem()
	{
		base.DropItem();
	}
}
