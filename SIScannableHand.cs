using UnityEngine;

public class SIScannableHand : MonoBehaviour
{
	public SIPlayer parentPlayer;

	private void Awake()
	{
		parentPlayer = GetComponentInParent<SIPlayer>();
	}
}
