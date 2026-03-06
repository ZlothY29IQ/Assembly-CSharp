using System;
using GorillaExtensions;
using GorillaTag;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
internal class GorillaSerializer : MonoBehaviour, IPunObservable, IPunInstantiateMagicCallback
{
	protected bool successfullInstantiate;

	protected IGorillaSerializeable serializeTarget;

	private Type targetType;

	protected GameObject targetObject;

	[SerializeField]
	protected PhotonView photonView;

	void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (successfullInstantiate && serializeTarget != null && ValidOnSerialize(stream, in info))
		{
			if (stream.IsReading)
			{
				serializeTarget.OnSerializeRead(stream, info);
			}
			else
			{
				serializeTarget.OnSerializeWrite(stream, info);
			}
		}
	}

	public virtual void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		if (photonView == null)
		{
			return;
		}
		successfullInstantiate = OnInstantiateSetup(info, out targetObject, out targetType);
		if (successfullInstantiate)
		{
			if (targetType != null && targetObject.IsNotNull() && targetObject.GetComponent(targetType) is IGorillaSerializeable gorillaSerializeable)
			{
				serializeTarget = gorillaSerializeable;
			}
			if (serializeTarget == null)
			{
				successfullInstantiate = false;
			}
		}
		if (successfullInstantiate)
		{
			OnSuccessfullInstantiate(info);
			return;
		}
		if (PhotonNetwork.InRoom && photonView.IsMine)
		{
			MonkeAgentCleanup.RegisterForDestroy(photonView);
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		photonView.ObservedComponents.Remove(this);
	}

	protected virtual void OnSuccessfullInstantiate(PhotonMessageInfo info)
	{
	}

	protected virtual bool OnInstantiateSetup(PhotonMessageInfo info, out GameObject outTargetObject, out Type outTargetType)
	{
		outTargetType = typeof(IGorillaSerializeable);
		outTargetObject = base.gameObject;
		return true;
	}

	protected virtual bool ValidOnSerialize(PhotonStream stream, in PhotonMessageInfo info)
	{
		if (info.Sender != info.photonView.Owner)
		{
			return false;
		}
		return true;
	}

	public virtual T AddRPCComponent<T>() where T : RPCNetworkBase
	{
		T result = base.gameObject.AddComponent<T>();
		photonView.RefreshRpcMonoBehaviourCache();
		return result;
	}

	public void SendRPC(string rpcName, bool targetOthers, params object[] data)
	{
		RpcTarget target = (targetOthers ? RpcTarget.Others : RpcTarget.MasterClient);
		photonView.RPC(rpcName, target, data);
	}

	public void SendRPC(string rpcName, Player targetPlayer, params object[] data)
	{
		photonView.RPC(rpcName, targetPlayer, data);
	}
}
