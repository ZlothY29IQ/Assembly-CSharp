using GorillaExtensions;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class DistanceCheckerCosmetic : MonoBehaviour, ISpawnable, IGorillaSliceableSimple
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
		else
		{
			ownerRig = GetComponentInParent<VRRig>();
		}
		if (ownerRig == null)
		{
			ownerRig = GorillaTagger.Instance.offlineVRRig;
		}
		ResetClosestPlayer();
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
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
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			if (!others || !(ownerRig != null) || !(activeRig == ownerRig))
			{
				Vector3 distance = activeRig.transform.position - distanceFrom.position;
				if (IsBelowThreshold(distance) && distance.sqrMagnitude < closestDistance.sqrMagnitude)
				{
					closestDistance = distance;
					currentClosestPlayer = activeRig;
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
			foreach (VRRig activeRig in VRRigCache.ActiveRigs)
			{
				Vector3 distance2 = activeRig.transform.position - distanceFrom.position;
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
			foreach (VRRig activeRig2 in VRRigCache.ActiveRigs)
			{
				if (!(ownerRig != null) || !(activeRig2 == ownerRig))
				{
					Vector3 distance3 = activeRig2.transform.position - distanceFrom.position;
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
