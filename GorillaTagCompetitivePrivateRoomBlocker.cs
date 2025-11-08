using UnityEngine;

public class GorillaTagCompetitivePrivateRoomBlocker : MonoBehaviour
{
	[SerializeField]
	private GameObject blocker;

	private void Update()
	{
		blocker.SetActive(NetworkSystem.Instance.SessionIsPrivate);
	}
}
