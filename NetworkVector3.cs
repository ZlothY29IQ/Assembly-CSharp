using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

internal class NetworkVector3
{
	private double lastSetNetTime;

	private Vector3 _currentSyncTarget = Vector3.zero;

	private Vector3 distanceTraveled = Vector3.zero;

	public Vector3 CurrentSyncTarget => _currentSyncTarget;

	public void SetNewSyncTarget(Vector3 newTarget)
	{
		Vector3 v = CurrentSyncTarget;
		v.SetValueSafe(in newTarget);
		distanceTraveled = v - _currentSyncTarget;
		_currentSyncTarget = v;
		lastSetNetTime = PhotonNetwork.Time;
	}

	public Vector3 GetPredictedFuture()
	{
		float num = (float)(PhotonNetwork.Time - lastSetNetTime) * (float)PhotonNetwork.SerializationRate;
		Vector3 vector = distanceTraveled * num;
		return _currentSyncTarget + vector;
	}

	public void Reset()
	{
		_currentSyncTarget = Vector3.zero;
		distanceTraveled = Vector3.zero;
		lastSetNetTime = 0.0;
	}
}
