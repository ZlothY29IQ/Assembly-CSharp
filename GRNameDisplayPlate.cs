using TMPro;
using UnityEngine;

public class GRNameDisplayPlate : MonoBehaviour
{
	public TMP_Text namePlateLabel;

	public void RefreshPlayerName(VRRig vrRig)
	{
		GRPlayer gRPlayer = GRPlayer.Get(vrRig);
		if (vrRig != null && gRPlayer != null)
		{
			if (!namePlateLabel.text.Equals(vrRig.playerNameVisible))
			{
				namePlateLabel.text = vrRig.playerNameVisible;
			}
		}
		else
		{
			namePlateLabel.text = "";
		}
	}

	public void Clear()
	{
		namePlateLabel.text = "";
	}
}
