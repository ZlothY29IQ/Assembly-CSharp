using GorillaExtensions;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class DistanceCheckerCosmetic : MonoBehaviour, ISpawnable
{
	private enum State
	{
		AboveThreshold,
		BelowThreshold,
		None
	}

	private enum DistanceCondition
	{
		None,
		Owner,
		Others,
		Everyone
	}

	[SerializeField]
	private Transform distanceFrom;

	[SerializeField]
	private DistanceCondition distanceTo;

	[Tooltip("Receive events when above or below this distance")]
	public float distanceThreshold;

	public UnityEvent onOneIsBelowThreshold;

	public UnityEvent onAllAreAboveThreshold;

	public UnityEvent<VRRig, float> onClosestPlayerBelowThresholdChanged;

	private VRRig myRig;

	private State currentState;

	private Vector3 closestDistance;

	private VRRig currentClosestPlayer;

	private VRRig ownerRig;

	private TransferrableObject transferableObject;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void OnSpawn(VRRig rig)
	{
		myRig = rig;
	}

	public void OnDespawn()
	{
	}

	private void OnEnable()
	{
		currentState = State.None;
		transferableObject = GetComponentInParent<TransferrableObject>();
		if (transferableObject != null)
		{
			ownerRig = transferableObject.ownerRig;
		}
		ResetClosestPlayer();
	}

	private void Update()
	{
		UpdateDistance();
	}

	private bool IsBelowThreshold(Vector3 distance)
	{
		if (distance.IsShorterThan(distanceThreshold))
		{
			return true;
		}
		return false;
	}

	private bool IsAboveThreshold(Vector3 distance)
	{
		if (distance.IsLongerThan(distanceThreshold))
		{
			return true;
		}
		return false;
	}

	private void UpdateClosestPlayer(bool others = false)
	{
		if (!PhotonNetwork.InRoom)
		{
			ResetClosestPlayer();
			return;
		}
		VRRig vRRig = currentClosestPlayer;
		closestDistance = Vector3.positiveInfinity;
		currentClosestPlayer = null;
		foreach (VRRig vrrig in GorillaParent.instance.vrrigs)
		{
			if (!others || !(ownerRig != null) || !(vrrig == ownerRig))
			{
				Vector3 distance = vrrig.transform.position - distanceFrom.position;
				if (IsBelowThreshold(distance) && distance.sqrMagnitude < closestDistance.sqrMagnitude)
				{
					closestDistance = distance;
					currentClosestPlayer = vrrig;
				}
			}
		}
		if (currentClosestPlayer != null && currentClosestPlayer != vRRig)
		{
			onClosestPlayerBelowThresholdChanged?.Invoke(currentClosestPlayer, closestDistance.magnitude);
		}
	}

	private void ResetClosestPlayer()
	{
		closestDistance = Vector3.positiveInfinity;
		currentClosestPlayer = null;
	}

	private void UpdateDistance()
	{
		bool flag = true;
		switch (distanceTo)
		{
		case DistanceCondition.Everyone:
			UpdateClosestPlayer();
			if (!PhotonNetwork.InRoom)
			{
				break;
			}
			foreach (VRRig vrrig in GorillaParent.instance.vrrigs)
			{
				Vector3 distance2 = vrrig.transform.position - distanceFrom.position;
				if (IsBelowThreshold(distance2))
				{
					UpdateState(State.BelowThreshold);
					flag = false;
				}
			}
			if (flag)
			{
				UpdateState(State.AboveThreshold);
			}
			break;
		case DistanceCondition.Others:
			UpdateClosestPlayer(others: true);
			if (!PhotonNetwork.InRoom)
			{
				break;
			}
			foreach (VRRig vrrig2 in GorillaParent.instance.vrrigs)
			{
				if (!(ownerRig != null) || !(vrrig2 == ownerRig))
				{
					Vector3 distance3 = vrrig2.transform.position - distanceFrom.position;
					if (IsBelowThreshold(distance3))
					{
						UpdateState(State.BelowThreshold);
						flag = false;
					}
				}
			}
			if (flag)
			{
				UpdateState(State.AboveThreshold);
			}
			break;
		case DistanceCondition.Owner:
		{
			Vector3 distance = myRig.transform.position - distanceFrom.position;
			if (IsBelowThreshold(distance))
			{
				UpdateState(State.BelowThreshold);
			}
			else if (IsAboveThreshold(distance))
			{
				UpdateState(State.AboveThreshold);
			}
			break;
		}
		}
	}

	private void UpdateState(State newState)
	{
		if (currentState != newState)
		{
			currentState = newState;
			if (currentState == State.AboveThreshold)
			{
				onAllAreAboveThreshold?.Invoke();
			}
			else if (currentState == State.BelowThreshold)
			{
				onOneIsBelowThreshold?.Invoke();
			}
		}
	}
}
