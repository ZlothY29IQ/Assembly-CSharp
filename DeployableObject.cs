using System;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

public class DeployableObject : TransferrableObject
{
	[SerializeField]
	private GameObject _objectToDeploy;

	[SerializeField]
	private DeployedChild _child;

	[SerializeField]
	private GameObject[] _disabledWhileDeployed = new GameObject[0];

	[SerializeField]
	private SoundBankPlayer deploySound;

	[SerializeField]
	private PhotonSignal<long, int, long> _deploySignal = "_deploySignal";

	[SerializeField]
	private float _maxDeployDistance = 4f;

	[SerializeField]
	private float _maxThrowVelocity = 50f;

	[SerializeField]
	private UnityEvent _onDeploy;

	[SerializeField]
	private UnityEvent _onReturn;

	[SerializeField]
	private Component[] _rigAwareObjects = new Component[0];

	[SerializeField]
	private CallLimiter m_spamChecker = new CallLimiter(2, 1f);

	private VRRig m_VRRig;

	protected override void Awake()
	{
		_deploySignal.OnSignal += DeployRPC;
		base.Awake();
	}

	internal override void OnEnable()
	{
		_deploySignal.Enable();
		VRRig componentInParent = GetComponentInParent<VRRig>();
		for (int i = 0; i < _rigAwareObjects.Length; i++)
		{
			if (_rigAwareObjects[i] is IRigAware rigAware)
			{
				rigAware.SetRig(componentInParent);
			}
		}
		m_VRRig = componentInParent;
		m_VRRig.rigContainer.RigEvents.disableEvent.Add(new Action<RigContainer>(OnRigPreDisable));
		base.OnEnable();
		if (base.gameObject.activeInHierarchy)
		{
			itemState &= (ItemStates)(-2);
		}
	}

	internal override void OnDisable()
	{
		m_VRRig = null;
		_deploySignal.Disable();
		if (_objectToDeploy.activeSelf)
		{
			ReturnChild();
		}
		base.OnDisable();
	}

	private void OnRigPreDisable(RigContainer rc)
	{
		m_spamChecker.Reset();
		rc.RigEvents.disableEvent.Remove(new Action<RigContainer>(OnRigPreDisable));
	}

	protected override void OnDestroy()
	{
		_deploySignal.Dispose();
		base.OnDestroy();
	}

	protected override void LateUpdateReplicated()
	{
		base.LateUpdateReplicated();
		if (itemState.HasFlag(ItemStates.State0))
		{
			if (!_objectToDeploy.activeSelf)
			{
				DeployChild();
			}
		}
		else if (_objectToDeploy.activeSelf)
		{
			ReturnChild();
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (VRRig.LocalRig != ownerRig)
		{
			return false;
		}
		bool isLeftHand = releasingHand == EquipmentInteractor.instance.leftHand;
		GorillaVelocityTracker interactPointVelocityTracker = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeftHand);
		Transform obj = base.transform;
		Vector3 vector = obj.TransformPoint(Vector3.zero);
		Quaternion rotation = obj.rotation;
		Vector3 averageVelocity = interactPointVelocityTracker.GetAverageVelocity(worldSpace: true);
		DeployLocal(vector, rotation, averageVelocity);
		_deploySignal.Raise(ReceiverGroup.Others, BitPackUtils.PackWorldPosForNetwork(vector), BitPackUtils.PackQuaternionForNetwork(rotation), BitPackUtils.PackWorldPosForNetwork(averageVelocity * 100f));
		return true;
	}

	protected virtual void DeployLocal(Vector3 launchPos, Quaternion launchRot, Vector3 releaseVel, bool isRemote = false)
	{
		DisableWhileDeployed(active: true);
		_child.Deploy(this, launchPos, launchRot, releaseVel, isRemote);
	}

	private void DeployRPC(long packedPos, int packedRot, long packedVel, PhotonSignalInfo info)
	{
		if (info.sender != OwningPlayer())
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "DeployRPC");
		if (m_spamChecker.CheckCallTime(Time.unscaledTime))
		{
			Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork(packedPos);
			Quaternion q = BitPackUtils.UnpackQuaternionFromNetwork(packedRot);
			Vector3 inVel = BitPackUtils.UnpackWorldPosFromNetwork(packedVel) / 100f;
			if (v.IsValid(10000f) && q.IsValid() && m_VRRig.IsPositionInRange(v, _maxDeployDistance))
			{
				DeployLocal(v, q, m_VRRig.ClampVelocityRelativeToPlayerSafe(inVel, _maxThrowVelocity), isRemote: true);
			}
		}
	}

	private void DisableWhileDeployed(bool active)
	{
		if (!_disabledWhileDeployed.IsNullOrEmpty())
		{
			for (int i = 0; i < _disabledWhileDeployed.Length; i++)
			{
				_disabledWhileDeployed[i].SetActive(!active);
			}
		}
	}

	public void DeployChild()
	{
		itemState |= ItemStates.State0;
		_objectToDeploy.SetActive(value: true);
		DisableWhileDeployed(active: true);
		_onDeploy?.Invoke();
	}

	public void ReturnChild()
	{
		itemState &= (ItemStates)(-2);
		_objectToDeploy.SetActive(value: false);
		DisableWhileDeployed(active: false);
		_onReturn?.Invoke();
	}
}
