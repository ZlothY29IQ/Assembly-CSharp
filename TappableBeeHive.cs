using System;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class TappableBeeHive : Tappable
{
	[SerializeField]
	private GameObject swarmEmergeFromPoint;

	[SerializeField]
	private GameObject swarmEmergeToPoint;

	[SerializeField]
	private GameObject honeycombSurface;

	[SerializeField]
	private float honeycombDisableDuration;

	[NonSerialized]
	private TimeSince _timeSinceLastTap;

	private float reenableHoneycombAtTimestamp;

	private Coroutine reenableHoneycombCoroutine;

	private void Awake()
	{
		if (swarmEmergeFromPoint == null || swarmEmergeToPoint == null)
		{
			Debug.LogError("TappableBeeHive: Disabling because swarmEmergePoint is null at: " + base.transform.GetPath(), this);
			base.enabled = false;
		}
		else
		{
			GetComponent<SlingshotProjectileHitNotifier>().OnProjectileHit += OnSlingshotHit;
		}
	}

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped info)
	{
		if (Application.isPlaying && !(swarmEmergeFromPoint == null) && !(swarmEmergeToPoint == null) && NetworkSystem.Instance.IsMasterClient && AngryBeeSwarm.instance.isDormant)
		{
			AngryBeeSwarm.instance.Emerge(swarmEmergeFromPoint.transform.position, swarmEmergeToPoint.transform.position);
		}
	}

	public void OnSlingshotHit(SlingshotProjectile projectile, Collision collision)
	{
		if (Application.isPlaying && !(swarmEmergeFromPoint == null) && !(swarmEmergeToPoint == null) && PhotonNetwork.IsMasterClient && AngryBeeSwarm.instance.isDormant)
		{
			AngryBeeSwarm.instance.Emerge(swarmEmergeFromPoint.transform.position, swarmEmergeToPoint.transform.position);
		}
	}
}
