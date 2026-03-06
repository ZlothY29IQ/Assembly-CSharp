using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class RCCosmeticNetworkSync : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback
{
	public struct SyncedState
	{
		public byte state;

		public Vector3 position;

		public Quaternion rotation;

		public byte dataA;

		public byte dataB;

		public byte dataC;
	}

	public SyncedState syncedState;

	private RCRemoteHoldable rcRemote;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		if (info.Sender == null)
		{
			DestroyThis();
			return;
		}
		if (info.Sender != base.photonView.Owner || base.photonView.IsRoomView)
		{
			MonkeAgent.instance.SendReport("spoofed rc instantiate", info.Sender.UserId, info.Sender.NickName);
			DestroyThis();
			return;
		}
		object[] instantiationData = info.photonView.InstantiationData;
		if (instantiationData == null || instantiationData.Length < 1 || !(instantiationData[0] is int num))
		{
			DestroyThis();
			return;
		}
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender.ActorNumber), out var playerRig) && num > -1 && num < playerRig.Rig.myBodyDockPositions.allObjects.Length)
		{
			rcRemote = playerRig.Rig.myBodyDockPositions.allObjects[num] as RCRemoteHoldable;
			if (rcRemote != null)
			{
				rcRemote.networkSync = this;
				rcRemote.WakeUpRemoteVehicle();
			}
		}
		if (rcRemote == null)
		{
			DestroyThis();
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender != base.photonView.Owner)
		{
			return;
		}
		if (stream.IsWriting)
		{
			stream.SendNext(syncedState.state);
			stream.SendNext(syncedState.position);
			stream.SendNext((int)BitPackUtils.PackRotation(syncedState.rotation));
			stream.SendNext(syncedState.dataA);
			stream.SendNext(syncedState.dataB);
			stream.SendNext(syncedState.dataC);
		}
		else if (stream.IsReading)
		{
			byte state = syncedState.state;
			syncedState.state = (byte)stream.ReceiveNext();
			syncedState.position.SetValueSafe((Vector3)stream.ReceiveNext());
			syncedState.rotation.SetValueSafe(BitPackUtils.UnpackRotation((uint)(int)stream.ReceiveNext()));
			syncedState.dataA = (byte)stream.ReceiveNext();
			syncedState.dataB = (byte)stream.ReceiveNext();
			syncedState.dataC = (byte)stream.ReceiveNext();
			if (state != syncedState.state && rcRemote != null && rcRemote.Vehicle != null && !rcRemote.Vehicle.enabled)
			{
				rcRemote.WakeUpRemoteVehicle();
			}
		}
	}

	[PunRPC]
	public void HitRCVehicleRPC(Vector3 hitVelocity, bool isProjectile, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "HitRCVehicleRPC");
		if (!hitVelocity.IsValid(10000f))
		{
			MonkeAgent.instance.SendReport("nan rc hit", info.Sender.UserId, info.Sender.NickName);
		}
		else if (rcRemote != null && rcRemote.Vehicle != null)
		{
			rcRemote.Vehicle.AuthorityApplyImpact(hitVelocity, isProjectile);
		}
	}

	private void DestroyThis()
	{
		if (base.photonView.IsMine)
		{
			PhotonNetwork.Destroy(base.gameObject);
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
