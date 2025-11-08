using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CrittersStickyTrap : CrittersToolThrowable
{
	[Header("Sticky Trap")]
	public bool stickOnImpact = true;

	public int subStickyGooIndex = -1;

	private bool isStuck;

	public override void Initialize()
	{
		base.Initialize();
		TogglePhysics(!isStuck);
	}

	public override void OnDisable()
	{
		base.OnDisable();
		isStuck = false;
	}

	public override void SetImpulse()
	{
		if (!isOnPlayer && !isSceneActor)
		{
			localLastImpulse = lastImpulseTime;
			MoveActor(lastImpulsePosition, lastImpulseQuaternion, parentActorId >= 0, updateImpulses: false);
			TogglePhysics(usesRB && parentActorId == -1 && !isStuck);
			if (!rb.isKinematic)
			{
				rb.linearVelocity = lastImpulseVelocity;
				rb.angularVelocity = lastImpulseAngularVelocity;
			}
		}
	}

	protected override void OnImpact(Vector3 hitPosition, Vector3 hitNormal)
	{
		if (CrittersManager.instance.LocalAuthority())
		{
			if (stickOnImpact)
			{
				rb.isKinematic = true;
				isStuck = true;
				updatedSinceLastFrame = true;
				UpdateImpulses(local: false, updateTime: true);
			}
			CrittersStickyGoo crittersStickyGoo = (CrittersStickyGoo)CrittersManager.instance.SpawnActor(CrittersActorType.StickyGoo, subStickyGooIndex);
			if (!(crittersStickyGoo == null))
			{
				CrittersManager.instance.TriggerEvent(CrittersManager.CritterEvent.StickyDeployed, actorId, base.transform.position, Quaternion.LookRotation(hitNormal));
				Vector3 forward = base.transform.forward;
				forward -= hitNormal * Vector3.Dot(hitNormal, forward);
				crittersStickyGoo.MoveActor(hitPosition, Quaternion.LookRotation(forward, hitNormal));
				crittersStickyGoo.SetImpulseVelocity(Vector3.zero, Vector3.zero);
				UpdateImpulses(local: true);
			}
		}
	}

	protected override void OnImpactCritter(CrittersPawn impactedCritter)
	{
		OnImpact(impactedCritter.transform.position, impactedCritter.transform.up);
	}

	protected override void OnPickedUp()
	{
		if (isStuck)
		{
			isStuck = false;
			updatedSinceLastFrame = true;
		}
	}

	public override void SendDataByCrittersActorType(PhotonStream stream)
	{
		base.SendDataByCrittersActorType(stream);
		stream.SendNext(isStuck);
	}

	public override bool UpdateSpecificActor(PhotonStream stream)
	{
		if (!(base.UpdateSpecificActor(stream) & CrittersManager.ValidateDataType<bool>(stream.ReceiveNext(), out var dataAsType)))
		{
			return false;
		}
		isStuck = dataAsType;
		TogglePhysics(!isStuck);
		return true;
	}

	public override int AddActorDataToList(ref List<object> objList)
	{
		base.AddActorDataToList(ref objList);
		objList.Add(isStuck);
		return TotalActorDataLength();
	}

	public override int TotalActorDataLength()
	{
		return BaseActorDataLength() + 1;
	}

	public override int UpdateFromRPC(object[] data, int startingIndex)
	{
		startingIndex += base.UpdateFromRPC(data, startingIndex);
		if (!CrittersManager.ValidateDataType<bool>(data[startingIndex], out var dataAsType))
		{
			return TotalActorDataLength();
		}
		isStuck = dataAsType;
		TogglePhysics(!isStuck);
		return TotalActorDataLength();
	}
}
