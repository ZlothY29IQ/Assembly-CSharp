using System.Diagnostics;
using Photon.Pun;
using UnityEngine;

public class Tappable : MonoBehaviour
{
	public int tappableId;

	public string staticId;

	public bool useStaticId;

	[Tooltip("If true, tap cooldown will be ignored.  Tapping will be allowed/disallowed based on result of CanTap()")]
	public bool overrideTapCooldown;

	[Space]
	public TappableManager manager;

	public RpcTarget rpcTarget;

	public void Validate()
	{
		CalculateId(force: true);
	}

	protected virtual void OnEnable()
	{
		if (!useStaticId)
		{
			CalculateId();
		}
		TappableManager.Register(this);
	}

	protected virtual void OnDisable()
	{
		TappableManager.Unregister(this);
	}

	public virtual bool CanTap(bool isLeftHand)
	{
		return true;
	}

	public void OnTap()
	{
		OnTap(1f);
	}

	public void OnTap(float tapStrength)
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			OnTapLocal(tapStrength, Time.time, default(PhotonMessageInfoWrapped));
		}
		else if ((bool)manager)
		{
			manager.photonView.RPC("SendOnTapRPC", RpcTarget.All, tappableId, tapStrength);
		}
	}

	public void OnGrab()
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			OnGrabLocal(Time.time, default(PhotonMessageInfoWrapped));
		}
		else if ((bool)manager)
		{
			manager.photonView.RPC("SendOnGrabRPC", RpcTarget.All, tappableId);
		}
	}

	public void OnRelease()
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			OnReleaseLocal(Time.time, default(PhotonMessageInfoWrapped));
		}
		else if ((bool)manager)
		{
			manager.photonView.RPC("SendOnReleaseRPC", RpcTarget.All, tappableId);
		}
	}

	public virtual void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped sender)
	{
	}

	public virtual void OnGrabLocal(float tapTime, PhotonMessageInfoWrapped sender)
	{
	}

	public virtual void OnReleaseLocal(float tapTime, PhotonMessageInfoWrapped sender)
	{
	}

	private void EdRecalculateId()
	{
		CalculateId(force: true);
	}

	private void CalculateId(bool force = false)
	{
		Transform transform = base.transform;
		int hashCode = TransformUtils.ComputePathHash(transform).ToId128().GetHashCode();
		int staticHash = GetType().Name.GetStaticHash();
		int hashCode2 = transform.position.QuantizedId128().GetHashCode();
		int num = StaticHash.Compute(hashCode, staticHash, hashCode2);
		if (useStaticId)
		{
			if (string.IsNullOrEmpty(staticId) || force)
			{
				int instanceID = transform.GetInstanceID();
				int num2 = StaticHash.Compute(num, instanceID);
				staticId = $"#ID_{num2:X8}";
			}
			tappableId = staticId.GetStaticHash();
		}
		else
		{
			tappableId = (Application.isPlaying ? num : 0);
		}
	}

	[Conditional("UNITY_EDITOR")]
	private void OnValidate()
	{
		CalculateId();
	}
}
