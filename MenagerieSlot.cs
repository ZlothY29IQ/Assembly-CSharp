using TMPro;
using UnityEngine;

public class MenagerieSlot : MonoBehaviour
{
	public Transform critterMountPoint;

	public TMP_Text label;

	public MenagerieCritter critter;

	private void Reset()
	{
		critterMountPoint = base.transform;
	}
}
