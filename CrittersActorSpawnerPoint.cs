using System;
using System.Collections.Generic;
using Photon.Pun;

public class CrittersActorSpawnerPoint : CrittersActor
{
	private CrittersActor spawnedActor;

	private int spawnedActorID = -1;

	public event Action<CrittersActor> OnSpawnChanged;

	public override void Initialize()
	{
		base.Initialize();
		UpdateImpulses();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		spawnedActorID = -1;
		spawnedActor = null;
	}

	public void SetSpawnedActor(CrittersActor actor)
	{
		if (!(spawnedActor == actor))
		{
			spawnedActor = actor;
			if (spawnedActor != null)
			{
				spawnedActorID = spawnedActor.actorId;
			}
			else
			{
				spawnedActorID = -1;
			}
			this.OnSpawnChanged?.Invoke(spawnedActor);
			updatedSinceLastFrame = true;
		}
	}

	private void UpdateSpawnedActor(int newSpawnedActorID)
	{
		if (spawnedActorID == newSpawnedActorID)
		{
			return;
		}
		if (newSpawnedActorID == -1)
		{
			spawnedActorID = newSpawnedActorID;
			spawnedActor = null;
		}
		else
		{
			if (!CrittersManager.instance.actorById.TryGetValue(newSpawnedActorID, out var value))
			{
				return;
			}
			spawnedActorID = newSpawnedActorID;
			spawnedActor = value;
		}
		this.OnSpawnChanged?.Invoke(spawnedActor);
	}

	public override void SendDataByCrittersActorType(PhotonStream stream)
	{
		base.SendDataByCrittersActorType(stream);
		stream.SendNext(spawnedActorID);
	}

	public override bool UpdateSpecificActor(PhotonStream stream)
	{
		if (!base.UpdateSpecificActor(stream))
		{
			return false;
		}
		if (!CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType))
		{
			return false;
		}
		if (dataAsType < -1 || dataAsType >= CrittersManager.instance.universalActorId)
		{
			return false;
		}
		UpdateSpawnedActor(dataAsType);
		return true;
	}

	public override int AddActorDataToList(ref List<object> objList)
	{
		base.AddActorDataToList(ref objList);
		objList.Add(spawnedActorID);
		return TotalActorDataLength();
	}

	public override int TotalActorDataLength()
	{
		return BaseActorDataLength() + 1;
	}

	public override int UpdateFromRPC(object[] data, int startingIndex)
	{
		startingIndex += base.UpdateFromRPC(data, startingIndex);
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex], out var dataAsType))
		{
			return TotalActorDataLength();
		}
		if (dataAsType >= -1 && dataAsType < CrittersManager.instance.universalActorId)
		{
			return TotalActorDataLength();
		}
		UpdateSpawnedActor(dataAsType);
		return TotalActorDataLength();
	}
}
