using GorillaTag;
using UnityEngine;

public class ZoneRootRegister : MonoBehaviour
{
	[SerializeField]
	private WatchableGameObjectSO watchableSlot;

	private void Awake()
	{
		watchableSlot.Value = base.gameObject;
	}

	private void OnDestroy()
	{
		watchableSlot.Value = null;
	}
}
