using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;

public class CustomMapTelemetryTrigger : MonoBehaviour
{
	public void OnTriggerEnter(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			CustomMapTelemetry.OnPlayerLeftMap();
			if (CustomMapTelemetry.IsActive)
			{
				CustomMapTelemetry.EndMapTracking();
			}
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (other == GTPlayer.Instance.headCollider && GorillaComputer.instance.IsPlayerInVirtualStump())
		{
			CustomMapTelemetry.OnPlayerEnteredMap();
			if (!CustomMapTelemetry.IsActive)
			{
				CustomMapTelemetry.StartMapTracking();
			}
		}
	}
}
