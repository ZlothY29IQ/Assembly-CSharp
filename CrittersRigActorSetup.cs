using System;
using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class CrittersRigActorSetup : MonoBehaviour
{
	[Serializable]
	public struct RigActor
	{
		public Transform location;

		public CrittersActor.CrittersActorType type;

		public int subIndex;

		public CrittersActor actorSet;
	}

	public RigActor[] rigActors;

	public List<object> rigActorData = new List<object>();

	public VRRig myRig;

	public void OnEnable()
	{
		CrittersManager.RegisterRigActorSetup(this);
	}

	public void OnDisable()
	{
		for (int i = 0; i < rigActors.Length; i++)
		{
			rigActors[i].actorSet = null;
		}
	}

	private CrittersActor RefreshActorForIndex(int index)
	{
		RigActor rigActor = rigActors[index];
		if (rigActor.actorSet.IsNotNull())
		{
			rigActor.actorSet.gameObject.SetActive(value: false);
		}
		CrittersActor crittersActor = CrittersManager.instance.SpawnActor(rigActor.type, rigActor.subIndex);
		if (crittersActor.IsNull())
		{
			return null;
		}
		crittersActor.isOnPlayer = true;
		crittersActor.rigIndex = index;
		crittersActor.rigPlayerId = myRig.Creator.ActorNumber;
		if (crittersActor.rigPlayerId == -1 && PhotonNetwork.InRoom)
		{
			crittersActor.rigPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
		}
		crittersActor.PlacePlayerCrittersActor();
		return crittersActor;
	}

	public void CheckUpdate(ref List<object> refActorData, bool forceCheck = false)
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return;
		}
		for (int i = 0; i < rigActors.Length; i++)
		{
			RigActor rigActor = rigActors[i];
			if (forceCheck || rigActor.actorSet == null || (rigActor.actorSet.rigPlayerId != myRig.Creator.ActorNumber && VRRigCache.Instance.TryGetVrrig(myRig.Creator, out var _) && CrittersManager.instance.rigSetupByRig.ContainsKey(myRig)))
			{
				RefreshActorForIndex(i)?.AddPlayerCrittersActorDataToList(ref refActorData);
			}
		}
	}
}
