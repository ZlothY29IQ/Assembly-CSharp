using UnityEngine;
using UnityEngine.Events;

public class SIHandScanner : MonoBehaviour
{
	public UnityEvent<int> onHandScanned;

	public void HandScanned(SIPlayer scannedPlayer)
	{
		if (scannedPlayer.gamePlayer.IsLocal())
		{
			onHandScanned.Invoke(NetworkSystem.Instance.LocalPlayerID);
		}
	}
}
