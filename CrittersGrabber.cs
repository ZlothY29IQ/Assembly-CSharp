using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CrittersGrabber : CrittersActor
{
	public Transform grabPosition;

	public bool grabbing;

	public float grabDistance;

	public List<CrittersActor> grabbedActors = new List<CrittersActor>();

	public bool isLeft;

	public override void ProcessRemote()
	{
		if (rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			UpdateAverageSpeed();
		}
	}

	public override bool ProcessLocal()
	{
		if (rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			UpdateAverageSpeed();
		}
		return base.ProcessLocal();
	}
}
