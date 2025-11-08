using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CrittersCage : CrittersActor
{
	public Transform grabPosition;

	public Transform cagePosition;

	public float grabDistance;

	[SerializeField]
	private Vector3[] critterScales = new Vector3[1] { Vector3.one };

	[SerializeField]
	private float releaseCooldown = 0.25f;

	[SerializeField]
	private AudioSource sound;

	[SerializeField]
	private AudioClip openSound;

	[SerializeField]
	private AudioClip closeSound;

	public GameObject lid;

	[NonSerialized]
	public bool heldByPlayer;

	[NonSerialized]
	private bool hasCritter;

	[NonSerialized]
	public bool inReleasingPosition;

	private float _releaseCooldownEnd;

	private bool _lidActive;

	public Vector3 critterScale
	{
		get
		{
			if (subObjectIndex < critterScales.Length && subObjectIndex >= 0)
			{
				return critterScales[subObjectIndex];
			}
			return Vector3.one;
		}
	}

	public bool CanCatch
	{
		get
		{
			if (heldByPlayer && !hasCritter && !inReleasingPosition)
			{
				return _releaseCooldownEnd <= Time.time;
			}
			return false;
		}
	}

	public void SetHasCritter(bool value)
	{
		if (hasCritter != value && !value)
		{
			_releaseCooldownEnd = Time.time + releaseCooldown;
		}
		hasCritter = value;
		UpdateCageVisuals();
	}

	public override void Initialize()
	{
		base.Initialize();
		hasCritter = false;
		heldByPlayer = false;
		inReleasingPosition = false;
		SetLidActive(active: true, playAudio: false);
	}

	private void UpdateCageVisuals()
	{
		SetLidActive(!heldByPlayer || hasCritter);
	}

	private void SetLidActive(bool active, bool playAudio = true)
	{
		if (active != _lidActive && playAudio)
		{
			sound.GTPlayOneShot(active ? openSound : closeSound);
		}
		lid.SetActive(active);
		_lidActive = active;
	}

	protected override void RemoteGrabbedBy(CrittersActor grabbingActor)
	{
		base.RemoteGrabbedBy(grabbingActor);
		heldByPlayer = grabbingActor.isOnPlayer;
		UpdateCageVisuals();
	}

	public override void GrabbedBy(CrittersActor grabbingActor, bool positionOverride = false, Quaternion localRotation = default(Quaternion), Vector3 localOffset = default(Vector3), bool disableGrabbing = false)
	{
		base.GrabbedBy(grabbingActor, positionOverride, localRotation, localOffset, disableGrabbing);
		heldByPlayer = grabbingActor.isOnPlayer;
		UpdateCageVisuals();
	}

	public override void Released(bool keepWorldPosition, Quaternion rotation = default(Quaternion), Vector3 position = default(Vector3), Vector3 impulseVelocity = default(Vector3), Vector3 impulseAngularVelocity = default(Vector3))
	{
		base.Released(keepWorldPosition, rotation, position, impulseVelocity, impulseAngularVelocity);
		heldByPlayer = false;
		UpdateCageVisuals();
	}

	protected override void HandleRemoteReleased()
	{
		base.HandleRemoteReleased();
		heldByPlayer = false;
		UpdateCageVisuals();
	}

	public override bool ShouldDespawn()
	{
		if (base.ShouldDespawn() && !hasCritter)
		{
			return true;
		}
		return false;
	}

	public override void SendDataByCrittersActorType(PhotonStream stream)
	{
		base.SendDataByCrittersActorType(stream);
		stream.SendNext(hasCritter);
	}

	public override bool UpdateSpecificActor(PhotonStream stream)
	{
		if (!base.UpdateSpecificActor(stream))
		{
			return false;
		}
		if (!CrittersManager.ValidateDataType<bool>(stream.ReceiveNext(), out var dataAsType))
		{
			return false;
		}
		SetHasCritter(dataAsType);
		return true;
	}

	public override int AddActorDataToList(ref List<object> objList)
	{
		base.AddActorDataToList(ref objList);
		objList.Add(hasCritter);
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
		SetHasCritter(dataAsType);
		return TotalActorDataLength();
	}
}
