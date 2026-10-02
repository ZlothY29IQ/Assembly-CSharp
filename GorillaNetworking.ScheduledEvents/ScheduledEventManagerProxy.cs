using UnityEngine;

namespace GorillaNetworking.ScheduledEvents;

public class ScheduledEventManagerProxy : MonoBehaviour
{
	public void SetEventSubphase(int subphase)
	{
		ScheduledEventManager.Instance.SetEventSubphase(subphase);
	}

	public void End()
	{
		ScheduledEventManager.Instance.OnShowEnded();
	}
}
