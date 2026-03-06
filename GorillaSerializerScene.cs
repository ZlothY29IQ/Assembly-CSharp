using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

internal class GorillaSerializerScene : GorillaSerializer, IOnPhotonViewPreNetDestroy, IPhotonViewCallback
{
	[SerializeField]
	private bool transferrable;

	[SerializeField]
	private MonoBehaviour targetComponent;

	private IGorillaSerializeableScene sceneSerializeTarget;

	protected bool validDisable = true;

	internal bool HasAuthority => photonView.IsMine;

	protected virtual void Start()
	{
		if (targetComponent.IsNull() || !(targetComponent is IGorillaSerializeableScene gorillaSerializeableScene))
		{
			Debug.LogError("GorillaSerializerscene: missing target component or invalid target", base.gameObject);
			base.gameObject.SetActive(value: false);
			return;
		}
		gorillaSerializeableScene.OnSceneLinking(this);
		serializeTarget = gorillaSerializeableScene;
		sceneSerializeTarget = gorillaSerializeableScene;
		successfullInstantiate = true;
		photonView.AddCallbackTarget(this);
	}

	private void OnEnable()
	{
		if (successfullInstantiate)
		{
			if (!validDisable)
			{
				validDisable = true;
			}
			else
			{
				OnValidEnable();
			}
		}
	}

	protected virtual void OnValidEnable()
	{
		sceneSerializeTarget.OnNetworkObjectEnable();
	}

	private void OnDisable()
	{
		if (successfullInstantiate && validDisable)
		{
			OnValidDisable();
		}
	}

	protected virtual void OnValidDisable()
	{
		sceneSerializeTarget.OnNetworkObjectDisable();
	}

	public override void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		MonkeAgent.instance.SendReport("bad net obj creation", info.Sender.UserId, info.Sender.NickName);
		if (info.photonView.IsMine)
		{
			PhotonNetwork.Destroy(info.photonView);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	void IOnPhotonViewPreNetDestroy.OnPreNetDestroy(PhotonView rootView)
	{
		validDisable = false;
	}

	protected override bool ValidOnSerialize(PhotonStream stream, in PhotonMessageInfo info)
	{
		if (!transferrable)
		{
			return info.Sender == PhotonNetwork.MasterClient;
		}
		return base.ValidOnSerialize(stream, in info);
	}
}
