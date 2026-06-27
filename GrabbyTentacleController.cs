using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GrabbyTentacleController : MonoBehaviour
{
	[SerializeField]
	private TentacleTracker[] tentacles;

	[SerializeField]
	private BoxCollider grabRegion;

	[SerializeField]
	private float minRetryDelay = 1f;

	[SerializeField]
	private float maxRetryDelay = 2f;

	private float nextAttemptTimestamp;

	private readonly HashSet<int> grabbedBefore = new HashSet<int>();

	private readonly List<VRRig> candidateBuffer = new List<VRRig>(16);

	private readonly List<VRRig> freshCandidates = new List<VRRig>(16);

	private void OnEnable()
	{
		if (tentacles != null)
		{
			_ = tentacles.Length;
		}
		nextAttemptTimestamp = 0f;
		grabbedBefore.Clear();
		if (GrabbyTentacleNetworking.Instance != null)
		{
			GrabbyTentacleNetworking.Instance.Register(this);
		}
		else
		{
			Debug.LogError("[GrabbyTentacleController] No GrabbyTentacleNetworking.Instance at OnEnable — is the main scene loaded?");
		}
	}

	private void OnDisable()
	{
		if (GrabbyTentacleNetworking.Instance != null)
		{
			GrabbyTentacleNetworking.Instance.Unregister(this);
		}
	}

	private void Update()
	{
		if ((PhotonNetwork.InRoom && (!PhotonNetwork.IsMasterClient || GrabbyTentacleNetworking.Instance == null)) || tentacles == null || tentacles.Length == 0 || grabRegion == null)
		{
			return;
		}
		for (int i = 0; i < tentacles.Length; i++)
		{
			if (nextAttemptTimestamp > Time.time)
			{
				break;
			}
			TentacleTracker tentacleTracker = tentacles[i];
			if (tentacleTracker == null || tentacleTracker.gameObject.activeSelf)
			{
				continue;
			}
			nextAttemptTimestamp = Time.time + Random.Range(minRetryDelay, maxRetryDelay);
			Player player = PickTarget();
			if (player != null)
			{
				grabbedBefore.Add(player.ActorNumber);
				if (PhotonNetwork.InRoom)
				{
					GrabbyTentacleNetworking.Instance.SendGrab(i, player);
				}
				else
				{
					OnGrabReceived(i, VRRig.LocalRig, isLocalPlayer: true);
				}
			}
		}
	}

	private Player PickTarget()
	{
		candidateBuffer.Clear();
		freshCandidates.Clear();
		IReadOnlyList<VRRig> activeRigs = VRRigCache.ActiveRigs;
		for (int i = 0; i < activeRigs.Count; i++)
		{
			VRRig vRRig = activeRigs[i];
			if (vRRig == null || vRRig.Creator == null || vRRig.Creator.IsNull || IsRigCurrentlyGrabbed(vRRig))
			{
				continue;
			}
			Vector3 vector = ((vRRig.head != null && vRRig.head.rigTarget != null) ? vRRig.head.rigTarget.position : vRRig.transform.position);
			if (!(grabRegion.ClosestPoint(vector) != vector))
			{
				candidateBuffer.Add(vRRig);
				if (!grabbedBefore.Contains(vRRig.Creator.ActorNumber))
				{
					freshCandidates.Add(vRRig);
				}
			}
		}
		List<VRRig> list = ((freshCandidates.Count > 0) ? freshCandidates : candidateBuffer);
		if (list.Count == 0)
		{
			return null;
		}
		VRRig vRRig2 = list[Random.Range(0, list.Count)];
		return PhotonNetwork.CurrentRoom?.GetPlayer(vRRig2.Creator.ActorNumber);
	}

	private bool IsRigCurrentlyGrabbed(VRRig rig)
	{
		for (int i = 0; i < tentacles.Length; i++)
		{
			TentacleTracker tentacleTracker = tentacles[i];
			if (tentacleTracker != null && tentacleTracker.gameObject.activeSelf && tentacleTracker.currentTargetRig == rig)
			{
				return true;
			}
		}
		return false;
	}

	public void OnGrabReceived(int tentacleIndex, VRRig targetRig, bool isLocalPlayer)
	{
		if (tentacles != null && tentacleIndex >= 0 && tentacleIndex < tentacles.Length)
		{
			TentacleTracker tentacleTracker = tentacles[tentacleIndex];
			if (!(tentacleTracker == null))
			{
				tentacleTracker.BeginGrab(targetRig, isLocalPlayer);
			}
		}
	}
}
