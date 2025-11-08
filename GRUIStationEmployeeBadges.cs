using System;
using System.Collections.Generic;
using UnityEngine;

public class GRUIStationEmployeeBadges : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	public List<GRUIEmployeeBadgeDispenser> badgeDispensers;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	public Dictionary<int, int> dispenserForActorNr;

	public List<GRBadge> registeredBadges;

	private GhostReactor reactor;

	public void Init(GhostReactor reactor)
	{
		this.reactor = reactor;
		for (int i = 0; i < badgeDispensers.Count; i++)
		{
			badgeDispensers[i].Setup(reactor, i);
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		registeredBadges = new List<GRBadge>();
		for (int i = 0; i < badgeDispensers.Count; i++)
		{
			badgeDispensers[i].index = i;
			badgeDispensers[i].actorNr = -1;
		}
		dispenserForActorNr = new Dictionary<int, int>();
		VRRigCache.OnRigActivated += UpdateRigs;
		VRRigCache.OnRigDeactivated += UpdateRigs;
		RoomSystem.JoinedRoomEvent += new Action(UpdateRigs);
		UpdateRigs();
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		VRRigCache.OnRigActivated -= UpdateRigs;
		VRRigCache.OnRigDeactivated -= UpdateRigs;
		RoomSystem.JoinedRoomEvent -= new Action(UpdateRigs);
	}

	public void UpdateRigs(RigContainer container)
	{
		UpdateRigs();
	}

	public void UpdateRigs()
	{
		tempRigs.Clear();
		tempRigs.Add(VRRig.LocalRig);
		if (VRRigCache.Instance != null)
		{
			VRRigCache.Instance.GetAllUsedRigs(tempRigs);
		}
	}

	public void RefreshBadgesAuthority()
	{
		for (int i = 0; i < tempRigs.Count; i++)
		{
			NetPlayer netPlayer = (tempRigs[i].isOfflineVRRig ? NetworkSystem.Instance.LocalPlayer : tempRigs[i].OwningNetPlayer);
			if (netPlayer == null || netPlayer.ActorNumber == -1 || dispenserForActorNr.TryGetValue(netPlayer.ActorNumber, out var _))
			{
				continue;
			}
			for (int j = 0; j < badgeDispensers.Count; j++)
			{
				if (badgeDispensers[j].actorNr == -1)
				{
					badgeDispensers[j].CreateBadge(netPlayer, reactor.grManager.gameEntityManager);
					break;
				}
			}
		}
		for (int num = registeredBadges.Count - 1; num >= 0; num--)
		{
			if (NetworkSystem.Instance.GetNetPlayerByID(registeredBadges[num].actorNr) == null || !dispenserForActorNr.TryGetValue(registeredBadges[num].actorNr, out var value2) || value2 != registeredBadges[num].dispenserIndex)
			{
				reactor.grManager.gameEntityManager.RequestDestroyItem(registeredBadges[num].GetComponent<GameEntity>().id);
			}
		}
	}

	public void SliceUpdate()
	{
		if (!(reactor == null) && !(reactor.grManager == null) && reactor.grManager.IsZoneActive())
		{
			if (reactor.grManager.gameEntityManager.IsAuthority())
			{
				RefreshBadgesAuthority();
			}
			for (int i = 0; i < badgeDispensers.Count; i++)
			{
				badgeDispensers[i].Refresh();
			}
		}
	}

	public void RemoveBadge(GRBadge badge)
	{
		if (registeredBadges.Contains(badge))
		{
			registeredBadges.Remove(badge);
		}
		if (badgeDispensers[badge.dispenserIndex].idBadge == badge)
		{
			dispenserForActorNr.Remove(badge.actorNr);
			badgeDispensers[badge.dispenserIndex].ClearBadge();
		}
	}

	public void LinkBadgeToDispenser(GRBadge badge, long createData)
	{
		if (!registeredBadges.Contains(badge))
		{
			registeredBadges.Add(badge);
		}
		int num = (int)(createData % 100);
		if (num <= badgeDispensers.Count)
		{
			NetPlayer netPlayerByID = NetworkSystem.Instance.GetNetPlayerByID((int)(createData / 100));
			if (netPlayerByID != null)
			{
				dispenserForActorNr[netPlayerByID.ActorNumber] = num;
				badgeDispensers[num].AttachIDBadge(badge, netPlayerByID);
			}
		}
	}

	public GRUIEmployeeBadgeDispenser GetDispenserForPlayer(int actorNumber)
	{
		if (!dispenserForActorNr.TryGetValue(actorNumber, out var value))
		{
			return null;
		}
		return badgeDispensers[value];
	}
}
