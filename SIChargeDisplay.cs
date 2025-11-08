using UnityEngine;

public class SIChargeDisplay : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer[] chargeDisplay;

	[SerializeField]
	private Material chargedMat;

	[SerializeField]
	private Material unchargedMat;

	public void UpdateDisplay(int chargeCount)
	{
		for (int i = 0; i < chargeDisplay.Length; i++)
		{
			chargeDisplay[i].material = ((i < chargeCount) ? chargedMat : unchargedMat);
		}
	}
}
